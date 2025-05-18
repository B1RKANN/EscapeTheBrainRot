using UnityEngine;
using UnityEngine.UI; // UI elemanlarını kullanmak için bu namespace gerekli

namespace EscapeTheBrainRot
{
    public class PlayerSprintAndStamina : MonoBehaviour
    {
        [Header("Referanslar")]
        public PlayerMove playerMove; // PlayerMove scriptine referans
        public Image staminaBarImage; // Canvas'taki stamina barının Image bileşeni

        [Header("Stamina Ayarları")]
        public float maxStamina = 100f;
        public float currentStamina;
        public float staminaConsumeRate = 10f; // Saniyede ne kadar stamina tüketeceği
        public float staminaRegenRate = 5f;  // Saniyede ne kadar stamina yenileyeceği
        public float staminaRegenDelay = 2f; // Koşmayı bıraktıktan ne kadar sonra stamina yenilenmeye başlar
        private float timeSinceLastSprint = 0f;

        [Header("Koşma Ayarları")]
        public float sprintSpeedMultiplier = 1.5f; // Normal yürüme hızının kaç katı hızla koşacak
        // public KeyCode sprintKey = KeyCode.LeftShift; // Kaldırıldı
        public bool sprintInputActive = false; // UI Butonundan kontrol edilecek
        private float originalSpeed;
        private bool isSprinting = false;

        void Start()
        {
            if (playerMove == null)
            {
                playerMove = GetComponent<PlayerMove>();
                if (playerMove == null)
                {
                    Debug.LogError("PlayerMove scripti bulunamadı!");
                    enabled = false; // Scripti devre dışı bırak
                    return;
                }
            }

            if (staminaBarImage == null)
            {
                Debug.LogError("Stamina Bar Image atanmamış! Lütfen Inspector'dan atayın.");
            }

            originalSpeed = playerMove.SpeedMove;
            currentStamina = maxStamina;
            UpdateStaminaUI();
        }

        void Update()
        {
            HandleSprintInput();
            
            if (isSprinting)
            {
                ConsumeStamina();
            }
            else
            {
                RegenerateStamina();
            }

            UpdateStaminaUI();
        }

        void HandleSprintInput()
        {
            // bool wantsToSprint = Input.GetKey(sprintKey); // Kaldırıldı
            bool wantsToSprint = sprintInputActive; 

            // if (wantsToSprint && currentStamina > 0 && playerMove.IsGrounded()) // Eski hali
            if (wantsToSprint && currentStamina > 0) // IsGrounded kontrolü kaldırıldı
            {
                if (!isSprinting)
                {
                    StartSprinting();
                }
            }
            else
            {
                if (isSprinting)
                {
                    StopSprinting();
                }
            }
        }

        void StartSprinting()
        {
            isSprinting = true;
            playerMove.SpeedMove = originalSpeed * sprintSpeedMultiplier;
            timeSinceLastSprint = 0f; 
        }

        void StopSprinting()
        {
            isSprinting = false;
            playerMove.SpeedMove = originalSpeed;
        }

        void ConsumeStamina()
        {
            if (currentStamina > 0)
            {
                currentStamina -= staminaConsumeRate * Time.deltaTime;
                if (currentStamina < 0)
                {
                    currentStamina = 0;
                    StopSprinting(); 
                }
            }
        }

        void RegenerateStamina()
        {
            if (!isSprinting)
            {
                timeSinceLastSprint += Time.deltaTime;
                if (timeSinceLastSprint >= staminaRegenDelay && currentStamina < maxStamina)
                {
                    currentStamina += staminaRegenRate * Time.deltaTime;
                    if (currentStamina > maxStamina)
                    {
                        currentStamina = maxStamina;
                    }
                }
            }
        }

        void UpdateStaminaUI()
        {
            if (staminaBarImage != null)
            {
                staminaBarImage.fillAmount = currentStamina / maxStamina;
            }
        }

        // Bu metodlar UI Butonunun EventTrigger'ı ile çağrılacak
        public void SetSprintInputActive(bool isActive)
        {
            sprintInputActive = isActive;
        }
    }
} 