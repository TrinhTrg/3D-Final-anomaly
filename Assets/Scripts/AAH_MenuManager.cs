using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AAH_MenuManager — Manages the Main Menu scene for Anomaly Animal Hospital.
/// Replaces menuScript.cs behavior with themed AAH menu.
/// Preserves the existing P=Play, Escape=Quit pattern but adds themed UI.
/// </summary>
public class AAH_MenuManager : MonoBehaviour
{
    [Header("Menu UI")]
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtSubtitle;
    public TextMeshProUGUI txtStartHint;
    public TextMeshProUGUI txtVersion;

    [Header("Scene")]
    [Tooltip("Scene name or build index for the hospital prototype scene.")]
    public string hospitalSceneName = "HospitalDay1";

    [Header("Audio")]
    public AudioSource menuAmbience;
    public AudioSource clickSound;

    void Start()
    {
        // Unlock cursor on menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetupUI();

        if (menuAmbience != null)
            menuAmbience.Play();
    }

    void SetupUI()
    {
        if (txtTitle != null)
            txtTitle.text = "ANOMALY ANIMAL HOSPITAL";

        if (txtSubtitle != null)
            txtSubtitle.text = "SHIFT 1 — Day 1 Prototype";

        if (txtStartHint != null)
            txtStartHint.text = "Press  [P]  to Begin Shift\nPress  [Esc]  to Exit";

        if (txtVersion != null)
            txtVersion.text = "Week 2 Prototype — Unity 6";
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (clickSound != null) clickSound.Play();
            StartGame();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void StartGame()
    {
        // Try named scene first, fallback to build index
        if (!string.IsNullOrEmpty(hospitalSceneName))
        {
            try
            {
                SceneManager.LoadScene(hospitalSceneName);
                return;
            }
            catch { }
        }
        // Fallback: next scene in build order
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
