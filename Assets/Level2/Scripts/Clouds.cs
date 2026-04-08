using UnityEngine;
using System.Collections;


public class Cloud : MonoBehaviour
{
    private GameObject player; // Mage is now found dynamically
    public float minSpeed = 1.5f;
    public float maxSpeed = 2.0f;
    private float speed;

    public static float cumulativeSpeedIncrease = 0f;


    public float resetPositionX = -10.0f;
    public float startPositionX = 10.0f;

    private float cloudWidth;
    private BoxCollider2D cloudCollider;

    public Sprite normalCloudSprite;
    public Sprite flickerCloudSprite;


    private SpriteRenderer spriteRenderer;

    public float flickerDuration = 1f; // Total time the cloud flickers at the start of a new wave
    public float flickerInterval = 0.2f; // Interval between sprite switches

    private Coroutine flickerCoroutine = null;

    void Start()
    {
        cloudWidth = GetComponent<SpriteRenderer>().bounds.size.x;
        speed = Random.Range(minSpeed, maxSpeed) + cumulativeSpeedIncrease; // Apply cumulative speed increase here
        cloudCollider = GetComponent<BoxCollider2D>();
        player = GameObject.FindGameObjectWithTag("Player");
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {

        // Standard movement logic
        transform.position += Vector3.left * speed * Time.deltaTime;
        if (transform.position.x < resetPositionX - (cloudWidth / 2))
        {
            transform.position = new Vector3(startPositionX + (cloudWidth / 2), transform.position.y, transform.position.z);
            speed = Random.Range(minSpeed, maxSpeed) + cumulativeSpeedIncrease; // Reapply speed increase after reset
        }

        // Adjust collider based on mage's position, if mage is found
        if (player != null)
        {
            AdjustColliderBasedOnPlayerPosition();
        }
    }

    public void IncreaseSpeed(float speedIncrease)
    {
        // Assuming 'speed' is a variable representing the cloud's speed.
        speed += speedIncrease;
    }





    void AdjustColliderBasedOnPlayerPosition()
    {
        // Enable the collider if the mage is above the cloud, disable otherwise
        cloudCollider.enabled = player.transform.position.y > transform.position.y;
    }

    public void StartFlickering()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine); // Stop flickering if it's already happening
        }
        flickerCoroutine = StartCoroutine(FlickerEffect());
    }

    private IEnumerator FlickerEffect()
    {
        float endTime = Time.time + flickerDuration;
        while (Time.time < endTime)
        {
            // Switch to the flicker sprite
            spriteRenderer.sprite = flickerCloudSprite;
            yield return new WaitForSeconds(flickerInterval);

            // Switch back to the normal sprite
            spriteRenderer.sprite = normalCloudSprite;
            yield return new WaitForSeconds(flickerInterval);
        }

        // Ensure sprite is set back to normal after flickering
        spriteRenderer.sprite = normalCloudSprite;
    }

}
