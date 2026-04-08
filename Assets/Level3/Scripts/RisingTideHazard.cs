using UnityEngine;

// Damaging water zone for the Brinewyrm boss arena. Cycles up/hold/down on a timer.
// Player takes damage on a cooldown while their feet are inside the water surface.
[RequireComponent(typeof(BoxCollider2D))]
public class RisingTideHazard : MonoBehaviour
{
    [Header("Cycle (seconds)")]
    [SerializeField] private float riseTime = 8f;
    [SerializeField] private float holdTime = 4f;
    [SerializeField] private float fallTime = 4f;

    [Header("Heights (world Y)")]
    [SerializeField] private float lowY = -10f;
    [SerializeField] private float highY = -2f;

    [Header("Damage")]
    [SerializeField] private float damagePerTick = 1f;
    [SerializeField] private float tickInterval = 0.6f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer waterSprite;

    private float cycleTimer;
    private float damageTimer;
    private bool active;

    public bool IsActive => active;

    private void Awake()
    {
        SetSurfaceY(lowY);
    }

    public void BeginCycling()
    {
        active = true;
        cycleTimer = 0f;
    }

    public void StopCycling()
    {
        active = false;
        SetSurfaceY(lowY);
    }

    private void Update()
    {
        if (!active) return;

        cycleTimer += Time.deltaTime;
        float total = riseTime + holdTime + fallTime;
        float t = cycleTimer % total;

        float surface;
        if (t < riseTime)
            surface = Mathf.Lerp(lowY, highY, t / riseTime);
        else if (t < riseTime + holdTime)
            surface = highY;
        else
            surface = Mathf.Lerp(highY, lowY, (t - riseTime - holdTime) / fallTime);

        SetSurfaceY(surface);

        if (damageTimer > 0f) damageTimer -= Time.deltaTime;
    }

    private void SetSurfaceY(float surfaceY)
    {
        Vector3 pos = transform.position;
        pos.y = surfaceY;
        transform.position = pos;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!active || damageTimer > 0f) return;
        if (!other.CompareTag("Player")) return;
        CharacterController2D controller = other.GetComponent<CharacterController2D>();
        if (controller == null) return;
        controller.ApplyDamage(damagePerTick, transform.position);
        damageTimer = tickInterval;
    }
}
