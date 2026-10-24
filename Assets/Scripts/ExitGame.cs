using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitGame : MonoBehaviour
{
    void Update()
    {
        if(Input.GetKey(KeyCode.Escape))
        {
            if(SceneManager.GetActiveScene().buildIndex == 2)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 2);
            } else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
            }
        }
    }
}
