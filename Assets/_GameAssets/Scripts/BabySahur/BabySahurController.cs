using UnityEngine;
using UnityEngine.UI;

public class BabySahurController : MonoBehaviour
{
    [Header("Algılama Ayarları")]
    [SerializeField] private float detectionDistance = 5f; // Algılama mesafesi arttırıldı
    [SerializeField] private float viewAngle = 60f; // Görüş açısı arttırıldı
    [SerializeField] private LayerMask obstacleLayer; // Engelleri belirleyen layer mask
    [SerializeField] private bool useObstacleDetection = true; // Engel tespiti kullanılsın mı?
    [SerializeField] private bool debugMode = true;
    
    [Header("Referanslar")]
    [SerializeField] private GameObject takeButtonObject;
    [SerializeField] private GameObject playerInventoryBabySahur;
    
    private Transform playerCamera;
    private bool isPlayerLooking = false;
    private TakeButtonController takeButtonController;
    private static BabySahurController lastLookedAtBabySahur;
    private float lastTimeChecked = 0f;
    private const float CHECK_INTERVAL = 0.05f; // 50 milisaniyede bir kontrol et (daha sık)
    
    private void Awake()
    {
        // TakeButtonController referansını al
        if (takeButtonObject != null)
        {
            takeButtonController = takeButtonObject.GetComponent<TakeButtonController>();
            if (takeButtonController == null)
            {
                Debug.LogError("Take butonunda TakeButtonController script'i bulunamadı!");
            }
        }
    }
    
    private void Start()
    {
        // Ana kamerayı bul
        playerCamera = Camera.main.transform;
        if (playerCamera == null)
        {
            Debug.LogError("Ana kamera bulunamadı! Tag'i 'MainCamera' olan bir kamera olduğundan emin olun.");
        }
        else if (debugMode)
        {
            Debug.Log("Ana kamera bulundu: " + playerCamera.name);
        }
        
        // TakeButtonController referansını kontrol et
        if (takeButtonController != null)
        {
            if (debugMode)
            {
                Debug.Log("TakeButtonController bulundu");
            }
            
            // Başlangıçta Take butonunu devre dışı bırak
            takeButtonObject.SetActive(false);
            
            if (debugMode)
            {
                Debug.Log("Take butonu başlangıçta pasifleştirildi");
            }
        }
        else if (takeButtonObject != null)
        {
            Debug.LogError("Take butonu referansı ayarlanmış ama TakeButtonController bulunamadı!");
        }
        else
        {
            Debug.LogError("Take butonu referansı ayarlanmamış! Inspector'dan atayın.");
        }
        
        // Oyuncunun envanterindeki BabySahur başlangıçta pasif olmalı
        if (playerInventoryBabySahur != null)
        {
            playerInventoryBabySahur.SetActive(false);
            if (debugMode)
            {
                Debug.Log("Oyuncu envanteri BabySahur bulundu");
            }
        }
        else
        {
            Debug.LogError("Oyuncu BabySahur referansı ayarlanmamış! Inspector'dan atayın.");
        }
    }
    
    private void Update()
    {
        // Performans için, her karede değil belirli aralıklarla kontrol et
        if (Time.time - lastTimeChecked >= CHECK_INTERVAL)
        {
            lastTimeChecked = Time.time;
            CheckPlayerLookingAtBabySahur();
        }
    }
    
