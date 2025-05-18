using UnityEngine;
using UnityEngine.UI;

public class DoorTrigger : MonoBehaviour
{
    [Header("Kapı Referansı")]
    [SerializeField] private Door targetDoor;
    
    [Header("UI Referansları")]
    [SerializeField] private GameObject doorButton;
    [SerializeField] private Button interactButton;
    [SerializeField] private DoorButton doorButtonScript; // DoorButton script referansı
    
    [Header("Debug Ayarları")]
    [SerializeField] private bool debugMode = true;
    
    private bool playerInRange = false;
    
    private void Awake()
    {
        if (debugMode) Debug.Log("DoorTrigger: Awake çağrıldı - " + gameObject.name);
        
        // Başlangıçta bileşenleri kontrol et
        if (targetDoor == null)
            Debug.LogError("DoorTrigger: targetDoor referansı atanmamış! Lütfen Inspector'da atayın.");
            
        if (doorButton == null)
            Debug.LogError("DoorTrigger: doorButton (GameObject) referansı atanmamış! Lütfen Inspector'da atayın.");
            
        // Bu objenin Collider'i olduğundan emin ol
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError("DoorTrigger: Bu objede bir Collider bileşeni bulunamadı! Lütfen Box Collider ekleyin ve isTrigger özelliğini işaretleyin.");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning("DoorTrigger: Collider'ın isTrigger özelliği işaretli değil! İşaretlenecek.");
            collider.isTrigger = true;
        }
    }
    
    private void Start()
    {
        if (debugMode) Debug.Log("DoorTrigger: Start çağrıldı - " + gameObject.name);
        
        // Başlangıçta buton görünmez olmalı
        if (doorButton != null)
        {
            // DoorButton script referansını al
            if (doorButtonScript == null)
            {
                doorButtonScript = doorButton.GetComponent<DoorButton>();
                if (doorButtonScript == null)
                {
                    Debug.LogError("DoorTrigger: doorButton GameObject'inde DoorButton scripti bulunamadı!", doorButton);
                }
            }
            
            // Door referansını ayarla
            if (doorButtonScript != null && targetDoor != null)
            {
                doorButtonScript.SetTargetDoor(targetDoor);
                if (debugMode) Debug.Log("DoorTrigger: DoorButton scriptine kapı referansı aktarıldı - " + targetDoor.name);
            }
            else if (doorButtonScript == null && debugMode)
            {
                Debug.LogWarning("DoorTrigger: DoorButton scripti atanmamış, kapı referansı aktarılamadı.");
            }

            doorButton.SetActive(false);
            if (debugMode) Debug.Log("DoorTrigger: Başlangıçta buton (GameObject) gizlendi.");
        }
    }
    
    // Oyuncu trigger alanına girdiğinde
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (debugMode) Debug.Log("DoorTrigger: Oyuncu trigger alanına girdi! - " + other.name);
            playerInRange = true;
            ShowDoorButton();
        }
    }
    
    // Oyuncu trigger alanından çıktığında
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (debugMode) Debug.Log("DoorTrigger: Oyuncu trigger alanından çıktı! - " + other.name);
            playerInRange = false;
            HideDoorButton();
        }
    }
    
    // Başlangıçta karakter zaten içerideyse
    private void OnTriggerStay(Collider other)
    {
        if (!playerInRange && other.CompareTag("Player"))
        {
            if (debugMode) Debug.Log("DoorTrigger: Oyuncu zaten trigger alanı içinde! - " + other.name);
            playerInRange = true;
            ShowDoorButton();
        }
    }
    
    // Kapı butonunu göster
    private void ShowDoorButton()
    {
        if (doorButton != null)
        {
            // Buton gösterilmeden önce kapı referansını yeniden kontrol et ve ayarla
            if (doorButtonScript != null && targetDoor != null)
            {
                doorButtonScript.SetTargetDoor(targetDoor);
                if (debugMode) Debug.Log("DoorTrigger: Buton gösterilirken DoorButton scriptine kapı referansı güncellendi - " + targetDoor.name);
            }
            else if (debugMode)
            {
                if(doorButtonScript == null) Debug.LogWarning("DoorTrigger: ShowDoorButton - doorButtonScript atanmamış.");
                if(targetDoor == null) Debug.LogWarning("DoorTrigger: ShowDoorButton - targetDoor atanmamış.");
            }
            
            doorButton.SetActive(true);
            if (debugMode) Debug.Log("DoorTrigger: Buton (GameObject) gösterildi!");
        }
        else
        {
            Debug.LogError("DoorTrigger: doorButton referansı bulunamadı! ShowDoorButton çağrılamadı.");
        }
    }
    
    // Kapı butonunu gizle
    private void HideDoorButton()
    {
        if (doorButton != null)
        {
            doorButton.SetActive(false);
            if (debugMode) Debug.Log("DoorTrigger: Buton (GameObject) gizlendi.");
        }
        else
        {
            Debug.LogError("DoorTrigger: doorButton referansı bulunamadı! HideDoorButton çağrılamadı.");
        }
    }
    
    // Unity Editor'da görselleştirmeler
    private void OnDrawGizmos()
    {
        // Trigger alanını görselleştir
        Gizmos.color = new Color(1, 0.92f, 0.016f, 0.2f); // Yarı saydam sarı
        
        // Collider boyutlarını alıp görselleştir
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            if (collider is BoxCollider)
            {
                BoxCollider boxCollider = collider as BoxCollider;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
            else if (collider is SphereCollider)
            {
                SphereCollider sphereCollider = collider as SphereCollider;
                Gizmos.DrawSphere(transform.position + sphereCollider.center, sphereCollider.radius);
            }
        }
        
        // Kapı ile bağlantıyı göster
        if (targetDoor != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetDoor.transform.position);
        }
    }
} 