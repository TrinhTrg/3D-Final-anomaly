using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[InitializeOnLoad]
internal static class PlayerHudVerification
{
    const string Key = "PlayerHudTest.State";
    static int frames, step; static float waitUntil; static double deadline;
    static readonly List<string> logs = new List<string>();
    static AAH_PlayerSanity sanity; static AAH_PlayerInventory inventory; static AAH_EquippedItemView view; static AAH_SanityHUD hud; static AAH_InventoryHUD invHud;
    static AAH_SanityEventRules rules; static AAH_CoffeeMachine coffee; static AAH_ItemDefinition coffeeItem, chocolateItem; static Camera cam;
    static int startValue, depletedCount;

    static PlayerHudVerification()
    {
        var s = SessionState.GetString(Key, "Idle");
        if (s == "WaitingForCompile") EditorApplication.delayCall += () => { SessionState.SetString(Key, "EnteringPlayMode"); EditorApplication.playModeStateChanged += Changed; EditorApplication.isPlaying = true; };
        else if (s == "EnteringPlayMode") { EditorApplication.playModeStateChanged += Changed; if (EditorApplication.isPlaying) Begin(); }
        else if (s == "Done" && string.IsNullOrEmpty(SessionState.GetString("PlayerHudTest.Screenshot", ""))) { }
    }
    static void Changed(PlayModeStateChange c) { if (c == PlayModeStateChange.EnteredPlayMode) Begin(); }
    static void Begin() { EditorApplication.playModeStateChanged -= Changed; SessionState.SetString(Key, "InPlayMode"); deadline = EditorApplication.timeSinceStartup + 40; EditorApplication.update += Tick; }
    static void Log(string s) { logs.Add(s); Debug.Log("[Test] " + s); }
    static void Require(bool ok, string s) { Log(s + " => " + ok); if (!ok) throw new Exception(s); }
    static void Wait(float seconds) { waitUntil = Time.realtimeSinceStartup + seconds; }
    static T Field<T>(object o, string n) { return (T)o.GetType().GetField(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(o); }
    static void SetField(object o, string n, object v) { o.GetType().GetField(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(o, v); }
    static string Percent() { return Field<TMPro.TMP_Text>(hud, "_percentText").text; }
    static string Status() { return Field<TMPro.TMP_Text>(hud, "_statusText").text; }

    static void Tick()
    {
        if (++frames < 5 || Time.realtimeSinceStartup < waitUntil) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout at step " + step);
            Run();
        }
        catch (Exception e) { Time.timeScale = 1f; Finish(false, e.Message); }
    }

    static void Run()
    {
        switch (step++)
        {
            case 0:
                var player = GameObject.Find("Player");
                sanity = player.GetComponent<AAH_PlayerSanity>(); inventory = player.GetComponent<AAH_PlayerInventory>(); view = player.GetComponent<AAH_EquippedItemView>();
                hud = UnityEngine.Object.FindFirstObjectByType<AAH_SanityHUD>(); invHud = UnityEngine.Object.FindFirstObjectByType<AAH_InventoryHUD>();
                rules = AAH_SanityEventRules.Instance; coffee = UnityEngine.Object.FindFirstObjectByType<AAH_CoffeeMachine>(); cam = player.GetComponent<SC_FPSController>().playerCamera;
                coffeeItem = AssetDatabase.LoadAssetAtPath<AAH_ItemDefinition>("Assets/Items/Item_Coffee.asset"); chocolateItem = AssetDatabase.LoadAssetAtPath<AAH_ItemDefinition>("Assets/Items/Item_Chocolate.asset");
                Require(sanity && inventory && view && hud && invHud && rules && coffee, "All components wired");
                Require(sanity.Current == 100 && Percent() == "100%" && Status() == "Stable", "Starts at 100% Stable");
                sanity.Depleted += () => depletedCount++;
                Require(inventory.TryAdd(coffeeItem) && inventory.TryAdd(chocolateItem), "Add coffee + chocolate");
                Require(view.CurrentInstance != null, "Slot 1 coffee is equipped");
                break;
            case 1:
                var vp = cam.WorldToViewportPoint(view.CurrentInstance.GetComponentInChildren<Renderer>().bounds.center);
                Log("Coffee viewport=" + vp.ToString("F2"));
                Require(vp.z > 0 && Mathf.Abs(vp.x - 0.8f) < 0.06f && Mathf.Abs(vp.y - 0.2f) < 0.08f, "Equipped item centred near viewport (0.8, 0.2)");
                var rs = view.CurrentInstance.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float minX = 1, maxX = 0, minY = 1, maxY = 0;
                for (int c = 0; c < 8; c++) { var corner = new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z); var v = cam.WorldToViewportPoint(corner); minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x); minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
                Log("Item viewport rect x[" + minX.ToString("F2") + "," + maxX.ToString("F2") + "] y[" + minY.ToString("F2") + "," + maxY.ToString("F2") + "]");
                Require(minX > 0.55f && maxX < 1f && minY > 0f && maxY < 0.45f, "Item fully on screen and away from the centre");
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                using (UnityEngine.InputSystem.LowLevel.StateEvent.From(keyboard, out var e)) { keyboard.digit2Key.WriteValueIntoEvent(1f, e); UnityEngine.InputSystem.InputSystem.QueueEvent(e); }
                Wait(0.25f);
                break;
            case 2:
                var kb = UnityEngine.InputSystem.Keyboard.current;
                using (UnityEngine.InputSystem.LowLevel.StateEvent.From(kb, out var e2)) { kb.digit2Key.WriteValueIntoEvent(0f, e2); UnityEngine.InputSystem.InputSystem.QueueEvent(e2); }
                if (inventory.SelectedIndex == 1) Log("Key 2 selected slot 2 via Input System");
                else { Log("Keyboard event dropped (Game view unfocused); selecting slot 2 directly"); inventory.Select(1); }
                Require(inventory.SelectedIndex == 1, "Slot 2 selected");
                Require(view.CurrentInstance == null, "Chocolate has no model so view is cleared (previous hidden)");
                var slots = Field<Array>(invHud, "_slots");
                var button = (UnityEngine.UI.Button)slots.GetValue(3).GetType().GetField("button").GetValue(slots.GetValue(3));
                button.onClick.Invoke();
                Require(inventory.SelectedIndex == 3 && view.CurrentInstance == null, "Clicking empty slot 4 selects it and hides item");
                var frame3 = (UnityEngine.UI.Image)slots.GetValue(3).GetType().GetField("frame").GetValue(slots.GetValue(3));
                var frame0 = (UnityEngine.UI.Image)slots.GetValue(0).GetType().GetField("frame").GetValue(slots.GetValue(0));
                Require(frame3.color != frame0.color, "Selected slot highlighted");
                inventory.Select(0);
                Require(view.CurrentInstance != null, "Reselecting coffee shows it again");
                Require(!inventory.UseSelected() && inventory.GetItem(0) == coffeeItem, "Coffee not wasted at 100 sanity");
                sanity.ApplyChange(-17);
                Require(Percent() == "83%" && Status() == "Stable", "83% Stable");
                var floating = Field<TMPro.TMP_Text>(hud, "_floatingText");
                Require(floating.gameObject.activeSelf && floating.text == "-17 Sanity", "Floating text shows -17 Sanity");
                ScreenCapture.CaptureScreenshot("Assets/PlayerHudTestPreview.png");
                SessionState.SetString("PlayerHudTest.Screenshot", "Assets/PlayerHudTestPreview.png");
                Wait(0.3f);
                break;
            case 3:
                sanity.ApplyChange(-38);
                Require(Percent() == "45%" && Status() == "Stressed", "45% Stressed");
                Require(inventory.UseSelected() && sanity.Current == 60 && inventory.GetItem(0) == null, "Coffee +15 consumed and removed");
                Require(view.CurrentInstance == null, "Consumed coffee unequipped");
                inventory.Select(1);
                Require(inventory.UseSelected() && sanity.Current == 100 && inventory.GetItem(1) == null, "Chocolate +60 clamped to 100");
                // Taser rules
                var normal = new GameObject("TestNormal"); normal.AddComponent<AAH_AnimalPatient>().isAnomaly = false;
                var anomaly = new GameObject("TestAnomaly"); anomaly.AddComponent<AAH_AnimalPatient>().isAnomaly = true;
                var mateGo = new GameObject("TestTeammate"); var mate = mateGo.AddComponent<AAH_PlayerSanity>();
                Require(rules.RegisterTaserHit(1, sanity, normal) == AAH_SanityEventRules.TaserResult.NormalPatient && sanity.Current == 85, "Normal patient taser -15");
                Require(rules.RegisterTaserHit(1, sanity, normal) == AAH_SanityEventRules.TaserResult.Ignored && sanity.Current == 85, "Duplicate hit id ignored");
                Require(rules.RegisterTaserHit(2, sanity, mateGo) == AAH_SanityEventRules.TaserResult.Teammate && sanity.Current == 83 && mate.Current == 98, "Teammate taser -2 both");
                Require(rules.RegisterTaserHit(3, sanity, anomaly) == AAH_SanityEventRules.TaserResult.Anomaly && sanity.Current == 85, "Anomaly taser +2");
                Require(rules.RegisterTaserHit(3, sanity, anomaly) == AAH_SanityEventRules.TaserResult.Ignored && sanity.Current == 85, "Anomaly reward not duplicated");
                UnityEngine.Object.Destroy(mateGo);
                // Passive + pause
                SetField(sanity, "_passiveInterval", 0.5f); SetField(sanity, "_passiveTimer", 0f);
                startValue = sanity.Current; Time.timeScale = 0f; Wait(1.2f);
                break;
            case 4:
                Require(sanity.Current == startValue, "No passive loss while paused");
                Time.timeScale = 1f; Wait(1.2f);
                break;
            case 5:
                Require(sanity.Current <= startValue - 1 && sanity.Current >= startValue - 3, "Passive loss resumes after unpause (" + startValue + "->" + sanity.Current + ")");
                SetField(sanity, "_passiveInterval", 9999f);
                SetField(sanity, "_anomalyDrainPerSecond", 10f);
                var threat = new object(); SessionState.SetString("unused", ""); _threat = threat;
                startValue = sanity.Current; rules.ReportAnomalyCheckedIn(threat); rules.ReportAnomalyCheckedIn(threat);
                Wait(0.6f);
                break;
            case 6:
                int drained = startValue - sanity.Current;
                Require(drained >= 4 && drained <= 8, "Anomaly drain active (" + drained + " in ~0.6s at 10/s)");
                rules.ReportAnomalyResolved(_threat); startValue = sanity.Current; Wait(0.5f);
                break;
            case 7:
                Require(sanity.Current == startValue && !sanity.IsAnomalyDrainActive, "Drain stops when resolved");
                _encounter = new object(); startValue = sanity.Current;
                rules.BeginRedEyeEncounter(sanity, _encounter); rules.BeginRedEyeEncounter(sanity, _encounter);
                Wait(2.3f);
                break;
            case 8:
                Require(sanity.Current == startValue - 5, "Red-eye -5 after 2s, no duplicate timer (" + startValue + "->" + sanity.Current + ")");
                rules.EndRedEyeEncounter(sanity, _encounter); startValue = sanity.Current; Wait(2.3f);
                break;
            case 9:
                Require(sanity.Current == startValue, "Red-eye drain stops after encounter ends");
                // Coffee machine with a full inventory does not start brewing
                for (int i = 0; i < 5; i++) inventory.TryAdd(chocolateItem);
                Require(!inventory.HasFreeSlot, "Inventory full at 5 items");
                Require(!inventory.TryAdd(coffeeItem), "6th item rejected");
                coffee.GetType().GetMethod("TakeCoffee", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(coffee, null);
                Require(coffee.IsReady && coffee.CupsTaken == 0, "Full inventory: coffee not dispensed, no cooldown");
                inventory.RemoveAt(4);
                coffee.GetType().GetMethod("TakeCoffee", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(coffee, null);
                Require(!coffee.IsReady && inventory.GetItem(4) == coffeeItem, "Coffee machine puts coffee in inventory and starts 180s");
                // Depletion
                sanity.ApplyChange(-500);
                Require(sanity.Current == 0 && sanity.IsDepleted && depletedCount == 1, "Depleted event once at 0");
                Require(Percent() == "0%" && Status() == "Sanity Depleted", "HUD shows Sanity Depleted");
                sanity.ApplyChange(-5); sanity.BeginRedEyeEncounter(new object());
                Require(sanity.Current == 0 && depletedCount == 1 && !sanity.IsRedEyeActive, "Further reduction disabled after depletion");
                Finish(true, null);
                break;
        }
    }
    static object _threat, _encounter;

    static void Finish(bool ok, string error)
    {
        EditorApplication.update -= Tick;
        SessionState.SetString("PlayerHudTest.Result", (ok ? "PASS" : "FAIL: " + error) + "\n" + string.Join("\n", logs));
        SessionState.SetString(Key, "Done");
        EditorApplication.isPlaying = false;
    }
}
