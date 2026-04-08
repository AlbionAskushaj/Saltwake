using UnityEngine;

public class BebbleMovement : MonoBehaviour
{
    public float speed = 5f;

    private GameObject player;
    public float homingStrength = 2f;

    private float timeSinceSpawned = 0f;
    private bool isHoming = true;

    private Vector3 initialDirection;
    private Vector3 lastHomingDirection;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        initialDirection = Vector3.left;
    }

    void Update()
    {
        timeSinceSpawned += Time.deltaTime;

        if (isHoming && timeSinceSpawned > 2f)
        {
            isHoming = false;
            if (player != null)
                lastHomingDirection = (player.transform.position - transform.position).normalized;
        }

        if (WaveManager.instance.CurrentWaveNumber >= 3 && player != null)
        {
            if (isHoming)
            {
                Vector2 direction = (player.transform.position - transform.position).normalized;
                transform.position += (Vector3)direction * homingStrength * Time.deltaTime;
            }
            else
            {
                transform.position += lastHomingDirection * speed * Time.deltaTime;
            }
        }
        else
        {
            transform.position += initialDirection * speed * Time.deltaTime;
        }

        if (IsOffScreen())
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController2D controller = other.GetComponent<CharacterController2D>();
            if (controller != null)
            {
                controller.ApplyDamage(3f, transform.position);
            }
            Destroy(gameObject);
        }
    }

    bool IsOffScreen()
    {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
        return screenPoint.x < 0;
    }
}
