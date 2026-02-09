using System.Collections.Generic;
using UnityEngine;

namespace BlockBlast
{
    /// <summary>
    /// Handles spawning and managing block shapes for the player.
    /// Generates random shapes and manages the shape queue.
    /// </summary>
    public class ShapeSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private int shapesPerBatch = 3;
        [SerializeField] private Transform[] spawnPositions;
        [SerializeField] private GameObject shapePrefab;
        
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        
        private List<BlockShape> currentShapes = new List<BlockShape>();
        
        // Pre-defined shape patterns (like Tetris)
        private static readonly List<List<Vector2Int>> ShapePatterns = new List<List<Vector2Int>>
        {
            // Single block
            new List<Vector2Int> { new Vector2Int(0, 0) },
            
            // 2x1 horizontal
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) },
            
            // 2x1 vertical
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1) },
            
            // 3x1 horizontal
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
            
            // 3x1 vertical
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) },
            
            // 2x2 square
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            
            // L shape
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0) },
            
            // Reverse L
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2) },
            
            // T shape
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) },
            
            // S shape
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) },
            
            // Z shape
            new List<Vector2Int> { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            
            // 4x1 horizontal
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) },
            
            // 4x1 vertical
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3) },
            
            // 3x3 square
            new List<Vector2Int> {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
            },
            
            // Small L corner
            new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },
            
            // Plus shape
            new List<Vector2Int> { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2) },
        };
        
        public System.Action OnAllShapesPlaced;
        public System.Action OnNoValidMoves;
        
        private void Start()
        {
            if (gridManager == null)
                gridManager = FindObjectOfType<GridManager>();
            
            SpawnNewBatch();
        }
        
        /// <summary>
        /// Spawn a new batch of shapes
        /// </summary>
        public void SpawnNewBatch()
        {
            // Clear existing shapes
            foreach (var shape in currentShapes)
            {
                if (shape != null)
                    Destroy(shape.gameObject);
            }
            currentShapes.Clear();
            
            // Create spawn positions if not set
            if (spawnPositions == null || spawnPositions.Length == 0)
            {
                spawnPositions = new Transform[shapesPerBatch];
                float spacing = 3f;
                float startX = -(shapesPerBatch - 1) * spacing / 2f;
                
                for (int i = 0; i < shapesPerBatch; i++)
                {
                    var posGO = new GameObject($"SpawnPos_{i}");
                    posGO.transform.SetParent(transform);
                    posGO.transform.position = new Vector3(startX + i * spacing, -5f, 0);
                    spawnPositions[i] = posGO.transform;
                }
            }
            
            // Spawn new shapes
            for (int i = 0; i < shapesPerBatch; i++)
            {
                var shape = SpawnRandomShape(spawnPositions[i].position);
                currentShapes.Add(shape);
            }
            
            // Check if any shape can be placed
            CheckForValidMoves();
        }
        
        /// <summary>
        /// Spawn a random shape at the given position
        /// </summary>
        private BlockShape SpawnRandomShape(Vector3 position)
        {
            // Pick random pattern
            var pattern = ShapePatterns[Random.Range(0, ShapePatterns.Count)];
            
            // Pick random color
            var color = (BlockColor)Random.Range(0, System.Enum.GetValues(typeof(BlockColor)).Length);
            
            // Create shape object
            GameObject shapeGO;
            if (shapePrefab != null)
            {
                shapeGO = Instantiate(shapePrefab, position, Quaternion.identity, transform);
            }
            else
            {
                shapeGO = new GameObject("BlockShape");
                shapeGO.transform.position = position;
                shapeGO.transform.SetParent(transform);
                
                // Add collider for mouse interaction
                var collider = shapeGO.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(3f, 3f);
                collider.isTrigger = true;
            }
            
            var blockShape = shapeGO.GetComponent<BlockShape>();
            if (blockShape == null)
                blockShape = shapeGO.AddComponent<BlockShape>();
            
            blockShape.Initialize(pattern, color, gridManager, this);
            
            return blockShape;
        }
        
        /// <summary>
        /// Called when a shape is placed on the grid
        /// </summary>
        public void OnShapePlaced(BlockShape shape)
        {
            currentShapes.Remove(shape);
            
            // Check if all shapes are placed
            bool allPlaced = true;
            foreach (var s in currentShapes)
            {
                if (s != null && !s.IsPlaced)
                {
                    allPlaced = false;
                    break;
                }
            }
            
            if (allPlaced || currentShapes.Count == 0)
            {
                // Spawn new batch after a short delay
                Invoke(nameof(SpawnNewBatch), 0.3f);
                OnAllShapesPlaced?.Invoke();
            }
            else
            {
                // Check if remaining shapes can be placed
                CheckForValidMoves();
            }
        }
        
        /// <summary>
        /// Check if any remaining shape can be placed
        /// </summary>
        private void CheckForValidMoves()
        {
            bool hasValidMove = false;
            
            foreach (var shape in currentShapes)
            {
                if (shape != null && !shape.IsPlaced && shape.CanBePlacedAnywhere(gridManager))
                {
                    hasValidMove = true;
                    break;
                }
            }
            
            if (!hasValidMove && currentShapes.Count > 0)
            {
                OnNoValidMoves?.Invoke();
            }
        }
        
        /// <summary>
        /// Get current active shapes
        /// </summary>
        public List<BlockShape> GetCurrentShapes() => currentShapes;
        
        /// <summary>
        /// Clear all current shapes
        /// </summary>
        public void ClearAllShapes()
        {
            foreach (var shape in currentShapes)
            {
                if (shape != null)
                    Destroy(shape.gameObject);
            }
            currentShapes.Clear();
        }
    }
}
