# Project Overview
- Game Title: Anomaly Animal Hospital (tên xuất hiện trong script hiện có; chưa xác nhận tên phát hành).
- High-Level Concept: Người chơi làm lễ tân bệnh viện động vật, tiếp nhận từng bệnh nhân qua hồ sơ, ảnh, đăng ký và giấy chỉ định phòng. Bệnh nhân tự đi từ ngoài bệnh viện tới quầy, bấm chuông, giới thiệu rồi tới giường trong phòng được cấp.
- Players: Single player, NPC do máy điều khiển.
- Inspiration / Reference Games: Ba ảnh tham chiếu người dùng cung cấp: NPC ngoài cửa, máy tính/máy in tại quầy, máy ảnh/chuông/clipboard và Polaroid. Không suy đoán tên trò chơi tham chiếu.
- Tone / Art Direction: Bệnh viện động vật 3D stylized, giữ model và vật liệu hiện có; prototype NPC không rig.
- Target Platform: StandaloneWindows64.
- Screen Orientation / Resolution: Landscape; HUD responsive, tham chiếu 1920x1080, kiểm tra thêm 16:10 và các kích thước Game View.
- Render Pipeline: URP 17.0.4, Unity 6000.0.85f1.

# Game Mechanics
## Core Gameplay Loop
1. Một NPC xuất phát ngoài cửa bệnh viện, đi vào và đứng đúng vị trí đối diện quầy.
2. NPC tự mô phỏng nhấn chuông một lần, phát tiếng placeholder và thoại theo hồ sơ, ví dụ “I'm Mai Yamada.” / “I'm here for my appointment...”. Người chơi không cần bấm chuông gọi bệnh nhân.
3. Sau thoại, NPC trình clipboard. Người chơi thực hiện đúng thứ tự: xem/ký clipboard → chụp ảnh → đăng ký máy tính → in giấy phòng → hoàn tất.
4. Chỉ vật của bước đang khả dụng có vòng viền xanh. Khóa tương tác khi modal hoặc thao tác bất đồng bộ đang chạy; không cho nhấn E nhiều lần để chạy trùng coroutine.
5. Ảnh bệnh nhân ban đầu đen, phát triển trong khoảng 3 giây, giữ trên HUD khi di chuyển. Giữ ảnh bệnh nhân vừa chụp cho tới khi chụp người tiếp theo; không xóa ngay khi người tiếp theo tới.
6. Cấp một trong sáu phòng còn trống, giữ chỗ theo patient ID trước khi in, không cấp trùng. Phòng và giường ánh xạ bằng cấu hình rõ ràng, không suy đoán từ số tên door.
7. Phát OnPatientCheckedIn đúng một lần. NPC chuyển Leaving (rời quầy), đi qua cửa phù hợp, tới điểm cạnh giường, dừng agent rồi chuyển model con sang tư thế nằm prototype.
8. NPC tiếp theo chỉ vào tiếp nhận khi quầy đã trống. Có thể NPC trước đang đi tới phòng trong lúc người tiếp theo tới quầy.
9. Khi đủ sáu phòng: dừng tiếp nhận NPC mới, HUD báo chờ phòng trống. Không tự xuất viện và không chồng NPC trên giường. Cung cấp API ReleaseRoom cho gameplay sau này, nhưng chưa tự động gọi.

## Controls and Input Methods
- Project Settings đã đọc: activeInputHandler=2 (Both), Input System 1.20.0; giữ FPSController hiện tại, không di chuyển toàn bộ hệ điều khiển sang backend mới.
- Tương tác check-in mới dùng InputAction event-driven với phím E. Một bộ chọn tương tác tập trung raycast từ PlayerCamera, giới hạn khoảng cách cấu hình, chỉ dispatch tới mục tiêu hợp lệ thuộc bước hiện tại.
- Khoảng cách tương tác check-in ban đầu giữ 2.5 m theo script hiện có và cho sửa Inspector. Phạm vi 7 m là riêng cửa reception, không tự áp dụng cho máy móc.
- Clipboard: E mở giấy, nút “Ký xác nhận” hoàn thành, Escape đóng mà không hoàn thành. Khi modal mở khóa chuột, tạm khóa di chuyển/góc nhìn; đóng phục hồi đúng trạng thái trước đó.
- Đăng ký: E bắt đầu progress, hoàn tất hiện REGISTERED và mở máy in. Không thêm thao tác xác nhận riêng ngoài yêu cầu nếu không cần.
- Cửa NPC tự mở khi tới vùng tiếp cận; giữ hành vi E mở cửa reception của người chơi.

