using System.Collections;
using UnityEngine;

public class CrowEnemy : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float maxLife = 8f;
    [SerializeField] private float contactDamage = 1f;
    [SerializeField] private float touchDamageCooldown = 0.4f;
    [SerializeField] private float hitRecoveryTime = 0.15f;
    [SerializeField] private float hitKnockback = 5f;

    [Header("Movement")]
    [SerializeField] private float wanderRadius = 2.5f;
    [SerializeField] private float wanderSpeed = 2.5f;
    [SerializeField] private float aggressiveSpeed = 5.5f;
    [SerializeField] private float sightRange = 8f;
    [SerializeField] private float sightHeight = 4f;
    [SerializeField] private float arrivalDistance = 0.35f;
    [SerializeField] private float wanderRetargetTime = 1.1f;
    [SerializeField] private float steeringStrength = 8f;
    [SerializeField] private float hoverAmount = 0.35f;

    [Header("Drops")]
    [SerializeField] private GameObject heartPickupPrefab;
    [SerializeField] [Range(0f, 1f)] private float dropChance = 0.2f;

    [Header("Detection")]
    [SerializeField] private LayerMask obstacleMask = 1;
    [SerializeField] private Transform player;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float life;
    private float wanderTimer;
    private float touchCooldownTimer;
    private float hitRecoverTimer;
    private float hoverSeed;
    private bool isDying;
    private Vector2 homePosition;
    private Vector2 wanderTarget;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        life = maxLife;
        homePosition = transform.position;
        hoverSeed = Random.Range(0f, 100f);
        PickWanderTarget(true);
    }

    private void FixedUpdate()
    {
        if (isDying)
        {
            return;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (touchCooldownTimer > 0f)
        {
            touchCooldownTimer -= Time.fixedDeltaTime;
        }

        if (hitRecoverTimer > 0f)
        {
            hitRecoverTimer -= Time.fixedDeltaTime;
            UpdateFacing(rb.linearVelocity.x);
            return;
        }

        bool canSeePlayer = TryGetVisiblePlayerPosition(out Vector2 playerPosition);
        Vector2 destination = canSeePlayer ? GetAggressiveTarget(playerPosition) : GetWanderTarget();
        float moveSpeed = canSeePlayer ? aggressiveSpeed : wanderSpeed;

        MoveTowards(destination, moveSpeed);
    }

    private void MoveTowards(Vector2 target, float moveSpeed)
    {
        Vector2 toTarget = target - rb.position;
        Vector2 desiredVelocity = toTarget.normalized * moveSpeed;

        if (toTarget.magnitude < arrivalDistance)
        {
            desiredVelocity *= toTarget.magnitude / Mathf.Max(arrivalDistance, 0.001f);
        }

        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, steeringStrength * Time.fixedDeltaTime);
        UpdateFacing(rb.linearVelocity.x);
    }

    private Vector2 GetWanderTarget()
    {
        wanderTimer -= Time.fixedDeltaTime;

        if (wanderTimer <= 0f || Vector2.Distance(rb.position, wanderTarget) <= arrivalDistance)
        {
            PickWanderTarget(false);
        }

        return wanderTarget + GetHoverOffset();
    }

    private Vector2 GetAggressiveTarget(Vector2 playerPosition)
    {
        Vector2 offset = new Vector2(Mathf.Sign(playerPosition.x - rb.position.x) * 0.6f, 0.35f);
        return playerPosition + offset + GetHoverOffset();
    }

    private Vector2 GetHoverOffset()
    {
        float time = Time.time + hoverSeed;
        return new Vector2(Mathf.Sin(time * 1.5f), Mathf.Cos(time * 2.2f)) * hoverAmount;
    }

    private void PickWanderTarget(bool forceImmediateRetarget)
    {
        wanderTimer = forceImmediateRetarget ? 0.05f : wanderRetargetTime;
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        wanderTarget = homePosition + randomOffset;
    }

    private bool TryGetVisiblePlayerPosition(out Vector2 playerPosition)
    {
        playerPosition = Vector2.zero;
        if (player == null)
        {
            return false;
        }

        Vector2 crowPosition = rb.position;
        playerPosition = player.position;
        Vector2 toPlayer = playerPosition - crowPosition;

        if (toPlayer.sqrMagnitude > sightRange * sightRange)
        {
            return false;
        }

        if (Mathf.Abs(toPlayer.y) > sightHeight)
        {
            return false;
        }

        RaycastHit2D hit = Physics2D.Linecast(crowPosition, playerPosition, obstacleMask);
        return hit.collider == null;
    }

    private void UpdateFacing(float horizontalVelocity)
    {
        if (spriteRenderer == null || Mathf.Abs(horizontalVelocity) < 0.05f)
        {
            return;
        }

        spriteRenderer.flipX = horizontalVelocity < 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (touchCooldownTimer > 0f || !other.CompareTag("Player"))
        {
            return;
        }

        CharacterController2D controller = other.GetComponent<CharacterController2D>();
        if (controller == null)
        {
            return;
        }

        controller.ApplyDamage(contactDamage, transform.position);
        touchCooldownTimer = touchDamageCooldown;
    }

    public void ApplyDamage(float damage)
    {
        if (isDying || Mathf.Approximately(damage, 0f))
        {
            return;
        }

        float direction = Mathf.Sign(damage);
        life -= Mathf.Abs(damage);
        rb.linearVelocity = new Vector2(direction * hitKnockback, 1.5f);
        hitRecoverTimer = hitRecoveryTime;
        UpdateFacing(rb.linearVelocity.x);

        if (life <= 0f)
        {
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        isDying = true;
        rb.linearVelocity = Vector2.zero;

        Collider2D collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            collider2D.enabled = false;
        }

        if (heartPickupPrefab != null && Random.value <= dropChance)
        {
            Instantiate(heartPickupPrefab, transform.position, Quaternion.identity);
        }

        yield return null;
        Destroy(gameObject);
    }
}
