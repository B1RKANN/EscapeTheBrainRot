using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Eğer UI elementleri kullanıyorsanız (Button gibi)

public class BurnMechanismController : MonoBehaviour
{
    [Header("UI Referansları")]
    [SerializeField] private GameObject burnedButton; // Canvas'taki yakma butonu

    [Header("Obje Referansları")]
    [SerializeField] private GameObject inventoryBabySahur; // Oyuncunun kamerasındaki/envanterindeki Bebek Sahur
    [SerializeField] private GameObject fireBabySahur;    // Ateşe verilecek Bebek Sahur modeli
    [SerializeField] private GameObject vfxFire;          // Ateş görsel efekti

    [Header("Ayarlar")]
    [SerializeField] private float delayToShowVFX = 2f;
    [SerializeField] private float delayToHideFireBabySahur = 2f; // vfx göründükten sonraki gecikme
    [SerializeField] private float delayToHideVFX = 1f;         // fireBabySahur gizlendikten sonraki gecikme
    
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
        if (debugMode) Debug.Log("FireBabySahur aktifleştirildi.");

        // Eğer önceki bir yakma işlemi varsa durdur
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
        }
        burnCoroutine = StartCoroutine(BurnSequenceCoroutine());
    }

    private IEnumerator BurnSequenceCoroutine()
    {
        if (debugMode) Debug.Log("Yakma sekansı başlatıldı.");

        // 2 saniye sonra VFX_Fire objesinin aktifliği açılacak
        yield return new WaitForSeconds(delayToShowVFX);
        if (vfxFire != null)
        {
            vfxFire.SetActive(true);
            if (debugMode) Debug.Log("VFX_Fire aktifleştirildi.");
        }

        // 2 saniye sonra FireBabySahur aktifliği kapanacak
        yield return new WaitForSeconds(delayToHideFireBabySahur);
        if (fireBabySahur != null)
        {
            fireBabySahur.SetActive(false);
            if (debugMode) Debug.Log("FireBabySahur pasifleştirildi.");
        }

        // 1 saniye sonra ise VFX_Fire aktifliği kapanacak
        yield return new WaitForSeconds(delayToHideVFX);
        if (vfxFire != null)
        {
            vfxFire.SetActive(false);
            if (debugMode) Debug.Log("VFX_Fire pasifleştirildi.");
        }
        
        burnCoroutine = null; // Coroutine tamamlandı
        if (debugMode) Debug.Log("Yakma sekansı tamamlandı.");
    }
} 