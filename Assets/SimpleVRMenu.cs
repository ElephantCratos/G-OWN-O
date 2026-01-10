using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace VRMenu
{
    public class SimpleVRMenu : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject mainMenuPanel;
        public GameObject settingsPanel;
        
        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip clickSound;
        public Slider volumeSlider;
        public TextMeshProUGUI volumeValueText;
        
        [Header("Scene")]
        public string gameSceneName = "Game";
        
        private const string PREF_VOLUME = "MasterVolume";

        void Start()
        {
            // Загрузить сохранённую громкость
            float savedVolume = PlayerPrefs.GetFloat(PREF_VOLUME, 1f);
            
            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.value = savedVolume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            
            AudioListener.volume = savedVolume;
            UpdateVolumeText(savedVolume);
            
            ShowMainMenu();
        }

        // === НАВИГАЦИЯ ===

        public void ShowMainMenu()
        {
            mainMenuPanel.SetActive(true);
            settingsPanel.SetActive(false);
        }

        public void ShowSettings()
        {
            PlayClick();
            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        public void BackToMenu()
        {
            PlayClick();
            PlayerPrefs.SetFloat(PREF_VOLUME, volumeSlider.value);
            PlayerPrefs.Save();
            ShowMainMenu();
        }

        // === КНОПКИ ГЛАВНОГО МЕНЮ ===

        public void OnNewGame()
        {
            PlayClick();
            SceneManager.LoadScene(gameSceneName);
        }

        public void OnLoadGame()
        {
            PlayClick();
            Debug.Log("Загрузка — не реализовано");
        }

        public void OnExit()
        {
            PlayClick();
            PlayerPrefs.Save();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        // === ЗВУК ===

        void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            UpdateVolumeText(value);
        }

        void UpdateVolumeText(float value)
        {
            if (volumeValueText != null)
                volumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        void PlayClick()
        {
            if (audioSource != null && clickSound != null)
                audioSource.PlayOneShot(clickSound);
        }
    }
}
