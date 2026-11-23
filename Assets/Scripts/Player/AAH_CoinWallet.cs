using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public interface IAAH_CoinWallet
{
    int Balance { get; }
    event Action<int> BalanceChanged;
    string GetOrCreateShift(int day);
    int GetShiftEarnings(string shiftId);
    int GetShiftDeaths(string shiftId);
    bool RecordShiftDeath(string shiftId, string patientId);
    bool TryAward(string rewardId, string shiftId, int coins);
    bool IsCharacterUnlocked(string characterId);
    bool TryUnlockCharacter(string characterId, int cost);
}

public static class AAH_CoinWallet
{
    private static IAAH_CoinWallet _current;
    private static bool _carryCoinsToDay2;

    public static void CarryCoinsToDay2()
    {
        _carryCoinsToDay2 = true;
    }

    public static string BeginHospitalShift(int day, string sceneName)
    {
        bool continuing = sceneName == AAH_ShiftEndUI.Day2Scene && _carryCoinsToDay2;
        _carryCoinsToDay2 = false;
        var wallet = Current;
        bool hospital = sceneName == "Hospital_day1" || sceneName == AAH_ShiftEndUI.Day2Scene;
        if (hospital && !continuing && wallet is AAH_JsonCoinWallet local)
            return local.StartFreshShift(day);
        return wallet.GetOrCreateShift(day);
    }

