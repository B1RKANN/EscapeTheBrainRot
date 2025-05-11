using UnityEngine;

namespace EscapeTheBrainRot
{
    public class WalkBobbingEffect : MonoBehaviour
    {
        [Header("Yürüme Sarsıntı Ayarları")]
        [Range(0.5f, 5.0f)]
        public float stepFrequency = 2.5f;       // Adım atma sıklığı
        [Range(0.01f, 0.15f)]
        public float verticalAmount = 0.08f;     // Yukarı-aşağı hareket miktarı (artırıldı)
        [Range(0.01f, 0.15f)]
        public float lateralAmount = 0.04f;      // Yanlara hareket miktarı (artırıldı)
        [Range(0.5f, 5.0f)]
        public float tiltAmount = 2.8f;          // Eğilme miktarı (derece) (artırıldı)
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
                
                // Hızlı güncelleme için MoveTowards kullanımı
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