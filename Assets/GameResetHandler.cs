using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResetHandler : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ResetGame();
        }
    }

    public void ResetGame()
    {
        // Reload the current scene to reset the game state. 
        // This will effectively clear all runtime-spawned decals and reset the room to its initial state.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