    public static IAAH_CoinWallet Current => _current ?? (_current = new AAH_JsonCoinWallet(Path.Combine(Application.persistentDataPath, "animal-coins.json")));
    // Supply a server-backed implementation here when multiplayer is introduced.
    public static void SetProvider(IAAH_CoinWallet wallet) { _current = wallet ?? throw new ArgumentNullException(nameof(wallet)); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetProvider()
    {
        _current = null;
        _carryCoinsToDay2 = false;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadAtStartup() { var wallet = Current; }
}

public sealed class AAH_JsonCoinWallet : IAAH_CoinWallet
{
    [Serializable] private sealed class Receipt
    {
        public string rewardId;
        public string shiftId;
        public int coins;
    }
    [Serializable] private sealed class SaveData
    {
        public int version = 1;
        public int totalCoins;
        public int activeDay;
        public string activeShiftId;
        public List<Receipt> awardedRewards = new List<Receipt>();
        public List<string> unlockedCharacters = new List<string>();
    }
    private readonly string _path;
    private SaveData _data;
    public int Balance => _data.totalCoins;
    public event Action<int> BalanceChanged;

    public AAH_JsonCoinWallet(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Save path is required.", nameof(path));
        _path = Path.GetFullPath(path);
        _data = Load();
    }
    private SaveData Load()
    {
        if (!File.Exists(_path)) return new SaveData();
        try { return Read(_path); }
        catch (NotSupportedException) { throw; }
        catch (Exception primaryError)
        {
            if (!File.Exists(_path + ".bak")) throw new InvalidDataException("Animal Coins save could not be read; it was not overwritten.", primaryError);
            var recovered = Read(_path + ".bak");
            Debug.LogWarning("Animal Coins save recovered from backup: " + primaryError.Message);
            return recovered;
        }
    }
    [Serializable] private sealed class SaveHeader { public int version; }
    private static SaveData Read(string path)
    {
        string json = File.ReadAllText(path);
        var header = JsonUtility.FromJson<SaveHeader>(json);
        if (header == null || header.version < 1) throw new InvalidDataException("Missing Animal Coins save version.");
        if (header.version != 1) throw new NotSupportedException("Unsupported Animal Coins save version: " + header.version);
        var data = JsonUtility.FromJson<SaveData>(json);
        if (data == null || data.version < 1 || data.totalCoins < 0 || data.awardedRewards == null || data.unlockedCharacters == null)
            throw new InvalidDataException("Invalid Animal Coins save.");
        if (data.version != 1) throw new NotSupportedException("Unsupported Animal Coins save version: " + data.version);
        var ids = new HashSet<string>();
        long ledgerBalance = 0;
        foreach (var receipt in data.awardedRewards)
        {
            if (receipt == null || string.IsNullOrEmpty(receipt.rewardId) || !ids.Add(receipt.rewardId))
                throw new InvalidDataException("Invalid or duplicate reward receipt.");
            ledgerBalance += receipt.coins;
        }
        if (ledgerBalance != data.totalCoins) throw new InvalidDataException("Coin balance does not match its reward receipts.");
        var unlockIds = new HashSet<string>();
        foreach (var id in data.unlockedCharacters)
            if (string.IsNullOrWhiteSpace(id) || !unlockIds.Add(id)) throw new InvalidDataException("Invalid character unlock record.");
        return data;
    }
    private SaveData Copy() => JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(_data));
    private bool Commit(SaveData next)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(next, true));
            using (var stream = new FileStream(_path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(_path)) File.Replace(_path + ".tmp", _path, _path + ".bak");
            else File.Move(_path + ".tmp", _path);
            bool balanceChanged = next.totalCoins != _data.totalCoins;
            _data = next;
            if (balanceChanged) BalanceChanged?.Invoke(Balance);
            return true;
        }
        catch (IOException e) { Debug.LogError("Animal Coins save failed; transaction was not applied: " + e.Message); return false; }
        catch (UnauthorizedAccessException e) { Debug.LogError("Animal Coins save is not writable: " + e.Message); return false; }
    }
    public string GetOrCreateShift(int day)
    {
        if (day < 1) throw new ArgumentOutOfRangeException(nameof(day));
        // Reloading/restarting the same day reuses the reward namespace, preventing repeat payouts.
        if (_data.activeDay == day && !string.IsNullOrEmpty(_data.activeShiftId)) return _data.activeShiftId;
        return SaveNewShift(day, Copy());
    }

    internal string StartFreshShift(int day)
    {
        var fresh = new SaveData { unlockedCharacters = new List<string>(_data.unlockedCharacters) };
        return SaveNewShift(day, fresh);
    }

    private string SaveNewShift(int day, SaveData next)
    {
        next.activeDay = day;
        next.activeShiftId = Guid.NewGuid().ToString("N");
        if (!Commit(next)) throw new IOException("Could not persist shift identity; rewards are disabled for this shift.");
        return _data.activeShiftId;
    }
    public int GetShiftEarnings(string shiftId)
    {
        int coins = 0;
        foreach (var receipt in _data.awardedRewards)
            if (!string.IsNullOrEmpty(shiftId) && receipt.shiftId == shiftId && receipt.coins > 0) coins = checked(coins + receipt.coins);
        return coins;
    }
    public int GetShiftDeaths(string shiftId)
    {
        int deaths = 0;
        foreach (var receipt in _data.awardedRewards)
            if (receipt.shiftId == shiftId && receipt.rewardId.StartsWith(shiftId + ":death:", StringComparison.Ordinal)) deaths++;
        return deaths;
    }
    public bool RecordShiftDeath(string shiftId, string patientId)
    {
        if (string.IsNullOrWhiteSpace(shiftId) || string.IsNullOrWhiteSpace(patientId)) return false;
        string id = shiftId + ":death:" + patientId;
        foreach (var receipt in _data.awardedRewards) if (receipt.rewardId == id) return false;
        var next = Copy(); next.awardedRewards.Add(new Receipt { rewardId = id, shiftId = shiftId, coins = 0 });
        return Commit(next);
    }
    public bool TryAward(string rewardId, string shiftId, int coins)
    {
        if (string.IsNullOrWhiteSpace(rewardId) || string.IsNullOrWhiteSpace(shiftId) || coins <= 0 || rewardId.StartsWith("character-unlock:", StringComparison.Ordinal)) return false;
        foreach (var receipt in _data.awardedRewards) if (receipt.rewardId == rewardId) return false;
        if (Balance > int.MaxValue - coins) return false;
        var next = Copy(); next.totalCoins += coins;
        next.awardedRewards.Add(new Receipt { rewardId = rewardId, shiftId = shiftId, coins = coins });
        return Commit(next);
    }
    public bool IsCharacterUnlocked(string characterId) => !string.IsNullOrEmpty(characterId) && _data.unlockedCharacters.Contains(characterId);
    public bool TryUnlockCharacter(string characterId, int cost)
    {
        if (string.IsNullOrWhiteSpace(characterId) || cost < 0 || Balance < cost || IsCharacterUnlocked(characterId)) return false;
        var next = Copy(); next.totalCoins -= cost; next.unlockedCharacters.Add(characterId);
        next.awardedRewards.Add(new Receipt { rewardId = "character-unlock:" + characterId, shiftId = "", coins = -cost });
        return Commit(next);
    }
}
