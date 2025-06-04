using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CageController : MonoBehaviour
{
    [Header("Object References")]
    [Tooltip("Player kamerasının altındaki anahtar objesi")]
    public GameObject keyObject;
    [Tooltip("Collider eklenecek ve animasyonu oynatılacak kafes objesi")]
    public GameObject cageObject;
    [Tooltip("Kafesi açma butonu")]
    public Button openCageButton;
    [Tooltip("Kafesin açılma animasyonunu kontrol eden Animator")]
    public Animator cageAnimator;
    [Tooltip("BabySahurların kaçacağı parent transform")]
    public Transform babySahurEscapeParent;
    [Tooltip("Kaçacak BabySahurların listesi")]
    public List<BabySahurController> babySahursToEscape; // Veya Transform babySahursParent;
    // [Tooltip("Bebeklerin kaçışını gösteren Timeline")] // KALDIRILDI
    // public PlayableDirector escapeCutscene; // KALDIRILDI
    [Tooltip("Oyuncu objesi")]
    public GameObject playerObject;

    [Header("Collider Settings")]
    [SerializeField] private Vector3 colliderCenter = Vector3.zero;
    [SerializeField] private Vector3 colliderSize = Vector3.one;

    [Header("Escape Settings")]
    [Tooltip("Bebeklerin kaçması için beklenecek tahmini süre (saniye).")]
    [SerializeField] private float estimatedEscapeTime = 5.0f;

    private BoxCollider cageCollider;
    private bool isPlayerNear = false;

    void Awake()
    {
        if (keyObject == null) Debug.LogError("Key Object referansı atanmamış!", this);
        if (cageObject == null) Debug.LogError("Cage Object referansı atanmamış!", this);
        if (openCageButton == null) Debug.LogError("Open Cage Button referansı atanmamış!", this);
        if (cageAnimator == null) Debug.LogWarning("Cage Animator referansı atanmamış! Animasyon çalışmayabilir.", this);
        // if (escapeCutscene == null) Debug.LogWarning("Escape Cutscene (PlayableDirector) referansı atanmamış! Kesme sahne çalışmayabilir.", this); // KALDIRILDI
        if (playerObject == null) Debug.LogWarning("Player Object referansı atanmamış!", this);
        if (babySahurEscapeParent == null) Debug.LogError("Baby Sahur Escape Parent referansı atanmamış!", this);

        if (openCageButton != null)
        {
            openCageButton.gameObject.SetActive(false);
            openCageButton.onClick.AddListener(OpenCageSequence);
        }
    }

    void Start()
    {
        if (keyObject != null && keyObject.activeInHierarchy)
        {
            InitializeCageCollider();
        }
        else
        {
            if (keyObject == null)
            {
                Debug.LogWarning("KeyObject atanmamış. Kafes collider'ı ve açılma mekanizması anahtar olmadan çalışmayacak şekilde ayarlanabilir veya farklı bir mantık izlenebilir.", this);
            }
            else
            {
                Debug.Log("Anahtar aktif değil. Kafes collider'ı eklenmedi. Anahtar aktif olduğunda tekrar kontrol edilebilir veya farklı bir mantık izlenebilir.", this);
                // İsteğe bağlı: Anahtarın aktifleşmesini dinleyen bir mekanizma eklenebilir.
            }
        }
    }

    void Update()
    {
        // Eğer anahtar oyun sırasında aktif/deaktif olabiliyorsa ve collider'ın buna göre
        // anlık olarak eklenip kaldırılması gerekiyorsa, burada bir kontrol eklenebilir.
        // Örneğin: if (keyObject != null && keyObject.activeInHierarchy && cageCollider == null) InitializeCageCollider();
        // else if (keyObject != null && !keyObject.activeInHierarchy && cageCollider != null) { Destroy(cageCollider); cageCollider = null; }
    }


    void InitializeCageCollider()
    {
        if (cageObject == null) return;

        cageCollider = cageObject.GetComponent<BoxCollider>();
        if (cageCollider == null)
        {
            cageCollider = cageObject.AddComponent<BoxCollider>();
            Debug.Log("Kafese BoxCollider eklendi.", cageObject);
        }
        cageCollider.isTrigger = true;
        cageCollider.center = colliderCenter;
        cageCollider.size = colliderSize;
        Debug.Log("Kafes collider'ı trigger olarak ayarlandı. Merkez: " + colliderCenter + ", Boyut: " + colliderSize, cageObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Anahtarın varlığını ve aktifliğini burada tekrar kontrol etmek iyi bir pratik olabilir.
            // Özellikle anahtar oyun içinde elde ediliyorsa.
            if (keyObject != null && keyObject.activeInHierarchy)
            {
                isPlayerNear = true;
                if (openCageButton != null)
                {
                    openCageButton.gameObject.SetActive(true);
                    Debug.Log("Oyuncu kafes trigger'ına girdi, anahtar aktif. Buton gösteriliyor.", this);
                }
            }
            else
            {
                Debug.Log("Oyuncu kafes trigger'ına girdi ancak anahtar aktif değil veya yok. Buton gösterilmeyecek.", this);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (openCageButton != null && openCageButton.gameObject.activeSelf)
            {
                openCageButton.gameObject.SetActive(false);
                Debug.Log("Oyuncu kafes trigger'ından çıktı. Buton gizlendi.", this);
            }
        }
    }

    public void OpenCageSequence()
    {
        Debug.Log("OpenCageSequence çağrıldı.", this);
        if (openCageButton != null) openCageButton.gameObject.SetActive(false);

        // Oyuncunun envanterindeki anahtarı (keyObject) yok et
        if (keyObject != null)
        {
            Destroy(keyObject);
            Debug.Log("Oyuncunun envanterindeki anahtar (keyObject) yok edildi.", this);
        }
        else
        {
            Debug.LogWarning("keyObject referansı null, envanterdeki anahtar yok edilemedi (belki daha önce yok edildi veya hiç atanmadı?).", this);
        }

        if (cageAnimator == null)
        {
            Debug.LogError("Cage Animator atanmamış! Kafes açılma animasyonu oynatılamıyor.", this);
            return; 
        }

        cageAnimator.SetTrigger("OpenTrigger"); 
        Debug.Log("Kafes açılma animasyonu 'OpenTrigger' ile tetiklendi.", this);

        // Kesme sahne ile ilgili kısım kaldırıldı
        // if (escapeCutscene != null)
        // {
        //     escapeCutscene.Play();
        //     Debug.Log("Kaçış kesme sahnesi (Timeline) başlatıldı.", this);
        // }
        // else
        // {
        //     Debug.LogWarning("EscapeCutscene (PlayableDirector) atanmamış. Kesme sahne oynatılamıyor.", this);
        // }
        
        if (babySahursToEscape != null && babySahursToEscape.Count > 0 && babySahurEscapeParent != null)
        {
            StartCoroutine(EscapeBabySahursCoroutine());
        }
        else
        {
            Debug.LogWarning("Kaçacak BabySahur listesi, sayısı veya kaçış noktaları parent'ı atanmamış/uygun değil. Bebekler kaçırılmayacak.", this);
        }
    }

    private System.Collections.IEnumerator EscapeBabySahursCoroutine()
    {
        Debug.Log("EscapeBabySahursCoroutine başlatıldı.", this);
        if (babySahurEscapeParent == null || babySahursToEscape == null || babySahursToEscape.Count == 0 || babySahurEscapeParent.childCount == 0)
        {
            Debug.LogError("Bebek Sahur kaçış noktaları, kaçacak bebekler düzgün ayarlanmamış veya hiç kaçış noktası yok!", this);
            yield break;
        }

        List<Transform> availableEscapePoints = new List<Transform>();
        foreach (Transform point in babySahurEscapeParent)
        {
            if (point != null) 
            {
                availableEscapePoints.Add(point);
            }
        }

        if (availableEscapePoints.Count == 0)
        {
            Debug.LogError("babySahurEscapeParent altında hiç geçerli kaçış noktası (child transform) bulunamadı!", this);
            yield break;
        }

        int babySahurIndex = 0;
        foreach (BabySahurController babySahur in babySahursToEscape)
        {
            if (babySahur == null)
            {
                Debug.LogWarning($"babySahursToEscape listesindeki {babySahurIndex}. eleman null.");
                babySahurIndex++;
                continue;
            }

            if (availableEscapePoints.Count == 0)
            {
                Debug.LogWarning($"{babySahur.name} için uygun kaçış noktası kalmadı. Toplam kaçış noktası: {babySahurEscapeParent.childCount}, Kaçırılmaya çalışılan bebek sayısı: {babySahursToEscape.Count}", babySahur.gameObject);
                babySahurIndex++;
                continue;
            }

            int randomIndex = Random.Range(0, availableEscapePoints.Count);
            Transform targetPoint = availableEscapePoints[randomIndex];
            availableEscapePoints.RemoveAt(randomIndex); 

            Debug.Log($"{babySahur.name} adlı BabySahur, {targetPoint.name} adlı kaçış noktasına yönlendiriliyor.", babySahur.gameObject);
            babySahur.StartEscape(targetPoint.position); 
            babySahurIndex++;
        }

        Debug.Log($"Bebeklerin kaçışı için tahmini {estimatedEscapeTime} saniye bekleniyor.", this);
        yield return new WaitForSeconds(estimatedEscapeTime);

        Debug.Log("EscapeBabySahursCoroutine tamamlandı.", this);
    }
} 