using UnityEngine;

public sealed class AAH_CoinHUD : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text _totalText;
    private IAAH_CoinWallet _wallet;
    private AAH_GameManager _day1Manager;
    private bool _started;

    // Scene objects have completed Awake by Start, so the manager is available
    // even when this HUD was enabled first.
    private void Start()
    {
        _started = true;
        Bind();
    }
    private void OnEnable()
    {
        if (_started) Bind();
    }
    private void Bind()
    {
        var manager = AAH_GameManager.Instance;
        if (manager != null && manager.currentDay == 1)
        {
            _day1Manager = manager;
            _day1Manager.Day1CoinsChanged += Refresh;
            Refresh(_day1Manager.Day1Coins);
            return;
        }
        try
        {
            _wallet = AAH_CoinWallet.Current;
            _wallet.BalanceChanged += Refresh;
            Refresh(_wallet.Balance);
        }
        catch (System.Exception e)
        {
            if (_totalText != null) _totalText.text = "Animal Coins: lỗi dữ liệu";
            Debug.LogError("Animal Coins HUD could not read the save: " + e.Message, this);
        }
    }
    private void OnDisable()
    {
        if (_day1Manager != null) _day1Manager.Day1CoinsChanged -= Refresh;
        _day1Manager = null;
        if (_wallet != null) _wallet.BalanceChanged -= Refresh;
        _wallet = null;
    }
    private void Refresh(int total)
    {
        if (_totalText != null) _totalText.SetText("Animal Coins: {0:0}", total);
    }
}
