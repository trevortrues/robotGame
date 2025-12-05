
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChaseEnemy : MonoBehaviour
{
    [Header("Grid References")]
    [SerializeField] Grid grid;
    [SerializeField] Tilemap wallsTilemap;
    [SerializeField] LayerMask obstacleLayers = -1;

    [Header("Movement")]
    [SerializeField] float stepTime = 0.12f;
    [SerializeField] float detectionRadius = 3f;

    Vector3Int enemyCell;
    bool moving;
    Transform player;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        enemyCell = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(enemyCell);
    }

    void Update()
    {
        if (moving || player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        Vector3Int dir;

        if (distance <= detectionRadius)
        {
            // CHASE MODE
            Vector3Int playerCell = grid.WorldToCell(player.position);
            Vector3Int delta = playerCell - enemyCell;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                dir = new Vector3Int(Mathf.Sign(delta.x) < 0 ? -1 : 1, 0, 0);
            else
                dir = new Vector3Int(0, Mathf.Sign(delta.y) < 0 ? -1 : 1, 0);
        }
        else
        {
            // WANDER MODE
            dir = RandomDirection();
        }

        Vector3Int next = enemyCell + dir;

        if (!IsBlocked(next))
            StartCoroutine(MoveTo(next));
    }

    IEnumerator MoveTo(Vector3Int target)
    {
        moving = true;
        Vector3 start = transform.position;
        Vector3 end = grid.GetCellCenterWorld(target);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / stepTime;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
        enemyCell = target;
        moving = false;
    }

    Vector3Int RandomDirection()
    {
        int r = Random.Range(0, 4);
        if (r == 0) return Vector3Int.up;
        if (r == 1) return Vector3Int.down;
        if (r == 2) return Vector3Int.left;
        return Vector3Int.right;
    }

    bool IsBlocked(Vector3Int cell)
    {
        if (wallsTilemap.HasTile(cell)) return true;

        Vector3 worldPos = grid.GetCellCenterWorld(cell);
        Vector2 boxSize = grid.cellSize * 0.8f;

        Collider2D[] hits = Physics2D.OverlapBoxAll(worldPos, boxSize, 0f, obstacleLayers);

        foreach (Collider2D hit in hits)
        {
            if (!hit.enabled) continue;
            if (hit.CompareTag("nonMoveable")) return true;
        }

        return false;
    }
}
