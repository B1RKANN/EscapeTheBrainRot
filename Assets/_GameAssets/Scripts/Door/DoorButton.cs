using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// UI butonunun üzerine tıklandığında kapıyı açacak script
public class DoorButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Kapı Referansı")]
    [SerializeField] private Door targetDoor;
    [SerializeField] private bool autoFindDoor = true; // En yakın kapıyı otomatik bul
    [SerializeField] private bool allowToggleDoor = true; // Kapıyı tekrar tıklayarak kapatmayı aktif et
    
    [Header("Buton Görünümü")]
    // Buton animasyonu için (isteğe bağlı)
    [SerializeField] private Animator buttonAnimator;
    [SerializeField] private string triggerParametreName = "Click"; // Animator'daki trigger parametre adı
    
    // Görsel geri bildirim için
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = Color.gray;
    [SerializeField] private Color openDoorColor = new Color(0.2f, 0.8f, 0.2f); // Kapı açıkken buton rengi
    
    // Kapının son durumunu takip et
    private bool isDoorOpen = false;
    
    // Kapı referansını dışarıdan ayarlamak için public metod
    public void SetTargetDoor(Door doorRef)
    {
        targetDoor = doorRef;
        Debug.Log("DoorButton: Hedef kapı ayarlandı - " + (doorRef != null ? doorRef.name : "null"));
    }
    
    // Kapı referansını almak için
    public Door GetTargetDoor()
    {
        return targetDoor;
    }
    
    private void Awake()
    {
        // Otomatik olarak kapıyı bul
        if (autoFindDoor && targetDoor == null)
        {
            FindNearestDoor();
        }
    }
    
    private void Start()
    {
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
        
        // Referansları kontrol et ve uyarı ver
        if (targetDoor == null)
        {
            Debug.LogWarning("DoorButton: targetDoor referansı yok! Lütfen Inspector'da atayın veya autoFindDoor'u etkinleştirin.");
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
    
    // En yakın kapıyı bul
    private void FindNearestDoor()
    {
        Door[] allDoors = FindObjectsOfType<Door>();
        float closestDistance = float.MaxValue;
        
        foreach (Door door in allDoors)
        {
            float distance = Vector3.Distance(transform.position, door.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetDoor = door;
            }
        }
        
        if (targetDoor != null)
        {
            Debug.Log($"DoorButton: En yakın kapı otomatik bulundu - {targetDoor.name} (Mesafe: {closestDistance:F2} birim)");
        }
    }
    
    // UI butonuna tıklama işlemi
    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetDoor == null)
        {
            // Tekrar kapı bulmayı dene
            FindNearestDoor();
            
            if (targetDoor == null)
            {
                Debug.LogError("DoorButton: targetDoor referansı yok! Buton tıklanınca kapı bulunamadı.");
                return;
            }
        }
        
        // Buton basılma animasyonu
        if (buttonImage != null)
        {
            buttonImage.color = pressedColor;
            // Kısa süre sonra normale dön
            Invoke("UpdateButtonColor", 0.2f);
        }
        
        // Buton animatörü varsa
        if (buttonAnimator != null)
        {
            try
            {
                // Animatör trigger'ı çalıştır
                buttonAnimator.SetTrigger(triggerParametreName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DoorButton: Animator SetTrigger hatası: {e.Message}");
            }
        }
        
        // Kapıyı aç veya kapat
        if (allowToggleDoor && isDoorOpen)
        {
            // Kapı zaten açıksa, kapat
            targetDoor.CloseDoor();
            isDoorOpen = false;
            Debug.Log("DoorButton: Kapı kapatılıyor...");
        }
        else
        {
            // Kapı kapalıysa, aç
            targetDoor.OpenDoor();
            isDoorOpen = true;
            Debug.Log("DoorButton: Kapı açılıyor...");
        }
    }
    
    // Butonun rengini güncelle
    private void UpdateButtonColor()
    {
        if (buttonImage != null)
        {
            // Kapı durumuna göre renk seç
            if (isDoorOpen && allowToggleDoor)
            {
                buttonImage.color = openDoorColor; // Kapı açıksa yeşil renk
            }
            else
            {
                buttonImage.color = normalColor; // Kapı kapalıysa normal renk
            }
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