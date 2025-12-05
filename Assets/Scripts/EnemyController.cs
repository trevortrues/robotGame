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

    Vector3Int enemyCell;
    bool moving;

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

            // ✅ HasWon REMOVED HERE
            if (!moving && !player.IsMoving)
            {
                Vector3Int dir = GetChaseDirection();
                if (dir != Vector3Int.zero)
                    yield return Move(dir);
            }

            yield return new WaitForSeconds(moveDelay);
        }
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
}