# UI
Wireframe đề xuất, dùng Canvas hiện có hoặc nhóm check-in riêng bên dưới Canvas thích hợp:
```text
+--------------------------------------------------+
| Tiếp nhận: tên NPC          Phòng còn trống: n/6  |
|                                                  |
|        [Hồ sơ bệnh nhân / Ký xác nhận]            |
|        hoặc [REGISTERING... ======= 70%]          |
|                                                  |
| [Tên NPC: I'm here for my appointment...]         |
|              [E] Chụp ảnh             +---------+ |
|                                      |  ẢNH    | |
|                                      | Tên NPC | |
+--------------------------------------------------+
```
- Dialogue ở dưới, prompt chỉ khi nhìn đúng vật trong phạm vi và được mở khóa.
- Polaroid trắng, phần ảnh vuông đen → ảnh thật, tên đen; góc dưới phải, không chặn tương tác.
- Giấy in có tên, patient ID và ROOM 1–6, đồng nhất với phòng NPC thực sự đi tới; có giấy world-space tại khe máy in.
- Vòng xanh là đường viền/ring rỗng có material URP, không phải cylinder xanh đặc; không có collider, không che mặt máy hay giấy.
- Progress có nền và fill thực, không chỉ Slider không có hình ảnh. Text có tương phản rõ, font TMP đủ glyph tiếng Việt; không sửa font dùng chung nếu chỉ font cửa hiện tại chứa ít glyph.
- Đảm bảo đúng một EventSystem, GraphicRaycaster, button callback và input module phù hợp. Không tạo lại toàn bộ UI hiện có.

# Key Asset & Context
## Tài nguyên hiện có cần tận dụng
- Scene: Assets/Scenes/HospitalDay1.unity.
- Assets/Scripts/AAH_CheckInSequence.cs: state enum, các reference UI/interactable, event Action<string,int> OnPatientCheckedIn.
- Assets/Scripts/AAH_InteractableStep.cs: hiện tự đọc Legacy Input và khoảng cách từ transform; isCompleted không reset. Phải sửa thành dữ liệu/đích tương tác với reset theo từng bệnh nhân.
- Assets/Scripts/AAH_ReceptionDesk.cs: hệ spawn cũ đang active, có mảng 9 bệnh nhân và interval 15s; phải tránh hai hệ cùng spawn/ẩn NPC.
- Assets/Scripts/AAH_GameManager.cs: OnPatientInspected đang tăng tiến độ khám và có thể kết thúc ca sau 3 bệnh nhân. Không gọi hàm này khi chỉ check-in; tách check-in khỏi khám bất thường.
- Assets/Scripts/Editor/AAH_CheckInSetupTool.cs: đang đặt đồ theo vị trí Player, tạo đồ trùng, một số UI thiếu. Sửa công cụ thành idempotent, gắn đồ thật theo mapping cấu hình; không chạy phiên bản cũ nguyên trạng.
- Assets/AAH_AutoDoor.cs: kiểm tra phiên bản thực tế trước chỉnh; bổ sung API mở dùng chung nếu cần mà giữ E/range cửa reception.
- Assets/Models/Characters/Patient_CheckIn_Actor.prefab: kiểm tra thấy MeshRenderer/MeshFilter, chưa có Animator/rig/NavMeshAgent.
- Model nhân vật người dùng cung cấp: Mary, Mike, Noah, Brady, Clyde, Polly, Paws, Rusty, officer_dogman. Giữ Dr.Mupple là NPC bác sĩ trừ khi cấu hình rõ cho bệnh nhân. Kiểm tra scale/model trước chọn danh sách patient preset; tên hồ sơ tách khỏi tên model.
- Đồ thật: computer_reception, computer_screem_reception, keyboard, printer_01, camera_take_photo, ReceptionBell.
- door_02, door_03 và các door còn lại; bed_room_01…06 và bed_room_08 có trong scene. Transform các mesh nhập nằm tại origin: đặt interaction/hinge/bed marker theo bounds và geometry, không dựa transform.position.
- AI Navigation 2.0.15 đã cài, không cần cài lại. Không phát hiện component NavMesh trong scene; triangulation 0 vertices.

