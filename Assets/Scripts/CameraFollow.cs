using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player object to follow")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("How smoothly the camera follows (lower = smoother, 0 = instant)")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.125f;

    [Tooltip("Offset from the player position")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Camera Bounds (Optional)")]
    [Tooltip("Enable to limit camera movement to specific area")]
    public bool useBounds = false;

    [Tooltip("Minimum X position the camera can go")]
    public float minX = -10f;

    [Tooltip("Maximum X position the camera can go")]
    public float maxX = 10f;

    [Tooltip("Minimum Y position the camera can go")]
    public float minY = -10f;

    [Tooltip("Maximum Y position the camera can go")]
    public float maxY = 10f;

    [Header("Grid Snapping (Optional)")]
    [Tooltip("Snap camera to grid cells for pixel-perfect movement")]
    public bool snapToGrid = false;

    [Tooltip("Reference to the grid (optional, for snapping)")]
    public Grid grid;

    [Header("Look Ahead")]
    [Tooltip("Camera looks ahead in movement direction")]
    public bool lookAhead = false;

    [Tooltip("How far ahead to look")]
    public float lookAheadDistance = 2f;

    [Tooltip("How fast to look ahead")]
    public float lookAheadSpeed = 2f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 lookAheadOffset = Vector3.zero;

    void Start()
    {
        // Auto-find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("CameraFollow: Automatically found player");
            }
            else
            {
                Debug.LogError("CameraFollow: No target assigned and couldn't find Player tag!");
            }
        }

        // Auto-find grid if snapping is enabled
        if (snapToGrid && grid == null)
        {
            grid = FindObjectOfType<Grid>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calculate look-ahead offset
        if (lookAhead)
        {
            Vector3 targetLookAhead = Vector3.zero;

            // Get player controller to check movement direction
            playerController pc = target.GetComponent<playerController>();
            if (pc != null)
            {
                // This assumes you add a public property to playerController for last move direction
                // For now, we'll calculate based on position change
                Vector3 moveDirection = (target.position - transform.position).normalized;
                targetLookAhead = new Vector3(moveDirection.x, moveDirection.y, 0f) * lookAheadDistance;
            }

            lookAheadOffset = Vector3.Lerp(lookAheadOffset, targetLookAhead, lookAheadSpeed * Time.deltaTime);
        }

        // Calculate desired position
        Vector3 desiredPosition = target.position + offset + lookAheadOffset;

        // Apply smoothing
        Vector3 smoothedPosition;
        if (smoothSpeed > 0f)
        {
            smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        }
        else
        {
            smoothedPosition = desiredPosition;
        }

        // Apply grid snapping if enabled
        if (snapToGrid && grid != null)
        {
            Vector3Int cellPosition = grid.WorldToCell(smoothedPosition);
            smoothedPosition = grid.GetCellCenterWorld(cellPosition);
            smoothedPosition.z = offset.z; // Keep camera Z offset
        }

        // Apply bounds if enabled
        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }

        // Always keep the Z offset
        smoothedPosition.z = offset.z;

        transform.position = smoothedPosition;
    }

    /// <summary>
    /// Set camera bounds based on a Tilemap or level size
    /// </summary>
    public void SetBounds(float minX, float maxX, float minY, float maxY)
    {
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;
        useBounds = true;
    }

    /// <summary>
    /// Instantly snap camera to target without smoothing
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        Vector3 snapPosition = target.position + offset;

        if (useBounds)
        {
            snapPosition.x = Mathf.Clamp(snapPosition.x, minX, maxX);
            snapPosition.y = Mathf.Clamp(snapPosition.y, minY, maxY);
        }

        snapPosition.z = offset.z;
        transform.position = snapPosition;
    }

    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;

        // Draw bounds rectangle
        Gizmos.color = Color.yellow;

        Vector3 bottomLeft = new Vector3(minX, minY, 0f);
        Vector3 bottomRight = new Vector3(maxX, minY, 0f);
        Vector3 topRight = new Vector3(maxX, maxY, 0f);
        Vector3 topLeft = new Vector3(minX, maxY, 0f);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        // Draw center cross
        Gizmos.color = Color.cyan;
        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;
        Gizmos.DrawLine(new Vector3(centerX - 1f, centerY, 0f), new Vector3(centerX + 1f, centerY, 0f));
        Gizmos.DrawLine(new Vector3(centerX, centerY - 1f, 0f), new Vector3(centerX, centerY + 1f, 0f));
    }
}
