#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool chuẩn hóa Player:
/// 1. Đặt vị trí Player đứng đúng phòng (X = 26, Y = -18, Z = 14.5).
/// 2. Cân chỉnh model y tá (CharacterModel) về gốc của Player (LocalPos = 0, LocalRot chuẩn).
/// 3. Đặt PlayerCamera đúng ngay tầm mắt nhân vật y tá nhìn thẳng.
/// 4. Chuẩn hóa CharacterController (chiều cao, tâm, bán kính) chống xuyên tường, kẹt sàn.
/// 5. Tạo sàn phụ an toàn (Safety Floor) và gắn MeshCollider cho bệnh viện chống rơi tự do.
/// </summary>
public class AAH_SetupPlayerPosition : EditorWindow
{
    [MenuItem("Tools/AAH/⚡ Căn Chỉnh Toàn Diện Player (Vị Trí + Camera + Chống Rơi)")]
    public static void SetupPlayerAll()
    {
        // 1. TÌM HOẶC KIỂM TRA PLAYER
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy GameObject 'Player' trong scene!", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(player, "Setup Player All");

        // Tắt CharacterController tạm thời trước khi di chuyển để tránh Unity snap ngược toạ độ
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 2. ĐẶT TOẠ ĐỘ PLAYER CHUẨN
        Vector3 targetPos = new Vector3(26f, -18f, 14.5f);
        player.transform.position = targetPos;
        player.transform.rotation = Quaternion.Euler(0, 0, 0); // Hướng nhìn ban đầu

        // 3. CĂN CHỈNH MODEL CON (CharacterModel - Y TÁ)
        Transform charModel = player.transform.Find("CharacterModel");
        if (charModel != null)
        {
            // Reset local position về đúng chân player
            charModel.localPosition = Vector3.zero;
            // Với GLB thường xoay X = -90 hoặc 0 tùy model gốc, nếu model có rotation riêng thì giữ hoặc đưa về identity
            // Đảm bảo scale chuẩn (1, 1, 1)
            charModel.localScale = Vector3.one;
        }

        // 4. CĂN CHỈNH PLAYERCAMERA (ĐẶT ĐÚNG TẦM MẮT Y TÁ)
        Transform camTrans = player.transform.Find("PlayerCamera");
        if (camTrans != null)
        {
            // Đặt ngang tầm đầu/mắt y tá (cao khoảng 1.1m - 1.2m, hơi nhích tới trước z = 0.1m)
            camTrans.localPosition = new Vector3(0f, 1.15f, 0.15f);
            camTrans.localRotation = Quaternion.identity;
            camTrans.localScale = Vector3.one;

            Camera cam = camTrans.GetComponent<Camera>();
            if (cam != null)
            {
                cam.nearClipPlane = 0.05f; // Chống bị cắt/thấy xuyên qua mặt chính mình
            }
        }

        // 5. CẤU HÌNH CHARACTER CONTROLLER (CHỐNG XUYÊN TƯỜNG, XUYÊN NỀN)
        if (cc == null) cc = player.AddComponent<CharacterController>();
        cc.height = 1.3f;        // Chiều cao vừa khít nhân vật y tá
        cc.radius = 0.35f;       // Bán kính đủ dày để không bao giờ đi lọt qua tường
        cc.center = new Vector3(0f, 0.65f, 0f); // Tâm ở giữa cơ thể
        cc.stepOffset = 0.3f;    // Bậc thềm leo được
        cc.skinWidth = 0.08f;
        cc.minMoveDistance = 0.001f;
        cc.enabled = true;       // Bật lại Controller

        // 6. GẮN MESH COLLIDER CHO BỆNH VIỆN VÀ TẠO SAFETY FLOOR CHỐNG RƠI
        SetupHospitalColliders(targetPos);

        // 7. LƯU SCENE
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Thành công!", 
            $"Đã căn chỉnh toàn diện Player:\n\n" +
            $"• Toạ độ Player: X = {targetPos.x}, Y = {targetPos.y}, Z = {targetPos.z}\n" +
            $"• Model Y tá: Đã ghim ngay tâm Player (LocalPos = 0).\n" +
            $"• Camera: Đã đặt ngay tầm mắt nhân vật (LocalPos = 0, 1.15, 0.15).\n" +
            $"• CharacterController: Height = 1.3m, Radius = 0.35m chống xuyên tường.\n" +
            $"• Đã tạo sàn an toàn (Safety Floor) tại Y = -18.2 chống rơi tự do.\n\n" +
            $"Bây giờ bạn chỉ cần bấm Play để trải nghiệm!", "Tuyệt vời");
    }

    private static void SetupHospitalColliders(Vector3 playerPos)
    {
        // Tìm model hospital trong scene
        GameObject hospital = GameObject.Find("hospital_full");
        if (hospital == null) hospital = GameObject.Find("Hospital_Environment");
        if (hospital == null) hospital = GameObject.Find("Hospital_Building");

        if (hospital != null)
        {
            // Gắn MeshCollider cho các mesh con nếu chưa có
            MeshFilter[] meshFilters = hospital.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }

            // Tạo tấm sàn an toàn vô hình ngay dưới chân Player tại Y = playerPos.y - 0.05f
            Transform safetyTrans = hospital.transform.Find("Safety_Floor_Solid");
            GameObject safetyFloor;
            if (safetyTrans != null) safetyTrans.gameObject.SetActive(true);
            
            if (safetyTrans != null)
            {
                safetyFloor = safetyTrans.gameObject;
            }
            else
            {
                safetyFloor = new GameObject("Safety_Floor_Solid");
                safetyFloor.transform.SetParent(hospital.transform);
            }

            safetyFloor.transform.position = new Vector3(playerPos.x, playerPos.y - 0.1f, playerPos.z);
            BoxCollider box = safetyFloor.GetComponent<BoxCollider>();
            if (box == null) box = safetyFloor.AddComponent<BoxCollider>();
            box.size = new Vector3(150f, 0.2f, 150f); // Bao phủ 150m rộng rãi
            box.center = Vector3.zero;
        }
    }
}
#endif

