using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// UI butonunun üzerine tıklandığında kapıyı açacak/kapatacak script
public class DoorButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Kapı Referansı")]
    // targetDoor referansı DoorTrigger tarafından atanacak
    private Door targetDoor;
    
    // [SerializeField] private bool autoFindDoor = false; // Bu özellik kafa karışıklığına neden olabileceğinden kaldırıldı veya false yapıldı.
    [SerializeField] private bool allowToggleDoor = true; // Bu hala geçerli, toggle işlemini kontrol eder.
    
    [Header("Buton Görünümü")]
    // Buton animasyonu için (isteğe bağlı)
    [SerializeField] private Animator buttonAnimator;
    [SerializeField] private string triggerParametreName = "Click"; // Animator'daki trigger parametre adı
    
    // Görsel geri bildirim için
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = Color.gray;
    [SerializeField] private Color openDoorColor = new Color(0.2f, 0.8f, 0.2f); // Kapı açıkken buton rengi
    
    // Kapının son durumunu takip etmek için targetDoor.isOpen kullanılacak
    // private bool isDoorOpen = false; // Kaldırıldı
    
    // Kapı referansını dışarıdan ayarlamak için public metod
    public void SetTargetDoor(Door doorRef)
    {
        targetDoor = doorRef;
        if (targetDoor != null)
        {
            // Debug.Log("DoorButton: Hedef kapı ayarlandı - " + doorRef.name);
            UpdateButtonVisuals(); // Kapı atandığında buton görünümünü güncelle
        }
        else
        {
            // Debug.LogWarning("DoorButton: Hedef kapı null olarak ayarlandı.");
        }
    }
    
    private void Awake()
    {
        // Otomatik olarak kapıyı bulma özelliği kaldırıldı veya devre dışı bırakıldı.
        // if (autoFindDoor && targetDoor == null)
        // {
        // FindNearestDoor();
        // }

        // Button Image referansını otomatik al
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }
        
        // Buton animatörünü al
        if (buttonAnimator == null)
        {
            buttonAnimator = GetComponent<Animator>();
        }
    }
    
    private void Start()
    {
        // Referansları kontrol et ve uyarı ver
        if (targetDoor == null)
        {
            // Bu uyarı DoorTrigger hedef kapıyı atayana kadar görünebilir, bu normaldir.
            // Debug.LogWarning("DoorButton: Başlangıçta targetDoor referansı yok! DoorTrigger tarafından atanması bekleniyor.");
        }
        
        // Animatör var mı kontrol et
        if (buttonAnimator != null)
        {
            // Trigger parametresinin varlığını kontrol et (sadece debug için)
            AnimatorControllerParameter[] parameters = buttonAnimator.parameters;
            bool triggerExists = false;
            
            foreach (var param in parameters)
            {
                if (param.name == triggerParametreName && param.type == AnimatorControllerParameterType.Trigger)
                {
                    triggerExists = true;
                    break;
                }
            }
            
            if (!triggerExists)
            {
                Debug.LogWarning($"DoorButton: '{triggerParametreName}' adında bir trigger parametresi animator'da bulunamadı! Lütfen Animator Controller'da bu isimde bir trigger oluşturun veya triggerParametreName değerini değiştirin.");
            }
        }
    }
    
    // En yakın kapıyı bulma özelliği gereksizleşti veya isteğe bağlı hale getirildi.
    /*
    private void FindNearestDoor()
    {
        Door[] allDoors = FindObjectsOfType<Door>();
        float closestDistance = float.MaxValue;
        Door foundDoor = null; // Yerel değişken olarak değiştirildi
        
        foreach (Door door in allDoors)
        {
            float distance = Vector3.Distance(transform.position, door.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                foundDoor = door;
            }
        }
        
        if (foundDoor != null)
        {
            targetDoor = foundDoor; // Sadece burada ata
            Debug.Log($"DoorButton: En yakın kapı otomatik bulundu - {targetDoor.name} (Mesafe: {closestDistance:F2} birim)");
        }
        else
        {
            Debug.LogWarning("DoorButton: FindNearestDoor çağrıldı ancak yakında bir kapı bulunamadı.");
        }
    }
    */
    
    // UI butonuna tıklama işlemi
    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetDoor == null)
        {
            Debug.LogError("DoorButton: targetDoor referansı yok! Buton tıklanmasına rağmen kapı bulunamadı. DoorTrigger'ı kontrol edin.");
            return;
        }
        
        // Buton basılma efekti (renk)
        if (buttonImage != null)
        {
            buttonImage.color = pressedColor;
            // Kısa süre sonra normale/durum rengine dön
            // Invoke("UpdateButtonVisuals", 0.2f); // UpdateButtonVisuals hemen çağrılacak
        }
        
        // Buton animatörü varsa
        if (buttonAnimator != null)
        {
            try
            {
                buttonAnimator.SetTrigger(triggerParametreName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DoorButton: Animator SetTrigger hatası: {e.Message}");
            }
        }
        
        // Kapıyı aç veya kapat (Toggle)
        if (allowToggleDoor)
        {
            targetDoor.ToggleDoor(); // Doğrudan Door.cs'deki ToggleDoor kullanılır
            // Debug.Log($"DoorButton: {targetDoor.name} için ToggleDoor çağrıldı. Yeni durum: {(targetDoor.isOpen ? "AÇIK" : "KAPALI")}");
        }
        else
        { 
            // Eğer toggle izin verilmiyorsa, sadece açmayı dene (eski davranış gibi)
            if (!targetDoor.isOpen)
            {
                targetDoor.OpenDoor();
                // Debug.Log($"DoorButton: {targetDoor.name} için OpenDoor çağrıldı (toggle kapalı).");
            }
            // else Debug.Log($"DoorButton: Kapı zaten açık ve toggle kapalı, işlem yapılmadı.");
        }
        
        // Tıklama sonrası buton görünümünü hemen güncelle
        UpdateButtonVisuals();
    }
    
    // Butonun rengini ve diğer görsellerini kapının durumuna göre güncelle
    private void UpdateButtonVisuals() // İsmi UpdateButtonColor'dan UpdateButtonVisuals'a değiştirildi
    {
        if (buttonImage == null) return; // Image yoksa işlem yapma

        if (targetDoor != null && targetDoor.isOpen && allowToggleDoor)
        {
            buttonImage.color = openDoorColor; // Kapı açıksa ve toggle edilebilirsee yeşil renk
        }
        else
        {
            buttonImage.color = normalColor; // Kapı kapalıysa veya toggle edilemezse normal renk
        }
        // Gelecekte buraya ikon değiştirme vb. eklenebilir.
    }
    
    // Start içinde çağrılabilir veya kapı durumu değiştiğinde event ile tetiklenebilir.
    void OnEnable() 
    {
        // Buton aktif olduğunda mevcut kapı durumuna göre görselini güncelleyebilir.
        if (targetDoor != null) // targetDoor null değilse güncelle
        {
            UpdateButtonVisuals();
        }
    }
    
    // Unity Editor'de targetDoor atanmışsa görsel gösterim
    private void OnDrawGizmosSelected()
    {
        if (targetDoor != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetDoor.transform.position);
        }
    }
} 