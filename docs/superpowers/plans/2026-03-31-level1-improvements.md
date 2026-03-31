# Level 1 Improvements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add gated room progression, enemy drop rewards, a dialogue text box UI, and a 3-phase Giant Crow boss to Level 1.

**Architecture:** Linear room chain — each room locks on entry, tracks enemies, opens exit gate + spawns reward on clear. A BuffManager on the player handles temporary damage/speed buffs. The boss extends CrowEnemy's steering logic with phased behavior. A reusable DialogueBox handles styled typewriter text.

**Tech Stack:** Unity 2D, C#, TextMeshPro, Unity UI

---

### Task 1: Pickup System

**Files:**
- Create: `Assets/MetroidvaniaController/Scripts/Items/Pickup.cs`

This is the foundation — other systems spawn pickups, so build it first.

- [ ] **Step 1: Create Pickup.cs**

```csharp
using UnityEngine;

public enum PickupType
{
    Heart,
    DamageBuff,
    SpeedBuff,
    FullHeal
}

public class Pickup : MonoBehaviour
{
    [SerializeField] private PickupType pickupType = PickupType.Heart;
    [SerializeField] private float healAmount = 2f;
    [SerializeField] private float buffDuration = 15f;
    [SerializeField] private float buffMultiplier = 1.5f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.25f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        // Float/bob animation
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        switch (pickupType)
        {
            case PickupType.Heart:
                PlayerStats.Instance.Heal(healAmount);
                break;
            case PickupType.FullHeal:
                PlayerStats.Instance.Heal(PlayerStats.Instance.MaxHealth);
                break;
            case PickupType.DamageBuff:
                BuffManager buffMgr = other.GetComponent<BuffManager>();
                if (buffMgr != null) buffMgr.ApplyDamageBuff(buffMultiplier, buffDuration);
                break;
            case PickupType.SpeedBuff:
                BuffManager speedBuff = other.GetComponent<BuffManager>();
                if (speedBuff != null) speedBuff.ApplySpeedBuff(buffMultiplier, buffDuration);
                break;
        }

        Destroy(gameObject);
    }
}
```

- [ ] **Step 2: Create the Pickup prefab in Unity**

In the Unity Editor:
1. Create an empty GameObject named "Pickup_Heart"
2. Add a SpriteRenderer (assign a heart sprite or placeholder)
3. Add a CircleCollider2D, set `Is Trigger = true`, radius `0.5`
4. Add the `Pickup.cs` script, set `pickupType = Heart`
5. Save as prefab to `Assets/MetroidvaniaController/Prefabs/Items/Pickup_Heart.prefab`
6. Duplicate for `Pickup_DamageBuff.prefab` (set type to DamageBuff, assign different sprite/color)
7. Duplicate for `Pickup_SpeedBuff.prefab` (set type to SpeedBuff)
8. Duplicate for `Pickup_FullHeal.prefab` (set type to FullHeal)

- [ ] **Step 3: Commit**

```bash
git add Assets/MetroidvaniaController/Scripts/Items/Pickup.cs
git commit -m "feat: add Pickup system with heart, damage buff, speed buff, and full heal types"
```

---

### Task 2: Buff Manager

**Files:**
- Create: `Assets/MetroidvaniaController/Scripts/Player/BuffManager.cs`
- Modify: `Assets/MetroidvaniaController/Scripts/Player/PlayerMovement.cs:10` (expose runSpeed for buff)
- Modify: `Assets/MetroidvaniaController/Scripts/Player/Attack.cs:7` (expose dmgValue for buff)

- [ ] **Step 1: Create BuffManager.cs**

