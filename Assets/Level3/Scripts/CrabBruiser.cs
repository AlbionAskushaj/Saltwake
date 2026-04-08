using System.Collections;
using UnityEngine;

// Armored ground enemy. Patrols a ledge and charges sideways at the player.
// Blocks all melee from the front — must be hit from behind or from above.
public class CrabBruiser : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float maxLife = 12f;
    [SerializeField] private float contactDamage = 2f;
    [SerializeField] private float touchDamageCooldown = 0.5f;
    [SerializeField] private float hitRecoveryTime = 0.1f;
    [SerializeField] private float hitKnockback = 3f;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chargeSpeed = 7f;
    [SerializeField] private float chargeTriggerRange = 5f;
    [SerializeField] private float chargeRecoverTime = 0.6f;

    [Header("Armor")]
    [Tooltip("Cosine threshold for what counts as a 'frontal' hit. 0.3 ≈ 70° cone in front.")]
    [SerializeField] private float frontalCosThreshold = 0.3f;
    [Tooltip("Hits coming from above (y >= this) bypass armor regardless of facing.")]
    [SerializeField] private float aboveYThreshold = 0.4f;

    [Header("Detection")]
    [SerializeField] private LayerMask groundMask = 1;
    [SerializeField] private Transform groundCheckFront;
    [SerializeField] private Transform wallCheckFront;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private float wallCheckDistance = 0.4f;

    [Header("Drops")]
    [SerializeField] private GameObject heartPickupPrefab;
    [SerializeField] [Range(0f, 1f)] private float dropChance = 0.3f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform player;

    private float life;
    private float touchCooldownTimer;
    private float hitRecoverTimer;
    private float chargeRecoverTimer;
    private int facingDir = 1;
    private bool isCharging;
    private bool isDying;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        life = maxLife;
    }

    private void Update()
    {
        if (touchCooldownTimer > 0f) touchCooldownTimer -= Time.deltaTime;
        if (hitRecoverTimer > 0f) hitRecoverTimer -= Time.deltaTime;
        if (chargeRecoverTimer > 0f) chargeRecoverTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (isDying || hitRecoverTimer > 0f) return;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Ledge / wall avoidance — flip if no ground ahead or wall in front
        if (groundCheckFront != null)
        {
            bool groundAhead = Physics2D.OverlapCircle(groundCheckFront.position, groundCheckRadius, groundMask);
            if (!groundAhead) FlipFacing();
        }
        if (wallCheckFront != null)
        {
            RaycastHit2D wall = Physics2D.Raycast(wallCheckFront.position, Vector2.right * facingDir, wallCheckDistance, groundMask);
            if (wall.collider != null) FlipFacing();
        }

        bool tryCharge = chargeRecoverTimer <= 0f && player != null
            && Mathf.Abs(player.position.x - rb.position.x) < chargeTriggerRange
            && Mathf.Abs(player.position.y - rb.position.y) < 1.5f;

        if (tryCharge && !isCharging)
        {
            // Face the player before charging
            int desired = player.position.x > rb.position.x ? 1 : -1;
            if (desired != facingDir) FlipFacing();
            isCharging = true;
        }

        float speed = isCharging ? chargeSpeed : patrolSpeed;
        rb.linearVelocity = new Vector2(facingDir * speed, rb.linearVelocity.y);

        // End charge after a fixed pursuit window or when we lose horizontal alignment
        if (isCharging && (player == null || Mathf.Abs(player.position.x - rb.position.x) > chargeTriggerRange + 1.5f))
        {
            isCharging = false;
            chargeRecoverTimer = chargeRecoverTime;
        }
    }

    private void FlipFacing()
    {
        facingDir = -facingDir;
        if (spriteRenderer != null) spriteRenderer.flipX = facingDir < 0;
        // Mirror the front-check transforms by negating local x
        if (groundCheckFront != null)
        {
            Vector3 lp = groundCheckFront.localPosition;
            lp.x = -lp.x;
            groundCheckFront.localPosition = lp;
        }
        if (wallCheckFront != null)
        {
            Vector3 lp = wallCheckFront.localPosition;
            lp.x = -lp.x;
            wallCheckFront.localPosition = lp;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryDamagePlayer(collision.collider);
    private void OnCollisionStay2D(Collision2D collision) => TryDamagePlayer(collision.collider);

    private void TryDamagePlayer(Collider2D other)
    {
        if (touchCooldownTimer > 0f || !other.CompareTag("Player")) return;
        CharacterController2D controller = other.GetComponent<CharacterController2D>();
        if (controller == null) return;
        controller.ApplyDamage(contactDamage, transform.position);
        touchCooldownTimer = touchDamageCooldown;
    }

    // Standard ApplyDamage entry point — used by all damageable enemies in the project.
    // The sign of `damage` carries the horizontal direction of the hit (Attack.cs convention).
    public void ApplyDamage(float damage)
    {
        if (isDying || Mathf.Approximately(damage, 0f)) return;

        float hitDirX = Mathf.Sign(damage);

        // Resolve hit position from the player so we can check angle
        Vector2 hitFromAbove = Vector2.zero;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            hitFromAbove = (Vector2)p.transform.position - rb.position;
        }

        bool fromAbove = hitFromAbove.y >= aboveYThreshold;
        bool fromBehind = hitDirX != 0f && Mathf.Sign(hitDirX) == facingDir;
        // The Attack swing pushes a sign matching the player's facing — if the player is BEHIND
        // the crab, the hit-direction sign equals the crab's facing (since the player is on the
        // crab's back side and swinging forward into it). That's our "behind" signal.

        bool armored = !(fromAbove || fromBehind);

        if (armored)
        {
            // Sparks: small recoil only, no damage
            rb.linearVelocity = new Vector2(-hitDirX * 1.5f, rb.linearVelocity.y);
            hitRecoverTimer = 0.05f;
            return;
        }

        life -= Mathf.Abs(damage);
        rb.linearVelocity = new Vector2(hitDirX * hitKnockback, 1f);
        hitRecoverTimer = hitRecoveryTime;

        if (life <= 0f) StartCoroutine(Die());
    }

    private IEnumerator Die()
    {
        isDying = true;
        rb.linearVelocity = Vector2.zero;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (heartPickupPrefab != null && Random.value <= dropChance)
            Instantiate(heartPickupPrefab, transform.position, Quaternion.identity);

        yield return null;
        Destroy(gameObject);
    }
}
