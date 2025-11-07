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
    
    // Internal state
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
            // Try to find the grid in the scene
            grid = FindObjectOfType<Grid>();
            if (grid == null)
            {
                Debug.LogError($"{name}: Grid reference not found!");
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        playerInTrigger = true;
        
        // Check if player is currently moving using the public property
        var playerCtrl = other.GetComponent<playerController>();
        if (playerCtrl != null && playerCtrl.IsMoving)
        {
            return; // Don't teleport while player is moving
        }
        
        if (canTeleport && destination != null)
        {
            TeleportPlayer(other);
        }
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        // Try to teleport if player finished moving while on the teleporter
        if (canTeleport && destination != null && playerInTrigger)
        {
            var playerCtrl = other.GetComponent<playerController>();
            if (playerCtrl != null && !playerCtrl.IsMoving)
            {
                TeleportPlayer(other);
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        playerInTrigger = false;
        
        // Re-enable teleporting after player leaves (if not on cooldown)
        if (requireReentry && cooldownCoroutine == null)
        {
            canTeleport = true;
        }
    }
    
    private void TeleportPlayer(Collider2D playerCollider)
    {
        if (!canTeleport || destination == null) return;
        
        // Get player controller
        var playerCtrl = playerCollider.GetComponent<playerController>();
        if (playerCtrl == null || playerCtrl.IsMoving) return;
        
        // Effects at source
        if (enterEffect)
        {
            enterEffect.transform.position = transform.position;
            enterEffect.Play();
        }
        if (teleportSound)
        {
            AudioSource.PlayClipAtPoint(teleportSound, transform.position, soundVolume);
        }
        
        // Teleport the player using the public method
        playerCtrl.TeleportTo(destination.position);
        
        // Effects at destination
        Vector3 destPos = destination.position;
        if (grid != null)
        {
            Vector3Int destCell = grid.WorldToCell(destination.position);
            destPos = grid.GetCellCenterWorld(destCell);
        }
        
        if (exitEffect)
        {
            exitEffect.transform.position = destPos;
            exitEffect.Play();
        }
        if (teleportSound)
        {
            AudioSource.PlayClipAtPoint(teleportSound, destPos, soundVolume * 0.8f);
        }
        
        // Handle reuse logic
        if (singleUse)
        {
            canTeleport = false;
            myTrigger.enabled = false;
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer) renderer.enabled = false;
        }
        else if (requireReentry)
        {
            canTeleport = false;
            if (reuseCooldown > 0)
            {
                if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
                cooldownCoroutine = StartCoroutine(CooldownCoroutine());
            }
        }
        else if (reuseCooldown > 0)
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
        
        // Only re-enable if player has left the trigger (when requireReentry is true)
        if (!requireReentry || !playerInTrigger)
        {
            canTeleport = true;
        }
    }
    
    // Public method to reset the teleporter
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
    
    // For debugging
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
                Gizmos.DrawWireCube(cellCenter, Vector3.one * 0.9f);
            }
        }
    }
}