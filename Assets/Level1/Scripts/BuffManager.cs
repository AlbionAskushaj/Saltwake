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
