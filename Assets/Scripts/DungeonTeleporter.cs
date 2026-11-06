using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTeleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private string targetSceneName = "Dungeon";
    [SerializeField] private string playerTag = "Player";

    private bool isTransitioning = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTransitioning) return;

        // Verify if the object touching the trigger is the Player
        if (other.CompareTag(playerTag) || other.GetComponentInParent<Player>() != null)
        {
            isTransitioning = true;
            Debug.Log($"Teleporting to {targetSceneName}...");
            SceneManager.LoadScene(targetSceneName);
        }
    }
}