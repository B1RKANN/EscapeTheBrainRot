using UnityEngine;
using UnityEngine.UI; // Sadece "Take Button" GameObject referansı için kalabilir, Text için değil.
using TMPro; // TextMeshPro için eklendi
using System.Collections;

public class CrosshairInteraction : MonoBehaviour
{
    [Header("Genel Ayarlar")]
    [Tooltip("Oyuncunun nesnelerle etkileşime girebileceği maksimum mesafe")]
    public float interactionDistance = 3f;

    [Tooltip("Etkileşime girilebilecek nesnelerin bulunduğu katman (Sopa, Çekmece Gözü vb.)")]
    public LayerMask interactableLayer;

    [Tooltip("Genel etkileşim bilgilerini gösteren UI Text elemanı (TextMeshPro)")]
    public TextMeshProUGUI interactPromptText; 

    [Header("Sopa Etkileşimi")]
    [Tooltip("Alınabilir sopa nesnelerinin sahip olacağı etiket")]
    public string stickInteractTag = "Sopa"; 
    [Tooltip("Sopayı almak için kullanılacak UI Butonu GameObject'i")]
    public GameObject takeStickButton; // Eski 'takeButton' ismini daha spesifik hale getirdim

    [Header("Çekmece Etkileşimi")]
    [Tooltip("Çekmece gözlerinin sahip olacağı etiket")]
    public string drawerCompartmentTag = "DrawerCompartment";
    [Tooltip("Çekmeceyi açıp kapatmak için kullanılacak UI Butonu GameObject'i")]
    public GameObject drawerInteractButton;

    [Header("Sopa Fırlatma")]
    [Tooltip("Oyuncunun elindeki, kameraya bağlı olan sopa objesi")]
    public GameObject baseballBatInHand;
    [Tooltip("Sopayı fırlatmak için kullanılacak UI Butonu")]
    public GameObject pushButton;
    [Tooltip("Fırlatılacak sopa prefab'ı (Rigidbody'li olmalı)")]
    public GameObject throwableBatPrefab;
    [Tooltip("Sopanın fırlatılma kuvveti")]
    public float throwForce = 20f;
    [Tooltip("Sopanın fırlatıldıktan sonra ele geri dönme süresi (saniye)")]
    public float batReturnCooldown = 3.0f;

    private Camera mainCamera;
    private GameObject _currentLookedAtStick = null;
    private DrawerCompartmentIdentifier _currentlyLookedAtDrawerCompartment = null;

    void Start()
    {
        Debug.Log("CrosshairInteraction Start() çağrıldı.");
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("CrosshairInteraction: Ana kamera bulunamadı! Lütfen kameranızın 'MainCamera' olarak etiketlendiğinden emin olun.");
            enabled = false; 
            return;
        }

        // Başlangıçta tüm UI elemanlarını gizle
        SetGameObjectActive(takeStickButton, false, "'Take Stick Button' (Start)");
        SetGameObjectActive(drawerInteractButton, false, "'Drawer Interact Button' (Start)");
        SetGameObjectActive(interactPromptText != null ? interactPromptText.gameObject : null, false, "'Interact Prompt Text' (Start)");
        
        // Yeni eklenen UI elemanlarını da başlangıçta gizle
        SetGameObjectActive(baseballBatInHand, false, "'Baseball Bat In Hand' (Start)");
        SetGameObjectActive(pushButton, false, "'Push Button' (Start)");

