using UnityEngine;
using UnityEngine.UI; // Button sınıfı için gerekli

[RequireComponent(typeof(Button))] // Bu scriptin bir Button bileşeni olan objeye eklenmesini zorunlu kılar
public class DrawerCompartmentButton : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Bu butonun kontrol edeceği kompartıman indeksi (0, 1, veya 2). " +
             "DrawerController'daki 'compartmentAnimators' dizisiyle eşleşmelidir.")]
    [SerializeField] private int compartmentIndex = 0;

    [Header("Referanslar")]
    [Tooltip("Bu butonun ait olduğu çekmecenin DrawerController scriptine sahip obje. MANUEL ATANMALIDIR!")]
    [SerializeField] private DrawerController drawerController;

    private Button uiButton;

    void Awake()
    {
        uiButton = GetComponent<Button>(); // Button bileşenini al
        
        if (drawerController == null)
        {
            // DrawerController referansı Inspector'dan atanmamışsa, bu kritik bir hatadır.
            // FindObjectOfType kullanımı birden fazla çekmece olduğunda yanlış DrawerController'ı bulabilir.
            // Bu yüzden manuel atama zorunludur.
            Debug.LogError($"DrawerCompartmentButton ({gameObject.name}): DrawerController referansı atanmamış! Lütfen Inspector üzerinden bu butona doğru DrawerController'ı (ait olduğu çekmecenin controller'ını) manuel olarak atayın. Çoğaltılmış çekmecelerde her buton kendi DrawerController'ına bağlanmalıdır.", this);
            
            // İsteğe bağlı: Butonu devre dışı bırakarak daha fazla karışıklığı önleyebilirsiniz.
            // if (uiButton != null) uiButton.interactable = false;
            return; 
        }
    }
    
    void Start()
    {
        // drawerController null değilse (Awake'de kontrol edildi) ve uiButton varsa listener ekle
        if (uiButton != null && drawerController != null) 
        {
            uiButton.onClick.AddListener(OnButtonClick);
        }
        else if (uiButton == null)
        {
             Debug.LogError($"DrawerCompartmentButton ({gameObject.name}): Button bileşeni bulunamadı!", this);
        }
        // drawerController null ise Awake'de zaten hata verildi ve return yapıldı.
    }

    private void OnButtonClick()
    {
        if (drawerController != null) // Bu kontrol Start'tan sonra yine de iyi bir pratiktir.
        {
            drawerController.ToggleCompartmentState(compartmentIndex);
        }
        else
        {
            // Bu durumun normalde Awake'de yakalanması gerekir.
            Debug.LogError($"DrawerCompartmentButton ({gameObject.name}): OnButtonClick çağrıldı ancak DrawerController referansı hala null!", this);
        }
    }

    // Obje yok edildiğinde listener'ı temizlemek iyi bir pratiktir
    void OnDestroy()
    {
        if (uiButton != null)
        {
            uiButton.onClick.RemoveListener(OnButtonClick);
        }
    }
} 