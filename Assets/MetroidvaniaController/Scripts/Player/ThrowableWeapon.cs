using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowableWeapon : MonoBehaviour
{
	public Vector2 direction;
	public bool hasHit = false;
	public float speed = 10f;

    void Start()
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider != null)
        {
            foreach (GameObject cloud in GameObject.FindGameObjectsWithTag("Cloud"))
            {
                Collider2D cloudCollider = cloud.GetComponent<Collider2D>();
                if (cloudCollider != null)
                    Physics2D.IgnoreCollision(myCollider, cloudCollider);
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
		if ( !hasHit)
		GetComponent<Rigidbody2D>().linearVelocity = direction * speed;
	}

	void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.tag == "Enemy")
		{
			collision.gameObject.SendMessage("ApplyDamage", Mathf.Sign(direction.x) * 2f);
			Destroy(gameObject);
		}
		else if (collision.gameObject.tag != "Player" && collision.gameObject.tag != "Cloud")
		{
			Destroy(gameObject);
		}
	}
}
