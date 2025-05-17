using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.AI;

public class BabySahurController : MonoBehaviour
{
    [Header("Algılama Ayarları")]
    [SerializeField] private float detectionDistance = 1.5f;
    [SerializeField] private float viewAngle = 35f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private bool useObstacleDetection = true;
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float checkInterval = 0.05f;

    [Header("Referanslar")]
    [SerializeField] private GameObject takeButtonObject;
    [SerializeField] private GameObject playerInventoryBabySahur;
    [SerializeField] private Transform babySahurKacisParent;
    // [SerializeField] private Animator animator; // YORUM SATIRI YAPILDI / KALDIRILDI

    [Header("Kaçış Ayarları")] // Animasyon ayarları kaldırıldı
    [SerializeField] private float kacisHizi = 5f;
    // private string standUpAnimTrigger = "StandUp"; // YORUM SATIRI YAPILDI / KALDIRILDI
    // private string lieDownAnimTrigger = "LieDown"; // YORUM SATIRI YAPILDI / KALDIRILDI

    private Transform playerCamera;
    private bool isPlayerLooking = false;
    private TakeButtonController takeButtonController;
    private static BabySahurController lastLookedAtBabySahur;

    private float sqrDetectionDistance;
    private float cosViewAngle;
    private Coroutine checkRoutine;
    private bool dahaOnceKacmayiDeneydi = false;
    private static int yakilmisBabySahurSayisi = 0;
    private Collider _collider;
    private NavMeshAgent navMeshAgent;

    private const float YATMA_ROTATION_X = -90f;
    private const float AYAKTA_ROTATION_X = 0f;
    
    private int frameCountForDebug = 0; // Update logları için sayaç

    // [Header("Kapı Etkileşim Ayarları")] // BU HEADER VE ALANLAR ARTIK KULLANILMAYACAK - Linter hatasını gidermek için bu satır da yorumlandı
    //SerializeField] private float kapiAlgilamaOnundekiMesafe = 1.2f; 
    //[SerializeField] private float kapiAlgilamaYaricapi = 0.4f; 
    //[SerializeField] private LayerMask kapiLayerMask;         
    //[SerializeField] private string kapiTag = "InteractableDoor"; 
    // private string lieDownAnimTrigger = "LieDown"; // YORUM SATIRI YAPILDI / KALDIRILDI

    // private bool isPathBlockedByDoor = false; // BU ALAN ARTIK KULLANILMAYACAK

