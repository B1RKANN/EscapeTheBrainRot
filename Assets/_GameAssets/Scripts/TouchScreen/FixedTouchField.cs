using UnityEngine;

public class FixedTouchField : MonoBehaviour
{
    [HideInInspector]
    public Vector2 TouchDist;
    
    // Görsel kontrol için bu değişkeni inspector'da gösterebilirsiniz
    [SerializeField] 
    private bool showDebug = false;
    
    // Ayarlar
    [SerializeField] private float minSwipeDistance = 5f;
    
    // Kontrol değişkenleri
    private bool isTouching = false;
    private bool hasSwiped = false;
    private Vector2 startTouchPos;
    private Vector2 lastTouchPos;
    private int trackingFingerId = -1;
    private RectTransform touchArea;
    
    void Awake()
    {
        touchArea = GetComponent<RectTransform>();
        if (touchArea == null)
        {
            Debug.LogWarning("FixedTouchField scripti bir UI elemanının (RectTransform) üzerinde olmalıdır!");
        }
    }
    
    void Update()
    {
        // Her frame başında rotasyon sıfırlanır
        TouchDist = Vector2.zero;
        
        // Dokunmatik ekran için
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        // Mouse için
        else
        {
            HandleMouseInput();
        }
        
        // Debug bilgisi göster
        if (showDebug && isTouching)
        {
            Debug.Log("Touch Active: " + isTouching + ", Swiped: " + hasSwiped + ", TouchDist: " + TouchDist);
        }
    }
    
    void HandleTouchInput()
    {
        // Daha önce takip ettiğimiz bir parmak varsa onu bulalım
        if (isTouching && trackingFingerId >= 0)
        {
            bool foundTouch = false;
            Touch currentTouch = new Touch();
            
            // Takip ettiğimiz parmağı bulalım
            for (int i = 0; i < Input.touchCount; i++)
            {
                currentTouch = Input.touches[i];
                if (currentTouch.fingerId == trackingFingerId)
                {
                    foundTouch = true;
                    break;
                }
            }
            
            // Takip ettiğimiz parmak hala dokunuyor mu?
            if (foundTouch)
            {
                // Parmak ekrandan kaldırıldı mı?
                if (currentTouch.phase == TouchPhase.Ended || currentTouch.phase == TouchPhase.Canceled)
                {
                    ResetTouchTracking();
                    return;
                }
                
                // TouchScreen alanımızın içinde mi?
                if (touchArea != null && !IsPointInsideRect(currentTouch.position, touchArea))
                {
                    // Alanın dışında, rotasyon yok ama takibi sürdürüyoruz
                    return;
                }
                
                // Daha önce kaydırma başladı mı kontrolü
                if (!hasSwiped)
                {
                    float swipeDistance = Vector2.Distance(startTouchPos, currentTouch.position);
                    if (swipeDistance >= minSwipeDistance)
                    {
                        hasSwiped = true;
                        lastTouchPos = currentTouch.position;
                    }
                }
                else
                {
                    // Kaydırma devam ediyor, rotasyon hesapla
                    TouchDist = currentTouch.position - lastTouchPos;
                    lastTouchPos = currentTouch.position;
                }
            }
            else
            {
                // Parmak bulunamadı, takibi sıfırla
                ResetTouchTracking();
            }
        }
        // Henüz takip ettiğimiz bir parmak yoksa, yeni dokunma var mı bakalım
        else if (!isTouching && Input.touchCount > 0)
        {
            // Tüm dokunmaları kontrol et ve TouchScreen alanında olanı seçelim
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch newTouch = Input.touches[i];
                
                // Sadece yeni başlayan dokunmaları değerlendiriyoruz
                if (newTouch.phase == TouchPhase.Began)
                {
                    // TouchScreen alanımızın içinde mi?
                    if (touchArea == null || IsPointInsideRect(newTouch.position, touchArea))
                    {
                        // Bu parmağı takip etmeye başla
                        trackingFingerId = newTouch.fingerId;
                        startTouchPos = newTouch.position;
                        lastTouchPos = newTouch.position;
                        isTouching = true;
                        hasSwiped = false;
                        break;
                    }
                }
            }
        }
    }
    
    void HandleMouseInput()
    {
        // Mouse henüz takip edilmiyorsa ve tıklanıldıysa
        if (!isTouching && Input.GetMouseButtonDown(0))
        {
            // TouchScreen alanımızın içinde mi?
            if (touchArea == null || IsPointInsideRect(Input.mousePosition, touchArea))
            {
                // Mouse takibini başlat
                startTouchPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                lastTouchPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                isTouching = true;
                hasSwiped = false;
                trackingFingerId = -999; // Mouse için özel değer
            }
        }
        // Mouse takip ediliyorsa
        else if (isTouching && trackingFingerId == -999)
        {
            // Mouse butonu bırakıldı mı?
            if (!Input.GetMouseButton(0))
            {
                ResetTouchTracking();
                return;
            }
            
            // Vector2'ye dönüştürme
            Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            
            // Mouse TouchScreen alanının içinde mi?
            if (touchArea != null && !IsPointInsideRect(mousePos, touchArea))
            {
                // Alanın dışında, rotasyon yok ama takibi sürdürüyoruz
                return;
            }
            
            // Daha önce kaydırma başladı mı kontrolü
            if (!hasSwiped)
            {
                float swipeDistance = Vector2.Distance(startTouchPos, mousePos);
                if (swipeDistance >= minSwipeDistance)
                {
                    hasSwiped = true;
                    lastTouchPos = mousePos;
                }
            }
            else
            {
                // Kaydırma devam ediyor, rotasyon hesapla
                TouchDist = mousePos - lastTouchPos;
                lastTouchPos = mousePos;
            }
        }
    }
    
    // Touch takibini sıfırla
    void ResetTouchTracking()
    {
        isTouching = false;
        hasSwiped = false;
        trackingFingerId = -1;
        TouchDist = Vector2.zero;
    }
    
    // Bir noktanın RectTransform içinde olup olmadığını kontrol et
    bool IsPointInsideRect(Vector2 point, RectTransform rect)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rect, point);
    }
}