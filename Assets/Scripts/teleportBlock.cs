using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public class TeleportBlock : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Where the player will be teleported to")]
    public Transform destination;
    
    [Tooltip("Reference to the grid (same as player uses)")]
    public Grid grid;
    
    [Tooltip("If true, teleporter is single-use")]
    public bool singleUse = false;
    
    [Header("Moveable Settings")]
    [Tooltip("Can moveables use this teleporter?")]
    public bool allowMoveables = true;
    
    [Tooltip("Reference to moveables tilemap")]
    public Tilemap moveablesTilemap;
    
    [Header("Reactivation Settings")]
    [Tooltip("Require player to step off and back on to use again")]
    public bool requireReentry = true;
    
    [Tooltip("Cooldown in seconds before teleporter can be used again")]
    public float reuseCooldown = 0.5f;
    
    [Header("Optional Effects")]
    public ParticleSystem enterEffect;
    public ParticleSystem exitEffect;
    public AudioClip teleportSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;
    
    private bool canTeleport = true;
    private bool playerInTrigger = false;
    private Collider2D myTrigger;
    private Coroutine cooldownCoroutine;
    private GameObject lastTeleportedMoveable = null;
    
    void Awake()
    {
        myTrigger = GetComponent<Collider2D>();
        
        if (!myTrigger.isTrigger)
        {
            Debug.LogWarning($"{name}: Setting Collider2D to trigger mode.");
            myTrigger.isTrigger = true;
        }
        
        if (destination == null)
        {
            Debug.LogError($"{name}: Destination Transform not assigned!");
        }
        
        if (grid == null)
        {
            grid = FindFirstObjectByType<Grid>();
        }
        
        if (moveablesTilemap == null && allowMoveables)
        {
            GameObject moveablesObj = GameObject.Find("Moveables");
            if (moveablesObj != null)
                moveablesTilemap = moveablesObj.GetComponent<Tilemap>();
        }
    }
    
    void Update()
    {
        if (!allowMoveables || !canTeleport || grid == null || destination == null)
            return;

        Vector3Int currentCell = grid.WorldToCell(transform.position);

        // Check for moveable tiles - instant teleport
        if (moveablesTilemap != null && moveablesTilemap.HasTile(currentCell))
        {
            if (!IsDestinationBlockedByMoveable())
            {
                TeleportMoveableTile(currentCell);
                return; // Exit after teleporting tile
            }
        }

        // Check for moveable GameObjects with larger radius to catch mid-push
        Vector3 worldPos = grid.GetCellCenterWorld(currentCell);
        float checkRadius = grid.cellSize.x * 0.6f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, checkRadius);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Moveable") && col.gameObject != lastTeleportedMoveable)
            {
                if (!IsDestinationBlockedByMoveable())
                {
                    TeleportMoveable(col.gameObject);
                    lastTeleportedMoveable = col.gameObject;
                    StartCoroutine(ClearLastTeleportedMoveable());
                }
                break;
            }
        }
    }

    IEnumerator ClearLastTeleportedMoveable()
    {
        yield return new WaitForSeconds(0.2f);
        lastTeleportedMoveable = null;
    }

    bool IsMoveableOnTeleporter()
    {
        if (grid == null) return false;

        Vector3Int currentCell = grid.WorldToCell(transform.position);

        if (moveablesTilemap != null && moveablesTilemap.HasTile(currentCell))
            return true;

        // Use larger radius to catch moveables being pushed onto teleporter
        Vector3 worldPos = grid.GetCellCenterWorld(currentCell);
        float checkRadius = grid.cellSize.x * 0.7f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, checkRadius);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Moveable"))
                return true;
        }

        return false;
    }
    
    bool IsDestinationBlockedByMoveable()
    {
        if (destination == null || grid == null) return false;

        Vector3Int destCell = grid.WorldToCell(destination.position);
        Vector3 destPos = grid.GetCellCenterWorld(destCell);

        if (moveablesTilemap != null && moveablesTilemap.HasTile(destCell))
            return true;

        float checkRadius = grid.cellSize.x * 0.4f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(destPos, checkRadius);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Moveable"))
                return true;
        }

        return false;
    }


    public void OnMoveablePushedHere()
    {
        if (!allowMoveables || grid == null || destination == null)
            return;

        if (IsDestinationBlockedByMoveable())
            return;

        Vector3Int currentCell = grid.WorldToCell(transform.position);

        if (moveablesTilemap != null && moveablesTilemap.HasTile(currentCell))
        {
            TeleportMoveableTile(currentCell);
            return;
        }

        Vector3 worldPos = grid.GetCellCenterWorld(currentCell);
        float checkRadius = grid.cellSize.x * 0.9f; // Large radius to ensure we find it
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, checkRadius);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Moveable"))
            {
                TeleportMoveable(col.gameObject);
                lastTeleportedMoveable = col.gameObject;
                StartCoroutine(ClearLastTeleportedMoveable());
                return;
            }
        }
    }

    void TryTeleportMoveableImmediately()
    {
        if (!allowMoveables || !canTeleport || grid == null || destination == null)
            return;

        if (IsDestinationBlockedByMoveable())
            return;

        Vector3Int currentCell = grid.WorldToCell(transform.position);

        // Check for moveable tiles first
        if (moveablesTilemap != null && moveablesTilemap.HasTile(currentCell))
        {
            TeleportMoveableTile(currentCell);
            return;
        }

        // Check for moveable GameObjects
        Vector3 worldPos = grid.GetCellCenterWorld(currentCell);
        float checkRadius = grid.cellSize.x * 0.7f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, checkRadius);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Moveable") && col.gameObject != lastTeleportedMoveable)
            {
                TeleportMoveable(col.gameObject);
                lastTeleportedMoveable = col.gameObject;
                StartCoroutine(ClearLastTeleportedMoveable());
                return;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;

            // Teleport any moveable on this teleporter FIRST before player teleports
            TryTeleportMoveableImmediately();

            if (IsMoveableOnTeleporter())
                return;

            if (IsDestinationBlockedByMoveable())
                return;

            var playerCtrl = other.GetComponent<playerController>();
            if (playerCtrl != null && playerCtrl.IsMoving)
                return;

            if (canTeleport && destination != null)
            {
                TeleportPlayer(other);
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Teleport any moveable on this teleporter FIRST before player teleports
            TryTeleportMoveableImmediately();

            if (IsMoveableOnTeleporter()) return;
            if (IsDestinationBlockedByMoveable()) return;

            if (canTeleport && destination != null && playerInTrigger)
            {
                var playerCtrl = other.GetComponent<playerController>();
                if (playerCtrl != null && !playerCtrl.IsMoving)
                {
                    TeleportPlayer(other);
                }
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        playerInTrigger = false;
        
        if (requireReentry && cooldownCoroutine == null)
        {
            canTeleport = true;
        }
    }
    
    private void TeleportPlayer(Collider2D playerCollider)
    {
        if (!canTeleport || destination == null) return;
        
        var playerCtrl = playerCollider.GetComponent<playerController>();
        if (playerCtrl == null || playerCtrl.IsMoving) return;
        
        PlayTeleportEffects(transform.position);
        playerCtrl.TeleportTo(destination.position);
        
        Vector3 destPos = destination.position;
        if (grid != null)
        {
            Vector3Int destCell = grid.WorldToCell(destination.position);
            destPos = grid.GetCellCenterWorld(destCell);
        }
        
        PlayTeleportEffects(destPos, true);
        HandlePostTeleport();
    }
    
    private void TeleportMoveable(GameObject moveable)
    {
        if (destination == null || grid == null) return;
        
        PlayTeleportEffects(moveable.transform.position);
        
        Vector3Int destCell = grid.WorldToCell(destination.position);
        Vector3 destPos = grid.GetCellCenterWorld(destCell);
        
        moveable.transform.position = destPos;
        
        PlayTeleportEffects(destPos, true);
    }
    
    private void TeleportMoveableTile(Vector3Int fromCell)
    {
        if (destination == null || grid == null || moveablesTilemap == null) return;
        
        Vector3Int destCell = grid.WorldToCell(destination.position);
        
        TileBase tile = moveablesTilemap.GetTile(fromCell);
        if (tile == null) return;
        
        moveablesTilemap.SetTile(fromCell, null);
        moveablesTilemap.SetTile(destCell, tile);
        moveablesTilemap.RefreshTile(fromCell);
        moveablesTilemap.RefreshTile(destCell);
        
        PlayTeleportEffects(grid.GetCellCenterWorld(fromCell));
        PlayTeleportEffects(grid.GetCellCenterWorld(destCell), true);
    }
    
    private void PlayTeleportEffects(Vector3 position, bool isExit = false)
    {
        var effect = isExit ? exitEffect : enterEffect;
        if (effect)
        {
            effect.transform.position = position;
            effect.Play();
        }
        
        if (teleportSound)
        {
            float volume = isExit ? soundVolume * 0.8f : soundVolume;
            AudioSource.PlayClipAtPoint(teleportSound, position, volume);
        }
    }
    
    private void HandlePostTeleport()
    {
        if (singleUse)
        {
            canTeleport = false;
            myTrigger.enabled = false;
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer) renderer.enabled = false;
        }
        else if (requireReentry || reuseCooldown > 0)
        {
            canTeleport = false;
            if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = StartCoroutine(CooldownCoroutine());
        }
    }
    
    private IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(reuseCooldown);
        cooldownCoroutine = null;
        
        if (!requireReentry || !playerInTrigger)
        {
            canTeleport = true;
        }
    }
    
    public void ResetTeleporter()
    {
        canTeleport = true;
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }
        myTrigger.enabled = true;
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer) renderer.enabled = true;
    }
    
    void OnDrawGizmos()
    {
        if (destination != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, destination.position);
            Gizmos.DrawWireSphere(destination.position, 0.3f);
            
            if (grid != null)
            {
                Vector3Int destCell = grid.WorldToCell(destination.position);
                Vector3 cellCenter = grid.GetCellCenterWorld(destCell);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(cellCenter, grid.cellSize * 0.9f);
            }
        }
    }
}