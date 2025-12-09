using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class EnemyChaser : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Grid grid;
    [SerializeField] Tilemap wallsTilemap;
    [SerializeField] Tilemap keyTilemap;
    [SerializeField] playerController player;

    [Header("Movement")]
    [SerializeField] float stepTime = 0.15f;
    [SerializeField] float moveDelay = 0.2f;

    [Header("Detection")]
    [Tooltip("Detection range in tiles. 0 = infinite range (always chase)")]
    [SerializeField] float detectionRange = 0f;

    [Tooltip("Visual indicator of detection range in editor")]
    [SerializeField] bool showDetectionRange = true;

    [Tooltip("Enemy behavior when player is out of range")]
    [SerializeField] EnemyBehavior outOfRangeBehavior = EnemyBehavior.Idle;

    Vector3Int enemyCell;
    bool moving;

    public enum EnemyBehavior
    {
        Idle,           // Stand still when player is far
        Patrol,         // Walk around randomly (not implemented yet)
        ReturnToStart   // Return to starting position (not implemented yet)
    }

    void Awake()
    {
        if (!grid || !wallsTilemap || !player)
        {
            Debug.LogError("EnemyChaser: missing references. Disabling.");
            enabled = false;
            return;
        }

        enemyCell = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(enemyCell);
        StartCoroutine(ChaseLoop());
    }

    IEnumerator ChaseLoop()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        while (true)
        {
            // If game is paused (timeScale == 0) do nothing
            if (Time.timeScale == 0f)
            {
                yield return null;
                continue;
            }

            // Enemy moves even when player is moving (more aggressive)
            if (!moving)
            {
                // Check if player is in detection range
                if (IsPlayerInRange())
                {
                    Vector3Int dir = GetChaseDirection();
                    if (dir != Vector3Int.zero)
                        yield return Move(dir);
                }
                else
                {
                    // Player is out of range - do idle behavior
                    // (Patrol and ReturnToStart not implemented yet)
                }
            }

            yield return new WaitForSeconds(moveDelay);
        }
    }

    bool IsPlayerInRange()
    {
        // If detection range is 0, always chase (infinite range)
        if (detectionRange <= 0f) return true;

        Vector3Int playerCell = grid.WorldToCell(player.transform.position);
        Vector3Int diff = playerCell - enemyCell;

        // Calculate Manhattan distance (grid distance)
        float distance = Mathf.Abs(diff.x) + Mathf.Abs(diff.y);

        return distance <= detectionRange;
    }

    Vector3Int GetChaseDirection()
    {
        Vector3Int playerCell = grid.WorldToCell(player.transform.position);
        Vector3Int diff = playerCell - enemyCell;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            int dirX = (int)Mathf.Sign(diff.x);
            Vector3Int dir = new Vector3Int(dirX, 0, 0);
            if (CanMove(dir)) return dir;
        }
        else
        {
            int dirY = (int)Mathf.Sign(diff.y);
            Vector3Int dir = new Vector3Int(0, dirY, 0);
            if (CanMove(dir)) return dir;
        }

        if (CanMove(Vector3Int.right)) return Vector3Int.right;
        if (CanMove(Vector3Int.left)) return Vector3Int.left;
        if (CanMove(Vector3Int.up)) return Vector3Int.up;
        if (CanMove(Vector3Int.down)) return Vector3Int.down;

        return Vector3Int.zero;
    }

    bool CanMove(Vector3Int dir)
    {
        Vector3Int target = enemyCell + dir;
        if (wallsTilemap.HasTile(target)) return false;
        if (keyTilemap != null && keyTilemap.HasTile(target)) return false;
        return true;
    }

    IEnumerator Move(Vector3Int dir)
    {
        Vector3Int target = enemyCell + dir;
        if (!CanMove(dir)) yield break;

        moving = true;
        Vector3 start = transform.position;
        Vector3 end = grid.GetCellCenterWorld(target);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, stepTime);
            transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(t));
            yield return null;
        }

        transform.position = end;
        enemyCell = target;
        moving = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        playerController pc = other.GetComponent<playerController>();
        if (pc != null)
        {
            StartCoroutine(RestartSceneRealtime(0.1f));
        }
    }

    IEnumerator RestartSceneRealtime(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnDrawGizmos()
    {
        if (!showDetectionRange || detectionRange <= 0f) return;

        // Draw detection range as a diamond shape (Manhattan distance)
        Vector3 center = Application.isPlaying ? grid.GetCellCenterWorld(enemyCell) : transform.position;

        // Check if player is in range and change color
        bool playerInRange = false;
        if (Application.isPlaying && player != null && grid != null)
        {
            playerInRange = IsPlayerInRange();
        }

        Gizmos.color = playerInRange ? new Color(1f, 0f, 0f, 0.3f) : new Color(1f, 1f, 0f, 0.2f);

        // Draw diamond shape representing Manhattan distance
        float cellSize = grid != null ? grid.cellSize.x : 1f;
        float rangeSize = detectionRange * cellSize;

        Vector3 top = center + Vector3.up * rangeSize;
        Vector3 bottom = center + Vector3.down * rangeSize;
        Vector3 left = center + Vector3.left * rangeSize;
        Vector3 right = center + Vector3.right * rangeSize;

        // Draw lines
        Gizmos.DrawLine(top, right);
        Gizmos.DrawLine(right, bottom);
        Gizmos.DrawLine(bottom, left);
        Gizmos.DrawLine(left, top);

        // Draw circle for approximate range
        Gizmos.color = playerInRange ? new Color(1f, 0f, 0f, 0.15f) : new Color(1f, 1f, 0f, 0.1f);
        UnityEditor.Handles.DrawWireDisc(center, Vector3.forward, rangeSize);

        // Draw line to player if in range
        if (playerInRange && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center, player.transform.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showDetectionRange || detectionRange <= 0f) return;

        // Draw more detailed range when selected
        Vector3 center = Application.isPlaying ? grid.GetCellCenterWorld(enemyCell) : transform.position;
        float cellSize = grid != null ? grid.cellSize.x : 1f;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);

        // Draw grid cells within detection range
        if (grid != null)
        {
            for (int x = -(int)detectionRange; x <= (int)detectionRange; x++)
            {
                for (int y = -(int)detectionRange; y <= (int)detectionRange; y++)
                {
                    // Check Manhattan distance
                    if (Mathf.Abs(x) + Mathf.Abs(y) <= detectionRange)
                    {
                        Vector3Int cellOffset = new Vector3Int(x, y, 0);
                        Vector3Int targetCell = Application.isPlaying ? enemyCell + cellOffset : grid.WorldToCell(center) + cellOffset;
                        Vector3 cellCenter = grid.GetCellCenterWorld(targetCell);

                        Gizmos.DrawWireCube(cellCenter, Vector3.one * cellSize * 0.9f);
                    }
                }
            }
        }
    }
}
