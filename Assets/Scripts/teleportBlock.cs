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
    
    [Header("Debug")]
    public bool debugMode = false;
    
    private bool canTeleport = true;
    private bool playerInTrigger = false;
    private Collider2D myTrigger;
    private Coroutine cooldownCoroutine;
    
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
            if (grid == null)
            {
                Debug.LogError($"{name}: Grid reference not found!");
            }
        }
        
        if (moveablesTilemap == null && allowMoveables)
        {
            GameObject moveablesObj = GameObject.Find("Moveables");
            if (moveablesObj != null)
                moveablesTilemap = moveablesObj.GetComponent<Tilemap>();
                
            if (debugMode && moveablesTilemap == null)
                Debug.LogWarning($"{name}: Moveables tilemap not found!");
        }
    }
    
    void Update()
    {
        
        if (allowMoveables && canTeleport && moveablesTilemap != null && grid != null && destination != null)
        {
            Vector3Int currentCell = grid.WorldToCell(transform.position);
            if (moveablesTilemap.HasTile(currentCell))
            {
                if (debugMode) Debug.Log($"Found moveable tile at {currentCell}");
                
                if (!IsDestinationBlocked())
                {
                    TeleportMoveableTile(currentCell);
                }
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (debugMode) Debug.Log($"OnTriggerEnter: {other.name} with tag '{other.tag}'");
        
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
            
            if (IsDestinationBlocked())
            {
                if (debugMode) Debug.Log("Player teleport blocked - destination occupied");
                return;
            }
            
            var playerCtrl = other.GetComponent<playerController>();
            if (playerCtrl != null && playerCtrl.IsMoving)
            {
                if (debugMode) Debug.Log("Player is still moving, waiting...");
                return;
            }
            
            if (canTeleport && destination != null)
            {
                TeleportPlayer(other);
            }
        }
        else if (allowMoveables && other.CompareTag("Moveable"))
        {
            if (debugMode) Debug.Log($"Moveable object detected: {other.name}");
            
            if (!canTeleport)
            {
                if (debugMode) Debug.Log("Teleporter on cooldown");
                return;
            }
            
            if (IsDestinationBlocked())
            {
                if (debugMode) Debug.Log("Moveable teleport blocked - destination occupied");
                return;
            }
            
            if (destination != null)
            {
                TeleportMoveable(other.gameObject);
            }
        }
        else if (debugMode)
        {
            Debug.Log($"Object {other.name} entered but has wrong tag: '{other.tag}'");
        }
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (IsDestinationBlocked()) return;
            
            if (canTeleport && destination != null && playerInTrigger)
            {
                var playerCtrl = other.GetComponent<playerController>();
                if (playerCtrl != null && !playerCtrl.IsMoving)
                {
                    TeleportPlayer(other);
                }
            }
        }
        else if (allowMoveables && other.CompareTag("Moveable") && canTeleport)
        {
           
            if (destination != null && !IsDestinationBlocked())
            {
                TeleportMoveable(other.gameObject);
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
    
    private bool IsDestinationBlocked()
    {
        if (destination == null || grid == null) return false;
        
        Vector3Int destCell = grid.WorldToCell(destination.position);
        Vector3 destPos = grid.GetCellCenterWorld(destCell);
        
        if (moveablesTilemap != null && moveablesTilemap.HasTile(destCell))
        {
            if (debugMode) Debug.Log($"Destination blocked by moveable tile at {destCell}");
            return true;
        }
        
        float checkRadius = grid.cellSize.x * 0.4f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(destPos, checkRadius);
        
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Moveable"))
            {
                if (debugMode) Debug.Log($"Destination blocked by moveable object: {col.name}");
                return true;
            }
        }
        
        return false;
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
        
        if (debugMode) Debug.Log($"Teleported player to {destPos}");
        
        HandlePostTeleport();
    }
    
    private void TeleportMoveable(GameObject moveable)
    {
        if (!canTeleport || destination == null || grid == null) return;
        
        if (debugMode) Debug.Log($"Teleporting moveable {moveable.name}");
        
        PlayTeleportEffects(moveable.transform.position);
        
        Vector3Int destCell = grid.WorldToCell(destination.position);
        Vector3 destPos = grid.GetCellCenterWorld(destCell);
        
        moveable.transform.position = destPos;
        
        PlayTeleportEffects(destPos, true);
        
        Debug.Log($"Teleported moveable {moveable.name} from {moveable.transform.position} to {destPos} (cell: {destCell})");
        
        HandlePostTeleport();
    }
    
    private void TeleportMoveableTile(Vector3Int fromCell)
    {
        if (destination == null || grid == null || moveablesTilemap == null) return;
        
        Vector3Int destCell = grid.WorldToCell(destination.position);
        
        if (debugMode) Debug.Log($"Teleporting tile from {fromCell} to {destCell}");
        
        TileBase tile = moveablesTilemap.GetTile(fromCell);
        if (tile == null)
        {
            if (debugMode) Debug.Log("No tile found to teleport!");
            return;
        }
        
        moveablesTilemap.SetTile(fromCell, null);
        moveablesTilemap.SetTile(destCell, tile);
        moveablesTilemap.RefreshTile(fromCell);
        moveablesTilemap.RefreshTile(destCell);
        
        PlayTeleportEffects(grid.GetCellCenterWorld(fromCell));
        PlayTeleportEffects(grid.GetCellCenterWorld(destCell), true);
        
        Debug.Log($"Teleported moveable tile from {fromCell} to {destCell}");
        
        HandlePostTeleport();
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
                Gizmos.DrawWireCube(cellCenter, grid.cellSize * 0.95f);
                
                Vector3Int myCell = grid.WorldToCell(transform.position);
                Vector3 myCenter = grid.GetCellCenterWorld(myCell);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(myCenter, grid.cellSize * 0.9f);
            }
        }
    }
}