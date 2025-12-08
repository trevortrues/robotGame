using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections;

public class chipController : MonoBehaviour
{
    [Header("Chip Collection")]
    [SerializeField] private List<GameObject> allChips = new List<GameObject>();
    [SerializeField] private List<GameObject> collectedChips = new List<GameObject>();
    [SerializeField] private bool allChipsCollected = false;
    
    [Header("UI Settings")]
    [SerializeField] private Vector2 screenMargin = new Vector2(0.05f, 0.05f); // Margin from edges (0-1 viewport space)
    [SerializeField] private float chipSpacing = 0.5f;
    [SerializeField] private float moveToUISpeed = 2f;
    [SerializeField] private bool arrangeHorizontally = true;
    [SerializeField] private int maxChipsPerRow = 10;
    
    [Header("Visual Settings")]
    [SerializeField] private float collectedScale = 0.5f;
    [SerializeField] private int uiSortingOrder = 100;
    [SerializeField] private bool animateCollection = true;
    
    [Header("Hover Animation")]
    [SerializeField] private float hoverHeight = 0.1f;
    [SerializeField] private float hoverSpeed = 2f; 
    [SerializeField] private float hoverScaleAmount = 0.05f;
    [SerializeField] private float randomOffsetRange = 0.5f;
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip chipCollectSound;
    [SerializeField] private AudioClip allChipsCollectedSound;
    [SerializeField] private float soundVolume = 1f;
    
    [Header("Scene Transition")]
    [SerializeField] private string nextScene;
    [SerializeField] private float nextSceneDelay = 1f;
    [SerializeField] private bool isFinalScene = false;
    [SerializeField] private Tilemap winTile;
    [SerializeField] private TileBase winTileAnimated;
    [SerializeField] private TileBase winTileFinal;
    [SerializeField] private float winTileAnimationDuration = 1f;
    private bool reachedWinTile = false;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private Camera mainCamera;
    
    public bool AllChipsCollected => allChipsCollected;
    public int TotalChips => allChips.Count;
    public int CollectedCount => collectedChips.Count;
    public float CollectionProgress => TotalChips > 0 ? (float)CollectedCount / TotalChips : 0f;
    