        Debug.Log("Başlangıç UI durumları ayarlandı.");
    }

    void Update()
    {
        // Debug.Log("CrosshairInteraction Update - Frame: " + Time.frameCount); // Her frame loglamak çok fazla olabilir, gerekirse açın
        ResetInteractionStates();

        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            Debug.Log("[Raycast HIT] Nesne: " + hit.collider.name + " | Tag: " + hit.collider.tag + " | Layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
            GameObject hitObject = hit.collider.gameObject;
            string hitTag = hit.collider.tag;

            if (hitTag == stickInteractTag)
            {
                HandleStickInteraction(hitObject);
            }
            else if (hitTag == drawerCompartmentTag)
            {
                HandleDrawerCompartmentInteraction(hitObject);
            }
        }
        // else
        // {
            // Debug.Log("[Raycast MISS] Hiçbir şeye çarpmadı."); // Gerekirse açın
        // }

        // Sopa elindeyse Fırlat (Push) butonunu göster
        if (baseballBatInHand != null)
        {
            SetGameObjectActive(pushButton, baseballBatInHand.activeSelf, null); // Sadece durum değişirse aktif/pasif yapar
        }
    }

    private void ResetInteractionStates()
    {
        _currentLookedAtStick = null;
        _currentlyLookedAtDrawerCompartment = null;

        SetGameObjectActive(takeStickButton, false);
        SetGameObjectActive(drawerInteractButton, false);
        SetGameObjectActive(interactPromptText != null ? interactPromptText.gameObject : null, false);
    }

    private void HandleStickInteraction(GameObject stickObject)
    {
        Debug.Log("HandleStickInteraction çağrıldı: " + stickObject.name);
        _currentLookedAtStick = stickObject;
        SetGameObjectActive(takeStickButton, true, "'Take Stick Button' (HandleStick)");
        SetGameObjectActive(drawerInteractButton, false); // Diğer butonu gizle

        if (interactPromptText != null)
        {
            interactPromptText.text = "Sopa";
            SetGameObjectActive(interactPromptText.gameObject, true, "'Interact Prompt Text' (HandleStick)");
        }
    }

    private void HandleDrawerCompartmentInteraction(GameObject compartmentObject)
    {
        Debug.Log("HandleDrawerCompartmentInteraction çağrıldı: " + compartmentObject.name);
        DrawerCompartmentIdentifier identifier = compartmentObject.GetComponent<DrawerCompartmentIdentifier>();
        if (identifier != null)
        {
            _currentlyLookedAtDrawerCompartment = identifier;
            Debug.Log("DrawerIdentifier bulundu: İndeks " + identifier.compartmentIndex + ", Controller: " + (identifier.drawerController != null ? identifier.drawerController.name : "NULL CONTROLLER"));
            SetGameObjectActive(drawerInteractButton, true, "'Drawer Interact Button' (HandleDrawer)");
            SetGameObjectActive(takeStickButton, false); // Diğer butonu gizle
            
            if (interactPromptText != null)
            {
                // Butonun üzerinde zaten "Aç/Kapat" yazıyorsa, burası sadece "Çekmece Gözü" gibi bir bilgi verebilir.
                // Veya çekmecenin açık/kapalı durumuna göre metni dinamik yapabilirsiniz.
                // Örneğin:
                // bool isOpen = identifier.drawerController.IsCompartmentOpen(identifier.compartmentIndex); 
                // interactPromptText.text = isOpen ? "Kapat" : "Aç";
                // Şimdilik genel bir metin kullanalım:
                interactPromptText.text = "Çekmece Gözü";
                SetGameObjectActive(interactPromptText.gameObject, true, "'Interact Prompt Text' (HandleDrawer)");
            }
        }
        else
        {
            Debug.LogError("DrawerCompartmentIdentifier BULUNAMADI: " + compartmentObject.name + " üzerinde!");
        }
    }

    /// <summary>
    /// Sopa almak için UI butonundan çağrılır.
    /// </summary>
    public void OnStickTakeButtonPressed()
    {
        // GÜVENLİK KONTROLÜ: Fonksiyonun sadece buton görünür olduğunda çalışmasını sağla.
        // Bu, oyun başlangıcında veya başka bir zamanda yanlışlıkla çağrılmasını engeller.
        if (takeStickButton == null || !takeStickButton.activeInHierarchy)
        {
            return;
        }

        Debug.Log("OnStickTakeButtonPressed ÇAĞRILDI. _currentLookedAtStick: " + (_currentLookedAtStick != null ? _currentLookedAtStick.name : "NULL"));
        if (_currentLookedAtStick != null)
        {
            Debug.Log(_currentLookedAtStick.name + " alındı ve yok edildi!");

            // Yerdeki sopayı tamamen yok et
            Destroy(_currentLookedAtStick);

            // Eldeki sopayı aktif et
            SetGameObjectActive(baseballBatInHand, true, "'Baseball Bat In Hand' (OnStickTakeButtonPressed)");

            // Etkileşim sonrası UI'ı temizle
            _currentLookedAtStick = null; // Artık bakılan bir sopa yok
            ResetInteractionStates();
        }
    }

    /// <summary>
    /// Çekmece gözünü açıp kapatmak için UI butonundan çağrılır.
    /// </summary>
    public void OnDrawerInteractButtonPressed()
    {
        Debug.Log("OnDrawerInteractButtonPressed ÇAĞRILDI. _currentlyLookedAtDrawerCompartment: " + (_currentlyLookedAtDrawerCompartment != null ? _currentlyLookedAtDrawerCompartment.gameObject.name : "NULL"));
        if (_currentlyLookedAtDrawerCompartment != null && _currentlyLookedAtDrawerCompartment.drawerController != null)
        {
            Debug.Log("ToggleCompartmentState çağrılıyor: " + _currentlyLookedAtDrawerCompartment.drawerController.name + ", İndeks: " + _currentlyLookedAtDrawerCompartment.compartmentIndex);
            _currentlyLookedAtDrawerCompartment.drawerController.ToggleCompartmentState(_currentlyLookedAtDrawerCompartment.compartmentIndex);
            // Etkileşim sonrası UI'ı hemen güncellemek yerine Update'in bir sonraki frame'de halletmesi beklenebilir.
            // Veya spesifik olarak butonları gizleyebilirsiniz:
            // SetGameObjectActive(drawerInteractButton, false); 
            // SetGameObjectActive(interactPromptText.gameObject, false); 
            // ResetInteractionStates(); // Eğer etkileşim sonrası crosshair başka bir şeye bakmıyorsa temizler.
        }
        else
        {
             Debug.LogError("Çekmece veya Controller NULL OnDrawerInteractButtonPressed içinde!");
        }
    }

    /// <summary>
    /// Sopayı fırlatmak için UI butonundan çağrılır.
    /// </summary>
    public void OnPushButtonPressed()
    {
        if (mainCamera == null || throwableBatPrefab == null || baseballBatInHand == null)
        {
            Debug.LogError("Fırlatma için Ana Kamera, Fırlatılabilir Sopa Prefab'ı veya Eldeki Sopa atanmamış!");
            return;
        }

        // Sadece eldeki sopa aktifse fırlatmaya izin ver
        if (!baseballBatInHand.activeSelf)
        {
            Debug.LogWarning("Sopa elde değil, fırlatılamaz.");
            return;
        }

        Debug.Log("OnPushButtonPressed ÇAĞRILDI.");

        // 1. Eldeki sopayı gizle
        SetGameObjectActive(baseballBatInHand, false, "'Baseball Bat In Hand' (OnPushButtonPressed)");

        // 2. Fırlatılabilir sopa prefab'ını crosshair yönünde oluştur
        Vector3 spawnPosition = mainCamera.transform.position + mainCamera.transform.forward * 1.0f; // Kameranın biraz önünde
        GameObject thrownBat = Instantiate(throwableBatPrefab, spawnPosition, mainCamera.transform.rotation);

        // Fırlatılan objenin kopyası da inaktif olabileceğinden, onu burada zorla aktif ediyoruz.
        thrownBat.SetActive(true);
        Debug.Log($"Fırlatılan sopa ({thrownBat.name}) oluşturuldu ve aktif edildi.", thrownBat);

        // 3. Fırlatma kuvvetini uygula
        Rigidbody rb = thrownBat.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Fırlatılan sopanın fizik kurallarından etkilenebilmesi için 'kinematic' olmaması gerekir.
            rb.isKinematic = false;
            // Yerçekimini devre dışı bırakıyoruz.
            rb.useGravity = false;
            rb.AddForce(mainCamera.transform.forward * throwForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogError("Fırlatılabilir sopa prefab'ında Rigidbody bileşeni bulunamadı!");
            Destroy(thrownBat); // Hatalı objeyi temizle
            // Eldeki sopayı geri getir, çünkü fırlatma başarısız oldu
            SetGameObjectActive(baseballBatInHand, true);
            return;
        }
    }

    /// <summary>
    /// Bir GameObject'i güvenli bir şekilde aktif/pasif yapar ve atanmamışsa uyarı verir.
    /// </summary>
    private void SetGameObjectActive(GameObject go, bool isActive, string gameObjectNameForWarning = null)
    {
        if (go != null)
        {
            if (go.activeSelf != isActive) // Sadece durumu değişiyorsa SetActive çağır
            {
                go.SetActive(isActive);
                // Debug.Log((gameObjectNameForWarning ?? go.name) + " durumu " + isActive + " olarak ayarlandı."); // Gerekirse açın
            }
        }
        else if (isActive && gameObjectNameForWarning != null) // Sadece aktif edilmeye çalışılırken ve isim verilmişse uyarı ver
        {
            Debug.LogWarning($"CrosshairInteraction: '{gameObjectNameForWarning}' atanmamış. Aktif edilemiyor.");
        }
    }
} 