using UnityEngine;

public class ChaseEnemy : MonoBehaviour
{
    public float wanderSpeed = 1.5f;     // Speed when wandering
    public float chaseSpeed = 3f;        // Speed when chasing player
    public float detectionRadius = 3f;   // How close player must be to trigger chase

    private Transform player;
    private Rigidbody2D rb;
    private Vector2 wanderDirection;
    private float directionTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        PickRandomDirection();
    }

    void Update()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            // Chase player
            Vector2 chaseDirection = (player.position - transform.position).normalized;
            rb.linearVelocity = chaseDirection * chaseSpeed;
        }
        else
        {
            // Wander randomly
            directionTimer -= Time.deltaTime;
            if (directionTimer <= 0)
            {
                PickRandomDirection();
            }

            rb.linearVelocity = wanderDirection * wanderSpeed;
        }
    }

    void PickRandomDirection()
    {
        int r = UnityEngine.Random.Range(0, 4); // Explicit UnityEngine.Random

        if (r == 0) wanderDirection = Vector2.up;
        else if (r == 1) wanderDirection = Vector2.down;
        else if (r == 2) wanderDirection = Vector2.left;
        else if (r == 3) wanderDirection = Vector2.right;

        directionTimer = UnityEngine.Random.Range(1f, 2f); // Randomize time before next direction change
    }
}
