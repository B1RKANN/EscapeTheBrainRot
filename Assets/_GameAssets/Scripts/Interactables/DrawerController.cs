using UnityEngine;

[RequireComponent(typeof(Collider))] // Bu scriptin bir Collider'a sahip olmasını zorunlu kıl
public class DrawerController : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Her bir çekmece gözünün Animator bileşenlerini içeren dizi.")]
    [SerializeField] private Animator[] compartmentAnimators;

    [Tooltip("Her bir kompartıman Animator'ünde kullanılacak boolean parametresinin adı (örn: IsOpen)")]
    [SerializeField] private string openParameterName = "IsOpen";

    [Header("Yakınlık Etkileşimi")]
    [Tooltip("Oyuncu yaklaştığında görünecek olan World Space Canvas'lar (her çekmece gözü için bir tane). Trigger girildiğinde hepsi birden aktifleşir.")]
    [SerializeField] private GameObject[] worldSpaceCanvases;

    [Tooltip("Oyuncunun etkileşim alanını belirleyen trigger collider. Otomatik olarak bu GameObject'ten alınır.")]
    private Collider interactionTrigger;

    void Awake()
    {
        interactionTrigger = GetComponent<Collider>();
        if (interactionTrigger == null)
        {
            Debug.LogError("DrawerController: Etkileşim için Collider bulunamadı! Lütfen bu GameObject'e bir Collider ekleyin.", this);
        }
        else
        {
            interactionTrigger.isTrigger = true; // Collider'ın trigger olduğundan emin ol
        }

        if (compartmentAnimators == null || compartmentAnimators.Length == 0)
        {
            Debug.LogError("DrawerController: Kompartıman Animatorleri (compartmentAnimators) dizisi atanmamış veya boş! Lütfen Inspector'dan atayın.", this);
            // Animators olmadan devam etmek sorun yaratabilir, bu yüzden return;
        }
        else
        {
            for (int i = 0; i < compartmentAnimators.Length; i++)
            {
                if (compartmentAnimators[i] == null)
                {
                    Debug.LogError($"DrawerController: compartmentAnimators dizisindeki {i}. indeksteki Animator atanmamış! Lütfen Inspector'dan atayın.", this);
                }
            }
        }

        if (worldSpaceCanvases == null || worldSpaceCanvases.Length == 0)
        {
            Debug.LogWarning("DrawerController: World Space Canvas dizisi (worldSpaceCanvases) atanmamış veya boş. Yakınlık etkileşimi Canvas'ları kontrol edemeyecek.", this);
        }
        else
        {
            for (int i = 0; i < worldSpaceCanvases.Length; i++)
            {
                if (worldSpaceCanvases[i] == null)
                {
                    Debug.LogError($"DrawerController: worldSpaceCanvases dizisindeki {i}. indeksteki Canvas atanmamış! Lütfen Inspector'dan atayın.", this);
                }
            }
        }
    }

    void Start()
    {
        // Başlangıçta tüm World Space Canvas'ları pasif yap
        if (worldSpaceCanvases != null)
        {
            foreach (GameObject canvasGO in worldSpaceCanvases)
            {
                if (canvasGO != null)
                {
                    canvasGO.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Belirtilen indeksteki kompartımanın açık/kapalı durumunu değiştirir.
    /// </summary>
    /// <param name="compartmentIndex">Değiştirilecek kompartımanın indeksi (compartmentAnimators dizisiyle eşleşir).</param>
    public void ToggleCompartmentState(int compartmentIndex)
    {
        if (compartmentAnimators == null || compartmentAnimators.Length == 0 || compartmentAnimators[compartmentIndex] == null)
        {
            Debug.LogError("DrawerController: Toggle için compartmentAnimators uygun şekilde ayarlanmamış.", this);
            return;
        }

        if (compartmentIndex < 0 || compartmentIndex >= compartmentAnimators.Length)
        {
            Debug.LogError($"DrawerController: Geçersiz kompartıman indeksi: {compartmentIndex}. İndeks 0 ile {compartmentAnimators.Length - 1} arasında olmalıdır.", this);
            return;
        }

        Animator targetAnimator = compartmentAnimators[compartmentIndex];
        // targetAnimator null check'i yukarıda yapıldı (compartmentAnimators[compartmentIndex] == null)

        bool paramExists = false;
        foreach (AnimatorControllerParameter param in targetAnimator.parameters)
        {
            if (param.name == openParameterName && param.type == AnimatorControllerParameterType.Bool)
            {
                paramExists = true;
                break;
            }
        }

        if (!paramExists)
        {
            Debug.LogError($"DrawerController: {targetAnimator.name} Animator'ünde '{openParameterName}' adında bir boolean parametre bulunamadı.", this);
            return;
        }
        
        bool currentState = targetAnimator.GetBool(openParameterName);
        targetAnimator.SetBool(openParameterName, !currentState);

        /* // Bu blok birden fazla çekmecenin aynı anda açık kalabilmesi için yorum satırı yapıldı.
        if (!currentState) // Eğer kompartıman AÇILIYORSA (önceki durumu false idi, şimdi true olacak)
        {
            for (int i = 0; i < compartmentAnimators.Length; i++)
            {
                if (i != compartmentIndex && compartmentAnimators[i] != null)
                {
                    compartmentAnimators[i].SetBool(openParameterName, false);
                }
            }
        }
        */

        if (Debug.isDebugBuild)
        {
            Debug.Log($"Kompartıman {compartmentIndex + 1} ({targetAnimator.name} Animator'ü, parametre: {openParameterName}) durumu değiştirildi: {!currentState}");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (worldSpaceCanvases != null)
            {
                foreach (GameObject canvasGO in worldSpaceCanvases)
                {
                    if (canvasGO != null)
                    {
                        canvasGO.SetActive(true);
                    }
                }
                if (Debug.isDebugBuild)
                {
                    Debug.Log("Oyuncu çekmece alanına girdi, tüm Canvas'lar aktif.", this);
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (worldSpaceCanvases != null)
            {
                foreach (GameObject canvasGO in worldSpaceCanvases)
                {
                    if (canvasGO != null)
                    {
                        canvasGO.SetActive(false);
                    }
                }
                if (Debug.isDebugBuild)
                {
                    Debug.Log("Oyuncu çekmece alanından çıktı, tüm Canvas'lar pasif.", this);
                }
            }
        }
    }
} 