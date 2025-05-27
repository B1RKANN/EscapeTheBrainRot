using UnityEngine;

namespace EscapeTheBrainRot
{
    public class PlayerMove : MonoBehaviour
    {
        public FixedJoystick joystick;
        public float SpeedMove = 5f;
        private CharacterController characterController;
        private bool canMove = true; // Hareketi kontrol etmek için bayrak
        
        [Header("Yürüme Sarsıntı Ayarları")]
        public WalkBobbingEffect walkEffect;
        public Camera playerCamera; // Elle atanabilir kamera referansı
        
        [Header("Yerçekimi Ayarları")]
        public float gravity = 20f;          // Yerçekimi şiddeti
        public float jumpHeight = 2.0f;      // Zıplama yüksekliği (isteğe bağlı)
        private float verticalVelocity = 0f;  // Dikey hız (m/s)
        private bool isGrounded;             // Yerde miyiz kontrolü
        
        void Start()
        {
            characterController = GetComponent<CharacterController>();
            
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                Debug.Log("PlayerMove: Ana kamera otomatik atandı: " + (playerCamera != null ? playerCamera.name : "NULL!"));
            }
            
            if (walkEffect == null && playerCamera != null)
            {
                walkEffect = playerCamera.gameObject.GetComponent<WalkBobbingEffect>();
                if (walkEffect == null)
                {
                    walkEffect = playerCamera.gameObject.AddComponent<WalkBobbingEffect>();
                    Debug.Log("WalkBobbingEffect kameraya otomatik eklendi: " + playerCamera.name);
                }
            }
            else if (walkEffect == null)
            {
                Debug.LogError("HATA: Kamera bulunamadı! Inspector'dan walkEffect veya playerCamera'yı ayarlayın!");
            }
        }

        void Update()
        {
            ApplyGravity(); // Yerçekimini her zaman uygula ve verticalVelocity'yi güncelle
            
            if (!canMove) 
            {
                // Hareket kısıtlıysa, sadece dikey hareketi (yerçekimi tarafından yönetilen) uygula
                characterController.Move(new Vector3(0f, verticalVelocity * Time.deltaTime, 0f));
                return;
            }
            
            float horizontalInput = joystick.Horizontal;
            float verticalInput = joystick.Vertical;
            
            // Yatay hareket yönünü belirle
            Vector3 horizontalDir = transform.right * horizontalInput + transform.forward * verticalInput;
            
            // Yatay hareketi hız ile ölçekle (bu bir hız vektörü m/s olur)
            Vector3 horizontalVelocity = horizontalDir * SpeedMove;
            
            // Son hareket vektörünü oluştur (X ve Z yatay hızdan, Y dikey hızdan)
            // verticalVelocity zaten bir hız (m/s) olduğu için doğrudan kullanılır.
            Vector3 finalVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
            
            // CharacterController.Move, Time.deltaTime ile çarpılmış bir *yer değiştirme* bekler.
            // Bu yüzden finalVelocity (hız vektörü) Time.deltaTime ile çarpılır.
            characterController.Move(finalVelocity * Time.deltaTime);
            
            // Kamera sarsıntısını aktifleştir veya devre dışı bırak
            if (walkEffect != null)
            {
                // Yürüme efekti için hareket, sadece yatay girdilere göre belirlenmeli
                bool isHorizontallyMoving = horizontalDir.sqrMagnitude > 0.01f; // Küçük bir eşik değer

                if (isHorizontallyMoving && isGrounded) // Sadece yerdeyken ve yatay hareket ederken sarsıntı olsun
                {
                    // Sarsıntı yoğunluğunu yatay hareketin büyüklüğüne göre ayarla
                    float intensity = Mathf.Clamp01(horizontalDir.magnitude * SpeedMove / 5f); // SpeedMove'a göre normalleştirilmiş bir yoğunluk (Örn: max SpeedMove 5 ise)
                    walkEffect.SetShakeActive(true, intensity);
                }
                else
                {
                    walkEffect.SetShakeActive(false, 0);
                }
            }
            else if ((Mathf.Abs(horizontalInput) > 0.05f || Mathf.Abs(verticalInput) > 0.05f) && Time.frameCount % 300 == 0)
            {
                Debug.LogError("HATA: WalkBobbingEffect bileşeni bulunamadı!");
            }
        }
        
        void ApplyGravity()
        {
            isGrounded = characterController.isGrounded;
            
            if (isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f; // Yerdeyken sabit, hafif bir aşağı doğru kuvvet
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime; // Yerçekimini uygula
            }
        }

        public bool IsGrounded()
        {
            return characterController.isGrounded;
        }

