using UnityEngine;

/// <summary>
/// Makes collected chips stay in screen space even when camera moves
/// Attach this to each collected chip sprite
/// </summary>
public class UIFollowCamera : MonoBehaviour
{
    [Header("Screen Position")]
    [Tooltip("Viewport position (0-1). (0,1) = top-left, (1,0) = bottom-right")]
    public Vector2 viewportPosition = new Vector2(0.05f, 0.95f);

    [Tooltip("Offset in world units from the viewport position")]
    public Vector2 worldOffset = Vector2.zero;

    [Tooltip("Distance from camera (Z position)")]
    public float distanceFromCamera = 5f;

    private Camera mainCamera;
    private int chipIndex = 0;
    private float chipSpacing = 0.5f;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("UIFollowCamera: No main camera found!");
            enabled = false;
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        UpdatePosition();
    }

    void UpdatePosition()
    {
        // Convert viewport position to world position relative to current camera
        Vector3 screenPos = new Vector3(viewportPosition.x, viewportPosition.y, distanceFromCamera);
        Vector3 worldPos = mainCamera.ViewportToWorldPoint(screenPos);

        // Add offset
        worldPos.x += worldOffset.x;
        worldPos.y += worldOffset.y;

        // Set Z to be in front of camera but behind UI
        worldPos.z = mainCamera.transform.position.z + distanceFromCamera;

        transform.position = worldPos;
    }

    /// <summary>
    /// Set chip index and spacing for automatic positioning
    /// </summary>
    public void SetChipIndex(int index, float spacing)
    {
        chipIndex = index;
        chipSpacing = spacing;

        // Calculate offset based on index
        int col = index % 10; // Max 10 per row
        int row = index / 10;

        worldOffset.x = col * spacing;
        worldOffset.y = -row * spacing;
    }

    /// <summary>
    /// Force position update immediately
    /// </summary>
    public void ForceUpdate()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        UpdatePosition();
    }
}
