using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))] // Bu scriptin olduğu objede bir Collider olmasını zorunlu kılar
public class DoorInteractionTrigger : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Bu trigger'ın kontrol edeceği kapının Door script'i")]
    [SerializeField] private Door doorToControl; 

    [Header("Ayarlar")]
    [Tooltip("BabySahur karakterinin sahip olduğu tag")]
    [SerializeField] private string babySahurTag = "BabySahur"; // BabySahur'unuzun tag'ini buraya girin
    [SerializeField] private bool debugMode = true;

    private Coroutine _doorCycleCoroutine;
    // Birden fazla BabySahur'un aynı anda trigger'da olmasına veya bir BabySahur'un
    // trigger içinde kalıp tekrar tekrar tetiklemesine karşı basit bir önlem.
    private bool _isCycleRunningForThisTrigger = false; 

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            if (debugMode) Debug.LogWarning($"DoorInteractionTrigger ({gameObject.name}): Collider bir trigger değil. Otomatik olarak trigger yapılıyor.", this);
            col.isTrigger = true;
        }

        if (doorToControl == null)
        {
            // Eğer atanmamışsa, parent objede Door component'ını aramayı deneyebiliriz
            doorToControl = GetComponentInParent<Door>();
            if (doorToControl == null)
            {
                Debug.LogError($"DoorInteractionTrigger ({gameObject.name}): Kontrol edilecek Door script'i atanmamış veya parent'ta bulunamadı! Bu trigger çalışmayacak.", this);
                enabled = false; // Script'i devre dışı bırak
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || doorToControl == null) return; // Script aktif değilse veya kapı yoksa çık

        if (other.CompareTag(babySahurTag))
        {
            if (_isCycleRunningForThisTrigger)
            {
                if (debugMode) Debug.Log($"DoorInteractionTrigger ({gameObject.name}): {other.name} trigger'a girdi ama zaten bir kapı döngüsü çalışıyor.", this);
                return; // Zaten bir döngü çalışıyorsa tekrar başlatma
            }
            
            if (debugMode) Debug.Log($"DoorInteractionTrigger ({gameObject.name}): {other.name} trigger'a girdi. Kontrol edilen kapı: {doorToControl.gameObject.name}, Durumu: {(doorToControl.isOpen ? "AÇIK" : "KAPALI")}", this);

            if (!doorToControl.isOpen)
            {
                if (debugMode) Debug.Log($"DoorInteractionTrigger ({gameObject.name}): Kapı ({doorToControl.gameObject.name}) kapalı, aç/kapat döngüsü başlatılıyor.", this);
                
                if (_doorCycleCoroutine != null) // Çok olası değil ama yine de bir önceki coroutine'i durdur
                {
                    StopCoroutine(_doorCycleCoroutine);
                }
                _doorCycleCoroutine = StartCoroutine(OpenAndCloseDoorCycle());
            }
            else
            {
                if (debugMode) Debug.Log($"DoorInteractionTrigger ({gameObject.name}): Kapı ({doorToControl.gameObject.name}) zaten açık, bir işlem yapılmıyor.", this);
            }
        }
    }

    private IEnumerator OpenAndCloseDoorCycle()
    {
        _isCycleRunningForThisTrigger = true; // Döngünün başladığını işaretle

        // Kapıyı Aç
        doorToControl.OpenDoor();
        if (debugMode) Debug.Log($"DoorInteractionTrigger ({gameObject.name}): {doorToControl.gameObject.name} OpenDoor() çağrıldı.", this);

        // Kapı açılma animasyonunun süresi kadar bekle
        float waitTime = doorToControl.openAnimationDuration > 0 ? doorToControl.openAnimationDuration : 0.5f;
        if (debugMode) Debug.Log($"DoorInteractionTrigger ({gameObject.name}): Kapı açılması için {waitTime} saniye bekleniyor.", this);
        yield return new WaitForSeconds(waitTime);

        // Kapıyı Kapat (sadece hala açıksa ve bu trigger tarafından açıldıysa gibi bir kontrol eklenebilir, ama basit tutalım)
        if(doorToControl.isOpen) // Bu kontrol önemli, başka bir şey kapıyı bu arada kapatmış olabilir.
        {
            doorToControl.CloseDoor();
            if (debugMode) Debug.Log($"DoorInteractionTrigger ({gameObject.name}): {doorToControl.gameObject.name} CloseDoor() çağrıldı.", this);
        }
        else if(debugMode)
        {
             Debug.Log($"DoorInteractionTrigger ({gameObject.name}): {doorToControl.gameObject.name} zaten kapanmış veya hiç açılmamış, CloseDoor() tekrar çağrılmadı.", this);
        }
        
        _doorCycleCoroutine = null;
        _isCycleRunningForThisTrigger = false; // Döngünün bittiğini işaretle
    }
} 