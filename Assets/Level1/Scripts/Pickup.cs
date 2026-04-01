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