        /// <summary>
        /// Oyuncu hareketini aktif veya deaktif eder.
        /// </summary>
        /// <param name="isActive">Hareket aktif mi?</param>
        public void SetMovementActive(bool isActive)
        {
            canMove = isActive;
            if (!isActive && walkEffect != null)
            {
                walkEffect.SetShakeActive(false, 0); 
            }
            Debug.Log("[PlayerMove] Hareket durumu ayarlandı: " + (isActive ? "Aktif" : "Deaktif"));
        }
    }
}

// Tüm kamera sarsıntı mantığını ayrı bir bileşene taşıyorum
[ExecuteInEditMode] // Bu sayede editor modunda da test edilebilir
public class CameraShake : MonoBehaviour
{
    [Header("Yürüme Sarsıntı Ayarları")]
    [Range(0.5f, 5.0f)]
    public float stepFrequency = 2.5f;       // Adım atma sıklığı
    [Range(0.01f, 0.1f)]
    public float verticalAmount = 0.03f;     // Yukarı-aşağı hareket miktarı
    [Range(0.01f, 0.1f)]
    public float lateralAmount = 0.02f;      // Yanlara hareket miktarı
    [Range(0.5f, 3.0f)]
    public float tiltAmount = 1.5f;          // Eğilme miktarı (derece)
    [Range(5f, 20f)]
    public float smoothness = 10f;           // Yumuşatma faktörü
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isShaking = false;
    private float currentIntensity = 0f;
    private float timer = 0f;
    
    void Start()
    {
        // Başlangıç pozisyonunu kaydet
        ResetOriginalPosition();
    }
    
    void OnEnable()
    {
        // Bileşen aktif olduğunda pozisyonu kaydet
        ResetOriginalPosition();
    }
    
    void LateUpdate()
    {
        // Sarsıntı aktif değilse ve zaten orijinal pozisyondaysak, hiçbir şey yapma
        if (!isShaking && Vector3.Distance(transform.localPosition, originalPosition) < 0.001f)
        {
            return;
        }
        
        if (isShaking)
        {
            // Adım zamanlayıcısını güncelle
            timer += Time.deltaTime * stepFrequency * Mathf.PI;
            
            // Adım atma efekti için yukarı-aşağı (dikey) hareket
            float verticalBob = Mathf.Sin(timer * 2f) * verticalAmount * currentIntensity;
            
            // Sağa-sola hafif sallanma
            float lateralBob = Mathf.Sin(timer) * lateralAmount * currentIntensity;
            
            // Yürürken öne-arkaya ve yanlara hafif eğilme
            float tiltAngleX = Mathf.Sin(timer) * tiltAmount * currentIntensity;
            float tiltAngleZ = Mathf.Cos(timer) * tiltAmount * 0.5f * currentIntensity;
            
            // Pozisyon ve rotasyon güncelleme
            Vector3 bobPosition = new Vector3(lateralBob, verticalBob, 0);
            Quaternion bobRotation = Quaternion.Euler(tiltAngleX, 0, tiltAngleZ);
            
            // Hızlı güncelleme için Lerp yerine MoveTowards
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition, 
                originalPosition + bobPosition, 
                Time.deltaTime * smoothness * 0.1f
            );
            
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                originalRotation * bobRotation,
                Time.deltaTime * smoothness * 10f
            );
            
            // Debug - her 60 karede bir pozisyonu yazdır
            if (Time.frameCount % 60 == 0)
            {
                Vector3 delta = transform.localPosition - originalPosition;
                Debug.Log("Kamera hareketleniyor: " + delta.magnitude.ToString("F5") + " birim");
            }
        }
        else
        {
            // Sarsıntı aktif değilse orijinal konuma geri dön
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition, 
                originalPosition, 
                Time.deltaTime * smoothness * 0.1f
            );
            
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                originalRotation,
                Time.deltaTime * smoothness * 10f
            );
            
            timer = 0;
        }
    }
    
    public void SetShakeActive(bool active, float intensity = 1.0f)
    {
        // Sarsıntı durumunu güncelle
        isShaking = active;
        currentIntensity = intensity;
        
        // Debug - sarsıntı durumu değiştiğinde yazdır
        if (active && Time.frameCount % 60 == 0)
        {
            Debug.Log("Sarsıntı aktifleştirildi. Yoğunluk: " + intensity);
        }
        
        if (!active)
        {
            timer = 0;
        }
    }
    
    // Oyun başladığında orijinal pozisyonu kaydet
    public void ResetOriginalPosition()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        Debug.Log("Kamera pozisyonu kaydedildi: " + originalPosition);
    }
}
