using UnityEngine;
using UnityEngine.AI;
using System.Collections; // Eğer Coroutine kullanmak isterseniz diye ekledim, şimdilik gerek yok.

/// <summary>
/// Sahur karakterinin yapay zeka davranışlarını yönetir.
/// Rastgele devriye atar ve belirli aralıklarla boşta durur.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))] // Gerekli bileşenleri otomatik eklemez ama eksikse uyarır
public class SahurAIController : MonoBehaviour
{
    // --- Inspector Ayarları ---
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2.5f;
    [Tooltip("Sahur'un mevcut konumundan ne kadar uzağa devriye noktası arayacağını belirler.")]
    [SerializeField] private float patrolRadius = 15f;
    [Tooltip("Sahur'un hedefe ne kadar yaklaşınca duracağını belirler. NavMeshAgent'ın stoppingDistance'ı ile senkronize olmalı.")]
    [SerializeField] private float agentStoppingDistance = 0.5f;

    [Header("State Durations")]
    [Tooltip("Sahur'un 'Idle' animasyonunda kalacağı süre (saniye).")]
    [SerializeField] private float idleDuration = 3.0f;

    [Header("Animation Settings")]
    [Tooltip("Animator'daki yürüme durumunu kontrol eden boolean parametresinin adı.")]
    [SerializeField] private string isWalkingParameterName = "IsWalking";

    // --- Özel Değişkenler ---
    private NavMeshAgent agent;
    private Animator animator;

    private enum AIState
    {
        Initializing,
        Idle,
        Patrolling
    }
    private AIState currentState;

    private float currentIdleTimer;
    private int isWalkingAnimatorHash; // Performans için parametre adını hash'e çevireceğiz

    // --- Unity Mesajları ---

    private void Awake()
    {
        // Gerekli bileşenleri al ve kontrol et
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        bool initializationError = false;

        if (agent == null)
        {
            Debug.LogError("[SahurAIController] NavMeshAgent bileşeni bu GameObject üzerinde bulunamadı! Lütfen ekleyin.", this);
            initializationError = true;
        }

        if (animator == null)
        {
            Debug.LogError("[SahurAIController] Animator bileşeni bu GameObject üzerinde bulunamadı! Lütfen ekleyin.", this);
            initializationError = true;
        }
        else
        {
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError("[SahurAIController] Animator bileşenine bir Animator Controller atanmamış! Lütfen 'Controller' alanına atama yapın.", this);
                initializationError = true;
            }
            // Animator parametresinin hash'ini al (performans için)
            isWalkingAnimatorHash = Animator.StringToHash(isWalkingParameterName);
        }

        if (initializationError)
        {
            Debug.LogError("[SahurAIController] Başlatma sırasında kritik hatalar bulundu. Betik düzgün çalışmayacak. Lütfen yukarıdaki hataları düzeltin.", this);
            enabled = false; // Betiği devre dışı bırak
            currentState = AIState.Initializing; // Hatalı durumu belirt
            return;
        }

        Debug.Log("[SahurAIController] Awake tamamlandı. Bileşenler başarıyla alındı.", this);
    }

    private void Start()
    {
        if (!enabled) return; // Awake'de hata olduysa Start'ı çalıştırma

        agent.speed = walkSpeed;
        agent.stoppingDistance = agentStoppingDistance; // NavMeshAgent'ın durma mesafesini ayarla

        Debug.Log("[SahurAIController] Start: Sahur AI başlatılıyor. Varsayılan durum: Idle.", this);
        SwitchToState(AIState.Idle);
    }

    private void Update()
    {
        if (!enabled) return; // Betik devre dışıysa Update'i çalıştırma

        // Mevcut duruma göre güncelleme mantığını çalıştır
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdleState();
                break;
            case AIState.Patrolling:
                UpdatePatrollingState();
                break;
            case AIState.Initializing:
                // Hata durumunda bir şey yapma
                break;
        }
    }

    // --- Durum Yönetimi ---

    private void SwitchToState(AIState newState)
    {
        if (currentState == newState && currentState != AIState.Initializing) // Zaten bu durumdaysa veya başlangıç hatası varsa tekrar girme
        {
            // Debug.LogWarning($"[SahurAIController] Zaten {newState} durumunda.", this);
            return;
        }

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
        }
    }

    // --- Idle Durumu ---
    private void EnterIdleState()
    {
        Debug.Log("[SahurAIController] Idle durumuna girildi.", this);
        if (agent.isOnNavMesh) // NavMesh üzerinde değilse hata verebilir
        {
            agent.isStopped = true; // Hareketi durdur
            agent.ResetPath();      // Mevcut yolu temizle
        }
        animator.SetBool(isWalkingAnimatorHash, false); // Yürüme animasyonunu kapat
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
        agent.isStopped = false; // Harekete izin ver
        animator.SetBool(isWalkingAnimatorHash, true); // Yürüme animasyonunu aç
        TrySetNewRandomDestination();
    }

    private void UpdatePatrollingState()
    {
        // Hedefe ulaşıp ulaşmadığını kontrol et
        // agent.remainingDistance hedefe kalan mesafeyi verir.
        // agent.stoppingDistance hedefe ne kadar yaklaşınca duracağını belirtir.
        // !agent.pathPending yol hesaplamasının bitip bitmediğini kontrol eder.
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // Eğer NavMeshAgent'ın bir yolu yoksa veya hızı çok düşükse (yani durmuşsa)
            // Bazen hedefe tam ulaşmadan hızı sıfırlanabiliyor, bu yüzden velocity kontrolü önemli.
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f) // Hızın karesi küçük bir eşikten düşükse durmuş kabul et
            {
                Debug.Log("[SahurAIController] Hedefe ulaşıldı veya hareket durdu. Idle durumuna geçiliyor.", this);
                SwitchToState(AIState.Idle);
            }
        }
    }

    // --- Hareket Mantığı ---
    private void TrySetNewRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position; // Mevcut pozisyona ekle

        NavMeshHit hit;
        // NavMesh üzerinde rastgele bir nokta bulmaya çalış
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log($"[SahurAIController] Yeni devriye hedefi belirlendi: {hit.position}", this);
        }
        else
        {
            Debug.LogWarning("[SahurAIController] Geçerli bir NavMesh devriye noktası bulunamadı. Tekrar Idle durumuna geçiliyor.", this);
            // Eğer geçerli bir nokta bulunamazsa, tekrar Idle durumuna geçip bir süre sonra yeni bir nokta aramayı deneyebilir.
            SwitchToState(AIState.Idle); // Veya belki bir süre sonra tekrar denemesi için farklı bir mantık eklenebilir
        }
    }


    // --- Editör Yardımları (Gizmos) ---
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) // Sadece editörde çalışırken ve seçiliyken göster
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, patrolRadius);
        }
        else if(agent != null) // Oyun çalışıyorsa ve agent varsa
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, patrolRadius);

            if (agent.hasPath)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, agent.destination);
                Gizmos.DrawWireSphere(agent.destination, 0.5f);

                // Gizmos.color = Color.cyan;
                // NavMeshPath path = agent.path;
                // Vector3 previousCorner = transform.position;
                // foreach (var corner in path.corners)
                // {
                //    Gizmos.DrawLine(previousCorner, corner);
                //    Gizmos.DrawSphere(corner, 0.1f);
                //    previousCorner = corner;
                // }
            }
        }
    }
} 