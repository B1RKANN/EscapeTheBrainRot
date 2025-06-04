using UnityEngine;
using System.Collections.Generic; // Listeler için eklenebilir, ama dizi yeterli olacak
using System.Linq;

public class KeyPlacementManager : MonoBehaviour
{
    [Header("Global Sahne Referansları")]
    [Tooltip("Sahnedeki potansiyel tüm anahtar objeleri. Oyun başında bunlardan biri rastgele seçilecek, diğerleri yok edilecek.")]
    [SerializeField] private GameObject[] allPossibleKeyObjects;

    [Tooltip("Oyuncunun anahtarı alabileceği global UI Butonu.")]
    [SerializeField] private GameObject takeKeyButton;

    [Tooltip("Oyuncunun envanterinde/kamerasında belirecek olan anahtar objesi.")]
    [SerializeField] private GameObject playerKeyObject;

    private GameObject activeKeyObjectInScene; // Oyun için seçilen ve aktif olan anahtar

    void Start()
    {
        // Global UI ve Oyuncu Anahtar Referans Kontrolü
        if (takeKeyButton == null || playerKeyObject == null)
        {
            Debug.LogError("KeyPlacementManager: 'Take Key Button' veya 'Player Key Object' atanmamış! Lütfen Inspector'dan atayın.", this);
            return;
        }

        // Potansiyel Anahtar Dizisi Kontrolü
        if (allPossibleKeyObjects == null || allPossibleKeyObjects.Length == 0)
        {
            Debug.LogError("KeyPlacementManager: 'All Possible Key Objects' dizisi atanmamış veya boş! Lütfen Inspector'dan en az bir anahtar objesi atayın.", this);
            return;
        }
        for (int i = 0; i < allPossibleKeyObjects.Length; i++)
        {
            if (allPossibleKeyObjects[i] == null)
            {
                Debug.LogError($"KeyPlacementManager: 'All Possible Key Objects' dizisindeki {i}. eleman atanmamış! Lütfen kontrol edin.", this);
                return;
            }
        }

        // Rastgele bir anahtar seç
        int randomIndex = Random.Range(0, allPossibleKeyObjects.Length);
        activeKeyObjectInScene = allPossibleKeyObjects[randomIndex];

        if (activeKeyObjectInScene == null)
        {
            Debug.LogError($"KeyPlacementManager: Rastgele seçilen anahtar (allPossibleKeyObjects[{randomIndex}]) null! Lütfen Inspector'daki diziyi kontrol edin.", this);
            return;
        }

        DrawerController keyOwningDrawerController = null;
        int keyOwningCompartmentIndex = -1;

        // Seçilen anahtarın hangi DrawerController ve kompartımana ait olduğunu bul
        // Varsayım: Anahtar objesi, ait olduğu kompartımanın Transform'unun doğrudan alt objesidir.
        DrawerController[] allDrawerControllers = FindObjectsOfType<DrawerController>();
        if (allDrawerControllers == null || allDrawerControllers.Length == 0)
        {
            Debug.LogError("KeyPlacementManager: Sahnede hiç DrawerController bulunamadı! Anahtar ilişkilendirilemiyor.", this);
            return;
        }

        bool ownerFound = false;
        foreach (DrawerController controller in allDrawerControllers)
        {
            if (ownerFound) break;
            for (int i = 0; i < controller.GetCompartmentCount(); i++)
            {
                Transform compartmentTransform = controller.GetCompartmentTransform(i);
                
                // Detaylı loglama eklendi
                if (compartmentTransform != null && activeKeyObjectInScene != null)
                {
                    string keyParentName = (activeKeyObjectInScene.transform.parent != null) ? activeKeyObjectInScene.transform.parent.name : "NULL_PARENT";
                    Debug.Log($"[KeyPlacementManager DEBUG] Kontrol ediliyor: Seçilen Anahtar='{activeKeyObjectInScene.name}', Anahtarın Parent'ı='{keyParentName}'. Karşılaştırılan Kompartıman='{compartmentTransform.name}', Ait Olduğu Çekmece='{controller.gameObject.name}', Kompartıman Index={i}");

                    if (activeKeyObjectInScene.transform.parent == compartmentTransform)
                    {
                        keyOwningDrawerController = controller;
                        keyOwningCompartmentIndex = i;
                        ownerFound = true;
                        Debug.Log($"[KeyPlacementManager DEBUG] EŞLEŞME BULUNDU: Anahtar='{activeKeyObjectInScene.name}' Çekmece='{controller.gameObject.name}' Kompartıman='{compartmentTransform.name}' (Index {i}) ile eşleşti.", activeKeyObjectInScene);
                        break; // Bu controller için iç döngüden çık
                    }
                }
                else if (compartmentTransform == null)
                {
                    Debug.LogWarning($"[KeyPlacementManager DEBUG] Uyarı: DrawerController '{controller.gameObject.name}' için {i}. kompartımanın transformu null.", controller.gameObject);
                }
                // activeKeyObjectInScene null olmamalı çünkü yukarıda kontrol ediliyor.
            }
        }

        if (!ownerFound)
        {
            Debug.LogError($"KeyPlacementManager: Seçilen anahtar '{activeKeyObjectInScene.name}' için sahip olan DrawerController/Kompartıman bulunamadı! Anahtarın, bir kompartımanın Transform'unun altında doğru şekilde parent edildiğinden emin olun. Anahtarın parent'ı: {(activeKeyObjectInScene.transform.parent != null ? activeKeyObjectInScene.transform.parent.name : "NULL")}", activeKeyObjectInScene);
            return;
        }

        // Tüm DrawerController'ların anahtar durumunu ayarla
        foreach (DrawerController controller in allDrawerControllers)
        {
            if (controller == keyOwningDrawerController)
            {
                controller.PrepareForNewKeyState(true, keyOwningCompartmentIndex, activeKeyObjectInScene, takeKeyButton, playerKeyObject);
            }
            else
            {
                controller.PrepareForNewKeyState(false);
            }
        }
        
        // Diğer potansiyel anahtar objelerini yok et
        for (int i = 0; i < allPossibleKeyObjects.Length; i++)
        {
            if (i != randomIndex && allPossibleKeyObjects[i] != null) 
            {
                Destroy(allPossibleKeyObjects[i]);
            }
        }
        
        // Seçilen anahtarı, mevcut pozisyonunda ve rotasyonunda aktif et
        if (activeKeyObjectInScene != null) 
        {
            activeKeyObjectInScene.SetActive(true); 
            Debug.Log($"KeyPlacementManager: Önceden yerleştirilmiş aktif anahtar modeli '{activeKeyObjectInScene.name}' seçildi. Sahip: '{keyOwningDrawerController.gameObject.name}', Kompartıman No: {keyOwningCompartmentIndex + 1}. Transformuna dokunulmadı.", activeKeyObjectInScene);
        }
        // else durumu yukarıda handle edildi (activeKeyObjectInScene null ise return).
        
        if (takeKeyButton != null)
        {
            takeKeyButton.SetActive(false); 
        }
        if (playerKeyObject != null)
        {
            playerKeyObject.SetActive(false); 
        }
    }
} 