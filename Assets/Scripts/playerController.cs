using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Collections;

public class playerController : MonoBehaviour
{
    [SerializeField] Grid grid;
    [SerializeField] Tilemap wallsTilemap;
    [SerializeField] Tilemap moveablesTilemap;
    [SerializeField] Tilemap destructablesTilemap;
    [SerializeField] chipController chipController;
    [SerializeField] float stepTime = 0.12f;
    [SerializeField] bool preferHorizontal = true;
    [SerializeField] LayerMask obstacleCheckLayers = -1;
    [SerializeField] bool debugMode = false;
    
    [Header("Dash Settings")]
    [SerializeField] float dashStepTime = 0.06f; 
    [SerializeField] float dashCooldown = 2f; 
    [SerializeField] int dashDistance = 2; 
    [SerializeField] Color dashTrailColor = new Color(1f, 1f, 1f, 0.5f);

    Vector3Int playerCell;
    bool moving;
    Vector3Int lastMoveDirection = Vector3Int.right;
    bool canDash = true;
    float dashCooldownTimer = 0f;

    void Awake()
    {
        if (!grid || !wallsTilemap || !moveablesTilemap) { enabled = false; return; }
        playerCell = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(playerCell);
    }

    void Update()
    {
        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
            {
                canDash = true;
                if (debugMode) Debug.Log("Dash ready!");
            }
        }
        
        if (moving) return;
        var kb = Keyboard.current; if (kb == null) return;

