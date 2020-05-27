using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float fireRate = 12f;
    [ColorUsage(true, true)] public Color projectileColor = Color.red;
    public float projectileSpeed = 10f;
    public float projectileDamage;
    public GameObject projectilePrefab;

    [Header("Targeting")]
    public Transform target;
    public Transform eye;
    public Vector3 targetPositionOffset;
    public float rangeThreshold;

    [Header("Health")]
    public float maxHealth;
    public float currentHealth;
    public Slider healthBar;

    private float _fireTimer = 0f;
    private NavMeshAgent _agent;

    public bool CanDecreaseEnemyCount { get; set; }

    private void OnEnable()
    {
        _agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if(_agent && _agent.isActiveAndEnabled)
            _agent.SetDestination(target.position);

        if (healthBar)
            healthBar.value = currentHealth / maxHealth;

        if (!target) return;
        if (!_agent || !_agent.isActiveAndEnabled) return;

        bool inRange = Vector3.Distance(target.position, transform.position) <= rangeThreshold;

        if (eye && inRange)
        {
            eye.LookAt(target);
            //transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));

            if (Time.time >= _fireTimer)
            {
                _fireTimer = Time.time + 1f / fireRate;
                FireProjectile();
            }
        }
    }

    public void ApplyDamage(float damage)
    {
        if (currentHealth - damage <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
        currentHealth -= damage;
    }

    private void Die()
    {
        if (_agent)
        {
            _agent.enabled = false;
            if (CanDecreaseEnemyCount)
            {
                if (GameManager.instance)
                    GameManager.instance.ConfirmEnemyDeath(this);
            }
            Destroy(gameObject);
        }
    }

    private void FireProjectile()
    {
        Vector3 direction = (target.position + targetPositionOffset - eye.position).normalized;
        GameObject projectile = Instantiate(projectilePrefab, eye.position, Quaternion.LookRotation(direction));
        Projectile proj = projectile.GetComponentInChildren<Projectile>();
        if (proj) {
            proj.isEnemy = true;
            proj.damage = projectileDamage;
        }
        Rigidbody rb = projectile.GetComponentInChildren<Rigidbody>();
        if (rb)
            rb.velocity = direction * projectileSpeed;
        Renderer r = projectile.GetComponentInChildren<Renderer>();
        if (r)
            r.material.SetColor("_EmissionColor", projectileColor);
        Destroy(projectile, 5f);
    }

    public void AssignTarget(Transform target)
    {
        this.target = target;
        if(_agent)
            _agent.SetDestination(target.position);
    }
}