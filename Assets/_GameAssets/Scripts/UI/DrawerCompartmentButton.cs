using UnityEngine;
using UnityEngine.UI; // Button sınıfı için gerekli

[RequireComponent(typeof(Button))] // Bu scriptin bir Button bileşeni olan objeye eklenmesini zorunlu kılar
public class DrawerCompartmentButton : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Bu butonun kontrol edeceği kompartıman indeksi (0, 1, veya 2). " +
             "DrawerController'daki 'compartmentOpenParams' dizisiyle eşleşmelidir.")]
    [SerializeField] private int compartmentIndex = 0;

    [Header("Referanslar")]
    [Tooltip("Sahnedeki DrawerController scriptine sahip obje.")]
    [SerializeField] private DrawerController drawerController;

    private Button uiButton;

    void Awake()
    {
        uiButton = GetComponent<Button>(); // Button bileşenini al
        
        if (drawerController == null)
        {
            // DrawerController referansı Inspector'dan atanmamışsa, sahnede bulmayı dene.
            // Genellikle en iyi pratik Inspector'dan manuel olarak atamaktır.
            drawerController = FindObjectOfType<DrawerController>();
            if (drawerController == null)
            {
                Debug.LogError("DrawerCompartmentButton: DrawerController referansı atanmamış ve sahnede bulunamadı! Lütfen Inspector'dan atayın.", this);
                return; // drawerController olmadan devam etme
            }
            else
            {
                 Debug.LogWarning("DrawerCompartmentButton: DrawerController referansı Inspector'dan atanmamış, sahnede otomatik olarak bulundu. Manuel atama tercih edilir.", this);
            }
        }
    }
    
    void Start()
    {
        // Butonun onClick event'ine kendi metodumuzu ekle
        if (uiButton != null && drawerController != null) // Null check sonrası
        {
            uiButton.onClick.AddListener(OnButtonClick);
        }
        else if (uiButton == null)
        {
             Debug.LogError("DrawerCompartmentButton: Button bileşeni bulunamadı!", this);
        }
    }

    private void OnButtonClick()
    {
        if (drawerController != null)
        {
            drawerController.ToggleCompartmentState(compartmentIndex);
        }
        else
        {
            // Bu durum Awake'de zaten kontrol edilmiş olmalı ama emin olmak için.
            Debug.LogError("DrawerCompartmentButton: DrawerController referansı atanmamış!", this);
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