using System.Collections.Generic;
using UnityEngine;

namespace BlockBlast
{
    /// <summary>
    /// Represents a draggable block shape composed of multiple BlockPiece components.
    /// Handles drag-drop functionality and placement validation.
    /// </summary>
    public class BlockShape : MonoBehaviour
    {
        [Header("Shape Configuration")]
        [SerializeField] private List<Vector2Int> shapeOffsets = new List<Vector2Int>();
        [SerializeField] private BlockColor shapeColor = BlockColor.Red;
        [SerializeField] private GameObject blockPiecePrefab;
        
        [Header("Drag Settings")]
        [SerializeField] private float dragScale = 1.2f;
        [SerializeField] private float normalScale = 0.8f;
        [SerializeField] private float snapSpeed = 20f;
        
        private List<BlockPiece> pieces = new List<BlockPiece>();
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private bool isDragging;
        private Camera mainCamera;
        private GridManager gridManager;
        private ShapeSpawner spawner;
        
        public bool IsPlaced { get; private set; }
        public BlockColor ShapeColor => shapeColor;
        public List<Vector2Int> ShapeOffsets => shapeOffsets;
        
        private void Awake()
        {
            mainCamera = Camera.main;
        }
        
        /// <summary>
        /// Initialize the shape with specific offsets and color
        /// </summary>
        public void Initialize(List<Vector2Int> offsets, BlockColor color, GridManager grid, ShapeSpawner shapeSpawner)
        {
            shapeOffsets = new List<Vector2Int>(offsets);
            shapeColor = color;
            gridManager = grid;
            spawner = shapeSpawner;
            originalPosition = transform.position;
            originalScale = Vector3.one * normalScale;
            transform.localScale = originalScale;
            
            CreatePieces();
        }
        
        /// <summary>
        /// Create block pieces based on shape offsets
        /// </summary>
        private void CreatePieces()
        {
            foreach (var piece in pieces)
            {
                if (piece != null)
                    Destroy(piece.gameObject);
            }
            pieces.Clear();
            
            if (blockPiecePrefab == null)
            {
                // Create simple colored blocks if no prefab
                foreach (var offset in shapeOffsets)
                {
                    var pieceGO = new GameObject($"Piece_{offset.x}_{offset.y}");
                    pieceGO.transform.SetParent(transform);
                    pieceGO.transform.localPosition = new Vector3(offset.x, offset.y, 0);
                    
                    var sr = pieceGO.AddComponent<SpriteRenderer>();
                    sr.sprite = CreateSquareSprite();
                    sr.color = BlockPiece.GetColorValue(shapeColor);
                    sr.sortingOrder = 10;
                    
                    var block = pieceGO.AddComponent<BlockPiece>();
                    block.Initialize(shapeColor);
                    pieces.Add(block);
                }
            }
            else
            {
                foreach (var offset in shapeOffsets)
                {
                    var pieceGO = Instantiate(blockPiecePrefab, transform);
                    pieceGO.transform.localPosition = new Vector3(offset.x, offset.y, 0);
                    
                    var block = pieceGO.GetComponent<BlockPiece>();
                    if (block != null)
                    {
                        block.Initialize(shapeColor);
                        pieces.Add(block);
                    }
                }
            }
        }
        
        /// <summary>
        /// Create a simple square sprite dynamically
        /// </summary>
        private Sprite CreateSquareSprite()
        {
            var texture = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % 32;
                int y = i / 32;
                // Add slight border
                if (x < 2 || x > 29 || y < 2 || y > 29)
                    pixels[i] = new Color(0, 0, 0, 0.3f);
                else
                    pixels[i] = Color.white;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }
        
        private void OnMouseDown()
        {
            if (IsPlaced) return;
            
            isDragging = true;
            originalPosition = transform.position;
            transform.localScale = Vector3.one * dragScale;
            
            // Increase sorting order while dragging
            foreach (var piece in pieces)
                piece.SetSortingOrder(100);
            
            AudioManager.Instance?.PlaySound("pickup");
        }
        
        private void OnMouseDrag()
        {
            if (!isDragging || IsPlaced) return;
            
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            // Offset so the shape is above finger
            mousePos.y += 1.5f;
            
            transform.position = Vector3.Lerp(transform.position, mousePos, snapSpeed * Time.deltaTime);
        }
        
        private void OnMouseUp()
        {
            if (!isDragging || IsPlaced) return;
            
            isDragging = false;
            transform.localScale = originalScale;
            
            // Reset sorting order
            foreach (var piece in pieces)
                piece.SetSortingOrder(10);
            
            // Try to place on grid
            if (gridManager != null && gridManager.TryPlaceShape(this, GetGridPosition()))
            {
                IsPlaced = true;
                AudioManager.Instance?.PlaySound("place");
                
                // Notify spawner
                spawner?.OnShapePlaced(this);
            }
            else
            {
                // Return to original position
                StartCoroutine(ReturnToOriginal());
            }
        }
        
        /// <summary>
        /// Get the grid position under the shape center
        /// </summary>
        private Vector2Int GetGridPosition()
        {
            if (gridManager == null) return Vector2Int.zero;
            return gridManager.WorldToGridPosition(transform.position);
        }
        
        private System.Collections.IEnumerator ReturnToOriginal()
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, originalPosition, elapsed / duration);
                yield return null;
            }
            
            transform.position = originalPosition;
        }
        
        /// <summary>
        /// Get all block pieces in this shape
        /// </summary>
        public List<BlockPiece> GetPieces() => pieces;
        
        /// <summary>
        /// Check if shape can be placed anywhere on the grid
        /// </summary>
        public bool CanBePlacedAnywhere(GridManager grid)
        {
            if (grid == null) return false;
            
            for (int x = 0; x < grid.GridWidth; x++)
            {
                for (int y = 0; y < grid.GridHeight; y++)
                {
                    if (grid.CanPlaceShapeAt(shapeOffsets, new Vector2Int(x, y)))
                        return true;
                }
            }
            return false;
        }
    }
}