```csharp
using UnityEngine;
using System.Collections;

public class BuffManager : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private Attack attack;

    private float baseRunSpeed;
    private float baseDmgValue;

    private Coroutine activeSpeedBuff;
    private Coroutine activeDamageBuff;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        attack = GetComponent<Attack>();
    }

    private void Start()
    {
        baseRunSpeed = playerMovement.runSpeed;
        baseDmgValue = attack.dmgValue;
    }

    public void ApplyDamageBuff(float multiplier, float duration)
    {
        if (activeDamageBuff != null) StopCoroutine(activeDamageBuff);
        activeDamageBuff = StartCoroutine(DamageBuffRoutine(multiplier, duration));
    }

    public void ApplySpeedBuff(float multiplier, float duration)
    {
        if (activeSpeedBuff != null) StopCoroutine(activeSpeedBuff);
        activeSpeedBuff = StartCoroutine(SpeedBuffRoutine(multiplier, duration));
    }

    private IEnumerator DamageBuffRoutine(float multiplier, float duration)
    {
        attack.dmgValue = baseDmgValue * multiplier;
        yield return new WaitForSeconds(duration);
        attack.dmgValue = baseDmgValue;
        activeDamageBuff = null;
    }

    private IEnumerator SpeedBuffRoutine(float multiplier, float duration)
    {
        playerMovement.runSpeed = baseRunSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        playerMovement.runSpeed = baseRunSpeed;
        activeSpeedBuff = null;
    }
}
```

- [ ] **Step 2: Attach BuffManager to the player**

In Unity Editor, select the Player GameObject and add the `BuffManager` component. It auto-finds `PlayerMovement` and `Attack` via `GetComponent`.

- [ ] **Step 3: Commit**

```bash
git add Assets/MetroidvaniaController/Scripts/Player/BuffManager.cs
git commit -m "feat: add BuffManager for temporary damage and speed buffs"
```

---

### Task 3: Enemy Death Drops

**Files:**
- Modify: `Assets/MetroidvaniaController/Scripts/Enemies/Enemy.cs:4,98-108`
- Modify: `Assets/Level1/Scripts/CrowEnemy.cs:4,213-224`

Add a 20% chance to drop a heart pickup on death for both enemy types.

- [ ] **Step 1: Modify Enemy.cs — add drop fields and spawn logic**

Add these fields after line 18 (`public bool isInvincible = false;`):

```csharp
[Header("Drops")]
public GameObject heartPickupPrefab;
[Range(0f, 1f)] public float dropChance = 0.2f;
```

Modify the `DestroyEnemy()` coroutine (line 98) to spawn a pickup before destruction. Replace the entire coroutine:

```csharp
IEnumerator DestroyEnemy()
{
    CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
    capsule.size = new Vector2(1f, 0.25f);
    capsule.offset = new Vector2(0f, -0.8f);
    capsule.direction = CapsuleDirection2D.Horizontal;
    yield return new WaitForSeconds(0.25f);
    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

    if (heartPickupPrefab != null && Random.value <= dropChance)
    {
        Instantiate(heartPickupPrefab, transform.position, Quaternion.identity);
    }

    yield return new WaitForSeconds(3f);
    Destroy(gameObject);
}
```

- [ ] **Step 2: Modify CrowEnemy.cs — add drop fields and spawn logic**

Add these fields after line 22 (`[SerializeField] private float hoverAmount = 0.35f;`):

```csharp
[Header("Drops")]
[SerializeField] private GameObject heartPickupPrefab;
[SerializeField] [Range(0f, 1f)] private float dropChance = 0.2f;
```

Modify the `Die()` coroutine (line 213) to spawn a pickup. Replace the entire coroutine:

```csharp
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
```

- [ ] **Step 3: Assign prefabs in Unity**

In the Unity Editor:
1. Select the EnemySimple prefab (`Assets/MetroidvaniaController/Prefabs/Enemies/EnemySimple.prefab`)
2. Drag the `Pickup_Heart` prefab into the `Heart Pickup Prefab` field
3. Set `Drop Chance` to 0.2
4. Repeat for the Crow prefab (`Assets/Level1/Enemies/Crow.prefab`)

- [ ] **Step 4: Commit**

```bash
git add Assets/MetroidvaniaController/Scripts/Enemies/Enemy.cs Assets/Level1/Scripts/CrowEnemy.cs
git commit -m "feat: add 20% heart drop chance on enemy death"
```

---

### Task 4: Room Gate

**Files:**
- Create: `Assets/MetroidvaniaController/Scripts/Environment/RoomGate.cs`

- [ ] **Step 1: Create RoomGate.cs**

