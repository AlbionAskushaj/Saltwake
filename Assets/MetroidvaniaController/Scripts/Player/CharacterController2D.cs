using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;

public class CharacterController2D : MonoBehaviour
{
	[Header("Movement")]
	[Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .02f;

	[Header("Jumping")]
	[SerializeField] private float m_JumpSpeed = 14f;
	[SerializeField] private float m_FallMultiplier = 2.5f;
	[SerializeField] private float m_LowJumpMultiplier = 2f;
	[SerializeField] private float m_MaxFallSpeed = 20f;
	[SerializeField] private float m_CoyoteTime = 0.08f;
	[SerializeField] private float m_JumpBufferTime = 0.1f;

	[Header("Wall")]
	[SerializeField] private float m_WallSlideSpeed = 2f;
	[SerializeField] private float m_WallJumpHorizontalSpeed = 10f;
	[SerializeField] private float m_WallJumpVerticalSpeed = 14f;
	[SerializeField] private float m_WallJumpLockTime = 0.15f;
	[SerializeField] private float m_WallGrabCooldown = 0.25f;

	[Header("Dash")]
	[SerializeField] private float m_DashForce = 25f;

	[Header("Detection")]
	[SerializeField] private LayerMask m_WhatIsGround;
	[SerializeField] private Transform m_GroundCheck;
	[SerializeField] private Transform m_WallCheck;

	const float k_GroundedRadius = .2f;
	private bool m_Grounded;
	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true;
	private Vector3 velocity = Vector3.zero;

	private bool canDash = true;
	private bool isDashing = false;
	private bool m_IsWall = false;
	private bool isWallSliding = false;
	private bool canMove = true;

	private float coyoteTimeCounter;
	private float jumpBufferCounter;
	private float wallJumpLockCounter;
	private float wallGrabCooldownCounter;
	private float wallSlideCheckCooldown;

	public float maxLife = 10f;
	public float life = 10f;
	public bool invincible = false;

	private Animator animator;
	public ParticleSystem particleJumpUp;
	public ParticleSystem particleJumpDown;

	[Header("Events")]
	[Space]

	public UnityEvent OnFallEvent;
	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();
		m_Rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		animator = GetComponent<Animator>();

		if (OnFallEvent == null)
			OnFallEvent = new UnityEvent();
		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();
	}

