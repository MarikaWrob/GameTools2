using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public const float kBaseHealth = 100f;

    [Header("Important References")]
    public ShipController playerShip;
    public GameObject alientPrefab;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI alientCountText;
    public TextMeshProUGUI statusText;

    [Header("Spawn Settings")]
    public float spawnRadius;
    public int spawnLimit;
    public float waveMultiplier;
    public int enemyCountMultiplier;
    public float spawnDelay = 1f;
    public int alienSpawnLimit = 20;
    public float waveStartDelay = 4f;
    public int waveLimit = 3;

    public static int CurrentAlienCount { get; set; }
    public static int CurrentWave { get; set; } = 1;

    private bool _spawning;
    private Coroutine _spawnRoutine;
    private int _enemiesToSpawn;
    private Machi machi;
    private bool _gameOver;

    private void Update()
    {
        if (alientCountText)
            alientCountText.text = string.Format("{0} Alien(s) Remaining", CurrentAlienCount);

        if (_gameOver && Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(gameObject.scene.buildIndex);
    }

    private void OnEnable()
    {
        instance = this;
        machi = GetComponent<Machi>();
        CurrentAlienCount = 0;
        CurrentWave = 1;
        _enemiesToSpawn = 0;
        _gameOver = false;
        _spawnRoutine = null;
        if(waveText)
            waveText.text = string.Format("WAVE {0}", CurrentWave);
        if (statusText)
        {
            statusText.text = "";
            statusText.gameObject.SetActive(false);
        }
        Invoke("StartWaveLogic", 4.5f);
    }

    private void StartWaveLogic() => QueueEnemySpawnsForWave(CurrentWave);

    public void QueueEnemySpawnsForWave(int wave = -1)
    {
        if (wave == -1) wave = CurrentWave;

        int enemiesToSpawn = (wave * enemyCountMultiplier / 2);
        _spawning = true;
        if (_spawnRoutine == null)
            _spawnRoutine = StartCoroutine(SpawnEnemies());
        Debug.Log("QUEUED " + enemiesToSpawn + " ENEMIES");
        _enemiesToSpawn = enemiesToSpawn;
    }

    private IEnumerator SpawnEnemies()
    {
        while (_spawning || _enemiesToSpawn > 0)
        {
            if (_enemiesToSpawn > 0 && CurrentAlienCount < alienSpawnLimit)
                SpawnEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void SpawnEnemy()
    {
        _enemiesToSpawn--;
        Vector3 spawnPosition = GetRandomPointOnNavMesh();
        GameObject o = Instantiate(alientPrefab, spawnPosition, Quaternion.identity);
        Enemy e = o.GetComponent<Enemy>();
        if (e)
        {
            e.AssignTarget(playerShip.transform);
            e.CanDecreaseEnemyCount = true;
            e.maxHealth = kBaseHealth + (kBaseHealth * waveMultiplier * (CurrentWave - 1));
            e.fireRate += CurrentWave * 0.5f;
            e.currentHealth = e.maxHealth;
            CurrentAlienCount++;
        }
    }

    public void ConfirmEnemyDeath(Enemy enemy)
    {
        if (!enemy) return;
        CurrentAlienCount--;
        if (CurrentAlienCount <= 0)
        {
            CurrentAlienCount = 0;
            _spawning = false;
            _enemiesToSpawn = 0;
            _spawnRoutine = null;
            StartCoroutine(DelayWaveStart());
        }
    }

    private IEnumerator DelayWaveStart() {
        yield return new WaitForSeconds(waveStartDelay);
        CurrentWave++;
        if (waveText && CurrentWave <= waveLimit)
        {
            waveText.text = string.Format("WAVE {0}", CurrentWave);
            QueueEnemySpawnsForWave();
        }
        else if (waveText && CurrentWave > waveLimit)
        {
            waveText.text = "YOU WIN! YOU DEFEATED ALL ALIENS!";
            EndGame(false);
        }
    }

    private readonly int _recursionLimit = 25;
    private int _currentRecursion = 0;

    private Vector3 GetRandomPointOnNavMesh() {
        Vector3 pos = new Vector3(machi.gridWidth / 2f, 0f, machi.gridHeight / 2f) + Random.insideUnitSphere * spawnRadius;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, spawnRadius, 1))
        {
            _currentRecursion = 0;
            return hit.position;
        }
        else
        {
            if (_currentRecursion < _recursionLimit)
                return GetRandomPointOnNavMesh();
            else {
                _currentRecursion = 0;
                return Vector3.zero;
            }
                
        }
    }

    public void EndGame(bool loss) {
        if (statusText)
        {
            statusText.text = loss ? "<color=red><b>You Lose!</b></color><br>Press R to Restart!" : "<color=green><b>You Win!</b></color><br>Press R to Restart!";
            statusText.gameObject.SetActive(true);
        }
        _gameOver = true;
    }

    public static float MapRange(float v, float s, float st, float s2, float st2)
    {
        return s2 + (st2 - s2) * ((v - s) / (st - s));
    }
}