```csharp
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomGate : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D gateCollider;
    [SerializeField] private float fadeSpeed = 2f;

    private bool isOpen = false;
    private Color baseColor;

    private void Awake()
    {
        if (gateCollider == null)
            gateCollider = GetComponent<BoxCollider2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        gateCollider.isTrigger = false; // Solid barrier
        baseColor = spriteRenderer.color;
    }

    public void Open()
    {
        isOpen = true;
        gateCollider.enabled = false;
    }

    public void Close()
    {
        isOpen = false;
        gateCollider.enabled = true;
        spriteRenderer.color = baseColor;
    }

    private void Update()
    {
        if (isOpen && spriteRenderer.color.a > 0f)
        {
            Color c = spriteRenderer.color;
            c.a -= fadeSpeed * Time.deltaTime;
            if (c.a < 0f) c.a = 0f;
            spriteRenderer.color = c;
        }
    }
}
```

- [ ] **Step 2: Create Gate prefab in Unity**

1. Create a new GameObject named "RoomGate"
2. Add a SpriteRenderer (use a tileable ice/barrier sprite, or a solid colored rectangle as placeholder)
3. Add a BoxCollider2D sized to block the room exit (NOT a trigger — this is a solid wall)
4. Add `RoomGate.cs`
5. Save as prefab to `Assets/MetroidvaniaController/Prefabs/Environment/RoomGate.prefab`

- [ ] **Step 3: Commit**

```bash
git add Assets/MetroidvaniaController/Scripts/Environment/RoomGate.cs
git commit -m "feat: add RoomGate barrier with open/close and fade animation"
```

---

### Task 5: Room Manager

**Files:**
- Create: `Assets/MetroidvaniaController/Scripts/Environment/RoomManager.cs`

This is the core orchestrator — it tracks enemies, manages gates, and spawns rewards.

- [ ] **Step 1: Create RoomManager.cs**

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    [Header("Enemies")]
    [SerializeField] private List<GameObject> enemies = new List<GameObject>();

    [Header("Gates")]
    [SerializeField] private RoomGate entranceGate;
    [SerializeField] private RoomGate exitGate;

    [Header("Rewards")]
    [SerializeField] private GameObject clearRewardPrefab;
    [SerializeField] private Transform rewardSpawnPoint;

    [Header("Boss Room")]
    [SerializeField] private bool isBossRoom = false;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform bossSpawnPoint;

    private bool roomCleared = false;
    private bool playerInside = false;
    private GameObject spawnedBoss;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || playerInside) return;

        playerInside = true;

        if (roomCleared) return;

        // Seal entrance
        if (entranceGate != null)
            entranceGate.Close();

        // Seal exit
        if (exitGate != null)
            exitGate.Close();

        if (isBossRoom && bossPrefab != null && spawnedBoss == null)
        {
            Vector3 spawnPos = bossSpawnPoint != null ? bossSpawnPoint.position : transform.position;
            spawnedBoss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
            enemies.Add(spawnedBoss);
        }
    }

    private void Update()
    {
        if (roomCleared || !playerInside) return;

        // Remove destroyed enemies from list
        enemies.RemoveAll(e => e == null);

        if (enemies.Count == 0)
        {
            roomCleared = true;
            OnRoomCleared();
        }
    }

    private void OnRoomCleared()
    {
        // Open exit gate
        if (exitGate != null)
            exitGate.Open();

        // Open entrance gate (allow backtracking)
        if (entranceGate != null)
            entranceGate.Open();

        // Spawn reward
        if (clearRewardPrefab != null && rewardSpawnPoint != null)
        {
            Instantiate(clearRewardPrefab, rewardSpawnPoint.position, Quaternion.identity);
        }
    }

    // Called by CrowBoss when it spawns minion crows during Phase 2
    public void TrackEnemy(GameObject enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }
}
```

- [ ] **Step 2: Set up RoomManager on each room in Unity**

For each room (Room1 through Room4 and BossRoom):
1. Select the CameraRoom GameObject
2. Add `RoomManager` component (it shares the same BoxCollider2D trigger as CameraRoom)
3. Drag all enemies in that room into the `Enemies` list
4. Assign the entrance and exit `RoomGate` references
5. Create an empty child Transform for `Reward Spawn Point` near the exit gate
6. Assign the appropriate pickup prefab to `Clear Reward Prefab`:
   - Room 1: `Pickup_Heart`
   - Room 2: `Pickup_DamageBuff`
   - Room 3: `Pickup_Heart`
   - Room 4: `Pickup_SpeedBuff`
   - Boss Room: `Pickup_FullHeal`, set `Is Boss Room = true`, assign `Boss Prefab` and `Boss Spawn Point`

- [ ] **Step 3: Place RoomGate instances in the scene**

For each room transition:
1. Instantiate a `RoomGate` prefab at each room exit/entrance boundary
2. Size the BoxCollider2D to block the passage
3. Room 1: exit gate only (no entrance gate — it's the spawn room)
4. Rooms 2-4: both entrance and exit gates
5. Boss Room: entrance gate only (exit triggers level transition)

- [ ] **Step 4: Commit**

```bash
git add Assets/MetroidvaniaController/Scripts/Environment/RoomManager.cs
git commit -m "feat: add RoomManager to track enemies, manage gates, and spawn rewards"
```

---

### Task 6: Dialogue Box UI

**Files:**
- Create: `Assets/MetroidvaniaController/Scripts/UI/DialogueBox.cs`
- Modify: `Assets/narrative_trigger.cs:5-32`

- [ ] **Step 1: Create DialogueBox.cs**

```csharp
using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueBox : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Animation")]
    [SerializeField] private float fadeInTime = 0.3f;
    [SerializeField] private float fadeOutTime = 0.5f;
    [SerializeField] private float charsPerSecond = 30f;

    private Coroutine activeRoutine;

    private static DialogueBox instance;

    private void Awake()
    {
        instance = this;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public static void Show(string message, float holdDuration = 3f)
    {
        if (instance == null) return;
        instance.gameObject.SetActive(true);

        if (instance.activeRoutine != null)
            instance.StopCoroutine(instance.activeRoutine);

        instance.activeRoutine = instance.StartCoroutine(instance.ShowRoutine(message, holdDuration));
    }

    private IEnumerator ShowRoutine(string message, float holdDuration)
    {
        dialogueText.text = "";

        // Fade in panel
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInTime);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Typewriter effect
        for (int i = 0; i < message.Length; i++)
        {
            dialogueText.text = message.Substring(0, i + 1);
            yield return new WaitForSeconds(1f / charsPerSecond);
        }

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        activeRoutine = null;
    }
}
```

- [ ] **Step 2: Create DialogueBox prefab in Unity**

1. On the existing UI Canvas in Level1, create a child Panel named "DialogueBox"
2. Set anchors to bottom-center, position Y ~15% from bottom
3. Width: 60% of canvas width, Height: auto (use ContentSizeFitter vertical preferred)
4. Panel Image: black color, alpha 0.7 (178/255), rounded corners sprite if available
5. Add a subtle blue-white Outline component (color: `#B0D4F1`, thickness 2)
6. Add a child TextMeshProUGUI:
   - Font color: white
   - Alignment: center
   - Font size: 24
   - Padding: 16 on all sides
