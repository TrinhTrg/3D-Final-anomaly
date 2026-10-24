#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// AAH_FixPlayerFall — Tự động tính toán vị trí sàn bệnh viện, 
/// gắn BoxCollider/MeshCollider cho sàn và đặt Player đứng vững trên sàn.
/// </summary>
public class AAH_FixPlayerFall : EditorWindow
{
    [MenuItem("Tools/AAH/Sửa lỗi rơi tự do (Fix Player Spawn & Floor)")]
    public static void FixFall()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy GameObject 'Player' trong scene!", "OK");
            return;
        }

        // Tìm model bệnh viện
        GameObject hospital = GameObject.Find("Hospital_Environment");
        if (hospital == null)
        {
            hospital = GameObject.Find("Hospital_Building");
        }

        if (hospital == null)
        {
            // Tìm bất kỳ object nào có chữ Hospital
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.ToLower().Contains("hospital") && go.transform.parent != null && go.transform.parent.name.Contains("ENVIRONMENT"))
                {
                    hospital = go;
                    break;
                }
            }
        }

        if (hospital == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy model bệnh viện trong scene!", "OK");
            return;
        }

        // 1. Quét toàn bộ Renderer để tính Bounding Box của Bệnh Viện
        Renderer[] renderers = hospital.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Model bệnh viện không chứa Mesh/Renderer nào!", "OK");
            return;
        }

        Bounds totalBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            totalBounds.Encapsulate(renderers[i].bounds);
        }

        Debug.Log($"[AAH] Kích thước Bệnh viện (Bounds): Center={totalBounds.center}, Size={totalBounds.size}, Min={totalBounds.min}, Max={totalBounds.max}");

        // 2. Thêm MeshCollider cho tất cả các Mesh trong bệnh viện
        int meshColliderCount = 0;
        MeshFilter[] meshFilters = hospital.GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                meshColliderCount++;
            }
        }

        // Kiểm tra xem có SkinnedMeshRenderer không
        SkinnedMeshRenderer[] smrs = hospital.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in smrs)
        {
            if (smr.sharedMesh != null && smr.GetComponent<Collider>() == null)
            {
                MeshCollider mc = smr.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = smr.sharedMesh;
                meshColliderCount++;
            }
        }

        // 3. Tạo một Sàn Phụ Bằng Tàng Hình (Invisible Safety Floor) ngay đáy bệnh viện
        // Để đảm bảo 100% không bao giờ bị rơi xuyên sàn dù model có khe hở mesh
        Transform safetyFloorTrans = hospital.transform.Find("Safety_Floor_Collider");
        GameObject safetyFloor;
        if (safetyFloorTrans != null)
        {
            safetyFloor = safetyFloorTrans.gameObject;
        }
        else
        {
            safetyFloor = new GameObject("Safety_Floor_Collider");
            safetyFloor.transform.SetParent(hospital.transform);
        }

        BoxCollider floorBox = safetyFloor.GetComponent<BoxCollider>();
        if (floorBox == null) floorBox = safetyFloor.AddComponent<BoxCollider>();

        // Đặt kích thước sàn bao phủ toàn bộ diện tích bệnh viện
        safetyFloor.transform.position = new Vector3(totalBounds.center.x, totalBounds.min.y + 0.1f, totalBounds.center.z);
        floorBox.size = new Vector3(totalBounds.size.x * 1.5f, 0.4f, totalBounds.size.z * 1.5f);
        floorBox.center = Vector3.zero;

        // 4. Đặt vị trí Player đứng ngay trên mặt sàn ở tâm bệnh viện
        Vector3 spawnPos = new Vector3(totalBounds.center.x, totalBounds.min.y + 1.5f, totalBounds.center.z);
        
        // Thử tìm xem có raycast chạm xuống sàn từ trên xuống không để spawn chuẩn nhất
        RaycastHit hit;
        Vector3 rayStart = new Vector3(totalBounds.center.x, totalBounds.max.y + 5f, totalBounds.center.z);
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 100f))
        {
            spawnPos = hit.point + Vector3.up * 1.0f;
        }

        player.transform.position = spawnPos;

        // Đảm bảo CharacterController bật và thông số chuẩn
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false; // Tắt trước khi đổi vị trí để tránh bị snap
            player.transform.position = spawnPos;
            cc.enabled = true;
        }

        // Đảm bảo SC_FPSController không bị tắt
        SC_FPSController fps = player.GetComponent<SC_FPSController>();
        if (fps != null)
        {
            fps.canMove = true;
        }

        // Đánh dấu scene đã sửa và lưu
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Thành công!", 
            $"Đã khắc phục lỗi rơi tự do:\n\n" +
            $"1. Gắn {meshColliderCount} MeshColliders cho các bức tường và sàn.\n" +
            $"2. Tạo Sàn an toàn tàng hình chống lọt khe mesh.\n" +
            $"3. Đặt Player vào đúng vị trí sàn tại: {spawnPos}\n\n" +
            $"Bây giờ bạn hãy bấm Play để test lại!", "OK");
    }
}
#endif