    public float HoverHeight => hoverHeight;
    public float HoverSpeed => hoverSpeed;
    public float HoverScaleAmount => hoverScaleAmount;
    public Tilemap WinTile => winTile;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("ChipController: No main camera found!");
            return;
        }
        
        GameObject[] chipObjects = GameObject.FindGameObjectsWithTag("chip");
        allChips.AddRange(chipObjects);
        
        if (debugMode)
        {
            Debug.Log($"ChipController: Found {allChips.Count} chips in the scene");
        }
        
        for (int i = 0; i < allChips.Count; i++)
        {
            float randomOffset = Random.Range(0f, randomOffsetRange);
            SetupChip(allChips[i], randomOffset);
        }

        if (isFinalScene)
        {
            reachedWinTile = true;
            StartCoroutine(AutoCollectAllChips());
        }
    }

    IEnumerator AutoCollectAllChips()
    {
        yield return null; // Wait one frame for chips to be set up

        foreach (GameObject chip in allChips)
        {
            if (!collectedChips.Contains(chip))
            {
                CollectChip(chip);
            }
        }
    }
    
    void SetupChip(GameObject chip, float animationOffset)
    {
        Collider2D col = chip.GetComponent<Collider2D>();
        if (col == null)
        {
            col = chip.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;

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

        ChipCollector collector = chip.GetComponent<ChipCollector>();
        if (collector != null)
        {
            collector.StopHover();
        }

        if (chipCollectSound != null)
        {
            PlayRandomClipSegment(chipCollectSound, chip.transform.position, 2f);
        }

        int index = collectedChips.Count - 1;
        Vector3 targetUIPosition = GetUIPositionForChip(index);

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
        
        CheckAllChipsCollected();
    }
    
    Vector3 GetUIPositionForChip(int index)
    {
        if (mainCamera == null) 
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("No camera found!");
                return Vector3.zero;
            }
        }
        
        // Get the top-left corner in viewport coordinates
        Vector3 viewportPos = new Vector3(screenMargin.x, 1f - screenMargin.y, 10f); // 10f is distance from camera
        
        // Convert viewport to world position
        Vector3 worldPos = mainCamera.ViewportToWorldPoint(viewportPos);
        
        // Adjust for chip index
        if (arrangeHorizontally)
        {
            int row = index / maxChipsPerRow;
            int col = index % maxChipsPerRow;
            worldPos.x += col * chipSpacing;
            worldPos.y -= row * chipSpacing;
        }
        else
        {
            int col = index / maxChipsPerRow;
            int row = index % maxChipsPerRow;
            worldPos.x += col * chipSpacing;
            worldPos.y -= row * chipSpacing;
        }
        
        // Keep the same Z as the camera to ensure it's visible
        worldPos.z = 0;
        
        return worldPos;
    }
    
    IEnumerator AnimateChipToUI(GameObject chip, Vector3 targetPosition)
    {
        Collider2D col = chip.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        SpriteRenderer renderer = chip.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = uiSortingOrder;
        }
        
        Vector3 startPosition = chip.transform.position;
        Vector3 startScale = chip.transform.localScale;
        Vector3 targetScale = Vector3.one * collectedScale; 
        
        float journey = 0f;
        while (journey <= 1f)
        {
            journey += Time.deltaTime * moveToUISpeed;
            float percent = Mathf.Clamp01(journey);
            
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
        Collider2D col = chip.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        SpriteRenderer renderer = chip.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = uiSortingOrder;
        }
        
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
            
            if (chipCollectSound != null)
            {
                PlayLastClipSegment(chipCollectSound, mainCamera.transform.position, 2f);
            }
            
            OnAllChipsCollected();
        }
    }
    
    void OnAllChipsCollected()
    {
        if (debugMode)
        {
            Debug.Log("All chips collected! Waiting for win tile...");
        }

        ActivateWinTileAnimation();
        TryTransitionToNextScene();
    }

    void ActivateWinTileAnimation()
    {
        if (winTile == null || winTileAnimated == null) return;

        BoundsInt bounds = winTile.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (winTile.HasTile(pos))
                {
                    winTile.SetTile(pos, winTileAnimated);
                }
            }
        }
        winTile.RefreshAllTiles();

        if (winTileFinal != null)
        {
            StartCoroutine(SwapToFinalTile());
        }
    }

    IEnumerator SwapToFinalTile()
    {
        yield return new WaitForSeconds(winTileAnimationDuration);

        BoundsInt bounds = winTile.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (winTile.HasTile(pos))
                {
                    winTile.SetTile(pos, winTileFinal);
                }
            }
        }
        winTile.RefreshAllTiles();
    }

    public void OnReachedWinTile()
    {
        if (reachedWinTile) return;

        reachedWinTile = true;

        if (debugMode)
        {
            Debug.Log("Reached win tile!");
        }

        TryTransitionToNextScene();
    }

    void TryTransitionToNextScene()
    {
        if (!allChipsCollected || !reachedWinTile) return;

        if (debugMode)
        {
            Debug.Log("Ready for next level!");
        }

        if (!isFinalScene && !string.IsNullOrEmpty(nextScene))
        {
            StartCoroutine(LoadNextSceneAfterDelay());
        }
    }

    IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(nextSceneDelay);
        SceneManager.LoadScene(nextScene);
    }
    
    float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    void PlayRandomClipSegment(AudioClip clip, Vector3 position, float duration, bool excludeLastSegment = true)
    {
        GameObject tempAudio = new GameObject("TempAudio");
        tempAudio.transform.position = position;
        AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = soundVolume;

        float maxStartTime;
        if (excludeLastSegment)
        {
            maxStartTime = Mathf.Max(0f, clip.length - duration * 2);
        }
        else
        {
            maxStartTime = Mathf.Max(0f, clip.length - duration);
        }
        audioSource.time = Random.Range(0f, maxStartTime);
        audioSource.Play();

        Destroy(tempAudio, duration);
    }

    void PlayLastClipSegment(AudioClip clip, Vector3 position, float duration)
    {
        GameObject tempAudio = new GameObject("TempAudio");
        tempAudio.transform.position = position;
        AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = soundVolume;

        audioSource.time = Mathf.Max(0f, clip.length - duration);
        audioSource.Play();

        Destroy(tempAudio, duration);
    }

    public void ResetChips()
    {
        collectedChips.Clear();
        allChipsCollected = false;
    }
    
    void OnDrawGizmos()
    {
        if (!debugMode) return;
        
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (mainCamera != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = GetUIPositionForChip(i);
                Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
            }
        }
    }
}

public class ChipCollector : MonoBehaviour
{
    private chipController controller;
    private bool collected = false;
    private float animationOffset = 0f;
    private bool hovering = true;

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

        float time = Time.time * controller.HoverSpeed + animationOffset;
        
        float yOffset = Mathf.Sin(time) * controller.HoverHeight;
        transform.position = originalPosition + Vector3.up * yOffset;

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