using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 기획서 레이아웃 이미지에 맞춰 8-1, 8-2, 9-2, 10-2 씬에 정식 아트를 입히는 도구.
/// 씬을 다시 만들지 않고 이름으로 오브젝트를 찾아 스프라이트 / 위치 / 콜라이더만 바꾸므로
/// 여러 번 실행해도 결과가 같고, 건드리지 않는 오브젝트의 수동 편집은 그대로 남는다.
///
/// 아직 리소스가 없는 것(2x2 탁자, 찌부된 침대, 작아진 책상, 길어진 서랍, 열리는 옷장 등)은
/// 기존 스프라이트의 크기만 조절해 임시로 맞춰 둔다. 리소스가 나오면 스프라이트만 바꾸면 된다.
///
/// 하루 이동은 중심점 기준 Linecast다. 발은 중심에서 1칸 아래이므로 콜라이더는 그림보다 1칸 위쪽을 막는다.
/// </summary>
public static class S9S2ArtApply
{
    private const string Tag = "[아트 적용]";

    private const string Bg = "Assets/Sprites/Background/";
    private const string Obj8 = "Assets/Sprites/Object/Sequence8/";
    private const string Obj9 = "Assets/Sprites/Object/Sequence9/";

    private const string Scene92 = "Assets/Scenes/2026PlayX4/Sequence9S#2.unity";
    private const string Scene81 = "Assets/Scenes/2026PlayX4/Sequence8S#1.unity";
    private const string Scene82 = "Assets/Scenes/2026PlayX4/Sequence8S#2.unity";
    private const string Scene102 = "Assets/Scenes/2026PlayX4/Sequence10S#2.unity";

    private const string LobbyBg = Bg + "BG_photoStudioLobby_S9_v1.png";
    private const string DarkroomBg = Bg + "BG_darkroom_S9_v1.png";
    private const string HouseBg = Bg + "BG_haruRoom_S9_v1.png";
    private const string CorridorBg = Bg + "BG_repeatCorridor_S9_v1.png";
    private const string GardenBg = Bg + "BG_garden_S8_v1.png";
    private const string GardenHedge = Bg + "BG_gardenHedge_S8_v1.png";

    private const string HaruSquat = "Assets/Sprites/Character/Haru/Haru_SquatBack_v1.png";
    private const string LunaSquat = "Assets/Sprites/Character/Luna/Luna_SquatBack_v1.png";

    private const string Statue = Obj8 + "OBJ_statue_v1.png";
    private const string StatueRubble = Obj8 + "OBJ_statueDestroyed_v1.png";
    private const string Curtain = Obj8 + "OBJ_redCurtain_v1.png";

    private const string Counter = Obj9 + "OBJ_counterDistorted_v1.png";
    private const string DeadTank = Obj9 + "OBJ_deadFishTank_v1.png";
    private const string PosterA = Obj9 + "OBJ_cinemaPosterA_S9_v1.png";
    private const string PosterB = Obj9 + "OBJ_cinemaPosterB_S9_v1.png";
    private const string PosterC = Obj9 + "OBJ_cinemaPosterC_S9_v1.png";
    private const string PosterEmpty = Obj9 + "OBJ_cinemaPosterEmpty_S9_v1.png";
    private const string HangingPhotos = Obj9 + "OBJ_hangingPhotos_S9_v1.png";
    private const string PhotoCollage = "Assets/Sprites/illust/ILL_lunaPhotoCollage_v1.png";
    private const string TicketBox = Obj9 + "OBJ_ticketBoxNoInterior_v1.png";
    private const string LongChairV = Obj9 + "OBJ_longChairVertical_v1.png";
    private const string Soot = Obj9 + "OBJ_blackSoot_v1.png";
    private const string Stool = "Assets/Sprites/Object/chair.png"; // 사진관 스툴. OBJ_stool_v1과 같은 그림이다
    private const string Doppelganger = Obj9 + "ANI_haruDoppelganger_strip_v1.png";
    private const string TempTable = Obj9 + "OBJ_table2x2_TEMP.png";
    private const string ScreenMap = Bg + "BG_screenSpace_S9_v1.png";

    // 스프라이트 2배 축소(scale 0.5) 기준 128px = 1칸.
    private const float DefaultPpu = 64f;

    private const int NoPassLayer = 6;

    private static readonly List<string> Missing = new List<string>();

    // ================================================================ 메뉴 / 배치 진입점

