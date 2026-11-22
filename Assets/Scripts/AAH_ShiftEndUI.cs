using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AAH_ShiftEndUI : MonoBehaviour
{
    public const string Day2Scene = "Hospital_day2";

    [Header("Scene Names")]
    public string menuSceneName = "menu";

    [Tooltip("Nút Về menu chính trên báo cáo. Có thể để trống khi dùng bảng báo cáo mặc định.")]
    [SerializeField] private Button _reportButton;
    private bool _day1ReportReady;
    private bool _loading;

    public void PrepareDay1Report()
    {
        if (_day1ReportReady) return;
        if (_reportButton == null) _reportButton = FindReportButton();
        if (_reportButton == null)
        {
            Debug.LogError("Hãy gán nút Về menu chính vào Report Button trên panel báo cáo.", this);
            return;
        }

        var label = _reportButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "Ngày tiếp theo";
        else
        {
            var legacyLabel = _reportButton.GetComponentInChildren<Text>(true);
            if (legacyLabel != null) legacyLabel.text = "Ngày tiếp theo";
        }
        // Replace the old serialized navigation on this button for the current scene instance.
        _reportButton.onClick = new Button.ButtonClickedEvent();
        _reportButton.onClick.AddListener(LoadDay2);
        _reportButton.interactable = true;
        _day1ReportReady = true;
    }

    private Button FindReportButton()
    {
        Button only = null;
        int visibleCount = 0;
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (!button.gameObject.activeInHierarchy) continue;
            visibleCount++;
            only = button;
            if (button.name == "BTN_MainMenu") return button;
            var tmp = button.GetComponentInChildren<TMP_Text>(true);
            var legacy = button.GetComponentInChildren<Text>(true);
            string caption = (tmp != null ? tmp.text : legacy != null ? legacy.text : "").Trim().ToUpperInvariant();
            if (caption == "VỀ MENU CHÍNH" || caption == "VỀ TRANG CHỦ" || caption == "MAIN MENU")
                return button;
        }
        return visibleCount == 1 ? only : null;
    }

    private void LoadDay2()
    {
        if (!_day1ReportReady || _loading) return;
        if (!Application.CanStreamedLevelBeLoaded(Day2Scene))
        {
            Debug.LogError("Hãy thêm và bật Hospital_day2 trong Scene List của Build Profile đang dùng.", this);
            return;
        }
        _loading = true;
        _reportButton.interactable = false;
        try
        {
            if (SceneManager.LoadSceneAsync(Day2Scene, LoadSceneMode.Single) == null)
                throw new System.InvalidOperationException("Không thể mở Hospital_day2.");
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        catch (System.Exception error)
        {
            _loading = false;
            _reportButton.interactable = true;
            Debug.LogException(error, this);
        }
    }

    // Existing scenes may still use this callback for their retry button.
    public void OnClickNextDay()
    {
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