7. Add a `CanvasGroup` component on the Panel
8. Add `DialogueBox.cs`, assign the CanvasGroup and TMP text references
9. Save as prefab to `Assets/MetroidvaniaController/Prefabs/UI/DialogueBox.prefab`

- [ ] **Step 3: Update DialogueOnlyTrigger.cs to use DialogueBox**

Replace the entire contents of `Assets/narrative_trigger.cs`:

```csharp
using UnityEngine;

public class DialogueOnlyTrigger : MonoBehaviour
{
    [Header("Message")]
    [TextArea(2, 5)]
    [SerializeField] private string message = "Default dialogue message.";
    [SerializeField] private float displayTime = 3f;

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnlyOnce && hasTriggered) return;

        hasTriggered = true;
        DialogueBox.Show(message, displayTime);
    }
}
```

- [ ] **Step 4: Place dialogue triggers in the scene**

1. In Room 1, place a `DialogueOnlyTrigger` near the player spawn:
   - Message: `"The salt winds carry something wrong from the peak. Push forward."`
   - Display Time: 3
   - Trigger Only Once: true
2. At the entrance to the Boss Room, place another:
   - Message: `"The air splits. Something ancient circles above."`
   - Display Time: 3
   - Trigger Only Once: true

- [ ] **Step 5: Commit**

```bash
git add Assets/MetroidvaniaController/Scripts/UI/DialogueBox.cs Assets/narrative_trigger.cs
git commit -m "feat: add styled DialogueBox UI with typewriter effect, update DialogueOnlyTrigger"
```

---

### Task 7: Boss Health Bar UI

**Files:**
- Create: `Assets/MetroidvaniaController/Scripts/UI/BossHealthBar.cs`

Build this before the boss so the boss can reference it.

