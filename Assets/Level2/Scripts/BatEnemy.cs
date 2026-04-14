using UnityEngine;
using System.Collections;

public class BatEnemy : MonoBehaviour
{
    public GameObject bebblePrefab; // Assign this in the Inspector
    public float entrySpeed = 20.0f;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float batWidth;
    private bool startMoving = false;
    public float minShootInterval = 1.0f; // Minimum interval between shots for regular bats
    public float maxShootInterval = 3.0f; // Maximum interval between shots for regular bats
    public float bossMinShootInterval = 0.5f; // Minimum interval between shots for the boss
    public float bossMaxShootInterval = 1.5f; // Maximum interval between shots for the boss
    public bool isBoss = false; // Flag to identify if this instance is the boss
    public delegate void BatDeathDelegate();
    public event BatDeathDelegate OnDeath;

    public GameObject Key3;

    public float health;

    private float maxHealth;
    private LineRenderer healthLine;
    private SpriteRenderer spriteRenderer;

    public float shakeDuration = 5.0f; // Duration of the shake effect
    public float shakeMagnitude = 0.5f; // Magnitude of the shake
    private bool isShaking = false; // If the boss is currently shaking



    void Start()
    {
        startPosition = transform.position;
        batWidth = GetComponent<SpriteRenderer>().bounds.size.x;

        float screenRightEdge = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

        // Adjust the multiplier for 'batWidth' or add/subtract a fixed value to fine-tune the final position
        if (isBoss)
        {
            // For the boss, adjust this value so it stops earlier than regular bats
            endPosition = new Vector3(screenRightEdge - (batWidth * 0.5f), startPosition.y, startPosition.z);
        }
        else
        {
            // Original calculation for regular bats
            endPosition = new Vector3(screenRightEdge - batWidth * 2, startPosition.y, startPosition.z);
        }

        StartCoroutine(EnterAfterDelay(1));


        StartCoroutine(ShootBebblesRandomly());

        AdjustHealthBasedOnWave();

        maxHealth = Mathf.Max(health, 0.01f);
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetupHealthLine();

    }

    void AdjustHealthBasedOnWave()
    {
        // Access WaveManager's current wave number
        int currentWave = WaveManager.instance.CurrentWaveNumber;

        // Increase health for bats in wave 3 and 4
        if (currentWave == 3 || currentWave == 4)
        {
            health *= 3f; // Example: Increase health by 50%
        }
        // Optionally, set different health for the boss
        if (isBoss)
        {
            health *= 10f; // Example: Boss has double health
        }
    }


    void Update()
    {
        if (startMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, endPosition, entrySpeed * Time.deltaTime);

            if (transform.position == endPosition && isBoss)
            {
                startMoving = false; // Ensure this entity stops moving.
                if (!isShaking)
                {
                    StartCoroutine(StartShaking()); // Start shaking only if it's not already shaking
                }
            }
        }

        // Add a shaking effect for the boss
        if (isShaking)
        {
            transform.position = startPosition + Random.insideUnitSphere * shakeMagnitude;
        }
    }

    void SetupHealthLine()
    {
        GameObject lineGo = new GameObject("HealthLine");
        lineGo.transform.SetParent(transform, false);

        healthLine = lineGo.AddComponent<LineRenderer>();
        healthLine.useWorldSpace = true;
        healthLine.positionCount = 2;
        healthLine.numCapVertices = 0;
        healthLine.numCornerVertices = 0;
        healthLine.textureMode = LineTextureMode.Stretch;
        healthLine.alignment = LineAlignment.TransformZ;
        healthLine.startWidth = healthLine.endWidth = isBoss ? 0.11f : 0.07f;

        if (spriteRenderer != null)
        {
            healthLine.sortingLayerID = spriteRenderer.sortingLayerID;
            healthLine.sortingOrder = spriteRenderer.sortingOrder + 15;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader != null)
            healthLine.material = new Material(shader);

        RefreshHealthLine();
    }

    void LateUpdate()
    {
        RefreshHealthLine();
    }

    void RefreshHealthLine()
    {
        if (healthLine == null || maxHealth <= 0f)
            return;

        SpriteRenderer sr = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        float ratio = Mathf.Clamp01(health / maxHealth);

        float halfTrack;
        float topY;
        float centerX;
        float z;

        if (sr != null)
        {
            Bounds b = sr.bounds;
            halfTrack = Mathf.Max(b.extents.x * 0.9f, 0.12f);
            topY = b.max.y + 0.08f;
            centerX = b.center.x;
            z = b.center.z;
        }
        else
        {
            halfTrack = Mathf.Max(batWidth * 0.45f, 0.12f);
            topY = transform.position.y + 0.5f;
            centerX = transform.position.x;
            z = transform.position.z;
        }

        Vector3 left = new Vector3(centerX - halfTrack, topY, z);
        Vector3 right = new Vector3(centerX - halfTrack + 2f * halfTrack * ratio, topY, z);
        healthLine.SetPosition(0, left);
        healthLine.SetPosition(1, right);

        Color c = Color.Lerp(new Color(0.92f, 0.22f, 0.2f), new Color(0.35f, 0.88f, 0.4f), ratio);
        healthLine.startColor = healthLine.endColor = c;
    }

    public void SetEndPosition(Vector3 newPosition)
    {
        endPosition = newPosition;
    }

    IEnumerator StartShaking()
    {
        Vector3 originalPosition = transform.position;
        isShaking = true;

        // Loop indefinitely until isShaking is false
        while (isShaking)
        {
            float shakeX = originalPosition.x + Random.Range(-shakeMagnitude, shakeMagnitude);
            float shakeY = originalPosition.y + Random.Range(-shakeMagnitude, shakeMagnitude);
            transform.position = new Vector3(shakeX, shakeY, originalPosition.z);

            // Yielding null waits for the next frame
            yield return null;
        }

        // Optionally reset the position to the original after stopping the shake
        transform.position = originalPosition;
    }





    IEnumerator EnterAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        startMoving = true;
    }

    IEnumerator ShootBebblesRandomly()
    {
        // Wait until the bat starts moving
        yield return new WaitForSeconds(4);

        while (true)
        {
            // Use shorter intervals if it's the boss
            float interval = isBoss ? Random.Range(bossMinShootInterval, bossMaxShootInterval)
                                    : Random.Range(minShootInterval, maxShootInterval);
            yield return new WaitForSeconds(interval);

            // Check if the bat has been destroyed and stop the coroutine if it has
            if (this == null || gameObject == null) yield break;

            if (!isBoss && WaveManager.instance != null)
                WaveManager.instance.NotifyProjectileSpawned();

            // Instantiate the bebble
            Instantiate(bebblePrefab, transform.position, Quaternion.identity);
        }
    }

    // Called by the new project's Attack.cs via SendMessage("ApplyDamage", dmgValue)
    public void ApplyDamage(float damage)
    {
        float damageAmount = Mathf.Abs(damage);
        health -= damageAmount;

        if (health <= 0)
        {
            isShaking = false;

            if (isBoss && Key3 != null)
            {
                Instantiate(Key3, transform.position, Quaternion.identity);
            }

            StopCoroutine("StartShaking");

            Destroy(gameObject);
        }
    }

    // Contact damage to the player using the new project's health system
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && health > 0)
        {
            CharacterController2D controller = collision.gameObject.GetComponent<CharacterController2D>();
            if (controller != null)
            {
                controller.ApplyDamage(2f, transform.position);
            }
        }
    }


    private void OnDestroy()
    {
        if (OnDeath != null)
        {
            OnDeath.Invoke();
        }
    }


}
