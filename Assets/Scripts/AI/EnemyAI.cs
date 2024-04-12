using NaughtyAttributes;

using System;

using UnityEngine;

using UnityEngine.AI;

using Random = UnityEngine.Random;

public enum EnemyStates
{
	Patrolling,
	Chasing,
	Attacking
}

public class EnemyAI : MonoBehaviour
{
	public float Health
	{
		get => health;
		set => health = value;
	}

	[Header("Enemy Settings")]
	[SerializeField] private float health;
	[SerializeField] private LayerMask whatIsGround;
	[SerializeField] private LayerMask whatIsPlayer;
	
	// Patrolling
	[SerializeField] private Vector3 walkPoint;
	[SerializeField] private float walkPointRange;
	
	// Attacking
	[SerializeField] private float timeBetweenAttacks;
	[SerializeField] private float angleOfProjectile;
	[SerializeField] private float forceOfProjectile;
	[SerializeField] private GameObject projectile;
	
	// States
	[SerializeField] private float sightRange, attackRange;
	[SerializeField] private bool playerInSightRange, playerInAttackRange;

	[Header("Debugging")]
	[SerializeField, ReadOnly] private EnemyStates currentState;
	[SerializeField, ReadOnly] private NavMeshAgent agent;
	[SerializeField, ReadOnly] private Transform player;

	private bool walkPointSet;
	private bool alreadyAttacked;
	
	private void Awake()
	{
		player = FindObjectOfType<PlayerController>().transform;
		agent = GetComponent<NavMeshAgent>();

		gameObject.layer = 9;
	}

	private void Start()
	{
		GameManager.Instance.enemies.Add(this);
	}

	private void Update()
	{
		// Check for sight and attack range
		playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
		playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);
		
		if(!playerInSightRange && !playerInAttackRange)
			Patrolling();
		
		if(playerInSightRange && !playerInAttackRange)
			ChasePlayer();
		
		if(playerInAttackRange && playerInSightRange)
			AttackPlayer();
	}

	private void Patrolling()
	{
		currentState = EnemyStates.Patrolling;
		
		if(!walkPointSet) SearchWalkPoint();

		if(walkPointSet)
			agent.SetDestination(walkPoint);

		Vector3 distanceToWalkPoint = transform.position - walkPoint;
		
		// Walk point reached
		if(distanceToWalkPoint.magnitude < 1f)
			walkPointSet = false;
	}

	private void SearchWalkPoint()
	{
		// Calculate random point in range
		float randomZ = Random.Range(-walkPointRange, walkPointRange);
		float randomX = Random.Range(-walkPointRange, walkPointRange);

		walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

		if(Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
			walkPointSet = true;
	}

	private void ChasePlayer()
	{
		currentState = EnemyStates.Chasing;
		
		agent.SetDestination(player.position);
	}

	// ReSharper disable Unity.PerformanceAnalysis
	private void AttackPlayer()
	{
		currentState = EnemyStates.Attacking;
		
		// Make sure enemy doesn't move
		agent.SetDestination(transform.position);
		
		transform.LookAt(player);

		if(!alreadyAttacked)
		{
			// Attack code here
			Rigidbody rb = Instantiate(projectile, transform.position, Quaternion.identity).GetComponent<Rigidbody>();
			
			rb.AddForce(transform.forward * forceOfProjectile, ForceMode.Impulse);
			rb.AddForce(transform.up * angleOfProjectile, ForceMode.Impulse);

			alreadyAttacked = true;
			Invoke(nameof(ResetAttack), timeBetweenAttacks);
		}
	}

	private void ResetAttack() => alreadyAttacked = false;

	public void TakeDamage(int _damage)
	{
		health -= _damage;
		
		if(health <= 0)
			Invoke(nameof(DestroyEnemy), 0.5f);
	}

	private void DestroyEnemy() => Destroy(gameObject);

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, attackRange);
		
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, sightRange);
	}
}