    private void CheckPlayerLookingAtBabySahur()
    {
        if (playerCamera == null) return;
        
        // Oyuncu ve BabySahur arasındaki mesafeyi kontrol et
        float distance = Vector3.Distance(playerCamera.position, transform.position);
        
        if (debugMode && Time.frameCount % 30 == 0) // Her 30 karede bir log göster
        {
            Debug.Log("BabySahur'a mesafe: " + distance.ToString("F2") + " (Max: " + detectionDistance + ")");
        }
        
        if (distance <= detectionDistance)
        {
            // BABYSAHUR ORTA NOKTASI YERİNE GÖZ SEVİYESİNE BAKMA KONTROLÜ
            // BabySahur'un "göz" seviyesini hesapla (biraz yukarısı)
            Vector3 eyePosition = transform.position + Vector3.up * 0.4f;
            
            // Oyuncunun bakış açısını kontrol et
            Vector3 directionToSahur = eyePosition - playerCamera.position;
            float angle = Vector3.Angle(playerCamera.forward, directionToSahur);
            
            if (debugMode && Time.frameCount % 30 == 0) // Her 30 karede bir log göster
            {
                Debug.Log("BabySahur'a bakış açısı: " + angle.ToString("F2") + " (Max: " + viewAngle + ")");
            }
            
            // Oyuncu uygun açı içinde bakıyor mu?
            bool isInAngle = angle <= viewAngle;
            bool hasLineOfSight = true; // Varsayılan olarak görüş alanı var
            
            // Engel tespiti
            if (useObstacleDetection && isInAngle)
            {
                // BabySahur ile kamera arasında engel var mı kontrol et
                hasLineOfSight = !Physics.Raycast(
                    playerCamera.position, 
                    directionToSahur, 
                    distance,
                    obstacleLayer
                );
                
                if (debugMode && Time.frameCount % 30 == 0) // Her 30 karede bir log göster
                {
                    Debug.Log("BabySahur ile görüş alanı: " + (hasLineOfSight ? "Açık" : "Engelli"));
                }
            }
            
            // Bakıyor mu sonucu
            bool isLooking = isInAngle && hasLineOfSight;
            
            // Eğer bakıyorsa
            if (isLooking)
            {
                // Bu BabySahur'u en son bakılan olarak ayarla
                lastLookedAtBabySahur = this;
                
                // Durum değiştiyse
                if (!isPlayerLooking)
                {
                    isPlayerLooking = true;
                    
                    if (debugMode)
                    {
                        Debug.Log(gameObject.name + " için bakış durumu değişti: Bakıyor");
                    }
                }
                
                // Buton durumunu kontrol et - her zaman güncel tut
                if (takeButtonObject != null && takeButtonController != null)
                {
                    if (!takeButtonObject.activeSelf)
                    {
                        takeButtonObject.SetActive(true);
                        
                        // TakeButtonController'a bu BabySahur'u bildir
                        takeButtonController.SetCurrentBabySahur(this);
                        
                        if (debugMode)
                        {
                            Debug.Log("Take butonu aktifleştirildi ve " + gameObject.name + " referansı atandı");
                        }
                    }
                }
            }
            // Bakmıyorsa ama önceden bakıyorduysa
            else if (isPlayerLooking && lastLookedAtBabySahur == this)
            {
                // Artık bakmıyor
                isPlayerLooking = false;
                lastLookedAtBabySahur = null;
                
                if (debugMode)
                {
                    Debug.Log(gameObject.name + " için bakış durumu değişti: Bakmıyor");
                }
                
                if (takeButtonObject != null)
                {
                    takeButtonObject.SetActive(false);
                    
                    if (debugMode)
                    {
                        Debug.Log("Take butonu pasifleştirildi");
                    }
                }
            }
        }
        else if (isPlayerLooking && lastLookedAtBabySahur == this)
        {
            // Mesafe dışına çıkıldığında buton pasif olsun
            isPlayerLooking = false;
            lastLookedAtBabySahur = null;
            
            if (takeButtonObject != null)
            {
                takeButtonObject.SetActive(false);
                if (debugMode)
                {
                    Debug.Log("Mesafe aşıldı, Take butonu pasifleştirildi");
                }
            }
        }
    }
    
    // Take butonuna tıklandığında çağrılacak fonksiyon
    public void PickupBabySahur()
    {
        if (debugMode)
        {
            Debug.Log(gameObject.name + " için PickupBabySahur çağrıldı! isPlayerLooking: " + isPlayerLooking);
        }
        
        // Oyuncunun envanterindeki BabySahur'u aktif et
        if (playerInventoryBabySahur != null)
        {
            playerInventoryBabySahur.SetActive(true);
            if (debugMode)
            {
                Debug.Log("Oyuncu envanterindeki BabySahur aktifleştirildi");
            }
        }
        
        // Take butonunu kapat
        if (takeButtonObject != null)
        {
            takeButtonObject.SetActive(false);
            if (debugMode)
            {
                Debug.Log("Take butonu kapatıldı");
            }
        }
        
        // Bu BabySahur objesini yok et
        if (debugMode)
        {
            Debug.Log(gameObject.name + " yok ediliyor...");
        }
        Destroy(gameObject);
    }
    
    // Gizmo çizerek debug gösterimi sağla
    private void OnDrawGizmosSelected()
    {
        // Algılama menzilini göster
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);
        
        // Kamera varsa bakış açısı konisini göster
        if (Application.isPlaying && playerCamera != null)
        {
            // BabySahur'un göz seviyesi
            Vector3 eyePosition = transform.position + Vector3.up * 0.4f;
            Gizmos.color = isPlayerLooking ? Color.green : Color.red;
            Gizmos.DrawLine(playerCamera.position, eyePosition);
            
            // Görüş açısını göster
            if (isPlayerLooking)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(eyePosition, 0.2f);
            }
        }
    }
} 