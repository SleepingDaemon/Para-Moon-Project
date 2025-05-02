using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugSceneLoading : MonoBehaviour
{
    void Start()
    {
        Debug.Log("==== SCENE LOADING DIAGNOSTICS ====");
        Debug.Log($"Current active scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Total scenes loaded: {SceneManager.sceneCount}");
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.Log($"Scene {i}: {scene.name}, Path: {scene.path}, Loaded: {scene.isLoaded}, Build Index: {scene.buildIndex}");
        }
        
        Debug.Log("==== SERVICE DIAGNOSTICS ====");
        // Add manual checks for each critical service
    }
}
