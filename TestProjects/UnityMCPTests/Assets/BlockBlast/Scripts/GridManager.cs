using System.Collections.Generic;
using UnityEngine;

namespace BlockBlast
{
    /// <summary>
    /// Manages the game grid, block placement, and line clearing mechanics.
    /// Core game logic for Block Blast puzzle game.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 8;
        [SerializeField] private int gridHeight = 8;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 gridOffset = Vector2.zero;
        
        [Header("Visual Settings")]
        [SerializeField] private GameObject cellBackgroundPrefab;
        [SerializeField] private Color emptyCellColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color highlightColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        [SerializeField] private Color invalidColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);
        
        [Header("References")]
        [SerializeField] private Transform gridParent;
        
        // Grid data
        private BlockPiece[,] grid;
        private SpriteRenderer[,] cellBackgrounds;
        
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float CellSize => cellSize;
        
        // Events
        public System.Action<int, int> OnLinesCleared; // rows, columns cleared
        public System.Action<int> OnBlocksCleared; // total blocks cleared
        
        private void Awake()
        {
            InitializeGrid();
        }
        
        /// <summary>
        /// Initialize the game grid
        /// </summary>
        public void InitializeGrid()
        {
            grid = new BlockPiece[gridWidth, gridHeight];
            cellBackgrounds = new SpriteRenderer[gridWidth, gridHeight];
            
            if (gridParent == null)
            {
                gridParent = new GameObject("GridParent").transform;
                gridParent.SetParent(transform);
            }
            
            // Center the grid
            float gridWorldWidth = gridWidth * cellSize;
            float gridWorldHeight = gridHeight * cellSize;
            gridOffset = new Vector2(-gridWorldWidth / 2f + cellSize / 2f, -gridWorldHeight / 2f + cellSize / 2f);
            
            CreateGridVisuals();
        }
        
        /// <summary>
        /// Create visual background for grid cells
        /// </summary>
        private void CreateGridVisuals()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 worldPos = GridToWorldPosition(new Vector2Int(x, y));
                    
                    GameObject cellGO;
                    if (cellBackgroundPrefab != null)
                    {
                        cellGO = Instantiate(cellBackgroundPrefab, worldPos, Quaternion.identity, gridParent);
                    }
                    else
                    {
                        cellGO = new GameObject($"Cell_{x}_{y}");
                        cellGO.transform.SetParent(gridParent);
                        cellGO.transform.position = worldPos;
                        
                        var sr = cellGO.AddComponent<SpriteRenderer>();
                        sr.sprite = CreateCellSprite();
                        sr.color = emptyCellColor;
                        sr.sortingOrder = 0;
                        
                        cellBackgrounds[x, y] = sr;
                    }
                    
                    cellGO.transform.localScale = Vector3.one * cellSize * 0.95f;
                }
            }
        }
        
        /// <summary>
        /// Create a simple cell background sprite
        /// </summary>
        private Sprite CreateCellSprite()
        {
            var texture = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % 32;
                int y = i / 32;
                
                // Rounded corners effect
                float cornerRadius = 4f;
                bool isCorner = (x < cornerRadius && y < cornerRadius) ||
                               (x > 31 - cornerRadius && y < cornerRadius) ||
                               (x < cornerRadius && y > 31 - cornerRadius) ||
                               (x > 31 - cornerRadius && y > 31 - cornerRadius);
                
                if (isCorner)
                {
                    float dx = x < 16 ? x - cornerRadius : 31 - x - cornerRadius;
                    float dy = y < 16 ? y - cornerRadius : 31 - y - cornerRadius;
                    if (dx < 0 && dy < 0 && dx * dx + dy * dy > cornerRadius * cornerRadius)
                    {
                        pixels[i] = Color.clear;
                        continue;
                    }
                }
                
                pixels[i] = Color.white;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }
        
        /// <summary>
        /// Convert grid position to world position
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return new Vector3(
                gridPos.x * cellSize + gridOffset.x + transform.position.x,
                gridPos.y * cellSize + gridOffset.y + transform.position.y,
                0
            );
        }
        
        /// <summary>
        /// Convert world position to grid position
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            float localX = worldPos.x - transform.position.x - gridOffset.x + cellSize / 2f;
            float localY = worldPos.y - transform.position.y - gridOffset.y + cellSize / 2f;
            
            return new Vector2Int(
                Mathf.FloorToInt(localX / cellSize),
                Mathf.FloorToInt(localY / cellSize)
            );
        }
        
        /// <summary>
        /// Check if a position is within grid bounds
        /// </summary>
        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
        }
        
        /// <summary>
        /// Check if a cell is empty
        /// </summary>
        public bool IsCellEmpty(Vector2Int pos)
        {
            if (!IsValidPosition(pos)) return false;
            return grid[pos.x, pos.y] == null;
        }
        
        /// <summary>
        /// Check if a shape can be placed at a specific position
        /// </summary>
        public bool CanPlaceShapeAt(List<Vector2Int> shapeOffsets, Vector2Int basePos)
        {
            foreach (var offset in shapeOffsets)
            {
                Vector2Int cellPos = basePos + offset;
                if (!IsValidPosition(cellPos) || !IsCellEmpty(cellPos))
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Try to place a shape on the grid
        /// </summary>
        public bool TryPlaceShape(BlockShape shape, Vector2Int gridPos)
        {
            if (!CanPlaceShapeAt(shape.ShapeOffsets, gridPos))
                return false;
            
            // Place each piece
            var pieces = shape.GetPieces();
            int pieceIndex = 0;
            
            foreach (var offset in shape.ShapeOffsets)
            {
                Vector2Int cellPos = gridPos + offset;
                
                if (pieceIndex < pieces.Count)
                {
                    var piece = pieces[pieceIndex];
                    piece.transform.SetParent(gridParent);
                    piece.transform.position = GridToWorldPosition(cellPos);
                    piece.transform.localScale = Vector3.one * cellSize * 0.9f;
                    piece.GridPosition = cellPos;
                    piece.SetSortingOrder(5);
                    
                    grid[cellPos.x, cellPos.y] = piece;
                    pieceIndex++;
                }
            }
            
            // Detach shape container (leave pieces on grid)
            Destroy(shape.gameObject);
            
            // Check for completed lines
            CheckAndClearLines();
            
            return true;
        }
        
        /// <summary>
        /// Check for completed rows and columns, then clear them
        /// </summary>
        public void CheckAndClearLines()
        {
            List<int> rowsToClear = new List<int>();
            List<int> columnsToClear = new List<int>();
            
            // Check rows
            for (int y = 0; y < gridHeight; y++)
            {
                bool rowComplete = true;
                for (int x = 0; x < gridWidth; x++)
                {
                    if (grid[x, y] == null)
                    {
                        rowComplete = false;
                        break;
                    }
                }
                if (rowComplete)
                    rowsToClear.Add(y);
            }
            
            // Check columns
            for (int x = 0; x < gridWidth; x++)
            {
                bool columnComplete = true;
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] == null)
                    {
                        columnComplete = false;
                        break;
                    }
                }
                if (columnComplete)
                    columnsToClear.Add(x);
            }
            
            if (rowsToClear.Count == 0 && columnsToClear.Count == 0)
                return;
            
            // Mark cells to clear (avoid double-clearing)
            HashSet<Vector2Int> cellsToClear = new HashSet<Vector2Int>();
            
            foreach (int y in rowsToClear)
            {
                for (int x = 0; x < gridWidth; x++)
                    cellsToClear.Add(new Vector2Int(x, y));
            }
            
            foreach (int x in columnsToClear)
            {
                for (int y = 0; y < gridHeight; y++)
                    cellsToClear.Add(new Vector2Int(x, y));
            }
            
            // Clear cells
            int blocksCleared = 0;
            foreach (var cellPos in cellsToClear)
            {
                var piece = grid[cellPos.x, cellPos.y];
                if (piece != null)
                {
                    piece.DestroyBlock();
                    grid[cellPos.x, cellPos.y] = null;
                    blocksCleared++;
                }
            }
            
            // Fire events
            OnLinesCleared?.Invoke(rowsToClear.Count, columnsToClear.Count);
            OnBlocksCleared?.Invoke(blocksCleared);
            
            // Play clear sound
            AudioManager.Instance?.PlaySound("clear");
            
            // Combo sound for multiple lines
            if (rowsToClear.Count + columnsToClear.Count > 1)
                AudioManager.Instance?.PlaySound("combo");
        }
        
        /// <summary>
        /// Highlight cells where a shape would be placed
        /// </summary>
        public void HighlightPlacement(List<Vector2Int> shapeOffsets, Vector2Int basePos, bool isValid)
        {
            ClearHighlights();
            
            Color highlightCol = isValid ? highlightColor : invalidColor;
            
            foreach (var offset in shapeOffsets)
            {
                Vector2Int cellPos = basePos + offset;
                if (IsValidPosition(cellPos) && cellBackgrounds[cellPos.x, cellPos.y] != null)
                {
                    cellBackgrounds[cellPos.x, cellPos.y].color = highlightCol;
                }
            }
        }
        
        /// <summary>
        /// Clear all cell highlights
        /// </summary>
        public void ClearHighlights()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (cellBackgrounds[x, y] != null)
                    {
                        Color cellColor = grid[x, y] == null ? emptyCellColor : emptyCellColor;
                        cellBackgrounds[x, y].color = cellColor;
                    }
                }
            }
        }
        
        /// <summary>
        /// Clear entire grid
        /// </summary>
        public void ClearGrid()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] != null)
                    {
                        Destroy(grid[x, y].gameObject);
                        grid[x, y] = null;
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if any shape from a list can be placed
        /// </summary>
        public bool CanAnyShapeBePlaced(List<BlockShape> shapes)
        {
            foreach (var shape in shapes)
            {
                if (shape != null && !shape.IsPlaced && shape.CanBePlacedAnywhere(this))
                    return true;
            }
            return false;
        }
    }
}
