using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections; // Coroutine için eklendi

public class BurnProgressUIController : MonoBehaviour
{
    [Header("UI Referansları")]
    [Tooltip("Yakılan bebekleri temsil eden UI Image'ları. Sırayla (soldan sağa) atanmalı.")]
    [SerializeField] private List<Image> progressImages; // Canvas'taki 5 adet Image

    [Tooltip("Bebek yandığında Image'lara atanacak yeni Sprite.")]
    [SerializeField] private Sprite burnedSprite; // Yanmış bebek görseli

    [Header("Animasyon Ayarları")]
    [SerializeField] private bool useSpriteChangeAnimation = true;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float animationScaleMultiplier = 1.2f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // Statik sayaç, kaç bebeğin yakıldığını oyun genelinde takip eder (scene yüklemeleri arasında sıfırlanmaz, dikkat!)
    // Eğer her levelde sıfırlanması gerekiyorsa, ResetBurnCount çağrılmalı.
    private static int burnedCount = 0;
    private Dictionary<Image, Coroutine> activeAnimations = new Dictionary<Image, Coroutine>();

    private void OnEnable()
    {
        // BurnMechanismController'daki olaya abone ol
        BurnMechanismController.OnBabyTungBurned += HandleBabyTungBurned;
        if (debugMode) Debug.Log("BurnProgressUIController: OnBabyTungBurned eventine abone olundu.");

        // Başlangıçta UI'ı mevcut duruma göre güncelle
        UpdateProgressUI(false); // Başlangıçta animasyonsuz güncelle
    }

    private void OnDisable()
    {
        // Script devre dışı kaldığında veya yok edildiğinde olayı bırak
        BurnMechanismController.OnBabyTungBurned -= HandleBabyTungBurned;
        if (debugMode) Debug.Log("BurnProgressUIController: OnBabyTungBurned eventinden abonelik kaldırıldı.");
        
        // Aktif animasyonları durdur
        foreach (var animCoroutine in activeAnimations.Values)
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
        }
        activeAnimations.Clear();
    }

    private void HandleBabyTungBurned()
    {
        if (debugMode) Debug.Log($"HandleBabyTungBurned çağrıldı. Mevcut burnedCount: {burnedCount} (artırılmadan önce)");

        if (progressImages == null || progressImages.Count == 0)
        {
            if (debugMode) Debug.LogError("Progress Images listesi atanmamış veya boş!");
            return;
        }
        if (burnedSprite == null)
        {
            if (debugMode) Debug.LogError("Burned Sprite atanmamış!");
            return;
        }

        if (burnedCount < progressImages.Count)
        {
            Image currentImage = progressImages[burnedCount];
            if (currentImage != null)
            {
                currentImage.sprite = burnedSprite;
                if (debugMode) Debug.Log($"Progress Image [{burnedCount}] sprite'ı '{burnedSprite.name}' olarak değiştirildi.");

                if (useSpriteChangeAnimation)
                {
                    // Önceki animasyonu durdur (varsa)
                    if (activeAnimations.TryGetValue(currentImage, out Coroutine existingAnim) && existingAnim != null)
                    {
                        StopCoroutine(existingAnim);
                    }
                    activeAnimations[currentImage] = StartCoroutine(AnimateImageChange(currentImage));
                }
            }
            else
            {
                if (debugMode) Debug.LogError($"Progress Image listesindeki {burnedCount}. eleman (index) null!");
            }
            burnedCount++; // Sayacı bir sonraki bebek için artır
        }
        else
        {
            if (debugMode) Debug.Log("Tüm bebekler zaten yakılmış veya progress image listesi dolu.");
        }
    }

    private IEnumerator AnimateImageChange(Image targetImage)
    {
        if (targetImage == null) yield break;

        Vector3 originalScale = targetImage.rectTransform.localScale; // UI için RectTransform.localScale
        Vector3 punchScale = originalScale * animationScaleMultiplier;
        float halfDuration = animationDuration / 2f;
        float elapsedTime = 0f;

        // Scale up
        while (elapsedTime < halfDuration)
        {
            targetImage.rectTransform.localScale = Vector3.Lerp(originalScale, punchScale, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        targetImage.rectTransform.localScale = punchScale;

        // Scale down
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            targetImage.rectTransform.localScale = Vector3.Lerp(punchScale, originalScale, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        targetImage.rectTransform.localScale = originalScale;
        activeAnimations.Remove(targetImage);
    }

    private void UpdateProgressUI(bool animateChanges = false) // Animasyonlu güncelleme için opsiyonel parametre
    {
        if (progressImages == null || burnedSprite == null) 
        {
            if (debugMode && progressImages == null) Debug.LogWarning("UpdateProgressUI: Progress Images listesi null.");
            if (debugMode && burnedSprite == null) Debug.LogWarning("UpdateProgressUI: Burned Sprite null.");
            return;
        }

        for (int i = 0; i < progressImages.Count; i++)
        {
            if (progressImages[i] != null)
            {
                if (i < burnedCount) 
                {
                    bool shouldAnimate = animateChanges && progressImages[i].sprite != burnedSprite;
                    progressImages[i].sprite = burnedSprite;
                    if (shouldAnimate && useSpriteChangeAnimation)
                    {
                        if (activeAnimations.TryGetValue(progressImages[i], out Coroutine existingAnim) && existingAnim != null)
                        {
                            StopCoroutine(existingAnim);
                        }
                        activeAnimations[progressImages[i]] = StartCoroutine(AnimateImageChange(progressImages[i]));
                    }
                }
            }
        }
        if (debugMode) Debug.Log($"UI güncellendi. {burnedCount} bebek yanmış olarak ayarlandı. Animasyon: {animateChanges}");
    }

    public static void ResetBurnProgress()
    {        
        burnedCount = 0;
        // UI'ı anında güncellemek için (animasyonsuz)
        BurnProgressUIController instance = FindObjectOfType<BurnProgressUIController>();
        if (instance != null)
        {
            instance.UpdateProgressUI(false); 
        }
        Debug.Log("BurnProgressUIController: Bebek yakma ilerlemesi (burnedCount) sıfırlandı.");
    }
} 