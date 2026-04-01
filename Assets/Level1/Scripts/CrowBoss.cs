using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CrowBoss : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 40f;
    [SerializeField] private float contactDamage = 2f;
    [SerializeField] private float touchDamageCooldown = 0.4f;
    [SerializeField] private float hitRecoveryTime = 0.15f;
    [SerializeField] private float hitKnockback = 3f;

    [Header("Movement")]
    [SerializeField] private float circleRadius = 5f;
    [SerializeField] private float circleSpeed = 1.5f;
    [SerializeField] private float steeringStrength = 6f;
    [SerializeField] private float hoverAmount = 0.4f;

    [Header("Dive Attack")]
    [SerializeField] private float diveSpeed = 14f;
    [SerializeField] private float diveTelegraphTime = 0.5f;

    [Header("Phase 2 - Summoning")]
    [SerializeField] private GameObject crowMinionPrefab;
    [SerializeField] private int maxMinions = 4;
    [SerializeField] private float summonInterval = 8f;
    [SerializeField] private int minionsPerSummon = 2;

    [Header("Phase 3 - Frenzy")]
    [SerializeField] private float frenzyDiveInterval = 0.8f;
    [SerializeField] private int frenzyDiveCount = 3;
    [SerializeField] private float frenzyPauseTime = 2f;
    [SerializeField] private float frenzySpeedMultiplier = 1.3f;

    [Header("Death")]
    [SerializeField] private GameObject fullHealPickupPrefab;
    [SerializeField] private Transform rewardSpawnPoint;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float health;
    private float circleAngle;
    private float touchCooldownTimer;
    private float hitRecoverTimer;
    private float hoverSeed;
    private bool isDying = false;
    private bool isDiving = false;
    private Transform player;
    private RoomManager roomManager;
    private List<GameObject> spawnedMinions = new List<GameObject>();

    private const float PHASE2_THRESHOLD = 0.7f;
    private const float PHASE3_THRESHOLD = 0.35f;

    private enum Phase { Circling, Summoning, Frenzy }
    private Phase currentPhase = Phase.Circling;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        health = maxHealth;
        hoverSeed = Random.Range(0f, 100f);
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        BossHealthBar.Show("The Stormcrow", maxHealth);
        StartCoroutine(BossLoop());
    }

    private IEnumerator BossLoop()
    {
        float diveTimer = 3f;
        float summonTimer = summonInterval;

        while (health > 0 && !isDying)
        {
            UpdatePhase();

            if (isDiving)
            {
                yield return null;
                continue;
            }

            if (player != null)
            {
                float currentSpeed = currentPhase == Phase.Frenzy ? circleSpeed * frenzySpeedMultiplier : circleSpeed;
                circleAngle += currentSpeed * Time.deltaTime;

                Vector2 circleTarget = (Vector2)player.position + new Vector2(
                    Mathf.Cos(circleAngle) * circleRadius,
                    Mathf.Sin(circleAngle) * circleRadius * 0.6f + 2f
                );
                circleTarget += GetHoverOffset();

                Vector2 toTarget = circleTarget - rb.position;
                Vector2 desired = toTarget.normalized * (currentSpeed * 3f);
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desired, steeringStrength * Time.deltaTime);
                UpdateFacing(rb.linearVelocity.x);
            }

            switch (currentPhase)
            {
                case Phase.Circling:
                    diveTimer -= Time.deltaTime;
                    if (diveTimer <= 0f)
                    {
                        StartCoroutine(DiveAttack());
                        diveTimer = 3f;
                    }
                    break;

                case Phase.Summoning:
                    diveTimer -= Time.deltaTime;
                    if (diveTimer <= 0f)
                    {
                        StartCoroutine(DiveAttack());
                        diveTimer = 2f;
                    }
                    summonTimer -= Time.deltaTime;
                    if (summonTimer <= 0f)
                    {
                        SpawnMinions();
                        summonTimer = summonInterval;
                    }
                    break;

                case Phase.Frenzy:
                    StartCoroutine(FrenzyAttack());
                    yield return new WaitUntil(() => !isDiving);
                    yield return new WaitForSeconds(frenzyPauseTime);
                    break;
            }

            yield return null;
        }
    }

    private void UpdatePhase()
    {
        float healthPercent = health / maxHealth;
        Phase newPhase;

        if (healthPercent <= PHASE3_THRESHOLD)
            newPhase = Phase.Frenzy;
        else if (healthPercent <= PHASE2_THRESHOLD)
            newPhase = Phase.Summoning;
        else
            newPhase = Phase.Circling;

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
        }
    }

    private IEnumerator DiveAttack()
    {
        if (isDiving || player == null) yield break;
        isDiving = true;

        for (int i = 0; i < 5; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(diveTelegraphTime / 10f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(diveTelegraphTime / 10f);
        }

        Vector2 diveTarget = player.position;
        Vector2 diveDir = (diveTarget - rb.position).normalized;
        float currentDiveSpeed = currentPhase == Phase.Frenzy ? diveSpeed * frenzySpeedMultiplier : diveSpeed;
        rb.linearVelocity = diveDir * currentDiveSpeed;

        yield return new WaitForSeconds(0.5f);

        rb.linearVelocity = rb.linearVelocity * 0.3f;
        isDiving = false;
    }

    private IEnumerator FrenzyAttack()
    {
        if (isDiving || player == null) yield break;
        isDiving = true;

        for (int i = 0; i < frenzyDiveCount; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = Color.white;

            Vector2 diveTarget = player.position;
            Vector2 diveDir = (diveTarget - rb.position).normalized;
            rb.linearVelocity = diveDir * diveSpeed * frenzySpeedMultiplier;

            yield return new WaitForSeconds(0.4f);
            rb.linearVelocity = rb.linearVelocity * 0.2f;

            if (i < frenzyDiveCount - 1)
                yield return new WaitForSeconds(frenzyDiveInterval);
        }

        isDiving = false;
    }

    private void SpawnMinions()
    {
        spawnedMinions.RemoveAll(m => m == null);

        int canSpawn = maxMinions - spawnedMinions.Count;
        int toSpawn = Mathf.Min(minionsPerSummon, canSpawn);

        for (int i = 0; i < toSpawn; i++)
        {
            Vector2 spawnPos = rb.position + Random.insideUnitCircle * 2f;
            GameObject minion = Instantiate(crowMinionPrefab, spawnPos, Quaternion.identity);
            spawnedMinions.Add(minion);

            if (roomManager != null)
                roomManager.TrackEnemy(minion);
        }
    }

    private Vector2 GetHoverOffset()
    {
        float time = Time.time + hoverSeed;
        return new Vector2(Mathf.Sin(time * 1.5f), Mathf.Cos(time * 2.2f)) * hoverAmount;
    }

    private void UpdateFacing(float horizontalVelocity)
    {
        if (spriteRenderer == null || Mathf.Abs(horizontalVelocity) < 0.05f) return;
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
        if (touchCooldownTimer > 0f || !other.CompareTag("Player")) return;

        CharacterController2D controller = other.GetComponent<CharacterController2D>();
        if (controller == null) return;

        controller.ApplyDamage(contactDamage, transform.position);
        touchCooldownTimer = touchDamageCooldown;
    }

    public void SetRoomManager(RoomManager manager)
    {
        roomManager = manager;
    }

    public void ApplyDamage(float damage)
    {
        if (isDying || Mathf.Approximately(damage, 0f)) return;

        float direction = Mathf.Sign(damage);
        health -= Mathf.Abs(damage);
        rb.linearVelocity = new Vector2(direction * hitKnockback, 1f);
        hitRecoverTimer = hitRecoveryTime;

        BossHealthBar.UpdateHealth(health);

        if (health <= 0f)
        {
            StartCoroutine(Die());
        }
    }

    private void Update()
    {
        if (touchCooldownTimer > 0f)
            touchCooldownTimer -= Time.deltaTime;
        if (hitRecoverTimer > 0f)
            hitRecoverTimer -= Time.deltaTime;
    }

    private IEnumerator Die()
    {
        isDying = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 1f;

        foreach (GameObject minion in spawnedMinions)
        {
            if (minion != null) Destroy(minion);
        }
        spawnedMinions.Clear();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        BossHealthBar.Hide();

        float fadeTime = 1.5f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            Color c = spriteRenderer.color;
            c.a = 1f - Mathf.Clamp01(elapsed / fadeTime);
            spriteRenderer.color = c;
            yield return null;
        }

        if (fullHealPickupPrefab != null)
        {
            Vector3 spawnPos = rewardSpawnPoint != null ? rewardSpawnPoint.position : transform.position;
            Instantiate(fullHealPickupPrefab, spawnPos, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
