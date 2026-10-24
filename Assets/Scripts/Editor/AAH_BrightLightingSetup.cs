#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// AAH_BrightLightingSetup — Xóa hoàn toàn đèn pin khỏi tay người chơi,
/// làm sáng toàn bộ bệnh viện với hệ thống đèn trần chuẩn bệnh viện và ánh sáng môi trường sáng rõ.
/// </summary>
public class AAH_BrightLightingSetup : EditorWindow
{
    [MenuItem("Tools/AAH/Bật Ánh Sáng Bệnh Viện & Xóa Đèn Pin (Bright Hospital Lighting)")]
    public static void ApplyBrightLighting()
    {
        // 1. XÓA ĐÈN PIN TRÊN NGƯỜI CHƠI VÀ TRONG SCENE
        int deletedLights = 0;
        GameObject playerCam = GameObject.FindGameObjectWithTag("MainCamera");
        if (playerCam != null)
        {
            Transform fl = playerCam.transform.Find("Flashlight");
            if (fl != null)
            {
                DestroyImmediate(fl.gameObject);
                deletedLights++;
            }
        }

        // Tìm bất kỳ object nào tên Flashlight hoặc script LightScript trong scene
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.name.ToLower().Contains("flashlight") || go.GetComponent<LightScript>() != null)
            {
                DestroyImmediate(go);
                deletedLights++;
            }
        }

        Debug.Log($"[AAH] Đã xóa {deletedLights} đối tượng đèn pin. Tay người chơi hoàn toàn trống!");

        // 2. THIẾT LẬP DIRECTIONAL LIGHT SÁNG RÕ
        Light dirLight = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional)
            {
                dirLight = l;
                break;
            }
        }

        if (dirLight == null)
        {
            GameObject dgo = new GameObject("Directional Light");
            dirLight = dgo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dgo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        dirLight.color = new Color(1f, 0.98f, 0.95f); // Ánh sáng trắng ấm tự nhiên
        dirLight.intensity = 1.1f;                     // Độ sáng chuẩn
        dirLight.shadows = LightShadows.Soft;

        // 3. TẮT SƯƠNG MÙ TỐI & BẬT ÁNH SÁNG MÔI TRƯỜNG (AMBIENT LIGHT) SÁNG
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.7f, 0.73f, 0.78f); // Ánh sáng tản đều khắp phòng, không còn góc tối đen
        RenderSettings.fog = false;                                   // Tắt sương mù đen

        // 4. TẠO HỆ THỐNG ĐÈN TRẦN BỆNH VIỆN (CEILING LIGHTS)
        GameObject lightsGroup = GameObject.Find("=== HOSPITAL_LIGHTS ===");
        if (lightsGroup != null) DestroyImmediate(lightsGroup);

        lightsGroup = new GameObject("=== HOSPITAL_LIGHTS ===");

        // Tìm kích thước bệnh viện để bố trí đèn phủ khắp
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(30f, 4f, 30f));
        GameObject hospital = GameObject.Find("Hospital_Building");
        if (hospital == null) hospital = GameObject.Find("Hospital_Environment");

        if (hospital != null)
        {
            Renderer[] rends = hospital.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                bounds = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) bounds.Encapsulate(rends[i].bounds);
            }
        }

        float lightY = Mathf.Max(bounds.min.y + 2.8f, 2.8f);
        Color hospitalFluorescent = new Color(0.95f, 0.98f, 1f); // Màu đèn huỳnh quang bệnh viện trắng sáng

        // Bố trí lưới đèn trần Point Light phủ kín diện tích
        int countX = Mathf.Clamp(Mathf.RoundToInt(bounds.size.x / 6.0f), 3, 7);
        int countZ = Mathf.Clamp(Mathf.RoundToInt(bounds.size.z / 6.0f), 3, 7);

        float stepX = bounds.size.x / countX;
        float stepZ = bounds.size.z / countZ;

        int spawnedLights = 0;
        for (int ix = 0; ix < countX; ix++)
        {
            for (int iz = 0; iz < countZ; iz++)
            {
                float posX = bounds.min.x + stepX * (ix + 0.5f);
                float posZ = bounds.min.z + stepZ * (iz + 0.5f);

                GameObject lightObj = new GameObject($"Ceiling_Light_{ix}_{iz}");
                lightObj.transform.SetParent(lightsGroup.transform);
                lightObj.transform.position = new Vector3(posX, lightY, posZ);

                Light ptLight = lightObj.AddComponent<Light>();
                ptLight.type = LightType.Point;
                ptLight.color = hospitalFluorescent;
                ptLight.intensity = 2.0f;
                ptLight.range = 10.0f;
                ptLight.shadows = LightShadows.None; // Không tính bóng đèn trần phụ để game chạy siêu mượt

                spawnedLights++;
            }
        }

        // 5. CẬP NHẬT UI HƯỚNG DẪN ĐIỀU KHIỂN (XÓA CHỮ "F: Flashlight")
        GameObject txtControls = GameObject.Find("TXT_Controls");
        if (txtControls != null)
        {
            TextMeshProUGUI tmp = txtControls.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = "WASD: Move | Shift: Sprint | E: Inspect | C: CCTV";
            }
        }

        // Lưu scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Thành công!",
            $"Đã thiết lập ánh sáng bệnh viện sáng rõ:\n\n" +
            $"1. Đã xóa hoàn toàn đèn pin trên tay người chơi.\n" +
            $"2. Tắt sương mù đen và tăng Ambient Light sáng rõ.\n" +
            $"3. Bố trí {spawnedLights} bóng đèn trần huỳnh quang phủ khắp bệnh viện.\n" +
            $"4. Cập nhật HUD giao diện loại bỏ phím Flashlight.\n\n" +
            $"Bây giờ game sáng sủa như một bệnh viện thật!", "Tuyệt vời");
    }
}
#endif
