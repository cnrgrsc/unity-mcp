using UnityEngine;

namespace BlockBlast
{
    /// <summary>
    /// Main game controller managing game state, scoring, and flow.
    /// Singleton pattern for easy access from other components.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Game Settings")]
        [SerializeField] private int pointsPerBlock = 10;
        [SerializeField] private int pointsPerLine = 100;
        [SerializeField] private int comboMultiplierBonus = 50;
        
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private ShapeSpawner shapeSpawner;
        [SerializeField] private UIManager uiManager;
        
        // Game state
        private int currentScore;
        private int bestScore;
        private int currentCombo;
        private GameState gameState;
        
        public int CurrentScore => currentScore;
        public int BestScore => bestScore;
        public int CurrentCombo => currentCombo;
        public GameState State => gameState;
        
        // Events
        public System.Action<int> OnScoreChanged;
        public System.Action<int> OnComboChanged;
        public System.Action<GameState> OnGameStateChanged;
        
        private const string BEST_SCORE_KEY = "BlockBlast_BestScore";
        
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            LoadBestScore();
        }
        
        private void Start()
        {
            FindComponents();
            SubscribeToEvents();
            StartGame();
        }
        
        private void FindComponents()
        {
            if (gridManager == null)
                gridManager = FindObjectOfType<GridManager>();
            if (shapeSpawner == null)
                shapeSpawner = FindObjectOfType<ShapeSpawner>();
            if (uiManager == null)
                uiManager = FindObjectOfType<UIManager>();
        }
        
        private void SubscribeToEvents()
        {
            if (gridManager != null)
            {
                gridManager.OnLinesCleared += HandleLinesCleared;
                gridManager.OnBlocksCleared += HandleBlocksCleared;
            }
            
            if (shapeSpawner != null)
            {
                shapeSpawner.OnNoValidMoves += HandleGameOver;
                shapeSpawner.OnAllShapesPlaced += HandleBatchComplete;
            }
        }
        
        private void OnDestroy()
        {
            if (gridManager != null)
            {
                gridManager.OnLinesCleared -= HandleLinesCleared;
                gridManager.OnBlocksCleared -= HandleBlocksCleared;
            }
            
            if (shapeSpawner != null)
            {
                shapeSpawner.OnNoValidMoves -= HandleGameOver;
                shapeSpawner.OnAllShapesPlaced -= HandleBatchComplete;
            }
        }
        
        /// <summary>
        /// Start a new game
        /// </summary>
        public void StartGame()
        {
            currentScore = 0;
            currentCombo = 0;
            gameState = GameState.Playing;
            
            gridManager?.ClearGrid();
            shapeSpawner?.SpawnNewBatch();
            
            OnScoreChanged?.Invoke(currentScore);
            OnComboChanged?.Invoke(currentCombo);
            OnGameStateChanged?.Invoke(gameState);
            
            uiManager?.ShowGameUI();
        }
        
        /// <summary>
        /// Restart the current game
        /// </summary>
        public void RestartGame()
        {
            shapeSpawner?.ClearAllShapes();
            StartGame();
        }
        
        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (gameState != GameState.Playing) return;
            
            gameState = GameState.Paused;
            Time.timeScale = 0f;
            OnGameStateChanged?.Invoke(gameState);
            uiManager?.ShowPausePanel();
        }
        
        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (gameState != GameState.Paused) return;
            
            gameState = GameState.Playing;
            Time.timeScale = 1f;
            OnGameStateChanged?.Invoke(gameState);
            uiManager?.HidePausePanel();
        }
        
        /// <summary>
        /// Handle lines cleared event
        /// </summary>
        private void HandleLinesCleared(int rows, int columns)
        {
            int totalLines = rows + columns;
            
            if (totalLines > 0)
            {
                // Increment combo
                currentCombo++;
                
                // Calculate score
                int lineScore = totalLines * pointsPerLine;
                int comboBonus = (currentCombo - 1) * comboMultiplierBonus;
                
                AddScore(lineScore + comboBonus);
                OnComboChanged?.Invoke(currentCombo);
                
                // Visual feedback for combo
                if (currentCombo > 1)
                {
                    uiManager?.ShowComboText(currentCombo);
                }
            }
        }
        
        /// <summary>
        /// Handle blocks cleared event
        /// </summary>
        private void HandleBlocksCleared(int blockCount)
        {
            AddScore(blockCount * pointsPerBlock);
        }
        
        /// <summary>
        /// Handle batch complete (all 3 shapes placed)
        /// </summary>
        private void HandleBatchComplete()
        {
            // Reset combo when new batch spawns
            currentCombo = 0;
            OnComboChanged?.Invoke(currentCombo);
        }
        
        /// <summary>
        /// Handle game over
        /// </summary>
        private void HandleGameOver()
        {
            gameState = GameState.GameOver;
            OnGameStateChanged?.Invoke(gameState);
            
            // Update best score
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                SaveBestScore();
            }
            
            uiManager?.ShowGameOverPanel(currentScore, bestScore);
            AudioManager.Instance?.PlaySound("gameover");
        }
        
        /// <summary>
        /// Add points to the score
        /// </summary>
        private void AddScore(int points)
        {
            currentScore += points;
            OnScoreChanged?.Invoke(currentScore);
            
            // Update best score in real-time
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
            }
        }
        
        /// <summary>
        /// Load best score from PlayerPrefs
        /// </summary>
        private void LoadBestScore()
        {
            bestScore = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);
        }
        
        /// <summary>
        /// Save best score to PlayerPrefs
        /// </summary>
        private void SaveBestScore()
        {
            PlayerPrefs.SetInt(BEST_SCORE_KEY, bestScore);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Go to main menu
        /// </summary>
        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            gameState = GameState.MainMenu;
            OnGameStateChanged?.Invoke(gameState);
            uiManager?.ShowMainMenu();
        }
    }
    
    /// <summary>
    /// Game state enum
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }
}