        bool shiftPressed = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        
        int x = 0, y = 0;
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) x = 1;
        else if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) x = -1;
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) y = 1;
        else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) y = -1;
        if (x != 0 && y != 0) { if (preferHorizontal) y = 0; else x = 0; }

        if (shiftPressed && x == 0 && y == 0 && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame))
        {
            x = lastMoveDirection.x;
            y = lastMoveDirection.y;
        }
        
        if (x == 0 && y == 0) return;
        
        Vector3Int dir = new Vector3Int(x, y, 0);

        if (x != 0 || y != 0)
        {
            lastMoveDirection = dir;
        }

        if (shiftPressed && canDash)
        {
            StartCoroutine(TryDash(dir));
        }
        else
        {
            StartCoroutine(TrySlide(dir));
        }
    }

    GameObject GetMoveableObjectAt(Vector3Int c)
    {
        Vector3 worldPos = grid.GetCellCenterWorld(c);
        float checkRadius = grid.cellSize.x * 0.45f; 
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, checkRadius, obstacleCheckLayers);
        
        foreach (Collider2D col in colliders)
        {
            if (col.gameObject == gameObject) continue;
            if (!col.enabled) continue;
            if (col.CompareTag("Moveable"))
            {
                if (debugMode) Debug.Log($"Found moveable object: {col.gameObject.name} at {c}");
                return col.gameObject;
            }
        }
        return null;
    }

    IEnumerator TryDash(Vector3Int dir)
    {
        if (debugMode) Debug.Log($"Attempting dash in direction {dir}");
        
        Vector3Int checkPos = playerCell;
        int actualDashDistance = 0;
        
        for (int i = 1; i <= dashDistance; i++)
        {
            checkPos = playerCell + (dir * i);
            
            if (IsBlockedForPlayer(checkPos))
            {
                if (debugMode) Debug.Log($"Dash blocked at distance {i}");
                break;
            }
            
            if (HasBox(checkPos) || GetMoveableObjectAt(checkPos) != null)
            {
                if (debugMode) Debug.Log($"Can't dash through box at distance {i}");
                break;
            }
            
            actualDashDistance = i;
        }
        
        if (actualDashDistance == 0)
        {
            if (debugMode) Debug.Log("Can't dash, trying normal move");
            yield return StartCoroutine(TrySlide(dir));
            yield break;
        }
        
        canDash = false;
        dashCooldownTimer = dashCooldown;
        
        Vector3 startPos = transform.position;
        Vector3Int targetCell = playerCell + (dir * actualDashDistance);
        Vector3 endPos = grid.GetCellCenterWorld(targetCell);
        
        moving = true;
        float t = 0f;
        float dashTime = dashStepTime * actualDashDistance; 
        if (debugMode)
        {
            for (int i = 1; i <= actualDashDistance; i++)
            {
                Vector3Int trailCell = playerCell + (dir * i);
                Debug.DrawLine(
                    grid.GetCellCenterWorld(trailCell) + Vector3.down * 0.4f,
                    grid.GetCellCenterWorld(trailCell) + Vector3.up * 0.4f,
                    dashTrailColor,
                    1f
                );
            }
        }
        
        while (t < 1f)
        {
            t += Time.deltaTime / dashTime;
            float u = Mathf.Clamp01(t);
            
            float easedU = EaseOutCubic(u);
            
            transform.position = Vector3.Lerp(startPos, endPos, easedU);
            yield return null;
        }
        
        transform.position = endPos;
        playerCell = targetCell;
        moving = false;

        if (debugMode) Debug.Log($"Dashed {actualDashDistance} cells to {playerCell}");

        CheckWinTile();
    }
    
    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    public void TeleportTo(Vector3 worldPosition)
    {
        if (moving) return;
        
        Vector3Int newCell = grid.WorldToCell(worldPosition);
        Vector3 snapPosition = grid.GetCellCenterWorld(newCell);
        
        transform.position = snapPosition;
        playerCell = newCell;
    }

    public bool IsMoving => moving;
    
    public bool CanDash => canDash;
    public float DashCooldownRemaining => dashCooldownTimer;
    public float DashCooldownPercent => canDash ? 1f : (1f - (dashCooldownTimer / dashCooldown));

    IEnumerator TrySlide(Vector3Int dir)
    {
        Vector3Int next = playerCell + dir;
        
        if (IsBlockedForPlayer(next)) 
        {
            if (debugMode) Debug.Log($"Player movement blocked at cell {next}");
            yield break;
        }

        bool pushingTile = HasBox(next);
        GameObject moveableObj = GetMoveableObjectAt(next);
        bool pushing = pushingTile || (moveableObj != null);
        
        Vector3Int boxFrom = next, boxTo = next + dir;

        if (pushing)
        {
            bool boxDestinationHasDestructable = HasDestructableTile(boxTo) || HasDestructableObject(boxTo);
            bool boxDestinationBlocked = IsBlockedForBox(boxTo);
            
            if (!boxDestinationHasDestructable && boxDestinationBlocked)
            {
                if (debugMode) Debug.Log($"Box can't be pushed to {boxTo} - blocked");
                yield break;
            }
            
            if (HasBox(boxTo) || GetMoveableObjectAt(boxTo) != null)
            {
                if (debugMode) Debug.Log($"Box can't push another box at {boxTo}");
                yield break;
            }
            
            if (boxDestinationHasDestructable)
            {
                DestroyDestructableAt(boxTo);
                if (debugMode) Debug.Log($"Destroyed destructable at {boxTo}");
                
                yield return new WaitForSeconds(0.1f);
            }
        }

        Vector3 playerStart = transform.position;
        Vector3 playerEnd = grid.GetCellCenterWorld(next);

        TileBase boxTile = null;
        Vector3 boxDelta = Vector3.zero;
        Vector3 moveableStart = Vector3.zero;
        
        if (pushing)
        {
            if (moveableObj != null)
            {
                moveableStart = moveableObj.transform.position;
                boxDelta = grid.GetCellCenterWorld(boxTo) - moveableStart;
            }
            else if (pushingTile)
            {
                boxTile = moveablesTilemap.GetTile(boxFrom);
                Vector3 boxStart = grid.GetCellCenterWorld(boxFrom);
                Vector3 boxEnd = grid.GetCellCenterWorld(boxTo);
                boxDelta = boxEnd - boxStart;
            }
        }

        moving = true;
        float t = 0f, d = Mathf.Max(0.0001f, stepTime);

        while (t < 1f)
        {
            t += Time.deltaTime / d;
            float u = Mathf.Clamp01(t);

            transform.position = Vector3.Lerp(playerStart, playerEnd, u);

            if (pushing)
            {
                if (moveableObj != null)
                {
                    moveableObj.transform.position = moveableStart + (boxDelta * u);
                }
                else if (pushingTile)
                {
                    Vector3 offset = Vector3.Lerp(Vector3.zero, boxDelta, u);
                    moveablesTilemap.SetTransformMatrix(boxFrom, Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one));
                }
            }
            yield return null;
        }

        transform.position = playerEnd;
        playerCell = next;

        if (pushing)
        {
            if (moveableObj != null)
            {
                moveableObj.transform.position = grid.GetCellCenterWorld(boxTo);
            }
            else if (pushingTile)
            {
                moveablesTilemap.SetTransformMatrix(boxFrom, Matrix4x4.identity);
                moveablesTilemap.SetTile(boxTo, boxTile);
                moveablesTilemap.SetTile(boxFrom, null);
                moveablesTilemap.RefreshTile(boxFrom);
                moveablesTilemap.RefreshTile(boxTo);
            }
        }
        moving = false;

        CheckWinTile();
    }

    bool IsBlockedForPlayer(Vector3Int c)
    {
        if (wallsTilemap.HasTile(c)) return true;
        if (HasDestructableTile(c)) return true;
        
        Vector3 worldPos = grid.GetCellCenterWorld(c);
        if (HasNonMoveableObject(worldPos)) return true;
        if (HasDestructableObject(c)) return true;
        
        return false;
    }

    bool IsBlockedForBox(Vector3Int c)
    {
        if (wallsTilemap.HasTile(c)) return true;
        
        Vector3 worldPos = grid.GetCellCenterWorld(c);
        if (HasNonMoveableObject(worldPos)) return true;
        
        return false;
    }

    bool HasDestructableTile(Vector3Int c)
    {
        return destructablesTilemap != null && destructablesTilemap.HasTile(c);
    }

    bool HasNonMoveableObject(Vector3 worldPos)
    {
        Vector2 boxSize = grid.cellSize * 0.8f; 
        Collider2D[] colliders = Physics2D.OverlapBoxAll(worldPos, boxSize, 0f, obstacleCheckLayers);
        
        if (debugMode && colliders.Length > 0)
        {
            Debug.Log($"Checking {worldPos}: Found {colliders.Length} colliders");
        }
        
        foreach (Collider2D col in colliders)
        {
            if (col.gameObject == gameObject) continue;
            
            if (!col.enabled) continue;
            
            if (col.CompareTag("nonMoveable"))
            {
                if (debugMode) 
                {
                    Debug.Log($"BLOCKED by nonMoveable: {col.gameObject.name} at {col.transform.position}");
                }
                return true;
            }
        }
        
        return false;
    }

    bool HasDestructableObject(Vector3Int c)
    {
        Vector3 worldPos = grid.GetCellCenterWorld(c);
        Vector2 boxSize = grid.cellSize * 0.8f;
        Collider2D[] colliders = Physics2D.OverlapBoxAll(worldPos, boxSize, 0f, obstacleCheckLayers);
        
        foreach (Collider2D col in colliders)
        {
            if (col.gameObject == gameObject) continue;
            if (!col.enabled) continue;
            
            if (col.CompareTag("Destructable"))
            {
                return true;
            }
        }
        return false;
    }

    void DestroyDestructableAt(Vector3Int c)
    {
        if (HasDestructableTile(c))
        {
            destructablesTilemap.SetTile(c, null);
            destructablesTilemap.RefreshTile(c);
        }
        
        Vector3 worldPos = grid.GetCellCenterWorld(c);
        float radius = grid.cellSize.x * 0.45f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, radius, obstacleCheckLayers);
        
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Destructable"))
            {
                // change sprite to destroyed version (later)
                Destroy(col.gameObject);
            }
        }
    }

    bool HasBox(Vector3Int c) => moveablesTilemap.HasTile(c);

    void CheckWinTile()
    {
        if (chipController == null || chipController.WinTile == null) return;

        if (chipController.WinTile.HasTile(playerCell))
        {
            chipController.OnReachedWinTile();
        }
    }
    
    void OnDrawGizmos()
    {
        if (!debugMode || !Application.isPlaying || grid == null) return;
        
        Vector3 cellCenter = grid.GetCellCenterWorld(playerCell);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(cellCenter, grid.cellSize);
        
        float radius = grid.cellSize.x * 0.45f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(cellCenter, radius);
        
        if (!canDash)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawSphere(cellCenter, radius * DashCooldownPercent);
        }
    }
}