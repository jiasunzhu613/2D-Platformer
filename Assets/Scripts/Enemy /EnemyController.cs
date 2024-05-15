using System;
using Player;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Unity.VisualScripting;

namespace Enemy {
	public class EnemyController : MonoBehaviour {
		/*
		need:
		- patrol points gameobject to get transform
			- sine wave?
		- Movement and death game state
		- death on contact on the top 
			- check normals and dot product

		- need method to enter movement state and method to update movement state
			- same goes for death state
		- need animator for enemy
		*/
		#region Movement
		[SerializeField] private float speed;
		[SerializeField] private float accel;
		[SerializeField] private float patrolDistance;
		private int desiredDirection = 1;
		#endregion

		#region Death
		[SerializeField] private float respawnTime;
		private float respawnTimer;
		#endregion

		#region Variables
		private EnemyState _state;
		private Collision2D[] contactPoints;
		[SerializeField] private Transform patrolPoint;
		private Vector2 respawnPoint;
		private float patrolPoint_x;
		private Rigidbody2D rb;
		private Collider2D collider_2d;
		private SpriteRenderer spriteRenderer;
		#endregion

		private void Awake(){
			_state = EnemyState.Movement; 
		}

		private void Start()
		{
			rb = GetComponent<Rigidbody2D>();
			collider_2d = GetComponent<Collider2D>();
			spriteRenderer = GetComponent<SpriteRenderer>();
			patrolPoint_x = patrolPoint.position.x;
			respawnPoint = patrolPoint.position;
		}

		private void Update(){
			// Debug.Log(respawnTimer);
			respawnTimer -= Time.deltaTime;
		}

		private void FixedUpdate(){
			switch (_state)
			{
				case EnemyState.Movement:
					UpdateMovementState();
					break;
				case EnemyState.Death:
					UpdateDeathState();
					break;
			}
		}

		#region State Methods
		private void EnterMovementState(){
			_state = EnemyState.Movement;
		}

		private void UpdateMovementState()
		{
			Vector2 changeInVelocity = Vector2.zero;
			
			float position = transform.position.x;
			desiredDirection =
				position > patrolPoint_x + patrolDistance ||
				position < patrolPoint_x - patrolDistance
					? -desiredDirection
					: desiredDirection;

			changeInVelocity.x = desiredDirection * speed - rb.velocity.x;
			
			// force movmeent until it is farther than patrol distance away from patrol cneter then make it turn around
			rb.AddForce(changeInVelocity, ForceMode2D.Impulse);
		}

		private void EnterDeathState()
		{
			_state = EnemyState.Death;
			// get rid of object and set its death timer to death time for it to start ticking
			collider_2d.enabled = false;
			spriteRenderer.enabled = false;
			Destroy(GetComponent<Rigidbody2D>());
			respawnTimer = respawnTime;
		}

		private void UpdateDeathState(){
			if (respawnTimer < 0)
			{
				Debug.Log(respawnPoint);
				collider_2d.enabled = true;
				spriteRenderer.enabled = true;
				this.AddComponent<Rigidbody2D>();
				rb = GetComponent<Rigidbody2D>();
				transform.position = respawnPoint; // need to change this to variable
				EnterMovementState();
			}
		}
		#endregion

		private float approach(float val, float target, float amount){
            return val > target ? Math.Max(val - amount, target) : Math.Min(val + amount, target);
        }

		private void OnCollisionEnter2D(Collision2D other)
		{
			foreach (ContactPoint2D i in other.contacts)
			{
				var player = other.gameObject.GetComponent<PlayerController>();
				Debug.Log(Vector2.Dot(i.normal.normalized, Vector2.down) > 0.5);
				if (Vector2.Dot(i.normal.normalized, Vector2.down) > 0.5) // look at value, if vectors are similarly cooincident, then kill enemy
				{
					EnterDeathState();
					// force a jump?
					player.Jump(player.FirstJumpForce);
				}
				else
				{
					player.EnterDeadState();
				}
			}
		}
	}

}