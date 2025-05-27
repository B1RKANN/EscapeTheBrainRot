using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections; // IEnumerator için eklendi

public class HeartManager : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Oyuncunun sahip olacağı maksimum can sayısı.")]
    [SerializeField] private int maxHealth = 2;
    private int currentHealth;

    [Header("UI Settings")]
    [Tooltip("Parlaklığı ayarlanacak kalp UI Image elemanları. Oyuncunun ilk canından son canına doğru sıralanmalıdır. Başlangıçta deaktif olmaları beklenir.")]
    [SerializeField] private List<Image> heartImages;
    [Tooltip("Can kaybedildiğinde kalbin alacağı alfa (görünürlük) değeri.")]
    [SerializeField, Range(0f, 1f)] private float dimmedAlpha = 0.3f;
    [Tooltip("Can dolu olduğunda kalbin alacağı alfa (görünürlük) değeri.")]
    [SerializeField, Range(0f, 1f)] private float fullAlpha = 1.0f;
    [Tooltip("Bir kalbin parlaklığının azalma animasyonunun süresi (saniye).")]
    [SerializeField] private float heartDimAnimationDuration = 0.5f;


    public static HeartManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Sahne değişikliklerinde objenin yok olmamasını istiyorsanız:
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Debug.LogWarning("[HeartManager] Sahnede birden fazla HeartManager bulundu. Bu kopya yok ediliyor.", this);
            Destroy(gameObject);
            return;
        }

        currentHealth = maxHealth;
        // Kalplerin başlangıçta deaktif olduğu varsayılır, SahurAIController tarafından aktive edilecekler.
    }

    /// <summary>
    /// Kalp UI elemanlarını aktif hale getirir ve mevcut can durumuna göre parlaklıklarını ayarlar.
    /// </summary>
    public void ActivateAndDisplayHearts()
    {
        if (heartImages == null)
        {
            Debug.LogWarning("[HeartManager] HeartImages listesi atanmamış. Kalpler gösterilemiyor.", this);
            return;
        }

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] != null)
            {
                heartImages[i].gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[HeartManager] HeartImages listesindeki {i}. eleman null, aktive edilemiyor.", this);
            }
        }
        UpdateHeartUI(); // Aktive ettikten sonra parlaklıkları ayarla
        Debug.Log("[HeartManager] Kalp UI'ları aktive edildi ve gösteriliyor.", this);
    }

    /// <summary>
    /// Tüm kalp UI elemanlarını deaktif hale getirir.
    /// </summary>
    public void DeactivateHearts()
    {
        if (heartImages == null) 
        {
            Debug.LogWarning("[HeartManager] HeartImages listesi atanmamış. Kalpler deaktive edilemiyor.", this);
            return;
        }

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] != null)
            {
                heartImages[i].gameObject.SetActive(false);
            }
        }
        Debug.Log("[HeartManager] Tüm kalp UI'ları deaktive edildi.", this);
    }

    private void InitializeHeartUI() // Bu metodun adı artık yanıltıcı olabilir, UpdateHeartUI direkt kullanılacak.
    {
        // Bu metodun içeriği ActivateAndDisplayHearts ve UpdateHeartUI tarafından yönetiliyor.
        // Başlangıçta kalpler deaktif olacağı için Awake'de özel bir UI güncellemesi yapılmıyor.
    }

    /// <summary>
    /// Oyuncunun canını belirtilen miktar kadar azaltır, ilgili kalp UI'ını anime eder ve UI'ı günceller.
    /// </summary>
    /// <param name="damageAmount">Azaltılacak can miktarı.</param>
    public IEnumerator TakeDamageAndAnimate(int damageAmount = 1)
    {
        if (currentHealth <= 0)
        {
            Debug.Log("[HeartManager] Oyuncunun zaten canı yok, hasar alınamıyor.", this);
            yield break; 
        }

        int heartIndexToDim = currentHealth - 1; 

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0); 

        Debug.Log($"[HeartManager] Oyuncu hasar aldı. Kalan can: {currentHealth}/{maxHealth}", this);

        if (heartIndexToDim >= 0 && heartIndexToDim < heartImages.Count && heartImages[heartIndexToDim] != null)
        {
            if (!heartImages[heartIndexToDim].gameObject.activeSelf)
            {
                Debug.LogWarning($"[HeartManager] Azaltılacak kalp ({heartImages[heartIndexToDim].name}) deaktif. Önce aktive edilmesi gerekirdi.", this);
                // Yine de animasyonu deneyecek, ancak görünmeyebilir.
            }
            yield return StartCoroutine(AnimateHeartDim(heartImages[heartIndexToDim]));
        }
        else
        {
            Debug.LogWarning($"[HeartManager] Azaltılacak kalp için geçerli index ({heartIndexToDim}) veya Image bulunamadı. Direkt UI güncellemesi yapılıyor.", this);
            UpdateHeartUI(); 
        }

        if (currentHealth <= 0)
        {
            Debug.Log("[HeartManager] Oyuncunun canı bitti!", this);
            // Burada oyun sonu mantığı tetiklenebilir. Örneğin:
            // GameManager.Instance.GameOver();
        }
    }

    private IEnumerator AnimateHeartDim(Image heartImage)
    {
        float elapsedTime = 0f;
        Color startColor = heartImage.color;
        // Animasyon başladığında kalbin parlaklığının fullAlpha olduğunu varsayıyoruz,
        // çünkü ActivateAndDisplayHearts bunu ayarlamış olmalı.
        startColor.a = fullAlpha; 
        Color endColor = startColor;
        endColor.a = dimmedAlpha;

        heartImage.color = startColor; 

        Debug.Log($"[HeartManager] Kalp ({heartImage.name}) parlaklık animasyonu başlıyor -> {dimmedAlpha}", this);

        while (elapsedTime < heartDimAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / heartDimAnimationDuration);
            heartImage.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }
        heartImage.color = endColor; 
        Debug.Log($"[HeartManager] Kalp ({heartImage.name}) parlaklık animasyonu bitti.", this);
    }

    /// <summary>
    /// Mevcut can durumuna göre kalp UI'larını statik olarak günceller.
    /// Genellikle başlangıçta veya animasyonsuz hızlı güncellemeler için kullanılır.
    /// </summary>
    private void UpdateHeartUI()
    {
        if (heartImages == null || heartImages.Count == 0) return;

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] != null && heartImages[i].gameObject.activeSelf)
            {
                Color heartColor = heartImages[i].color;
                if (i < currentHealth) 
                {
                    heartColor.a = fullAlpha; 
                }
                else 
                {
                    heartColor.a = dimmedAlpha; 
                }
                heartImages[i].color = heartColor;
            }
            // else if (heartImages[i] != null && !heartImages[i].gameObject.activeSelf)
            // {
                 // Deaktif kalpler için bir işlem yapmaya gerek yok, zaten görünmüyorlar.
            // }
        }
    }

    /// <summary>
    /// Oyuncunun mevcut can sayısını döndürür.
    /// </summary>
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Oyuncunun maksimum can sayısını döndürür.
    /// </summary>
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    [ContextMenu("Test - Activate and Show Hearts")]
    private void TestActivateAndShow()
    {
        ActivateAndDisplayHearts();
    }

    [ContextMenu("Test - Take 1 Damage Animated")]
    private void TestTakeDamageAnimated()
    {
        if (currentHealth == maxHealth) // Eğer canlar tam ise önce göster
        {
             bool allInactive = true;
             foreach(var heart in heartImages) { if(heart.gameObject.activeSelf) { allInactive = false; break;} }
             if(allInactive) ActivateAndDisplayHearts();
        }
        StartCoroutine(TakeDamageAndAnimate(1));
    }

    [ContextMenu("Test - Reset Health (and hide hearts)")]
    private void TestResetHealthAndHide()
    {
        currentHealth = maxHealth;
        DeactivateHearts(); // Yeni metodu burada da kullanabiliriz.
        // if (heartImages != null)
        // {
        //     foreach (Image heart in heartImages)
        //     {
        //         if (heart != null) heart.gameObject.SetActive(false);
        //     }
        // }
        Debug.Log("[HeartManager] Canlar test amaçlı sıfırlandı ve kalpler gizlendi.", this);
    }
} 