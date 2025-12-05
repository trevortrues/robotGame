
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RandomEnemy : MonoBehaviour
{
    [SerializeField] Grid grid;
    [SerializeField] Tilemap wallsTilemap;
    [SerializeField] LayerMask obstacleLayers = -1;
    [SerializeField] float stepTime = 0.15f;

    Vector3Int enemyCell;
    Vector3Int currentDir;
    bool moving;

    void Awake()
    {
        enemyCell = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(enemyCell);
        PickRandomDirection();
    }

    void Update()
    {
        if (moving) return;

        Vector3Int next = enemyCell + currentDir;

        if (IsBlocked(next))
        {
            PickRandomDirection();
            return;
        }

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

        PickRandomDirection();
    }

    void PickRandomDirection()
    {
        int r = Random.Range(0, 4);
        if (r == 0) currentDir = Vector3Int.up;
        else if (r == 1) currentDir = Vector3Int.down;
        else if (r == 2) currentDir = Vector3Int.left;
        else currentDir = Vector3Int.right;
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