- [ ] **Step 1: Create BossHealthBar.cs**

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float fadeOutTime = 1f;

    private static BossHealthBar instance;

    private void Awake()
    {
        instance = this;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public static void Show(string bossName, float maxHealth)
    {
        if (instance == null) return;
        instance.gameObject.SetActive(true);
        instance.bossNameText.text = bossName;
        instance.healthSlider.maxValue = maxHealth;
        instance.healthSlider.value = maxHealth;
        instance.StartCoroutine(instance.FadeIn());
    }

    public static void UpdateHealth(float currentHealth)
    {
        if (instance == null) return;
        instance.healthSlider.value = Mathf.Max(0f, currentHealth);
    }

    public static void Hide()
    {
        if (instance == null) return;
        instance.StartCoroutine(instance.FadeOut());
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInTime);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutTime);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
```

- [ ] **Step 2: Create BossHealthBar prefab in Unity**

1. On the UI Canvas, create a child Panel named "BossHealthBar" at the top-center
2. Add a `CanvasGroup` component
3. Add a child `TextMeshProUGUI` for boss name ("The Stormcrow"), centered, white, size 20
4. Below it, add a Unity `Slider`:
   - Remove the Handle
   - Background: dark semi-transparent
   - Fill: red or orange gradient
   - Width: ~50% of screen, Height: 16px
5. Add `BossHealthBar.cs`, assign references
6. Save as prefab

- [ ] **Step 3: Commit**

```bash
git add Assets/MetroidvaniaController/Scripts/UI/BossHealthBar.cs
git commit -m "feat: add BossHealthBar UI with fade in/out"
```

---

### Task 8: Crow Boss AI

**Files:**
- Create: `Assets/Level1/Scripts/CrowBoss.cs`

The main boss script with 3 phases. Reuses steering concepts from CrowEnemy but is a standalone script (not inheritance — CrowEnemy uses private fields).

- [ ] **Step 1: Create CrowBoss.cs**

```csharp
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
    [SerializeField] private int nextLevelBuildIndex = -1;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // State
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

    // Phase thresholds
    private const float PHASE2_THRESHOLD = 0.7f; // 70% = 28 HP
    private const float PHASE3_THRESHOLD = 0.35f; // 35% = 14 HP

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

        roomManager = FindObjectOfType<RoomManager>();

        BossHealthBar.Show("The Stormcrow", maxHealth);
        StartCoroutine(BossLoop());
    }

    private IEnumerator BossLoop()
    {
        // Phase 1: Circling with dives
        float diveCooldown = 3f;
        float diveTimer = diveCooldown;
        float summonTimer = summonInterval;

        while (health > 0 && !isDying)
        {
            UpdatePhase();

            if (isDiving)
            {
                yield return null;
                continue;
            }

            // Circle around player
            if (player != null)
            {
                circleAngle += circleSpeed * Time.deltaTime;
                float currentSpeed = currentPhase == Phase.Frenzy ? circleSpeed * frenzySpeedMultiplier : circleSpeed;
                circleAngle += currentSpeed * Time.deltaTime;

                Vector2 circleTarget = (Vector2)player.position + new Vector2(
                    Mathf.Cos(circleAngle) * circleRadius,
                    Mathf.Sin(circleAngle) * circleRadius * 0.6f + 2f // Hover above
                );
                circleTarget += GetHoverOffset();

                Vector2 toTarget = circleTarget - rb.position;
                Vector2 desired = toTarget.normalized * (currentSpeed * 3f);
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desired, steeringStrength * Time.fixedDeltaTime);
                UpdateFacing(rb.linearVelocity.x);
            }

            // Phase-specific behavior
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
                        diveTimer = 2f; // Faster dives in phase 2
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
                    // FrenzyAttack handles its own timing, wait for it
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

        // Telegraph — flash sprite
        for (int i = 0; i < 5; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(diveTelegraphTime / 10f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(diveTelegraphTime / 10f);
        }

        // Dive toward player's current position
        Vector2 diveTarget = player.position;
        Vector2 diveDir = (diveTarget - rb.position).normalized;
        float currentDiveSpeed = currentPhase == Phase.Frenzy ? diveSpeed * frenzySpeedMultiplier : diveSpeed;
        rb.linearVelocity = diveDir * currentDiveSpeed;

        yield return new WaitForSeconds(0.5f);

        // Decelerate
        rb.linearVelocity = rb.linearVelocity * 0.3f;
        isDiving = false;
    }

    private IEnumerator FrenzyAttack()
    {
        if (isDiving || player == null) yield break;
        isDiving = true;

        for (int i = 0; i < frenzyDiveCount; i++)
        {
            // Telegraph
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = Color.white;

            // Dive
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
        // Clean up dead minions
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
        StopCoroutine(BossLoop());
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 1f; // Let it fall

        // Kill all minions
        foreach (GameObject minion in spawnedMinions)
        {
            if (minion != null) Destroy(minion);
        }
        spawnedMinions.Clear();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        BossHealthBar.Hide();

        // Fade out
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

        // Spawn full heal
        if (fullHealPickupPrefab != null)
        {
            Vector3 spawnPos = rewardSpawnPoint != null ? rewardSpawnPoint.position : transform.position;
            Instantiate(fullHealPickupPrefab, spawnPos, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
```

- [ ] **Step 2: Create the CrowBoss prefab in Unity**

1. Duplicate the existing Crow prefab or create a new GameObject named "CrowBoss"
2. Scale the sprite to ~3x (localScale 3,3,1)
3. Add a Rigidbody2D: gravity scale 0, freeze rotation Z
4. Add a CircleCollider2D or CapsuleCollider2D sized to the scaled sprite
5. Add `CrowBoss.cs`
6. Tag as "Enemy"
7. Assign references: Rigidbody2D, SpriteRenderer
8. Assign `crowMinionPrefab` = existing Crow prefab
9. Assign `fullHealPickupPrefab` = `Pickup_FullHeal` prefab
10. Set `nextLevelBuildIndex` to the Level2 build index (likely 1)
11. Save as prefab to `Assets/Level1/Enemies/CrowBoss.prefab`

- [ ] **Step 3: Commit**

```bash
git add Assets/Level1/Scripts/CrowBoss.cs
git commit -m "feat: add CrowBoss with 3-phase AI (circling, summoning, frenzy)"
```

---

### Task 9: Level Transition on Boss Death

**Files:**
- Modify: `Assets/MetroidvaniaController/Scripts/Environment/RoomManager.cs`

The boss room's RoomManager needs to load the next level when the player exits after the boss dies, rather than just opening a gate.

- [ ] **Step 1: Add level transition to RoomManager.cs**

Add a new field after the existing boss fields:

```csharp
[Header("Level Transition")]
[SerializeField] private bool loadNextLevelOnClear = false;
[SerializeField] private int nextLevelBuildIndex = -1;
```

Add a trigger exit method and modify `OnRoomCleared`:

```csharp
private void OnRoomCleared()
{
    // Open exit gate
    if (exitGate != null)
        exitGate.Open();

    // Open entrance gate (allow backtracking)
    if (entranceGate != null)
        entranceGate.Open();

    // Spawn reward
    if (clearRewardPrefab != null && rewardSpawnPoint != null)
    {
        Instantiate(clearRewardPrefab, rewardSpawnPoint.position, Quaternion.identity);
    }

    if (loadNextLevelOnClear && nextLevelBuildIndex >= 0)
    {
        StartCoroutine(LoadNextLevel());
    }
}

private IEnumerator LoadNextLevel()
{
    yield return new WaitForSeconds(3f); // Give player time to grab reward
    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(nextLevelBuildIndex);
}
```

Add `using UnityEngine.SceneManagement;` at the top of the file if not already present.

- [ ] **Step 2: Configure the boss room in Unity**

On the Boss Room's `RoomManager`:
- Set `Load Next Level On Clear = true`
- Set `Next Level Build Index` to the Level2 scene build index

- [ ] **Step 3: Commit**

```bash
git add Assets/MetroidvaniaController/Scripts/Environment/RoomManager.cs
git commit -m "feat: add level transition on boss room clear"
```

---

### Task 10: Scene Setup & Enemy Placement

**Files:**
- Modify: `Assets/Level1/Level1.unity` (Unity scene — manual editor work)

This is all Unity Editor work to wire everything together.

- [ ] **Step 1: Place gates in each room**

For each room, instantiate `RoomGate` prefabs at the entry/exit boundaries:
- Room 1: 1 exit gate (down-right passage)
- Room 2: 1 entrance gate + 1 exit gate
- Room 3: 1 entrance gate + 1 exit gate
- Room 4: 1 entrance gate + 1 exit gate
- Boss Room: 1 entrance gate (no exit — level transition handles it)

- [ ] **Step 2: Place and configure enemies**

Verify enemy counts match the design:
- Room 1: 2-3 patrol enemies (EnemySimple prefab)
- Room 2: 2 Crows + 1 patrol
- Room 3: 2 patrols + 2 Crows
- Room 4: 3 Crows + 1 patrol
- Boss Room: empty (boss spawns via RoomManager)

Assign `heartPickupPrefab` on all enemy instances.

- [ ] **Step 3: Wire up all RoomManager references**

For each room's `RoomManager`:
1. Drag all room enemies into the `Enemies` list
2. Assign entrance/exit gate references
3. Set the appropriate `Clear Reward Prefab`
4. Create and assign `Reward Spawn Point` transforms near exit gates

For Boss Room:
1. Set `Is Boss Room = true`
2. Assign `Boss Prefab` = CrowBoss prefab
3. Create and assign `Boss Spawn Point` transform
4. Set `Load Next Level On Clear = true`
5. Set `Next Level Build Index` appropriately

- [ ] **Step 4: Place dialogue triggers**

1. Room 1 spawn area: `DialogueOnlyTrigger` with intro message
2. Boss Room entrance: `DialogueOnlyTrigger` with pre-boss message

- [ ] **Step 5: Ensure DialogueBox is on the Canvas**

Add the `DialogueBox` prefab as a child of the UI Canvas in Level1.

- [ ] **Step 6: Ensure BossHealthBar is on the Canvas**

Add the `BossHealthBar` prefab as a child of the UI Canvas in Level1.

- [ ] **Step 7: Verify Level2 is in Build Settings**

Open File > Build Settings, ensure Level2.unity is listed after Level1.unity. Note its build index for the boss room's `nextLevelBuildIndex`.

- [ ] **Step 8: Commit**

```bash
git add -A Assets/Level1/
git commit -m "feat: wire up Level 1 rooms with gates, enemies, dialogue, and boss"
```

---

### Task 11: CLAUDE.md

**Files:**
- Already created: `CLAUDE.md` (at project root)

- [ ] **Step 1: Verify CLAUDE.md is up to date**

The CLAUDE.md was already created during the design phase. Verify it reflects the new systems (RoomManager, RoomGate, Pickup, BuffManager, DialogueBox, CrowBoss, BossHealthBar). If any new scripts were added that aren't documented, update the file.

- [ ] **Step 2: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: add CLAUDE.md with project overview and conventions"
```

---

### Task 12: Playtest & Polish

**Files:** None (Unity Editor testing)

- [ ] **Step 1: Test Room 1 flow**

Play from Room 1. Verify:
- Intro dialogue shows with typewriter effect and styled box
- Enemies spawn and patrol correctly
- Exit gate is solid (blocks passage)
- Killing all enemies opens the gate (fade animation)
- Heart pickup spawns at reward point
- Picking up heart heals the player

- [ ] **Step 2: Test Room 2-4 flow**

Progress through each room. Verify:
- Entrance gate seals behind player
- Exit gate blocks until room is cleared
- Correct reward type spawns (Room 2: damage buff, Room 3: heart, Room 4: speed buff)
- Buffs apply correctly (check damage numbers, movement speed) and expire after 15s

- [ ] **Step 3: Test boss fight**

Enter Boss Room. Verify:
- Pre-boss dialogue triggers
- Entrance gate seals
- Boss spawns and health bar appears
- Phase 1: circling + dive with telegraph
- Phase 2 (~28 HP): faster dives + crow minion spawns (max 4)
- Phase 3 (~14 HP): triple-dive frenzy + pause vulnerability window
- On death: minions die, boss fades, full heal spawns, level transition fires

- [ ] **Step 4: Test edge cases**

- Die in a room — does respawn work correctly with gates?
- Kill enemies with throwable weapon — do drops still work?
- Stack buffs — does picking up a second buff correctly reset the timer?
- Boss phase transitions — does it transition smoothly mid-dive?

- [ ] **Step 5: Final commit**

```bash
git add -A
git commit -m "feat: Level 1 complete — gated rooms, drops, dialogue, Stormcrow boss"
```
