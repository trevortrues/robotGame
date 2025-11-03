using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Collections;

public class playerController : MonoBehaviour
{
    [SerializeField] Grid grid;
    [SerializeField] Tilemap wallsTilemap;
    [SerializeField] Tilemap moveablesTilemap;
    [SerializeField] float stepTime = 0.12f;
    [SerializeField] bool preferHorizontal = true;

    Vector3Int playerCell;
    bool moving;

    void Awake()
    {
        if (!grid || !wallsTilemap || !moveablesTilemap) { enabled = false; return; }
        playerCell = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(playerCell);
    }

    void Update()
    {
        if (moving) return;
        var kb = Keyboard.current; if (kb == null) return;

        int x = 0, y = 0;
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) x = 1;
        else if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) x = -1;
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) y = 1;
        else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) y = -1;
        if (x != 0 && y != 0) { if (preferHorizontal) y = 0; else x = 0; }
        if (x == 0 && y == 0) return;


        StartCoroutine(TrySlide(new Vector3Int(x, y, 0)));
    }

    IEnumerator TrySlide(Vector3Int dir)
    {
        Vector3Int next = playerCell + dir;
        if (IsWall(next)) yield break;

        bool pushing = HasBox(next);
        Vector3Int boxFrom = next, boxTo = next + dir;

        if (pushing && (IsWall(boxTo) || HasBox(boxTo))) yield break;

        Vector3 playerStart = transform.position;
        Vector3 playerEnd = grid.GetCellCenterWorld(next);

        TileBase boxTile = null;
        Vector3 boxDelta = Vector3.zero;
        if (pushing)
        {
            boxTile = moveablesTilemap.GetTile(boxFrom);
            Vector3 boxStart = grid.GetCellCenterWorld(boxFrom);
            Vector3 boxEnd = grid.GetCellCenterWorld(boxTo);
            boxDelta = boxEnd - boxStart;
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
                Vector3 offset = Vector3.Lerp(Vector3.zero, boxDelta, u);
                moveablesTilemap.SetTransformMatrix(boxFrom, Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one));
            }
            yield return null;
        }

        transform.position = playerEnd;
        playerCell = next;

        if (pushing)
        {
            moveablesTilemap.SetTransformMatrix(boxFrom, Matrix4x4.identity);
            moveablesTilemap.SetTile(boxTo, boxTile);
            moveablesTilemap.SetTile(boxFrom, null);
            moveablesTilemap.RefreshTile(boxFrom);
            moveablesTilemap.RefreshTile(boxTo);
        }
        moving = false;
    }

    bool IsWall(Vector3Int c) => wallsTilemap.HasTile(c);
    bool HasBox(Vector3Int c) => moveablesTilemap.HasTile(c);
}