## Tạo mới nếu chưa có bản tương đương
- Assets/Scripts/AAH_PatientActor.cs: FSM Approaching → WaitingAtDesk → Dialogue → CheckingIn → Leaving → AtBed, references model con và NavMeshAgent.
- Assets/Scripts/AAH_PatientArrivalController.cs: danh sách preset, thứ tự và thời gian cấu hình; một người tiếp nhận mỗi lần, không spawn khi hết phòng.
- Assets/Scripts/AAH_CheckInInteractionController.cs: InputAction E và lựa chọn mục tiêu, khóa modal/busy.
- Assets/Scripts/AAH_RoomRegistry.cs: 6 RoomSlot với roomNumber, entry, door, bedApproach, bedPose và reservation owner. Reserve/occupy/release theo ID, đảm bảo idempotent.
- Assets/Scripts/AAH_PatientDoorAccess.cs nếu cần adapter cho door và tác nhân NPC, không dùng đồng thời nhiều script quay cùng cửa.
- Assets/Data/CheckIn/ patient presets và room mapping nếu dùng ScriptableObject; chỉ tạo loại dữ liệu phục vụ yêu cầu.
- Assets/Navigation/ dữ liệu bake; nhóm scene markers cho outside spawn, entrance, quầy, cửa và giường.
- Clipboard/giấy in prototype bằng primitive/mesh đơn giản nếu không có model phù hợp; không tạo thêm AI asset hoặc rig trong phạm vi này.
- Assets/UI/CheckIn/ material vòng, UI/font riêng khi cần; âm thanh placeholder ngắn local cho chuông, shutter, printer/beep.
- Assets/Tests/EditMode/ và Assets/Tests/PlayMode/ tests tương ứng theo assembly hiện có.

## Những lỗi hiện có cần xử lý
- NPC teleport vào quầy, Lerp xuyên vật cản rồi SetActive(false) thay vì tới giường.
- Chuông do người chơi gọi thay vì NPC tự bấm.
- Reset isCompleted, unsubscribe events, chặn ký sai trạng thái, chặn thao tác lặp.
- Capture hiện không dispose RenderTexture/Texture/Sprite và không restore render target cũ.
- Room random 1–6 chưa có occupancy/mapping.
- SetupTool tạo thiết bị trùng và có thể thiếu modal/dialogue/ảnh/progress wiring.

# Implementation Steps
1. **Kiểm tra và lập mapping scene** — Dependency: duyệt kế hoạch.
   - Đọc lại các script/prefab/scene liên quan; kiểm tra dependency/dependent bằng công cụ dependency nếu khả dụng, hoặc AssetDatabase.GetDependencies và rà references nếu công cụ không có.
   - Ghi nhận thay đổi scene chưa lưu, không ghi đè tùy tiện. Kiểm tra reference bằng SerializedObject (JSON instanceID=0 không đủ kết luận null).
   - Mapping 6 phòng tới cửa và giường theo geometry; marker ở phía đúng của quầy, ngoài cửa, tại ngưỡng cửa và cạnh giường. Kiểm tra ánh xạ trực quan trước tiếp tục.
2. **Nền state và tiếp nhận một NPC** — Dependency: 1.
   - Refactor AAH_CheckInSequence, AAH_InteractableStep; tạo PatientActor/ArrivalController và patient preset.
   - Một transaction cho mỗi bệnh nhân, busy flag/generation token, reset step state, lifecycle cancellation và unsubscribe.
   - Tắt luồng arrival cũ trên ReceptionDeskManager trong scene hoặc chuyển nó sang adapter; giữ các hệ khám khác không thay đổi.
   - Bỏ việc gọi OnPatientInspected ở check-in; event hoàn tất giữ signature hiện có và bổ sung patient ID qua dữ liệu context nếu cần.
3. **RoomRegistry và hạn mức sáu phòng** — Dependency: 1–2.
   - Cấu hình đúng sáu RoomSlot, reserve trước cấp giấy; giấy, sự kiện và NPC dùng cùng room ID.
   - Không reserve lại khi E lặp; giữ chỗ khi đi tới phòng; release khi transaction hủy trước check-in, không release tự động bệnh nhân đang nằm.
   - Kiểm tra hết phòng và API ReleaseRoom; không thêm xuất viện tự động.
