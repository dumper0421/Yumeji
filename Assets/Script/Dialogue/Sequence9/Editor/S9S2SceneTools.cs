using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// [9-2] 상영 중 씬을 한 번에 만들어 주는 메뉴 도구.
/// 유니티 상단 메뉴 → Tools → 유메지 [9-2 상영 중] 에서 사용한다.
///
/// 시퀀스 9 전용 맵 리소스가 아직 없어서, 기획서가 "변형해서 쓴다"고 한 원본 공간을
/// 각 씬에서 그대로 복사해 온다. (맵 리소스가 나오면 해당 Room_ 그룹의 배경만 교체하면 된다)
///   사진관 로비/암실 ← Sequence2S#1 (Guest Area / Work Room)
///   하루의 집        ← Sequence2S#4 (BG_haruRoom_v2)
///   반복되는 복도    ← Sequence2S#4 (BG_haruApartmentLobby_v1, 현관문 4개)
///   영화관 로비/스크린 ← Sequence2S#2 (BG_boxOffice_v2 / BG_theater_v1)
///   텔레비전 공간    ← Sequence1S#1 (BG_LostRoom_v1)
///
/// 베이스는 Sequence8S#2(매니저/대화창/하루/케이가 이미 연결된 씬)를 복사한 뒤
/// 8-2 전용 오브젝트를 걷어내고 쓴다.
///
/// 복사해 온 맵 오브젝트의 게임 스크립트(대화/상호작용/텔레포트 등)는 원래 씬 전용이라 모두 떼어내고,
/// 스프라이트·벽 콜라이더·카메라 경계만 남긴다.
/// </summary>
public static class S9S2SceneTools
{
    private const string Tag = "[9-2 상영 중]";

    private const string BaseScenePath = "Assets/Scenes/2026PlayX4/Sequence8S#2.unity";
    private const string TargetScenePath = "Assets/Scenes/2026PlayX4/Sequence9S#2.unity";
    private const string TargetSceneName = "Sequence9S#2";

    private const string PhotoScenePath = "Assets/Scenes/2026PlayX4/Sequence2S#1.unity";
    private const string HouseScenePath = "Assets/Scenes/2026PlayX4/Sequence2S#4.unity";
    private const string CinemaScenePath = "Assets/Scenes/2026PlayX4/Sequence2S#2.unity";
    private const string TvScenePath = "Assets/Scenes/2026PlayX4/Sequence1S#1.unity";

    private const string LoaderPath = "Assets/Script/Dialogue/DialogueLoader.asset";
    private const string SparklePrefabPath = "Assets/Prefab/VFX/VFX_interactionlight.prefab";
    private const string SootSpritePath = "Assets/Sprites/Object/Sequence7/OBJ_blackSilhouette_v1.png";
    private const string DeskSpritePath = "Assets/Sprites/Object/Sequence2/Scene4/OBJ_haruDesk_v1.png";
    private const string GunshotPath = "Assets/Resources/SFX/SFX_Shooting.mp3";
    private const string PhoneRingPath = "Assets/Sound/Sequence2/S#4/SFX_전화벨.mp3";

    // 기획서 사운드 표기 이름. 파일이 프로젝트에 들어오면 '리소스 다시 연결'로 잡힌다.
    private const string BgmName = "BGM_9-2";
    private const string JumpScareName = "SFX_ghostJumpScare";
    private const string TvOffName = "SFX_TvOff";

    private static readonly string[] FallbackSlidePaths =
    {
        "Assets/Sprites/illust/ILL_haruToiletMirrorPopup_v1.png",
        "Assets/Sprites/illust/ILL_charred _photograph_V1.png",
        "Assets/Resources/DialogueImages/SEQ8_FilmSet.png",
        "Assets/Resources/DialogueImages/SEQ8_StatueMatch.png",
    };

    // 공간끼리 겹치지 않도록 원본 좌표에 더하는 오프셋
    private static readonly Vector3 PhotoOffset = new Vector3(0f, 0f, 0f);
    private static readonly Vector3 HouseOffset = new Vector3(300f, 0f, 0f);
    private static readonly Vector3 CinemaOffset = new Vector3(600f, 0f, 0f);
    private static readonly Vector3 CorridorOffset = new Vector3(900f, 0f, 0f);
    private static readonly Vector3 TvOffset = new Vector3(1200f, 0f, 0f);

    // 기획: 스크린 사이즈 21x15 (인게임 뷰포트와 동일)
    private const float ScreenWidth = 21f;
    private const float ScreenHeight = 15f;

    private const int NoPassLayer = 6;

    private class Ctx
    {
        public readonly List<string> Report = new List<string>();

        // 원본 맵 연출을 그대로 살린 이동 트리거 (사진관 로비 ↔ 암실). SceneController는 마지막에 연결한다.
        public readonly List<TeleportPoint> KeptTeleports = new List<TeleportPoint>();

        // 9-2 동선에 맞지 않아 지운 원본 이동 트리거의 위치. 같은 자리에 9-2 문을 놓을 때 쓴다.
        public readonly Dictionary<string, Vector3> RemovedTeleports = new Dictionary<string, Vector3>();
        public Scene Scene;
        public Transform Haru;
        public TMP_FontAsset Font;
    }

    // ================================================================ 메뉴

