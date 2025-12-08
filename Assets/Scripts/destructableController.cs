using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class DestructableController : MonoBehaviour
{
    [Header("Tilemap References")]
    [Tooltip("The tilemap containing moveable blocks")]
    public Tilemap moveablesTilemap;
    [Tooltip("The tilemap containing destructable blocks")]
    public Tilemap destructablesTilemap;
    
    [Header("Settings")]
    [Tooltip("How often to check for overlaps (in seconds)")]
    public float checkInterval = 0.1f;
    [Tooltip("Destroy effect prefab to spawn when destructable is destroyed")]
    public GameObject destroyEffectPrefab;
    [Tooltip("Sprites to show after rock is destroyed (picks random)")]
    public Sprite[] destructedSprites;
    [Tooltip("Sound to play when destructable is destroyed")]
    public AudioClip destroySound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;
    [Tooltip("Sorting order for debris sprites")]
    public int debrisSortingOrder = 0;
    
    private HashSet<Vector3Int> previousMoveablePositions = new HashSet<Vector3Int>();
    private float checkTimer = 0f;
    
    void Start()
    {
        if (moveablesTilemap == null)
        {
            Debug.LogError("DestructableController: Moveables Tilemap not assigned!");
        }
        if (destructablesTilemap == null)
        {
            Debug.LogError("DestructableController: Destructables Tilemap not assigned!");
        }
        
        UpdateMoveablePositions();
    }
    
    void Update()
    {
        if (moveablesTilemap == null || destructablesTilemap == null) return;
        
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckForOverlaps();
        }
    }
    
    void CheckForOverlaps()
    {
        HashSet<Vector3Int> currentMoveablePositions = new HashSet<Vector3Int>();
        
        BoundsInt bounds = moveablesTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int z = bounds.zMin; z < bounds.zMax; z++)
                {
                    Vector3Int cellPosition = new Vector3Int(x, y, z);
                    TileBase moveableTile = moveablesTilemap.GetTile(cellPosition);
                    
                    if (moveableTile != null)
                    {
                        currentMoveablePositions.Add(cellPosition);
                        
                        if (!previousMoveablePositions.Contains(cellPosition))
                        {
                            CheckAndDestroyDestructable(cellPosition);
                        }
                    }
                }
            }
        }

        previousMoveablePositions = currentMoveablePositions;
    }
    
    void CheckAndDestroyDestructable(Vector3Int position)
    {
        TileBase destructableTile = destructablesTilemap.GetTile(position);
        
        if (destructableTile != null)
        {
            DestroyDestructable(position);
        }
    }
    
    void DestroyDestructable(Vector3Int tilePosition)
    {
        destructablesTilemap.SetTile(tilePosition, null);

        Vector3 worldPosition = destructablesTilemap.CellToWorld(tilePosition) + destructablesTilemap.tileAnchor;

        if (destructedSprites != null && destructedSprites.Length > 0)
        {
            Sprite randomSprite = destructedSprites[Random.Range(0, destructedSprites.Length)];
            GameObject debris = new GameObject("DestructedRock");
            debris.transform.position = worldPosition;
            SpriteRenderer sr = debris.AddComponent<SpriteRenderer>();
            sr.sprite = randomSprite;

            TilemapRenderer tilemapRenderer = destructablesTilemap.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null)
            {
                sr.sortingLayerID = tilemapRenderer.sortingLayerID;
            }
            sr.sortingOrder = debrisSortingOrder;
        }

        if (destroyEffectPrefab != null)
        {
            GameObject effect = Instantiate(destroyEffectPrefab, worldPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, worldPosition, soundVolume);
        }

        Debug.Log($"Destroyed destructable at position {tilePosition}");
    }

    public void OnMoveablePushed(Vector3Int newPosition)
    {
        CheckAndDestroyDestructable(newPosition);
        UpdateMoveablePositions();
    }
    
    void UpdateMoveablePositions()
    {
        previousMoveablePositions.Clear();
        BoundsInt bounds = moveablesTilemap.cellBounds;
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int z = bounds.zMin; z < bounds.zMax; z++)
                {
                    Vector3Int cellPosition = new Vector3Int(x, y, z);
                    if (moveablesTilemap.GetTile(cellPosition) != null)
                    {
                        previousMoveablePositions.Add(cellPosition);
                    }
                }
            }
        }
    }

    public bool IsDestructableAt(Vector3Int position)
    {
        return destructablesTilemap.GetTile(position) != null;
    }

    public void DestroyAt(Vector3Int position)
    {
        if (IsDestructableAt(position))
        {
            DestroyDestructable(position);
        }
    }
}