4. **NavMesh và cửa** — Dependency: 1–3.
   - Tạo NavMeshSurface bake hành lang, ngoài cửa, lối vào, quầy và sáu phòng; agent size theo bounds model/scaling thực tế, không default mù.
   - Loại NPC, ring và cánh cửa động khỏi bake thích hợp; tường/quầy/giường là chướng ngại.
   - Bố trí obstacle hoặc link cửa nếu thực sự cần; mở cửa trước khi NPC đi qua, đợi đủ clearance. Không force-warp qua tường nếu path fail.
   - NavMeshAgent sở hữu chuyển động root; model con thực hiện mô phỏng bấm chuông/trình clipboard/tư thế nằm không rig.
   - Validate path complete từ spawn→quầy và quầy→tất cả bedApproach, có timeout/retry và log bệnh nhân/phòng khi bị kẹt.
5. **Tương tác và vòng xanh trên đồ thật** — Dependency: 2, 4.
   - Gắn marker/collider mục tiêu cho clipboard, camera_take_photo, computer/keyboard, printer_01; wire ReceptionBell cho NPC arrival, không yêu cầu người chơi gọi.
   - Tạo InteractionController event-driven New Input System; khóa khi modal, progress, photo development, print và pause.
   - Vòng rỗng sáng xanh chỉ hiện ở bước hợp lệ; không tạo máy móc thứ hai.
6. **UI giấy tờ và thoại** — Dependency: 2, 5.
   - Sửa SetupTool idempotent, UI hierarchy hiện có; bổ sung Dialogue, Clipboard, button ký/Escape, cursor/player-state restoration.
   - Profile của NPC hiện tại quyết định tên/thoại/hồ sơ, không dùng mãi Mai Yamada.
   - Kiểm tra TMP glyph tiếng Việt, không dùng font chỉ chứa chuỗi prompt cửa cho toàn bộ hồ sơ.
7. **Chụp ảnh và Polaroid** — Dependency: 5–6.
   - Photo camera riêng vô hiệu render liên tục, bố trí trước NPC đúng framing; chụp snapshot duy nhất bằng RenderTexture và texture/sprite cho HUD.
   - Restore RenderTexture.active và camera.targetTexture trong finally; dispose RT và ảnh cũ đúng lúc. Không dùng live RT làm ảnh lưu vì NPC sẽ rời đi.
   - Flash ngắn và shutter placeholder, phát triển ảnh từ đen tới rõ trong khoảng 3 s; máy tính chỉ mở sau khi xong. HUD photo giữ khi đi lại.
8. **Đăng ký, in giấy và dispatch tới giường** — Dependency: 3–7.
   - REGISTERING + fill progress, REGISTERED; khóa re-entry. Nhấn E máy in tạo giấy khớp reservation.
   - Phát event một lần; NPC Leaving tới assigned bed qua cửa tự mở, tắt agent lúc lên giường và giữ model nằm tại bedPose với bounds không xuyên nệm/tường.
   - Quầy trống mới tiếp nhận NPC tiếp theo; phòng vẫn occupied và bệnh nhân vẫn tồn tại trong scene.
9. **Kiểm thử tích hợp và bàn giao** — Dependency: 1–8.
   - Run EditMode/PlayMode tests, kiểm tra trực quan từ PlayerCamera ở quầy và cả sáu phòng.
   - Dọn runner/tài nguyên test tạm, lưu scene/prefab/dữ liệu cần thiết; báo rõ animation chỉ prototype, chưa có rig thật.

