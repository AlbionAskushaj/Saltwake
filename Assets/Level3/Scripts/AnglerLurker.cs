using System.Collections;
using UnityEngine;

// Sea-cave ambusher. Idles with a glowing lure; lunges in a straight line when the
// player enters its forward sight cone. Hits a wall = stunned and vulnerable.
public class AnglerLurker : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float maxLife = 6f;
    [SerializeField] private float contactDamage = 2f;
    [SerializeField] private float touchDamageCooldown = 0.4f;
    [SerializeField] private float hitRecoveryTime = 0.15f;
    [SerializeField] private float hitKnockback = 4f;

    [Header("Sight & Lunge")]
    [SerializeField] private float sightRange = 5f;
    [SerializeField] private float sightHeight = 1.6f;
    [SerializeField] private float lungeTelegraphTime = 0.5f;
    [SerializeField] private float lungeSpeed = 14f;
    [SerializeField] private float lungeDuration = 0.55f;
    [SerializeField] private float stunDuration = 1.5f;
    [SerializeField] private float lungeCooldown = 2f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer lureRenderer;
    [SerializeField] private Color lureIdleColor = new Color(0.55f, 1f, 0.85f, 1f);
    [SerializeField] private Color lureTelegraphColor = Color.red;

    [Header("Drops")]
    [SerializeField] private GameObject heartPickupPrefab;
    [SerializeField] [Range(0f, 1f)] private float dropChance = 0.25f;

    [Header("Detection")]
    [SerializeField] private LayerMask obstacleMask = 1;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform player;

    private float life;
    private float touchCooldownTimer;
    private float hitRecoverTimer;
    private float lungeCooldownTimer;
    private bool isLunging;
    private bool isStunned;
    private bool isDying;
    private int facingDir = 1;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        life = maxLife;
        if (lureRenderer != null) lureRenderer.color = lureIdleColor;
    }

    private void Update()
    {
        if (touchCooldownTimer > 0f) touchCooldownTimer -= Time.deltaTime;
        if (hitRecoverTimer > 0f) hitRecoverTimer -= Time.deltaTime;
        if (lungeCooldownTimer > 0f) lungeCooldownTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (isDying || isLunging || isStunned || hitRecoverTimer > 0f) return;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        // Idle: damp velocity, face the player
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 8f * Time.fixedDeltaTime);

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        if (Mathf.Abs(toPlayer.x) > 0.05f)
        {
            facingDir = toPlayer.x > 0f ? 1 : -1;
            if (spriteRenderer != null) spriteRenderer.flipX = facingDir < 0;
        }

        if (lungeCooldownTimer <= 0f && PlayerInSightCone(toPlayer))
        {
            StartCoroutine(LungeRoutine());
        }
    }

    private bool PlayerInSightCone(Vector2 toPlayer)
    {
        // Player must be in front of the lurker, within range, within vertical band,
        // and not occluded by terrain.
        if (Mathf.Sign(toPlayer.x) != facingDir) return false;
        if (toPlayer.sqrMagnitude > sightRange * sightRange) return false;
        if (Mathf.Abs(toPlayer.y) > sightHeight) return false;
        RaycastHit2D hit = Physics2D.Linecast(rb.position, player.position, obstacleMask);
        return hit.collider == null;
    }

    private IEnumerator LungeRoutine()
    {
        isLunging = true;
        Vector2 lungeDir = new Vector2(facingDir, 0f);

        // Telegraph: lure flashes red
        float t = 0f;
        while (t < lungeTelegraphTime)
        {
            if (lureRenderer != null)
                lureRenderer.color = Mathf.PingPong(t * 12f, 1f) > 0.5f ? lureTelegraphColor : lureIdleColor;
            t += Time.deltaTime;
            yield return null;
        }
        if (lureRenderer != null) lureRenderer.color = lureIdleColor;

        // Lunge
        float lungeT = 0f;
        bool hitWall = false;
        while (lungeT < lungeDuration && !hitWall)
        {
            rb.linearVelocity = lungeDir * lungeSpeed;
            // Stop early if we slam into something
            RaycastHit2D wall = Physics2D.Raycast(rb.position, lungeDir, 0.6f, obstacleMask);
            if (wall.collider != null) hitWall = true;
            lungeT += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isLunging = false;

        if (hitWall)
        {
            isStunned = true;
            if (spriteRenderer != null) spriteRenderer.color = new Color(0.7f, 0.7f, 1f);
            yield return new WaitForSeconds(stunDuration);
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            isStunned = false;
        }

        lungeCooldownTimer = lungeCooldown;
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

    public void ApplyDamage(float damage)
    {
        if (isDying || Mathf.Approximately(damage, 0f)) return;

        // Stunned anglers take bonus damage — rewards the bait-and-punish loop
        float dealt = Mathf.Abs(damage) * (isStunned ? 1.5f : 1f);

        float dir = Mathf.Sign(damage);
        life -= dealt;
        rb.linearVelocity = new Vector2(dir * hitKnockback, 1.5f);
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
