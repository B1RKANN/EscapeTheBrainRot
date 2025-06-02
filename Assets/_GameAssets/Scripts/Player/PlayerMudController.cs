using UnityEngine;
using System; // Action için gerekli

namespace EscapeTheBrainRot
{
    [RequireComponent(typeof(PlayerMove))] // PlayerMove betiğinin oyuncuda olduğunu varsayıyoruz
    public class PlayerMudController : MonoBehaviour
    {
        public static event Action OnPlayerEnteredMud;
        public static event Action OnPlayerExitedMud;

        [Tooltip("Çamura girildiğinde oyuncunun hızının ne kadar yavaşlayacağını belirler (örn: 0.5 yarı hız).")]
        [SerializeField] private float mudSpeedMultiplier = 0.5f;
        [Tooltip("Çamur olarak algılanacak nesnelerin sahip olması gereken etiket.")]
        [SerializeField] private string mudTag = "Mud";

        private PlayerMove playerMove;
        private float originalSpeedMultiplier; // Oyuncunun normal hız çarpanını saklamak için

        private void Awake()
        {
            playerMove = GetComponent<PlayerMove>();
            if (playerMove == null)
            {
                Debug.LogError("[PlayerMudController] PlayerMove bileşeni bu GameObject üzerinde bulunamadı!", this);
                enabled = false;
                return;
            }
            // PlayerMove betiğinizde mevcut hız çarpanını alabileceğiniz bir özellik veya metot olduğunu varsayalım.
            // Şimdilik varsayılan olarak 1.0f (normal hız) alıyoruz.
            // originalSpeedMultiplier = playerMove.GetCurrentSpeedMultiplier(); // Örnek
            originalSpeedMultiplier = 1.0f; // Varsayılan
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(mudTag))
            {
                Debug.Log("[PlayerMudController] Oyuncu çamura girdi: " + other.name, this);
                // PlayerMove betiğinizde hızı ayarlamak için bir metot olduğunu varsayıyoruz.
                // Örneğin: playerMove.SetSpeedMultiplier(mudSpeedMultiplier);
                // Eğer PlayerMove betiğinizde böyle bir metot yoksa, onu eklemeniz gerekecektir.
                // Şimdilik sadece bir Debug.Log bırakıyorum ve olayı tetikliyorum.
                if (playerMove != null)
                {
                    // Örnek: playerMove.ApplySpeedModifier(mudSpeedMultiplier); 
                    // Ya da doğrudan bir özellik varsa:
                    // playerMove.speedMultiplier = mudSpeedMultiplier;
                    playerMove.SetSpeedMultiplier(mudSpeedMultiplier); // PlayerMove'daki metodu çağır
                    Debug.LogWarning("[PlayerMudController] PlayerMove.SetSpeedMultiplier() çağrıldı!", this);
                }

                OnPlayerEnteredMud?.Invoke();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(mudTag))
            {
                Debug.Log("[PlayerMudController] Oyuncu çamurdan çıktı: " + other.name, this);
                // Oyuncunun hızını normale döndür
                // Örneğin: playerMove.SetSpeedMultiplier(originalSpeedMultiplier);
                if (playerMove != null)
                {
                    // Örnek: playerMove.ApplySpeedModifier(originalSpeedMultiplier);
                    // playerMove.speedMultiplier = originalSpeedMultiplier;
                    playerMove.SetSpeedMultiplier(originalSpeedMultiplier); // PlayerMove'daki metodu çağır (normale döndür)
                    Debug.LogWarning("[PlayerMudController] PlayerMove.SetSpeedMultiplier() çağrıldı (normale döndürmek için)!", this);
                }

                OnPlayerExitedMud?.Invoke();
            }
        }
    }
} 