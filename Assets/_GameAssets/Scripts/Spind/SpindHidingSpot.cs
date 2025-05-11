using UnityEngine;
using UnityEngine.UI;

namespace EscapeTheBrainRot
{
    [RequireComponent(typeof(BoxCollider))]  // Trigger için BoxCollider gerekli
    public class SpindHidingSpot : MonoBehaviour
    {
        [Header("Etkileşim Ayarları")]
        public Transform exitPosition;            // Çıkış pozisyonu
        public HideButton hideButtonUI;           // Saklanma buton UI referansı
        public float triggerSize = 2f;            // Trigger boyutu (Inspector'dan düzenlenebilir)
        
        [Header("Gizlenme Ayarları")]
        public Camera spindCam;                   // Dolap içi kamera
        public AudioClip doorOpenSound;           // Kapı açılma sesi
        public AudioClip doorCloseSound;          // Kapı kapanma sesi
        
        [Header("UI Ayarları")]
        public GameObject[] uiElementsToHide; // Saklanma sırasında gizlenecek UI elementleri
        
        [Header("Kamera Sarsıntı Ayarları")]
        public bool enableBreathingEffect = true;   // Nefes alma efektini etkinleştirir/devre dışı bırakır
        public float breathingIntensity = 0.002f;   // Sarsıntı gücünü düşürdük (öncekine göre yarı yarıya)
        public float breathingSpeed     = 1.0f;     // Nefes alma hızını biraz yavaşlattık
        public bool onlyVerticalMovement = true;    // Sadece yukarı-aşağı hareket
        public float randomIntensity    = 0.0008f;  // Rastgele küçük oynama ekledik, ama çok hafif   // Rastgele hareket gücü
        
        private bool playerInRange = false;       // Oyuncu menzilde mi?
        private bool playerHiding = false;        // Oyuncu saklanıyor mu?
        private GameObject playerObject;          // Oyuncu GameObject'i
        private AudioSource audioSource;          // Ses efekti için
        private BoxCollider triggerCollider;      // Trigger collider'ı
        private Vector3 originalCamPosition;       // Kameranın orijinal pozisyonu
        private float breathingTime = 0f;          // Nefes alma zamanlayıcısı
        
        void Start()
        {
            // Çıkış pozisyonu kontrolü
            if (exitPosition == null)
                exitPosition = transform;
                
            // Trigger collider ayarları
            SetupTriggerCollider();
            
            // Ses bileşeni ekle
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
            audioSource.volume = 0.7f;
                
            // Buton ve kamera kontrolü
            if (hideButtonUI == null)
                Debug.LogError("HATA: Saklanma butonu UI referansı atanmamış!");
                
            if (spindCam == null)
                Debug.LogError("HATA: Dolap kamerası atanmamış!");
            else
                spindCam.enabled = false; // Başlangıçta kamera kapalı
                
            // Otomatik olarak UI elementlerini bul (eğer belirtilmemişse)
            if (uiElementsToHide == null || uiElementsToHide.Length == 0)
            {
                AutoFindUIElements();
            }
        }
        
        // Update metodu - kamera sarsıntı efekti için
        void Update()
        {
            // Sadece oyuncu saklanma modundayken ve nefes efekti etkinse
            if (playerHiding && spindCam != null && spindCam.enabled && enableBreathingEffect)
            {
                ApplyBreathingEffect();
            }
        }
        
        // Nefes alma efekti uygula
        private void ApplyBreathingEffect()
        {
            if (originalCamPosition == Vector3.zero)
            {
                originalCamPosition = spindCam.transform.localPosition;
            }
            
            // Zaman ilerlet
            breathingTime += Time.deltaTime * breathingSpeed;
            
            // Sinüs dalgası ile yukarı-aşağı hareket (nefes alma hissi)
            float breathingY = Mathf.Sin(breathingTime) * breathingIntensity;
            
            if (onlyVerticalMovement)
            {
                // Sadece yukarı-aşağı hareket
                spindCam.transform.localPosition = originalCamPosition + new Vector3(0f, breathingY, 0f);
            }
            else
            {
                // Rastgele küçük hareketler ekleyerek titreme hissi ver
                Vector3 randomMovement = Random.insideUnitSphere * randomIntensity;
                
                // Kameranın pozisyonunu güncelle
                spindCam.transform.localPosition = originalCamPosition + new Vector3(randomMovement.x, breathingY + randomMovement.y, randomMovement.z);
            }
        }
        
        // UI elementlerini otomatik olarak bul
        private void AutoFindUIElements()
        {
            // Canvas'ı bul
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                // Canvas altındaki tüm birinci seviye objeleri bul
                Transform canvasTransform = canvas.transform;
                int childCount = canvasTransform.childCount;
                
                // Hide dışındaki elementleri listeye ekle
                System.Collections.Generic.List<GameObject> elements = new System.Collections.Generic.List<GameObject>();
                for (int i = 0; i < childCount; i++)
                {
                    Transform child = canvasTransform.GetChild(i);
                    if (child.name != "Hide") // Hide dışındaki elementleri ekle
                    {
                        elements.Add(child.gameObject);
                    }
                }
                
                uiElementsToHide = elements.ToArray();
                Debug.Log($"Otomatik olarak {uiElementsToHide.Length} UI elementi bulundu.");
            }
            else
            {
                Debug.LogWarning("Canvas bulunamadı, UI elementleri otomatik eklenemedi.");
            }
        }
        
