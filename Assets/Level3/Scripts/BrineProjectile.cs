using UnityEngine;

// Homing projectile — copied from Level 2's BebbleMovement pattern.
// Uses transform.position for movement (no physics velocity).
// Homes toward the player, then despawns after a timeout.
public class BrineProjectile : MonoBehaviour
{
    public float speed = 2.5f;
    public float homingStrength = 2f;
    public float damage = 1f;
    public float lifetime = 3.5f;

    private GameObject player;
    private float timeSinceSpawned = 0f;
    private bool isHoming = true;
    private Vector3 lastHomingDirection;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            lastHomingDirection = (player.transform.position - transform.position).normalized;
        else
            lastHomingDirection = Vector3.down;
    }

    // Called by BrineSpitter — sets initial direction
    public void Launch(Vector2 velocity)
    {
        lastHomingDirection = velocity.normalized;
    }

    void Update()
    {
        timeSinceSpawned += Time.deltaTime;

        if (timeSinceSpawned > lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Stop homing after 3 seconds — fly straight
        if (isHoming && timeSinceSpawned > 3f)
        {
            isHoming = false;
            if (player != null)
                lastHomingDirection = (player.transform.position - transform.position).normalized;
        }

        if (player != null && isHoming)
        {
            Vector2 direction = (player.transform.position - transform.position).normalized;
            transform.position += (Vector3)direction * homingStrength * Time.deltaTime;
        }
        else
        {
            transform.position += lastHomingDirection * speed * Time.deltaTime;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController2D controller = other.GetComponent<CharacterController2D>();
            if (controller != null)
            {
                controller.ApplyDamage(damage, transform.position);
            }
            Destroy(gameObject);
        }
    }
}
