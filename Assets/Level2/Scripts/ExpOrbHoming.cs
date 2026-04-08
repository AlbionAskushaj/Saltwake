using UnityEngine;

public class ExpOrbHoming : MonoBehaviour
{
    public float speed = 5f; // Speed at which the ExpOrb moves towards the mage
    private GameObject player; // The mage

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        if (player != null)
        {
            // Move towards the mage each frame at the given speed
            transform.position = Vector3.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.Heal(1f);

            Destroy(gameObject);
        }
    }
}
