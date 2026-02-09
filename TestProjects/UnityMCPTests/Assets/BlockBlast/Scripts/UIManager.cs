using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlockBlast
{
    /// <summary>
    /// Manages all UI elements including menus, HUD, and panels.
    /// Handles UI animations and transitions.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameHUDPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject settingsPanel;
        
        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI comboText;
        
        [Header("Main Menu Elements")]
        [SerializeField] private TextMeshProUGUI menuBestScoreText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        
        [Header("Game Over Elements")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI gameOverBestScoreText;
        [SerializeField] private TextMeshProUGUI newBestText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        
        [Header("Pause Elements")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseMenuButton;
        [SerializeField] private Button pauseRestartButton;
        
        [Header("Settings")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button settingsCloseButton;
        
        [Header("Animation Settings")]
        [SerializeField] private float panelFadeDuration = 0.3f;
        
        private GameManager gameManager;
        
        private void Awake()
        {
            SetupButtons();
        }
        
        private void Start()
        {
            gameManager = GameManager.Instance;
            
            if (gameManager != null)
            {
                gameManager.OnScoreChanged += UpdateScore;
                gameManager.OnComboChanged += UpdateCombo;
            }
            
            ShowMainMenu();
        }
        
        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnScoreChanged -= UpdateScore;
                gameManager.OnComboChanged -= UpdateCombo;
            }
        }
        
        private void SetupButtons()
        {
            // Main Menu
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            
            // Game Over
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);
            
            // Pause
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
            if (pauseMenuButton != null)
                pauseMenuButton.onClick.AddListener(OnMenuClicked);
            if (pauseRestartButton != null)
                pauseRestartButton.onClick.AddListener(OnRestartClicked);
            
            // Settings
            if (settingsCloseButton != null)
                settingsCloseButton.onClick.AddListener(OnSettingsCloseClicked);
            if (musicSlider != null)
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        
        #region Panel Control
        
        /// <summary>
        /// Show main menu panel
        /// </summary>
        public void ShowMainMenu()
        {
            HideAllPanels();
            SetPanelActive(mainMenuPanel, true);
            
            // Update best score display
            if (menuBestScoreText != null && gameManager != null)
                menuBestScoreText.text = $"Best: {gameManager.BestScore}";
        }
        
        /// <summary>
        /// Show game HUD
        /// </summary>
        public void ShowGameUI()
        {
            HideAllPanels();
            SetPanelActive(gameHUDPanel, true);
            
            UpdateScore(0);
            UpdateBestScore();
            HideComboText();
        }
        
        /// <summary>
        /// Show pause panel
        /// </summary>
        public void ShowPausePanel()
        {
            SetPanelActive(pausePanel, true);
        }
        
        /// <summary>
        /// Hide pause panel
        /// </summary>
        public void HidePausePanel()
        {
            SetPanelActive(pausePanel, false);
        }
        
        /// <summary>
        /// Show game over panel
        /// </summary>
        public void ShowGameOverPanel(int finalScore, int bestScore)
        {
            SetPanelActive(gameOverPanel, true);
            
            if (finalScoreText != null)
                finalScoreText.text = $"Score: {finalScore}";
            
            if (gameOverBestScoreText != null)
                gameOverBestScoreText.text = $"Best: {bestScore}";
            
            if (newBestText != null)
                newBestText.gameObject.SetActive(finalScore >= bestScore && finalScore > 0);
        }
        
        /// <summary>
        /// Show settings panel
        /// </summary>
        public void ShowSettingsPanel()
        {
            SetPanelActive(settingsPanel, true);
            
            // Load current volume settings
            if (musicSlider != null)
                musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            if (sfxSlider != null)
                sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }
        
        /// <summary>
        /// Hide all panels
        /// </summary>
        private void HideAllPanels()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(gameHUDPanel, false);
            SetPanelActive(pausePanel, false);
            SetPanelActive(gameOverPanel, false);
            SetPanelActive(settingsPanel, false);
        }
        
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
        
        #endregion
        
        #region UI Updates
        
        /// <summary>
        /// Update score display
        /// </summary>
        public void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = score.ToString();
        }
        
        /// <summary>
        /// Update best score display
        /// </summary>
        public void UpdateBestScore()
        {
            if (bestScoreText != null && gameManager != null)
                bestScoreText.text = $"Best: {gameManager.BestScore}";
        }
        
        /// <summary>
        /// Update combo display
        /// </summary>
        public void UpdateCombo(int combo)
        {
            if (combo > 1)
            {
                ShowComboText(combo);
            }
            else
            {
                HideComboText();
            }
        }
        
        /// <summary>
        /// Show combo text with animation
        /// </summary>
        public void ShowComboText(int combo)
        {
            if (comboText == null) return;
            
            comboText.gameObject.SetActive(true);
            comboText.text = $"COMBO x{combo}!";
            
            // Simple scale punch animation
            StartCoroutine(ComboAnimation());
        }
        
        private System.Collections.IEnumerator ComboAnimation()
        {
            if (comboText == null) yield break;
            
            var rt = comboText.rectTransform;
            Vector3 originalScale = Vector3.one;
            Vector3 punchScale = Vector3.one * 1.3f;
            
            // Scale up
            float t = 0;
            while (t < 0.1f)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(originalScale, punchScale, t / 0.1f);
                yield return null;
            }
            
            // Scale down
            t = 0;
            while (t < 0.15f)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(punchScale, originalScale, t / 0.15f);
                yield return null;
            }
            
            rt.localScale = originalScale;
        }
        
        /// <summary>
        /// Hide combo text
        /// </summary>
        public void HideComboText()
        {
            if (comboText != null)
                comboText.gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnPlayClicked()
        {
            AudioManager.Instance?.PlaySound("click");
            gameManager?.StartGame();
        }
        
        private void OnRestartClicked()
        {
            AudioManager.Instance?.PlaySound("click");
            HideAllPanels();
            SetPanelActive(gameHUDPanel, true);
            gameManager?.RestartGame();
        }
        
        private void OnMenuClicked()
        {
            AudioManager.Instance?.PlaySound("click");
            gameManager?.GoToMainMenu();
        }
        
        private void OnResumeClicked()
        {
            AudioManager.Instance?.PlaySound("click");
            gameManager?.ResumeGame();
        }
        
        private void OnSettingsClicked()
        {
            AudioManager.Instance?.PlaySound("click");
            ShowSettingsPanel();
        }
        
        private void OnSettingsCloseClicked()
        {
            AudioManager.Instance?.PlaySound("click");
            SetPanelActive(settingsPanel, false);
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume", value);
            AudioManager.Instance?.SetMusicVolume(value);
        }
        
        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            AudioManager.Instance?.SetSFXVolume(value);
        }
        
        #endregion
    }
}
