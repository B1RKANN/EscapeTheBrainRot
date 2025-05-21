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


    // --- Özel Değişkenler ---
    private NavMeshAgent agent;
    private Animator animator;

    private enum AIState
    {
        Initializing,
        Idle,
        Patrolling,
        OpeningDoor
    }
    private AIState currentState;

    private float currentIdleTimer;
    private float currentPatrolTimer;
    private int isWalkingAnimatorHash;

    private System.Collections.Generic.List<Transform> availablePatrolPoints;
    private System.Collections.Generic.List<Vector3> recentlyVisitedPositions; // Artık pozisyonları saklayacağız

    private Door currentTargetDoor = null;
    private float doorWaitTimer;

    // --- Unity Mesajları ---

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

        InitializePatrolPoints();

        if (availablePatrolPoints.Count == 0)
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
        agent.isStopped = false;
        animator.SetBool(isWalkingAnimatorHash, true);
        currentPatrolTimer = minPatrolDuration; // Her yeni devriye hedefi için zamanlayıcıyı sıfırla
        TrySetNewRandomDestination();
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
                // NavMesh üzerinde geçerli bir nokta olup olmadığını kontrol et
                NavMeshHit hit;
                if (agent.CalculatePath(potentialDestPosition, agent.path) && agent.path.status == NavMeshPathStatus.PathComplete)
                {
                    // NavMesh.SamplePosition ile de teyit edilebilir ama CalculatePath daha kesin sonuç verir.
                     selectedPoint = testPoint;
                     destinationFound = true;
                     break; 
                }
                else
                {
                    Debug.LogWarning($"[SahurAIController] Seçilen devriye noktası {testPoint.name} ({potentialDestPosition}) için geçerli bir NavMesh yolu bulunamadı. Başka bir nokta deneniyor.", this);
                }
            }
        }


        if (destinationFound && selectedPoint != null)
        {
            agent.SetDestination(selectedPoint.position);
            Debug.Log($"[SahurAIController] Yeni devriye hedefi: {selectedPoint.name} ({selectedPoint.position})", this);

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