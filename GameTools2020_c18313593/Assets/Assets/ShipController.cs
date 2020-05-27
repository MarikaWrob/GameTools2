using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShipController : MonoBehaviour
{
    [Header("References")]
    public Camera shipCamera;
    public Slider boostBar;

    [Header("Ship Settings")]
    public float speedBase;
    public float boostMutliplier;
    public float maxBoost;
    public float acceleration = 2f;
    public float decceleration = 1.5f;
    public float currentBoost;
    public float boostDecrease = 1f;
    public Transform[] shipCannons;
    public float rotationSpeed = 10f;
    public float rollSpeed = 2f;
    public KeyCode boostKey = KeyCode.Space;
    public float boostRegenTime = 2.5f;
    public float boostRegenRate = 10f;
    public float maxRoll = 45f;
    public float fireRate;

    [Header("Projectile Settings")]
    [ColorUsage(true, true)] public Color projectileColor = Color.red;
    public float projectileSpeed;
    public float projectileDamage;
    public GameObject projectilePrefab;
    public KeyCode fireKey = KeyCode.LeftControl;
    public bool useMouseForFire;

    [Header("Health")]
    public float maxHealth;
    public float currentHealth;
    public Slider healthBar;
    public Gradient healthStateGradient;
    public Image healthBarFill;
    public float healthChangeRate = 10f;
    public float healthRegenTime = 4f;
    public float healthRegenRate;

    [Header("Minimap")]
    public Camera minimapCamera;
    public float minimapCameraHeight;
    public Transform shipMinimapMarker;

    [Header("Engine Visuals")]
    public float minIntensity = 1.1f;
    public float maxIntensity = 4.5f;
    public int engineMaterialIndex;

    private Rigidbody _rigidbody;
    private Vector3 _spawnLocation;
    private bool _boosting;
    private float _currentSpeed;
    private float _speedTarget;
    private CameraController _cameraController;
    private float _roll;
    private bool _desu;
    private float _healthTarget;
    private bool _regeneratingBoost;
    private bool _regeneratingHealth;
    private Coroutine _boostRegenCoroutine;
    private Coroutine _healthRegenCoroutine;
    private Renderer _renderer;
    private Color _initialEngineColor;

    private float _cannonFireTimer = 0f;
    private int _currentCannonIndex;

    private void OnEnable()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer && engineMaterialIndex < _renderer.materials.Length)
            _initialEngineColor = _renderer.materials[engineMaterialIndex].GetColor("_EmissionColor");
        _rigidbody = GetComponent<Rigidbody>();
        _cameraController = shipCamera.GetComponent<CameraController>();
        _spawnLocation = transform.position;
        currentHealth = maxHealth;
        _healthTarget = currentHealth;
        currentBoost = maxBoost;

        if(healthBar)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = 1f;
            healthBar.value = 1f;
        }

        if (boostBar) {
            boostBar.minValue = 0f;
            boostBar.maxValue = maxBoost;
            boostBar.value = boostBar.maxValue;
        }
    }

    void Update()
    {
        HandleResources();
        if (_desu) return;
        HandleMovement();
        HandleVisuals();
        HandleCannons();
    }

    private void HandleCannons() {
        bool firing = Input.GetKey(fireKey) || (useMouseForFire && Input.GetMouseButton(0));

        if (firing && Time.time >= _cannonFireTimer) {
            _cannonFireTimer = Time.time + 1f / fireRate;
            FireProjectile();
            _currentCannonIndex = _currentCannonIndex + 1 < shipCannons.Length ? _currentCannonIndex + 1 : 0;
        }
    }

    private void FireProjectile() {
        GameObject projectile = Instantiate(projectilePrefab, shipCannons[_currentCannonIndex].position, Quaternion.LookRotation(transform.forward.normalized));
        Rigidbody rb = projectile.GetComponentInChildren<Rigidbody>();
        Projectile proj = projectile.GetComponentInChildren<Projectile>();
        if (proj)
        {
            proj.isEnemy = false;
            proj.damage = projectileDamage;
        }
        if (rb)
            rb.velocity = transform.forward.normalized * projectileSpeed;
        Renderer r = projectile.GetComponentInChildren<Renderer>();
        if (r)
            r.material.SetColor("_EmissionColor", projectileColor);
        Destroy(projectile, 5f);
    }

    private void HandleVisuals() {
        if (_renderer) {
            if (engineMaterialIndex >= _renderer.materials.Length) return;        
            Color engineColor = _initialEngineColor * GameManager.MapRange(_currentSpeed, 0f, speedBase * boostMutliplier, minIntensity, maxIntensity);
            _renderer.materials[engineMaterialIndex].SetColor("_EmissionColor", engineColor);
        }
    }

    private void HandleResources() {
        if (healthBar) {
            currentHealth = Mathf.MoveTowards(currentHealth, _healthTarget, healthChangeRate * Time.deltaTime);
            healthBar.value = currentHealth / maxHealth;
            if (healthBarFill)
                healthBarFill.color = healthStateGradient.Evaluate(healthBar.value);
        }

        if (_boosting)
        {
            _regeneratingBoost = false;
            if (_boostRegenCoroutine != null)
            {
                StopCoroutine(_boostRegenCoroutine);
                _boostRegenCoroutine = null;
            }
            currentBoost -= boostDecrease * Time.deltaTime;
        }
        else {
            if (_boostRegenCoroutine == null)
                _boostRegenCoroutine = StartCoroutine(RegenerateBoost());
        }

        if (_regeneratingBoost)
            currentBoost += boostRegenRate * Time.deltaTime;

        if (_regeneratingHealth)
            _healthTarget += healthRegenRate * Time.deltaTime;

        _healthTarget = Mathf.Clamp(_healthTarget, 0f, maxHealth);
        currentBoost = Mathf.Clamp(currentBoost, 0f, maxBoost);

        if (boostBar)
            boostBar.value = currentBoost;
    }

    private IEnumerator RegenerateBoost() {
        yield return new WaitForSeconds(boostRegenTime);
        _regeneratingBoost = true;
    }

    private IEnumerator RegenerateHealth()
    {
        yield return new WaitForSeconds(healthRegenTime);
        _regeneratingHealth = true;
    }

    public void ApplyDamage(float damage)
    {
        _regeneratingHealth = false;
        if (_healthRegenCoroutine != null)
        {
            StopCoroutine(_healthRegenCoroutine);
            _healthRegenCoroutine = null;
        }
        if (_healthTarget - damage <= 0f)
        {
            _healthTarget = 0f;
            Die();
        }
        if (_healthRegenCoroutine == null)
            _healthRegenCoroutine = StartCoroutine(RegenerateHealth());
        _healthTarget -= damage;
    }

    private void Die() {
        _desu = true;
        if (_healthRegenCoroutine != null)
        {
            StopCoroutine(_healthRegenCoroutine);
            _healthRegenCoroutine = null;
        }
        if (_rigidbody) {
            _rigidbody.mass = 100f;
            _rigidbody.useGravity = true;
        }
        if (GameManager.instance)
            GameManager.instance.EndGame(true);
    }

    private void HandleMovement()
    {
        _boosting = Input.GetKey(boostKey) && currentBoost > 0f;

        _speedTarget = _boosting ? speedBase * boostMutliplier : speedBase;

        if (_currentSpeed < _speedTarget)
        {
            _currentSpeed += acceleration * Time.deltaTime;
        }
        else if (_currentSpeed > _speedTarget)
            _currentSpeed -= decceleration * Time.deltaTime;
        else
            _currentSpeed = _speedTarget;

        transform.position += shipCamera.transform.forward.normalized * _currentSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(shipCamera.transform.forward.normalized), rotationSpeed * Time.deltaTime);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, _roll);

        if (!_cameraController) return;
        _roll = Mathf.Lerp(_roll, -_cameraController.Axes.x * maxRoll, Time.deltaTime * rollSpeed);
        _roll = Mathf.Clamp(_roll, -maxRoll, maxRoll);

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, _roll);
    }

    private void LateUpdate()
    {
        if (minimapCamera)
            minimapCamera.transform.position = transform.position + Vector3.up * minimapCameraHeight;
        if (shipMinimapMarker)
        {
            shipMinimapMarker.position = transform.position + Vector3.up * (minimapCameraHeight / 2f);
            shipMinimapMarker.eulerAngles = new Vector3(shipMinimapMarker.eulerAngles.x, transform.eulerAngles.y, shipMinimapMarker.eulerAngles.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EnemyProjectile")) {
            Projectile proj = other.GetComponentInChildren<Projectile>();

            if(proj.isEnemy)
                ApplyDamage(proj.damage);
            Destroy(other.gameObject);
        }
    }
}