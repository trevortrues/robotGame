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
    [Tooltip("Sound to play when destructable is destroyed")]
    public AudioClip destroySound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;
    
    // Track moveable positions
    private HashSet<Vector3Int> previousMoveablePositions = new HashSet<Vector3Int>();
    private float checkTimer = 0f;
    
    void Start()
    {
        // Validate references
        if (moveablesTilemap == null)
        {
            Debug.LogError("DestructableController: Moveables Tilemap not assigned!");
        }
        if (destructablesTilemap == null)
        {
            Debug.LogError("DestructableController: Destructables Tilemap not assigned!");
        }
        
        // Store initial moveable positions
        UpdateMoveablePositions();
    }
    
    void Update()
    {
        if (moveablesTilemap == null || destructablesTilemap == null) return;
        
        // Check at intervals instead of every frame for performance
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckForOverlaps();
        }
    }
    
    void CheckForOverlaps()
    {
        // Get current moveable positions
        HashSet<Vector3Int> currentMoveablePositions = new HashSet<Vector3Int>();
        
        // Get bounds of the moveables tilemap
        BoundsInt bounds = moveablesTilemap.cellBounds;
        
        // Iterate through all positions in the moveables tilemap
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
                        
                        // Check if this moveable has moved to a new position
                        if (!previousMoveablePositions.Contains(cellPosition))
                        {
                            // This is a new position for a moveable, check if there's a destructable here
                            CheckAndDestroyDestructable(cellPosition);
                        }
                    }
                }
            }
        }
        
        // Update tracked positions
        previousMoveablePositions = currentMoveablePositions;
    }
    
    void CheckAndDestroyDestructable(Vector3Int position)
    {
        // Check if there's a destructable tile at this position
        TileBase destructableTile = destructablesTilemap.GetTile(position);
        
        if (destructableTile != null)
        {
            // Destroy the destructable tile
            DestroyDestructable(position);
        }
    }
    
    void DestroyDestructable(Vector3Int tilePosition)
    {
        // Remove the destructable tile
        destructablesTilemap.SetTile(tilePosition, null);
        
        // Convert tile position to world position for effects
        Vector3 worldPosition = destructablesTilemap.CellToWorld(tilePosition) + destructablesTilemap.tileAnchor;
        
        // Spawn destroy effect if assigned
        if (destroyEffectPrefab != null)
        {
            GameObject effect = Instantiate(destroyEffectPrefab, worldPosition, Quaternion.identity);
            Destroy(effect, 2f); // Destroy effect after 2 seconds
        }
        
        // Play destroy sound if assigned
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, worldPosition, soundVolume);
        }
        
        Debug.Log($"Destroyed destructable at position {tilePosition}");
    }
    
    // Call this method when a moveable is pushed to immediately check that specific position
    public void OnMoveablePushed(Vector3Int newPosition)
    {
        CheckAndDestroyDestructable(newPosition);
        UpdateMoveablePositions();
    }
    
    // Helper method to update tracked moveable positions
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
    
    // Optional: Method to check if a position has a destructable
    public bool IsDestructableAt(Vector3Int position)
    {
        return destructablesTilemap.GetTile(position) != null;
    }
    
    // Optional: Method to manually destroy a destructable at position
    public void DestroyAt(Vector3Int position)
    {
        if (IsDestructableAt(position))
        {
            DestroyDestructable(position);
        }
    }
}