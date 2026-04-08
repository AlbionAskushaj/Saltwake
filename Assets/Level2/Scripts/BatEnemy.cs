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

    public GameObject Coin;
    public GameObject ExpOrb;
    public GameObject Key3;

    public float health;


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

            if (Coin != null)
                Instantiate(Coin, transform.position, Quaternion.identity);
            if (ExpOrb != null)
                Instantiate(ExpOrb, transform.position, Quaternion.identity);

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
