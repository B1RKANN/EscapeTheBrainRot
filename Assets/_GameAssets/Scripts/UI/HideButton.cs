using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EscapeTheBrainRot
{
    public class HideButton : MonoBehaviour
    {
        [Header("UI Bileşenleri")]
        public Button button;
        public TextMeshProUGUI buttonText;
        public GameObject buttonObject;
        
        private SpindHidingSpot currentSpind;
        
        void Start()
        {
            // Gerekli bileşenleri otomatik bul
            if (button == null)
                button = GetComponent<Button>();
            
            if (buttonText == null)
                buttonText = GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonObject == null)
                buttonObject = gameObject;
            
            // Başlangıçta butonu gizle
            HideButtonUI();
            
            // Buton tıklama olayı ekle
            if (button != null)
                button.onClick.AddListener(OnButtonClicked);
            else
                Debug.LogError("HATA: Button bileşeni bulunamadı!");
        }
        
        // Butona tıklandığında
        public void OnButtonClicked()
        {
            if (currentSpind != null)
                currentSpind.OnHideButtonPressed();
        }
        
        // Hedef dolabı ayarla ve butonu göster
        public void SetTargetSpind(SpindHidingSpot spind, string buttonLabel = "Saklan")
        {
            currentSpind = spind;
            
            // Buton metnini güncelle
            if (buttonText != null)
                buttonText.text = buttonLabel;
            
            // Butonu görünür yap
            if (buttonObject != null)
                buttonObject.SetActive(true);
        }
        
        // Butonu gizle
        public void HideButtonUI()
        {
            currentSpind = null;
            
            if (buttonObject != null)
                buttonObject.SetActive(false);
        }
    }
} 