# Verification & Testing
- EditMode: state transition đúng thứ tự; không ký khi chưa tới clipboard; ResetForPatient mở lại đủ bước; reserve/occupy/release không trùng, room ID 1–6 đúng mapping, room thứ 7 bị từ chối.
- EditMode: validation báo thiếu model/marker/UI/cửa/giường; SerializedObject xác nhận tất cả wiring, scene chỉ có một nguồn arrival và một EventSystem.
- PlayMode: NPC tự đi từ ngoài cửa tới quầy, bấm chuông đúng một lần, thoại xong mới mở clipboard; player không cần gọi NPC.
- PlayMode: nhìn/range đúng mới E hoạt động; nhấn E nhanh không duplicate camera capture/register/print/event; quay lưng không tương tác; tương tác cửa không đồng thời kích hoạt máy ở xa.
- PlayMode: modal khóa điều khiển và phục hồi sau ký/Escape; pause không tự tiến progress; disable/destroy sequence hủy coroutine và không để UI/cursor kẹt.
- PlayMode: ảnh ban đầu đen, sau ~3 s thấy đúng NPC, vẫn cố định khi đi lại; ảnh không biến thành ảnh NPC kế tiếp trước lần chụp mới. Đo tài nguyên texture/RT qua nhiều lượt để tránh leak.
- PlayMode: snapshot, giấy phòng, event và NPC tới giường thống nhất cùng patient/room; kiểm thử cả sáu phòng, không xuyên cửa/tường, path complete.
- PlayMode: liên tiếp ít nhất hai NPC chứng minh step completion reset; sáu NPC có sáu giường riêng, NPC tiếp theo không được tiếp nhận khi hết phòng; ReleaseRoom thủ công qua test cho phép tiếp tục.
- Regression: FPS movement, E mở door_reception phạm vi 7 m và bản lề trái không bị phá; hệ khám/anomaly không tăng đếm chỉ vì check-in và không kết thúc ca sau ba lần đăng ký.
- Visual: chụp Game View trong PlayMode để xác nhận vòng xanh rỗng, UI đọc được, thanh progress có fill, Polaroid không che mục tiêu, NPC/clipboard/giường đúng vị trí. Scene View đơn thuần không thay thế test UI runtime.
- Build: kiểm tra compile và Windows Development Build nếu môi trường cho phép; không sửa package source hoặc compatibility defines. Báo rõ phần nào chưa được chạy thực tế.

## Phạm vi được người dùng chọn
- Từng NPC một; không crowd/wave đồng thời trong phiên bản này.
- Chờ phòng trống khi đủ sáu phòng; chưa có xuất viện tự động.
- Prototype không rig, không hứa animation tay/chân tự nhiên.
- NavMesh được chọn thay vì waypoint Lerp: cần bake/mapping nhưng tránh đường thẳng xuyên tường; waypoint-only ít setup hơn nhưng không đáp ứng tốt nhiều phòng/cửa.
- Không thêm cloud/backend, voice AI, rig/animation generation hoặc model mới ngoài clipboard/giấy placeholder cần thiết.

# Implementation verification report
- Đã cấu hình NPC queue, RoomRegistry sáu slot, NavMesh bake, cửa NPC, đồ tương tác thật, UI hồ sơ/đăng ký/giấy in, ảnh snapshot 512x512 và audio placeholder.
- door_03 chứa hai cánh cửa rời trong cùng mesh: tạo hai mesh scene asset door_03_Room1/Room3, giữ nguyên source nhập và vô hiệu renderer/collider gộp trong scene.
- EditMode: cả 6 đường từ quầy tới entry và entry tới bedApproach đều PathComplete; reserve 6 phòng độc nhất, từ chối phòng thứ 7, release rồi reserve lại; reset step sau hoàn tất thành công.
- PlayMode run 1: sáu NPC hoàn thành toàn bộ chuỗi bằng E tập trung, tới sáu giường, FreeCount=0 và NPC kế tiếp bị chặn bởi capacity.
- PlayMode run 2: handover clipboard, ảnh chính diện có mặt bệnh nhân, ảnh phát triển 3 giây, đúng một OnPatientCheckedIn, có một AudioListener. Camera test chụp giường ban đầu vượt trần, nên ảnh giường đó không được chấp nhận để xác minh trực quan.
- PlayMode run 3: kiểm tra alignment/fit và screenshot từ trong phòng cho cả sáu giường; đủ sáu bệnh nhân nằm ngang trên mặt nệm, không vượt bounds giường.
- Đã xem screenshot hồ sơ với nút ký, Polaroid, chính diện NPC và sáu tư thế nằm; đã loại bỏ toàn bộ runner/screenshot tạm.
- Final scene: đúng một EventSystem và AudioListener, FPS/CharacterController phục hồi enabled/canMove; door_reception giữ range 7 m, góc -90 độ; scene đã lưu, Editor ngoài PlayMode.
- Chưa chạy Windows standalone Development Build; chưa nghe/đánh giá âm thanh bằng tai. Audio là placeholder, thoại là text không có lồng tiếng.
- Animation không rig: NPC di chuyển theo NavMesh, mô phỏng chuông/đưa clipboard và quay model nằm. Không có locomotion/gesture skeletal animation tự nhiên. Chưa có xuất viện tự động.