    private void Awake()
    {
        if (takeButtonObject != null)
        {
            takeButtonController = takeButtonObject.GetComponent<TakeButtonController>();
            if (takeButtonController == null)
            {
                Debug.LogError("Take butonunda TakeButtonController script'i bulunamadı!");
            }
        }

        sqrDetectionDistance = detectionDistance * detectionDistance;
        cosViewAngle = Mathf.Cos(viewAngle * Mathf.Deg2Rad);

        // Animator component'ını alma kısmı kaldırıldı
        // if (animator == null) { animator = GetComponent<Animator>(); }
        // if (animator == null && debugMode) { Debug.LogWarning("BabySahur objesinde Animator component'ı bulunamadı!", this); }

        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            Debug.LogError("BabySahur objesinde bir Collider bulunamadı!", this);
        }

        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogError("BabySahur objesinde NavMeshAgent component'ı bulunamadı! Lütfen ekleyin.", this);
        }
        else
        {
            navMeshAgent.enabled = false; // NavMeshAgent'ı başlangıçta devre dışı bırak
            if(debugMode) Debug.Log($"[NAV_CTRL] {gameObject.name} ({GetInstanceID()}) NavMeshAgent Awake içinde devre dışı bırakıldı.", this);
        }

        BurnMechanismController.OnBabyTungBurned += HandleBabySahurBurned;
    }

    private void Start()
    {
        playerCamera = Camera.main.transform;
        if (playerCamera == null) Debug.LogError("Ana kamera bulunamadı!");

        if (takeButtonObject != null) takeButtonObject.SetActive(false);
        if (playerInventoryBabySahur != null) playerInventoryBabySahur.SetActive(false);

        Quaternion initialRotation = Quaternion.Euler(YATMA_ROTATION_X, transform.eulerAngles.y, transform.eulerAngles.z);
        transform.rotation = initialRotation;
        if(debugMode) Debug.Log($"[ROT_CTRL] {gameObject.name} ({GetInstanceID()}) Start İÇİ, AYARLANDIKTAN HEMEN SONRA rotasyon: {transform.eulerAngles}, Quaternion: {transform.rotation}", this);

        // NavMeshAgent'ı gecikmeli aktif etmek için Coroutine başlat
        // Sadece gameObject aktifse ve script enable ise başlat
        // KALDIRILDI: Kullanıcının isteği üzerine NavMeshAgent sadece kaçışta aktif olacak.
        // if (gameObject.activeInHierarchy && this.enabled)
        // {
        //     StartCoroutine(EnableNavMeshAgentDelayed());
        // }

        StartCheckRoutine();
        if(debugMode) Debug.Log($"[ROT_CTRL] {gameObject.name} ({GetInstanceID()}) Start SONU rotasyon: {transform.eulerAngles}, Quaternion: {transform.rotation}", this);
    }
    
    private void Update()
    {
        if (debugMode && frameCountForDebug < 10) // İlk 10 frame için logla
        {
            Debug.Log($"[ROT_CTRL_UPDATE] Frame: {Time.frameCount}, {gameObject.name} ({GetInstanceID()}) Update rotasyon: {transform.eulerAngles}", this);
            frameCountForDebug++;
        }
    }

    private void OnEnable()
    {
        // StartCheckRoutine zaten Start içinde çağrılıyor ve kendi aktiflik kontrolü var.
        // Eğer OnEnable'da spesifik bir şey yapılacaksa, o da aktiflik kontrolü yapmalı.
        // EnableNavMeshAgentDelayed coroutine'i kaldırıldığı için buradaki mantık da kaldırıldı.
        BurnMechanismController.OnBabyTungBurned -= HandleBabySahurBurned; 
        BurnMechanismController.OnBabyTungBurned += HandleBabySahurBurned;
    }
    
    private void OnDisable()
    {
        StopCheckRoutine();
        BurnMechanismController.OnBabyTungBurned -= HandleBabySahurBurned;
    }
    
    private void StartCheckRoutine()
    {
        StopCheckRoutine();
        if (gameObject.activeInHierarchy && this.enabled)
        {
            checkRoutine = StartCoroutine(LookCheckRoutine());
        }
    }
    
    private void StopCheckRoutine()
    {
        if (checkRoutine != null)
        {
            StopCoroutine(checkRoutine);
            checkRoutine = null;
        }
    }
    
    private IEnumerator LookCheckRoutine()
    {
        // Güvenlik: playerCamera null ise bekle/tekrar dene veya logla
        while (playerCamera == null)
        {
            if (debugMode) Debug.LogError($"[LOOK_CTRL] {gameObject.name} ({GetInstanceID()}) LookCheckRoutine içinde PlayerCamera hala null! Bir sonraki frame'de tekrar denenecek.", this);
            yield return null; // Bir sonraki frame'de tekrar kontrol et
            playerCamera = Camera.main.transform; // Yeniden bulmayı dene
        }

        while (true)
        {
            CheckPlayerLookingAtBabySahur();
            yield return new WaitForSeconds(checkInterval);
        }
    }
    
    private void CheckPlayerLookingAtBabySahur()
    {
        if (playerCamera == null) 
        {
            // Bu durum yukarıdaki while döngüsü ile azaltılmış olmalı ama yine de bir güvenlik kontrolü
            if (debugMode && Time.frameCount % 120 == 0) Debug.LogError("[LOOK_CTRL] CheckPlayerLookingAtBabySahur içinde PlayerCamera null!", this);
            return;
        }

        if (takeButtonObject != null && takeButtonController == null)
        {
            takeButtonController = takeButtonObject.GetComponent<TakeButtonController>();
            if (takeButtonController == null && debugMode) 
                Debug.LogError($"[LOOK_CTRL] {gameObject.name} ({GetInstanceID()}): TakeButtonObject üzerinde TakeButtonController component'ı bulunamadı!", this);
        }
        bool canManageButton = takeButtonObject != null && takeButtonController != null;

        Vector3 playerPos = playerCamera.position;
        Vector3 sahurPos = transform.position;
        float sqrDistance = (playerPos - sahurPos).sqrMagnitude;
        
        bool meetsCriteriaThisFrame = false;
        
        if (sqrDistance <= sqrDetectionDistance)
        {
            Vector3 eyePosition = sahurPos + Vector3.up * 0.4f; 
            Vector3 directionToSahurNormalized = (eyePosition - playerPos).normalized;
            float dotProduct = Vector3.Dot(playerCamera.forward, directionToSahurNormalized);
            bool isInAngle = dotProduct >= cosViewAngle;
            
            bool hasLineOfSight = true;
            if (useObstacleDetection && isInAngle)
            {
                if(Physics.Raycast(playerPos, directionToSahurNormalized, out RaycastHit hit, Mathf.Sqrt(sqrDistance), obstacleLayer))
                {
                    hasLineOfSight = false;
                    if (debugMode && Time.frameCount % 60 == 0) Debug.Log($"[LOOK_CTRL] {gameObject.name} ({GetInstanceID()}): Engel tespit edildi: {hit.collider.name}", this);
                }
            }
            meetsCriteriaThisFrame = isInAngle && hasLineOfSight;
        }

        if (meetsCriteriaThisFrame)
            {
            isPlayerLooking = true; 
            lastLookedAtBabySahur = this; 

            if (canManageButton)
                {
                bool inventoryHasBabySahur = playerInventoryBabySahur != null && playerInventoryBabySahur.activeSelf;
                if (inventoryHasBabySahur)
                {
                    if (takeButtonObject.activeSelf) 
                        {
                            takeButtonObject.SetActive(false);
                        if (debugMode) Debug.Log($"[BTN_CTRL] {gameObject.name} ({GetInstanceID()}): Odak BUNDA, ama envanter DOLU. Buton KAPATILDI.", this);
                        }
                    }
                else 
                    {
                    if (!takeButtonObject.activeSelf) 
                        {
                            takeButtonObject.SetActive(true);
                        if (debugMode) Debug.Log($"[BTN_CTRL] {gameObject.name} ({GetInstanceID()}): Odak BUNDA, envanter BOŞ. Buton AÇILDI.", this);
                    }
                    if (takeButtonController != null) takeButtonController.SetCurrentBabySahur(this); 
                }
            }
            else if (debugMode && Time.frameCount % 120 == 0) Debug.LogWarning($"[LOOK_CTRL] {gameObject.name} ({GetInstanceID()}): Buton yönetilemiyor (takeButtonObject veya controller null).", this);
                            }
        else 
        {
            isPlayerLooking = false; 
            
            if (lastLookedAtBabySahur == this)
            {
                if (canManageButton && takeButtonObject != null && takeButtonObject.activeSelf)
                {
                    takeButtonObject.SetActive(false);
                    if (debugMode) Debug.Log($"[BTN_CTRL] {gameObject.name} ({GetInstanceID()}): Odak BUNDAYDI, ama artık bakılmıyor. Buton KAPATILDI.", this);
                }
                lastLookedAtBabySahur = null; 
                    }
                }
        
        if (debugMode && Time.frameCount % 75 == 0) 
        {
            string canLookStatus = meetsCriteriaThisFrame ? "BAKIYOR" : "BAKMIYOR";
            string invStatus = (playerInventoryBabySahur != null && playerInventoryBabySahur.activeSelf) ? "ENVANTER_DOLU" : "ENVANTER_BOŞ";
            string btnStatus = (takeButtonObject != null && takeButtonObject.activeSelf && canManageButton) ? "BUTON_AKTİF" : "BUTON_PASİF";
            string lastLookedName = lastLookedAtBabySahur == null ? "YOK" : lastLookedAtBabySahur.gameObject.name;
            Debug.Log($"[DURUM ÖZETİ] {gameObject.name} ({GetInstanceID()}): {canLookStatus}, {invStatus}, {btnStatus}. Odak: {lastLookedName}. BuOdaktaMı: {(lastLookedAtBabySahur == this)}", this);
        }
    }
    
    private static void HandleBabySahurBurned()
    {
        yakilmisBabySahurSayisi++;
        if (Debug.isDebugBuild)
        {
            Debug.Log($"Bir BabySahur yakıldı. Toplam yakılmış BabySahur sayısı: {yakilmisBabySahurSayisi}");
        }
    }

    public static void ResetYakilmisSayisi()
    {
        yakilmisBabySahurSayisi = 0;
        if (Debug.isDebugBuild) Debug.Log("Yakılmış BabySahur sayısı sıfırlandı.");
    }

    public void PickupBabySahur()
    {
        if (debugMode) Debug.Log($"{gameObject.name} ({GetInstanceID()}) için PickupBabySahur çağrıldı! dahaOnceKacmayiDeneydi: {dahaOnceKacmayiDeneydi}");

        if (dahaOnceKacmayiDeneydi)
        {
            AlVeYokEt();
            return;
        }
        dahaOnceKacmayiDeneydi = true;

        float kacisSansi = 1.0f; // GEÇİCİ TEST İÇİN
        if (debugMode) Debug.Log($"[KAÇIŞ KONTROL GEÇİCİ TEST] {gameObject.name} ({GetInstanceID()}) - Kaçış Şansı %100 olarak ayarlandı.", this);

        if (debugMode) Debug.Log($"[KAÇIŞ KONTROL] {gameObject.name} ({GetInstanceID()}) - Yakılmış: {yakilmisBabySahurSayisi}, Hesaplanan Şans: {kacisSansi * 100}% (Test nedeniyle %100)", this);
        float rastgeleDeger = Random.Range(0f, 1f);

        if (rastgeleDeger < kacisSansi) 
        {
            if (debugMode) Debug.Log($"[KAÇIŞ KONTROL] {gameObject.name} ({GetInstanceID()}) - Kaçış Şansı TUTTU! (Test nedeniyle garantili)", this);
            if (babySahurKacisParent != null && babySahurKacisParent.childCount > 0)
            {
                transform.rotation = Quaternion.Euler(AYAKTA_ROTATION_X, transform.eulerAngles.y, transform.eulerAngles.z);
                if(debugMode) Debug.Log($"[ROT_CTRL] {gameObject.name} ({GetInstanceID()}) Kaçış öncesi rotasyon: {transform.eulerAngles}", this);

                if (navMeshAgent != null)
                {
                    navMeshAgent.enabled = true; // NavMeshAgent'ı kaçış için aktif et
                    if(debugMode) Debug.Log($"[NAV_CTRL] {gameObject.name} ({GetInstanceID()}) NavMeshAgent kaçış için AKTİF edildi.", this);
                }
                else
                {
                    if(debugMode) Debug.LogError($"[NAV_CTRL] {gameObject.name} ({GetInstanceID()}) Kaçış başlatılırken NavMeshAgent null! Hareket edemeyecek.", this);
                    AlVeYokEt(); 
                    return;
                }

                int randomIndex = Random.Range(0, babySahurKacisParent.childCount);
                Transform hedefCheckpoint = babySahurKacisParent.GetChild(randomIndex);
                if (takeButtonObject != null) takeButtonObject.SetActive(false);
                if (gameObject.activeInHierarchy && this.enabled) StartCoroutine(KacRoutine(hedefCheckpoint));
            }
            else
            {
                if (debugMode) Debug.LogError($"[KAÇIŞ KONTROL] {gameObject.name} ({GetInstanceID()}) - Kaçış noktaları yok! Alınıyor.", this);
                AlVeYokEt();
            }
        }
        else
        {
            if (debugMode) Debug.Log($"[KAÇIŞ KONTROL] {gameObject.name} ({GetInstanceID()}) - Kaçış Şansı TUTMADI! Alınıyor. (Bu mesaj testte görünmemeli)", this);
            AlVeYokEt();
        }
    }

    private void AlVeYokEt()
    {
        if (debugMode) Debug.Log($"[AlVeYokEt] {gameObject.name} ({GetInstanceID()}) alınıyor ve yok ediliyor.", this);
        if (playerInventoryBabySahur != null) playerInventoryBabySahur.SetActive(true);
        if (takeButtonObject != null) takeButtonObject.SetActive(false);
        Destroy(gameObject);
    }

    IEnumerator KacRoutine(Transform hedefNokta)
    {
        if (navMeshAgent == null || !navMeshAgent.enabled)
        {
             if(debugMode) Debug.LogError($"[KacRoutine NAVMESH] {gameObject.name} ({this.GetInstanceID()}) Coroutine başlarken NavMeshAgent null veya devre dışı! Kaçış iptal. Agent Aktif Mi: {(navMeshAgent == null ? "NULL" : navMeshAgent.enabled.ToString())}", this);
             yield break;
        }
        if (debugMode) Debug.Log($"[KacRoutine NAVMESH] {gameObject.name} ({this.GetInstanceID()}) için başlatıldı. Hedef: {hedefNokta.name}. NavMeshAgent Aktif: {navMeshAgent.enabled}", this);

        if (_collider != null) _collider.enabled = false;
        else if(debugMode) Debug.LogWarning($"[KacRoutine NAVMESH] {gameObject.name} ({this.GetInstanceID()}) Kaçış sırasında devre dışı bırakılacak Collider bulunamadı!", this);

        StopCheckRoutine();
        isPlayerLooking = false;
        if(lastLookedAtBabySahur == this) lastLookedAtBabySahur = null;

        if (!navMeshAgent.isOnNavMesh)
        {
            if (debugMode) Debug.LogError($"[KacRoutine NAVMESH] {gameObject.name} ({this.GetInstanceID()}) NavMesh üzerinde değil! Kaçış iptal.", this);
            if (navMeshAgent != null) navMeshAgent.enabled = false; // Başarısızsa tekrar kapat
            transform.rotation = Quaternion.Euler(YATMA_ROTATION_X, transform.eulerAngles.y, transform.eulerAngles.z); // Tekrar yatır
            StartCheckRoutine(); // Oyuncu etkileşim kontrolünü yeniden başlat
            yield break;
        }
        
        navMeshAgent.SetDestination(hedefNokta.position);
        if(debugMode) Debug.Log($"[KacRoutine NAVMESH] {gameObject.name} ({this.GetInstanceID()}) Hedef atandı: {hedefNokta.position}. Path pending: {navMeshAgent.pathPending}, Path Status: {navMeshAgent.pathStatus}", this);
        
        navMeshAgent.speed = kacisHizi;
        navMeshAgent.isStopped = false;

        float lastLoggedVelocity = -1f;

        while (navMeshAgent != null && navMeshAgent.enabled && (navMeshAgent.pathPending || (navMeshAgent.hasPath && navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)))
        {
            float currentVelocity = navMeshAgent.velocity.magnitude;
            if (debugMode && Mathf.Abs(currentVelocity - lastLoggedVelocity) > 0.05f)
            {
                Debug.Log($"[KacRoutine NAVMESH DÖNGÜ] {gameObject.name} ({this.GetInstanceID()}) - Hız: {currentVelocity:F2}, KalanMesafe: {navMeshAgent.remainingDistance:F2}, PathStatus: {navMeshAgent.pathStatus}", this);
                lastLoggedVelocity = currentVelocity;
            }
            if (currentVelocity < 0.01f && navMeshAgent.hasPath && navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
            {
                 if(debugMode && Time.frameCount % 120 == 0) Debug.LogWarning($"[KacRoutine NAVMESH DÖNGÜ] {gameObject.name} ({this.GetInstanceID()}) - Hız çok düşük ({currentVelocity:F2}), takılmış olabilir! PathStatus: {navMeshAgent.pathStatus}", this);
            }

            // KAPIDAN GEÇME KONTROLÜ (SADECE AÇ/KAPAT) - BU BLOK TAMAMEN KALDIRILACAK
            /*
            if (navMeshAgent.isOnNavMesh && navMeshAgent.hasPath) 
            {
                RaycastHit doorHit;
                Vector3 desiredVelocity = navMeshAgent.desiredVelocity;
                Vector3 castDirection = desiredVelocity.normalized;

                if (desiredVelocity.sqrMagnitude < 0.01f) 
                {
                    castDirection = transform.forward;
                }
                castDirection.y = 0; 
                castDirection.Normalize(); 

                Vector3 sphereCastOrigin = transform.position + Vector3.up * (navMeshAgent.height / 2f) + castDirection * navMeshAgent.radius * 0.5f;

                if (Physics.SphereCast(sphereCastOrigin, kapiAlgilamaYaricapi, castDirection, out doorHit, kapiAlgilamaOnundekiMesafe, kapiLayerMask, QueryTriggerInteraction.Collide))
                {
                    if(debugMode && Time.frameCount % 30 == 0) Debug.Log($"[KAPI BASİT DEBUG] SphereCast bir şeye çarptı: {doorHit.collider.name}", doorHit.collider.gameObject);
                    Door door = doorHit.collider.GetComponent<Door>(); 
                    if (door != null && !door.isOpen) 
                    {
                        if(debugMode) Debug.Log($"[KAPI BASİT DEBUG] {doorHit.collider.name} üzerinde Door script'i bulundu ve kapı kapalı. Aç/Kapat tetikleniyor...", door.gameObject);
                        
                        door.OpenDoor(); 
                        
                        if (door.openAnimationDuration > 0)
                        {
                            yield return new WaitForSeconds(door.openAnimationDuration); 
                        }
                        else
                        {
                            yield return new WaitForSeconds(0.5f); 
                        }
                        
                        if(door.isOpen) 
                        {
                            if(debugMode) Debug.Log($"[KAPI BASİT DEBUG] {door.name} kapısı şimdi geri kapatılıyor...", door.gameObject);
                            door.CloseDoor();
                        }
                    }
                }
            }
            */
            yield return null;
        }

        if (navMeshAgent != null) // navMeshAgent null olabilir, kontrol ekle
        {
            navMeshAgent.isStopped = true;
            if (debugMode) Debug.Log($"[KacRoutine NAVMESH] {gameObject.name} ({this.GetInstanceID()}) hedefe ulaştı/durdu. KalanMesafe: {navMeshAgent.remainingDistance:F2}, Hız: {navMeshAgent.velocity.magnitude:F2}, PathStatus: {navMeshAgent.pathStatus}", this);
        }
        else if (debugMode)
        {
            Debug.LogWarning($"[KacRoutine NAVMESH] {gameObject.name} ({this.GetInstanceID()}) hedefe ulaşma kontrolü sırasında NavMeshAgent null bulundu.", this);
        }

        transform.rotation = Quaternion.Euler(YATMA_ROTATION_X, transform.eulerAngles.y, transform.eulerAngles.z);
        if(debugMode) Debug.Log($"[ROT_CTRL] {gameObject.name} ({GetInstanceID()}) Hedefe varış sonrası rotasyon: {transform.eulerAngles}", this);

        // Y pozisyonunu ayarla
        Vector3 currentPosition = transform.position;
        transform.position = new Vector3(currentPosition.x, currentPosition.y - 0.22236f, currentPosition.z);
        if(debugMode) Debug.Log($"[POS_CTRL] {gameObject.name} ({GetInstanceID()}) Hedefe varış sonrası Y pozisyonu ayarlandı: {transform.position.y}", this);

        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false; // NavMeshAgent'ı hedefe varınca devre dışı bırak
            if(debugMode) Debug.Log($"[NAV_CTRL] {gameObject.name} ({GetInstanceID()}) NavMeshAgent hedefe varınca devre dışı bırakıldı.", this);
        }
        StartCheckRoutine(); // Oyuncu etkileşim kontrolünü yeniden başlat
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);
        
        if (Application.isPlaying && playerCamera != null)
        {
            Vector3 eyePosition = transform.position + Vector3.up * 0.4f;
            Gizmos.color = isPlayerLooking ? Color.green : Color.red;
            Gizmos.DrawLine(playerCamera.position, eyePosition);
            
            if (isPlayerLooking)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(eyePosition, 0.2f);
            }
        }
    }
} 