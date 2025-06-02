using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using EscapeTheBrainRot;
using EscapeTheBrainRot; 

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

    [Header("Physical Attack Settings")]
    [Tooltip("Fiziksel saldırı sekansının gerçekleşme olasılığı (0 ile 1 arasında. 0: Asla, 1: Her zaman).")]
    [SerializeField, Range(0f, 1f)] private float attackSequenceProbability = 0.4f; 
    [Tooltip("Sahur'un kendi Animator'ündeki saldırı animasyonunu başlatan TRIGGER parametresinin adı.")]
    [SerializeField] private string attackAnimationTriggerName = "Attack"; // Trigger adı
    [Tooltip("Sahur'un oyuncunun ne kadar önünde belireceği (metre).")]
    [SerializeField] private float positionInFrontOffset = 1.2f;
    [Tooltip("Saldırı animasyonu başladıktan ne kadar sonra ekran flaşının/oyuncu düşmesinin tetikleneceği (saniye).")]
    [SerializeField] private float attackHitDelay = 0.3f;
    [Tooltip("Ekran kırmızı flaşı için kullanılacak UI Image.")]
    [SerializeField] private UnityEngine.UI.Image screenFlashImage;
    [Tooltip("Ekran flaşının rengi (Alfa değeri de önemlidir).")]
    [SerializeField] private Color screenFlashColor = new Color(0.8f, 0f, 0f, 0.6f); // Kırmızı, biraz transparan
    [Tooltip("Ekran flaşının belirginleşme süresi (saniye).")]
    [SerializeField] private float screenFlashInDuration = 0.05f;
    [Tooltip("Ekran flaşının kaybolma süresi (saniye).")]
    [SerializeField] private float screenFlashOutDuration = 0.25f;
    [Tooltip("Fiziksel saldırı sonrası Sahur'un Idle'a geçmeden önce bekleme süresi (saniye).")]
    [SerializeField] private float postAttackFreezeDuration = 1.5f;

    [Header("Player Animation Durations")] 
    [Tooltip("Oyuncunun düşme animasyonunun toplam süresi (saniye). Göz kapanma efekti bu süre bittikten sonra başlayacak.")]
    [SerializeField] private float playerFallAnimationDuration = 1.5f; 

    [Header("UI Jumpscare Flash Settings")] 
    [Tooltip("UI Jumpscare sırasında ekran flaşının tetiklenmesinden önceki gecikme (saniye).")]
    [SerializeField] private float uiJumpscareFlashDelay = 0.1f;

    [Header("Player & UI Control for Attack")]
    [Tooltip("Oyuncunun hareketlerini kontrol eden betik (PlayerMovement vb.). Bu betikte hareketi durduracak/başlatacak bir public metot olmalı.")]
    [SerializeField] private MonoBehaviour playerMovementScript; 
    [Tooltip("Fiziksel saldırı sırasında gizlenecek ana oyun arayüzü Canvas'ı.")]
    [SerializeField] private Canvas mainGameCanvas;

    [Header("Vision Settings For Chase")] // YENİ BAŞLIK
    [Tooltip("Sahur'un oyuncuyu görebileceği maksimum açı (derece). Görüş konisinin toplam açısıdır.")]
    [SerializeField, Range(1f, 360f)] private float visionAngle = 90f;
    [Tooltip("Sahur'un oyuncuyu görebileceği maksimum mesafe.")]
    [SerializeField] private float visionDistance = 15f;
    [Tooltip("Sahur'un görüşünü engelleyebilecek katmanlar (örn: Duvarlar, Kapılar). Player katmanı BU LİSTEDE OLMAMALI.")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Görerek başlatılan bir kovalamada, oyuncu bu mesafeden uzağa giderse Sahur kovalamayı bırakır.")]
    [SerializeField] private float loseSightChaseDistance = 25f;
    [Tooltip("Sahur, görerek başlattığı bir kovalamayı kaybettikten sonra ne kadar süre (saniye) boyunca tekrar görerek kovalamaya başlamaz.")]
    [SerializeField] private float chaseAfterLosingSightCooldown = 10f;

    [Header("Mud Chase Settings")] // YENİ BAŞLIK: Çamur Kovalaması Ayarları
    [Tooltip("Oyuncu çamura girdiğinde Sahur'un kullanacağı kovalama hızı.")]
    [SerializeField] private float mudChaseSpeed = 3.5f;
    [Tooltip("Çamur kovalamasında, oyuncu bu mesafeden uzağa giderse Sahur kovalamayı bırakır.")]
    [SerializeField] private float mudChaseLoseDistance = 30f;
    [Tooltip("Sahur, çamur kovalamasını kaybettikten sonra ne kadar süre (saniye) boyunca tekrar çamurla tetiklenen bir kovalamaya başlamaz.")]
    [SerializeField] private float mudChaseCooldown = 15f;

    [Header("New Catch Sequence Settings")]
    [Tooltip("Sahur'un child objesi olan ve oyuncunun saldırı sırasında çekileceği nokta.")]
    [SerializeField] private Transform catchPoint;
    [Tooltip("Oyuncunun Animator bileşeni (düşme animasyonu için).")]
    [SerializeField] private Animator playerAnimator;
    [Tooltip("Oyuncunun Animator'ündeki düşme animasyonunu başlatan TRIGGER parametresinin adı.")]
    [SerializeField] private string playerFallAnimationTriggerName = "Fall";
    [Tooltip("Kan efekti için kullanılacak UI Image. (Eski screenFlashImage buraya atanabilir)")]
    [SerializeField] private UnityEngine.UI.Image bloodEffectImage;
    [Tooltip("Kan efektinin rengi (Alfa değeri de önemlidir).")]
    [SerializeField] private Color bloodEffectColor = new Color(0.8f, 0f, 0f, 0.6f);
    [Tooltip("Kan efektinin belirginleşme süresi (saniye).")]
    [SerializeField] private float bloodEffectInDuration = 0.05f;
    [Tooltip("Kan efektinin ekranda kalma süresi (fade in sonrası).")]
    [SerializeField] private float bloodEffectHoldDuration = 0.15f;
    [Tooltip("Kan efektinin kaybolma süresi (saniye).")]
    [SerializeField] private float bloodEffectOutDuration = 0.25f;
    [Tooltip("Oyuncu düştükten ve kan efekti bittikten sonra gözlerin kapanmaya başlamasından önceki bekleme süresi (saniye).")]
    [SerializeField] private float postFallDelayBeforeEyeClose = 0.5f;
    [Tooltip("Oyuncunun yeniden doğacağı nokta.")]
    [SerializeField] private Transform playerRespawnPoint;
    [Tooltip("Oyuncunun Animator'ündeki Idle animasyonunu başlatan TRIGGER parametresinin adı.")]
    [SerializeField] private string playerIdleAnimationTriggerName = "Idle";

    [Header("Eye Closing Effect Settings")]
    // [Tooltip("Göz kapanma efekti için üst göz kapağı UI Image.")] // KALDIRILDI
    // [SerializeField] private Image topEyelidImage; // KALDIRILDI
    // [Tooltip("Göz kapanma efekti için alt göz kapağı UI Image.")] // KALDIRILDI
    // [SerializeField] private Image bottomEyelidImage; // KALDIRILDI
    [Tooltip("Göz kapakları kapandıktan sonra veya tek başına ekranı karartmak için kullanılacak ana siyah ekran.")] // Tooltip güncellendi
    [SerializeField] private Image finalBlackScreenImage; 
    [Tooltip("Gözlerin kapanma animasyonunun süresi (saniye).")]
    [SerializeField] private float eyeCloseAnimDuration = 1.0f;
    [Tooltip("Göz kapakları ve son siyah ekran için hedef alfa değeri.")]
    [SerializeField, Range(0f, 1f)] private float eyeCloseTargetAlpha = 1.0f;
    [Tooltip("Siyah ekranın tekrar kaybolma (fade out) animasyon süresi (saniye).")] // YENİ
    [SerializeField] private float blackScreenFadeOutDuration = 0.75f; // YENİ

    // --- Özel Değişkenler ---
    private NavMeshAgent agent;
    private Animator animator;

    private enum AIState
    {
        Initializing,
        Idle,
        Patrolling,
        OpeningDoor,
        ChasingPlayer,
        ActionInProgress
    }
    private AIState currentState;

    private float currentIdleTimer;
    private float currentPatrolTimer;
    private int isWalkingAnimatorHash;
    private int isChasingAnimatorHash;

    private System.Collections.Generic.List<Transform> availablePatrolPoints;
    private System.Collections.Generic.List<Vector3> recentlyVisitedPositions; 

    private Door currentTargetDoor = null;
    private float doorWaitTimer;
    private float stuckTimer = 0f; 

    private float chasePathRecalculateTimer; 
    private const float ChasePathRecalculateInterval = 0.3f; 
    private Vector3 lastPlayerPositionForPathRecalculation; 
    private const float PlayerMoveThresholdForPathRecalcSqr = 0.25f; 

    private float currentPathPendingTimer = 0f;
    private const float MaxPathPendingDuration = 1.0f; 

    private bool shouldForceLookAtSahur = false;
    private Vector3 forcedLookAtPoint_SahurPosition;
    private Transform playerToForceLook;

    // YENİ Değişkenler (Saklanma için)
    private bool isPlayerHiding = false;
    private Transform currentHidingSpotLocation = null; // Dolabın pozisyonu (genellikle dolabın önü)
    private bool sahurKnowsPlayerIsHiding = false; // Sahur oyuncunun saklandığını biliyor mu ve dolaba mı gidiyor?
    private float timeToWaitAtHidingSpot = 2.0f; // Dolapta bekleme süresi
    private float hidingSpotWaitTimer = 0f;
    private bool hasReachedHidingSpotDoor = false; // Sahur dolabın kapısına ulaştı mı?

    // YENİ Değişkenler (Görerek Kovalama için)
    private bool isChasingDueToVision = false;
    private float currentChaseCooldownTimer = 0f;

    // YENİ Değişkenler (Çamur Kovalaması için)
    private bool isChasingDueToMud = false;
    private float currentMudChaseCooldownTimer = 0f;

    // --- Jumpscare Settings başlığı ve ilgili alanlar (ses hariç) KORUNUYOR
    [Header("Jumpscare Sound Settings")] // Bu başlık ve altındaki ses değişkenleri KORUNUYOR
    [Tooltip("Oyuncu yakalandığında çalınacak ses klibi.")]
    [SerializeField] private AudioClip jumpscareAudioClip;
    [Tooltip("Jumpscare sesini çalmak için kullanılacak AudioSource. Genellikle Sahur objesinin üzerinde veya bir alt objesinde bulunur.")]
    [SerializeField] private AudioSource jumpscareAudioSource;
    [Tooltip("Jumpscare sesi için uygulanacak ek ses çarpanı (1 varsayılan, >1 daha yüksek ses).")]
    [SerializeField, Range(0.1f, 5f)] private float jumpscareVolumeMultiplier = 1.0f;


    // --- Unity Mesajları ---

    private void OnEnable()
    {
        BurnMechanismController.OnBabyTungBurned += HandleBabyBurned;
        SpindHidingSpot.OnPlayerHidingSpotStateChanged += HandlePlayerHidingSpotStateChanged; // YENİ
        // YENİ: PlayerMudController olaylarına abone ol (Bu betiğin var olduğunu varsayıyoruz)
        PlayerMudController.OnPlayerEnteredMud += HandlePlayerEnteredMud;
        PlayerMudController.OnPlayerExitedMud += HandlePlayerExitedMud;
    }

    private void OnDisable()
    {
        BurnMechanismController.OnBabyTungBurned -= HandleBabyBurned;
        SpindHidingSpot.OnPlayerHidingSpotStateChanged -= HandlePlayerHidingSpotStateChanged; // YENİ
        // YENİ: PlayerMudController olaylarından aboneliği kaldır
        PlayerMudController.OnPlayerEnteredMud -= HandlePlayerEnteredMud;
        PlayerMudController.OnPlayerExitedMud -= HandlePlayerExitedMud;
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

        agent.updateRotation = true; 
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
            }
        }

        if (availablePatrolPoints.Count == 0 && currentState != AIState.ChasingPlayer) 
        {
            Debug.LogError("[SahurAIController] Başlangıçta hiç devriye noktası bulunamadı! Lütfen 'PatrolPointsParent' altına devriye noktaları ekleyin ve atamayı kontrol edin. Betik devre dışı bırakılıyor.", this);
            enabled = false;
            currentState = AIState.Initializing; 
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
                if (child != null && child != transform) 
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
            case AIState.ActionInProgress:
                // ActionInProgress durumunda özel bir Update mantığı genellikle olmaz,
                // çünkü Coroutine'ler bu durumu yönetir.
                // Ancak, eğer dolapta bekleme gibi bir durum buraya dahil edilecekse eklenebilir.
                if (sahurKnowsPlayerIsHiding && hasReachedHidingSpotDoor)
                {
                    UpdateHidingSpotWaitTimer();
                }
                break;
        }

        // Kovalamayı kaybettikten sonraki cooldown'u işle
        if (currentChaseCooldownTimer > 0f)
        {
            currentChaseCooldownTimer -= Time.deltaTime;
        }

        // YENİ: Çamur kovalaması cooldown'unu işle
        if (currentMudChaseCooldownTimer > 0f)
        {
            currentMudChaseCooldownTimer -= Time.deltaTime;
        }
    }

    void LateUpdate()
    {
        if (shouldForceLookAtSahur && playerToForceLook != null)
        {
            // Sahur'un GÜNCEL pozisyonunu her LateUpdate'te al
            forcedLookAtPoint_SahurPosition = this.transform.position; 

            Vector3 lookPos = forcedLookAtPoint_SahurPosition;
            lookPos.y = playerToForceLook.position.y; // Oyuncunun Y seviyesinde bak
            playerToForceLook.LookAt(lookPos);
        }
    }

    // --- Durum Yönetimi ---

    private void SwitchToState(AIState newState)
    {
        if (currentState == newState && currentState != AIState.Initializing) return;

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
            case AIState.ActionInProgress: 
                Debug.Log("[SahurAIController] ActionInProgress durumuna girildi.", this);
                break;
        }
    }

    // --- Idle Durumu ---
    private void EnterIdleState()
    {
        Debug.Log("[SahurAIController] Idle durumuna girildi.", this);
        shouldForceLookAtSahur = false; // Her ihtimale karşı Idle'a girerken bakış zorlamasını kapat
        playerToForceLook = null;
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        animator.SetBool(isWalkingAnimatorHash, false);
        animator.SetBool(isChasingAnimatorHash, false); 
        currentIdleTimer = idleDuration;
    }

    private void UpdateIdleState()
    {
        currentIdleTimer -= Time.deltaTime;
        if (currentIdleTimer <= 0f)
        {
            SwitchToState(AIState.Patrolling);
        }

        // Görerek kovalama kontrolü
        if (!isPlayerHiding && currentChaseCooldownTimer <= 0f && CanSeePlayer())
        {
            Debug.Log("[SahurAIController] Oyuncu Idle durumundayken GÖRÜLDÜ! Kovalamaya geçiliyor.", this);
            isChasingDueToVision = true;
            isChasingDueToMud = false; // Diğer kovalama türünü sıfırla
            SwitchToState(AIState.ChasingPlayer);
        }
    }

    // --- Patrolling Durumu ---
    private void EnterPatrollingState()
    {
        Debug.Log("[SahurAIController] Patrolling durumuna girildi.", this);
        agent.speed = walkSpeed; 
        agent.angularSpeed = agentAngularSpeed;
        agent.acceleration = agentAcceleration;
        agent.stoppingDistance = agentStoppingDistance; 
        agent.isStopped = false; 
        animator.SetBool(isChasingAnimatorHash, false); 
        animator.SetBool(isWalkingAnimatorHash, true); 
        currentPatrolTimer = minPatrolDuration; 
        TrySetNewRandomDestination();
        Debug.Log($"[SahurAIController PATROL_ENTER] Agent Speed: {agent.speed}, IsStopped: {agent.isStopped}");
    }

    private void UpdatePatrollingState()
    {
        currentPatrolTimer -= Time.deltaTime;

        // Görerek kovalama kontrolü
        if (!isPlayerHiding && currentChaseCooldownTimer <= 0f && CanSeePlayer())
        {
            Debug.Log("[SahurAIController] Oyuncu devriye durumundayken GÖRÜLDÜ! Kovalamaya geçiliyor.", this);
            isChasingDueToVision = true;
            isChasingDueToMud = false; // Diğer kovalama türünü sıfırla
            SwitchToState(AIState.ChasingPlayer);
            return; // Kovalamaya geçtiği için diğer devriye mantığını çalıştırma
        }

        if (agent.hasPath && agent.remainingDistance > agentStoppingDistance) 
        {
            RaycastHit hit;
            Vector3 directionToTarget = (agent.steeringTarget - transform.position).normalized;
            float distanceToSteeringTarget = Vector3.Distance(transform.position, agent.steeringTarget);
            float checkDistance = Mathf.Min(doorInteractionDistance, distanceToSteeringTarget); 

            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToTarget, out hit, checkDistance, doorLayer))
            {
                Door door = hit.collider.GetComponent<Door>();
                if (door != null && !door.isOpen)
                {
                    Debug.Log($"[SahurAIController] Yol üzerinde kapalı kapı ({door.gameObject.name}) tespit edildi. Açılacak.", this);
                    currentTargetDoor = door;
                    SwitchToState(AIState.OpeningDoor);
                    return; 
                }
            }
        }

        bool hasReachedDestination = !agent.pathPending &&
                                     agent.remainingDistance <= agent.stoppingDistance &&
                                     (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f);

        if (hasReachedDestination)
        {
            Debug.Log("[SahurAIController] Hedefe ulaşıldı.", this);
            if (currentPatrolTimer <= 0f)
            {
                Debug.Log("[SahurAIController] Minimum devriye süresi doldu. Idle durumuna geçiliyor.", this);
                SwitchToState(AIState.Idle);
            }
            else
            {
                Debug.Log("[SahurAIController] Minimum devriye süresi henüz dolmadı ama hedefe ulaşıldı. Yeni hedef aranıyor.", this);
                TrySetNewRandomDestination();
            }
        }
    }

    // --- OpeningDoor Durumu --- 
    private void EnterOpeningDoorState()
    {
        Debug.Log("[SahurAIController] OpeningDoor durumuna girildi.", this);
        agent.isStopped = true; 
        animator.SetBool(isWalkingAnimatorHash, false); 

        if (currentTargetDoor != null && !currentTargetDoor.isOpen)
        {
            Debug.Log($"[SahurAIController] {currentTargetDoor.gameObject.name} kapısı açılıyor.", this);
            currentTargetDoor.OpenDoor();
            doorWaitTimer = doorOpenWaitTime;
        }
        else
        {
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
            currentTargetDoor = null; 
            SwitchToState(AIState.Patrolling); 
        }
    }

    // --- ChasingPlayer Durumu --- 
    private void EnterChasingPlayerState()
    {
        Debug.Log("[SahurAIController] ChasingPlayer durumuna girildi!", this);
        if (playerTransform == null && !(isPlayerHiding && sahurKnowsPlayerIsHiding)) // Oyuncu saklanmıyorsa ve hedefi dolap değilse, oyuncu referansı GEREKLİ
        {
            Debug.LogError("[SahurAIController] Oyuncu referansı yok ve hedef dolap değil! ChasingPlayer durumu düzgün çalışamaz. Idle durumuna geçiliyor.", this);
            SwitchToState(AIState.Idle);
            return;
        }

        // Eğer bu state'e `isChasingDueToVision` true olmadan girildiyse (örn: bebek yakma),
        // o zaman `isChasingDueToVision`'ı false yap (HandleBabyBurned'de zaten yapılıyor ama emin olmak için).
        // Ancak, eğer `CanSeePlayer` ile girildiyse, `isChasingDueToVision` zaten true olmalı.
        // Bu satırı kaldırdım çünkü `isChasingDueToVision` durumu başlatan yerde ayarlanıyor.
        // if (!isChasingDueToVision) isChasingDueToVision = false; 

        agent.speed = chaseSpeed; // Varsayılan kovalama hızı
        if (isChasingDueToMud)
        {
            agent.speed = mudChaseSpeed; // Çamur kovalaması ise hızı ayarla
            Debug.Log($"[SahurAIController CHASE_ENTER] Çamur Kovalaması! Hız: {agent.speed}");
        }
        else if (isChasingDueToVision)
        {
            Debug.Log($"[SahurAIController CHASE_ENTER] Görerek Kovalama! Hız: {agent.speed}");
        }
        else
        {
            // Bebek yakma veya diğer durumlar için varsayılan chaseSpeed kullanılır.
            Debug.Log($"[SahurAIController CHASE_ENTER] Genel Kovalama (Bebek vb.)! Hız: {agent.speed}");
        }
        
        agent.angularSpeed = agentAngularSpeed; 
        agent.acceleration = agentAcceleration; 
        agent.isStopped = false; 
        animator.SetBool(isWalkingAnimatorHash, false); 
        animator.SetBool(isChasingAnimatorHash, true);  
        agent.stoppingDistance = playerCatchDistance; 
        stuckTimer = 0f; 
        currentPathPendingTimer = 0f; 
        
        chasePathRecalculateTimer = 0f; 
        lastPlayerPositionForPathRecalculation = playerTransform.position + (Vector3.one * 100f); 

        Debug.Log($"[SahurAIController CHASE_ENTER] Agent Speed: {agent.speed}, StoppingDistance: {agent.stoppingDistance}", this);
    }

    private void UpdateChasingPlayerState()
    {
        // Debug.Log("[SahurAIController] UpdateChasingPlayerState ÇAĞRILDI!", this);

        if (isPlayerHiding && sahurKnowsPlayerIsHiding && currentHidingSpotLocation != null)
        {
            // Oyuncu saklanıyor ve Sahur bunu biliyor, dolaba doğru git
            agent.SetDestination(currentHidingSpotLocation.position);
            agent.stoppingDistance = agentStoppingDistance; // Dolabın yakınında durması için
            animator.SetBool(isChasingParameterName, true); // Dolaba giderken de koşabilir
            animator.SetBool(isWalkingAnimatorHash, false);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // Dolaba ulaşıldı
                Debug.Log("[SahurAIController] Oyuncunun saklandığı dolaba ulaşıldı.", this);
                hasReachedHidingSpotDoor = true;
                hidingSpotWaitTimer = timeToWaitAtHidingSpot;
                animator.SetBool(isChasingParameterName, false); 
                animator.SetBool(isWalkingAnimatorHash, false); 
                agent.isStopped = true;
                agent.ResetPath();
                SwitchToState(AIState.ActionInProgress); 
                return;
            }
        }
        else if (playerTransform == null)
        {
            Debug.LogWarning("[SahurAIController] ChasingPlayer: Oyuncu referansı kayboldu! Idle durumuna geçiliyor.", this);
            SwitchToState(AIState.Idle);
            return;
        }
        else // Normal kovalama mantığı (görerek veya bebek yakma ile)
        {
            // Görerek başlatılan kovalamada mesafeyi kontrol et
            if (isChasingDueToVision)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer > loseSightChaseDistance)
                {
                    Debug.Log($"[SahurAIController] Oyuncu ({playerTransform.name}) görerek kovalama için çok uzaklaştı ({distanceToPlayer:F1}m > {loseSightChaseDistance}m). Kovalamayı bırakıp devriyeye dönülüyor.", this);
                    isChasingDueToVision = false;
                    currentChaseCooldownTimer = chaseAfterLosingSightCooldown;
                    SwitchToState(AIState.Patrolling);
                    return;
                }
                // Hala görerek kovalıyorsa, oyuncu hedef olmalı
                agent.stoppingDistance = playerCatchDistance; 
            }
            // YENİ: Çamurla başlatılan kovalamada mesafeyi kontrol et
            else if (isChasingDueToMud)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer > mudChaseLoseDistance)
                {
                    Debug.Log($"[SahurAIController] Oyuncu ({playerTransform.name}) çamur kovalaması için çok uzaklaştı ({distanceToPlayer:F1}m > {mudChaseLoseDistance}m). Kovalamayı bırakıp devriyeye dönülüyor.", this);
                    isChasingDueToMud = false;
                    currentMudChaseCooldownTimer = mudChaseCooldown;
                    SwitchToState(AIState.Patrolling);
                    return;
                }
                // Çamur kovalamasında yakalama mesafesi normal playerCatchDistance olabilir.
                // Eğer farklı bir yakalama mesafesi isteniyorsa burası ayarlanabilir.
                agent.stoppingDistance = playerCatchDistance; 
            }
            // else -> Bebek yakma ile kovalama, mesafe kontrolü yok, sadece oyuncuyu yakalamaya çalışır.
            // (isChasingDueToVision false ise stoppingDistance zaten playerCatchDistance olmalı)

            // Kalan kovalama mantığı (hedef belirleme, takılma tespiti vs.) buraya gelecek
            // ... (Önceki mesajlardaki normal kovalama mantığı buraya kopyalanacak)
            // Bu kısım bir önceki mesajınızdaki UpdateChasingPlayerState içeriğinin "else" bloğundan alınacak.
            // KODUN DEVAMI (STUCK DETECTION VS.) BURADA YER ALACAK
            // Ajanın NavMesh üzerinde olup olmadığını ve geçerli bir yolu olup olmadığını kontrol et
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) 
            {
                Debug.LogError($"[SahurAIController CHASE_UPDATE] NavMeshAgent null, devre dışı veya NavMesh üzerinde değil! Agent Durumu: {(agent == null ? "Null" : agent.enabled.ToString())}, IsOnNavMesh: {(agent == null ? "N/A" : agent.isOnNavMesh.ToString())}", this);
                SwitchToState(AIState.Idle); 
                return;
            }

            // Ajanı NavMesh'e sabitleme (nadiren gerekebilir)
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

            // Hedef yeniden hesaplama mantığı
            chasePathRecalculateTimer -= Time.deltaTime;
            Vector3 currentPlayerPos = playerTransform.position;
            float playerMovementSinceLastRecalcSqr = (currentPlayerPos - lastPlayerPositionForPathRecalculation).sqrMagnitude;

            if (chasePathRecalculateTimer <= 0f || 
                playerMovementSinceLastRecalcSqr > PlayerMoveThresholdForPathRecalcSqr || 
                !agent.hasPath || 
                agent.pathStatus != NavMeshPathStatus.PathComplete) 
            {
                if (!agent.pathPending && agent.isOnNavMesh)
                {
                    Vector3 targetNavMeshPosition = currentPlayerPos;
                    NavMeshHit playerNavMeshHit;
                    float sampleRadius = 3.0f; 
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
                        lastPlayerPositionForPathRecalculation = currentPlayerPos; 
                        chasePathRecalculateTimer = ChasePathRecalculateInterval;
                         animator.SetBool(isChasingParameterName, true); // Hedef ayarlandığında koşma animasyonu
                         animator.SetBool(isWalkingAnimatorHash, false);
                    }
                    else if (agent.isOnNavMesh) 
                    {
                        Debug.LogError($"[SahurAIController CHASE_UPDATE] SetDestination ({targetNavMeshPosition}) BAŞARISIZ OLDU. PathStatus: {agent.pathStatus}", this);
                    }
                }
            }
            
            // Ajanın durdurulup durdurulmadığını kontrol et
            if (agent.isStopped) 
            {
                agent.isStopped = false;
            }

            // Oyuncuyu yakalama kontrolü
            if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
            {
                // Bu noktada isPlayerHiding kontrolü yukarıda (dolap mantığında) yapıldı, 
                // eğer buraya geldiyse ve isPlayerHiding değilse yakalanmıştır.
                // Ancak, görerek kovalama durumunda bu koşul farklı olabilir, 
                // çünkü oyuncu dolaba girmiş olabilir ve Sahur dolaba ulaşmış olabilir.
                // Bu yüzden dolap kontrolü daha öncelikli.
                
                // Eğer bu satıra kadar geldiysek ve oyuncu saklanmıyorsa, yakalanmıştır.
                if (!isPlayerHiding) 
                { 
                    Debug.Log($"[SahurAIController] Oyuncu yakalandı! AgentPos: {transform.position}, PlayerPos: {playerTransform.position}, Hedef: {agent.destination}", this);
                    animator.SetBool(isChasingParameterName, false); 
                    HandlePlayerCaught(); 
                    return; 
                }
            }

            // Takılma tespiti (Stuck detection)
            if (agent.pathPending)
            {
                currentPathPendingTimer += Time.deltaTime;
                if (currentPathPendingTimer >= MaxPathPendingDuration && agent.velocity.magnitude < 0.05f)
                {
                    Debug.LogWarning($"[SahurAIController CHASE_PATH_PENDING_STUCK] Ajan {MaxPathPendingDuration} saniyedir PathPending'de ve hızı çok düşük! Takılma çözümü tetikleniyor.", this);
                    stuckTimer = maxStuckTime; 
                }
            }
            else
            {
                currentPathPendingTimer = 0f; 
            }

            if (stuckTimer < maxStuckTime) 
            {
                bool isPotentiallyStuck = agent.hasPath && agent.velocity.magnitude < 0.05f && agent.remainingDistance > agent.stoppingDistance && !agent.pathPending;
                bool isPathProblem = (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid) && agent.velocity.magnitude < 0.05f && !agent.pathPending;

                if (isPotentiallyStuck || isPathProblem)
                {
                    stuckTimer += Time.deltaTime;
                }
                else
                {
                    stuckTimer = 0f; 
                }
            }

            if (stuckTimer >= maxStuckTime)
            {
                // ... (Mevcut takılma çözme kodunuz buraya gelecek) ...
                Debug.LogError($"[SahurAIController CHASE_STUCK_DETECTED] Ajan {maxStuckTime} saniyedir takılı! Kademeli agresif çözüm deneniyor. PathStatus: {agent.pathStatus}", this);
                agent.ResetPath(); 

                bool unstuckAttempted = false;
                bool warpSuccess = false; 

                if (playerTransform != null && agent.isOnNavMesh) 
                {
                    Vector3 dirToPlayerFromAgent = (playerTransform.position - transform.position).normalized;
                    Vector3 targetWarpPosNearPlayer = playerTransform.position - dirToPlayerFromAgent * (playerCatchDistance + 0.5f); 
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
                            playerTransform.position - dirToPlayerFromAgent * (playerCatchDistance + 2.0f) 
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

                    if (!warpSuccess)
                    {
                        Debug.LogWarning("[SahurAIController STUCK_RECOVERY_LVL2] Başarısız. LVL3 (Translate + Warp) deneniyor.", this);
                        Vector3 moveDir = dirToPlayerFromAgent * 0.1f; 
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
    }

    private void HandlePlayerCaught() // YAKALAMA ÖNCESİ KONTROL
    {
        if (isPlayerHiding) 
        {
            Debug.Log("[SahurAIController] Oyuncu saklandığı için yakalama sekansı İPTAL EDİLDİ.", this);
            // Eğer Sahur dolaba ulaştıysa ve oyuncu hala saklanıyorsa, devriyeye dönmesi gerekir.
            // Bu durum UpdateChasingPlayerState içinde yönetilecek.
            // Veya burada zorla Idle/Patrol'e geçirilebilir.
            if (currentState != AIState.Patrolling && currentState != AIState.Idle && hasReachedHidingSpotDoor) 
            { // Eğer dolaba ulaşıldıysa ve Sahur hala başka bir moddaysa
                 SwitchToState(AIState.Patrolling); 
            }
            return; // Oyuncu saklanıyorsa yakalama işlemini yapma
        }

        SwitchToState(AIState.ActionInProgress);

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        animator.SetBool(isWalkingAnimatorHash, false); 

        StartCoroutine(PerformCombinedAttackSequence());
    }

    private System.Collections.IEnumerator PerformCombinedAttackSequence()
    {
        Debug.Log("[SahurAIController] Yakalama Sekansı (Güncel Sahur Pozisyonu ile Bakış) Başlatılıyor.", this);

        // 1. ADIM: Oyuncu hareketini HEMEN durdur
        SetPlayerMovementActive(false);
        // 2. ADIM: UI'ı devre dışı bırak
        SetMainCanvasActive(false);

        // 3. ADIM: Jumpscare sesini çal (isteğe bağlı)
        if (jumpscareAudioClip != null && jumpscareAudioSource != null)
        {
            if (!jumpscareAudioSource.isPlaying)
            {
                jumpscareAudioSource.PlayOneShot(jumpscareAudioClip, jumpscareVolumeMultiplier);
                Debug.Log($"[SahurAIController] Yakalanma sesi çalındı: {jumpscareAudioClip.name} (Çarpan: {jumpscareVolumeMultiplier})", this);
            }
        }

        // 4. ADIM: Mevcut frame'in sonunu bekle (Tüm Update/LateUpdate'ler bitsin)
        yield return new WaitForEndOfFrame();
        Debug.Log("[SahurAIController] WaitForEndOfFrame tamamlandı.", this);

        // 5. ADIM: Oyuncuyu CatchPoint'e taşı, Sahur'a baktır ve bakış zorlamasını başlat
        if (playerTransform != null && catchPoint != null)
        {
            Vector3 targetPosition = catchPoint.position;
            targetPosition.y = playerTransform.position.y; 
            playerTransform.position = targetPosition;
            Debug.Log($"[SahurAIController] Oyuncu ({playerTransform.name}) CatchPoint'e ({targetPosition}) taşındı (EndOfFrame sonrası).", this);

            Vector3 initialLookAtSahurPosition = transform.position; 
            initialLookAtSahurPosition.y = playerTransform.position.y; 
            playerTransform.LookAt(initialLookAtSahurPosition);
            Debug.Log($"[SahurAIController] Oyuncu Sahur'a ({initialLookAtSahurPosition}) baktırıldı (EndOfFrame sonrası).", this);

            playerToForceLook = playerTransform;
            // forcedLookAtPoint_SahurPosition = transform.position; // Bu satır artık LateUpdate'te dinamik olarak ayarlanacak
            shouldForceLookAtSahur = true; 
        }
        else
        {
            Debug.LogError("[SahurAIController] PlayerTransform veya CatchPoint atanmamış! Yakalama sekansı düzgün çalışmayabilir.", this);
            SwitchToState(AIState.Idle); 
            yield break;
        }

        // 6. ADIM: Sahur'un saldırı animasyonunu oynat
        if (animator != null && !string.IsNullOrEmpty(attackAnimationTriggerName))
        {
             if (animator.runtimeAnimatorController != null) {
                bool triggerParamExists = false;
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.name == attackAnimationTriggerName && param.type == AnimatorControllerParameterType.Trigger)
                    {
                        triggerParamExists = true;
                        break;
                    }
                }
                if (triggerParamExists) {
                    animator.SetTrigger(attackAnimationTriggerName);
                    Debug.Log($"[SahurAIController] Sahur saldırı animasyonu ('{attackAnimationTriggerName}') tetiklendi.", this);
                } else {
                    Debug.LogError($"[SahurAIController] Sahur Animator Controller'da '{attackAnimationTriggerName}' adında bir Trigger parametresi bulunamadı!", animator);
                }
             } else {
                Debug.LogError("[SahurAIController] Sahur Animator bileşenine bir Animator Controller atanmamış!", animator);
             }
        }
        else { Debug.LogWarning("[SahurAIController] Sahur Animator veya attackAnimationTriggerName atanmamış/belirtilmemiş.", this); }

        // 7. ADIM: Oyuncunun düşme animasyonu ve kan efekti için bekle (Sahur'un saldırı animasyonu başladıktan sonra)
        Debug.Log($"[SahurAIController] Oyuncunun düşmesi için AttackHitDelay ({attackHitDelay}s) bekleniyor.", this);
        yield return new WaitForSeconds(attackHitDelay);

        // 8. ADIM: Kan efektini başlat
        if (bloodEffectImage != null)
        {
            StartCoroutine(PlayBloodEffectCoroutine());
        }
        else
        {
            Debug.LogWarning("[SahurAIController] BloodEffectImage atanmamış. Kan efekti gösterilemeyecek.", this);
        }

        // 9. ADIM: Oyuncunun düşme animasyonunu tetikle
        if (playerAnimator != null && !string.IsNullOrEmpty(playerFallAnimationTriggerName))
        {
            playerAnimator.enabled = true; 
            bool fallTriggerExists = false;
            foreach (AnimatorControllerParameter param in playerAnimator.parameters)
            {
                if (param.name == playerFallAnimationTriggerName && param.type == AnimatorControllerParameterType.Trigger)
                {
                    fallTriggerExists = true;
                    break;
                }
            }
            if(fallTriggerExists)
            {
                // Düşme animasyonu başlamadan hemen önce Sahur'a bakmayı bırak
                shouldForceLookAtSahur = false;
                playerToForceLook = null; 

                playerAnimator.SetTrigger(playerFallAnimationTriggerName);
                Debug.Log($"[SahurAIController] Oyuncu düşme animasyonu ('{playerFallAnimationTriggerName}') tetiklendi.", this);
                
            }
            else
            {
                Debug.LogError($"[SahurAIController] Player Animator Controller'da '{playerFallAnimationTriggerName}' adında bir Trigger parametresi bulunamadı!", playerAnimator);
                playerAnimator.enabled = false; 
                // shouldForceLookAtSahur = false; // KALDIRILDI
            }
        }
        else
        {
            Debug.LogWarning("[SahurAIController] PlayerAnimator veya playerFallAnimationTriggerName atanmamış/belirtilmemiş. Oyuncu düşme animasyonu oynatılamayacak.", this);
            // shouldForceLookAtSahur = false; // KALDIRILDI
        }
        
        // 10. ADIM: Oyuncunun düşme animasyonunun bitmesini bekle
        Debug.Log($"[SahurAIController] Oyuncunun düşme animasyonunun ({playerFallAnimationDuration}s) bitmesi bekleniyor.", this);
        yield return new WaitForSeconds(playerFallAnimationDuration);
        Debug.Log("[SahurAIController] Oyuncunun düşme animasyonu bitti.", this);

        // 11. ADIM: Gözlerin kapanmasından önceki ek bekleme (isteğe bağlı)
        if (postFallDelayBeforeEyeClose > 0)
        {
            Debug.Log($"[SahurAIController] Göz kapanmasından önce ek bekleme ({postFallDelayBeforeEyeClose}s) başlıyor.", this);
            yield return new WaitForSeconds(postFallDelayBeforeEyeClose);
        }

        // 12. ADIM: Göz kapanma efektini başlat
        Debug.Log("[SahurAIController] Göz kapanma efekti başlatılıyor.", this);
        StartCoroutine(AnimateEyeClosingCoroutine());

        // 13. ADIM: Göz kapanma animasyonunun bitmesini bekle
        yield return new WaitForSeconds(eyeCloseAnimDuration);
        Debug.Log("[SahurAIController] Göz kapanma efekti tamamlandı.", this);

        // Gözler tamamen kapandı, bakış zorlama referanslarını temizle.
        shouldForceLookAtSahur = false; 
        playerToForceLook = null; 

        // YENİ ADIM: Siyah ekran tamamen göründüğünde kalp UI'larını aktive et
        if (HeartManager.Instance != null)
        {
            HeartManager.Instance.ActivateAndDisplayHearts();
        }
        else
        {
            Debug.LogWarning("[SahurAIController] HeartManager.Instance bulunamadı. Kalp UI'ları aktive edilemedi.", this);
        }

        // YENİ ADIM: Siyah ekran için ek 1 saniye bekleniyor.
        Debug.Log("[SahurAIController] Siyah ekran için ek 1 saniye bekleniyor.", this);
        yield return new WaitForSeconds(1.0f);

        // YENİ ADIMLAR: Siyah ekran sonrası 0.5sn bekle, Idle animasyonu, ışınlanma, Animator'ü kapatma
        Debug.Log("[SahurAIController] Yeniden doğma sekansı için 0.5 saniye bekleniyor.", this);
        yield return new WaitForSeconds(0.5f);

        if (playerAnimator != null) // Önce null kontrolü
        {
            playerAnimator.enabled = true; // Idle animasyonunu tetiklemeden önce Animator'ü aktif et
            if (!string.IsNullOrEmpty(playerIdleAnimationTriggerName))
            {
                bool idleTriggerExists = false;
                foreach (AnimatorControllerParameter param in playerAnimator.parameters)
                {
                    if (param.name == playerIdleAnimationTriggerName && param.type == AnimatorControllerParameterType.Trigger)
                    {
                        idleTriggerExists = true;
                        break;
                    }
                }
                if (idleTriggerExists)
                {
                    playerAnimator.SetTrigger(playerIdleAnimationTriggerName);
                    Debug.Log($"[SahurAIController] Oyuncu Idle animasyonu ('{playerIdleAnimationTriggerName}') tetiklendi.", this);
                }
                else
                {
                    Debug.LogError($"[SahurAIController] Player Animator Controller'da '{playerIdleAnimationTriggerName}' adında bir Trigger parametresi bulunamadı!", playerAnimator);
                }
            }
            else
            {
                Debug.LogWarning("[SahurAIController] playerIdleAnimationTriggerName belirtilmemiş. Idle animasyonu tetiklenemiyor.", this);
            }
        }
        else
        {
            Debug.LogWarning("[SahurAIController] PlayerAnimator null veya devre dışı. Idle animasyonu tetiklenemiyor.", this);
        }

        if (playerTransform != null && playerRespawnPoint != null)
        {
            playerTransform.position = playerRespawnPoint.position;
            playerTransform.rotation = playerRespawnPoint.rotation; // Opsiyonel: Rotasyonu da ayarla
            Debug.Log($"[SahurAIController] Oyuncu, '{playerRespawnPoint.name}' noktasına ışınlandı ({playerRespawnPoint.position}).", this);
        }
        else
        {
            Debug.LogWarning("[SahurAIController] PlayerTransform veya playerRespawnPoint atanmamış. Oyuncu ışınlanamıyor.", this);
        }

        if (playerAnimator != null && playerAnimator.enabled)
        {
            // Animator'ü hemen devre dışı bırakmak yerine, Idle animasyonunun bir frame oynaması için
            // kısa bir bekleme veya DisableAnimatorAfterAnimation coroutine'i kullanılabilir.
            // Şimdilik direkt devre dışı bırakıyoruz.
            StartCoroutine(DisableAnimatorAfterAnimation(playerAnimator)); // Bu coroutine zaten vardı, onu kullanalım.
            Debug.Log("[SahurAIController] Oyuncu Animator'ü devre dışı bırakılacak (Coroutine ile).", this);
        }

        // 13.A ADIM: Oyuncunun canını azalt ve kalp animasyonunu oynat
        if (HeartManager.Instance != null)
        {
            Debug.Log("[SahurAIController] Kalp azaltma animasyonu başlatılıyor.", this);
            yield return StartCoroutine(HeartManager.Instance.TakeDamageAndAnimate(1));
            Debug.Log("[SahurAIController] Kalp azaltma animasyonu tamamlandı.", this);
        }
        else
        {
            Debug.LogWarning("[SahurAIController] HeartManager.Instance bulunamadı. Can azaltılamadı.", this);
        }

        // 13.B ADIM: Canvas açılmadan önce 0.5 saniye bekle
        Debug.Log("[SahurAIController] Ana canvas açılmadan önce 0.5sn bekleniyor.", this);
        yield return new WaitForSeconds(0.5f);

        // 14. ADIM: Oyuncu Animator'ünü devre dışı bırak (Bu adım Canvas aktif edilmeden önce veya sonra olabilir, şimdilik burada kalıyor)
        if (playerAnimator != null && playerAnimator.enabled) 
        {
            Debug.Log("[SahurAIController] Göz kapanma bitti, oyuncu Animator'ü devre dışı bırakılacak.", this);
            StartCoroutine(DisableAnimatorAfterAnimation(playerAnimator));
        }
        
        Debug.Log("[SahurAIController] Yakalama Sekansı Tamamlandı. Durum Idle'a çevriliyor.", this);
        
        SetPlayerMovementActive(true); 
        SetMainCanvasActive(true);     // Bu satır zaten vardı, ana canvas'ı tekrar aktif eder.

        // YENİ ADIM: Siyah ekran kapanmadan önce kalpleri deaktif et
        if (HeartManager.Instance != null)
        {
            HeartManager.Instance.DeactivateHearts();
        }
        else
        {
            Debug.LogWarning("[SahurAIController] HeartManager.Instance bulunamadı. Kalpler deaktive edilemedi.", this);
        }

        // 15. ADIM: Siyah ekranı animasyonlu olarak kaldır (Fade Out)
        if (finalBlackScreenImage != null && finalBlackScreenImage.gameObject.activeSelf) // Eğer zaten kapanmışsa tekrar fade out yapma
        {
            Debug.Log("[SahurAIController] Siyah ekran fade out animasyonu başlatılıyor.", this);
            yield return StartCoroutine(AnimateBlackScreenFadeOutCoroutine());
            Debug.Log("[SahurAIController] Siyah ekran fade out animasyonu tamamlandı.", this);
        }
        
        SwitchToState(AIState.Idle);
    }

    private void SetPlayerMovementActive(bool isActive)
    {
        if (playerMovementScript == null)
        {
            Debug.LogWarning("[SahurAIController] PlayerMovementScript atanmamış. Oyuncu hareketi kontrol edilemiyor.", this);
            return;
        }

        PlayerMove pm = playerMovementScript as PlayerMove;
        if (pm != null)
        {
            pm.SetMovementActive(isActive);
            Debug.Log("[SahurAIController] Oyuncu hareketi (PlayerMove.SetMovementActive ile) " + (isActive ? "aktif" : "deaktif") + " edildi.", this);
        }
        else
        {
            Debug.LogWarning("[SahurAIController] PlayerMovementScript, PlayerMove tipine doğrudan cast edilemedi. Script'in enabled durumu değiştiriliyor: " + playerMovementScript.GetType().Name, this);
            playerMovementScript.enabled = isActive;
        }
    }

    private void SetMainCanvasActive(bool isActive)
    {
        if (mainGameCanvas != null)
        {
            mainGameCanvas.gameObject.SetActive(isActive);
            Debug.Log("[SahurAIController] Ana oyun Canvas'ı " + (isActive ? "aktif" : "deaktif") + " edildi.", this);
        }
        else
        {
            Debug.LogWarning("[SahurAIController] MainGameCanvas atanmamış. Canvas aktivitesi kontrol edilemiyor.", this);
        }
    }

    private System.Collections.IEnumerator PlayScreenFlashCoroutine()
    {
        Image targetImage = screenFlashImage; 
        Color flashColor = screenFlashColor;
        float fadeInDuration = screenFlashInDuration;
        float holdDuration = 0.1f; 
        float fadeOutDuration = screenFlashOutDuration;

        if (bloodEffectImage != null) 
        {
            targetImage = bloodEffectImage;
            flashColor = bloodEffectColor;
            fadeInDuration = bloodEffectInDuration;
            holdDuration = bloodEffectHoldDuration; 
            fadeOutDuration = bloodEffectOutDuration;
        }
        else if (screenFlashImage == null)
        {
            Debug.LogError("[SahurAIController] Ne BloodEffectImage ne de ScreenFlashImage atanmamış! Efekt çalışmayacak.", this);
            yield break;
        }


        Debug.Log($"[SahurAIController] {(targetImage == bloodEffectImage ? "Kan Efekti" : "Ekran Flaş")} Animasyonu Başlıyor (Renk: {flashColor}, GirişSüresi: {fadeInDuration}).", this);
        targetImage.gameObject.SetActive(true);
        Color initialImageColor = targetImage.color;
        initialImageColor.a = 0f; 
        targetImage.color = initialImageColor; 

        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeInDuration);
            targetImage.color = Color.Lerp(initialImageColor, flashColor, progress);
            yield return null;
        }
        targetImage.color = flashColor;

        if (holdDuration > 0)
        {
            yield return new WaitForSeconds(holdDuration);
        }

        elapsedTime = 0f;
        Color transparentColor = flashColor;
        transparentColor.a = 0f; 

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            targetImage.color = Color.Lerp(flashColor, transparentColor, progress);
            yield return null;
        }

        targetImage.color = transparentColor;
        targetImage.gameObject.SetActive(false); 
        Debug.Log($"[SahurAIController] {(targetImage == bloodEffectImage ? "Kan Efekti" : "Ekran Flaş")} Animasyonu Tamamlandı.", this);
    }




    private void HandleBabyBurned()
    {
        Debug.Log("[SahurAIController] Bebek Sahur yakıldı bilgisi alındı! Oyuncu kovalanacak.", this);
        if (playerTransform == null)
        {
            Debug.LogWarning("[SahurAIController] HandleBabyBurned: PlayerTransform null, kovalamaya geçilemiyor. Oyuncu bulunmaya çalışılacak.");
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                Debug.Log("[SahurAIController] HandleBabyBurned: PlayerTransform 'Player' tag'i ile tekrar bulundu.", this);
            }
            else
            {
                Debug.LogError("[SahurAIController] HandleBabyBurned: PlayerTransform hala bulunamadı! Kovalama başlatılamıyor.", this);
                return; 
            }
        }
        isChasingDueToVision = false; // Bebek yakma ile başlayan kovalama, görerek değil
        sahurKnowsPlayerIsHiding = false; // Oyuncu saklanıyorsa bile bu kovalama onu geçersiz kılar (ya da dolaptan çıkarır gibi düşünebiliriz)
        SwitchToState(AIState.ChasingPlayer);
    }

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
        ShuffleList(candidatePoints); 

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
                Vector3 sampledTargetPosition = potentialDestPosition; 

                NavMeshHit hit;
                if (NavMesh.SamplePosition(potentialDestPosition, out hit, agent.radius * 2f > 0.5f ? agent.radius * 2f : 0.5f, NavMesh.AllAreas))
                {
                    sampledTargetPosition = hit.position; 
                }
                else
                {
                    Debug.LogWarning($"[SahurAIController] {testPoint.name} ({potentialDestPosition}) için NavMesh.SamplePosition BAŞARISIZ. Orijinal pozisyon ile devam edilecek.", this);
                }

                if (!agent.isOnNavMesh)
                {
                    Debug.LogError($"[SahurAIController DIAGNOSTIC] Ajan, CalculatePath çağrılmadan önce NavMesh üzerinde DEĞİL! Pozisyon: {agent.transform.position}", this);
                }

                if (agent.CalculatePath(sampledTargetPosition, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                     selectedPoint = testPoint; 
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
            Vector3 finalTargetPosition = selectedPoint.position;
            NavMeshHit finalHit;
            if (NavMesh.SamplePosition(selectedPoint.position, out finalHit, agent.radius * 2f > 0.5f ? agent.radius * 2f : 0.5f, NavMesh.AllAreas))
            {
                finalTargetPosition = finalHit.position;
            }

            agent.SetDestination(finalTargetPosition);
            Debug.Log($"[SahurAIController] Yeni devriye hedefi: {selectedPoint.name} (Orijinal: {selectedPoint.position}, Ayarlanan Hedef: {finalTargetPosition})", this);

            if (recentlyVisitedPositions.Count >= maxRecentDestinationsToRemember)
            {
                recentlyVisitedPositions.RemoveAt(0);
            }
            recentlyVisitedPositions.Add(selectedPoint.position);
            currentPatrolTimer = minPatrolDuration; 
        }
        else
        {
            Debug.LogWarning($"[SahurAIController] Tekrarlamayan veya ulaşılabilir uygun bir devriye noktası bulunamadı ({candidatePoints.Count} aday denendi). Mevcut hedefe devam ediliyor veya Idle durumuna geçiliyor.", this);
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

        if (catchPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(catchPoint.position, 0.25f);
            Gizmos.DrawLine(transform.position, catchPoint.position);
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

    private System.Collections.IEnumerator PlayBloodEffectCoroutine()
    {
        if (bloodEffectImage == null)
        {
            Debug.LogError("[SahurAIController] BloodEffectImage atanmamış! Kan efekti çalışmayacak.", this);
            yield break;
        }

        Debug.Log($"[SahurAIController] Kan Efekti Animasyonu Başlıyor (Renk: {bloodEffectColor}, GirişSüresi: {bloodEffectInDuration}).", this);
        bloodEffectImage.gameObject.SetActive(true);
        Color initialImageColor = bloodEffectImage.color; 
        initialImageColor.a = 0f; 
        bloodEffectImage.color = initialImageColor;

        float elapsedTime = 0f;
        while (elapsedTime < bloodEffectInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / bloodEffectInDuration);
            bloodEffectImage.color = Color.Lerp(initialImageColor, bloodEffectColor, progress);
            yield return null;
        }
        bloodEffectImage.color = bloodEffectColor;

        if (bloodEffectHoldDuration > 0)
        {
            yield return new WaitForSeconds(bloodEffectHoldDuration);
        }

        elapsedTime = 0f;
        Color transparentColor = bloodEffectColor; 
        transparentColor.a = 0f;

        while (elapsedTime < bloodEffectOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / bloodEffectOutDuration);
            bloodEffectImage.color = Color.Lerp(bloodEffectColor, transparentColor, progress);
            yield return null;
        }

        bloodEffectImage.color = transparentColor;
        bloodEffectImage.gameObject.SetActive(false);
        Debug.Log("[SahurAIController] Kan Efekti Animasyonu Tamamlandı.", this);
    }

    private System.Collections.IEnumerator AnimateEyeClosingCoroutine()
    {
        bool useFinalBlackScreen = finalBlackScreenImage != null;

        if (!useFinalBlackScreen)
        {
            Debug.LogWarning("[SahurAIController] Göz kapanma efekti için 'FinalBlackScreenImage' atanmamış.", this);
            yield break;
        }

        float elapsedTime = 0f;
        finalBlackScreenImage.gameObject.SetActive(true);
        Color finalColor = finalBlackScreenImage.color;
        finalColor.a = 0;
        finalBlackScreenImage.color = finalColor;
        
        Debug.Log($"[SahurAIController] Siyah ekran fade-in animasyonu başlıyor. Süre: {eyeCloseAnimDuration}s", this);

        while (elapsedTime < eyeCloseAnimDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / eyeCloseAnimDuration);

            Color c = finalBlackScreenImage.color;
            c.a = Mathf.Lerp(0, eyeCloseTargetAlpha, progress);
            finalBlackScreenImage.color = c;
            yield return null;
        }

        Color endColor = finalBlackScreenImage.color;
        endColor.a = eyeCloseTargetAlpha;
        finalBlackScreenImage.color = endColor;
        Debug.Log("[SahurAIController] Siyah ekran fade-in animasyonu tamamlandı.", this);
    }

    private System.Collections.IEnumerator AnimateBlackScreenFadeOutCoroutine() // YENİ COROUTINE
    {
        if (finalBlackScreenImage == null || !finalBlackScreenImage.gameObject.activeSelf)
        {
            Debug.LogWarning("[SahurAIController] FinalBlackScreenImage atanmamış veya aktif değil. Fade out yapılamıyor.", this);
            yield break;
        }

        float elapsedTime = 0f;
        Color startColor = finalBlackScreenImage.color; // Mevcut alfa değerinden başla (muhtemelen eyeCloseTargetAlpha)
        Color endColor = startColor;
        endColor.a = 0f; // Hedef alfa: tamamen transparan
        
        Debug.Log($"[SahurAIController] Siyah ekran fade-out animasyonu başlıyor. Süre: {blackScreenFadeOutDuration}s", this);

        while (elapsedTime < blackScreenFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / blackScreenFadeOutDuration);
            finalBlackScreenImage.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }

        finalBlackScreenImage.color = endColor;
        finalBlackScreenImage.gameObject.SetActive(false); // Tamamen kaybolunca objeyi de kapatabiliriz
        Debug.Log("[SahurAIController] Siyah ekran fade out animasyonu tamamlandı ve obje deaktif edildi.", this);
    }

    private System.Collections.IEnumerator DisableAnimatorAfterAnimation(Animator animatorToDisable)
    {
        if (animatorToDisable == null || !animatorToDisable.enabled)
        {
            yield break; 
        }

        yield return new WaitForSeconds(0.5f); // Trigger'ın ve Idle animasyonunun biraz oynaması için bekleme süresini artırdık

        if (animatorToDisable != null && animatorToDisable.enabled) 
        {
            animatorToDisable.enabled = false;
            Debug.Log("[SahurAIController] DisableAnimatorAfterAnimation: Player Animator devre dışı bırakıldı (0.5s sonra).", this);
        }
    }

    // YENİ METOT (Saklanma durumu için)
    private void HandlePlayerHidingSpotStateChanged(SpindHidingSpot spot, bool isHiding)
    {
        isPlayerHiding = isHiding;
        if (isHiding)
        {
            Debug.Log($"[SahurAIController] Oyuncu '{spot.gameObject.name}' dolabında saklandı.", this);
            currentHidingSpotLocation = spot.transform; // Dolabın transformunu al
            hasReachedHidingSpotDoor = false; 

            // Eğer Sahur oyuncuyu kovalıyorsa veya son gördüğü yer oyuncunun saklandığı yerin yakınındaysa,
            // Sahur'un bu durumu fark etmesini ve dolaba yönelmesini sağlayalım.
            if (currentState == AIState.ChasingPlayer || 
                (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) < 10f)) // Örnek bir farkındalık mesafesi
            {
                sahurKnowsPlayerIsHiding = true;
                agent.stoppingDistance = agentStoppingDistance; // Dolaba varınca durması için stopping distance'ı ayarla
                Debug.Log("[SahurAIController] Sahur, oyuncunun saklandığını fark etti ve dolaba yöneliyor.", this);
                // ChasingPlayer state'i zaten dolaba yönlendirecek.
                 if (currentState != AIState.ChasingPlayer) SwitchToState(AIState.ChasingPlayer);
            }
            else
            {
                sahurKnowsPlayerIsHiding = false; // Sahur uzaktaysa veya devriyedeyse fark etmesin
            }
        }
        else
        {
            Debug.Log($"[SahurAIController] Oyuncu '{spot.gameObject.name}' dolabından çıktı.", this);
            currentHidingSpotLocation = null;
            sahurKnowsPlayerIsHiding = false;
            hasReachedHidingSpotDoor = false;
            agent.stoppingDistance = playerCatchDistance; // Normal yakalama mesafesine dön

            // Eğer Sahur dolaba doğru gidiyorduysa (veya dolapta bekliyorduysa) ve oyuncu çıktıysa,
            // oyuncuyu tekrar normal şekilde kovalamaya başlasın.
            if (currentState == AIState.ChasingPlayer || currentState == AIState.ActionInProgress)
            {
                if (playerTransform != null) // Oyuncu referansı hala geçerliyse
                {
                    Debug.Log("[SahurAIController] Oyuncu dolaptan çıktı. Normal kovalama devam ediyor.", this);
                    SwitchToState(AIState.ChasingPlayer); 
                }
                else
                {
                    SwitchToState(AIState.Idle); // Oyuncu bir şekilde kaybolduysa Idle'a geç
                }
            }
        }
    }

    private void UpdateHidingSpotWaitTimer() // YENİ METOT (Dolapta bekleme için)
    {
        if (hidingSpotWaitTimer > 0)
        {
            hidingSpotWaitTimer -= Time.deltaTime;
            if (hidingSpotWaitTimer <= 0)
            {
                Debug.Log("[SahurAIController] Dolapta bekleme süresi doldu. Devriyeye dönülüyor.", this);
                sahurKnowsPlayerIsHiding = false; // Artık oyuncunun saklandığını bilmiyor (yeni bir bilgi gelene kadar)
                currentHidingSpotLocation = null;
                hasReachedHidingSpotDoor = false;
                agent.stoppingDistance = playerCatchDistance; // Normal yakalama mesafesine dön
                SwitchToState(AIState.Patrolling);
            }
        }
    }

    // YENİ METOT (Oyuncuyu Görüş Kontrolü)
    private bool CanSeePlayer()
    {
        if (playerTransform == null || isPlayerHiding) // Oyuncu yoksa veya saklanıyorsa göremez
        {
            return false;
        }

        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > visionDistance)
        {
            return false; // Oyuncu görüş mesafesinin dışında
        }

        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        // visionAngle toplam açı olduğu için yarısıyla karşılaştırıyoruz
        if (angleToPlayer > visionAngle / 2f) 
        {
            return false; // Oyuncu görüş açısının dışında
        }

        // Sahur'un göz hizasından (biraz yukarıdan) ışın gönderelim
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // Sahur'un boyuna göre ayarlanabilir
        Vector3 playerTargetPosition = playerTransform.position + Vector3.up * 0.5f; // Oyuncunun vücudunun ortasına doğru
        Vector3 directionToPlayerTarget = playerTargetPosition - rayOrigin;

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, directionToPlayerTarget.normalized, out hit, distanceToPlayer, obstacleLayer))
        {
            // Arada bir engel var
            // Debug.Log($"[SahurAIController] CanSeePlayer: Oyuncuya giden ışın bir engele çarptı: {hit.collider.name}");
            return false; 
        }
        
        // Debug.Log("[SahurAIController] CanSeePlayer: Oyuncu GÖRÜLDÜ!");
        return true; // Oyuncu görüş alanında ve arada engel yok
    }

    // YENİ METOTLAR (PlayerMudController Olayları İçin)
    // Bu metotların çağrılabilmesi için PlayerMudController adında bir betik ve 
    // OnPlayerEnteredMud, OnPlayerExitedMud static event'lerinin olması gerekir.
    // Şimdilik bu metotları yorum satırı olarak ekliyorum. PlayerMudController.cs oluşturulduğunda aktif edilebilirler.
    
    private void HandlePlayerEnteredMud()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[SahurAIController] HandlePlayerEnteredMud: PlayerTransform null. Oyuncu bulunmaya çalışılıyor.", this);
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) playerTransform = playerObject.transform;
            else
            {
                Debug.LogError("[SahurAIController] HandlePlayerEnteredMud: PlayerTransform bulunamadı. Çamur kovalaması başlatılamıyor.", this);
                return;
            }
        }

        if (currentState == AIState.ActionInProgress || isPlayerHiding)
        {
            Debug.Log("[SahurAIController] HandlePlayerEnteredMud: Sahur meşgul veya oyuncu saklanıyor. Çamur kovalaması başlatılmayacak.", this);
            return;
        }

        if (currentMudChaseCooldownTimer > 0f)
        {
            Debug.Log($"[SahurAIController] HandlePlayerEnteredMud: Çamur kovalaması cooldown aktif ({currentMudChaseCooldownTimer:F1}s). Kovalama başlatılmayacak.", this);
            return;
        }
        
        Debug.Log("[SahurAIController] Oyuncu çamura girdi! Kovalamaya geçiliyor.", this);
        isChasingDueToMud = true;
        isChasingDueToVision = false; // Diğer kovalama türlerini sıfırla
        sahurKnowsPlayerIsHiding = false; // Oyuncu saklanıyorsa bile çamur bunu geçersiz kılar
        SwitchToState(AIState.ChasingPlayer);
    }

    private void HandlePlayerExitedMud()
    {
        // Oyuncu çamurdan çıktığında özel bir davranış gerekmiyorsa bu metot boş kalabilir
        // veya Sahur'un davranışını değiştirmek için kullanılabilir.
        // Örneğin, eğer Sahur çamur yüzünden kovalıyorsa ve oyuncu çamurdan çıktıysa,
        // belki bir süre daha kovalamaya devam eder veya hemen normale döner.
        // Şimdilik, çamurdan çıkış direkt bir aksiyonu tetiklemiyor,
        // kovalama mesafesi UpdateChasingPlayerState'te kontrol ediliyor.
        Debug.Log("[SahurAIController] Oyuncu çamurdan çıktı bilgisi alındı.", this);
    }
}