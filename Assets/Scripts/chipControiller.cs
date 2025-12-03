using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class chipController : MonoBehaviour
{
    [Header("Chip Collection")]
    [SerializeField] private List<GameObject> allChips = new List<GameObject>();
    [SerializeField] private List<GameObject> collectedChips = new List<GameObject>();
    [SerializeField] private bool allChipsCollected = false;
    
    [Header("UI Settings")]
    [SerializeField] private Vector2 firstChipUIPosition = new Vector2(-8f, 4f); // Top left corner position
    [SerializeField] private float chipSpacing = 0.5f; // Spacing between chips in UI
    [SerializeField] private float moveToUISpeed = 2f; // Speed of chip moving to UI
    [SerializeField] private bool arrangeHorizontally = true; // Horizontal or vertical arrangement
    [SerializeField] private int maxChipsPerRow = 10; // Max chips before wrapping to next row/column
    
    [Header("Visual Settings")]
    [SerializeField] private float collectedScale = 0.5f; // Scale of chips when in UI
    [SerializeField] private int uiSortingOrder = 100; // Sorting order for UI chips
    [SerializeField] private bool animateCollection = true; // Animate chip to UI position
    
    [Header("Hover Animation")]
    [SerializeField] private float hoverHeight = 0.1f; // How high the chip hovers
    [SerializeField] private float hoverSpeed = 2f; // Speed of hover animation
    [SerializeField] private float hoverScaleAmount = 0.05f; // Scale change amount (5% by default)
    [SerializeField] private float randomOffsetRange = 0.5f; // Random offset for animation sync
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip chipCollectSound;
    [SerializeField] private AudioClip allChipsCollectedSound;
    [SerializeField] private float soundVolume = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    // Properties
    public bool AllChipsCollected => allChipsCollected;
    public int TotalChips => allChips.Count;
    public int CollectedCount => collectedChips.Count;
    public float CollectionProgress => TotalChips > 0 ? (float)CollectedCount / TotalChips : 0f;
    
    // Hover settings getter for ChipCollector
    public float HoverHeight => hoverHeight;
    public float HoverSpeed => hoverSpeed;
    public float HoverScaleAmount => hoverScaleAmount;
    
    void Start()
    {
        // Find all objects tagged "chip" in the scene
        GameObject[] chipObjects = GameObject.FindGameObjectsWithTag("chip");
        allChips.AddRange(chipObjects);
        
        if (debugMode)
        {
            Debug.Log($"ChipController: Found {allChips.Count} chips in the scene");
        }
        
        // Set up each chip with a random animation offset
        for (int i = 0; i < allChips.Count; i++)
        {
            float randomOffset = Random.Range(0f, randomOffsetRange);
            SetupChip(allChips[i], randomOffset);
        }
    }
    
    void SetupChip(GameObject chip, float animationOffset)
    {
        // Make sure chip has a collider set as trigger
        Collider2D col = chip.GetComponent<Collider2D>();
        if (col == null)
        {
            col = chip.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;
        
        // Add ChipCollector component if it doesn't exist
        if (chip.GetComponent<ChipCollector>() == null)
        {
            ChipCollector collector = chip.AddComponent<ChipCollector>();
            collector.Initialize(this, animationOffset);
        }
    }
    
    public void CollectChip(GameObject chip)
    {
        if (collectedChips.Contains(chip))
        {
            if (debugMode) Debug.Log($"Chip {chip.name} already collected");
            return;
        }
        
        collectedChips.Add(chip);
        
        // Stop hover animation
        ChipCollector collector = chip.GetComponent<ChipCollector>();
        if (collector != null)
        {
            collector.StopHover();
        }
        
        // Play collection sound
        if (chipCollectSound != null)
        {
            AudioSource.PlayClipAtPoint(chipCollectSound, chip.transform.position, soundVolume);
        }
        
        // Calculate UI position for this chip
        int index = collectedChips.Count - 1;
        Vector3 targetUIPosition = GetUIPositionForChip(index);
        
        // Move chip to UI
        if (animateCollection)
        {
            StartCoroutine(AnimateChipToUI(chip, targetUIPosition));
        }
        else
        {
            MoveChipToUIInstant(chip, targetUIPosition);
        }
        
        if (debugMode)
        {
            Debug.Log($"Collected chip {chip.name}. Progress: {CollectedCount}/{TotalChips}");
        }
        
        // Check if all chips collected
        CheckAllChipsCollected();
    }
    
    Vector3 GetUIPositionForChip(int index)
    {
        Vector3 basePosition = firstChipUIPosition;
        
        if (arrangeHorizontally)
        {
            int row = index / maxChipsPerRow;
            int col = index % maxChipsPerRow;
            basePosition.x += col * chipSpacing;
            basePosition.y -= row * chipSpacing;
        }
        else
        {
            int col = index / maxChipsPerRow;
            int row = index % maxChipsPerRow;
            basePosition.x += col * chipSpacing;
            basePosition.y -= row * chipSpacing;
        }
        
        basePosition.z = 0; // Ensure chips are at z=0
        return basePosition;
    }
    
    IEnumerator AnimateChipToUI(GameObject chip, Vector3 targetPosition)
    {
        // Disable the collider so it can't be collected again
        Collider2D col = chip.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        // Set sorting order for UI
        SpriteRenderer renderer = chip.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = uiSortingOrder;
        }
        
        Vector3 startPosition = chip.transform.position;
        Vector3 startScale = chip.transform.localScale;
        Vector3 targetScale = Vector3.one * collectedScale; // Use base scale
        
        float journey = 0f;
        while (journey <= 1f)
        {
            journey += Time.deltaTime * moveToUISpeed;
            float percent = Mathf.Clamp01(journey);
            
            // Use an easing curve for smooth animation
            float eased = EaseInOutCubic(percent);
            
            chip.transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
            chip.transform.localScale = Vector3.Lerp(startScale, targetScale, eased);
            
            yield return null;
        }
        
        chip.transform.position = targetPosition;
        chip.transform.localScale = targetScale;
    }
    
    void MoveChipToUIInstant(GameObject chip, Vector3 targetPosition)
    {
        // Disable the collider
        Collider2D col = chip.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        // Set sorting order for UI
        SpriteRenderer renderer = chip.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = uiSortingOrder;
        }
        
        // Move and scale
        chip.transform.position = targetPosition;
        chip.transform.localScale = Vector3.one * collectedScale;
    }
    
    void CheckAllChipsCollected()
    {
        if (collectedChips.Count >= allChips.Count && !allChipsCollected)
        {
            allChipsCollected = true;
            
            if (debugMode)
            {
                Debug.Log("ALL CHIPS COLLECTED! Level complete!");
            }
            
            // Play completion sound
            if (allChipsCollectedSound != null)
            {
                AudioSource.PlayClipAtPoint(allChipsCollectedSound, Camera.main.transform.position, soundVolume);
            }
            
            // Trigger any completion events here
            OnAllChipsCollected();
        }
    }
    
    void OnAllChipsCollected()
    {
        // This is where you'd trigger level completion logic
        // For now, just a placeholder
        if (debugMode)
        {
            Debug.Log("Ready for next level!");
        }
    }
    
    float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
    
    // Public method to reset chips (useful for restarting level)
    public void ResetChips()
    {
        collectedChips.Clear();
        allChipsCollected = false;
        
        // You'd need to reset chip positions and visibility here
        // Implementation depends on whether you destroy chips or just hide them
    }
    
    void OnDrawGizmos()
    {
        if (!debugMode) return;
        
        // Draw UI area
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 5; i++)
        {
            Vector3 pos = GetUIPositionForChip(i);
            Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
        }
    }
}

