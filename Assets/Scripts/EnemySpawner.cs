using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum SpawnPattern
{
    Circle, Arc, Cross
}

[System.Serializable]
public class Wave
{
    public string waveName = "New Wave";
    public GameObject enemyPrefab;
    public int enemyCount = 5;
    public float spawnDelaySeconds = 0f;

    public SpawnPattern pattern = SpawnPattern.Circle;
    public float circleRadius = 8f;
    public float arcAngle = 120f;
    public float crossDistance = 4f;
}

public class EnemySpawner : MonoBehaviour
{
    public enum SpawnerState
    {
        Waiting, Spawning, Active, Finished
    }

    [Header("Target & References")]
    [SerializeField] private Transform playerXf;
    [SerializeField] private List<Wave> waves = new List<Wave>();

    [Header("Validation & Safe Margin")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float minSpawnRadius = 4f; // Enforces minimum distance from player
    [SerializeField] private float enemySpawnRadius = 0.5f;
    [SerializeField] private float maxGroundRayDistance = 15f;
    [SerializeField] private bool useNavMeshValidation = false;

    [Header("Wave Timing")]
    [SerializeField] private float minSecondsBetweenWaves = 3f;

    private int currentWaveIndex = 0;
    private SpawnerState state = SpawnerState.Waiting;
    private List<GameObject> enemies = new List<GameObject>();
    private float countdown;

    void Start()
    {
        countdown = minSecondsBetweenWaves;

        if (playerXf == null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                playerXf = player.transform;
            }
            else
            {
                Debug.LogWarning("EnemySpawner: Player not found in scene.");
            }
        }
    }

    void Update()
    {
        switch (state)
        {
            case SpawnerState.Active:
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    if (enemies[i] == null)
                    {
                        enemies.RemoveAt(i);
                    }
                }

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
        Debug.Log($"Wave {currentWaveIndex + 1}: {waves[currentWaveIndex].waveName} completed");
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
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        state = SpawnerState.Spawning;

        if (playerXf == null)
        {
            Debug.LogError("Unable to spawn wave: Player reference is missing.");
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
        Vector3 rawPos = CalculateRawSpawnPosition(wave, idx);
        Vector3 validPos = ValidateSpawnPosition(rawPos, idx, wave.enemyCount);

        Vector3 direction = (playerXf.position - validPos).normalized;
        direction.y = 0;

        GameObject enemyInst = Instantiate(wave.enemyPrefab, validPos, Quaternion.identity);

        if (direction != Vector3.zero)
        {
            enemyInst.transform.rotation = Quaternion.LookRotation(direction);
        }
        enemies.Add(enemyInst);
    }

    private Vector3 CalculateRawSpawnPosition(Wave wave, int idx)
    {
        Vector3 playerPos = playerXf.position;
        Vector3 offset = Vector3.zero;

        // Ensure circle radius is never smaller than minSpawnRadius
        float effectiveRadius = Mathf.Max(wave.circleRadius, minSpawnRadius);

        switch (wave.pattern)
        {
            case SpawnPattern.Circle:
                // Divide 360 degrees evenly among total enemies
                float angleStep = 360f / Mathf.Max(1, wave.enemyCount);
                float circleAngle = (idx * angleStep) * Mathf.Deg2Rad;
                offset = new Vector3(Mathf.Cos(circleAngle), 0f, Mathf.Sin(circleAngle)) * effectiveRadius;
                break;

            case SpawnPattern.Arc:
                Vector3 playerForward = playerXf.forward;
                float baseAngle = Mathf.Atan2(playerForward.z, playerForward.x) * Mathf.Rad2Deg;
                float startAngle = baseAngle - (wave.arcAngle / 2f);
                float stepArc = wave.enemyCount > 1 ? wave.arcAngle / (wave.enemyCount - 1) : 0f;
                float angleArc = (startAngle + idx * stepArc) * Mathf.Deg2Rad;
                offset = new Vector3(Mathf.Cos(angleArc), 0f, Mathf.Sin(angleArc)) * effectiveRadius;
                break;

            case SpawnPattern.Cross:
                int axisIndex = idx % 4;
                float multiplier = (idx / 4) + 1;
                float dist = Mathf.Max(minSpawnRadius, multiplier * wave.crossDistance);
                float rootHalf = 0.70710678f;

                if (axisIndex == 0) offset = new Vector3(rootHalf, 0f, rootHalf) * dist;
                else if (axisIndex == 1) offset = new Vector3(-rootHalf, 0f, rootHalf) * dist;
                else if (axisIndex == 2) offset = new Vector3(rootHalf, 0f, -rootHalf) * dist;
                else if (axisIndex == 3) offset = new Vector3(-rootHalf, 0f, -rootHalf) * dist;
                break;
        }

        return playerPos + offset;
    }

    private Vector3 ValidateSpawnPosition(Vector3 rawPos, int idx, int totalCount)
    {
        Vector3 playerPos = playerXf.position;

        // 1. Optional NavMesh Validation
        if (useNavMeshValidation && NavMesh.SamplePosition(rawPos, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
        {
            if (Vector3.Distance(playerPos, navHit.position) >= minSpawnRadius)
            {
                return navHit.position;
            }
        }

        // 2. Enforce Minimum Distance & Direction Vector
        Vector3 dirFromPlayer = rawPos - playerPos;
        dirFromPlayer.y = 0;
        float distFromPlayer = dirFromPlayer.magnitude;

        if (distFromPlayer < minSpawnRadius)
        {
            if (distFromPlayer < 0.01f)
            {
                // Fallback directional vector based on index angle if exactly at player position
                float fallbackAngle = (idx * 360f / Mathf.Max(1, totalCount)) * Mathf.Deg2Rad;
                dirFromPlayer = new Vector3(Mathf.Cos(fallbackAngle), 0f, Mathf.Sin(fallbackAngle));
            }
            else
            {
                dirFromPlayer.Normalize();
            }
            distFromPlayer = minSpawnRadius;
        }
        else
        {
            dirFromPlayer.Normalize();
        }

        Vector3 targetPos = playerPos + dirFromPlayer * distFromPlayer;

        // 3. Wall Collision Handling (Raycast Outward from Player)
        if (obstacleLayer.value != 0 && Physics.Raycast(playerPos, dirFromPlayer, out RaycastHit wallHit, distFromPlayer, obstacleLayer))
        {
            // Stop inside the room before hitting the wall face
            float safeWallDist = Mathf.Max(minSpawnRadius * 0.8f, wallHit.distance - (enemySpawnRadius + 0.3f));
            targetPos = playerPos + dirFromPlayer * safeWallDist;
        }

        // 4. Ground Snapping (Raycast Downward)
        Vector3 rayOrigin = targetPos + Vector3.up * 10f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, maxGroundRayDistance, groundLayer))
        {
            targetPos.y = groundHit.point.y;
        }
        else
        {
            targetPos.y = playerPos.y;
        }

        return targetPos;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerXf == null || waves == null || waves.Count == 0) return;

        Gizmos.color = Color.cyan;
        Wave currentWave = waves[Mathf.Clamp(currentWaveIndex, 0, waves.Count - 1)];

        for (int i = 0; i < currentWave.enemyCount; i++)
        {
            Vector3 raw = CalculateRawSpawnPosition(currentWave, i);
            Vector3 validated = Application.isPlaying ? ValidateSpawnPosition(raw, i, currentWave.enemyCount) : raw;
            Gizmos.DrawWireSphere(validated, enemySpawnRadius);
        }
    }
}