using UnityEngine;
using UnityEngine.SceneManagement;

public class menuScript : MonoBehaviour
{
    [Header("Main Menu UI")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _menuNoticePanel;
    [SerializeField] private TMPro.TMP_Text _menuNoticeTitle;
    [SerializeField] private TMPro.TMP_Text _menuNoticeDescription;
    [Header("LAN Menu UI")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _createRoomPanel;
    [SerializeField] private UnityEngine.UI.Button _multiplayerButton;
    [SerializeField] private UnityEngine.UI.Button _lobbyBackButton;
    [SerializeField] private UnityEngine.UI.Button _createRoomButton;
    [SerializeField] private UnityEngine.UI.Button _cancelCreateButton;
    [SerializeField] private UnityEngine.UI.Button _submitCreateButton;
    [SerializeField] private TMPro.TMP_InputField _roomNameInput;
    [SerializeField] private TMPro.TMP_Text _createRoomStatus;
    [SerializeField] private CanvasGroup _lobbyCanvasGroup;
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowMainMenu();
    }
    public void ShowLobbyUI()
    {
        if (_lobbyPanel == null) return;
        ShowMainMenu();
        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
        _lobbyPanel.SetActive(true);
    }
    public void ShowCreateRoomUI()
    {
        if (_createRoomPanel == null) return;
        if (_lobbyPanel == null || !_lobbyPanel.activeSelf) ShowLobbyUI();
        _createRoomPanel.SetActive(true);
        if (_lobbyCanvasGroup != null)
        {
            _lobbyCanvasGroup.interactable = false;
            _lobbyCanvasGroup.blocksRaycasts = false;
        }
        if (_createRoomStatus != null) _createRoomStatus.text = "";
        if (_roomNameInput != null)
        {
            _roomNameInput.Select();
            _roomNameInput.ActivateInputField();
        }
    }
    private void CloseCreateRoomUI()
    {
        if (_createRoomPanel != null) _createRoomPanel.SetActive(false);
        if (_lobbyCanvasGroup != null)
        {
            _lobbyCanvasGroup.interactable = true;
            _lobbyCanvasGroup.blocksRaycasts = true;
        }
        if (_createRoomButton != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_createRoomButton.gameObject);
    }
    private void SubmitCreateRoomUI()
    {
        if (_createRoomStatus == null) return;
        _createRoomStatus.text = _roomNameInput == null || string.IsNullOrWhiteSpace(_roomNameInput.text)
            ? "Vui lòng nhập tên phòng."
            : "Chức năng tạo phòng chưa được kết nối.";
    }
    public void ShowJoinLobbyUI(){ ShowMenuNotice("Join Lobby", "Chức năng tham gia Lobby chưa được kết nối."); }
    public void ShowSettingsUI(){ ShowMenuNotice("Cài đặt", "Màn hình cài đặt chưa được kết nối."); }
    private void ShowMenuNotice(string title, string description)
    {
        if (_menuNoticePanel == null) return;
        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
        if (_gameModePanel != null) _gameModePanel.SetActive(false);
        if (_characterPanel != null) _characterPanel.SetActive(false);
        if (_menuNoticeTitle != null) _menuNoticeTitle.text = title;
        if (_menuNoticeDescription != null) _menuNoticeDescription.text = description;
        _menuNoticePanel.SetActive(true);
    }
    [Header("Game Mode UI")]
    [SerializeField] private GameObject _gameModePanel;
    [Header("Character Selection UI")]
    [SerializeField] private GameObject _characterPanel;
    [System.Serializable]
    private class CharacterCard
    {
        public string characterId;
        public string nameKey;
        public string abilityKey;
        public string descriptionKey;
        public GameObject existingModel;
        public UnityEngine.UI.Button button;
        public UnityEngine.UI.Image frame;
        public TMPro.TMP_Text nameLabel;
        public GameObject selectedBadge;
        public GameObject viewedBadge;
        public bool unlocked;
        public TMPro.TMP_Text lockLabel;
        public string displayName;
        public string tier;
        [TextArea(2, 6)] public string bonuses;
        [TextArea(2, 6)] public string passive;
        public string unlockRequirement;
    }
    [SerializeField] private UnityEngine.UI.Button _soloButton;
    [SerializeField] private UnityEngine.UI.Button _backButton;
    [SerializeField] private UnityEngine.UI.Button _startButton;
    [SerializeField] private UnityEngine.UI.Button _chooseButton;
    [SerializeField] private TMPro.TMP_Text _detailsText;
    [SerializeField] private UnityEngine.UI.ScrollRect _detailsScroll;
    [SerializeField] private CharacterCard[] _characterCards = System.Array.Empty<CharacterCard>();
    [SerializeField] private Color _normalCardColor= new Color(0.18f, 0.21f, 0.24f, 1f);
    [SerializeField] private Color _selectedCardColor = new Color(0.72f, 0.23f, 0.22f, 1f);
    private int _selectedCharacter = -1;
    private int _viewedCharacter;
    private UnityEngine.InputSystem.InputAction _backAction;
    private UnityEngine.Events.UnityAction[] _cardCallbacks;
    public string SelectedCharacterId => _selectedCharacter >= 0 && _characterCards.Length > _selectedCharacter ? _characterCards[_selectedCharacter].characterId : "";
    public string ViewedCharacterId => _characterCards.Length > _viewedCharacter ? _characterCards[_viewedCharacter].characterId : "";
    private void Awake()
    {
        _cardCallbacks = new UnityEngine.Events.UnityAction[_characterCards.Length];
        for (int i = 0; i < _characterCards.Length; i++)
        {
            int index = i;
            _cardCallbacks[i] = () => SelectCharacter(index);
        }
        _backAction = new UnityEngine.InputSystem.InputAction("MenuBack", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/escape");
        _backAction.performed += OnBackPerformed;
        if (_startButton != null) _startButton.interactable = false;
    }
    private void OnEnable()
    {
        if (_soloButton != null) _soloButton.onClick.AddListener(ShowCharacterSelection);
        if (_backButton != null) _backButton.onClick.AddListener(ShowMainMenu);
        if (_chooseButton != null) _chooseButton.onClick.AddListener(ChooseMouse);
        if (_multiplayerButton != null) _multiplayerButton.onClick.AddListener(ShowLobbyUI);
        if (_lobbyBackButton != null) _lobbyBackButton.onClick.AddListener(ShowMainMenu);
        if (_cancelCreateButton != null) _cancelCreateButton.onClick.AddListener(CloseCreateRoomUI);
        if (_submitCreateButton != null) _submitCreateButton.onClick.AddListener(SubmitCreateRoomUI);
        if (_createRoomButton != null)
        {
            _createRoomButton.interactable = _createRoomPanel != null;
            _createRoomButton.onClick.AddListener(ShowCreateRoomUI);
        }
        for (int i = 0; i < _characterCards.Length; i++)
            if (_characterCards[i].button != null) _characterCards[i].button.onClick.AddListener(_cardCallbacks[i]);
        _backAction.Enable();
    }
    private void OnDisable()
    {
        if (_soloButton != null) _soloButton.onClick.RemoveListener(ShowCharacterSelection);
        if (_backButton != null) _backButton.onClick.RemoveListener(ShowMainMenu);
        if (_chooseButton != null) _chooseButton.onClick.RemoveListener(ChooseMouse);
        if (_multiplayerButton != null) _multiplayerButton.onClick.RemoveListener(ShowLobbyUI);
        if (_lobbyBackButton != null) _lobbyBackButton.onClick.RemoveListener(ShowMainMenu);
        if (_cancelCreateButton != null) _cancelCreateButton.onClick.RemoveListener(CloseCreateRoomUI);
        if (_submitCreateButton != null) _submitCreateButton.onClick.RemoveListener(SubmitCreateRoomUI);
        if (_createRoomButton != null) _createRoomButton.onClick.RemoveListener(ShowCreateRoomUI);
        for (int i = 0; i < _characterCards.Length; i++)
            if (_characterCards[i].button != null) _characterCards[i].button.onClick.RemoveListener(_cardCallbacks[i]);
        _backAction.Disable();
    }
    private void OnDestroy()
    {
        _backAction?.Dispose();
    }
    private void OnBackPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (_createRoomPanel != null && _createRoomPanel.activeSelf)
        {
            CloseCreateRoomUI();
            return;
        }
        if ((_characterPanel != null && _characterPanel.activeSelf) ||
            (_lobbyPanel != null && _lobbyPanel.activeSelf)) ShowMainMenu();
    }
    public void ShowCharacterSelection()
    {
        if (_characterPanel == null) return;
        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
        if (_gameModePanel != null) _gameModePanel.SetActive(false);
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
        if (_createRoomPanel != null) _createRoomPanel.SetActive(false);
        if (_menuNoticePanel != null) _menuNoticePanel.SetActive(false);
_characterPanel.SetActive(true);
        RefreshCharacterPreview();
    }
    private void SelectCharacter(int index)
    {
        if (index < 0 || index >= _characterCards.Length) return;
        _viewedCharacter = index;
        RefreshCharacterPreview();
    }
    private void ChooseMouse()
    {
        if (_characterCards.Length == 0 || !_characterCards[0].unlocked) return;
        _selectedCharacter = 0;
        RefreshCharacterPreview();
    }
    private void RefreshCharacterPreview()
    {
        if (_characterCards.Length == 0) return;
        for (int i = 0; i < _characterCards.Length; i++)
        {
            var card = _characterCards[i];
            if (card == null) continue;
            if (card.frame != null) card.frame.gameObject.SetActive(i == _viewedCharacter);
            if (card.selectedBadge != null) card.selectedBadge.SetActive(i == _selectedCharacter);
            if (card.viewedBadge != null) card.viewedBadge.SetActive(false);
        }
        if (_chooseButton != null) _chooseButton.gameObject.SetActive(_selectedCharacter != 0);
        var viewed = _characterCards[_viewedCharacter];
        if (_detailsText != null)
            _detailsText.text = "<b>" + viewed.displayName + " — " + viewed.tier + "</b>\n" + viewed.bonuses + "\n" + viewed.passive + "\nMở khóa: " + viewed.unlockRequirement;
        if (_detailsScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            _detailsScroll.StopMovement();
            _detailsScroll.verticalNormalizedPosition = 1f;
        }
    }
    public void PreviewSelectedCharacter()
    {
        RefreshCharacterPreview();
    }
    public void ShowGameModes()
    {
        if (_gameModePanel != null) _gameModePanel.SetActive(true);
    }
    public void ShowMainMenu()
    {
        if (_lobbyCanvasGroup != null)
        {
            _lobbyCanvasGroup.interactable = true;
            _lobbyCanvasGroup.blocksRaycasts = true;
        }
        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(true);
        if (_gameModePanel != null) _gameModePanel.SetActive(false);
        if (_characterPanel != null) _characterPanel.SetActive(false);
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
        if (_createRoomPanel != null) _createRoomPanel.SetActive(false);
        if (_menuNoticePanel != null) _menuNoticePanel.SetActive(false);
}
}
