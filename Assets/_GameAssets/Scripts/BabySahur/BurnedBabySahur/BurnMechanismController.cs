using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Eğer UI elementleri kullanıyorsanız (Button gibi)

public class BurnMechanismController : MonoBehaviour
{
    public static event System.Action OnBabyTungBurned;

    [Header("UI Referansları")]
    [SerializeField] private GameObject burnedButton; // Canvas'taki yakma butonu

    [Header("Obje Referansları")]
    [SerializeField] private GameObject inventoryBabySahur; // Oyuncunun kamerasındaki/envanterindeki Bebek Sahur
    [SerializeField] private GameObject fireBabySahur;    // Ateşe verilecek Bebek Sahur modeli
    [SerializeField] private GameObject vfxFire;          // Ateş görsel efekti

    [Header("Ayarlar")]
    [SerializeField] private float delayToShowVFX = 2f;
    [SerializeField] private float delayToHideFireBabySahur = 2f; // fireBabySahur'un solmaya başlamasından önceki gecikme
    [SerializeField] private float fadeOutDuration = 1.0f; // fireBabySahur'un solma süresi
    [SerializeField] private float delayToHideVFX = 1f;         // fireBabySahur solduktan sonra VFX'in gizlenme gecikmesi
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private Coroutine burnCoroutine;

