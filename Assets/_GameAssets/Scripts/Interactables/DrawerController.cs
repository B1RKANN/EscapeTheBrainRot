using UnityEngine;

[RequireComponent(typeof(Collider))] // Bu scriptin bir Collider'a sahip olmasını zorunlu kıl
public class DrawerController : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Her bir çekmece gözünün Animator bileşenlerini içeren dizi.")]
    [SerializeField] private Animator[] compartmentAnimators;

    [Tooltip("Her bir kompartıman Animator'ünde kullanılacak boolean parametresinin adı (örn: IsOpen)")]
    [SerializeField] private string openParameterName = "IsOpen";

    private int _keyCompartmentIndex = -1; // Eğer bu dolap anahtarı tutuyorsa, anahtarın olduğu çekmecenin indeksi
    private GameObject _takeKeyButtonInstance;
    private GameObject _keyObjectInSceneInstance;
    private GameObject _playerKeyObjectInstance;

    private bool _isKeyTaken = false; 
    private bool _isPlayerInTrigger = false; 

    public bool HasKey { get; private set; } = false;

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

        // Butonun başlangıç durumu KeyPlacementManager ve UpdateTakeKeyButtonVisibility tarafından yönetilecek.
    }

    public int GetCompartmentCount()
    {
        return compartmentAnimators != null ? compartmentAnimators.Length : 0;
    }

    /// <summary>
    /// Belirtilen indeksteki kompartımanın (çekmecenin) Transformunu döndürür.
    /// </summary>
    public Transform GetCompartmentTransform(int compartmentIndex)
    {
        if (compartmentAnimators != null && compartmentIndex >= 0 && compartmentIndex < compartmentAnimators.Length && compartmentAnimators[compartmentIndex] != null)
        {
            return compartmentAnimators[compartmentIndex].transform;
        }
        Debug.LogWarning($"GetCompartmentTransform: Geçersiz indeks ({compartmentIndex}) veya atanmamış Animator.", this);
        return null;
    }

    public void PrepareForNewKeyState(bool isHolder, int keyCompartmentIdx = -1, GameObject keyInSceneRef = null, GameObject takeKeyBtnRef = null, GameObject playerKeyObjRef = null)
    {
        HasKey = isHolder;
        _isKeyTaken = false; // Her yeni durumda anahtar alınmamış sayılır.

        if (isHolder)
        {
            _keyCompartmentIndex = keyCompartmentIdx;
            _keyObjectInSceneInstance = keyInSceneRef;
            _takeKeyButtonInstance = takeKeyBtnRef;
            _playerKeyObjectInstance = playerKeyObjRef;

            // _keyObjectInSceneInstance'ın başlangıçta aktif olması KeyPlacementManager tarafından sağlanacak.
            // _takeKeyButtonInstance'ın başlangıçta pasif olması KeyPlacementManager tarafından sağlanacak.
        }
        else
        {
            _keyCompartmentIndex = -1;
            // Referansları null yapmak isteğe bağlı, HasKey kontrolü yeterli olabilir.
            // _keyObjectInSceneInstance = null;
            // _takeKeyButtonInstance = null;
            // _playerKeyObjectInstance = null;
        }
        UpdateTakeKeyButtonVisibility(); // Durum değiştiğinde buton görünürlüğünü hemen güncelle.
    }
    
    /// <summary>
    /// Belirtilen indeksteki kompartımanın açık/kapalı durumunu değiştirir.
    /// </summary>
    /// <param name="compartmentIndex">Değiştirilecek kompartımanın indeksi (compartmentAnimators dizisiyle eşleşir).</param>
    public void ToggleCompartmentState(int compartmentIndex)
    {
        if (compartmentAnimators == null || compartmentAnimators.Length == 0 || compartmentAnimators[compartmentIndex] == null)
        {
            Debug.LogError($"[DrawerController] Toggle için compartmentAnimators uygun şekilde ayarlanmamış: {gameObject.name}", this);
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
        
        bool previousState = targetAnimator.GetBool(openParameterName);
        targetAnimator.SetBool(openParameterName, !previousState);
        bool newState = targetAnimator.GetBool(openParameterName); // SetBool sonrası hemen oku
        Debug.Log($"[DrawerController] ToggleCompartmentState on {gameObject.name} for compartment {compartmentIndex} ({targetAnimator.name}): Parameter '{openParameterName}' was {previousState}, set to {!previousState}. Current GetBool after set: {newState}", this);

        UpdateTakeKeyButtonVisibility();

        if (Debug.isDebugBuild)
        {
            Debug.Log($"Kompartıman {compartmentIndex + 1} ({targetAnimator.name} Animator'ü, parametre: {openParameterName}) durumu değiştirildi: {!previousState}");
            // Debug logları HasKey durumuna göre güncellenebilir.
            if (HasKey && _takeKeyButtonInstance != null && _takeKeyButtonInstance.activeSelf)
            {
                Debug.Log($"'Take Key' butonu şimdi aktif. Açılan çekmece: {compartmentIndex}, Doğru çekmece: {_keyCompartmentIndex}");
            }
            else if (HasKey && _takeKeyButtonInstance != null)
            {
                Debug.Log($"'Take Key' butonu şimdi pasif. Açılan çekmece: {compartmentIndex}, Doğru çekmece: {_keyCompartmentIndex}, Anahtar alındı mı: {_isKeyTaken}, Oyuncu triggerda mı: {_isPlayerInTrigger}");
            }
        }
    }

    private void UpdateTakeKeyButtonVisibility()
    {
        if (_takeKeyButtonInstance == null)
        {
            // Bu durum KeyPlacementManager'da takeKeyButton atanmamışsa veya bir hata oluşmuşsa meydana gelebilir.
            Debug.LogWarning($"[DrawerController] UpdateTakeKeyButtonVisibility: TakeKeyButton referansı (_takeKeyButtonInstance) {gameObject.name} için null. Buton görünürlüğü güncellenemiyor.", this);
            return;
        }

        // Temel durumları loglayalım (oyuncunun trigger'da olup olmadığı hariç, onu önce kontrol edeceğiz)
        // Debug.Log($"[DrawerController] UpdateTakeKeyButtonVisibility çağrıldı: {gameObject.name} | HasKey: {HasKey} | _isKeyTaken: {_isKeyTaken} | _keyCompartmentIndex: {_keyCompartmentIndex}", this);


        // 1. Oyuncu etkileşim alanında değilse, diğer koşullara bakmaksızın butonu pasif yap.
        if (!_isPlayerInTrigger)
        {
            _takeKeyButtonInstance.SetActive(false);
            Debug.Log($"[DrawerController] Buton ({_takeKeyButtonInstance.name}) {gameObject.name} için PASİF ayarlandı (Sebep: Oyuncu trigger alanında değil: _isPlayerInTrigger = {_isPlayerInTrigger})", _takeKeyButtonInstance);
            return;
        }

        // Oyuncu trigger'daysa, diğer koşulları kontrol et.
        Debug.Log($"[DrawerController] UpdateTakeKeyButtonVisibility çağrıldı (Oyuncu Trigger'da): {gameObject.name} | HasKey: {HasKey} | _isKeyTaken: {_isKeyTaken} | _keyCompartmentIndex: {_keyCompartmentIndex}", this);

        // 2. Bu dolapta anahtar yoksa butonu pasif yap.
        if (!HasKey) 
        {
            _takeKeyButtonInstance.SetActive(false);
            Debug.Log($"[DrawerController] Buton ({_takeKeyButtonInstance.name}) {gameObject.name} için PASİF ayarlandı (Sebep: HasKey = false)", _takeKeyButtonInstance);
            return;
        }

        // 3. Anahtar zaten alınmışsa butonu pasif yap.
        if (_isKeyTaken) 
        {
            _takeKeyButtonInstance.SetActive(false);
            Debug.Log($"[DrawerController] Buton ({_takeKeyButtonInstance.name}) {gameObject.name} için PASİF ayarlandı (Sebep: _isKeyTaken = true)", _takeKeyButtonInstance);
            return;
        }

        // 4. Doğru çekmecenin açık olup olmadığını kontrol et.
        bool correctDrawerIsOpen = false;
        if (_keyCompartmentIndex >= 0 && _keyCompartmentIndex < compartmentAnimators.Length && compartmentAnimators[_keyCompartmentIndex] != null)
        {
            correctDrawerIsOpen = compartmentAnimators[_keyCompartmentIndex].GetBool(openParameterName);
            Debug.Log($"[DrawerController] {gameObject.name} | Doğru çekmece kontrolü: İndeks: {_keyCompartmentIndex}, Animator: {compartmentAnimators[_keyCompartmentIndex].name}, '{openParameterName}' parametre değeri: {correctDrawerIsOpen}", this);
        }
        else
        {
            // Bu durum genellikle _keyCompartmentIndex'in hatalı atanması veya compartmentAnimators dizisinin eksik/hatalı olması durumunda oluşur.
            Debug.LogWarning($"[DrawerController] {gameObject.name} | _keyCompartmentIndex ({_keyCompartmentIndex}) geçersiz veya ilgili compartmentAnimator null. Doğru çekmecenin açık olup olmadığı kontrol edilemiyor. Buton pasif kalacak.", this);
            _takeKeyButtonInstance.SetActive(false); // Kontrol edilemiyorsa butonu pasif tutmak güvenlidir.
            return;
        }
        
        // Tüm ön koşullar sağlandı (Oyuncu trigger'da, bu dolapta anahtar var, anahtar alınmamış).
        // Butonun aktif olup olmayacağı artık sadece doğru çekmecenin açık olup olmadığına bağlı.
        _takeKeyButtonInstance.SetActive(correctDrawerIsOpen);

        Debug.Log($"[DrawerController] {gameObject.name} | Buton ({_takeKeyButtonInstance.name}) durumu ayarlandı: {correctDrawerIsOpen} (Koşullar: _isPlayerInTrigger: {_isPlayerInTrigger}, HasKey: {HasKey}, !_isKeyTaken: {!_isKeyTaken}, correctDrawerIsOpen: {correctDrawerIsOpen})", _takeKeyButtonInstance);
    }

    /// <summary>
    /// 'Take Key' butonuna basıldığında çağrılır.
    /// Anahtarı sahneden kaldırır, oyuncuya verir ve butonu deaktif eder.
    /// </summary>
    public void TakeKey()
    {
        if (!HasKey || _isKeyTaken)
        {
            Debug.LogWarning("Anahtar bu çekmecede değil veya zaten alınmış.", this);
            return;
        }

        if (_keyObjectInSceneInstance != null)
        {
            _keyObjectInSceneInstance.SetActive(false);
            Debug.Log("Sahnedeki anahtar objesi deaktif edildi.", this);
        }
        else
        {
            Debug.LogWarning("Sahnedeki anahtar objesi (keyObjectInSceneInstance) atanmamış, kaldırılamadı.", this);
        }

        if (_playerKeyObjectInstance != null)
        {
            _playerKeyObjectInstance.SetActive(true);
            Debug.Log("Oyuncunun anahtar objesi aktif edildi.", this);
        }
        else
        {
            Debug.LogError("Oyuncunun anahtar objesi (playerKeyObjectInstance) atanmamış, aktif edilemedi!", this);
        }

        if (_takeKeyButtonInstance != null)
        {
            _takeKeyButtonInstance.SetActive(false);
        }

        _isKeyTaken = true;
        // HasKey = false; // Anahtar alındıktan sonra bu dolap artık anahtarı tutmuyor.
                       // Bu satır önemli, böylece başka bir çekmece açıldığında bu dolap tekrar anahtar vermeye çalışmaz.
                       // Ancak, oyun yeniden başladığında KeyPlacementManager HasKey'i tekrar ayarlayacağı için,
                       // bu anlık olarak false yapmak şart değil, isKeyTaken yeterli.
                       // Şimdilik yoruma alıyorum, oyun mantığına göre gerekirse açılabilir.
        Debug.Log("Anahtar alındı ve _isKeyTaken = true olarak ayarlandı.", this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInTrigger = true;
            Debug.Log($"[DrawerController] OnTriggerEnter on {gameObject.name}: Player TAG MATCHED. _isPlayerInTrigger set to TRUE.", this);

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
                    Debug.Log($"[DrawerController] Oyuncu {gameObject.name} alanına girdi, tüm Canvas'lar aktif. _isPlayerInTrigger: {_isPlayerInTrigger}", this);
                }
            }
            UpdateTakeKeyButtonVisibility(); 
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInTrigger = false;
            Debug.Log($"[DrawerController] OnTriggerExit on {gameObject.name}: Player TAG MATCHED. _isPlayerInTrigger set to FALSE.", this);

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
                    Debug.Log($"[DrawerController] Oyuncu {gameObject.name} alanından çıktı, tüm Canvas'lar pasif. _isPlayerInTrigger: {_isPlayerInTrigger}", this);
                }
            }
            UpdateTakeKeyButtonVisibility();
        }
    }
} 