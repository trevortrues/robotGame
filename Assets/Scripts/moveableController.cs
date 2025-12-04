using UnityEngine;
using System.Collections.Generic;

public class MoveableManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    
    [Header("Moveable Settings")]
    [SerializeField] private List<GameObject> allMoveables = new List<GameObject>();
    [SerializeField] private bool snapToGridOnStart = true;
    [SerializeField] private float colliderSize = 1f; // Full grid size for proper collision
    [SerializeField] private bool resizeSpritesToGrid = false; // Turn this OFF by default
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    void Start()
    {
        if (grid == null)
            grid = FindFirstObjectByType<Grid>();
            
        if (grid == null)
        {
            Debug.LogError("MoveableManager: No grid found!");
            return;
        }
        
        SetupAllMoveables();
    }
    
    void SetupAllMoveables()
    {
        GameObject[] moveableObjects = GameObject.FindGameObjectsWithTag("Moveable");
        allMoveables.Clear();
        allMoveables.AddRange(moveableObjects);
        
        if (debugMode)
            Debug.Log($"Found {allMoveables.Count} moveable objects");
        
        foreach (GameObject moveable in allMoveables)
        {
            // Snap to grid center
            Vector3Int cell = grid.WorldToCell(moveable.transform.position);
            moveable.transform.position = grid.GetCellCenterWorld(cell);
            
            // Setup collider
            BoxCollider2D col = moveable.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = moveable.AddComponent<BoxCollider2D>();
            }
            
            // Set collider to match grid cell size exactly
            col.size = Vector2.one;
            col.offset = Vector2.zero;
            col.isTrigger = false; // MUST be false for collision
            
            if (debugMode)
                Debug.Log($"Setup {moveable.name}: pos={moveable.transform.position}, collider size={col.size}");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!debugMode || grid == null) return;
        
        Gizmos.color = Color.cyan;
        foreach (GameObject moveable in allMoveables)
        {
            if (moveable == null) continue;
            
            Vector3Int cell = grid.WorldToCell(moveable.transform.position);
            Vector3 cellCenter = grid.GetCellCenterWorld(cell);
            
            // Show grid cell
            Gizmos.DrawWireCube(cellCenter, grid.cellSize);
            
            // Show collider
            Gizmos.color = Color.yellow;
            BoxCollider2D col = moveable.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                Gizmos.DrawWireCube(moveable.transform.position, col.size);
            }
        }
    }
}