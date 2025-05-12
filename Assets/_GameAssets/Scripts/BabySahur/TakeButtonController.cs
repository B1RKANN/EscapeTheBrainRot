using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TakeButtonController : MonoBehaviour
{
    [Header("UI Bileşenleri")]
    [SerializeField] private Button takeButton;
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI buttonText;
    
    [Header("Animasyon Ayarları")]
    [SerializeField] private bool useButtonAnimation = true;
    [SerializeField] private float animationSpeed = 10f;
    [SerializeField] private float pulseAmount = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private BabySahurController currentBabySahur;
    private Vector3 originalScale;
    private bool isAnimating = false;

    private void Awake()
    {
        // Buton referansını otomatik bul (null ise)
        if (takeButton == null)
        {
            takeButton = GetComponentInChildren<Button>(true);
            
            if (takeButton == null)
            {
                Debug.LogError("Take butonu bileşeni bulunamadı!");
            }
            else if (debugMode)
            {
                Debug.Log("Take butonu otomatik bulundu: " + takeButton.name);
            }
        }
        
        // Text referansını otomatik bul (null ise)
        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        
        // Image referansını otomatik bul (null ise)
        if (buttonImage == null && takeButton != null)
        {
            buttonImage = takeButton.GetComponent<Image>();
        }
        
        // Buton olayını ayarla
        if (takeButton != null)
        {
            takeButton.onClick.RemoveAllListeners();
            takeButton.onClick.AddListener(OnTakeButtonClick);
            if (debugMode)
            {
                Debug.Log("Take butonuna tıklama eventi eklendi");
            }
            
            // Animasyon için orijinal boyutu kaydet
            if (useButtonAnimation && takeButton.transform != null)
            {
                originalScale = takeButton.transform.localScale;
            }
        }
    }

    private void Start()
    {
        // Başlangıçta butonu gizli yap
        gameObject.SetActive(false);
        
        if (debugMode)
        {
            Debug.Log("TakeButtonController başlatıldı, buton pasif");
        }
    }
    
    private void Update()
    {
        // Buton animasyonu
        if (useButtonAnimation && isAnimating && takeButton != null)
        {
            float pulse = 1 + Mathf.Sin(Time.time * animationSpeed) * pulseAmount;
            takeButton.transform.localScale = originalScale * pulse;
        }
    }

    // Bu metodu BabySahurController'dan çağıracağız
    public void SetCurrentBabySahur(BabySahurController babySahur)
    {
        if (babySahur == null)
        {
            Debug.LogError("TakeButtonController'a null BabySahur gönderildi!");
            return;
        }
        
        currentBabySahur = babySahur;
        
        // Butonu görünür yap
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            isAnimating = true;
        }
        
        if (debugMode)
        {
            Debug.Log("Mevcut BabySahur ayarlandı: " + babySahur.name + " (" + babySahur.GetInstanceID() + ")");
        }
    }

    private void OnTakeButtonClick()
    {
        if (debugMode)
        {
            Debug.Log("Take butonuna tıklandı!");
        }
        
        if (currentBabySahur != null)
        {
            BabySahurController babySahurToPickup = currentBabySahur;
            
            if (debugMode)
            {
                Debug.Log("Tıklama - BabySahur bulundu: " + babySahurToPickup.name);
            }
            
            // İşlem öncesi animasyonu durdur
            isAnimating = false;
            
            // Önce referansı yerel değişkene kopyala, sonra PickupBabySahur'u çağır
            // Böylece Destroy işlemi sırasında referans kaybı yaşanmaz
            babySahurToPickup.PickupBabySahur();
            
            // Temizliği sonra yap (Destroy'dan sonra olduğu için güvenli)
            currentBabySahur = null;
        }
        else
        {
            Debug.LogWarning("Tıklama sırasında currentBabySahur null!");
            
            // Butonu kapat (null ref hatası olduğundan)
            gameObject.SetActive(false);
        }
    }
    
    // Buton her aktif olduğunda çağrılıyor
    private void OnEnable()
    {
        if (debugMode)
        {
            Debug.Log("TakeButton aktifleşti. currentBabySahur: " + (currentBabySahur != null ? currentBabySahur.name : "null"));
        }
        
        // Butonu orijinal boyutuna sıfırla
        if (useButtonAnimation && takeButton != null)
        {
            takeButton.transform.localScale = originalScale;
            isAnimating = true;
        }
        
        // Eğer currentBabySahur null ise butonu otomatik gizle
        if (currentBabySahur == null)
        {
            if (debugMode)
            {
                Debug.LogWarning("Buton aktif ama currentBabySahur null! Butonu otomatik gizliyorum.");
            }
            gameObject.SetActive(false);
        }
    }
    
    // Buton deaktif olduğunda
    private void OnDisable()
    {
        isAnimating = false;
        
        // Butonu orijinal boyutuna sıfırla
        if (useButtonAnimation && takeButton != null)
        {
            takeButton.transform.localScale = originalScale;
        }
    }
} 