	private void FixedUpdate()
	{
		bool wasGrounded = m_Grounded;
		m_Grounded = false;

		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;
				break;
			}
		}

		if (m_Grounded && !wasGrounded)
		{
			OnLandEvent.Invoke();
			if (!m_IsWall && !isDashing)
				particleJumpDown.Play();
			wallJumpLockCounter = 0;
		}

		// Coyote time
		if (m_Grounded)
			coyoteTimeCounter = m_CoyoteTime;
		else
			coyoteTimeCounter -= Time.fixedDeltaTime;

		// Wall detection (only when airborne)
		m_IsWall = false;
		if (!m_Grounded)
		{
			OnFallEvent.Invoke();
			Collider2D[] collidersWall = Physics2D.OverlapCircleAll(m_WallCheck.position, k_GroundedRadius, m_WhatIsGround);
			for (int i = 0; i < collidersWall.Length; i++)
			{
				if (collidersWall[i].gameObject != null)
				{
					m_IsWall = true;
					break;
				}
			}
			if (m_IsWall)
				isDashing = false;
		}

		// Tick timers
		if (jumpBufferCounter > 0) jumpBufferCounter -= Time.fixedDeltaTime;
		if (wallJumpLockCounter > 0) wallJumpLockCounter -= Time.fixedDeltaTime;
		if (wallGrabCooldownCounter > 0) wallGrabCooldownCounter -= Time.fixedDeltaTime;
		if (wallSlideCheckCooldown > 0) wallSlideCheckCooldown -= Time.fixedDeltaTime;
	}

	public void Move(float move, bool jump, bool jumpHeld, bool dash)
	{
		if (!canMove) return;

		// Buffer jump input
		if (jump)
			jumpBufferCounter = m_JumpBufferTime;

		// --- DASH ---
		if (dash && canDash)
		{
			if (isWallSliding)
				StopWallSlide();
			StartCoroutine(DashCooldown());
		}

		if (isDashing)
		{
			m_Rigidbody2D.linearVelocity = new Vector2(transform.localScale.x * m_DashForce, 0);
			return;
		}

		// --- WALL SLIDE ENTER ---
		if (m_IsWall && !m_Grounded && !isWallSliding && wallGrabCooldownCounter <= 0
			&& m_Rigidbody2D.linearVelocity.y <= 0)
		{
			StartWallSlide();
		}

		// --- WALL SLIDE EXIT ---
		if (isWallSliding)
		{
			if (m_Grounded)
			{
				StopWallSlide();
			}
			else if (wallSlideCheckCooldown <= 0 && !m_IsWall)
			{
				StopWallSlide();
			}
			else if (move * transform.localScale.x > 0.1f)
			{
				StopWallSlide();
			}
		}

		// --- WALL JUMP ---
		if (isWallSliding && jumpBufferCounter > 0)
		{
			jumpBufferCounter = 0;
			coyoteTimeCounter = 0;
			wallGrabCooldownCounter = m_WallGrabCooldown;
			wallJumpLockCounter = m_WallJumpLockTime;

			float awayDir = transform.localScale.x;
			StopWallSlide();

			m_Rigidbody2D.linearVelocity = new Vector2(
				awayDir * m_WallJumpHorizontalSpeed,
				m_WallJumpVerticalSpeed
			);

			animator.SetBool("IsJumping", true);
			animator.SetBool("JumpUp", true);
			particleJumpUp.Play();
			return;
		}

		// --- GROUND / COYOTE JUMP ---
		if (!isWallSliding && jumpBufferCounter > 0 && coyoteTimeCounter > 0)
		{
			jumpBufferCounter = 0;
			coyoteTimeCounter = 0;
			m_Grounded = false;

			m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, m_JumpSpeed);

			animator.SetBool("IsJumping", true);
			animator.SetBool("JumpUp", true);
			particleJumpDown.Play();
			particleJumpUp.Play();
		}

		// --- AIR JUMP (infinite mid-air) — works when not on valid ground/coyote (e.g. Level 2 clouds off mask)
		else if (!isWallSliding && jumpBufferCounter > 0 && !m_Grounded && coyoteTimeCounter <= 0f)
		{
			jumpBufferCounter = 0;
			m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, m_JumpSpeed);

			animator.SetBool("IsJumping", true);
			animator.SetBool("JumpUp", true);
			particleJumpDown.Play();
			particleJumpUp.Play();
		}

		// --- WALL SLIDE VELOCITY ---
		if (isWallSliding)
		{
			m_Rigidbody2D.linearVelocity = new Vector2(0, -m_WallSlideSpeed);
		}

		// --- HORIZONTAL MOVEMENT (full air control, locked briefly after wall jump) ---
		if (!isWallSliding && wallJumpLockCounter <= 0)
		{
			Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.linearVelocity.y);
			m_Rigidbody2D.linearVelocity = Vector3.SmoothDamp(
				m_Rigidbody2D.linearVelocity, targetVelocity, ref velocity, m_MovementSmoothing);
		}

		// --- FLIP ---
		if (!isWallSliding && wallJumpLockCounter <= 0)
		{
			if (move > 0 && !m_FacingRight)
				Flip();
			else if (move < 0 && m_FacingRight)
				Flip();
		}

		// --- GRAVITY MODIFIERS (variable jump height + snappy descent) ---
		if (!isDashing && !isWallSliding)
		{
			if (m_Rigidbody2D.linearVelocity.y < 0)
			{
				// Falling — extra gravity for fast, weighty descent
				m_Rigidbody2D.linearVelocity += Vector2.up * Physics2D.gravity.y * (m_FallMultiplier - 1) * Time.fixedDeltaTime;
			}
			else if (m_Rigidbody2D.linearVelocity.y > 0 && !jumpHeld)
			{
				// Rising but jump released — cut jump short for variable height
				m_Rigidbody2D.linearVelocity += Vector2.up * Physics2D.gravity.y * (m_LowJumpMultiplier - 1) * Time.fixedDeltaTime;
			}
		}

		// --- CAP FALL SPEED ---
		if (m_Rigidbody2D.linearVelocity.y < -m_MaxFallSpeed)
			m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, -m_MaxFallSpeed);
	}

	private void StartWallSlide()
	{
		isWallSliding = true;
		wallSlideCheckCooldown = 0.1f;
		wallJumpLockCounter = 0;
		m_WallCheck.localPosition = new Vector3(-m_WallCheck.localPosition.x, m_WallCheck.localPosition.y, 0);
		Flip();
		animator.SetBool("IsWallSliding", true);
	}

	private void StopWallSlide()
	{
		if (!isWallSliding) return;
		isWallSliding = false;
		animator.SetBool("IsWallSliding", false);
		m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
	}

	private void Flip()
	{
		m_FacingRight = !m_FacingRight;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}

	public void ApplyDamage(float damage, Vector3 position, bool applyKnockback = true)
	{
		if (!invincible)
		{
			if (isWallSliding)
				StopWallSlide();

			animator.SetBool("Hit", true);
			life -= damage;
			if (PlayerStats.Instance != null)
				PlayerStats.Instance.TakeDamage(damage);
			if (applyKnockback)
			{
				Vector2 damageDir = Vector3.Normalize(transform.position - position) * 40f;
				m_Rigidbody2D.linearVelocity = Vector2.zero;
				m_Rigidbody2D.AddForce(damageDir * 10);
			}
			if (life <= 0)
			{
				StartCoroutine(WaitToDead());
			}
			else
			{
				StartCoroutine(Stun(0.25f));
				StartCoroutine(MakeInvincible(1f));
			}
		}
	}

	IEnumerator DashCooldown()
	{
		animator.SetBool("IsDashing", true);
		isDashing = true;
		canDash = false;
		yield return new WaitForSeconds(0.1f);
		isDashing = false;
		yield return new WaitForSeconds(0.5f);
		canDash = true;
	}

	IEnumerator Stun(float time)
	{
		canMove = false;
		yield return new WaitForSeconds(time);
		canMove = true;
	}

	IEnumerator MakeInvincible(float time)
	{
		invincible = true;
		yield return new WaitForSeconds(time);
		invincible = false;
	}

	IEnumerator WaitToDead()
	{
		if (isWallSliding)
			StopWallSlide();
		animator.SetBool("IsDead", true);
		canMove = false;
		invincible = true;
		GetComponent<Attack>().enabled = false;
		yield return new WaitForSeconds(0.4f);
		m_Rigidbody2D.linearVelocity = new Vector2(0, m_Rigidbody2D.linearVelocity.y);
		yield return new WaitForSeconds(1.1f);

		PlayerRespawn respawn = GetComponent<PlayerRespawn>();
		if (respawn != null && respawn.GetRespawnPoint() != null)
		{
			if (WaveManager.instance != null)
			{
				Level2Progress.SaveWaveAndReloadScene(WaveManager.instance.currentWaveNumber);
				yield break;
			}

			respawn.Respawn();
			animator.SetBool("IsDead", false);
			canMove = true;
			GetComponent<Attack>().enabled = true;
			life = (int)maxLife;
			if (PlayerStats.Instance != null)
				PlayerStats.Instance.Heal(PlayerStats.Instance.MaxTotalHealth);
			StartCoroutine(MakeInvincible(1f));
		}
		else
		{
			SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
		}
	}
}