        // Trigger collider'ı ayarla
        private void SetupTriggerCollider()
        {
            triggerCollider = GetComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(triggerSize, triggerSize, triggerSize);
            triggerCollider.center = new Vector3(0, 0, triggerSize / 2f);
        }
        
        // Oyuncu trigger alanına girdiğinde
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerObject = other.gameObject;
                playerInRange = true;
                ShowHideButton("Saklan");
            }
        }
        
        // Oyuncu trigger alanından çıktığında
        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                HideButton();
            }
        }
        
        // Butona basıldığında çağrılır
        public void OnHideButtonPressed()
        {
            Debug.Log("Dolap butonuna basıldı. playerHiding: " + playerHiding);
            
            if (playerHiding)
                ExitHidingSpot();
            else
                EnterHidingSpot();
        }
        
        private void EnterHidingSpot()
        {
            Debug.Log("EnterHidingSpot çağrıldı");
            
            if (playerObject == null)
            {
                Debug.LogError("HATA: Oyuncu referansı bulunamadı!");
                return;
            }
            
            // Oyuncuyu devre dışı bırak
            playerObject.SetActive(false);
            Debug.Log("Oyuncu deaktive edildi");
            
            // Dolap kamerasını aktifleştir
            if (spindCam != null)
            {
                Debug.Log("Kamera aktif ediliyor: " + spindCam.name);
                
                // Kameranın orijinal pozisyonunu kaydet
                originalCamPosition = spindCam.transform.localPosition;
                
                // Nefes alma zamanlayıcısını sıfırla
                breathingTime = 0f;
                
                spindCam.enabled = true;
                Debug.Log("Kamera aktif edildi mi: " + spindCam.enabled);
            }
            else
            {
                Debug.LogError("HATA: spindCam referansı null!");
            }
            
            // Diğer UI elementlerini gizle
            SetUIElementsActive(false);
            
            // Durumu güncelle
            playerHiding = true;
            ShowHideButton("Çık");
            
            // Kapı kapanma sesi çal
            if (doorCloseSound != null)
            {
                audioSource.clip = doorCloseSound;
                audioSource.Play();
            }
        }
        
        private void ExitHidingSpot()
        {
            // Dolap kamerasını devreden çıkar
            if (spindCam != null)
            {
                // Kamerayı orijinal pozisyonuna döndür
                if (originalCamPosition != Vector3.zero)
                {
                    spindCam.transform.localPosition = originalCamPosition;
                }
                
                spindCam.enabled = false;
            }
            
            // Oyuncuyu tekrar aktifleştir ve çıkış noktasına yerleştir
            if (playerObject != null)
            {
                playerObject.SetActive(true);
                playerObject.transform.position = exitPosition.position;
            }
            else
            {
                Debug.LogError("HATA: Oyuncu objesi bulunamadı!");
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerObject = player;
                    playerObject.SetActive(true);
                    playerObject.transform.position = exitPosition.position;
                }
            }
            
            // Diğer UI elementlerini göster
            SetUIElementsActive(true);
            
            // Durumu güncelle
            playerHiding = false;
            ShowHideButton("Saklan");
            
            // Kapı açılma sesi çal
            if (doorOpenSound != null)
            {
                audioSource.clip = doorOpenSound;
                audioSource.Play();
            }
        }
        
        // UI elementlerini aktif/pasif yap
        private void SetUIElementsActive(bool active)
        {
            if (uiElementsToHide != null)
            {
                foreach (GameObject uiElement in uiElementsToHide)
                {
                    if (uiElement != null)
                    {
                        uiElement.SetActive(active);
                        Debug.Log($"UI elementi {uiElement.name} {(active ? "gösterildi" : "gizlendi")}");
                    }
                }
            }
        }
        
        private void ShowHideButton(string text)
        {
            if (hideButtonUI != null)
                hideButtonUI.SetTargetSpind(this, text);
            else
                Debug.LogError("HATA: hideButtonUI referansı bulunamadı!");
        }
        
        private void HideButton()
        {
            if (hideButtonUI != null)
                hideButtonUI.HideButtonUI();
        }
        
        // Gizmo çizim
        void OnDrawGizmosSelected()
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Vector3 center = transform.position + transform.rotation * boxCollider.center;
                Vector3 size = Vector3.Scale(boxCollider.size, transform.lossyScale);
                Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
                Gizmos.DrawCube(Vector3.zero, size);
                
                Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
                Gizmos.DrawWireCube(Vector3.zero, size);
            }
            
            if (exitPosition != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(exitPosition.position, 0.3f);
            }
            
            if (spindCam != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(spindCam.transform.position, 0.1f);
                Gizmos.DrawRay(spindCam.transform.position, spindCam.transform.forward * 0.5f);
            }
        }
    }
} 