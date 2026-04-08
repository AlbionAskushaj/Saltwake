using UnityEngine;

// Arcing brine projectile fired by BrineSpitter and the Brinewyrm boss.
// Uses physics gravity for the arc; despawns on terrain or on hit.
[RequireComponent(typeof(Rigidbody2D))]
public class BrineProjectile : MonoBehaviour
{
    [SerializeField] private float damage = 2f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask terrainMask = 1;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1.5f;
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController2D controller = other.GetComponent<CharacterController2D>();
            if (controller != null) controller.ApplyDamage(damage, transform.position);
            Destroy(gameObject);
            return;
        }
        if (((1 << other.gameObject.layer) & terrainMask) != 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Fallback for non-trigger setups
        if (collision.collider.CompareTag("Player"))
        {
            CharacterController2D controller = collision.collider.GetComponent<CharacterController2D>();
            if (controller != null) controller.ApplyDamage(damage, transform.position);
        }
        Destroy(gameObject);
    }
}
