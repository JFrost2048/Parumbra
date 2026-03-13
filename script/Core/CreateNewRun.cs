using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public void StartNewGame()
    {
        if (GameRunManager.Instance == null)
        {
            Debug.LogError("[MainMenuController] RunManager missing");
            return;
        }

        GameRunManager.Instance.CreateNewRun();
    }
}