// Helper component for individual chips with hover animation
public class ChipCollector : MonoBehaviour
{
    private chipController controller;
    private bool collected = false;
    private float animationOffset = 0f;
    private bool hovering = true;
    
    // Cache initial values
    private Vector3 originalPosition;
    private Vector3 originalScale;
    
    public void Initialize(chipController chipController, float offset)
    {
        controller = chipController;
        animationOffset = offset;
        originalPosition = transform.position;
        originalScale = transform.localScale;
        hovering = true;
    }
    
    void Update()
    {
        if (!hovering || collected) return;
        
        if (controller == null) return;
        
        // Calculate hover animation
        float time = Time.time * controller.HoverSpeed + animationOffset;
        
        // Vertical hover using sine wave
        float yOffset = Mathf.Sin(time) * controller.HoverHeight;
        transform.position = originalPosition + Vector3.up * yOffset;
        
        // Scale animation to simulate depth (closer = bigger, farther = smaller)
        // When chip is up (positive yOffset), it should be slightly larger
        float scaleMultiplier = 1f + (yOffset / controller.HoverHeight) * controller.HoverScaleAmount;
        transform.localScale = originalScale * scaleMultiplier;
    }
    
    public void StopHover()
    {
        hovering = false;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        
        if (other.CompareTag("Player"))
        {
            collected = true;
            if (controller != null)
            {
                controller.CollectChip(gameObject);
            }
        }
    }
}