using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("Kapı Ayarları")]
    public bool isOpen = false;
    public float openAnimationDuration = 1.0f;
    
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
                animator = GetComponentInChildren<Animator>();
                if (animator != null) Debug.Log("Door: Animator kapının child objesinde bulundu.");
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
        
        if (animator != null)
        {
            if (debugMode) Debug.Log($"Door: {gameObject.name} kapısı açılıyor (SADECE ANİMASYON).");
            animator.SetBool(DOOR_OPEN, true);
        }
        else if (debugMode)
        {
            Debug.LogWarning($"Door: {gameObject.name} için Kapı Animatörü bulunamadı!", this);
        }
            
        if (debugMode)
        {
            StartCoroutine(CheckAnimatorState());
        }
    }
    
    public void CloseDoor()
    {
        isOpen = false;
        
        if (animator != null)
        {
            if (debugMode) Debug.Log($"Door: {gameObject.name} kapısı kapanıyor (SADECE ANİMASYON).");
            animator.SetBool(DOOR_OPEN, false);
        }
    }
    
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
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(1, 2, 0.1f));
        Gizmos.color = Color.green;
        Vector3 openDirection = transform.right * 1.5f;
        Gizmos.DrawLine(transform.position, transform.position + openDirection);
    }
} 