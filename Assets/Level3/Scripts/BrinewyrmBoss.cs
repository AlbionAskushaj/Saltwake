using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The Brinewyrm — Level 3 boss. Lives beneath the arena floor and surfaces from
// designated points to attack. Invulnerable while submerged. Three phases:
//   P1 (100% → 65%): Single surface attacks with telegraph.
//   P2 (65%  → 30%): Adds rising-tide hazard cycles + summons jellyfish minions.
//   P3 (30%  →  0%): Frenzy — short telegraphs, double surfaces, brine fans.
public class BrinewyrmBoss : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 60f;
    [SerializeField] private float contactDamage = 3f;
    [SerializeField] private float touchDamageCooldown = 0.5f;

    [Header("Surfacing")]
    [SerializeField] private Transform[] surfacePoints;
    [SerializeField] private float telegraphTimeP1 = 0.7f;
    [SerializeField] private float telegraphTimeP3 = 0.3f;
    [SerializeField] private float surfacedDuration = 1.8f;
    [SerializeField] private float pauseBetweenSurfacesP1 = 1.5f;
    [SerializeField] private float pauseBetweenSurfacesP2 = 1.1f;
    [SerializeField] private float pauseBetweenSurfacesP3 = 0.6f;

    [Header("Phase 2 — Tide & Summons")]
    [SerializeField] private RisingTideHazard tideHazard;
    [SerializeField] private GameObject jellyfishPrefab;
    [SerializeField] private int maxMinions = 3;
    [SerializeField] private float summonInterval = 6f;

    [Header("Phase 3 — Frenzy Spit")]
    [SerializeField] private GameObject brineProjectilePrefab;
    [SerializeField] private int spitFanCount = 3;
    [SerializeField] private float spitArcSpread = 35f;
    [SerializeField] private float spitSpeed = 9f;

    [Header("Death")]
    [SerializeField] private GameObject fullHealPickupPrefab;
    [SerializeField] private Transform rewardSpawnPoint;
    [TextArea] [SerializeField] private string deathDialogue =
        "The wyrm sinks. Salt fills my mouth — and a memory: a ship's wheel, my hands, a name I can almost hear.";
    [SerializeField] private float deathDialogueDuration = 6f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color idleColor = new Color(0.4f, 0.7f, 0.9f);
    [SerializeField] private Color telegraphColor = Color.red;

    private float health;
    private bool isDying;
    private bool isVulnerable;
    private float touchCooldownTimer;
    private Transform player;
    private RoomManager roomManager;
    private List<GameObject> spawnedMinions = new List<GameObject>();

    private const float PHASE2_THRESHOLD = 0.65f;
    private const float PHASE3_THRESHOLD = 0.30f;

    private enum Phase { Surface, TideAndSummon, Frenzy }
    private Phase currentPhase = Phase.Surface;

    public void SetRoomManager(RoomManager mgr) { roomManager = mgr; }

    // Called by RoomManager right after Instantiate so the boss can pick up scene-only
    // references (surface points, tide hazard, reward spawn) that a prefab can't store.
    // Any argument can be null — those features simply won't run.
    public void Initialize(Transform[] surfacePoints, RisingTideHazard tide, Transform rewardSpawn)
    {
        if (surfacePoints != null && surfacePoints.Length > 0)
            this.surfacePoints = surfacePoints;
        if (tide != null)
            this.tideHazard = tide;
        if (rewardSpawn != null)
            this.rewardSpawnPoint = rewardSpawn;
    }

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        health = maxHealth;
        if (spriteRenderer != null) spriteRenderer.color = idleColor;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        BossHealthBar.Show("The Brinewyrm", maxHealth);
        StartCoroutine(BossLoop());
    }

    private void Update()
    {
        if (touchCooldownTimer > 0f) touchCooldownTimer -= Time.deltaTime;
    }

    private IEnumerator BossLoop()
    {
        // Start hidden below the arena
        SetHidden();
        yield return new WaitForSeconds(1f);

        float summonTimer = summonInterval;

        while (health > 0 && !isDying)
        {
            UpdatePhase();

            // P2/P3 hazard management
            if (currentPhase == Phase.TideAndSummon || currentPhase == Phase.Frenzy)
            {
                if (tideHazard != null && !tideHazard.IsActive)
                    tideHazard.BeginCycling();
            }

            // Summons (P2 only — P3 is too lethal alone for the player)
            if (currentPhase == Phase.TideAndSummon)
            {
                summonTimer -= Time.deltaTime;
                if (summonTimer <= 0f)
                {
                    SummonJellyfish();
                    summonTimer = summonInterval;
                }
            }

            float telegraph = currentPhase == Phase.Frenzy ? telegraphTimeP3 : telegraphTimeP1;
            float pause = currentPhase switch
            {
                Phase.Surface => pauseBetweenSurfacesP1,
                Phase.TideAndSummon => pauseBetweenSurfacesP2,
                Phase.Frenzy => pauseBetweenSurfacesP3,
                _ => pauseBetweenSurfacesP1
            };

            // One surface attack
            yield return StartCoroutine(SurfaceAttack(telegraph));
            if (isDying) yield break;

            // Frenzy: immediate second surface + spit fan
            if (currentPhase == Phase.Frenzy && health > 0)
            {
                yield return new WaitForSeconds(0.2f);
                yield return StartCoroutine(SurfaceAttack(telegraph));
                SpitFan();
            }

            yield return new WaitForSeconds(pause);
        }
    }

    private void UpdatePhase()
    {
        float pct = health / maxHealth;
        Phase next;
        if (pct <= PHASE3_THRESHOLD) next = Phase.Frenzy;
        else if (pct <= PHASE2_THRESHOLD) next = Phase.TideAndSummon;
        else next = Phase.Surface;
        currentPhase = next;
    }

    private IEnumerator SurfaceAttack(float telegraphTime)
    {
        if (surfacePoints == null || surfacePoints.Length == 0) yield break;

        // Pick the surface point closest to the player so the fight stays engaging
        Transform target = surfacePoints[0];
        float bestDist = float.MaxValue;
        if (player != null)
        {
            foreach (Transform sp in surfacePoints)
            {
                if (sp == null) continue;
                float d = Mathf.Abs(sp.position.x - player.position.x);
                if (d < bestDist) { bestDist = d; target = sp; }
            }
        }

        // Move to the surface point underground (still hidden)
        transform.position = target.position + Vector3.down * 4f;

        // Telegraph at the surface point
        float t = 0f;
        while (t < telegraphTime)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Mathf.PingPong(t * 14f, 1f) > 0.5f ? telegraphColor : idleColor;
            t += Time.deltaTime;
            yield return null;
        }
        if (spriteRenderer != null) spriteRenderer.color = idleColor;

        // Burst up — vulnerable window
        SetExposed(target.position);
        isVulnerable = true;

        yield return new WaitForSeconds(surfacedDuration);

        isVulnerable = false;
        SetHidden();
    }

    private void SetExposed(Vector3 surfacePos)
    {
        transform.position = surfacePos;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        if (spriteRenderer != null)
        {
            Color c = idleColor;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }

    private void SetHidden()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (spriteRenderer != null)
        {
            Color c = idleColor;
            c.a = 0.15f;
            spriteRenderer.color = c;
        }
    }

    private void SpitFan()
    {
        if (brineProjectilePrefab == null || player == null) return;
        // Spawn multiple homing projectiles — they find the player on their own
        for (int i = 0; i < spitFanCount; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 1f), 0f);
            Instantiate(brineProjectilePrefab, transform.position + offset, Quaternion.identity);
        }
    }

    private void SummonJellyfish()
    {
        if (jellyfishPrefab == null) return;
        spawnedMinions.RemoveAll(m => m == null);
        if (spawnedMinions.Count >= maxMinions) return;

        Vector2 spawnPos = (Vector2)transform.position + Vector2.up * 4f + Random.insideUnitCircle * 1.5f;
        GameObject m = Instantiate(jellyfishPrefab, spawnPos, Quaternion.identity);
        spawnedMinions.Add(m);
        if (roomManager != null) roomManager.TrackEnemy(m);
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
        // Submerged = invulnerable. Reading the surface tell is the entire fight.
        if (!isVulnerable) return;

        health -= Mathf.Abs(damage);
        BossHealthBar.UpdateHealth(health);
        if (health <= 0f) StartCoroutine(Die());
    }

    private IEnumerator Die()
    {
        isDying = true;
        isVulnerable = false;
        BossHealthBar.Hide();

        if (tideHazard != null) tideHazard.StopCycling();

        foreach (GameObject m in spawnedMinions)
            if (m != null) Destroy(m);
        spawnedMinions.Clear();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Fade out
        float fade = 1.5f, e = 0f;
        while (e < fade)
        {
            e += Time.deltaTime;
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 1f - Mathf.Clamp01(e / fade);
                spriteRenderer.color = c;
            }
            yield return null;
        }

        if (fullHealPickupPrefab != null)
        {
            Vector3 pos = rewardSpawnPoint != null ? rewardSpawnPoint.position : transform.position;
            Instantiate(fullHealPickupPrefab, pos, Quaternion.identity);
        }

        // Memory-fragment narration beat (uses existing DialogueBox singleton).
        // Once the inventory system from the proposal lands, this hook becomes the
        // place to grant the actual fragment.
        if (!string.IsNullOrEmpty(deathDialogue))
            DialogueBox.Show(deathDialogue, deathDialogueDuration);

        Destroy(gameObject);
    }
}
