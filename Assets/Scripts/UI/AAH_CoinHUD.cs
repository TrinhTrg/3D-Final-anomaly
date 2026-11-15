using UnityEngine;

public sealed class AAH_CoinHUD : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text _totalText;
    private IAAH_CoinWallet _wallet;
    private void OnEnable()
    {
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
        if (_wallet != null) _wallet.BalanceChanged -= Refresh;
        _wallet = null;
    }
    private void Refresh(int total)
    {
        if (_totalText != null) _totalText.SetText("Animal Coins: {0:0}", total);
    }
}
