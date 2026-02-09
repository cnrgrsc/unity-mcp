using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BlockBlast
{
    /// <summary>
    /// Sets up the Block Blast game scene with all necessary components.
    /// Run this from Unity Editor to create the complete game setup.
    /// </summary>
    public class BlockBlastSceneSetup : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("BlockBlast/Setup Scene")]
        public static void SetupScene()
        {
            // Create new scene or use current
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Setup Camera
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 8;
                mainCamera.transform.position = new Vector3(0, 0, -10);
                mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }
            
            // Create Game Container
            var gameContainer = new GameObject("GameContainer");
            
            // Create Grid Manager
            var gridGO = new GameObject("GridManager");
            gridGO.transform.SetParent(gameContainer.transform);
            gridGO.transform.position = new Vector3(0, 1, 0);
            var gridManager = gridGO.AddComponent<GridManager>();
            
            // Create Shape Spawner
            var spawnerGO = new GameObject("ShapeSpawner");
            spawnerGO.transform.SetParent(gameContainer.transform);
            spawnerGO.transform.position = new Vector3(0, -5, 0);
            var shapeSpawner = spawnerGO.AddComponent<ShapeSpawner>();
            
            // Create Game Manager
            var gameManagerGO = new GameObject("GameManager");
            gameManagerGO.transform.SetParent(gameContainer.transform);
            var gameManager = gameManagerGO.AddComponent<GameManager>();
            
            // Create Audio Manager
            var audioManagerGO = new GameObject("AudioManager");
            audioManagerGO.transform.SetParent(gameContainer.transform);
            var audioManager = audioManagerGO.AddComponent<AudioManager>();
            
            // Create UI Canvas
            CreateUICanvas(gameContainer.transform);
            
            // Save scene
            string scenePath = "Assets/BlockBlast/Scenes/BlockBlastGame.unity";
            System.IO.Directory.CreateDirectory("Assets/BlockBlast/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log("Block Blast scene created successfully at: " + scenePath);
        }
        
        private static void CreateUICanvas(Transform parent)
        {
            // Create Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Create EventSystem
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            // Add UI Manager
            var uiManager = canvasGO.AddComponent<UIManager>();
            
            // Create Main Menu Panel
            var mainMenu = CreatePanel(canvasGO.transform, "MainMenuPanel");
            CreateText(mainMenu.transform, "TitleText", "BLOCK BLAST", new Vector2(0, 300), 72);
            CreateButton(mainMenu.transform, "PlayButton", "PLAY", new Vector2(0, 0));
            CreateButton(mainMenu.transform, "SettingsButton", "SETTINGS", new Vector2(0, -100));
            CreateText(mainMenu.transform, "BestScoreText", "Best: 0", new Vector2(0, -250), 36);
            
            // Create Game HUD Panel
            var gameHUD = CreatePanel(canvasGO.transform, "GameHUDPanel");
            gameHUD.SetActive(false);
            CreateText(gameHUD.transform, "ScoreText", "0", new Vector2(0, 850), 64);
            CreateText(gameHUD.transform, "BestText", "Best: 0", new Vector2(0, 780), 32);
            CreateText(gameHUD.transform, "ComboText", "COMBO!", new Vector2(0, 600), 48);
            var pauseBtn = CreateButton(gameHUD.transform, "PauseButton", "| |", new Vector2(450, 850));
            var pauseRect = pauseBtn.GetComponent<RectTransform>();
            pauseRect.sizeDelta = new Vector2(80, 80);
            
            // Create Pause Panel
            var pausePanel = CreatePanel(canvasGO.transform, "PausePanel");
            pausePanel.SetActive(false);
            var pauseBG = pausePanel.AddComponent<UnityEngine.UI.Image>();
            pauseBG.color = new Color(0, 0, 0, 0.7f);
            CreateText(pausePanel.transform, "PausedText", "PAUSED", new Vector2(0, 200), 64);
            CreateButton(pausePanel.transform, "ResumeButton", "RESUME", new Vector2(0, 0));
            CreateButton(pausePanel.transform, "PauseRestartButton", "RESTART", new Vector2(0, -100));
            CreateButton(pausePanel.transform, "PauseMenuButton", "MAIN MENU", new Vector2(0, -200));
            
            // Create Game Over Panel
            var gameOverPanel = CreatePanel(canvasGO.transform, "GameOverPanel");
            gameOverPanel.SetActive(false);
            var gameOverBG = gameOverPanel.AddComponent<UnityEngine.UI.Image>();
            gameOverBG.color = new Color(0, 0, 0, 0.8f);
            CreateText(gameOverPanel.transform, "GameOverText", "GAME OVER", new Vector2(0, 300), 64);
            CreateText(gameOverPanel.transform, "FinalScoreText", "Score: 0", new Vector2(0, 150), 48);
            CreateText(gameOverPanel.transform, "GameOverBestText", "Best: 0", new Vector2(0, 80), 36);
            CreateText(gameOverPanel.transform, "NewBestText", "NEW BEST!", new Vector2(0, 20), 32);
            CreateButton(gameOverPanel.transform, "RestartButton", "PLAY AGAIN", new Vector2(0, -100));
            CreateButton(gameOverPanel.transform, "MenuButton", "MAIN MENU", new Vector2(0, -200));
            
            // Create Settings Panel
            var settingsPanel = CreatePanel(canvasGO.transform, "SettingsPanel");
            settingsPanel.SetActive(false);
            var settingsBG = settingsPanel.AddComponent<UnityEngine.UI.Image>();
            settingsBG.color = new Color(0, 0, 0, 0.8f);
            CreateText(settingsPanel.transform, "SettingsTitle", "SETTINGS", new Vector2(0, 300), 64);
            CreateText(settingsPanel.transform, "MusicLabel", "Music", new Vector2(-200, 100), 32);
            CreateSlider(settingsPanel.transform, "MusicSlider", new Vector2(100, 100));
            CreateText(settingsPanel.transform, "SFXLabel", "SFX", new Vector2(-200, 0), 32);
            CreateSlider(settingsPanel.transform, "SFXSlider", new Vector2(100, 0));
            CreateButton(settingsPanel.transform, "SettingsCloseButton", "CLOSE", new Vector2(0, -200));
            
            Debug.Log("UI Canvas created successfully");
        }
        
        private static GameObject CreatePanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            return panel;
        }
        
        private static GameObject CreateText(Transform parent, string name, string text, Vector2 position, int fontSize)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent);
            
            var rect = textGO.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(800, 100);
            
            var tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            return textGO;
        }
        
        private static GameObject CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent);
            
            var rect = buttonGO.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 80);
            
            var image = buttonGO.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.4f, 0.8f);
            
            var button = buttonGO.AddComponent<UnityEngine.UI.Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.9f);
            colors.pressedColor = new Color(0.15f, 0.3f, 0.6f);
            button.colors = colors;
            
            // Add text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 32;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            return buttonGO;
        }
        
        private static GameObject CreateSlider(Transform parent, string name, Vector2 position)
        {
            var sliderGO = new GameObject(name);
            sliderGO.transform.SetParent(parent);
            
            var rect = sliderGO.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 30);
            
            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGO.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            // Fill Area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform);
            var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(10, 0);
            fillAreaRect.offsetMax = new Vector2(-10, 0);
            
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillGO.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0.3f, 0.6f, 1f);
            
            // Handle
            var handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(sliderGO.transform);
            var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);
            
            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform);
            var handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            var handleImage = handleGO.AddComponent<UnityEngine.UI.Image>();
            handleImage.color = Color.white;
            
            // Slider component
            var slider = sliderGO.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.value = 1f;
            
            return sliderGO;
        }
#endif
    }
}
