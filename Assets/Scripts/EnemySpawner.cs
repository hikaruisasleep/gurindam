using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpawnPattern
{
    Circle, Arc, Cross
}


[System.Serializable]
public class Wave
{
    public string waveName = "New Wave";
    public GameObject enemyPrefab;
    public int enemyCount;

    public float spawnDelaySeconds = 0f;

    public SpawnPattern pattern;
    public float circleRadius = 10f;
    public float arcAngle = 120f;
    public float crossDistance = 3f;

}

public class EnemySpawner : MonoBehaviour
{
    public enum SpawnerState
    {
        Waiting, Spawning, Active, Finished
    }

    [SerializeField]
    private Transform playerXf;
    [SerializeField]
    private List<Wave> waves = new List<Wave>();

    [SerializeField]
    private float minSecondsBetweenWaves = 3f;

    private int currentWaveIndex = 0;
    private SpawnerState state = SpawnerState.Waiting;
    private List<GameObject> enemies = new List<GameObject>();
    private float countdown;

    void Start()
    {
        countdown = minSecondsBetweenWaves;

        //check for player
        if (playerXf == null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                playerXf = player.transform;
            }
            else
            {
                Debug.LogWarning("Player not found in scene");
            }
        }
    }

    void Update()
    {
        switch (state)
        {
            case SpawnerState.Active:
                enemies.RemoveAll(enemy => enemy == null);
                if (enemies.Count == 0)
                {
                    OnWaveComplete();
                }
                return;

            case SpawnerState.Waiting:
                countdown -= Time.deltaTime;
                if (countdown <= 0f)
                {
                    if (currentWaveIndex < waves.Count)
                    {
                        StartCoroutine(SpawnWave(waves[currentWaveIndex]));
                    }
                    else
                    {
                        state = SpawnerState.Finished;
                    }
                }
                return;

            default:
                return;
        }
    }

    void OnWaveComplete()
    {
        Debug.Log($"wave {currentWaveIndex + 1}: {waves[currentWaveIndex].waveName} completed");
        currentWaveIndex++;

        if (currentWaveIndex < waves.Count)
        {
            state = SpawnerState.Waiting;
            countdown = minSecondsBetweenWaves;
        }
        else
        {
            state = SpawnerState.Finished;
        }

        return;
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        state = SpawnerState.Spawning;

        if (playerXf == null)
        {
            Debug.LogError("Unable to spawn wave: Player not found");
            state = SpawnerState.Waiting;
            yield break;
        }

        for (int i = 0; i < wave.enemyCount; i++)
        {
            SpawnEnemy(wave, i);
            if (wave.spawnDelaySeconds > 0f && i < wave.enemyCount - 1)
            {
                yield return new WaitForSeconds(wave.spawnDelaySeconds);
            }
        }

        state = SpawnerState.Active;
    }

    void SpawnEnemy(Wave wave, int idx)
    {
        Vector3 pos = CalculateSpawnPosition(wave, idx);
        Vector3 direction = (playerXf.position - pos).normalized;
        direction.y = 0;

        GameObject enemyInst = Instantiate(wave.enemyPrefab, pos, Quaternion.identity);

        if (direction != Vector3.zero)
        {
            enemyInst.transform.rotation = Quaternion.LookRotation(direction);
        }
        enemies.Add(enemyInst);
    }

    Vector3 CalculateSpawnPosition(Wave wave, int idx)
    {
        Vector3 playerPos = playerXf.position;
        Vector3 offset = Vector3.zero;

        switch (wave.pattern)
        {
            case SpawnPattern.Circle:
                float circleAngle = idx * 360f / wave.enemyCount * Mathf.Deg2Rad;
                offset = new Vector3(Mathf.Cos(circleAngle), 0f, Mathf.Sin(circleAngle)) * wave.circleRadius;
                break;
            case SpawnPattern.Arc:
                Vector3 playerForward = playerXf.forward;
                float baseAngle = Mathf.Atan2(playerForward.z, playerForward.x) * Mathf.Rad2Deg;
                float startAngle = baseAngle - (wave.arcAngle / 2f);
                float angleStep = wave.enemyCount > 1 ? wave.arcAngle / (wave.enemyCount - 1) : 0f;
                float angleArc = (startAngle + idx * angleStep) * Mathf.Deg2Rad;
                offset = new Vector3(Mathf.Cos(angleArc), 0f, Mathf.Sin(angleArc)) * wave.circleRadius;
                break;
            case SpawnPattern.Cross:
                int axisIndex = idx % 4;
                float multiplier = (idx / 4) + 1;
                float dist = multiplier * wave.crossDistance;
                float rootHalf = 0.70710678f; // 1/sqrt(2) for normalized diagonal vectors

                if (axisIndex == 0)      offset = new Vector3(rootHalf, 0f, rootHalf) * dist;   // NE
                else if (axisIndex == 1) offset = new Vector3(-rootHalf, 0f, rootHalf) * dist;  // NW
                else if (axisIndex == 2) offset = new Vector3(rootHalf, 0f, -rootHalf) * dist;  // SE
                else if (axisIndex == 3) offset = new Vector3(-rootHalf, 0f, -rootHalf) * dist; // SW
                break;
        }

        return new Vector3(playerPos.x + offset.x, playerPos.y, playerPos.z + offset.z);
    }
}
