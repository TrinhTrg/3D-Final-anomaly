using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AAH_ShiftEndUI — Controls the Shift End / Results panel buttons.
/// Attach to the ShiftEnd panel in the Hospital scene.
/// </summary>
public class AAH_ShiftEndUI : MonoBehaviour
{
    [Header("Scene Names")]
    public string menuSceneName = "menu";

    public void OnClickNextDay()
    {
        // Week 2: just reload or go back to menu since Day 2 is not implemented
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnClickMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(menuSceneName);
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }
}
