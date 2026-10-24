#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// AAH_SuperBrightLighting — Giải quyết triệt để vấn đề tối bên trong bệnh viện:
/// 1. Tắt bóng đổ của Directional Light để ánh sáng chiếu xuyên qua trần nhà vào tất cả các phòng.
/// 2. Gắn một nguồn sáng tỏa quang rộng (Player Ambient Aura Light) theo người chơi để đi tới đâu phòng đó sáng bừng.
/// 3. Bố trí đèn trần siêu sáng trực tiếp ngay trên quầy lễ tân và văn phòng tiếp nhận.
/// 4. Đặt Ambient Light môi trường lên mức sáng trắng tối đa.
/// </summary>
public class AAH_SuperBrightLighting : EditorWindow
{
    [MenuItem("Tools/AAH/Làm Sáng Tối Đa (Super Bright Interior)")]
    public static void MakeSuperBright()
    {
        // 1. TẮT BÓNG ĐỔ DIRECTIONAL LIGHT (Để ánh sáng xuyên qua mái nhà vào bên trong)
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional)
            {
                l.intensity = 1.5f;
                l.color = Color.white;
                l.shadows = LightShadows.None; // QUAN TRỌNG: Tắt bóng để ánh sáng xuyên trần vào trong phòng!
            }
        }

        // 2. GẮN ĐÈN TỎA QUANG VÀO NGƯỜI CHƠI (PLAYER AURA LIGHT - KHÔNG CẦN CẦM TAY)
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Transform existingAura = player.transform.Find("Player_Aura_Light");
            GameObject auraObj;
            if (existingAura != null)
            {
                auraObj = existingAura.gameObject;
            }
            else
            {
                auraObj = new GameObject("Player_Aura_Light");
                auraObj.transform.SetParent(player.transform);
                auraObj.transform.localPosition = new Vector3(0, 1.8f, 0); // Ngang tầm đầu
            }

            Light auraLight = auraObj.GetComponent<Light>();
            if (auraLight == null) auraLight = auraObj.AddComponent<Light>();

            auraLight.type = LightType.Point;
            auraLight.color = new Color(1f, 0.98f, 0.95f); // Trắng sáng dịu
            auraLight.intensity = 3.5f;                    // Siêu sáng
            auraLight.range = 35.0f;                       // Tỏa rộng 35 mét bao trọn mọi căn phòng người chơi bước vào
            auraLight.shadows = LightShadows.None;         // Không tính bóng để không bị sọc đen
        }

        // 3. ĐẶT ĐÈN TRẦN TRỰC TIẾP TRÊN NƠI NGƯỜI CHƠI ĐỨNG (QUẦY LỄ TÂN)
        if (player != null)
        {
            GameObject deskLight = GameObject.Find("Desk_Direct_Light");
            if (deskLight == null) deskLight = new GameObject("Desk_Direct_Light");
            
            deskLight.transform.position = player.transform.position + new Vector3(0, 2.5f, 1.5f);
            Light dl = deskLight.GetComponent<Light>();
            if (dl == null) dl = deskLight.AddComponent<Light>();

            dl.type = LightType.Point;
            dl.color = Color.white;
            dl.intensity = 4.0f;
            dl.range = 20.0f;
            dl.shadows = LightShadows.None;
        }

        // 4. THIẾT LẬP MÔI TRƯỜNG AMBIENT LIGHT TRẮNG SÁNG TỐI ĐA
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.9f, 0.9f, 0.95f); // Trắng sáng trong trẻo
        RenderSettings.fog = false;

        // Lưu scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Hoàn tất!",
            "Đã xử lý triệt để nguyên nhân tối trong phòng:\n\n" +
            "1. Tắt bóng đổ trần nhà (ánh sáng tổng giờ chiếu thẳng vào trong phòng).\n" +
            "2. Gắn Đèn Tỏa Quang tàng hình trên đầu Player (tỏa sáng 35m xung quanh bạn mà không cần cầm đèn pin).\n" +
            "3. Thêm đèn trần trực diện ngay trên quầy lễ tân.\n" +
            "4. Đẩy Ambient Light lên 90% trắng sáng.\n\n" +
            "Hãy bấm Play hoặc xem màn hình Scene, mọi thứ đã sáng bừng!", "OK");
    }
}
#endif
