using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrownBatController : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Sopa bir şeye çarptıktan sonra ne kadar sürede yok edilecek (saniye)")]
    public float destroyDelayAfterHit = 0.5f;
    [Tooltip("Sahur karakterinin sahip olduğu etiket")]
    public string sahurTag = "Sahur";
    [Tooltip("Zemin nesnelerinin sahip olduğu etiket")]
    public string groundTag = "Ground";
    [Tooltip("Sahur'u bayıltma süresi (saniye)")]
    public float stunDuration = 30.0f;

    private bool hasHit = false;

    // Start fonksiyonu artık boş, çünkü sopanın kendi kendine yok olmasını istemiyoruz.
    void Start()
    {
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Eğer sopa zaten yok olmak üzere işaretlendiyse, tekrar işlem yapma.
        if (hasHit) return;

        bool shouldBeDestroyed = false;

        // Çarptığımız nesne Sahur mu?
        if (collision.gameObject.CompareTag(sahurTag))
        {
            Debug.Log("Fırlatılan sopa, Sahur'a çarptı!");
            
            // SahurAIController'ı al ve bayıltma metodunu çağır
            SahurAIController sahurAI = collision.gameObject.GetComponent<SahurAIController>();
            if (sahurAI != null)
            {
                sahurAI.GetStunned(stunDuration);
            }
            else
            {
                Debug.LogError("Sahur objesinde ('" + sahurTag + "' etiketli) SahurAIController betiği bulunamadı!");
            }
            shouldBeDestroyed = true;
        }
        // Çarptığımız nesne Zemin mi?
        else if (collision.gameObject.CompareTag(groundTag))
        {
            Debug.Log("Fırlatılan sopa zemine çarptı: " + collision.gameObject.name);
            shouldBeDestroyed = true;
        }
        else 
        {
            // Diğer nesnelere çarptığında (duvar vb.) sadece logla, yok etme.
            Debug.Log("Fırlatılan sopa bir engele çarptı: " + collision.gameObject.name);
        }
        
        // Eğer Sahur'a veya zemine çarptıysa yok etme sürecini başlat.
        if(shouldBeDestroyed)
        {
            hasHit = true;

            // Fiziği devre dışı bırak ve objeyi gecikmeli olarak yok et
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Hızı sıfırlayıp kinematic yapmak, çarpışma sonrası garip sekmeleri önler.
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Objeyi gecikmeli olarak yok et
            Destroy(gameObject, destroyDelayAfterHit);
        }
    }
} 