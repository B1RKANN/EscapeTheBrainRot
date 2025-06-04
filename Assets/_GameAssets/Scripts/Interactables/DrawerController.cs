using UnityEngine;

[RequireComponent(typeof(Collider))] // Bu scriptin bir Collider'a sahip olmasını zorunlu kıl
public class DrawerController : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Her bir çekmece gözünün Animator bileşenlerini içeren dizi.")]
    [SerializeField] private Animator[] compartmentAnimators;

    [Tooltip("Her bir kompartıman Animator'ünde kullanılacak boolean parametresinin adı (örn: IsOpen)")]
    [SerializeField] private string openParameterName = "IsOpen";

    [Header("Anahtar Etkileşimi")] // Yeni başlık
    [Tooltip("Anahtarın bulunduğu doğru çekmecenin compartmentAnimators dizisindeki indeksi.")]
    [SerializeField] private int correctCompartmentIndex = 0; // Varsayılan olarak ilk çekmece

    [Tooltip("Oyuncunun anahtarı alabileceği 'Take Key' UI Butonu.")]
    [SerializeField] private GameObject takeKeyButton;

    [Tooltip("Sahnedeki alınabilir anahtar objesi.")]
    [SerializeField] private GameObject keyObjectInScene;

    [Tooltip("Oyuncunun kamerasının altında/parent'ında bulunan ve anahtar alındığında aktif edilecek olan anahtar objesi.")]
    [SerializeField] private GameObject playerKeyObject;

    private bool isKeyTaken = false; // Anahtarın alınıp alınmadığını takip eder
    private bool isPlayerInTrigger = false; // Oyuncunun trigger alanında olup olmadığını takip eder

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

        // Anahtar ile ilgili başlangıç ayarları
        if (takeKeyButton != null)
        {
            takeKeyButton.SetActive(false); // Butonu başlangıçta pasif yap
        }
        else
        {
            Debug.LogError("DrawerController: 'Take Key' butonu (takeKeyButton) atanmamış!", this);
        }

        if (keyObjectInScene == null)
        {
            Debug.LogWarning("DrawerController: Sahnedeki anahtar objesi (keyObjectInScene) atanmamış. Anahtar mekaniği düzgün çalışmayabilir.", this);
        }

        if (playerKeyObject != null)
        {
            playerKeyObject.SetActive(false); // Oyuncudaki anahtarı başlangıçta pasif yap
        }
        else
        {
            Debug.LogError("DrawerController: Oyuncunun anahtar objesi (playerKeyObject) atanmamış!", this);
        }

        if (correctCompartmentIndex < 0 || compartmentAnimators == null || correctCompartmentIndex >= compartmentAnimators.Length)
        {
            Debug.LogError($"DrawerController: Geçersiz 'correctCompartmentIndex' ({correctCompartmentIndex}). Lütfen 0 ile {compartmentAnimators?.Length - 1} arasında bir değer atayın.", this);
            // Oyunu durdurmak veya bir hata durumu yönetimi eklemek daha iyi olabilir.
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

        // Anahtar butonu görünürlüğünü güncelle
        UpdateTakeKeyButtonVisibility();

        if (Debug.isDebugBuild)
        {
            Debug.Log($"Kompartıman {compartmentIndex + 1} ({targetAnimator.name} Animator'ü, parametre: {openParameterName}) durumu değiştirildi: {!currentState}");
            if (takeKeyButton != null && takeKeyButton.activeSelf)
            {
                Debug.Log($"'Take Key' butonu şimdi aktif. Açılan çekmece: {compartmentIndex}, Doğru çekmece: {correctCompartmentIndex}");
            }
            else if (takeKeyButton != null)
            {
                Debug.Log($"'Take Key' butonu şimdi pasif. Açılan çekmece: {compartmentIndex}, Doğru çekmece: {correctCompartmentIndex}, Anahtar alındı mı: {isKeyTaken}");
            }
        }
    }

    private void UpdateTakeKeyButtonVisibility()
    {
        if (takeKeyButton == null)
        {
            return; // Buton atanmamışsa bir şey yapma
        }

        if (isKeyTaken) // Anahtar zaten alınmışsa butonu her zaman pasif yap
        {
            takeKeyButton.SetActive(false);
            return;
        }

        bool correctDrawerIsOpen = false;
        if (correctCompartmentIndex >= 0 && correctCompartmentIndex < compartmentAnimators.Length && compartmentAnimators[correctCompartmentIndex] != null)
        {
            correctDrawerIsOpen = compartmentAnimators[correctCompartmentIndex].GetBool(openParameterName);
        }
        else
        {
            // Hatalı correctCompartmentIndex durumu Start içinde zaten loglanıyor.
            // Burada ek bir loglama yapılabilir veya olduğu gibi bırakılabilir.
            // Debug.LogWarning($"UpdateTakeKeyButtonVisibility: correctCompartmentIndex ({correctCompartmentIndex}) geçersiz veya ilgili animator null.");
        }
        
        // Buton sadece doğru çekmece açıksa VE oyuncu trigger içindeyse aktif olmalı
        takeKeyButton.SetActive(correctDrawerIsOpen && isPlayerInTrigger);
    }

    /// <summary>
    /// 'Take Key' butonuna basıldığında çağrılır.
    /// Anahtarı sahneden kaldırır, oyuncuya verir ve butonu deaktif eder.
    /// </summary>
    public void TakeKey()
    {
        if (isKeyTaken)
        {
            Debug.LogWarning("Anahtar zaten alınmış.", this);
            return;
        }

        if (keyObjectInScene != null)
        {
            keyObjectInScene.SetActive(false);
            Debug.Log("Sahnedeki anahtar objesi deaktif edildi.", this);
        }
        else
        {
            Debug.LogWarning("Sahnedeki anahtar objesi (keyObjectInScene) atanmamış, kaldırılamadı.", this);
        }

        if (playerKeyObject != null)
        {
            playerKeyObject.SetActive(true);
            Debug.Log("Oyuncunun anahtar objesi aktif edildi.", this);
        }
        else
        {
            Debug.LogError("Oyuncunun anahtar objesi (playerKeyObject) atanmamış, aktif edilemedi!", this);
        }

        if (takeKeyButton != null)
        {
            takeKeyButton.SetActive(false);
        }

        isKeyTaken = true;
        Debug.Log("Anahtar alındı ve isKeyTaken = true olarak ayarlandı.", this);

        // Anahtar alındıktan sonra, doğru çekmece kapansa bile butonun tekrar aktif olmamasını garantilemek için
        // UpdateTakeKeyButtonVisibility çağrılabilir veya bu durum isKeyTaken ile zaten yönetiliyor.
        // Eğer doğru çekmece hala açıksa ve butonun hemen kaybolması isteniyorsa:
        // UpdateTakeKeyButtonVisibility(); // Bu satır isKeyTaken kontrolü nedeniyle butonu false yapacaktır.
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
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
            UpdateTakeKeyButtonVisibility(); // Oyuncu girdiğinde buton durumunu güncelle
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
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
            UpdateTakeKeyButtonVisibility(); // Oyuncu çıktığında buton durumunu güncelle
        }
    }
} 