    [MenuItem("Tools/유메지 [9-2 상영 중]/씬 생성 (Sequence9S#2)", false, 1)]
    public static void BuildSceneMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) != null)
        {
            bool ok = EditorUtility.DisplayDialog(
                "9-2 상영 중",
                $"{TargetScenePath} 가 이미 있습니다.\n지우고 처음부터 다시 만들까요? (씬에서 직접 옮긴 위치는 사라집니다)",
                "다시 만들기",
                "취소"
            );

            if (!ok)
                return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        BuildScene();
    }

    [MenuItem("Tools/유메지 [9-2 상영 중]/리소스 다시 연결 (BGM·SFX·일러스트)", false, 10)]
    public static void RelinkMenu()
    {
        var controller = Object.FindObjectOfType<Sequence9Scene2DialogueController>(true);

        if (controller == null)
        {
            Debug.LogWarning($"{Tag} 열린 씬에 9-2 컨트롤러가 없다. {TargetSceneName} 씬을 열고 실행할 것.");
            return;
        }

        var ctx = new Ctx();
        LinkResources(new SerializedObject(controller), ctx);

        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log($"{Tag} 리소스 연결\n - " + string.Join("\n - ", ctx.Report));
    }

    [MenuItem("Tools/유메지 [9-2 상영 중]/연결 상태 검사", false, 20)]
    public static void Validate()
    {
        var controller = Object.FindObjectOfType<Sequence9Scene2DialogueController>(true);

        if (controller == null)
        {
            Debug.LogWarning($"{Tag} 열린 씬에 9-2 컨트롤러가 없다.");
            return;
        }

        var so = new SerializedObject(controller);
        var empty = new List<string>();
        SerializedProperty p = so.GetIterator();
        bool enter = true;

        while (p.NextVisible(enter))
        {
            enter = false;

            if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null)
                empty.Add(p.displayName);
        }

        if (empty.Count == 0)
            Debug.Log($"{Tag} 비어 있는 참조 없음.");
        else
            Debug.LogWarning(
                $"{Tag} 비어 있는 항목 (리소스 대기 중인 것은 임시 표시로 진행된다):\n - " + string.Join("\n - ", empty)
            );
    }

    /// <summary>배치모드용: Unity.exe -batchmode -executeMethod S9S2SceneTools.BuildFromCommandLine</summary>
    public static void BuildFromCommandLine()
    {
        bool ok = false;

        try
        {
            ok = BuildScene();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }

        EditorApplication.Exit(ok ? 0 : 1);
    }

    // ================================================================ 생성

    private static bool BuildScene()
    {
        var ctx = new Ctx();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BaseScenePath) == null)
        {
            Debug.LogError($"{Tag} 베이스 씬이 없다: {BaseScenePath}");
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) != null)
            AssetDatabase.DeleteAsset(TargetScenePath);

        if (!AssetDatabase.CopyAsset(BaseScenePath, TargetScenePath))
        {
            Debug.LogError($"{Tag} 베이스 씬 복사 실패");
            return false;
        }

        ctx.Scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        ctx.Font = FindFont();

        var haruMove = Object.FindObjectOfType<PlayerMove_Test_Lerp>(true);
        ctx.Haru = haruMove != null ? haruMove.transform : null;

        if (ctx.Haru == null)
        {
            Debug.LogError($"{Tag} 베이스 씬에서 하루를 못 찾았다.");
            return false;
        }

        StripEightTwo(ctx);

        // ---------- 맵 복사 ----------
        Transform mapRoot = EnsureRoot("=====Map=====", ctx).transform;

        Vector3 photoHaru;
        var photo = ImportRoom(
            ctx,
            PhotoScenePath,
            "Room_PhotoStudio",
            PhotoOffset,
            new[] { "=====Map=====/Guest Area", "=====Map=====/Work Room" },
            new[] { "Guest Area Virtual Camera", "Work Room Virtual Camera" },
            true,
            mapRoot,
            out photoHaru
        );

        Vector3 houseHaru;
        var house = ImportRoom(
            ctx,
            HouseScenePath,
            "Room_House",
            HouseOffset,
            new[] { "=====Map=====/BG_haruRoom_v2", "=====Map=====/Haru_Room_Colider" },
            new[] { "haruRoom_Virtual Camera" },
            false,
            mapRoot,
            out houseHaru
        );

        var corridor = ImportRoom(
            ctx,
            HouseScenePath,
            "Room_Corridor",
            CorridorOffset,
            new[] { "=====Map=====/BG_haruApartmentLobby_v1", "Apt_Colider" },
            new[] { "Apt_Virtual Camera" },
            false,
            mapRoot,
            out _
        );

        var cinema = ImportRoom(
            ctx,
            CinemaScenePath,
            "Room_Cinema",
            CinemaOffset,
            new[] { "=====Map=====/BG_boxOffice_v2", "=====Map=====/BG_theater_v1" },
            new[] { "Box Office Virtual Camera", "Theather Virtual Camera" },
            false,
            mapRoot,
            out _
        );

        Vector3 tvHaru;
        var tv = ImportRoom(
            ctx,
            TvScenePath,
            "Room_TV",
            TvOffset,
            new[] { "=====Map=====/BG_LostRoom_v1", "=====Map=====/ANI_television_v1_0" },
            new[] { "Virtual Camera" },
            false,
            mapRoot,
            out tvHaru
        );

        if (photo == null || house == null || corridor == null || cinema == null || tv == null)
        {
            Debug.LogError($"{Tag} 맵 복사 실패\n - " + string.Join("\n - ", ctx.Report));
            return false;
        }

        // 이동한 맵 콜라이더를 2D 물리에 반영해야 빈 칸 탐색이 맞는다
        Physics2D.SyncTransforms();

        // ---------- 연출 그룹 ----------
        Transform root = EnsureRoot("=====S9S2=====", ctx).transform;
        Transform points = EnsureChild(root, "Points");
        Transform doors = EnsureChild(root, "Doors");

        // ===== 5-1 변형된 사진관 =====
        Bounds lobbyBg = SpriteBounds(photo, "Guest Area/Background");
        Bounds darkBg = SpriteBounds(photo, "Work Room/bg_workroom");

        Vector3 lobbyOrigin = lobbyBg.Contains(new Vector3(photoHaru.x, photoHaru.y, lobbyBg.center.z))
            ? photoHaru
            : lobbyBg.center;

        Transform photoLobbySpawn = MakePoint(points, "PhotoLobbySpawn", FreeSpot(lobbyOrigin, lobbyBg));

        // 로비 ↔ 암실은 원본(2-1)의 TeleportPoint1~3 + ChangeLightTrigger를 그대로 쓴다 (ImportRoom에서 살려둠).
        // 암실 도착 지점(세이브 복원용)도 원본 텔레포트의 목적지를 따른다.
        TeleportPoint toDarkroom = ctx.KeptTeleports.FirstOrDefault(
            tp => tp != null && tp.transform.parent != null && tp.transform.parent.name == "Guest Area"
        );
        Vector3 darkroomPos = toDarkroom != null
            ? toDarkroom.TargetPoint
            : FreeSpot(SnapTo(lobbyOrigin, darkBg.center - new Vector3(0f, darkBg.size.y * 0.25f, 0f)), darkBg);
        Transform darkroomSpawn = MakePoint(points, "DarkroomSpawn", darkroomPos);

        // 기존 암실 선반(필름 보관함) 자리가 변형된 집으로 이어지는 열린 문이 된다
        S9S2Door shelfDoor = AttachDoor(ctx, Find(photo, "Work Room/Objects/obj_filmstoragebox"), S9S2Door.DoorKind.ShelfToHouse);
        if (shelfDoor != null)
            SetDoorOpenVisual(ctx, shelfDoor, alwaysOpen: true);

        // 암실의 사진 벽 (건조 중인 사진들 자리)
        Transform hangingFilms = Find(photo, "Work Room/Objects/OBJ_hangingFilms_v1");
        Vector3 photoWallPos = hangingFilms != null ? hangingFilms.position : darkBg.center;
        S9S2Interactable photoWall = MakeInteractable(
            ctx,
            root,
            "PhotoWall",
            photoWallPos,
            new Vector2(2f, 1f),
            S9S2Interactable.InteractKind.PhotoWall
        );

        // ===== 5-2 변형된 집 =====
        Bounds houseBg = SpriteBounds(house, "BG_haruRoom_v2");
        Vector3 houseOrigin = houseBg.Contains(new Vector3(houseHaru.x, houseHaru.y, houseBg.center.z))
            ? houseHaru
            : houseBg.center;
        Transform houseSpawn = MakePoint(points, "HouseSpawn", FreeSpot(houseOrigin, houseBg));

        S9S2Interactable telephone = AttachInteractable(
            ctx,
            Find(house, "BG_haruRoom_v2/OBJ_telephone_v1"),
            S9S2Interactable.InteractKind.Telephone
        );

        S9S2Door wardrobe = AttachDoor(ctx, Find(house, "BG_haruRoom_v2/OBJ_haruWardrobe_v1"), S9S2Door.DoorKind.Wardrobe);
        if (wardrobe != null)
            SetDoorOpenVisual(ctx, wardrobe, alwaysOpen: false);

        // ===== 5-4 변형된 영화관 로비 =====
        Bounds cinemaBg = SpriteBounds(cinema, "BG_boxOffice_v2");
        Transform cinemaLobbySpawn = MakePoint(points, "CinemaLobbySpawn", FreeSpot(cinemaBg.center, cinemaBg));

        // 오른쪽 상단의 문 = 원본(2-2)에서 매표소 → 극장으로 넘어가던 TeleportPoint 자리.
        // 원본 텔레포트는 스크린 연출을 건너뛰어 버리므로 지우고, 같은 자리에 9-2 문을 둔다.
        Transform entrance = Find(cinema, "BG_boxOffice_v2/EntranceWall");
        Vector3 entranceGuess;
        if (ctx.RemovedTeleports.TryGetValue("TeleportPoint_boxOffice (1)", out Vector3 origTp))
            entranceGuess = origTp;
        else if (entrance != null)
            entranceGuess = entrance.position;
        else
            entranceGuess = new Vector3(cinemaBg.max.x - cinemaBg.size.x * 0.2f, cinemaBg.max.y - cinemaBg.size.y * 0.15f, 0f);

        Vector3 entrancePos = SnapTo(cinemaLobbySpawn.position, entranceGuess);
        MakeDoor(ctx, doors, "Door_CinemaToScreen", entrancePos, S9S2Door.DoorKind.CinemaToScreen, true);

        // ===== 5-5 스크린 공간 =====
        Bounds theaterBg = SpriteBounds(cinema, "BG_theater_v1");
        Transform topWall = Find(cinema, "BG_theater_v1/TopWall");
        float topY = topWall != null ? topWall.position.y : theaterBg.max.y - 3f;

        Vector3 standGuess = SnapTo(cinemaLobbySpawn.position, new Vector3(theaterBg.center.x, topY - 1f, 0f));
        Vector3 stand = FreeSpot(standGuess, theaterBg, preferDown: true);
        Transform screenStand = MakePoint(points, "ScreenStandPoint", stand);
        Transform screenRoomSpawn = MakePoint(
            points,
            "ScreenRoomSpawn",
            FreeSpot(stand + new Vector3(0f, -5f, 0f), theaterBg, preferDown: true)
        );

        S9S2Door screenDoor = MakeDoor(ctx, doors, "Door_Screen", stand + Vector3.up, S9S2Door.DoorKind.ScreenDoor, true);

        Vector3 screenCenter = new Vector3(stand.x, stand.y + 1.5f + ScreenHeight * 0.5f, 0f);
        MakeScreen(ctx, root, screenCenter, out RawImage screenImage, out TMP_Text screenLabel, out VideoPlayer screenVideo);

        // ===== 5-6 반복되는 복도 =====
        Transform lobbyBgTr = Find(corridor, "BG_haruApartmentLobby_v1");
        var frontDoors = new List<Transform>();
        if (lobbyBgTr != null)
        {
            foreach (Transform child in lobbyBgTr)
            {
                if (child.name.StartsWith("FrontDoor"))
                    frontDoors.Add(child);
            }
        }

        frontDoors.Sort((a, b) => a.position.x.CompareTo(b.position.x));

        Bounds corridorBg = SpriteBounds(corridor, "BG_haruApartmentLobby_v1");
        var corridorDoors = new List<S9S2Door>();
        string[] plates = { "401", "403", "404", "405" };

        for (int i = 0; i < plates.Length; i++)
        {
            Vector3 pos = i < frontDoors.Count
                ? frontDoors[i].position
                : new Vector3(
                    corridorBg.min.x + corridorBg.size.x * (0.15f + 0.23f * i),
                    corridorBg.max.y - 3f,
                    0f
                );

            if (i >= frontDoors.Count)
                ctx.Report.Add($"⚠ 복도 현관문 {i}번을 원본에서 못 찾아 배경 비율로 배치");

            S9S2Door d = MakeDoor(ctx, doors, $"Door_Corridor_{plates[i]}", pos, S9S2Door.DoorKind.Corridor, false);
            d.CorridorIndex = i;
            d.OriginalPlate = plates[i];
            MakePlate(ctx, d, plates[i]);
            SetDoorOpenVisual(ctx, d, alwaysOpen: false);
            corridorDoors.Add(d);
        }

        Vector3 door0 = corridorDoors[0].transform.position;
        Vector3 door2 = corridorDoors[2].transform.position;

        Transform corridorSpawn = MakePoint(
            points,
            "CorridorSpawn",
            FreeSpot(door0 + new Vector3(0f, -2f, 0f), corridorBg, preferDown: true)
        );
        // 케이를 맞으며 한 걸음 물러날 뒤 칸까지 비어 있는 자리를 고른다 (복도 바닥이 3칸 남짓이라 아래 끝은 안 된다)
        Vector3 finalPos = FreeSpot(door2 + new Vector3(0f, -4f, 0f), corridorBg, preferDown: true);
        if (!IsFree(finalPos + Vector3.down) && IsFree(finalPos + Vector3.up) && finalPos.y + 1f < door2.y - 1f)
            finalPos += Vector3.up;

        Transform corridorFinal = MakePoint(points, "CorridorFinalPoint", finalPos);

        // 벽에 붙은 탁자 (4-2의 작아진 책상 재사용) + 권총
        Vector3 door1 = corridorDoors[1].transform.position;
        Vector3 tablePos = SnapTo(door0, new Vector3((door0.x + door1.x) * 0.5f, door0.y - 1f, 0f));
        MakePistolTable(ctx, root, tablePos);
        S9S2Interactable pistol = MakeInteractable(
            ctx,
            root,
            "Pistol",
            tablePos,
            new Vector2(1f, 1f),
            S9S2Interactable.InteractKind.Pistol
        );
        MakeLabel(ctx, pistol.transform, "[임시] 권총", new Vector3(0f, 0.6f, 0f), 2.2f, Color.white);
        pistol.gameObject.SetActive(false);

        GameObject soot = MakeSprite(ctx, root, "Soot", corridorFinal.position, LoadAsset<Sprite>(SootSpritePath, ctx), 5);
        soot.SetActive(false);

        // 케이는 8-2 베이스에 이미 애니메이터까지 붙어 있다
        Transform kei = FindDeepInScene("Kei");
        if (kei != null)
        {
            kei.position = new Vector3(door2.x, door2.y, kei.position.z);
            kei.gameObject.SetActive(false);
        }
        else
        {
            ctx.Report.Add("⚠ 케이를 못 찾았다");
        }

        // ===== 5-7 텔레비전 공간 =====
        Transform tvSpawn = MakePoint(points, "TvRoomSpawn", tvHaru);
        Transform tvObj = Find(tv, "ANI_television_v1_0");

        // ---------- 카메라 ----------
        CinemachineVirtualCamera lobbyCam = FindVcam(photo, "Guest Area Virtual Camera");
        CinemachineVirtualCamera darkCam = FindVcam(photo, "Work Room Virtual Camera");
        CinemachineVirtualCamera houseCam = FindVcam(house, "haruRoom_Virtual Camera");
        CinemachineVirtualCamera corridorCam = FindVcam(corridor, "Apt_Virtual Camera");
        CinemachineVirtualCamera cinemaCam = FindVcam(cinema, "Box Office Virtual Camera");
        CinemachineVirtualCamera theaterCam = FindVcam(cinema, "Theather Virtual Camera");
        CinemachineVirtualCamera tvCam = FindVcam(tv, "Virtual Camera");
        CinemachineVirtualCamera screenCam = MakeScreenCamera(ctx, root, screenCenter);

        foreach (var cam in new[] { lobbyCam, darkCam, houseCam, corridorCam, cinemaCam, theaterCam, tvCam, screenCam })
        {
            if (cam != null)
                cam.gameObject.SetActive(cam == lobbyCam);
        }

        // ---------- 전체화면 일러스트 ----------
        MakeIllustrationCanvas(ctx, out Image illust, out TMP_Text illustLabel, out Image black);

        // ---------- 하루 시작 위치 ----------
        ctx.Haru.position = new Vector3(photoLobbySpawn.position.x, photoLobbySpawn.position.y, ctx.Haru.position.z);

        // ---------- 컨트롤러 ----------
        GameObject controllerObj = EnsureChild(root, "S9S2Controller").gameObject;
        var controller = controllerObj.AddComponent<Sequence9Scene2DialogueController>();

        foreach (S9S2Door d in Object.FindObjectsOfType<S9S2Door>(true))
            SetRef(d, "_controller", controller);

        foreach (S9S2Interactable it in Object.FindObjectsOfType<S9S2Interactable>(true))
            SetRef(it, "_controller", controller);

        var so = new SerializedObject(controller);

        SetObj(so, "dialogueManager", Object.FindObjectOfType<DialogueManager>(true), "대화 매니저", ctx);
        SetObj(so, "dialogueLoader", LoadAsset<DialogueLoader>(LoaderPath, ctx), "대화 로더", ctx);
        SetStr(so, "sceneId", "Sequence9Scene2");
        SetStr(so, "puzzleId", "S9S2");

        SetObj(so, "_playerMove", ctx.Haru.GetComponent<PlayerMove_Test_Lerp>(), "하루", ctx);
        SetObj(so, "_kei", kei != null ? kei.gameObject : null, "케이", ctx);
        SetObj(so, "_keiAnimator", kei != null ? kei.GetComponentInChildren<Animator>(true) : null, "케이 애니메이터", ctx);

        SetObj(so, "_photoLobbySpawn", photoLobbySpawn, null, ctx);
        SetObj(so, "_darkroomSpawn", darkroomSpawn, null, ctx);
        SetObj(so, "_houseSpawn", houseSpawn, null, ctx);
        SetObj(so, "_cinemaLobbySpawn", cinemaLobbySpawn, null, ctx);
        SetObj(so, "_screenRoomSpawn", screenRoomSpawn, null, ctx);
        SetObj(so, "_screenStandPoint", screenStand, null, ctx);
        SetObj(so, "_corridorSpawn", corridorSpawn, null, ctx);
        SetObj(so, "_corridorFinalPoint", corridorFinal, null, ctx);
        SetObj(so, "_tvRoomSpawn", tvSpawn, null, ctx);

        SetObj(so, "_photoLobbyCam", lobbyCam, "사진관 로비 카메라", ctx);
        SetObj(so, "_darkroomCam", darkCam, "암실 카메라", ctx);
        SetObj(so, "_houseCam", houseCam, "집 카메라", ctx);
        SetObj(so, "_cinemaLobbyCam", cinemaCam, "영화관 로비 카메라", ctx);
        SetObj(so, "_screenRoomCam", theaterCam, "스크린 공간 카메라", ctx);
        SetObj(so, "_screenCloseupCam", screenCam, "스크린 틸트 카메라", ctx);
        SetObj(so, "_corridorCam", corridorCam, "복도 카메라", ctx);
        SetObj(so, "_tvRoomCam", tvCam, "텔레비전 공간 카메라", ctx);

        SetObj(so, "_wardrobeDoor", wardrobe, "옷장 문", ctx);
        SetObj(so, "_screenDoor", screenDoor, "스크린 문", ctx);
        SetArray(so, "_corridorDoors", corridorDoors.Cast<Object>().ToArray());

        SetObj(so, "_photoWall", photoWall, "사진 벽", ctx);
        SetObj(so, "_telephone", telephone, "전화기", ctx);
        SetObj(so, "_pistol", pistol, "권총", ctx);

        SetObj(so, "_illustrationImage", illust, null, ctx);
        SetObj(so, "_illustrationPlaceholder", illustLabel, null, ctx);
        SetObj(so, "_blackImage", black, null, ctx);
        SetObj(so, "_soot", soot, null, ctx);

        SetObj(so, "_screenVideoPlayer", screenVideo, null, ctx);
        SetObj(so, "_screenImage", screenImage, null, ctx);
        SetObj(so, "_screenPlaceholder", screenLabel, null, ctx);
        SetArray(
            so,
            "_screenFallbackSlides",
            FallbackSlidePaths.Select(p => (Object)AssetDatabase.LoadAssetAtPath<Sprite>(p)).Where(s => s != null).ToArray()
        );

        SetObj(so, "_tvTransform", tvObj, "텔레비전", ctx);
        SetObj(so, "_haruLight", ctx.Haru.GetComponent<Light2D>(), "하루 조명", ctx);

        LinkResources(so, ctx);

        so.ApplyModifiedPropertiesWithoutUndo();

        // 원본 사진관 TeleportPoint가 카메라를 바꿀 때 쓰는 SceneController (다른 씬과 같은 방식)
        WireSceneController(
            ctx,
            new[] { lobbyCam, darkCam, houseCam, corridorCam, cinemaCam, theaterCam, tvCam, screenCam }
        );

        // 8-2의 정원 색조를 이어받지 않도록 전역 조명을 기본값으로
        ResetGlobalLight(ctx);

        AddToBuildSettings(ctx);

        EditorSceneManager.MarkSceneDirty(ctx.Scene);
        EditorSceneManager.SaveScene(ctx.Scene);
        Selection.activeGameObject = controllerObj;

        Debug.Log($"{Tag} 씬 생성 완료: {TargetScenePath}\n - " + string.Join("\n - ", ctx.Report));
        return true;
    }

    private static void WireSceneController(Ctx ctx, CinemachineVirtualCamera[] cams)
    {
        Transform system = EnsureRoot("=====System=====", ctx).transform;
        Transform holder = EnsureChild(system, "SceneController");

        var sc = holder.GetComponent<Sequence9Scene2Controller>();
        if (sc == null)
            sc = holder.gameObject.AddComponent<Sequence9Scene2Controller>();

        var so = new SerializedObject(sc);
        SetObj(so, "Player", ctx.Haru.gameObject, "SceneController 하루", ctx);
        SetArray(so, "cinemachineCameras", cams.Where(c => c != null).Cast<Object>().ToArray());
        so.ApplyModifiedPropertiesWithoutUndo();

        foreach (TeleportPoint tp in ctx.KeptTeleports)
        {
            if (tp == null)
                continue;

            tp.controller = sc;
            EditorUtility.SetDirty(tp);
        }

        ctx.Report.Add($"Sequence9Scene2Controller 연결 (원본 텔레포트 {ctx.KeptTeleports.Count}개)");
    }

    // ================================================================ 8-2 정리

    private static void StripEightTwo(Ctx ctx)
    {
        foreach (var c in Object.FindObjectsOfType<Sequence8Scene2DialogueController>(true))
            Object.DestroyImmediate(c);

        foreach (var c in Object.FindObjectsOfType<S8S2CurtainTrigger>(true))
            Object.DestroyImmediate(c);

        foreach (GameObject go in ctx.Scene.GetRootGameObjects())
        {
            string n = go.name.Trim();

            if (
                n == "=====S8S2====="
                || n == "_gardenVolume"
                || n == "SEQ8_Garden_Tilemap_8-1"
                || n == "Kei Camera"
                || n == "Virtual Camera"
            )
            {
                ctx.Report.Add($"8-2 전용 '{n}' 제거");
                Object.DestroyImmediate(go);
            }
        }

        GameObject map = FindRoot("=====Map=====", ctx.Scene);
        if (map != null)
        {
            for (int i = map.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(map.transform.GetChild(i).gameObject);
        }

        Transform luna = FindDeepInScene("Luna");
        if (luna != null)
            Object.DestroyImmediate(luna.gameObject);

        Transform illust = FindDeepInScene("S8S2 Illustration Canvas");
        if (illust != null)
            Object.DestroyImmediate(illust.gameObject);
    }

    private static void ResetGlobalLight(Ctx ctx)
    {
        foreach (Light2D light in Object.FindObjectsOfType<Light2D>(true))
        {
            if (light.lightType != Light2D.LightType.Global || light.gameObject.scene != ctx.Scene)
                continue;

            light.intensity = 1f;
            light.color = Color.white;
        }
    }

    // ================================================================ 맵 복사

    /// <summary>
    /// 원본 씬을 추가로 열어 지정한 오브젝트들과 그 공간의 버추얼 카메라를 복사해 온다.
    /// 카메라 Confiner가 가리키던 경계 콜라이더는 복사본 안의 같은 경로로 다시 연결한다.
    /// </summary>
    private static Transform ImportRoom(
        Ctx ctx,
        string scenePath,
        string containerName,
        Vector3 offset,
        string[] rootPaths,
        string[] vcamNames,
        bool keepTeleports,
        Transform parent,
        out Vector3 haruPos
    )
    {
        haruPos = Vector3.zero;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            ctx.Report.Add($"⚠ 원본 씬 없음: {scenePath}");
            return null;
        }

        Scene src = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(ctx.Scene);

        var container = new GameObject(containerName);
        container.transform.SetParent(parent, false);

        // 원본 경로 → 복사본
        var copies = new Dictionary<string, Transform>();

        foreach (string path in rootPaths)
        {
            Transform srcTr = FindByPath(src, path);
            if (srcTr == null)
            {
                ctx.Report.Add($"⚠ {scenePath} 에서 '{path}' 를 못 찾음");
                continue;
            }

            copies[path] = CopyInto(srcTr, container.transform);
        }

        var vcamCopies = new List<Transform>();

        foreach (string camName in vcamNames)
        {
            Transform srcCam = FindByPath(src, camName);
            if (srcCam == null)
            {
                ctx.Report.Add($"⚠ {scenePath} 에서 카메라 '{camName}' 를 못 찾음");
                continue;
            }

            Transform camCopy = CopyInto(srcCam, container.transform);
            camCopy.gameObject.SetActive(true);
            vcamCopies.Add(camCopy);

            var srcConfiner = srcCam.GetComponent<CinemachineConfiner>();
            var dstConfiner = camCopy.GetComponent<CinemachineConfiner>();

            if (srcConfiner != null && dstConfiner != null)
            {
                Collider2D bound = srcConfiner.m_BoundingShape2D;
                dstConfiner.m_BoundingShape2D = bound != null ? RemapCollider(src, bound, copies) : null;

                if (bound != null && dstConfiner.m_BoundingShape2D == null)
                    ctx.Report.Add($"⚠ '{camName}' 의 카메라 경계 '{bound.name}' 를 복사본에서 못 찾아 비워둠");
            }

            var vcam = camCopy.GetComponent<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                vcam.Follow = ctx.Haru;
                vcam.LookAt = null;
            }
        }

        // 원본 씬의 하루 시작 위치 (공간 도착 지점 기준으로 쓴다)
        foreach (GameObject go in src.GetRootGameObjects())
        {
            var move = go.GetComponentInChildren<PlayerMove_Test_Lerp>(true);
            if (move != null)
            {
                haruPos = move.transform.position + offset;
                break;
            }
        }

        // 원본 씬이 열려 있는 동안에 해야 카메라 참조를 이름으로 찾을 수 있다
        HandleTeleports(ctx, container.transform, offset, vcamCopies, keepTeleports);

        EditorSceneManager.CloseScene(src, true);

        CleanImportedHierarchy(container.transform, ctx, keepTeleports);

        container.transform.position = offset;

        ctx.Report.Add($"'{containerName}' ← {System.IO.Path.GetFileNameWithoutExtension(scenePath)} ({copies.Count}개 + 카메라 {vcamCopies.Count}개)");
        return container.transform;
    }

    /// <summary>
    /// 원본 맵의 이동 트리거 처리.
    /// keep: 원본 연출(페이드/카메라 전환/하루 조명)을 그대로 쓰도록 목적지에 오프셋을 더하고,
    ///       원본 씬 카메라를 가리키던 참조를 복사해 온 카메라로 바꾼다. 원본 스토리 전용 동작은 끈다.
    /// 아니면: 원본 씬의 다른 공간(9-2 동선 밖)으로 보내는 트리거라 위치만 기록하고 지운다.
    /// </summary>
    private static void HandleTeleports(
        Ctx ctx,
        Transform container,
        Vector3 offset,
        List<Transform> vcamCopies,
        bool keep
    )
    {
        var camByName = new Dictionary<string, CinemachineVirtualCamera>();
        foreach (Transform t in vcamCopies)
        {
            var v = t.GetComponent<CinemachineVirtualCamera>();
            if (v != null)
                camByName[t.name] = v;
        }

        CinemachineVirtualCamera Remap(Object srcCam, string owner)
        {
            if (srcCam == null)
                return null;

            if (camByName.TryGetValue(srcCam.name, out CinemachineVirtualCamera found))
                return found;

            ctx.Report.Add($"⚠ '{owner}' 가 쓰던 카메라 '{srcCam.name}' 를 복사해 오지 않아 비워둠");
            return null;
        }

        foreach (TeleportPoint tp in container.GetComponentsInChildren<TeleportPoint>(true))
        {
            if (!keep)
            {
                ctx.RemovedTeleports[tp.name.Trim()] = tp.transform.position + offset;
                ctx.Report.Add($"  └ 9-2 동선 밖으로 보내는 원본 텔레포트 '{tp.name.Trim()}' 제거");
                Object.DestroyImmediate(tp.gameObject);
                continue;
            }

            tp.TargetPoint += offset;
            tp.cinemachine = Remap(tp.cinemachine, tp.name);
            tp.cinemachineBase = Remap(tp.cinemachineBase, tp.name);
            tp.controller = null; // 마지막에 9-2 SceneController로 연결
            tp.IsActive = true;
            ctx.KeptTeleports.Add(tp);
            ctx.Report.Add($"  └ 원본 텔레포트 유지: {PathOf(tp.transform)} → {tp.TargetPoint}");
        }

        foreach (FadeTeleportTrigger ft in container.GetComponentsInChildren<FadeTeleportTrigger>(true))
        {
            // 지금 9-2에서 살리는 공간(사진관)에는 없다. 동선 밖으로 보내는 트리거라 지운다.
            ctx.RemovedTeleports[ft.name.Trim()] = ft.transform.position + offset;
            ctx.Report.Add($"  └ 9-2 동선 밖으로 보내는 원본 텔레포트 '{ft.name.Trim()}' 제거");
            Object.DestroyImmediate(ft.gameObject);
        }

        if (!keep)
            return;

        // 2-1의 ChangeLightTrigger: 암실에 들어가면 하루 조명이 켜지고 로비로 나오면 꺼진다 (원본 연출).
        // 같은 컴포넌트에 붙은 2-1 스토리 동작(진상 손님 이동, 켄 대사)은 끈다.
        var haruLight = ctx.Haru.GetComponent<Light2D>();

        foreach (ChangeLightTrigger clt in container.GetComponentsInChildren<ChangeLightTrigger>(true))
        {
            clt.Light = haruLight;
            clt.CanLeave = false;
            clt.DialogueManager = null;

            var so = new SerializedObject(clt);
            SetBool(so, "_isGusetEntry", false);
            so.FindProperty("_BadCustomer").objectReferenceValue = null;
            so.FindProperty("_exitTrigger").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        if (haruLight == null)
            ctx.Report.Add("⚠ 하루의 Light2D를 못 찾아 암실 조명 연출이 빠진다");
    }

    private static void SetBool(SerializedObject so, string field, bool value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null)
            p.boolValue = value;
    }

    private static Transform CopyInto(Transform src, Transform container)
    {
        GameObject copy = Object.Instantiate(src.gameObject, src.position, src.rotation);
        copy.name = src.name;
        copy.transform.localScale = src.lossyScale;
        copy.transform.SetParent(container, true);
        return copy.transform;
    }

    private static Collider2D RemapCollider(Scene src, Collider2D bound, Dictionary<string, Transform> copies)
    {
        string boundPath = PathOf(bound.transform);

        foreach (var kv in copies)
        {
            if (boundPath == kv.Key)
                return kv.Value.GetComponent<Collider2D>();

            if (boundPath.StartsWith(kv.Key + "/"))
            {
                string rest = boundPath.Substring(kv.Key.Length + 1);
                Transform t = kv.Value.Find(rest);
                if (t != null)
                    return t.GetComponent<Collider2D>();
            }
        }

        return null;
    }

    /// <summary>
    /// 원본 씬 전용 게임 스크립트를 떼어낸다. 스프라이트 정렬용 스크립트, 렌더러, 콜라이더, 애니메이터,
    /// Cinemachine/URP 컴포넌트는 남긴다.
    /// </summary>
    private static void CleanImportedHierarchy(Transform root, Ctx ctx, bool keepTeleports)
    {
        int removed = 0;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

        var keep = new HashSet<string> { "LayerSetting", "LayerSetting_child", "LayerSetting_Picture", "SimpleLayerSorting", "CandeFlicker" };

        if (keepTeleports)
        {
            keep.Add(nameof(TeleportPoint));
            keep.Add(nameof(ChangeLightTrigger));
        }

        foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null)
                continue;

            System.Type type = mb.GetType();
            if (type.Assembly.GetName().Name != "Assembly-CSharp" || keep.Contains(type.Name))
                continue;

            Object.DestroyImmediate(mb);
            removed++;
        }

        // 전역 조명이 공간마다 겹치면 화면 전체 밝기가 꼬인다. 씬에 이미 있는 것 하나만 쓴다.
        foreach (Light2D light in root.GetComponentsInChildren<Light2D>(true))
        {
            if (light.lightType == Light2D.LightType.Global)
                Object.DestroyImmediate(light);
        }

        // 원본에서 효과음을 직접 틀던 소스(전화기 등)는 SoundManager로 대신한다
        foreach (AudioSource a in root.GetComponentsInChildren<AudioSource>(true))
            Object.DestroyImmediate(a);

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true).ToArray())
        {
            if (t == null)
                continue;

            // 원본 스토리 전용 트리거(DialoguePoint, 사진관 출구 ExitTrigger)와 누워 있는 하루(Haru_Lie)는
            // 9-2에서 쓰지 않는다. 스크립트만 떼면 빈 트리거가 남아 헷갈리니 통째로 지운다.
            // (원본 텔레포트는 HandleTeleports에서 이미 살리거나 지웠다. 이름만 남은 찌꺼기가 있으면 같이 정리)
            string n = t.name.Trim();
            bool leftoverTeleport =
                n.IndexOf("eleport", System.StringComparison.OrdinalIgnoreCase) >= 0
                && t.GetComponent<TeleportPoint>() == null
                && t.GetComponentInParent<TeleportPoint>() == null;

            if (n == "Haru_Lie" || n == "ExitTrigger" || n.StartsWith("DialoguePoint") || leftoverTeleport)
            {
                Object.DestroyImmediate(t.gameObject);
                ctx.Report.Add($"  └ 원본 전용 트리거 '{n}' 제거");
            }
        }

        // 원본 스크립트에 딸린 트리거용 Rigidbody2D가 남으면 벽이 밀려난다.
        // 단, 살려둔 TeleportPoint의 Kinematic Rigidbody2D는 남겨야 한다.
        // 하루에게는 Rigidbody2D가 없어서, 트리거 쪽에 있어야 OnTriggerEnter2D가 불린다.
        foreach (Rigidbody2D rb in root.GetComponentsInChildren<Rigidbody2D>(true))
        {
            if (rb.GetComponent<TeleportPoint>() != null)
                continue;

            Object.DestroyImmediate(rb);
        }

        if (removed > 0)
            ctx.Report.Add($"  └ {root.name}: 원본 전용 스크립트 {removed}개 제거");
    }

    // ================================================================ 오브젝트 생성

    private static Transform MakePoint(Transform parent, string name, Vector3 pos)
    {
        Transform t = EnsureChild(parent, name);
        t.position = new Vector3(pos.x, pos.y, 0f);
        return t;
    }

    private static S9S2Door MakeDoor(Ctx ctx, Transform parent, string name, Vector3 pos, S9S2Door.DoorKind kind, bool markerVisual)
    {
        var go = new GameObject(name);
        go.layer = NoPassLayer;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;

        var door = go.AddComponent<S9S2Door>();
        door.Kind = kind;

        if (markerVisual)
        {
            // 원본 배경에 문 그림이 없는 자리라 임시 문을 그려둔다
            GameObject visual = MakeRect(ctx, go.transform, "DoorVisual", Vector3.zero, new Vector2(1f, 1.6f), new Color(0.05f, 0.05f, 0.05f, 0.9f), 3);
            MakeLabel(ctx, visual.transform, "[임시] 문", new Vector3(0f, 1.1f, 0f), 2f, new Color(1f, 1f, 1f, 0.8f));
            SetRef(door, "_doorVisual", visual);
        }

        return door;
    }

    private static S9S2Door AttachDoor(Ctx ctx, Transform target, S9S2Door.DoorKind kind)
    {
        if (target == null)
        {
            ctx.Report.Add($"⚠ {kind} 문으로 쓸 원본 오브젝트를 못 찾았다");
            return null;
        }

        target.gameObject.layer = NoPassLayer;

        if (target.GetComponent<Collider2D>() == null)
            target.gameObject.AddComponent<BoxCollider2D>();

        var door = target.gameObject.AddComponent<S9S2Door>();
        door.Kind = kind;
        ctx.Report.Add($"'{target.name}' → {kind} 문");
        return door;
    }

    /// <summary>열린 문 표시 (검은 문틈). 옷장/복도 문은 상태에 따라 켜지고, 선반 문은 항상 열려 있다.</summary>
    private static void SetDoorOpenVisual(Ctx ctx, S9S2Door door, bool alwaysOpen)
    {
        Vector2 size = Vector2.one;
        var box = door.GetComponent<BoxCollider2D>();

        if (box != null)
            size = new Vector2(Mathf.Min(box.size.x, 2f) * 0.8f, Mathf.Min(box.size.y, 3f) * 0.8f);

        Vector3 center = box != null ? (Vector3)box.offset : Vector3.zero;
        GameObject open = MakeRect(ctx, door.transform, "OpenVisual", center, size, Color.black, 4);
        open.SetActive(alwaysOpen);
        SetRef(door, "_openVisual", open);
    }

    private static void MakePlate(Ctx ctx, S9S2Door door, string text)
    {
        TMP_Text plate = MakeLabel(ctx, door.transform, text, new Vector3(0f, 0.9f, 0f), 2.6f, Color.white);
        plate.name = "Plate";
        SetRef(door, "_plate", plate);
    }

    private static S9S2Interactable MakeInteractable(
        Ctx ctx,
        Transform parent,
        string name,
        Vector3 pos,
        Vector2 size,
        S9S2Interactable.InteractKind kind
    )
    {
        var go = new GameObject(name);
        go.layer = NoPassLayer;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;

        var it = go.AddComponent<S9S2Interactable>();
        it.Kind = kind;

        if (kind == S9S2Interactable.InteractKind.Pistol)
            MakeRect(ctx, go.transform, "PistolVisual", new Vector3(0f, 0.15f, 0f), new Vector2(0.6f, 0.25f), new Color(0.15f, 0.15f, 0.15f, 1f), 6);

        AddSparkle(ctx, it);
        return it;
    }

    private static S9S2Interactable AttachInteractable(Ctx ctx, Transform target, S9S2Interactable.InteractKind kind)
    {
        if (target == null)
        {
            ctx.Report.Add($"⚠ {kind} 로 쓸 원본 오브젝트를 못 찾았다");
            return null;
        }

        target.gameObject.layer = NoPassLayer;

        if (target.GetComponent<Collider2D>() == null)
            target.gameObject.AddComponent<BoxCollider2D>();

        var it = target.gameObject.AddComponent<S9S2Interactable>();
        it.Kind = kind;
        AddSparkle(ctx, it);
        ctx.Report.Add($"'{target.name}' → {kind}");
        return it;
    }

    private static void AddSparkle(Ctx ctx, S9S2Interactable it)
    {
        var prefab = LoadAsset<GameObject>(SparklePrefabPath, ctx);
        if (prefab == null)
            return;

        var sparkle = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ctx.Scene);
        sparkle.name = "Sparkle";
        sparkle.transform.SetParent(it.transform, false);
        sparkle.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        sparkle.SetActive(false);
        SetRef(it, "_sparkle", sparkle);
    }

    private static void MakePistolTable(Ctx ctx, Transform parent, Vector3 pos)
    {
        Sprite desk = LoadAsset<Sprite>(DeskSpritePath, ctx);
        GameObject table = MakeSprite(ctx, parent, "PistolTable", pos, desk, 2);
        table.layer = NoPassLayer;

        // 기획: '작아진 책상' 재사용
        if (desk != null)
        {
            float s = 1.2f / Mathf.Max(desk.bounds.size.x, 0.01f);
            table.transform.localScale = new Vector3(s, s, 1f);
        }

        var box = table.AddComponent<BoxCollider2D>();
        if (desk != null)
            box.size = new Vector2(desk.bounds.size.x, desk.bounds.size.y);
    }

    private static GameObject MakeScreen(
        Ctx ctx,
        Transform parent,
        Vector3 center,
        out RawImage image,
        out TMP_Text label,
        out VideoPlayer video
    )
    {
        var go = new GameObject("Screen", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(parent, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1;

        var rect = go.GetComponent<RectTransform>();
        rect.position = new Vector3(center.x, center.y, 0f);
        rect.sizeDelta = new Vector2(ScreenWidth, ScreenHeight);
        rect.localScale = Vector3.one;

        var imgGo = new GameObject("ScreenImage", typeof(RectTransform), typeof(RawImage));
        imgGo.transform.SetParent(go.transform, false);
        Stretch(imgGo.GetComponent<RectTransform>());
        image = imgGo.GetComponent<RawImage>();
        image.color = Color.black;
        image.raycastTarget = false;

        var labelGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        Stretch(labelGo.GetComponent<RectTransform>());
        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text = "[임시] 스크린 영상\n(시퀀스 2~8 장면 → 스크린 앞의 하루)";
        tmp.fontSize = 1.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (ctx.Font != null)
            tmp.font = ctx.Font;
        labelGo.SetActive(false);
        label = tmp;

        video = go.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.renderMode = VideoRenderMode.RenderTexture;
        video.audioOutputMode = VideoAudioOutputMode.Direct;

        return go;
    }

    private static CinemachineVirtualCamera MakeScreenCamera(Ctx ctx, Transform parent, Vector3 screenCenter)
    {
        var go = new GameObject("Screen Closeup Virtual Camera");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(screenCenter.x, screenCenter.y, -10f);

        var vcam = go.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 10;

        // 스크린(21x15)이 화면을 꽉 채우는 크기
        vcam.m_Lens.OrthographicSize = ScreenHeight * 0.5f;

        return vcam;
    }

    private static void MakeIllustrationCanvas(Ctx ctx, out Image illust, out TMP_Text label, out Image black)
    {
        Transform canvasRoot = EnsureRoot("=====Canvas=====", ctx).transform;

        var go = new GameObject("S9S2 Illustration Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.layer = 5;
        go.transform.SetParent(canvasRoot, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 8-2 일러스트 캔버스와 같은 값: 맵 위, 대화창(0) 아래
        canvas.sortingOrder = -1;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1344f, 960f);

        illust = MakeFullImage(go.transform, "Illustration", Color.white);
        illust.preserveAspect = true;
        illust.gameObject.SetActive(false);

        var labelGo = new GameObject("PlaceholderLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.layer = 5;
        labelGo.transform.SetParent(go.transform, false);
        Stretch(labelGo.GetComponent<RectTransform>());
        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 42f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (ctx.Font != null)
            tmp.font = ctx.Font;
        labelGo.SetActive(false);
        label = tmp;

        black = MakeFullImage(go.transform, "Black", Color.black);
        black.gameObject.SetActive(false);
    }

    private static Image MakeFullImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject MakeSprite(Ctx ctx, Transform parent, string name, Vector3 pos, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    private static GameObject MakeRect(Ctx ctx, Transform parent, string name, Vector3 localPos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = color;
        sr.sortingOrder = order;

        // 부모 스케일을 상쇄해 월드 크기 그대로 보이게
        Vector3 ls = parent.lossyScale;
        go.transform.localScale = new Vector3(
            1f / Mathf.Max(Mathf.Abs(ls.x), 0.0001f),
            1f / Mathf.Max(Mathf.Abs(ls.y), 0.0001f),
            1f
        );

        return go;
    }

    private static TMP_Text MakeLabel(Ctx ctx, Transform parent, string text, Vector3 localPos, float fontSize, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        Vector3 ls = parent.lossyScale;
        go.transform.localScale = new Vector3(
            1f / Mathf.Max(Mathf.Abs(ls.x), 0.0001f),
            1f / Mathf.Max(Mathf.Abs(ls.y), 0.0001f),
            1f
        );

        var tmp = go.GetComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        tmp.sortingOrder = 20;
        if (ctx.Font != null)
            tmp.font = ctx.Font;

        go.GetComponent<RectTransform>().sizeDelta = new Vector2(4f, 1f);
        return tmp;
    }

    // ================================================================ 리소스

    private static void LinkResources(SerializedObject so, Ctx ctx)
    {
        SetObj(so, "_gunshotSfx", LoadAsset<AudioClip>(GunshotPath, ctx), "총성", ctx);
        SetObj(so, "_phoneRingSfx", LoadAsset<AudioClip>(PhoneRingPath, ctx), "전화벨", ctx);

        SetIfFound<AudioClip>(so, "_bgm", BgmName, ctx);
        SetIfFound<AudioClip>(so, "_jumpScareSfx", JumpScareName, ctx);
        SetIfFound<AudioClip>(so, "_tvOffSfx", TvOffName, ctx);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetIfFound<T>(SerializedObject so, string field, string assetName, Ctx ctx)
        where T : Object
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
            return;

        if (p.objectReferenceValue != null)
            return;

        string typeName = typeof(T) == typeof(AudioClip) ? "AudioClip" : typeof(T).Name;

        foreach (string guid in AssetDatabase.FindAssets($"{assetName} t:{typeName}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != assetName)
                continue;

            p.objectReferenceValue = AssetDatabase.LoadAssetAtPath<T>(path);
            ctx.Report.Add($"{assetName} 연결 ({path})");
            return;
        }

        ctx.Report.Add($"⏳ {assetName} 은 아직 프로젝트에 없다 (들어오면 '리소스 다시 연결')");
    }

    private static TMP_FontAsset FindFont()
    {
        foreach (string guid in AssetDatabase.FindAssets("KoPubWorld Batang Medium t:TMP_FontAsset"))
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (f != null)
                return f;
        }

        return null;
    }

    // ================================================================ 좌표

    /// <summary>
    /// 하루는 시작 위치에서 한 칸(1유닛)씩 움직이므로, 같은 공간의 지점들은 기준점과 정수 칸만큼 떨어져 있어야
    /// 문 앞에 정확히 설 수 있다.
    /// </summary>
    private static Vector3 SnapTo(Vector3 origin, Vector3 p)
    {
        return new Vector3(
            origin.x + Mathf.Round(p.x - origin.x),
            origin.y + Mathf.Round(p.y - origin.y),
            0f
        );
    }

    /// <summary>
    /// 벽/오브젝트(NoPassing)와 겹치지 않는 가장 가까운 칸을 찾는다.
    /// 원본 맵 좌표를 추정해서 잡는 지점이 많아서, 오브젝트 속에 하루가 박히는 것을 막는다.
    /// </summary>
    private static Vector3 FreeSpot(Vector3 start, Bounds within, bool preferDown = false)
    {
        int mask = 1 << NoPassLayer;
        start.z = 0f;

        for (int r = 0; r <= 10; r++)
        {
            var candidates = new List<Vector3>();

            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r)
                        continue;

                    candidates.Add(start + new Vector3(dx, dy, 0f));
                }
            }

            // 아래쪽(문에서 멀어지는 쪽)을 먼저 본다
            if (preferDown)
                candidates.Sort((a, b) => a.y.CompareTo(b.y));

            foreach (Vector3 c in candidates)
            {
                bool inside =
                    c.x > within.min.x + 0.5f
                    && c.x < within.max.x - 0.5f
                    && c.y > within.min.y + 0.5f
                    && c.y < within.max.y - 0.5f;

                if (inside && Physics2D.OverlapBox(c, new Vector2(0.8f, 0.8f), 0f, mask) == null)
                    return c;
            }
        }

        return start;
    }

    private static bool IsFree(Vector3 p) =>
        Physics2D.OverlapBox(p, new Vector2(0.8f, 0.8f), 0f, 1 << NoPassLayer) == null;

    private static Bounds SpriteBounds(Transform room, string path)
    {
        Transform t = Find(room, path);
        var sr = t != null ? t.GetComponent<SpriteRenderer>() : null;

        if (sr != null)
            return sr.bounds;

        // 못 찾으면 공간 전체의 렌더러 합집합
        var bounds = new Bounds(room.position, Vector3.one * 10f);
        bool first = true;

        foreach (SpriteRenderer r in room.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (first)
            {
                bounds = r.bounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return bounds;
    }

    // ================================================================ 탐색 / 직렬화 헬퍼

    private static Transform Find(Transform room, string path)
    {
        if (room == null)
            return null;

        Transform t = room.Find(path);
        if (t != null)
            return t;

        // 원본 이름 끝에 공백이 붙은 오브젝트가 있다 ('obj_filmstoragebox ')
        string[] parts = path.Split('/');
        Transform cur = room;

        foreach (string part in parts)
        {
            Transform next = null;
            foreach (Transform child in cur)
            {
                if (child.name.Trim() == part.Trim())
                {
                    next = child;
                    break;
                }
            }

            if (next == null)
                return null;

            cur = next;
        }

        return cur;
    }

    private static Transform FindByPath(Scene scene, string path)
    {
        string[] parts = path.Split('/');

        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name.Trim() != parts[0].Trim())
                continue;

            if (parts.Length == 1)
                return go.transform;

            Transform found = Find(go.transform, string.Join("/", parts.Skip(1)));
            if (found != null)
                return found;
        }

        return null;
    }

    private static string PathOf(Transform t)
    {
        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    private static CinemachineVirtualCamera FindVcam(Transform room, string name)
    {
        Transform t = Find(room, name);
        return t != null ? t.GetComponent<CinemachineVirtualCamera>() : null;
    }

    private static Transform FindDeepInScene(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
        }

        return null;
    }

    private static GameObject FindRoot(string name, Scene scene)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == name)
                return go;
        }

        return null;
    }

    private static GameObject EnsureRoot(string name, Ctx ctx)
    {
        GameObject found = FindRoot(name, ctx.Scene);
        if (found != null)
            return found;

        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, ctx.Scene);
        return go;
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null)
            return found;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void AddToBuildSettings(Ctx ctx)
    {
        var scenes = EditorBuildSettings.scenes.ToList();

        if (scenes.Any(s => s.path == TargetScenePath))
            return;

        int after = scenes.FindIndex(s => s.path == BaseScenePath);
        var entry = new EditorBuildSettingsScene(TargetScenePath, true);

        if (after >= 0)
            scenes.Insert(after + 1, entry);
        else
            scenes.Add(entry);

        EditorBuildSettings.scenes = scenes.ToArray();
        ctx.Report.Add("빌드 설정에 Sequence9S#2 추가 (8-2 바로 뒤)");
    }

    private static T LoadAsset<T>(string path, Ctx ctx)
        where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            ctx.Report.Add($"⚠ 애셋을 못 찾았다: {path}");

        return asset;
    }

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
            return;

        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObj(SerializedObject so, string field, Object value, string label, Ctx ctx)
    {
        SerializedProperty p = so.FindProperty(field);

        if (p == null)
        {
            Debug.LogWarning($"{Tag} 컨트롤러에 '{field}' 필드가 없다. 스크립트가 바뀌었는지 확인할 것.");
            return;
        }

        p.objectReferenceValue = value;

        if (label != null && value == null)
            ctx.Report.Add($"⚠ {label} 를 못 찾아 비워둠");
    }

    private static void SetStr(SerializedObject so, string field, string value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null)
            p.stringValue = value;
    }

    private static void SetArray(SerializedObject so, string field, Object[] values)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
            return;

        p.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
