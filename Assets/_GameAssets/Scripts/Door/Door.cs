using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("Kapı Ayarları")]
    public bool isOpen = false;
    public float openSpeed = 2.0f;
    
    [Header("Animasyon Referansları")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool debugMode = true;
    
    // Animatör için parametre isimleri
    private const string DOOR_OPEN = "IsOpen";
    
    private void Awake()
    {
        // Animator bileşeni yoksa, bu GameObject'ten almaya çalış
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Door: Kapı için Animator bulunamadı! Doğru objeye atanmış mı kontrol edin.");
                
                // Child objelerinde Animator var mı kontrol et
                animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    Debug.Log("Door: Animator kapının child objesinde bulundu.");
                }
            }
        }
        
        // Animator parametresini kontrol et
        if (animator != null)
        {
            bool hasIsOpenParameter = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == DOOR_OPEN && param.type == AnimatorControllerParameterType.Bool)
                {
                    hasIsOpenParameter = true;
                    break;
                }
            }
            
            if (!hasIsOpenParameter)
            {
                Debug.LogError($"Door: Kapı Animator'ında '{DOOR_OPEN}' adında bir bool parametre bulunamadı! Animator Controller'ı kontrol edin.");
            }
            
            if (debugMode)
            {
                Debug.Log($"Door: Animator bulundu - Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "YOK!")}");
            }
        }
    }
    
    private void Start()
    {
        // Başlangıçta doğru durumu ayarla
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(DOOR_OPEN, isOpen);
        }
    }
    
    public void OpenDoor()
    {
        isOpen = true;
        
        // Animator varsa, animasyonu oynat
        if (animator != null)
        {
            if (debugMode) Debug.Log("Door: Kapı açılıyor, animator.SetBool(" + DOOR_OPEN + ", true) çağrılıyor.");
            animator.SetBool(DOOR_OPEN, true);
            
            // Animator durumunu kontrol et
            if (debugMode)
            {
                StartCoroutine(CheckAnimatorState());
            }
        }
        else
        {
            Debug.LogWarning("Door: Kapı Animatörü bulunamadı!");
        }
    }
    
    public void CloseDoor()
    {
        isOpen = false;
        
        // Animator varsa, animasyonu kapat
        if (animator != null)
        {
            if (debugMode) Debug.Log("Door: Kapı kapanıyor, animator.SetBool(" + DOOR_OPEN + ", false) çağrılıyor.");
            animator.SetBool(DOOR_OPEN, false);
        }
    }
    
    // Buton için doğrudan çağrılabilecek Toggle fonksiyonu
    public void ToggleDoor()
    {
        if (isOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }
    
    // Animator durumunu kontrol eden coroutine
    private System.Collections.IEnumerator CheckAnimatorState()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"Door: Animator durumu - NormalizedTime: {stateInfo.normalizedTime}, " +
                      $"IsName('Open'): {stateInfo.IsName("Open")}, " +
                      $"IsName('Close'): {stateInfo.IsName("Close")}, " +
                      $"IsOpen param değeri: {animator.GetBool(DOOR_OPEN)}");
        }
    }
    
    // Editor'da kapı ve animator durumunu görselleştir
    private void OnDrawGizmosSelected()
    {
        // Kapının rotasyonunu ve pozisyonunu göster
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(1, 2, 0.1f));
        
        // Açık kapı pozisyonunu göster
        Gizmos.color = Color.green;
        Vector3 openDirection = transform.right * 1.5f;
        Gizmos.DrawLine(transform.position, transform.position + openDirection);
    }
} 