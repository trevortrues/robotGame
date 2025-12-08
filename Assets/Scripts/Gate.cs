using UnityEngine;

public class Gate : MonoBehaviour
{   
    public GameObject leftDoorOpen;
    public GameObject rightDoorOpen;
    public bool isOpenAtStart = false;
    
    [Header("Grid Settings")]
    public Grid grid;
    public bool snapToGridOnStart = true;

    [Header("Audio")]
    public AudioClip openSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    Collider2D _collider;
    SpriteRenderer _renderer;

    void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _renderer = GetComponent<SpriteRenderer>();
        
        if (grid == null)
        {
            grid = FindObjectOfType<Grid>();
        }
    }

    void Start()
    {
        if (snapToGridOnStart && grid != null)
        {
            Vector3Int cellPosition = grid.WorldToCell(transform.position);
            transform.position = grid.GetCellCenterWorld(cellPosition);
            
            Debug.Log($"Gate snapped to grid position: {transform.position}, Cell: {cellPosition}");
        }
        
        SetOpen(isOpenAtStart);
    }

    public void SetOpen(bool open)
    {
        Debug.Log($"Gate SetOpen called with: {open}");
        
        _collider.enabled = !open;

        if (_renderer != null)
        {
            _renderer.enabled = !open;
        }
        
        if (leftDoorOpen != null)
        {
            leftDoorOpen.SetActive(open);
            Debug.Log($"leftDoorOpen set to: {open}");
        }
        else
        {
            Debug.LogWarning("leftDoorOpen is not assigned!");
        }
        
        if (rightDoorOpen != null)
        {
            rightDoorOpen.SetActive(open);
            Debug.Log($"rightDoorOpen set to: {open}");
        }
        else
        {
            Debug.LogWarning("rightDoorOpen is not assigned!");
        }
        
        if (open)
        {
            gameObject.tag = "Untagged";
            Debug.Log("Gate opened - Tag set to Untagged");

            if (openSound != null)
            {
                AudioSource.PlayClipAtPoint(openSound, transform.position, soundVolume);
            }
        }
        else
        {
            gameObject.tag = "nonMoveable";
            Debug.Log("Gate closed - Tag set to nonMoveable");
        }
    }

    public void Toggle()
    {
        bool currentlyOpen = !_collider.enabled;
        SetOpen(!currentlyOpen);
    }
    
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = _collider != null && _collider.enabled ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                $"Tag: {gameObject.tag}\nCollider: {(_collider != null ? _collider.enabled.ToString() : "null")}");
            #endif
        }
        
        if (!Application.isPlaying && grid != null)
        {
            Vector3Int cellPosition = grid.WorldToCell(transform.position);
            Vector3 cellCenter = grid.GetCellCenterWorld(cellPosition);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(cellCenter, grid.cellSize * 0.95f);
        }
    }
}