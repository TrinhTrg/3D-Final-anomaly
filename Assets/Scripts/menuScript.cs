using UnityEngine;
using UnityEngine.SceneManagement;

public class menuScript : MonoBehaviour
{
    void Update()
    {
        if(Input.GetKey(KeyCode.P))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
