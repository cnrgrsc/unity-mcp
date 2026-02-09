using UnityEngine;

namespace BlockBlast
{
    /// <summary>
    /// Represents a single block cell in the game grid or shape.
    /// Handles visual representation and destruction effects.
    /// </summary>
    public class BlockPiece : MonoBehaviour
    {
        [Header("Block Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BlockColor blockColor = BlockColor.Red;
        
        [Header("Visual Feedback")]
        [SerializeField] private float destroyAnimationDuration = 0.2f;
        [SerializeField] private ParticleSystem destroyEffect;
        
        public BlockColor Color => blockColor;
        public bool IsDestroying { get; private set; }
        
        private Vector2Int gridPosition;
        
        public Vector2Int GridPosition
        {
            get => gridPosition;
            set => gridPosition = value;
        }
        
        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        /// <summary>
        /// Initialize the block with a specific color
        /// </summary>
        public void Initialize(BlockColor color)
        {
            blockColor = color;
            ApplyColor();
        }
        
        /// <summary>
        /// Apply the color to the sprite renderer
        /// </summary>
        private void ApplyColor()
        {
            if (spriteRenderer == null) return;
            
            spriteRenderer.color = GetColorValue(blockColor);
        }
        
        /// <summary>
        /// Get Unity Color from BlockColor enum
        /// </summary>
        public static Color GetColorValue(BlockColor blockColor)
        {
            return blockColor switch
            {
                BlockColor.Red => new Color(1f, 0.27f, 0.27f),      // #FF4444
                BlockColor.Blue => new Color(0.27f, 0.27f, 1f),     // #4444FF
                BlockColor.Green => new Color(0.27f, 1f, 0.27f),    // #44FF44
                BlockColor.Yellow => new Color(1f, 1f, 0.27f),      // #FFFF44
                BlockColor.Purple => new Color(0.67f, 0.27f, 1f),   // #AA44FF
                _ => UnityEngine.Color.white
            };
        }
        
        /// <summary>
        /// Destroy the block with animation
        /// </summary>
        public void DestroyBlock()
        {
            if (IsDestroying) return;
            IsDestroying = true;
            
            // Play destroy effect if available
            if (destroyEffect != null)
            {
                var effect = Instantiate(destroyEffect, transform.position, Quaternion.identity);
                var main = effect.main;
                main.startColor = GetColorValue(blockColor);
                effect.Play();
                Destroy(effect.gameObject, main.duration + main.startLifetime.constantMax);
            }
            
            // Scale down animation
            StartCoroutine(DestroyAnimation());
        }
        
        private System.Collections.IEnumerator DestroyAnimation()
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            
            while (elapsed < destroyAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / destroyAnimationDuration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }
            
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Set sorting order for proper layering
        /// </summary>
        public void SetSortingOrder(int order)
        {
            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = order;
        }
    }
    
    /// <summary>
    /// Available block colors
    /// </summary>
    public enum BlockColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple
    }
}