    [MenuItem("Tools/유메지 [9-2 상영 중]/아트 리소스 적용 (현재 씬 유지)", false, 5)]
    public static void ApplyMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        ApplyAllScenes();
    }

    public static void BuildFromCommandLine()
    {
        ApplyAllScenes();
        EditorApplication.Exit(Missing.Count == 0 ? 0 : 1);
    }

    private static void ApplyAllScenes()
    {
        Missing.Clear();
        EnsureImportSettings();

        Scene s = EditorSceneManager.OpenScene(Scene92, OpenSceneMode.Single);
        ApplyNineTwo(s);
        EditorSceneManager.SaveScene(s);

        s = EditorSceneManager.OpenScene(Scene81, OpenSceneMode.Single);
        ApplyStatue(s);
        EditorSceneManager.SaveScene(s);

        s = EditorSceneManager.OpenScene(Scene82, OpenSceneMode.Single);
        ApplyStatue(s);
        ApplySequence8Extras(s);
        EditorSceneManager.SaveScene(s);

        s = EditorSceneManager.OpenScene(Scene102, OpenSceneMode.Single);
        ApplySquatPoses(s);
        EditorSceneManager.SaveScene(s);

        AssetDatabase.SaveAssets();
        LogResult();
    }

    /// <summary>씬 생성 도구가 만든 직후의 9-2 씬에 곧바로 입힐 때 쓴다.</summary>
    public static void ApplyAfterBuild(Scene scene)
    {
        Missing.Clear();
        EnsureImportSettings();
        ApplyNineTwo(scene);
        LogResult();
    }

    private static void LogResult()
    {
        if (Missing.Count > 0)
            Debug.LogError($"{Tag} 찾지 못한 대상\n - " + string.Join("\n - ", Missing));
        else
            Debug.Log($"{Tag} 완료");
    }

    private static void ApplyNineTwo(Scene scene)
    {
        ApplyLobby(scene);
        ApplyDarkroom(scene);
        ApplyHouse(scene);
        ApplyCinema(scene);
        ApplyScreenSpace(scene);
        ApplyCorridor(scene);

        EditorSceneManager.MarkSceneDirty(scene);
    }

    // ================================================================ 임포트 설정

    private static void EnsureImportSettings()
    {
        AssetDatabase.Refresh();

        foreach (string path in new[]
                 {
                     LobbyBg, DarkroomBg, HouseBg, Statue, StatueRubble, Curtain, Counter, DeadTank, PosterA,
                     PosterB, PosterC, PosterEmpty, HangingPhotos, TicketBox, LongChairV, Soot, TempTable,
                 })
            ImportAsSprite(path, DefaultPpu);

        // 일러스트는 도트가 아니라 다른 일러스트처럼 부드럽게 표시한다
        ImportAsSprite(PhotoCollage, 100f, 2048, null, FilterMode.Bilinear);

        // 672x608인 21x19칸 맵. 다른 오브젝트와 같은 스케일 0.5로 칸이 맞도록 PPU 16
        ImportAsSprite(ScreenMap, 16f);

        // 2048을 넘는 원본은 줄어들지 않게 최대 크기를 올린다
        ImportAsSprite(CorridorBg, DefaultPpu, 4096);
        ImportAsSprite(GardenBg, DefaultPpu, 4096);
        ImportAsSprite(GardenHedge, DefaultPpu, 4096);
        ImportAsSprite(Doppelganger, DefaultPpu, 8192);

        // 쪼그려 앉은 자세는 서 있는 자세와 발 위치가 같아야 한다.
        // 하루는 피봇 아래 128px(1칸 / 스케일 0.5)이 발, 루나는 피봇이 곧 발이다
        ImportAsSprite(HaruSquat, DefaultPpu, 2048, new Vector2(0.5f, 128f / 220f));
        ImportAsSprite(LunaSquat, DefaultPpu, 2048, new Vector2(0.5f, 0f));
    }

    private static void ImportAsSprite(string path, float ppu, int maxSize = 2048, Vector2? pivot = null, FilterMode filter = FilterMode.Point)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;

        if (imp == null)
        {
            Missing.Add("이미지 없음: " + path);
            return;
        }

        bool same = imp.textureType == TextureImporterType.Sprite
                    && imp.spriteImportMode == SpriteImportMode.Single
                    && Mathf.Approximately(imp.spritePixelsPerUnit, ppu)
                    && imp.filterMode == filter
                    && imp.textureCompression == TextureImporterCompression.Uncompressed
                    && !imp.mipmapEnabled
                    && imp.maxTextureSize == maxSize
                    && (pivot == null || imp.spritePivot == pivot.Value);

        if (same)
            return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = filter;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.maxTextureSize = maxSize;

        if (pivot != null)
        {
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot.Value;
            imp.SetTextureSettings(settings);
            imp.spritePivot = pivot.Value;
        }

        imp.SaveAndReimport();
    }

    // ================================================================ 사진관 로비 (이미지 11, 10)

    private static void ApplyLobby(Scene scene)
    {
        Transform room = Room(scene, "=====Map=====/Room_PhotoStudio/Guest Area");
        if (room == null)
            return;

        // 새 배경은 왼쪽 벽을 기준으로 맞춘다. 문(-1~0)과 커튼(8~10)이 원본 텔레포트 자리와 정확히 일치한다.
        Do(room, "Background", t =>
        {
            SetSprite(t, LobbyBg);
            SetPos(t, 3.506f, -2.49f);
        });

        // 옛 배경의 오른쪽 벽 틈(-3.44 ~ -2.5)은 새 배경에 없으므로 막는다
        WorldWall(room, "Walls/RightWallFill_S9", new Vector2(10.03f, -3.6f), new Vector2(11.03f, -2.4f));

        // 스툴 4개 (기획: 왼쪽 벽 옆 세로 한 줄)
        float[] stoolY = { 0.5f, -1f, -2.5f, -4f };
        for (int i = 0; i < 6; i++)
        {
            string path = i == 0 ? "Objects/Stalls/chair" : $"Objects/Stalls/chair ({i})";
            int index = i;

            Do(room, path, t =>
            {
                bool used = index < stoolY.Length;
                SetSprite(t, Stool);
                t.gameObject.SetActive(used);
                if (used)
                    SetPos(t, -1.5f, stoolY[index]);
            });
        }

        // 가운데 사각 카운터 + 안쪽 소파 + 필름통
        Do(room, "Objects/obj_countertable/GameObject", t => t.gameObject.SetActive(false));
        Do(room, "Objects/obj_countertable/GameObject (1)", t => t.gameObject.SetActive(false));
        Do(room, "Objects/obj_countertable/OBJ_counterLShape_v1", t =>
        {
            SetSprite(t, Counter, "Player", 2);
            SetPos(t, 3.5f, -1f);
            t.gameObject.layer = NoPassLayer;
            SetBox(t.gameObject, new Vector2(0.45f, -2.5f), new Vector2(6.55f, 1.5f));
        });

        Do(room, "Objects/obj_sofa_v1", t =>
        {
            SetPos(t, 3.5f, -0.41f);
            SetOrder(t, 3);

            // 소파는 카운터 안쪽 그림 위에 그려야 해서 y 기반 정렬을 끈다 (카운터 콜라이더가 대신 막는다)
            var layer = t.GetComponent<LayerSetting>();
            if (layer != null)
                layer.enabled = false;

            foreach (Collider2D c in t.GetComponents<Collider2D>())
                c.enabled = false;
        });

        Do(room, "Objects/obj_FilmCanister", t =>
        {
            SetPos(t, 6f, -1f);
            SetOrder(t, 5);

            Transform oldStand = t.Find("FilmStand");
            if (oldStand != null)
                UnityEngine.Object.DestroyImmediate(oldStand.gameObject);
        });

        // 긴 탁자와 흰 스크린은 레이아웃 자리로
        Do(room, "Objects/obj_table_v1", t => SetPos(t, 1.75f, -6f));
        Do(room, "Objects/obj_softbox_v1", t =>
        {
            SetPos(t, 8.5f, -6.5f);
            var collider = t.GetComponent<BoxCollider2D>();
            if (collider != null)
                collider.offset = Vector2.zero;
        });

        // 오른쪽 벽의 2x2 탁자 + 전화기. 탁자 리소스가 진행 중이라 임시 스프라이트를 쓴다.
        Transform table = EnsureSpriteObject(room, "Objects/OBJ_table2x2_TEMP", TempTable, "Player", 1);
        if (table != null)
        {
            table.position = new Vector3(9f, -2.5f, 0f);
            SetWorldScale(table, 0.5f, 0.5f);
            table.gameObject.layer = NoPassLayer;
            SetBox(table.gameObject, new Vector2(7.95f, -2.5f), new Vector2(10f, -1f));
        }

        Do(room, "Objects/obj_telephone", t =>
        {
            t.position = new Vector3(9.2f, -1.95f, 0f);
            SetOrder(t, 2);
        });

        // 레이아웃에 없는 원본 소품은 지우지 않고 끈다
        foreach (string name in new[] { "Objects/obj_lp_v1", "Objects/obj_post", "Objects/obj_tripod_v1" })
            Do(room, name, t => t.gameObject.SetActive(false));

        // ① 시작: 맵 아래쪽 가운데
        Vector3 spawn = new Vector3(3.5f, -7.75f, 0f);
        DoScene(scene, "=====S9S2=====/Points/PhotoLobbySpawn", t => t.position = spawn);
        DoScene(scene, "=====Character=====/Haru_Player", t => t.position = new Vector3(spawn.x, spawn.y, t.position.z));
    }

    // ================================================================ 암실 (이미지 12)

    private static void ApplyDarkroom(Scene scene)
    {
        // 옛 bg_workroom과 크기가 같고 틈 위치(-1~0, 8~10)도 같아 스프라이트만 바꾼다
        DoScene(scene, "=====Map=====/Room_PhotoStudio/Work Room/bg_workroom", t => SetSprite(t, DarkroomBg));

        // 벽의 사진 묶음과 조사했을 때 뜨는 콜라주 일러스트
        DoScene(scene, "=====Map=====/Room_PhotoStudio/Work Room/Objects/obj_photography", t => SetSprite(t, HangingPhotos));

        var controller = UnityEngine.Object.FindObjectOfType<Sequence9Scene2DialogueController>(true);
        Sprite collage = LoadSprite(PhotoCollage);
        if (controller != null && collage != null)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("_photoWallSprite").objectReferenceValue = collage;

            // 콜라주는 4:3이라 화면(1.4:1) 양옆이 남는다. 일러스트 바로 뒤에 검은 판을 깐다
            var illust = so.FindProperty("_illustrationImage").objectReferenceValue as UnityEngine.UI.Image;
            if (illust != null)
                so.FindProperty("_illustrationBackdrop").objectReferenceValue = EnsureBackdrop(illust);
            else
                Missing.Add("_illustrationImage");

            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else if (controller == null)
        {
            Missing.Add("9-2 컨트롤러");
        }

        // 사진 벽은 레이아웃의 벽 가운데 사진 묶음이다. 하루는 탁자 앞(y=20.25)에서 위를 보고 조사한다
        DoScene(scene, "=====S9S2=====/PhotoWall", t =>
        {
            SetPos(t, 2.92f, 23.4f);
            SetBox(t.gameObject, new Vector2(1.25f, 21f), new Vector2(4.76f, 22f));
        });

        // 선반 문: 레이아웃처럼 선반 위로 빛나는 문틈을 보이게 한다 ('암실 선반 문' 리소스가 오면 교체)
        DoScene(scene, "=====Map=====/Room_PhotoStudio/Work Room/Objects/obj_filmstoragebox /OpenVisual", t =>
        {
            SetPos(t, 9f, 22.5f);
            SetOpenVisual(t, new Vector2(1.7f, 2.9f), new Color(1f, 0.97f, 0.92f, 1f), "Player", -21);
        });
    }

    // ================================================================ 하루의 집 (이미지 7, 13)

    private static void ApplyHouse(Scene scene)
    {
        Transform room = Room(scene, "=====Map=====/Room_House");
        if (room == null)
            return;

        const string B = "BG_haruRoom_v2/";

        // 960x928 = 7.5 x 7.25칸. 내부 7x7칸(벽 2 + 바닥 5), x 297~304, y -3~4
        Do(room, "BG_haruRoom_v2", t =>
        {
            SetSprite(t, HouseBg);
            SetPos(t, 300.5f, 0.625f);
        });

        // 옛 벽은 새 방 크기와 맞지 않아 끄고 새로 만든다
        Do(room, B + "Wall", t => t.gameObject.SetActive(false));
        Do(room, B + "OBJ_Haru_Door", t => t.gameObject.SetActive(false));

        WorldWall(room, "Wall_S9/Top", new Vector2(296f, 2.5f), new Vector2(305f, 5f));
        WorldWall(room, "Wall_S9/Bottom", new Vector2(296f, -3.5f), new Vector2(305f, -2.25f));
        WorldWall(room, "Wall_S9/Left", new Vector2(296f, -3.5f), new Vector2(297f, 5f));
        WorldWall(room, "Wall_S9/Right", new Vector2(304f, -3.5f), new Vector2(305f, 5f));

        // 카메라 경계도 새 방 중심으로
        Do(room, "Haru_Room_Colider", t => SetPos(t, 300.5f, 0.625f));

        Do(room, B + "OBJ_Window", t => SetPos(t, 300.5f, 3.28f));

        // 죽어있는 어항
        Do(room, B + "ANI_haruFishTank_v1_0", t =>
        {
            SetSprite(t, DeadTank);

            var anim = t.GetComponent<Animator>();
            if (anim != null)
                anim.enabled = false;

            SetPos(t, 300f, 1.75f);
            SetWorldScale(t, 0.5f, 0.5f);
            SetBox(t.gameObject, new Vector2(299f, 1.5f), new Vector2(301f, 3f));
        });

        // 찌부된 침대 (리소스 없음): 옛 침대를 세로로 돌려 납작하게. 회전이 있어 콜라이더는 따로 둔다
        Do(room, B + "OBJ_haruBed_v1", t =>
        {
            t.rotation = Quaternion.Euler(0f, 0f, -90f);
            SetPos(t, 297.5f, -0.4f);
            SetWorldScale(t, 0.536f, 0.216f);

            foreach (Collider2D c in t.GetComponents<Collider2D>())
                c.enabled = false;
        });
        WorldWall(room, "Wall_S9/Bed", new Vector2(297f, -1.25f), new Vector2(298f, 2.5f));

        // 길어진 서랍 (리소스 없음): 옛 서랍을 세로로 늘린다
        Do(room, B + "OBJ_haruDrawer_v1", t =>
        {
            SetPos(t, 301.4f, 0.875f);
            SetWorldScale(t, 0.303f, 0.9f);
            SetBox(t.gameObject, new Vector2(301f, -0.25f), new Vector2(301.8f, 3f));
        });

        // 열리는 옷장 (리소스 없음): 옛 옷장을 벽에 맞춘다. S9S2Door가 그대로 붙어 있다
        Do(room, B + "OBJ_haruWardrobe_v1", t =>
        {
            SetPos(t, 302.87f, 2.25f);
            SetBox(t.gameObject, new Vector2(301.8f, 1.4f), new Vector2(304f, 4f));
        });
        Do(room, B + "OBJ_haruWardrobe_v1/OpenVisual", t =>
        {
            SetPos(t, 302.87f, 2.4f);
            SetOpenVisual(t, new Vector2(1.6f, 2.6f), new Color(1f, 0.97f, 0.92f, 1f), "Object", 5);
        });

        // 작아진 책상 (리소스 없음): 옛 책상을 절반으로. 전화기가 책상 위를 모두 덮어 상호작용 대상이 된다
        Do(room, B + "OBJ_haruDesk_v1", t =>
        {
            SetPos(t, 300.5f, -1.25f);
            SetWorldScale(t, 0.25f, 0.25f);

            foreach (Collider2D c in t.GetComponents<Collider2D>())
                c.enabled = false;
        });
        Do(room, B + "OBJ_haruDesk_v1/OBJ_HaruChair_v1", t => t.gameObject.SetActive(false));
        Do(room, B + "OBJ_telephone_v1", t =>
        {
            t.position = new Vector3(300.5f, -0.75f, 0f);
            SetOrder(t, 2);
            SetBox(t.gameObject, new Vector2(300f, -1.7f), new Vector2(301f, -0.25f));
        });
    }

    // ================================================================ 영화관 로비 (이미지 14~18)

    private static void ApplyCinema(Scene scene)
    {
        Transform room = Room(scene, "=====Map=====/Room_Cinema/BG_boxOffice_v2");
        if (room == null)
            return;

        const string O = "Object/";

        // 포스터 A, B, (빈 액자 자리), C
        Do(room, O + "OBJ_cinemaPosterA_v1", t =>
        {
            SetSprite(t, PosterA);
            SetPos(t, 599.5f, 55.75f);
        });
        Do(room, O + "OBJ_cinemaPosterB_v1", t =>
        {
            SetSprite(t, PosterB);
            SetPos(t, 601f, 55.75f);
        });
        Do(room, O + "OBJ_cinemaPosterC_v1", t =>
        {
            SetSprite(t, PosterC);
            SetPos(t, 604f, 55.75f);
        });

        // 비어 있는 액자는 B와 C 사이. 포스터 C를 복사해 콜라이더 구성을 맞춘다
        Transform empty = room.Find(O + "OBJ_cinemaPosterEmpty_v1");
        Transform posterC = room.Find(O + "OBJ_cinemaPosterC_v1");
        if (empty == null && posterC != null)
        {
            empty = UnityEngine.Object.Instantiate(posterC.gameObject, posterC.parent).transform;
            empty.name = "OBJ_cinemaPosterEmpty_v1";
        }

        if (empty != null)
        {
            SetSprite(empty, PosterEmpty);
            SetPos(empty, 602.5f, 55.75f);
        }
        else
        {
            Missing.Add("OBJ_cinemaPosterC_v1");
        }

        // 내부가 보이지 않는 티켓 박스. 직원은 없앤다
        Do(room, O + "OBJ_ticketBox_v1", t =>
        {
            SetSprite(t, TicketBox);
            SetPos(t, 597f, 51.5f);
            SetBox(t.gameObject, new Vector2(595f, 50f), new Vector2(599f, 54.5f));
        });
        Do(room, O + "OBJ_ticketBox_v1/OBJ_boxOfficeStaff_v1", t => t.gameObject.SetActive(false));

        // 티켓 박스 아래 가로 긴 의자 하나
        // 양옆으로 한 칸씩(x = 594.5, 599.5) 지나갈 수 있게 콜라이더는 그림보다 살짝 좁다
        Do(room, O + "OBJ_longChair_v2", t =>
        {
            SetPos(t, 597f, 47.4f);
            SetBox(t.gameObject, new Vector2(594.6f, 47.3f), new Vector2(599.4f, 48.88f));
        });

        // 양옆 세로 긴 의자
        Transform left = room.Find(O + "OBJ_longChairV_L");
        if (left == null)
        {
            left = room.Find(O + "OBJ_longChair_v2 (1)");
            if (left != null)
                left.name = "OBJ_longChairV_L";
            else
                Missing.Add("OBJ_longChair_v2 (1)");
        }

        if (left != null)
        {
            SetSprite(left, LongChairV);
            SetPos(left, 593.5f, 49.1f);
            SetBox(left.gameObject, new Vector2(593f, 47.1f), new Vector2(594f, 52.1f));

            Transform right = room.Find(O + "OBJ_longChairV_R");
            if (right == null)
            {
                right = UnityEngine.Object.Instantiate(left.gameObject, left.parent).transform;
                right.name = "OBJ_longChairV_R";
            }

            SetPos(right, 600.5f, 49.1f);
            SetBox(right.gameObject, new Vector2(600f, 47.1f), new Vector2(601f, 52.1f));
        }

        // 책상 세트: 왼쪽 위 / 오른쪽 아래. 팜플렛 스탠드는 레이아웃에 없다
        Do(room, O + "OBJ_pamphletStand_v1", t => t.gameObject.SetActive(false));
        Do(room, O + "OBJ_haruDesk_v1", t => SetPos(t, 593f, 53.2f));
        Do(room, O + "obj_lamp_v1", t => SetPos(t, 591.5f, 53.5f));
        Do(room, O + "OBJ_haruDesk_v1 (1)", t => SetPos(t, 607.2f, 47.2f));
        Do(room, O + "obj_lamp_v1 (1)", t => SetPos(t, 608.5f, 47.5f));

        // 배경에 그려진 양문(605~607)이 스크린으로 가는 문이다.
        // 이 맵의 칸은 원본 2-2와 같이 x = n + 0.5, y = n + 0.25 이므로 문 앞 두 칸(605.5, 606.5)에서만 넘어간다
        DoScene(scene, "=====S9S2=====/Doors/Door_CinemaToScreen", t =>
        {
            SetPos(t, 606f, 54.8f);
            SetBox(t.gameObject, new Vector2(605f, 54.28f), new Vector2(607f, 55.3f));

            Transform visual = t.Find("DoorVisual");
            if (visual != null)
                visual.gameObject.SetActive(false);
        });

        // ① 아래쪽 홈으로 들어온다. 홈 바닥이 원본에서는 텔레포트로 막혀 있었으니 벽으로 대신 막는다
        WorldWall(room.parent, "AlcoveBottom_S9", new Vector2(597f, 44.5f), new Vector2(603f, 45.2f));
        DoScene(scene, "=====S9S2=====/Points/CinemaLobbySpawn", t => t.position = new Vector3(599.5f, 45.25f, 0f));
    }

    // ================================================================ 스크린 공간

    /// <summary>
    /// 4-4 스크린 공간 맵: 21x15칸 스크린 + 그 아래 4칸 바닥. 옛 극장 좌석 맵은 끈다.
    /// 스크린은 x 591.5~612.5, y 88.125~103.125. 하루는 x = 정수, y = n + 0.375 칸을 다닌다.
    /// 영상용 ScreenImage는 맵의 스크린 위에 겹쳐 있고, 재생하지 않을 때는 투명하다.
    /// </summary>
    private static void ApplyScreenSpace(Scene scene)
    {
        Transform cinema = Room(scene, "=====Map=====/Room_Cinema");
        if (cinema == null)
            return;

        Do(cinema, "BG_theater_v1", t => t.gameObject.SetActive(false));

        Transform oldFloor = cinema.Find("ScreenSpace_S9/Floor_TEMP");
        if (oldFloor != null)
            UnityEngine.Object.DestroyImmediate(oldFloor.gameObject);

        Transform map = EnsureSpriteObject(cinema, "ScreenSpace_S9/Background", ScreenMap, "Default", 0);
        if (map != null)
        {
            map.position = new Vector3(602f, 93.625f, 0f);
            SetWorldScale(map, 0.5f, 0.5f);
        }

        WorldWall(cinema, "ScreenSpace_S9/Wall_Top", new Vector2(590.5f, 88.875f), new Vector2(613.5f, 89.875f));
        WorldWall(cinema, "ScreenSpace_S9/Wall_Bottom", new Vector2(590.5f, 83.875f), new Vector2(613.5f, 84.875f));
        WorldWall(cinema, "ScreenSpace_S9/Wall_Left", new Vector2(590.5f, 84.875f), new Vector2(591.5f, 88.875f));
        WorldWall(cinema, "ScreenSpace_S9/Wall_Right", new Vector2(612.5f, 84.875f), new Vector2(613.5f, 88.875f));

        // 카메라 경계: 공간 전체. 뷰가 항상 이 안에 있어 맵 밖의 검은 부분이 보이지 않는다
        Transform boundT = EnsurePath(cinema, "ScreenSpace_S9/CameraBoundary");
        boundT.position = Vector3.zero;
        boundT.gameObject.layer = 2;
        var bound = boundT.GetComponent<PolygonCollider2D>();
        if (bound == null)
            bound = boundT.gameObject.AddComponent<PolygonCollider2D>();
        bound.isTrigger = true;
        bound.pathCount = 1;
        bound.SetPath(0, new[]
        {
            new Vector2(591.5f, 84.125f),
            new Vector2(591.5f, 103.125f),
            new Vector2(612.5f, 103.125f),
            new Vector2(612.5f, 84.125f),
        });

        var controller = UnityEngine.Object.FindObjectOfType<Sequence9Scene2DialogueController>(true);
        if (controller == null)
        {
            Missing.Add("9-2 컨트롤러");
            return;
        }

        var so = new SerializedObject(controller);

        var roomCam = so.FindProperty("_screenRoomCam").objectReferenceValue as CinemachineVirtualCamera;
        var confiner = roomCam != null ? roomCam.GetComponent<CinemachineConfiner>() : null;
        if (confiner != null)
        {
            confiner.m_BoundingShape2D = bound;
            EditorUtility.SetDirty(confiner);
        }
        else
        {
            Missing.Add("_screenRoomCam 또는 CinemachineConfiner");
        }

        var image = so.FindProperty("_screenImage").objectReferenceValue as UnityEngine.UI.RawImage;
        if (image != null)
        {
            so.FindProperty("_screenIdleColor").colorValue = Color.clear;
            so.ApplyModifiedPropertiesWithoutUndo();
            image.texture = null;
            image.color = Color.clear;
            EditorUtility.SetDirty(image);
        }
        else
        {
            Missing.Add("_screenImage");
        }

        // ① 아래쪽 가운데로 들어와 ② 스크린 바로 앞까지 걷는다
        DoScene(scene, "=====S9S2=====/Points/ScreenRoomSpawn", t => t.position = new Vector3(602f, 85.375f, 0f));
        DoScene(scene, "=====S9S2=====/Points/ScreenStandPoint", t => t.position = new Vector3(602f, 87.375f, 0f));

        // ③ 검은 문: 기획 이미지 5-5의 위치 (스크린 하단 오른쪽, 2.65 x 3칸)
        DoScene(scene, "=====S9S2=====/Doors/Door_Screen", t =>
        {
            SetPos(t, 610f, 89.375f);
            SetBox(t.gameObject, new Vector2(608.75f, 88.875f), new Vector2(611.25f, 89.875f));

            Transform visual = t.Find("DoorVisual");
            if (visual == null)
            {
                Missing.Add("Door_Screen/DoorVisual");
                return;
            }

            SetPos(visual, 610f, 89.625f);
            SetOpenVisual(visual, new Vector2(2.65f, 3f), Color.black, "Default", 3);

            Transform label = visual.Find("Label");
            if (label != null)
                label.gameObject.SetActive(false);
        });
    }

    // ================================================================ 반복되는 복도 (이미지 6, 9)

    private static void ApplyCorridor(Scene scene)
    {
        Transform room = Room(scene, "=====Map=====/Room_Corridor");
        if (room == null)
            return;

        const string B = "BG_haruApartmentLobby_v1/";

        // 18x5칸. x 894~912, 바닥 y -23~-20, 벽 -20~-18
        Do(room, "BG_haruApartmentLobby_v1", t =>
        {
            SetSprite(t, CorridorBg);
            SetPos(t, 903f, -20.375f);
        });

        foreach (string name in new[] { "FrontDoor", "DoorBell (1)", "FrontDoor (1)", "FrontDoor (2)", "FrontDoor (3)", "Wall (2)" })
            Do(room, B + name, t => t.gameObject.SetActive(false));

        WorldWall(room, "Wall_S9/Top", new Vector2(893f, -19.41f), new Vector2(913f, -18.41f));
        WorldWall(room, "Wall_S9/Bottom", new Vector2(893f, -23.59f), new Vector2(913f, -22.59f));
        WorldWall(room, "Wall_S9/Left", new Vector2(893f, -24f), new Vector2(894f, -17f));
        WorldWall(room, "Wall_S9/Right", new Vector2(912f, -24f), new Vector2(913f, -17f));

        // 카메라 경계: 세로는 그대로, 가로만 새 길이로
        Do(room, "Apt_Colider", t =>
        {
            SetPos(t, 903f, -20.78f);

            var poly = t.GetComponent<PolygonCollider2D>();
            if (poly == null)
            {
                Missing.Add("Apt_Colider PolygonCollider2D");
                return;
            }

            poly.pathCount = 1;
            poly.SetPath(0, new[]
            {
                new Vector2(-9.8f, -3f),
                new Vector2(-9.8f, 4f),
                new Vector2(9.8f, 4f),
                new Vector2(9.8f, -3f),
            });
        });

        // 문 4개: 여백 1, 문 1, 간격 4, 문 1, 간격 4, 문 1, 간격 4, 문 1, 여백 1
        string[] doors = { "401", "403", "404", "405" };
        float[] doorX = { 895.5f, 900.5f, 905.5f, 910.5f };

        for (int i = 0; i < doors.Length; i++)
        {
            float x = doorX[i];

            DoScene(scene, "=====S9S2=====/Doors/Door_Corridor_" + doors[i], t =>
            {
                SetPos(t, x, -19f);

                // 문패는 RectTransform이라 anchoredPosition으로 옮긴다. 레이아웃처럼 문 윗부분 안쪽에 온다
                var plate = t.Find("Plate") as RectTransform;
                if (plate != null)
                    plate.anchoredPosition = new Vector2(0f, 0.44f);

                // 그림 속 문 크기에 맞춰 검은 문틈을 씌운다 ('열린 402호 문' 리소스가 오면 교체)
                Transform open = t.Find("OpenVisual");
                if (open != null)
                {
                    SetPos(open, x, -19.02f);
                    SetOpenVisual(open, new Vector2(0.93f, 1.89f), Color.black, "Default", 4);
                }
            });
        }

        // ① 도착은 아래쪽 가운데, ③ 권총 탁자는 2번과 3번 문 사이, 마지막 복도의 하루는 그 아래
        DoScene(scene, "=====S9S2=====/Points/CorridorSpawn", t => t.position = new Vector3(902.5f, -22f, 0f));
        DoScene(scene, "=====S9S2=====/Points/CorridorFinalPoint", t => t.position = new Vector3(902.5f, -21f, 0f));

        DoScene(scene, "=====S9S2=====/Pistol", t =>
        {
            SetPos(t, 902.5f, -20f);

            // 탁자는 권총이 나타나는 회차의 복도에만 있다. 레이아웃 ③ 외의 복도에는 탁자가 없다
            Transform table = FindScene(scene, "=====S9S2=====/PistolTable") ?? t.Find("PistolTable");
            if (table == null)
            {
                Missing.Add("PistolTable");
                return;
            }

            table.SetParent(t, true);
            table.position = new Vector3(902.5f, -20f, table.position.z);
        });

        DoScene(scene, "=====S9S2=====/Soot", t =>
        {
            SetSprite(t, Soot);
            SetWorldScale(t, 0.5f, 0.5f);
            SetPos(t, 904.5f, -21f);
        });
    }

    // ================================================================ 8-1, 8-2 (이미지 3, 4, 5)

    private static void ApplyStatue(Scene scene)
    {
        ApplyGarden(scene);

        // 임시 스프라이트는 3x4칸(바닥 y=7)이었다. 정식은 3x6.125칸이므로 바닥을 맞춰 위로 올린다
        DoScene(scene, "=====Map=====/Objects/Statue", t =>
        {
            SetSprite(t, Statue);
            SetPos(t, 0.5f, 10.0625f);
            SetWorldScale(t, 0.5f, 0.5f);
            SetBox(t.gameObject, new Vector2(-1f, 7f), new Vector2(2f, 11f));
        });

        EditorSceneManager.MarkSceneDirty(scene);
    }

    /// <summary>
    /// 임시 맵(SEQ8_Garden_Tilemap_8-1)과 격자가 같은 31x25칸 정식 맵. x -15~16, y -3~22.
    /// 바닥 위에 생울타리 층을 얹는다. 하루가 울타리 옆에 서도 머리가 가려지지 않게 Player 아래에 둔다.
    /// </summary>
    private static void ApplyGarden(Scene scene)
    {
        DoScene(scene, "SEQ8_Garden_Tilemap_8-1", t =>
        {
            SetSprite(t, GardenBg);
            SetPos(t, 0.5f, 9.5f);
            SetWorldScale(t, 0.5f, 0.5f);

            Transform hedge = EnsureSpriteObject(t, "HedgeTop", GardenHedge, "Object", 0);
            if (hedge != null)
            {
                hedge.localPosition = Vector3.zero;
                hedge.localScale = Vector3.one;
            }
        });

        ApplyGardenWalls(scene);
    }

    /// <summary>
    /// 생울타리와 맵 끝을 막는 벽. 시작 복도의 기존 벽 3개는 그대로 쓴다.
    /// 하루 중심은 x = n + 0.5, y = n + 0.25 칸을 다니고 발은 1칸 아래이므로,
    /// 벽 경계는 칸 사이인 x = 정수, y = n + 0.75 에 둔다. (이동 판정은 중심끼리 잇는 Linecast)
    ///
    ///   y 21.25        x -1.5 ~ 2.5   (커튼 앞)
    ///   y 15.25~20.25  x -7.5 ~ 8.5
    ///   y 12.25~14.25  x -8.5 ~ 9.5
    ///   y  7.25~11.25  x -13.5 ~ 9.5  (서쪽 복도 포함)
    ///   y  6.25        x -8.5 ~ 9.5
    ///   y  4.25~5.25   x -7.5 ~ 8.5
    ///   y -0.75~3.25   x -1.5 ~ 2.5   (시작 복도)
    /// </summary>
    private static void ApplyGardenWalls(Scene scene)
    {
        Transform walls = Room(scene, "=====Map=====/Walls");
        if (walls == null)
            return;

        void Wall(string name, float x0, float y0, float x1, float y1) =>
            WorldWall(walls, "Hedge_S8/" + name, new Vector2(x0, y0), new Vector2(x1, y1));

        Wall("TopCap", -3f, 21.75f, 4f, 22.75f);
        Wall("TopLeft", -9f, 20.75f, -2f, 21.75f);
        Wall("TopRight", 3f, 20.75f, 10f, 21.75f);
        Wall("UpperLeft", -9f, 14.75f, -8f, 20.75f);
        Wall("UpperRight", 9f, 14.75f, 10f, 20.75f);
        Wall("MidLeft", -10f, 11.75f, -9f, 14.75f);
        Wall("MidRight", 10f, 5.75f, 11f, 14.75f);
        Wall("WestTop", -15f, 11.75f, -9f, 12.75f);
        Wall("WestEnd", -15f, 6.75f, -14f, 11.75f);
        Wall("WestBottom", -15f, 5.75f, -9f, 6.75f);
        Wall("LowLeft", -9f, 3.75f, -8f, 5.75f);
        Wall("LowRight", 9f, 3.75f, 10f, 5.75f);
        Wall("BottomLeft", -8f, 2.75f, -2f, 3.75f);
        Wall("BottomRight", 3f, 2.75f, 9f, 3.75f);
    }

    // ================================================================ 10-2 쪼그려 앉은 뒷모습

    /// <summary>
    /// 지금까지 임시로 쓰던 선 자세를 전용 자세로 바꾼다. 발 위치는 그대로 두고, 루나의 발을 하루의 발 높이에 맞춘다.
    /// 씬을 다시 만들지 않으므로 수동으로 옮긴 위치가 그대로 남는다.
    /// </summary>
    private static void ApplySquatPoses(Scene scene)
    {
        var controller = UnityEngine.Object.FindObjectOfType<Sequence10Scene2DialogueController>(true);
        Sprite haruPose = LoadSprite(HaruSquat);
        Sprite lunaPose = LoadSprite(LunaSquat);

        if (controller == null || haruPose == null || lunaPose == null)
        {
            Missing.Add("10-2 컨트롤러 또는 쪼그려 앉은 자세");
            return;
        }

        var so = new SerializedObject(controller);
        so.FindProperty("_haruSitSprite").objectReferenceValue = haruPose;
        so.FindProperty("_lunaSitSprite").objectReferenceValue = lunaPose;
        so.ApplyModifiedPropertiesWithoutUndo();

        var haru = so.FindProperty("_playerMove").objectReferenceValue as Component;
        var luna = so.FindProperty("_luna").objectReferenceValue as Component;
        var sea = so.FindProperty("_lunaSeaPoint").objectReferenceValue as Transform;

        if (haru == null || luna == null)
        {
            Missing.Add("10-2 하루 / 루나");
            return;
        }

        haru.GetComponent<SpriteRenderer>().sprite = haruPose;

        var lunaSr = luna.GetComponentInChildren<SpriteRenderer>(true);
        if (lunaSr != null)
            lunaSr.sprite = lunaPose;

        float lunaY = haru.transform.position.y - 1f;
        float dy = lunaY - luna.transform.position.y;
        luna.transform.position += new Vector3(0f, dy, 0f);

        if (sea != null)
            sea.position += new Vector3(0f, dy, 0f);

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void ApplySequence8Extras(Scene scene)
    {
        DoScene(scene, "=====S8S2=====/StatueRubble", t =>
        {
            SetSprite(t, StatueRubble);
            SetPos(t, 0.5f, 10.0625f);
            SetWorldScale(t, 0.5f, 0.5f);

            if (t.GetComponent<Collider2D>() != null)
                SetBox(t.gameObject, new Vector2(-1f, 7f), new Vector2(2f, 11f));

            UseYSort(t);
        });

        // 640x256 = 5x2칸. 임시 스프라이트와 크기가 같다
        DoScene(scene, "=====S8S2=====/RedCurtain", t =>
        {
            SetSprite(t, Curtain);
            SetWorldScale(t, 0.5f, 0.5f);
            UseYSort(t);
        });

        EditorSceneManager.MarkSceneDirty(scene);
    }

    // ================================================================ 공통

    private static Transform Room(Scene scene, string path)
    {
        Transform t = FindScene(scene, path);
        if (t == null)
            Missing.Add(path);
        return t;
    }

    private static Transform FindScene(Scene scene, string path)
    {
        string[] parts = path.Split(new[] { '/' }, 2);
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == parts[0]);

        if (root == null)
            return null;

        return parts.Length == 1 ? root.transform : root.transform.Find(parts[1]);
    }

    private static void Do(Transform room, string path, Action<Transform> action)
    {
        Transform t = room.Find(path);

        if (t == null)
        {
            Missing.Add(room.name + "/" + path);
            return;
        }

        action(t);
    }

    private static void DoScene(Scene scene, string path, Action<Transform> action)
    {
        Transform t = FindScene(scene, path);

        if (t == null)
        {
            Missing.Add(path);
            return;
        }

        action(t);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
            Missing.Add("스프라이트 없음: " + path);
        return s;
    }

    private static void SetSprite(Transform t, string path, string sortingLayer = null, int order = 0)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        Sprite sprite = LoadSprite(path);

        if (sr == null || sprite == null)
        {
            if (sr == null)
                Missing.Add("SpriteRenderer 없음: " + t.name);
            return;
        }

        sr.sprite = sprite;

        if (sortingLayer != null)
        {
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
        }
    }

    private static void SetOrder(Transform t, int order)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = order;
    }

    /// <summary>
    /// Player 레이어 오브젝트의 y 정렬. 하루의 순서는 -round(y)라서 순서 0에 고정되면
    /// y가 양수인 곳에서 하루가 항상 그 아래로 깔린다.
    /// </summary>
    private static void UseYSort(Transform t)
    {
        if (t.GetComponent<LayerSetting>() == null)
            t.gameObject.AddComponent<LayerSetting>();

        SetOrder(t, -Mathf.RoundToInt(t.position.y));
    }

    private static GameObject EnsureBackdrop(UnityEngine.UI.Image illust)
    {
        Transform parent = illust.transform.parent;
        Transform t = parent.Find("IllustrationBackdrop");

        if (t == null)
        {
            var go = new GameObject("IllustrationBackdrop", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.layer = illust.gameObject.layer;
            t = go.transform;
            t.SetParent(parent, false);
        }

        var rt = (RectTransform)t;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var image = t.GetComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        // 맨 앞 형제 = 캔버스에서 가장 먼저(가장 뒤에) 그려진다. 여러 번 실행해도 순서가 바뀌지 않는다
        t.SetAsFirstSibling();
        t.gameObject.SetActive(false);
        return t.gameObject;
    }

    /// <summary>9슬라이스 임시 사각형의 월드 크기 / 색 / 정렬을 맞춘다.</summary>
    private static void SetOpenVisual(Transform t, Vector2 size, Color color, string layer, int order)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Missing.Add("SpriteRenderer 없음: " + t.name);
            return;
        }

        SetWorldScale(t, 1f, 1f);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = color;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
    }

    /// <summary>z는 그대로 두고 월드 x, y만 옮긴다.</summary>
    private static void SetPos(Transform t, float x, float y) => t.position = new Vector3(x, y, t.position.z);

    private static void SetWorldScale(Transform t, float x, float y)
    {
        Vector3 p = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(x / p.x, y / p.y, 1f);
    }

    /// <summary>회전 없는 오브젝트의 BoxCollider2D를 월드 좌표 범위로 맞춘다.</summary>
    private static void SetBox(GameObject go, Vector2 min, Vector2 max)
    {
        var box = go.GetComponent<BoxCollider2D>();
        if (box == null)
            box = go.AddComponent<BoxCollider2D>();

        Vector3 s = go.transform.lossyScale;
        Vector2 p = go.transform.position;

        box.size = new Vector2((max.x - min.x) / Mathf.Abs(s.x), (max.y - min.y) / Mathf.Abs(s.y));
        box.offset = new Vector2(((max.x + min.x) * 0.5f - p.x) / s.x, ((max.y + min.y) * 0.5f - p.y) / s.y);
        box.enabled = true;
    }

    /// <summary>스프라이트 없는 NoPass 벽. 경로의 중간 그룹이 없으면 만든다. 위치 / 스케일은 부모와 상관없이 월드 기준이다.</summary>
    private static void WorldWall(Transform parent, string path, Vector2 min, Vector2 max)
    {
        Transform t = EnsurePath(parent, path);
        t.position = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
        t.rotation = Quaternion.identity;
        t.gameObject.layer = NoPassLayer;
        SetWorldScale(t, 1f, 1f);
        SetBox(t.gameObject, min, max);
    }

    private static Transform EnsurePath(Transform parent, string path)
    {
        Transform cur = parent;

        foreach (string part in path.Split('/'))
        {
            Transform next = cur.Find(part);

            if (next == null)
            {
                next = new GameObject(part).transform;
                next.SetParent(cur, false);
            }

            cur = next;
        }

        return cur;
    }

    private static Transform EnsureSpriteObject(Transform parent, string path, string spritePath, string layer, int order)
    {
        Transform t = EnsurePath(parent, path);
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = t.gameObject.AddComponent<SpriteRenderer>();

        Sprite sprite = LoadSprite(spritePath);
        if (sprite == null)
            return null;

        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return t;
    }

}
