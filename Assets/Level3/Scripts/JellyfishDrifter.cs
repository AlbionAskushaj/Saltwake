using System.Collections;
using UnityEngine;

// Slow floating enemy. Bobs along a vertical path and emits a telegraphed
// shock pulse on a fixed cadence. Players time their attacks between pulses.
public class JellyfishDrifter : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float maxLife = 4f;
    [SerializeField] private float pulseDamage = 2f;
    [SerializeField] private float hitRecoveryTime = 0.1f;
    [SerializeField] private float hitKnockback = 3f;

    [Header("Drift")]
    [SerializeField] private float driftAmplitude = 1.2f;
    [SerializeField] private float driftSpeed = 1.2f;
    [SerializeField] private float horizontalDrift = 0.4f;

    [Header("Pulse")]
    [SerializeField] private float pulseInterval = 2.5f;
    [SerializeField] private float pulseTelegraphTime = 0.6f;
    [SerializeField] private float pulseRadius = 2f;
    [SerializeField] private float pulseActiveTime = 0.25f;

    [Header("Visuals")]
    [SerializeField] private Color idleColor = new Color(0.7f, 0.9f, 1f, 1f);
    [SerializeField] private Color telegraphColor = new Color(1f, 1f, 0.4f, 1f);
    [SerializeField] private Color pulseColor = new Color(1f, 0.5f, 1f, 1f);

    [Header("Drops")]
    [SerializeField] private GameObject heartPickupPrefab;
    [SerializeField] [Range(0f, 1f)] private float dropChance = 0.2f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float life;
    private float hitRecoverTimer;
    private float pulseTimer;
    private bool isDying;
    private bool isPulseActive;
    private Vector2 homePosition;
    private float driftSeed;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        life = maxLife;
        homePosition = transform.position;
        driftSeed = Random.Range(0f, 100f);
        if (rb != null) rb.gravityScale = 0f;
        pulseTimer = pulseInterval;
        if (spriteRenderer != null) spriteRenderer.color = idleColor;
    }

    private void Update()
    {
        if (hitRecoverTimer > 0f) hitRecoverTimer -= Time.deltaTime;
        if (isDying) return;

        pulseTimer -= Time.deltaTime;
        if (pulseTimer <= 0f && !isPulseActive)
        {
            StartCoroutine(PulseRoutine());
            pulseTimer = pulseInterval;
        }
    }

    private void FixedUpdate()
    {
        if (isDying || hitRecoverTimer > 0f) return;
        float t = Time.time + driftSeed;
        Vector2 target = homePosition + new Vector2(
            Mathf.Sin(t * driftSpeed * 0.7f) * horizontalDrift,
            Mathf.Sin(t * driftSpeed) * driftAmplitude
        );
        Vector2 toTarget = target - rb.position;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, toTarget * 2f, 4f * Time.fixedDeltaTime);
    }

    private IEnumerator PulseRoutine()
    {
        // Telegraph
        float t = 0f;
        while (t < pulseTelegraphTime)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(idleColor, telegraphColor, t / pulseTelegraphTime);
            t += Time.deltaTime;
            yield return null;
        }

        // Active pulse — damage anything tagged Player inside the radius
        isPulseActive = true;
        if (spriteRenderer != null) spriteRenderer.color = pulseColor;

        Collider2D[] hits = Physics2D.OverlapCircleAll(rb.position, pulseRadius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            CharacterController2D controller = hit.GetComponent<CharacterController2D>();
            if (controller != null) controller.ApplyDamage(pulseDamage, transform.position);
        }

        yield return new WaitForSeconds(pulseActiveTime);
        if (spriteRenderer != null) spriteRenderer.color = idleColor;
        isPulseActive = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDying || !collision.collider.CompareTag("Player")) return;
        CharacterController2D controller = collision.collider.GetComponent<CharacterController2D>();
        if (controller != null) controller.ApplyDamage(pulseDamage, transform.position);
    }

    public void ApplyDamage(float damage)
    {
        if (isDying || Mathf.Approximately(damage, 0f)) return;
        float dir = Mathf.Sign(damage);
        life -= Mathf.Abs(damage);
        rb.linearVelocity = new Vector2(dir * hitKnockback, 1f);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, pulseRadius);
    }
}
