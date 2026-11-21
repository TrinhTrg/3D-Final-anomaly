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
    [Header("Waiting Room UI")]
    [SerializeField] private GameObject _waitingRoomPanel;
    [SerializeField] private UnityEngine.UI.Toggle _roomPrivateToggle;
    [SerializeField] private TMPro.TMP_Dropdown _roomDifficultyDropdown;
    [SerializeField] private TMPro.TMP_Text _waitingRoomName;
    [SerializeField] private TMPro.TMP_Text _waitingPrivacy;
    [SerializeField] private TMPro.TMP_Text _waitingDifficulty;
    [SerializeField] private UnityEngine.UI.Button _waitingChooseButton;
    [SerializeField] private UnityEngine.UI.Button _leaveRoomButton;
    [SerializeField] private UnityEngine.UI.Image _waitingPortrait;
    [SerializeField] private GameObject _waitingPlaceholder;
    [SerializeField] private TMPro.TMP_Text _waitingCharacterName;
    [SerializeField] private TMPro.TMP_Text _waitingChooseLabel;
    [SerializeField] private Sprite _waitingMousePortrait;
    private int _waitingSelectedCharacter = -1;
    private int _pendingWaitingCharacter = -1;
    private bool _selectingForWaitingRoom;
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
        if (_roomNameInput == null || string.IsNullOrWhiteSpace(_roomNameInput.text))
        {
            if (_createRoomStatus != null) _createRoomStatus.text = "Vui lòng nhập tên phòng.";
            return;
        }
        if (_waitingRoomPanel == null) return;
        if (_waitingRoomName != null) _waitingRoomName.text = _roomNameInput.text.Trim();
        if (_waitingPrivacy != null) _waitingPrivacy.text = _roomPrivateToggle != null && _roomPrivateToggle.isOn ? "Riêng tư" : "Công khai";
        if (_waitingDifficulty != null && _roomDifficultyDropdown != null)
            _waitingDifficulty.text = _roomDifficultyDropdown.options[_roomDifficultyDropdown.value].text;
        _waitingSelectedCharacter = -1;
        CloseCreateRoomUI();
        ShowWaitingRoomUI();
    }
    private void ShowWaitingRoomUI()
    {
        if (_waitingRoomPanel == null) return;
        ShowMainMenu();
        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
        _waitingRoomPanel.SetActive(true);
        bool hasCharacter = _waitingSelectedCharacter == 0;
        if (_waitingPortrait != null)
        {
            _waitingPortrait.sprite = hasCharacter ? _waitingMousePortrait : null;
            _waitingPortrait.gameObject.SetActive(hasCharacter);
        }
        if (_waitingPlaceholder != null) _waitingPlaceholder.SetActive(!hasCharacter);
        if (_waitingCharacterName != null) _waitingCharacterName.text = hasCharacter ? "Y tá Chuột" : "Chưa chọn nhân vật";
        if (_waitingChooseLabel != null) _waitingChooseLabel.text = hasCharacter ? "Đổi nhân vật" : "Chọn nhân vật";
    }
    private void ShowWaitingCharacterSelection()
    {
        ShowCharacterSelection();
        _pendingWaitingCharacter = _waitingSelectedCharacter;
        _selectingForWaitingRoom = true;
        RefreshCharacterPreview();
    }
    private void ReturnFromCharacterSelection()
    {
        if (_selectingForWaitingRoom)
        {
            _pendingWaitingCharacter = _waitingSelectedCharacter;
            ShowWaitingRoomUI();
        }
        else ShowMainMenu();
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
    [SerializeField] private string _soloScenePath;
    private bool _soloLoading;
    public static string SoloCharacterId { get; private set; } = "";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSoloCharacterId()
    {
        SoloCharacterId = "";
    }
    [SerializeField] private TMPro.TMP_Text _selectionFooterLabel;
    private Color _soloFooterLabelColor;
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
        if (_selectionFooterLabel != null) _soloFooterLabelColor = _selectionFooterLabel.color;
        if (_startButton != null) _startButton.interactable = false;
    }
    private void OnEnable()
    {
        if (_soloButton != null) _soloButton.onClick.AddListener(ShowCharacterSelection);
        if (_backButton != null) _backButton.onClick.AddListener(ReturnFromCharacterSelection);
        if (_waitingChooseButton != null) _waitingChooseButton.onClick.AddListener(ShowWaitingCharacterSelection);
        if (_leaveRoomButton != null) _leaveRoomButton.onClick.AddListener(ShowLobbyUI);
        if (_chooseButton != null) _chooseButton.onClick.AddListener(ChooseMouse);
        if (_startButton != null) _startButton.onClick.AddListener(OnSelectionFooterClicked);
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
        if (_backButton != null) _backButton.onClick.RemoveListener(ReturnFromCharacterSelection);
        if (_waitingChooseButton != null) _waitingChooseButton.onClick.RemoveListener(ShowWaitingCharacterSelection);
        if (_leaveRoomButton != null) _leaveRoomButton.onClick.RemoveListener(ShowLobbyUI);
        if (_chooseButton != null) _chooseButton.onClick.RemoveListener(ChooseMouse);
        if (_startButton != null) _startButton.onClick.RemoveListener(OnSelectionFooterClicked);
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
        if (_characterPanel != null && _characterPanel.activeSelf)
        {
            ReturnFromCharacterSelection();
            return;
        }
        if (_waitingRoomPanel != null && _waitingRoomPanel.activeSelf)
        {
            ShowLobbyUI();
            return;
        }
        if (_lobbyPanel != null && _lobbyPanel.activeSelf) ShowMainMenu();
    }
    public void ShowCharacterSelection()
    {
        if (_characterPanel == null) return;
        _selectingForWaitingRoom = false;
        if (_waitingRoomPanel != null) _waitingRoomPanel.SetActive(false);
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
        if (_selectingForWaitingRoom)
        {
            _pendingWaitingCharacter = 0;
            RefreshCharacterPreview();
            return;
        }
        _selectedCharacter = 0;
        RefreshCharacterPreview();
    }
    private void ConfirmWaitingCharacter()
    {
        if (!_selectingForWaitingRoom || _characterPanel == null || !_characterPanel.activeSelf) return;
        if (_pendingWaitingCharacter < 0 || _pendingWaitingCharacter >= _characterCards.Length ||
            !_characterCards[_pendingWaitingCharacter].unlocked) return;
        _waitingSelectedCharacter = _pendingWaitingCharacter;
        ShowWaitingRoomUI();
    }
    private void OnSelectionFooterClicked()
    {
        if (_selectingForWaitingRoom) ConfirmWaitingCharacter();
        else StartSoloGame();
    }
    private void StartSoloGame()
    {
        if (_soloLoading || _selectingForWaitingRoom || _characterPanel == null || !_characterPanel.activeSelf) return;
        if (_selectedCharacter < 0 || _selectedCharacter >= _characterCards.Length ||
            !_characterCards[_selectedCharacter].unlocked) return;
        if (string.IsNullOrEmpty(_soloScenePath) || !Application.CanStreamedLevelBeLoaded(_soloScenePath))
        {
            Debug.LogError("Hospital Day 1 is not enabled in the build scene list: " + _soloScenePath, this);
            return;
        }
        string previousCharacterId = SoloCharacterId;
        _soloLoading = true;
        SoloCharacterId = _characterCards[_selectedCharacter].characterId;
        UpdateSelectionFooter();
        try
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(_soloScenePath, LoadSceneMode.Single);
            if (operation == null) throw new System.InvalidOperationException("Hospital Day 1 scene load could not be started.");
        }
        catch (System.Exception exception)
        {
            _soloLoading = false;
            SoloCharacterId = previousCharacterId;
            UpdateSelectionFooter();
            Debug.LogException(exception, this);
        }
    }
    private void UpdateSelectionFooter()
    {
        bool canConfirm = _selectingForWaitingRoom && _pendingWaitingCharacter >= 0 &&
            _pendingWaitingCharacter < _characterCards.Length && _characterCards[_pendingWaitingCharacter].unlocked;
        bool canStartSolo = !_selectingForWaitingRoom && !_soloLoading && _selectedCharacter >= 0 &&
            _selectedCharacter < _characterCards.Length && _characterCards[_selectedCharacter].unlocked;
        if (_startButton != null) _startButton.interactable = canConfirm || canStartSolo;
        if (_selectionFooterLabel != null)
        {
            _selectionFooterLabel.text = _selectingForWaitingRoom ? "Xác nhận" : "Bắt đầu";
            _selectionFooterLabel.color = canConfirm || canStartSolo ? Color.white : _soloFooterLabelColor;
        }
    }
    private void RefreshCharacterPreview()
    {
        if (_characterCards.Length == 0) return;
        UpdateSelectionFooter();
        int selectedCharacter = _selectingForWaitingRoom ? _pendingWaitingCharacter : _selectedCharacter;
        for (int i = 0; i < _characterCards.Length; i++)
        {
            var card = _characterCards[i];
            if (card == null) continue;
            if (card.frame != null) card.frame.gameObject.SetActive(i == _viewedCharacter);
            if (card.selectedBadge != null) card.selectedBadge.SetActive(i == selectedCharacter);
            if (card.viewedBadge != null) card.viewedBadge.SetActive(false);
        }
        if (_chooseButton != null) _chooseButton.gameObject.SetActive(selectedCharacter != 0);
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
        _selectingForWaitingRoom = false;
        if (_waitingRoomPanel != null) _waitingRoomPanel.SetActive(false);
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
