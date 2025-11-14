using UnityEngine;

public class Gate : MonoBehaviour
{   public GameObject leftDoorOpen;
    public GameObject rightDoorOpen;
    public bool isOpenAtStart = false;

    Collider2D _collider;
    SpriteRenderer _renderer;

    void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _renderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        SetOpen(isOpenAtStart);
    }

    public void SetOpen(bool open)
    {
        // Logic: if open → disable collider, maybe change sprite/alpha
        _collider.enabled = !open;

        if (_renderer != null)
        {
            _renderer.enabled = !open;
        }
        
        leftDoorOpen.SetActive(open);
        rightDoorOpen.SetActive(open);
    }

    public void Toggle()
    {
        bool currentlyOpen = !_collider.enabled;
        SetOpen(!currentlyOpen);
    }
}
