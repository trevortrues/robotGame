using UnityEngine;

public class RandomEnemy : MonoBehaviour
{
    public float speed = 2f;
    public float changeDirectionCooldown = 0.2f;

    private Rigidbody2D rb;
    private Vector2 currentDirection;
    private float cooldownTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        PickRandomDirection();
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;
        rb.linearVelocity = currentDirection * speed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (cooldownTimer <= 0)
        {
            PickRandomDirection();
            cooldownTimer = changeDirectionCooldown;
        }
    }

    void PickRandomDirection()
    {
        int random = UnityEngine.Random.Range(0, 4); // <- use UnityEngine.Random explicitly

        if (random == 0) currentDirection = Vector2.up;
        else if (random == 1) currentDirection = Vector2.down;
        else if (random == 2) currentDirection = Vector2.left;
        else if (random == 3) currentDirection = Vector2.right;
    }
}
