using UnityEngine;

namespace EscapeTheBrainRot
{
    public class WalkBobbingEffect : MonoBehaviour
    {
        [Header("Yürüme Sarsıntı Ayarları")]
        [Range(0.5f, 5.0f)]
        public float stepFrequency = 2.0f;       // Adım atma sıklığı (Örn: 2.5 -> 2.0)
        [Range(0.01f, 0.15f)]
        public float verticalAmount = 0.03f;     // Yukarı-aşağı hareket miktarı (Örn: 0.08 -> 0.03)
        [Range(0.01f, 0.15f)]
        public float lateralAmount = 0.02f;      // Yanlara hareket miktarı (Örn: 0.04 -> 0.02)
        [Range(0.5f, 5.0f)]
        public float tiltAmount = 1.0f;          // Eğilme miktarı (derece) (Örn: 2.8 -> 1.0)
        [Range(5f, 20f)]
        public float smoothness = 15f;           // Yumuşatma faktörü (Örn: 10f -> 15f, daha yumuşak)
        
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private bool isShaking = false;
        private float currentIntensity = 0f;
        private float timer = 0f;

        // Son uygulanan sarsıntı miktarını saklamak için
        private Vector3 lastAppliedBobPosition = Vector3.zero;
        private Quaternion lastAppliedBobRotation = Quaternion.identity;
        
        void Start()
        {
            // Başlangıç pozisyonunu kaydet
            ResetOriginalPosition();
            // Başlangıçta sarsıntı uygulanmadığı için sıfırla
            lastAppliedBobPosition = Vector3.zero;
            lastAppliedBobRotation = Quaternion.identity;
        }
        
        void OnEnable()
        {
            // Bileşen aktif olduğunda pozisyonu kaydet
            ResetOriginalPosition();
            // Aktif olduğunda da sarsıntı uygulanmadığı için sıfırla
            lastAppliedBobPosition = Vector3.zero;
            lastAppliedBobRotation = Quaternion.identity;
        }
        
        void LateUpdate()
        {
            // Mevcut transform'dan geçen karedeki sarsıntıyı çıkararak "temel" transform'u elde et
            // Bu, fare bakışı gibi diğer script'lerin bu kare için ayarladığı transform'dur
            Quaternion baseRotationThisFrame = transform.localRotation * Quaternion.Inverse(lastAppliedBobRotation);
            Vector3 basePositionThisFrame = transform.localPosition - lastAppliedBobPosition;

            Vector3 targetPosition;
            Quaternion targetRotation;

            if (isShaking)
            {
                // Adım zamanlayıcısını güncelle
                timer += Time.deltaTime * stepFrequency * Mathf.PI;
                
                // Adım atma efekti için yukarı-aşağı (dikey) hareket
                float verticalBobValue = Mathf.Sin(timer * 2f) * verticalAmount * currentIntensity;
                // Sağa-sola hafif sallanma
                float lateralBobValue = Mathf.Sin(timer) * lateralAmount * currentIntensity;
                
                // Yürürken öne-arkaya ve yanlara hafif eğilme
                float tiltAngleXValue = Mathf.Sin(timer) * tiltAmount * currentIntensity;
                float tiltAngleZValue = Mathf.Cos(timer) * tiltAmount * 0.5f * currentIntensity;
                
                // Hesaplanan sarsıntı offset'leri
                Vector3 currentBobPositionOffset = new Vector3(lateralBobValue, verticalBobValue, 0);
                Quaternion currentBobRotationOffset = Quaternion.Euler(tiltAngleXValue, 0, tiltAngleZValue);
                
                // Hedef: Temel transform + mevcut sarsıntı offset'i
                targetPosition = basePositionThisFrame + currentBobPositionOffset;
                targetRotation = baseRotationThisFrame * currentBobRotationOffset;
                
                // Bir sonraki karede geri almak için bu karede uygulanan sarsıntıyı kaydet
                lastAppliedBobPosition = currentBobPositionOffset;
                lastAppliedBobRotation = currentBobRotationOffset;
            }
            else
            {
                // Sarsıntı aktif değilse hedef, sarsıntısız temel transform'dur
                targetPosition = basePositionThisFrame;
                targetRotation = baseRotationThisFrame;
                
                // Bir sonraki karede geri almak için sarsıntı olmadığını kaydet
                lastAppliedBobPosition = Vector3.zero;
                lastAppliedBobRotation = Quaternion.identity;
                
                timer = 0; // Zamanlayıcıyı sıfırla, bir önceki if bloğunun dışına taşıdım çünkü sarsıntı bitince sıfırlanmalı.
                               // Önceki kodda zaten dışarıdaydı, yerini korudum.
            }
            
            // Yumuşak geçişle hedefe ilerle
            // Not: transform.localPosition/Rotation (sol taraf) mevcut fiziksel değeri içerir (geçen karenin sarsıntısıyla birlikte)
            // Bu sayede (temel_poz + geçen_sarsıntı) durumundan (temel_poz + yeni_sarsıntı)'ya veya (temel_poz)'a yumuşak geçiş yapılır.
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition, 
                targetPosition, 
                Time.deltaTime * smoothness * 0.1f 
            );
            
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * smoothness * 1.0f // 10f yerine 1.0f olarak değiştirildi
            );
        }
        
        public void SetShakeActive(bool active, float intensity = 1.0f)
        {
            // Sarsıntı durumunu güncelle
            isShaking = active;
            currentIntensity = intensity;
            
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
} 