using UnityEngine;
using UnityEngine.AI;
// System.Collections Coroutine için gerekli değil, kaldırılabilir.

/// <summary>
/// Sahur karakterinin yapay zeka davranışlarını yönetir.
/// Belirlenen devriye noktaları arasında rastgele gezer ve belirli aralıklarla boşta durur.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class SahurAIController : MonoBehaviour
{
    // --- Inspector Ayarları ---
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2.5f;
    [Tooltip("Sahur'un hedefe ne kadar yaklaşınca duracağını belirler. NavMeshAgent'ın stoppingDistance'ı ile senkronize olmalı.")]
    [SerializeField] private float agentStoppingDistance = 0.5f;

    [Header("State Durations")]
    [Tooltip("Sahur'un 'Idle' animasyonunda kalacağı süre (saniye).")]
    [SerializeField] private float idleDuration = 3.0f;
    [Tooltip("Sahur'un 'Patrolling' state'inde bir hedefe doğru giderken en az kalacağı süre (saniye). Bu süre dolmadan ve hedefe varmadan Idle'a geçmez.")]
    [SerializeField] private float minPatrolDuration = 5.0f;

    [Header("Animation Settings")]
    [Tooltip("Animator'daki yürüme durumunu kontrol eden boolean parametresinin adı.")]
    [SerializeField] private string isWalkingParameterName = "IsWalking";

    [Header("Patrol Points")]
    [Tooltip("Sahur karakterinin gideceği devriye noktalarının ebeveyn nesnesi.")]
    [SerializeField] private Transform patrolPointsParent;

    [Header("Navigation Anti-Repetition")]
    [Tooltip("Kaç tane son ziyaret edilen hedefin (pozisyonunun) hatırlanacağını belirler.")]
    [SerializeField] private int maxRecentDestinationsToRemember = 3; // Daha küçük bir değer daha iyi olabilir
    [Tooltip("Yeni bir hedefin, son ziyaret edilen hedeflere ne kadar yakın olabileceğini belirleyen minimum mesafe. Bu, aynı patrol point'e geri dönmeyi engellemek için kullanılır.")]
    [SerializeField] private float minDistanceToRecentTarget = 0.5f; // Neredeyse aynı nokta olmamasını sağlar
    [Tooltip("Yeni, tekrarlamayan bir devriye noktası bulmak için kaç kez deneneceğini belirler.")]
    [SerializeField] private int maxDestinationFindAttempts = 10;

    [Header("Door Interaction")]
    [Tooltip("Sahur'un kapılarla etkileşime gireceği mesafe.")]
    [SerializeField] private float doorInteractionDistance = 1.5f;
    [Tooltip("Kapıların bulunduğu katman.")]
    [SerializeField] private LayerMask doorLayer;
    [Tooltip("Kapıyı açtıktan sonra hareket etmeden önce bekleme süresi (saniye).")]
    [SerializeField] private float doorOpenWaitTime = 1.2f; // Kapı animasyon süresine göre ayarlanabilir

    [Header("Chase Settings")]
    [Tooltip("Oyuncuyu kovalama hızı.")]
    [SerializeField] private float chaseSpeed = 4.0f;
    [Tooltip("Animator'daki hızlı koşma durumunu kontrol eden boolean parametresinin adı.")]
    [SerializeField] private string isChasingParameterName = "IsChasing";
    [Tooltip("Oyuncunun Transform referansı. Otomatik olarak Player tag'i ile bulunmaya çalışılır.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Oyuncuyu yakaladığını kabul edeceği mesafe.")]
    [SerializeField] private float playerCatchDistance = 1.0f;

    [Header("Agent Turning Settings")] // Yeni ayarlar
    [Tooltip("Ajanın saniyede dönebileceği maksimum hız (derece). Yüksek değerler daha hızlı dönüş sağlar.")]
    [SerializeField] private float agentAngularSpeed = 360f; // Daha hızlı bir varsayılan değer
    [Tooltip("Ajanın hızlanma oranı. Yüksek değerler daha çabuk hızlanmasını sağlar.")]
    [SerializeField] private float agentAcceleration = 12f; // Daha hızlı bir varsayılan değer

    [Header("Stuck Detection")] 
    [Tooltip("Ajanın takıldığını varsaymadan önce ne kadar süre hareketsiz kalabileceği (saniye).")]
    [SerializeField] private float maxStuckTime = 0.2f; // GEÇİCİ: Agresif test için çok düşük bir değere ayarlandı (0.2f)

    // --- Özel Değişkenler ---
    private NavMeshAgent agent;
    private Animator animator;

    private enum AIState
    {
        Initializing,
        Idle,
        Patrolling,
        OpeningDoor,
        ChasingPlayer
    }
    private AIState currentState;

    private float currentIdleTimer;
    private float currentPatrolTimer;
    private int isWalkingAnimatorHash;
    private int isChasingAnimatorHash;

    private System.Collections.Generic.List<Transform> availablePatrolPoints;
    private System.Collections.Generic.List<Vector3> recentlyVisitedPositions; // Artık pozisyonları saklayacağız

    private Door currentTargetDoor = null;
    private float doorWaitTimer;
    private float stuckTimer = 0f; // Takılma sayacı

    // Chase state için yeni değişkenler
    private float chasePathRecalculateTimer; 
    private const float ChasePathRecalculateInterval = 0.3f; 
    private Vector3 lastPlayerPositionForPathRecalculation; 
    private const float PlayerMoveThresholdForPathRecalcSqr = 0.25f; // 0.5 birim kare = 0.25f

    // PathPending takılmasını tespit için yeni değişkenler
    private float currentPathPendingTimer = 0f;
    private const float MaxPathPendingDuration = 1.0f; // Ajanın en fazla ne kadar süre PathPending'de kalabileceği (saniye)

    // --- Unity Mesajları ---

    private void OnEnable()
    {
        BurnMechanismController.OnBabyTungBurned += HandleBabyBurned;
    }

    private void OnDisable()
    {
        BurnMechanismController.OnBabyTungBurned -= HandleBabyBurned;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        bool initializationError = false;
        if (agent == null)
        {
            Debug.LogError("[SahurAIController] NavMeshAgent bileşeni bu GameObject üzerinde bulunamadı!", this);
            initializationError = true;
        }
        if (animator == null)
        {
            Debug.LogError("[SahurAIController] Animator bileşeni bu GameObject üzerinde bulunamadı!", this);
            initializationError = true;
        }
        else
        {
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError("[SahurAIController] Animator bileşenine bir Animator Controller atanmamış!", this);
                initializationError = true;
            }
            isWalkingAnimatorHash = Animator.StringToHash(isWalkingParameterName);
            isChasingAnimatorHash = Animator.StringToHash(isChasingParameterName);
        }

        if (initializationError)
        {
            Debug.LogError("[SahurAIController] Başlatma sırasında kritik hatalar bulundu. Betik devre dışı bırakılıyor.", this);
            enabled = false;
            currentState = AIState.Initializing;
            return;
        }

        recentlyVisitedPositions = new System.Collections.Generic.List<Vector3>();
        availablePatrolPoints = new System.Collections.Generic.List<Transform>();
        Debug.Log("[SahurAIController] Awake tamamlandı.", this);
    }

    private void Start()
    {
        if (!enabled) return;

        agent.speed = walkSpeed;
        agent.stoppingDistance = agentStoppingDistance;

        // NavMeshAgent dönüş ayarları
        agent.updateRotation = true; // Ajanın rotasyonu kendisinin güncellemesini sağla
        agent.angularSpeed = agentAngularSpeed;
        agent.acceleration = agentAcceleration;

        InitializePatrolPoints();

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                Debug.Log("[SahurAIController] Player Transform referansı 'Player' tag'i ile bulundu.", this);
            }
            else
            {
                Debug.LogError("[SahurAIController] Player Transform referansı Inspector'dan atanmamış ve 'Player' tag'ine sahip bir obje bulunamadı! Kovalama özelliği çalışmayabilir.", this);
                // Oyuncu yoksa kovalama mantığı düzgün çalışmaz, bu durumda bazı özellikler devre dışı bırakılabilir veya hata verilebilir.
            }
        }

        if (availablePatrolPoints.Count == 0 && currentState != AIState.ChasingPlayer) // Eğer chase ile başlıyorsa patrol point olmaması sorun değil
        {
            Debug.LogError("[SahurAIController] Başlangıçta hiç devriye noktası bulunamadı! Lütfen 'PatrolPointsParent' altına devriye noktaları ekleyin ve atamayı kontrol edin. Betik devre dışı bırakılıyor.", this);
            enabled = false;
            currentState = AIState.Initializing; // Hatalı durumu belirt
            return;
        }

        Debug.Log("[SahurAIController] Start: Sahur AI başlatılıyor. Varsayılan durum: Idle.", this);
        SwitchToState(AIState.Idle);
    }

    private void InitializePatrolPoints()
    {
        if (patrolPointsParent == null)
        {
            Debug.LogError("[SahurAIController] PatrolPointsParent atanmamış! Inspector üzerinden atama yapın.", this);
            // Hata durumunda availablePatrolPoints boş kalacak ve Start içinde kontrol edilecek.
            return;
        }

        availablePatrolPoints.Clear();
        if (patrolPointsParent.childCount == 0)
        {
            Debug.LogWarning("[SahurAIController] PatrolPointsParent altında hiç devriye noktası (child) bulunamadı.", this);
        }
        else
        {
            foreach (Transform child in patrolPointsParent)
            {
                if (child != null && child != transform) // Kendisini eklememeli
                {
                    availablePatrolPoints.Add(child);
                }
            }
            Debug.Log($"[SahurAIController] {availablePatrolPoints.Count} adet devriye noktası bulundu.", this);
        }
    }

    private void Update()
    {
        if (!enabled) return;

        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdleState();
                break;
            case AIState.Patrolling:
                UpdatePatrollingState();
                break;
            case AIState.OpeningDoor:
                UpdateOpeningDoorState();
                break;
            case AIState.ChasingPlayer:
                UpdateChasingPlayerState();
                break;
        }
    }

    // --- Durum Yönetimi ---

    private void SwitchToState(AIState newState)
    {
        if (currentState == newState && currentState != AIState.Initializing) return;

        // Debug.Log($"[SahurAIController] Durum Değişikliği: {currentState} -> {newState}", this);
        currentState = newState;

        switch (currentState)
        {
            case AIState.Idle:
                EnterIdleState();
                break;
            case AIState.Patrolling:
                EnterPatrollingState();
                break;
            case AIState.OpeningDoor:
                EnterOpeningDoorState();
                break;
            case AIState.ChasingPlayer:
                EnterChasingPlayerState();
                break;
        }
    }

    // --- Idle Durumu ---
    private void EnterIdleState()
    {
        Debug.Log("[SahurAIController] Idle durumuna girildi.", this);
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        animator.SetBool(isWalkingAnimatorHash, false);
        animator.SetBool(isChasingAnimatorHash, false); // Kovalama animasyonunu da kapat
        currentIdleTimer = idleDuration;
    }

    private void UpdateIdleState()
    {
        currentIdleTimer -= Time.deltaTime;
        if (currentIdleTimer <= 0f)
        {
            SwitchToState(AIState.Patrolling);
        }
    }

    // --- Patrolling Durumu ---
    private void EnterPatrollingState()
    {
        Debug.Log("[SahurAIController] Patrolling durumuna girildi.", this);
        agent.speed = walkSpeed; // Hızı normale ayarla
        agent.angularSpeed = agentAngularSpeed;
        agent.acceleration = agentAcceleration;
        agent.stoppingDistance = agentStoppingDistance; // Durma mesafesini normale ayarla
        agent.isStopped = false; // Harekete izin ver
        animator.SetBool(isChasingAnimatorHash, false); // Kovalama animasyonunu kapat (eğer açıksa)
        animator.SetBool(isWalkingAnimatorHash, true); // Yürüme animasyonunu aç
        currentPatrolTimer = minPatrolDuration; 
        TrySetNewRandomDestination();
        Debug.Log($"[SahurAIController PATROL_ENTER] Agent Speed: {agent.speed}, IsStopped: {agent.isStopped}");
    }

    private void UpdatePatrollingState()
    {
        currentPatrolTimer -= Time.deltaTime;

        // Yolda kapı kontrolü
        if (agent.hasPath && agent.remainingDistance > agentStoppingDistance) // Sadece hareket halindeyken ve hedefe varmamışken kontrol et
        {
            RaycastHit hit;
            Vector3 directionToTarget = (agent.steeringTarget - transform.position).normalized;
            float distanceToSteeringTarget = Vector3.Distance(transform.position, agent.steeringTarget);
            float checkDistance = Mathf.Min(doorInteractionDistance, distanceToSteeringTarget); // Kapıya çarpacak kadar yakın mı diye bakarız

            // Debug.DrawRay(transform.position + Vector3.up * 0.5f, directionToTarget * checkDistance, Color.magenta, 0.1f);

            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToTarget, out hit, checkDistance, doorLayer))
            {
                Door door = hit.collider.GetComponent<Door>();
                if (door != null && !door.isOpen)
                {
                    Debug.Log($"[SahurAIController] Yol üzerinde kapalı kapı ({door.gameObject.name}) tespit edildi. Açılacak.", this);
                    currentTargetDoor = door;
                    SwitchToState(AIState.OpeningDoor);
                    return; // OpeningDoor durumuna geçildiği için bu frame'de başka işlem yapma
                }
            }
        }

        bool hasReachedDestination = !agent.pathPending &&
                                     agent.remainingDistance <= agent.stoppingDistance &&
                                     (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f);

        if (hasReachedDestination)
        {
            Debug.Log("[SahurAIController] Hedefe ulaşıldı.", this);
            // Hedefe ulaşıldı. Şimdi minPatrolDuration dolmuş mu diye bak.
            if (currentPatrolTimer <= 0f)
            {
                Debug.Log("[SahurAIController] Minimum devriye süresi doldu. Idle durumuna geçiliyor.", this);
                SwitchToState(AIState.Idle);
            }
            else
            {
                // Süre dolmadı ama hedefe vardı, hemen yeni bir hedef arasın.
                Debug.Log("[SahurAIController] Minimum devriye süresi henüz dolmadı ama hedefe ulaşıldı. Yeni hedef aranıyor.", this);
                TrySetNewRandomDestination();
            }
        }
        // Eğer hedefe henüz ulaşılmadıysa ve currentPatrolTimer > 0 ise yürümeye devam et.
        // Eğer hedefe henüz ulaşılmadıysa AMA currentPatrolTimer <= 0 ise,
        // karakter hedefe ulaşana kadar yürümeye devam ETMELİ.
        // Zamanlayıcı sadece "bir sonraki hedef ne zaman seçilmeli" veya "Idle'a ne zaman geçilmeli"
        // kararını, hedefe ULAŞILDIKTAN SONRA etkilemeli.
    }

    // --- OpeningDoor Durumu --- (Yeni eklenecek)
    private void EnterOpeningDoorState()
    {
        Debug.Log("[SahurAIController] OpeningDoor durumuna girildi.", this);
        agent.isStopped = true; // Kapıyı açarken dursun
        animator.SetBool(isWalkingAnimatorHash, false); // Yürüme animasyonunu kapat

        if (currentTargetDoor != null && !currentTargetDoor.isOpen)
        {
            Debug.Log($"[SahurAIController] {currentTargetDoor.gameObject.name} kapısı açılıyor.", this);
            currentTargetDoor.OpenDoor();
            doorWaitTimer = doorOpenWaitTime;
        }
        else
        {
            // Hedef kapı yoksa veya zaten açıksa, hemen devriyeye dön
            Debug.LogWarning("[SahurAIController] OpeningDoor durumuna girildi ama hedef kapı yok veya zaten açık. Patrolling durumuna dönülüyor.", this);
            SwitchToState(AIState.Patrolling);
        }
    }

    private void UpdateOpeningDoorState()
    {
        doorWaitTimer -= Time.deltaTime;
        if (doorWaitTimer <= 0f)
        {
            Debug.Log("[SahurAIController] Kapı açma bekleme süresi doldu. Patrolling durumuna dönülüyor.", this);
            currentTargetDoor = null; // Kapı referansını temizle
            SwitchToState(AIState.Patrolling); // Devriyeye devam et
            // Patrolling durumuna geçerken EnterPatrollingState içinde yeni hedef belirlenecek veya mevcut hedefe devam edilecek.
            // Eğer aynı hedefe devam ediliyorsa ve kapı açıldıysa, artık ilerleyebilmesi lazım.
            // Bu yüzden EnterPatrollingState içinde agent.isStopped = false; tekrar çağrılacak.
        }
    }

    // --- ChasingPlayer Durumu --- (Yeni eklenecek)
    private void EnterChasingPlayerState()
    {
        Debug.Log("[SahurAIController] ChasingPlayer durumuna girildi!", this);
        if (playerTransform == null)
        {
            Debug.LogError("[SahurAIController] Oyuncu referansı yok! ChasingPlayer durumu düzgün çalışamaz. Idle durumuna geçiliyor.", this);
            SwitchToState(AIState.Idle);
            return;
        }

        agent.speed = chaseSpeed;
        agent.angularSpeed = agentAngularSpeed; 
        agent.acceleration = agentAcceleration; 
        agent.isStopped = false; 
        animator.SetBool(isWalkingAnimatorHash, false); 
        animator.SetBool(isChasingAnimatorHash, true);  // DİKKAT: Burası isChasingAnimatorHash olmalıydı, düzeltiyorum.
        agent.stoppingDistance = playerCatchDistance; 
        stuckTimer = 0f; 
        currentPathPendingTimer = 0f; // PathPending zamanlayıcısını sıfırla
        
        chasePathRecalculateTimer = 0f; 
        lastPlayerPositionForPathRecalculation = playerTransform.position + (Vector3.one * 100f); 

        Debug.Log($"[SahurAIController CHASE_ENTER] Agent Speed: {agent.speed}, StoppingDistance: {agent.stoppingDistance}", this);
    }

    private void UpdateChasingPlayerState()
    {
        Debug.Log("[SahurAIController] UpdateChasingPlayerState ÇAĞRILDI!", this);

        if (playerTransform == null)
        {
            Debug.LogWarning("[SahurAIController] ChasingPlayer: Oyuncu referansı kayboldu! Idle durumuna geçiliyor.", this);
            SwitchToState(AIState.Idle);
            return;
        }
        
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) // isOnNavMesh kontrolü eklendi
        {
            Debug.LogError($"[SahurAIController CHASE_UPDATE] NavMeshAgent null, devre dışı veya NavMesh üzerinde değil! Agent Durumu: {(agent == null ? "Null" : agent.enabled.ToString())}, IsOnNavMesh: {(agent == null ? "N/A" : agent.isOnNavMesh.ToString())}", this);
            SwitchToState(AIState.Idle); 
            return;
        }

        // Ajanın kendi pozisyonunu NavMesh'e göre doğrula (çok küçük bir arama ile)
        NavMeshHit agentPosHit;
        if (agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out agentPosHit, 0.5f, NavMesh.AllAreas))
        {
            if (Vector3.Distance(transform.position, agentPosHit.position) > 0.1f)
            {
                if (!agent.Warp(agentPosHit.position))
                {
                    Debug.LogError("[SahurAIController CHASE_UPDATE] Ajanı NavMesh'e warp etmek BAŞARISIZ OLDU.", this);
                }
            }
        }
        else if (!agent.isOnNavMesh)
        {
            Debug.LogError($"[SahurAIController CHASE_UPDATE] Ajan NavMesh üzerinde değil! ({transform.position}). Kovalama durduruluyor.", this);
            SwitchToState(AIState.Idle);
            return;
        }
        // Eğer SamplePosition başarısız oldu ama ajan NavMesh üzerindeyse, bu genellikle küçük bir tutarsızlıktır, devam edilebilir.

        // Hedef Belirleme Mantığı
        chasePathRecalculateTimer -= Time.deltaTime;
        Vector3 currentPlayerPos = playerTransform.position;
        float playerMovementSinceLastRecalcSqr = (currentPlayerPos - lastPlayerPositionForPathRecalculation).sqrMagnitude;

        if (chasePathRecalculateTimer <= 0f || 
            playerMovementSinceLastRecalcSqr > PlayerMoveThresholdForPathRecalcSqr || 
            !agent.hasPath || 
            agent.pathStatus != NavMeshPathStatus.PathComplete) // PathComplete değilse (yani Partial veya Invalid ise) yolu yeniden hesapla
        {
            if (!agent.pathPending && agent.isOnNavMesh)
            {
                Vector3 targetNavMeshPosition = currentPlayerPos;
                NavMeshHit playerNavMeshHit;
                float sampleRadius = 3.0f; // SamplePosition arama yarıçapını biraz artıralım
                if (NavMesh.SamplePosition(currentPlayerPos, out playerNavMeshHit, sampleRadius, NavMesh.AllAreas))
                {
                    targetNavMeshPosition = playerNavMeshHit.position;
                }
                else
                {
                    Debug.LogWarning($"[SahurAIController CHASE_UPDATE] Oyuncunun pozisyonu ({currentPlayerPos}) için {sampleRadius}m yarıçapında NavMesh üzerinde geçerli bir nokta bulunamadı. Ham pozisyon kullanılacak.", this);
                }

                bool setDestSuccess = agent.SetDestination(targetNavMeshPosition);
                if (setDestSuccess)
                {
                    lastPlayerPositionForPathRecalculation = currentPlayerPos; // Başarıyla hedef ayarlandıysa zamanlayıcıyı ve pozisyonu güncelle
                    chasePathRecalculateTimer = ChasePathRecalculateInterval;
                    // Debug.Log($"[SahurAIController CHASE_UPDATE] Yeni kovalama hedefi: {targetNavMeshPosition} (Oyuncu: {currentPlayerPos})");
                }
                else if (agent.isOnNavMesh) // Sadece NavMesh üzerindeyken hata ver (diğer türlü zaten başarısız olması beklenir)
                {
                    Debug.LogError($"[SahurAIController CHASE_UPDATE] SetDestination ({targetNavMeshPosition}) BAŞARISIZ OLDU. PathStatus: {agent.pathStatus}", this);
                }
            }
        }
        
        if (agent.isStopped) // Eğer bir şekilde durdurulduysa harekete geçir
        {
            agent.isStopped = false;
            Debug.Log("[SahurAIController CHASE_UPDATE] Agent.isStopped true idi, false yapıldı.", this);
        }

        // Oyuncuyu Yakalama Kontrolü
        if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            Debug.Log($"[SahurAIController] Oyuncu yakalandı! AgentPos: {transform.position}, PlayerPos: {playerTransform.position}, Hedef: {agent.destination}", this);
            animator.SetBool(isChasingAnimatorHash, false);
            // agent.isStopped = true; // SwitchToState Idle bunu yapacak
            SwitchToState(AIState.Idle); // ÖNEMLİ: Yakaladıktan sonra Idle durumuna geç (veya başka bir uygun duruma)
            return; // Durum değişti, bu update fonksiyonundan çık
        }

        // Takılma Tespiti (Stuck Detection)
        if (Time.frameCount % 15 == 0) { 
            Debug.Log($"[SahurAIController DIAGNOSTIC] Velocity: {agent.velocity.magnitude:F3}, HasPath: {agent.hasPath}, PathPending: {agent.pathPending}, PathStatus: {agent.pathStatus}, RemainingDist: {agent.remainingDistance:F2}, StoppingDist: {agent.stoppingDistance:F2}, IsStopped: {agent.isStopped}, StuckTimer: {stuckTimer:F2}, PathPendingTimer: {currentPathPendingTimer:F2}", this);
        }

        // PathPending Takılma Tespiti
        if (agent.pathPending)
        {
            currentPathPendingTimer += Time.deltaTime;
            if (currentPathPendingTimer >= MaxPathPendingDuration && agent.velocity.magnitude < 0.05f)
            {
                Debug.LogWarning($"[SahurAIController CHASE_PATH_PENDING_STUCK] Ajan {MaxPathPendingDuration} saniyedir PathPending'de ve hızı çok düşük! Takılma çözümü tetikleniyor.", this);
                stuckTimer = maxStuckTime; // Takılma çözümünü doğrudan tetikle
            }
        }
        else
        {
            currentPathPendingTimer = 0f; // Yol beklenmiyorsa sayacı sıfırla
        }

        // Normal Takılma Tespiti (Eğer PathPending takılması zaten tetiklemediyse)
        if (stuckTimer < maxStuckTime) // Eğer PathPending takılması zaten stuckTimer'ı max yapmadıysa
        {
            bool isPotentiallyStuck = agent.hasPath && agent.velocity.magnitude < 0.05f && agent.remainingDistance > agent.stoppingDistance && !agent.pathPending;
            bool isPathProblem = (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid) && agent.velocity.magnitude < 0.05f && !agent.pathPending;

            if (isPotentiallyStuck || isPathProblem)
            {
                stuckTimer += Time.deltaTime;
                if (Time.frameCount % 30 == 0) 
                {
                    Debug.LogWarning($"[SahurAIController CHASE_STUCK_SUSPECT] Hız: {agent.velocity.magnitude:F2}, KalanMesafe: {agent.remainingDistance:F2}, PathStatus: {agent.pathStatus}, Hedef: {agent.destination}, Yolu Var mı: {agent.hasPath}. Takılma sayacı: {stuckTimer:F2}", this);
                }
            }
            else
            {
                stuckTimer = 0f; 
            }
        }

        // Takılma Çözümü (Stuck Resolution)
        if (stuckTimer >= maxStuckTime)
        {
            Debug.LogError($"[SahurAIController CHASE_STUCK_DETECTED] Ajan {maxStuckTime} saniyedir takılı! Kademeli agresif çözüm deneniyor. PathStatus: {agent.pathStatus}", this);
            agent.ResetPath(); 

            bool unstuckAttempted = false;
            bool warpSuccess = false; 

            if (playerTransform != null && agent.isOnNavMesh) 
            {
                // --- ADIM 1: Oyuncunun yakınına (biraz arkasına) ışınlanmayı dene ---
                Vector3 dirToPlayerFromAgent = (playerTransform.position - transform.position).normalized;
                Vector3 targetWarpPosNearPlayer = playerTransform.position - dirToPlayerFromAgent * (playerCatchDistance + 0.5f); // Yakalama mesafesinden biraz daha geriye
                NavMeshHit hitNearPlayer;
                if (NavMesh.SamplePosition(targetWarpPosNearPlayer, out hitNearPlayer, 1.5f, NavMesh.AllAreas))
                {
                    if (agent.Warp(hitNearPlayer.position))
                    {
                        Debug.Log($"[SahurAIController STUCK_RECOVERY_LVL1] Ajan oyuncunun yakınına ({hitNearPlayer.position}) başarıyla warp edildi.", this);
                        warpSuccess = true;
                    }
                    else
                    {Debug.LogError("[SahurAIController STUCK_RECOVERY_LVL1] Oyuncunun yakınına warp BAŞARISIZ.", this);}
                    unstuckAttempted = true;
                }

                // --- ADIM 2: Çok yönlü kaçış ışınlanması (Eğer Adım 1 başarısızsa) ---
                if (!warpSuccess) 
                {
                    Debug.LogWarning("[SahurAIController STUCK_RECOVERY_LVL1] Başarısız. LVL2 (çok yönlü kaçış) deneniyor.", this);
                    Vector3 escapePosition = transform.position; 
                    bool foundGenericEscapePos = false;
                    Vector3 backwardDir = -dirToPlayerFromAgent;
                    Vector3 sidewayDir = Vector3.Cross(Vector3.up, dirToPlayerFromAgent).normalized;
                    Vector3[] escapeDirs = new Vector3[] {
                        transform.position + backwardDir * 2.0f, 
                        transform.position + sidewayDir * 2.0f,  
                        transform.position - sidewayDir * 2.0f, 
                        playerTransform.position - dirToPlayerFromAgent * (playerCatchDistance + 2.0f) // Oyuncudan daha da geriye
                    };
                    NavMeshHit hitGenericEscape;
                    foreach (Vector3 potentialPos in escapeDirs)
                    {
                        if (NavMesh.SamplePosition(potentialPos, out hitGenericEscape, 2.0f, NavMesh.AllAreas))
                        {
                            escapePosition = hitGenericEscape.position;
                            foundGenericEscapePos = true;
                            break;
                        }
                    }
                    if (foundGenericEscapePos)
                    {
                        if (agent.Warp(escapePosition))
                        {
                            Debug.Log($"[SahurAIController STUCK_RECOVERY_LVL2] Ajan genel kaçış pozisyonuna ({escapePosition}) warp edildi.", this);
                            warpSuccess = true;
                        }
                        else {Debug.LogError("[SahurAIController STUCK_RECOVERY_LVL2] Genel kaçış pozisyonuna warp BAŞARISIZ.", this);}
                    }
                    else { Debug.LogWarning("[SahurAIController STUCK_RECOVERY_LVL2] Genel kaçış pozisyonu bulunamadı.", this); }
                    unstuckAttempted = true;
                }

                // --- ADIM 3: Son Çare - Doğrudan Translate ve Warp (Eğer Adım 1 & 2 başarısızsa) ---
                if (!warpSuccess)
                {
                    Debug.LogWarning("[SahurAIController STUCK_RECOVERY_LVL2] Başarısız. LVL3 (Translate + Warp) deneniyor.", this);
                    Vector3 moveDir = dirToPlayerFromAgent * 0.1f; // Çok küçük bir hareket
                    transform.Translate(moveDir, Space.World);
                    Debug.Log($"[SahurAIController STUCK_RECOVERY_LVL3] Ajan {moveDir.magnitude} birim translate edildi: {transform.position}", this);
                    NavMeshHit snapHit;
                    if (agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out snapHit, 1.0f, NavMesh.AllAreas))
                    {
                        if (agent.Warp(snapHit.position))
                        { Debug.Log($"[SahurAIController STUCK_RECOVERY_LVL3] Translate sonrası NavMesh'e ({snapHit.position}) warp BAŞARILI.", this); warpSuccess = true;}
                        else { Debug.LogError("[SahurAIController STUCK_RECOVERY_LVL3] Translate sonrası NavMesh'e warp BAŞARISIZ.", this);}
                    }
                    else if (!agent.isOnNavMesh)
                    {
                         Debug.LogError("[SahurAIController STUCK_RECOVERY_LVL3] Translate sonrası ajan NavMesh DIŞINDA KALDI! Son pozisyon: {transform.position}", this);
                         // Burada ajanı yeniden etkinleştirmek veya başka bir acil durum planı gerekebilir.
                    }
                    else { Debug.LogWarning("[SahurAIController STUCK_RECOVERY_LVL3] Translate sonrası warp için geçerli NavMesh noktası bulunamadı.", this);}
                    unstuckAttempted = true;
                }
            }
            
            if (!unstuckAttempted)
            {
                Debug.LogWarning("[SahurAIController STUCK_RECOVERY] Oyuncu veya NavMesh durumu nedeniyle hiçbir takılma çözme yöntemi denenemedi.", this);
            }

            agent.isStopped = false; 
            chasePathRecalculateTimer = 0f; 
            lastPlayerPositionForPathRecalculation = playerTransform != null ? playerTransform.position + (Vector3.one * 100f) : Vector3.one * 100f; 
            stuckTimer = 0f;   
        }
    }

    // --- Event Handler ---
    private void HandleBabyBurned()
    {
        Debug.Log("[SahurAIController] Bebek Sahur yakıldı bilgisi alındı! Oyuncu kovalanacak.", this);
        SwitchToState(AIState.ChasingPlayer);
    }

    // --- Hareket Mantığı ---
    private void TrySetNewRandomDestination()
    {
        if (availablePatrolPoints == null || availablePatrolPoints.Count == 0)
        {
            Debug.LogWarning("[SahurAIController] Hiç devriye noktası bulunmuyor. Idle durumuna geçiriliyor.", this);
            SwitchToState(AIState.Idle);
            return;
        }

        Transform selectedPoint = null;
        bool destinationFound = false;

        System.Collections.Generic.List<Transform> candidatePoints = new System.Collections.Generic.List<Transform>(availablePatrolPoints);
        ShuffleList(candidatePoints); // Aday noktaları karıştır

        for (int i = 0; i < candidatePoints.Count; i++)
        {
            Transform testPoint = candidatePoints[i];
            Vector3 potentialDestPosition = testPoint.position;

            bool isRecent = false;
            foreach (Vector3 recentPos in recentlyVisitedPositions)
            {
                if (Vector3.Distance(potentialDestPosition, recentPos) < minDistanceToRecentTarget)
                {
                    isRecent = true;
                    break;
                }
            }

            if (!isRecent)
            {
                NavMeshPath path = new NavMeshPath();
                Vector3 sampledTargetPosition = potentialDestPosition; // Varsayılan olarak orijinal pozisyon

                NavMeshHit hit;
                // Devriye noktasının 1.0f birim yakınına kadar NavMesh'te bir nokta ara (veya agent radius kadar)
                if (NavMesh.SamplePosition(potentialDestPosition, out hit, agent.radius * 2f > 0.5f ? agent.radius * 2f : 0.5f, NavMesh.AllAreas))
                {
                    sampledTargetPosition = hit.position; // Bulunan en yakın NavMesh pozisyonunu kullan
                }
                else
                {
                    Debug.LogWarning($"[SahurAIController] {testPoint.name} ({potentialDestPosition}) için NavMesh.SamplePosition BAŞARISIZ. Orijinal pozisyon ile devam edilecek.", this);
                    // SamplePosition başarısız olursa, bu nokta muhtemelen NavMesh'e çok uzak.
                    // Bu durumda CalculatePath'in de başarısız olması beklenir, yine de devam edip loglayalım.
                }

                // --- YENİ EKLENEN KONTROL ---
                if (!agent.isOnNavMesh)
                {
                    Debug.LogError($"[SahurAIController DIAGNOSTIC] Ajan, CalculatePath çağrılmadan önce NavMesh üzerinde DEĞİL! Pozisyon: {agent.transform.position}", this);
                }
                // --- KONTROL SONU ---

                if (agent.CalculatePath(sampledTargetPosition, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                     selectedPoint = testPoint; // Orijinal Transform'u sakla
                     // Hedef olarak SamplePosition'dan gelen noktayı kullanacağız.
                     // Bu yüzden SetDestination'da sampledTargetPosition'ı kullanmak üzere bir işaret bırakalım.
                     // Şimdilik selectedPoint'i bulduk demek yeterli.
                     destinationFound = true;
                     break;
                }
                else
                {
                    Debug.LogWarning($"[SahurAIController] Seçilen devriye noktası {testPoint.name} (Orijinal: {potentialDestPosition}, Örneklenmiş: {sampledTargetPosition}) için geçerli bir NavMesh yolu bulunamadı. Durum: {path.status}", this);
                }
            }
        }

        if (destinationFound && selectedPoint != null)
        {
            // Yeniden SamplePosition yapmak yerine, yukarıdaki döngüde bulunan 'sampledTargetPosition'ı kullanmalıyız.
            // Bunun için 'sampledTargetPosition'ı döngü dışına taşımamız veya
            // döngüden çıkarken doğru değeri saklamamız gerekir.
            // Geçici çözüm olarak, eğer selectedPoint bulunduysa, onun pozisyonunu tekrar sample edelim.
            Vector3 finalTargetPosition = selectedPoint.position;
            NavMeshHit finalHit;
            if (NavMesh.SamplePosition(selectedPoint.position, out finalHit, agent.radius * 2f > 0.5f ? agent.radius * 2f : 0.5f, NavMesh.AllAreas))
            {
                finalTargetPosition = finalHit.position;
            }
            // Eğer yukarıdaki SamplePosition başarısız olursa, finalTargetPosition orijinal selectedPoint.position kalır.

            agent.SetDestination(finalTargetPosition);
            Debug.Log($"[SahurAIController] Yeni devriye hedefi: {selectedPoint.name} (Orijinal: {selectedPoint.position}, Ayarlanan Hedef: {finalTargetPosition})", this);

            // Yeni hedef pozisyonunu listeye ekle
            if (recentlyVisitedPositions.Count >= maxRecentDestinationsToRemember)
            {
                recentlyVisitedPositions.RemoveAt(0);
            }
            recentlyVisitedPositions.Add(selectedPoint.position);
            currentPatrolTimer = minPatrolDuration; // Yeni hedef için zamanlayıcıyı sıfırla
        }
        else
        {
            Debug.LogWarning($"[SahurAIController] Tekrarlamayan veya ulaşılabilir uygun bir devriye noktası bulunamadı ({candidatePoints.Count} aday denendi). Mevcut hedefe devam ediliyor veya Idle durumuna geçiliyor.", this);
            // Eğer hiçbir hedef bulunamazsa ve zaten bir hedefi yoksa Idle'a geç
            if (!agent.hasPath || agent.remainingDistance < agent.stoppingDistance)
            {
                SwitchToState(AIState.Idle);
            }
        }
    }

    private void ShuffleList<T>(System.Collections.Generic.List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // --- Editör Yardımları (Gizmos) ---
    private void OnDrawGizmosSelected()
    {
        if (patrolPointsParent != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform point in patrolPointsParent)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.3f);
                }
            }
        }

        if (Application.isPlaying && agent != null && agent.hasPath)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, agent.destination);
            Gizmos.DrawWireSphere(agent.destination, 0.4f);

            Gizmos.color = Color.cyan;
            for (int i = 0; i < agent.path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(agent.path.corners[i], agent.path.corners[i + 1]);
            }
        }
    }
} 