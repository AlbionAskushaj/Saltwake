using System.Collections;
using UnityEngine;

// Stationary turret. Lobs an arcing BrineProjectile at the player on a fixed cadence.
public class BrineSpitter : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float maxLife = 6f;
    [SerializeField] private float hitRecoveryTime = 0.1f;

    [Header("Firing")]
    [SerializeField] private GameObject brineProjectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private float arcHeightBias = 4f;
    [SerializeField] private float maxRange = 12f;
    [SerializeField] private float minRange = 2f;

    [Header("Drops")]
    [SerializeField] private GameObject heartPickupPrefab;
    [SerializeField] [Range(0f, 1f)] private float dropChance = 0.25f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform player;

    private float life;
    private float fireTimer;
    private float hitRecoverTimer;
    private bool isDying;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        life = maxLife;
        fireTimer = Random.Range(0.5f, fireInterval);
    }

    private void Update()
    {
        if (isDying) return;
        if (hitRecoverTimer > 0f) hitRecoverTimer -= Time.deltaTime;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        // Face the player visually
        if (spriteRenderer != null)
            spriteRenderer.flipX = player.position.x < transform.position.x;

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            float dist = Vector2.Distance(player.position, transform.position);
            if (dist <= maxRange && dist >= minRange)
                Fire();
            fireTimer = fireInterval;
        }
    }

    private void Fire()
    {
        if (brineProjectilePrefab == null) return;

        Vector3 origin = muzzle != null ? muzzle.position : transform.position;
        Vector2 toPlayer = (Vector2)player.position - (Vector2)origin;

        // Solve a simple lobbed velocity: horizontal speed proportional to distance,
        // vertical bumped up by arcHeightBias so it actually arcs.
        float horizontalTime = Mathf.Clamp(Mathf.Abs(toPlayer.x) / 6f, 0.4f, 1.4f);
        float vx = toPlayer.x / horizontalTime;
        // y = vy*t + 0.5*g*t^2  (g is negative; rb gravity scale = 1.5 so g = -14.7)
        float g = Physics2D.gravity.y * 1.5f;
        float vy = (toPlayer.y - 0.5f * g * horizontalTime * horizontalTime) / horizontalTime + arcHeightBias;

        GameObject proj = Instantiate(brineProjectilePrefab, origin, Quaternion.identity);
        BrineProjectile bp = proj.GetComponent<BrineProjectile>();
        if (bp != null) bp.Launch(new Vector2(vx, vy));
    }

    public void ApplyDamage(float damage)
    {
        if (isDying || Mathf.Approximately(damage, 0f)) return;
        life -= Mathf.Abs(damage);
        hitRecoverTimer = hitRecoveryTime;
        if (life <= 0f) StartCoroutine(Die());
    }

    private IEnumerator Die()
    {
        isDying = true;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (heartPickupPrefab != null && Random.value <= dropChance)
            Instantiate(heartPickupPrefab, transform.position, Quaternion.identity);
        yield return null;
        Destroy(gameObject);
    }
}
