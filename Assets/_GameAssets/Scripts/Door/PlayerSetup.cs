using UnityEngine;

// Bu script oyuncu objesinin doğru ayarlanmış olduğundan emin olmak için kullanılır
public class PlayerSetup : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Oyuncunun tag'i 'Player' olarak ayarlanır")]
    [SerializeField] private bool enforcePlayerTag = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private void Awake()
    {
        if (enforcePlayerTag && tag != "Player")
        {
            Debug.LogWarning("PlayerSetup: Player tag'i eksik! Otomatik olarak ayarlandı.");
            tag = "Player";
        }
        
        // Debug bilgisi
        if (showDebugInfo)
        {
            Debug.Log($"PlayerSetup: Oyuncu objesi başlatıldı! Tag: {tag}, Pozisyon: {transform.position}");
        }
    }
    
    private void Start()
    {
        // Kapı trigger'larını bul ve debug bilgisi
        DoorTrigger[] doorTriggers = FindObjectsOfType<DoorTrigger>();
        
        if (showDebugInfo)
        {
            if (doorTriggers.Length > 0)
            {
                Debug.Log($"PlayerSetup: {doorTriggers.Length} adet kapı trigger'ı bulundu:");
                
                foreach (DoorTrigger trigger in doorTriggers)
                {
                    float distance = Vector3.Distance(transform.position, trigger.transform.position);
                    Debug.Log($"  - {trigger.name}: Uzaklık = {distance:F2} birim");
                }
            }
            else
            {
                Debug.LogWarning("PlayerSetup: Hiç kapı trigger'ı bulunamadı!");
            }
        }
    }
    
    // Hata ayıklama için oyuncunun konumunu görselleştir
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2);
    }
} 