    private void Start()
    {
        // Başlangıçta butonları ve efektleri gizle
        if (burnedButton != null)
        {
            burnedButton.SetActive(false);
        }
        else
        {
            Debug.LogError("Burned Button referansı atanmamış!");
        }

        if (fireBabySahur != null)
        {
            fireBabySahur.SetActive(false);
        }
        else
        {
            Debug.LogError("FireBabySahur referansı atanmamış!");
        }

        if (vfxFire != null)
        {
            vfxFire.SetActive(false);
        }
        else
        {
            Debug.LogError("VFX_Fire referansı atanmamış!");
        }
        
        if (inventoryBabySahur == null)
        {
            Debug.LogError("InventoryBabySahur referansı atanmamış! Bu, oyuncunun taşıdığı Bebek Sahur olmalı.");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) // Oyuncunun tag'inin "Player" olduğundan emin olun
        {
            if (inventoryBabySahur != null && inventoryBabySahur.activeSelf)
            {
                if (burnedButton != null && !burnedButton.activeSelf)
                {
                    burnedButton.SetActive(true);
                    if (debugMode) Debug.Log("Oyuncu trigger alanında ve envanterde Bebek Sahur var. Yakma butonu aktif.");
                }
            }
            else
            {
                if (burnedButton != null && burnedButton.activeSelf)
                {
                    burnedButton.SetActive(false);
                    if (debugMode) Debug.Log("Oyuncu trigger alanında ama envanterde Bebek Sahur yok. Yakma butonu pasif.");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (burnedButton != null && burnedButton.activeSelf)
            {
                burnedButton.SetActive(false);
                if (debugMode) Debug.Log("Oyuncu trigger alanından çıktı. Yakma butonu pasif.");
            }
        }
    }

    // Bu metod UI Button'ının OnClick event'ine atanacak
    public void OnBurnButtonClick()
    {
        if (debugMode) Debug.Log("Yakma butonuna tıklandı.");

        if (inventoryBabySahur == null || !inventoryBabySahur.activeSelf)
        {
            if (debugMode) Debug.LogWarning("Envanterde yakılacak Bebek Sahur bulunamadı.");
            return;
        }

        if (fireBabySahur == null || vfxFire == null)
        {
            if (debugMode) Debug.LogError("FireBabySahur veya VFX_Fire referansları eksik!");
            return;
        }

        if (burnedButton != null)
        {
            burnedButton.SetActive(false); // Butonu hemen gizle
        }

        inventoryBabySahur.SetActive(false);
        if (debugMode) Debug.Log("Envanterdeki Bebek Sahur pasifleştirildi.");

        fireBabySahur.SetActive(true); 
        if (debugMode) Debug.Log("FireBabySahur aktifleştirildi (solma için hazırlanıyor).");


        // Eğer önceki bir yakma işlemi varsa durdur
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
        }
        burnCoroutine = StartCoroutine(BurnSequenceCoroutine());
    }

    private IEnumerator FadeOutAndDeactivate(GameObject objToFade, float duration)
    {
        if (objToFade == null)
        {
            if(debugMode) Debug.LogError("FadeOutAndDeactivate: objToFade null!");
            yield break;
        }

        Renderer renderer = objToFade.GetComponent<Renderer>();
        SpriteRenderer spriteRenderer = objToFade.GetComponent<SpriteRenderer>();
        Image uiImage = objToFade.GetComponent<Image>();
        
        Material materialInstance = null;
        if (renderer != null)
        {
            materialInstance = renderer.material; 
        }

        if (renderer == null && spriteRenderer == null && uiImage == null)
        {
            if (debugMode) Debug.LogWarning(objToFade.name + " için Renderer, SpriteRenderer veya Image bulunamadı. Direkt pasifleştiriliyor.");
            objToFade.SetActive(false);
            yield break;
        }

        float counter = 0f;
        Color originalColor = Color.white;

        if (materialInstance != null)
        {
            originalColor = materialInstance.color;
        }
        else if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else if (uiImage != null)
        {
            originalColor = uiImage.color;
        }

        float startAlpha = originalColor.a;
        
        // Başlangıçta objenin alfa değerini 1 yap (tamamen görünür)
        // Bu, obje aktif edildiğinde hemen solmaya başlıyorsa ve startAlpha zaten 0 ise görünmez olmasını engeller.
        if (materialInstance != null)
        {
            materialInstance.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
            startAlpha = 1f;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
            startAlpha = 1f;
        }
        else if (uiImage != null)
        {
            uiImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
            startAlpha = 1f;
        }


        while (counter < duration)
        {
            counter += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, counter / duration);

            if (materialInstance != null)
            {
                materialInstance.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
            else if (uiImage != null)
            {
                uiImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
            yield return null;
        }

        // Alfa değerinin tam 0 olduğundan emin ol
        if (materialInstance != null)
        {
            materialInstance.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }
        else if (uiImage != null)
        {
            uiImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }
        
        objToFade.SetActive(false);
        if (debugMode) Debug.Log(objToFade.name + " başarıyla soldu ve pasifleştirildi.");
    }

    private IEnumerator BurnSequenceCoroutine()
    {
        if (debugMode) Debug.Log("Yakma sekansı başlatıldı.");

        // VFX_Fire objesinin aktifliği açılacak
        yield return new WaitForSeconds(delayToShowVFX);
        if (vfxFire != null)
        {
            vfxFire.SetActive(true);
            if (debugMode) Debug.Log("VFX_Fire aktifleştirildi.");
        }

        // FireBabySahur solmaya başlamadan önceki bekleme
        yield return new WaitForSeconds(delayToHideFireBabySahur); 
        if (fireBabySahur != null && fireBabySahur.activeSelf) // Hala aktif mi kontrol et
        {
            if (debugMode) Debug.Log("FireBabySahur solmaya başlıyor...");
            yield return StartCoroutine(FadeOutAndDeactivate(fireBabySahur, fadeOutDuration)); // Düzeltilmiş çağrı
            
            if (fireBabySahur == null || !fireBabySahur.activeSelf) 
            {
                if (debugMode) Debug.Log("FireBabySahur başarıyla soldu ve deaktif oldu. OnBabyTungBurned olayı tetikleniyor.");
                OnBabyTungBurned?.Invoke(); 
            }
        }
        else if (fireBabySahur == null)
        {
             if (debugMode) Debug.LogError("FireBabySahur referansı null, solma işlemi yapılamıyor.");
        }
         else if (!fireBabySahur.activeSelf && debugMode) 
        {
            if (debugMode) Debug.LogWarning("FireBabySahur zaten pasif, solma işlemi atlanıyor.");
        }


        // FireBabySahur solduktan sonra VFX_Fire'ın gizlenmesi için bekle
        yield return new WaitForSeconds(delayToHideVFX); 
        if (vfxFire != null && vfxFire.activeSelf) 
        {
            vfxFire.SetActive(false);
            if (debugMode) Debug.Log("VFX_Fire pasifleştirildi (FireBabySahur solduktan sonra).");
        }
        
        burnCoroutine = null; // Coroutine tamamlandı
        if (debugMode) Debug.Log("Yakma sekansı tamamlandı.");
    }
} 