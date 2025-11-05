using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TeleportBlock : MonoBehaviour
{
    [Header("Teleport settings")]
    [Tooltip("Where the player will be teleported to")]
    public Transform destination;

    [Tooltip("Seconds to ignore triggers after teleporting (prevents immediate re-teleport)")]
    public float cooldownAfterTeleport = 0.25f;

    [Tooltip("If true, teleporter is single-use and disables after use")]
    public bool singleUse = false;

    [Header("Optional effects")]
    public ParticleSystem enterEffect;
    public ParticleSystem exitEffect;
    public AudioClip teleportSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    // internal
    private bool isCoolingDown = false;
    private Collider2D myTrigger;

    void Awake()
    {
        myTrigger = GetComponent<Collider2D>();

        if (!myTrigger.isTrigger)
            Debug.LogWarning($"{name}: Collider2D should be set to 'Is Trigger' for teleport behavior.");

        if (destination == null)
            Debug.LogWarning($"{name}: Destination Transform not assigned. Assign a TeleportDestination object.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCoolingDown) return;
        if (!other.CompareTag("Player")) return;

        // Start teleport
        StartCoroutine(TeleportCoroutine(other));
    }

    private IEnumerator TeleportCoroutine(Collider2D playerCollider)
    {
        // safety checks
        if (destination == null) yield break;

        isCoolingDown = true;

        // optional enter effect/sound
        if (enterEffect) enterEffect.Play();
        if (teleportSound) AudioSource.PlayClipAtPoint(teleportSound, transform.position, soundVolume);

        // get player's Rigidbody2D (if it exists)
        Rigidbody2D rb = playerCollider.GetComponent<Rigidbody2D>();
        Transform playerT = playerCollider.transform;

        // disable player collider briefly to avoid re-triggering the source immediately
        Collider2D playerCol = playerCollider;
        if (playerCol != null) playerCol.enabled = false;

        // move player to destination position
        playerT.position = destination.position;

        // reset velocity if there's a Rigidbody2D
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            // optionally set rb.position to ensure physics picks it up next FixedUpdate
            rb.position = destination.position;
        }

        // optional exit effect/sound
        if (exitEffect) exitEffect.transform.position = destination.position;
        if (exitEffect) exitEffect.Play();
        if (teleportSound) AudioSource.PlayClipAtPoint(teleportSound, destination.position, soundVolume);

        // wait a short cooldown to avoid the destination teleporter sending the player back
        yield return new WaitForSeconds(cooldownAfterTeleport);

        // re-enable player collider
        if (playerCol != null) playerCol.enabled = true;

        // if single-use, disable this teleporter
        if (singleUse)
        {
            myTrigger.enabled = false;
            // optionally hide or destroy
            // gameObject.SetActive(false);
        }

        isCoolingDown = false;
    }
}
