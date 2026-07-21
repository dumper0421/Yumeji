using UnityEngine;

/// <summary>
/// [7-2] 기획자용 테스트 패널.
/// 씬의 빈 오브젝트에 이 스크립트 하나만 붙이면 됩니다. 연결할 것 없음.
/// 게임을 실행한 뒤 F1 키를 누르면 화면에 조절 패널이 나옵니다.
///
/// - 마네킹 이동 속도 / 정지 시간을 실시간으로 조절
/// - 마네킹 개수(활성화된 세트 수)를 실시간으로 조절
/// - 무적 모드, 불 켜기, 전력 즉시 복구 등 테스트 단축 기능
///
/// 조절한 값은 패널 맨 위의 '지금 값 저장하기' 버튼으로
/// Assets/Resources/S7S2GimmickSettings.asset 에 기록됩니다.
/// 에셋 파일이라 플레이 모드를 빠져나가도 값이 유지됩니다.
/// </summary>
public class S7S2TestPanel : MonoBehaviour
{
    [Header("패널 열기/닫기 키")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;

    [Tooltip("체크하면 게임 시작하자마자 패널이 열려 있습니다")]
    [SerializeField] private bool _openOnStart = false;

    [Header("연결 (비워두면 자동으로 찾습니다)")]
    [SerializeField] private Sequence7Scene2DialogueController _dialogueController;
    [SerializeField] private ItemData _cutterData;

    private bool _open;

    private WaltzMannequinSet[] _waltzSets;
    private TriggeredMannequin[] _triggers;
    private MannequinObstacle[] _obstacles;

    // 슬라이더 값
    private float _waltzSpeed = 2f;
    private float _stillDuration = 2f;
    private float _shakeDuration = 1f;
    private int _activeWaltzCount;

    private float _moveOutDuration = 0.25f;
    private float _holdDuration = 3f;
    private float _returnDuration = 0.4f;

    private Vector2 _scroll;
    private GUIStyle _headerStyle;
    private GUIStyle _labelStyle;
    private string _lastMessage = "";
    private bool _lightsOn = false;
    private bool _dirty = false;
    private string _saveMessage = "";

    private void Start()
    {
        S7S2Test.Reset();
        _open = _openOnStart;
        Refresh();
        ApplyCursorState();
    }

    /// <summary>씬의 기믹 오브젝트를 다시 찾고 현재 값을 읽어온다.</summary>
    private void Refresh()
    {
        _waltzSets = FindObjectsOfType<WaltzMannequinSet>(true);
        _triggers = FindObjectsOfType<TriggeredMannequin>(true);
        _obstacles = FindObjectsOfType<MannequinObstacle>(true);

        if (_dialogueController == null)
            _dialogueController = FindObjectOfType<Sequence7Scene2DialogueController>();

        if (_waltzSets.Length > 0)
        {
            _waltzSpeed = _waltzSets[0].MoveSpeed;
            _stillDuration = _waltzSets[0].StillDuration;
            _shakeDuration = _waltzSets[0].ShakeDuration;
        }

        if (_triggers.Length > 0)
        {
            _moveOutDuration = _triggers[0].MoveOutDuration;
            _holdDuration = _triggers[0].HoldDuration;
            _returnDuration = _triggers[0].ReturnDuration;
        }

        _activeWaltzCount = 0;
        foreach (var set in _waltzSets)
        {
            if (set != null && set.gameObject.activeSelf)
                _activeWaltzCount++;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            _open = !_open;
            if (_open) Refresh();
            ApplyCursorState();
        }

        if (!_open) return;

        // 키보드만으로도 조작할 수 있게: 패널이 열려 있는 동안 단축키 제공
        HandleHotkeys();
    }

    /// <summary>
    /// 이 게임은 GameManager가 커서를 잠그므로(Cursor.lockState = Locked),
    /// 패널이 열려 있는 동안만 커서를 풀어 마우스로 조작할 수 있게 한다.
    /// </summary>
    private void ApplyCursorState()
    {
        if (_open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        // 패널이 열린 채로 씬이 바뀌어도 커서가 풀린 상태로 남지 않게 복구
        if (_open)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>마우스 없이 키보드만으로 주요 기능을 쓰기 위한 단축키.</summary>
    private void HandleHotkeys()
    {
        // 무적 토글
        if (Input.GetKeyDown(KeyCode.F2))
            S7S2Test.Invincible = !S7S2Test.Invincible;

        // 왈츠 즉시 시작
        if (Input.GetKeyDown(KeyCode.F3))
            ForceStartWaltz();

        // 불 켜기/끄기 (마네킹 유지)
        if (Input.GetKeyDown(KeyCode.F4))
            SetLights(!_lightsOn);

        // 전력 복구 (마네킹 삭제까지)
        if (Input.GetKeyDown(KeyCode.F6) && _dialogueController != null)
        {
            _dialogueController.OnPowerRestored();
            _lightsOn = true;
        }

        // 절단기 획득
        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (_cutterData != null && InventoryManager.Instance != null)
                InventoryManager.Instance.AddItem(_cutterData);
        }

        // 이동 속도 조절 (- / +)
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            SetWaltzSpeed(_waltzSpeed - 0.2f);
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            SetWaltzSpeed(_waltzSpeed + 0.2f);

        // 마네킹 개수 조절 ( [ / ] )
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            _activeWaltzCount = Mathf.Max(0, _activeWaltzCount - 1);
            ApplyWaltzCount();
        }
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            _activeWaltzCount = Mathf.Min(_waltzSets.Length, _activeWaltzCount + 1);
            ApplyWaltzCount();
        }
    }

    /// <summary>
    /// 왈츠를 강제로 시작하고, 안 움직이면 그 이유를 화면에 알려준다.
    /// 꺼져 있는 세트는 켜고, 대사 때문에 멈춰 있으면 대사 무시도 함께 켠다.
    /// </summary>
    private void ForceStartWaltz()
    {
        Refresh();

        if (_waltzSets == null || _waltzSets.Length == 0)
        {
            _lastMessage = "▶ 씬에 왈츠 마네킹이 하나도 없습니다.\n" +
                           "   마네킹에 WaltzMannequinSet 스크립트를 붙였는지 확인하세요.";
            Debug.LogWarning("[7-2 테스트] 왈츠 마네킹 세트를 찾지 못했습니다.");
            return;
        }

        int started = 0;
        int noWaypoint = 0;

        foreach (var set in _waltzSets)
        {
            if (set == null) continue;

            // 꺼져 있으면 켜기
            if (!set.gameObject.activeSelf)
                set.gameObject.SetActive(true);

            set.Activate();

            if (set.WaypointCount == 0)
                noWaypoint++;
            else
                started++;
        }

        _activeWaltzCount = _waltzSets.Length;

        // 대사 때문에 멈춰 있으면 대사 무시를 자동으로 켠다
        S7S2Test.IgnoreDialoguePause = true;

        if (noWaypoint > 0)
        {
            _lastMessage =
                $"▶ {noWaypoint}개 세트에 동선 포인트가 없습니다!\n" +
                "   마네킹을 선택하고 인스펙터의\n" +
                "   '동선 포인트 만들기' 버튼을 눌러주세요.";
            Debug.LogWarning(
                $"[7-2 테스트] 동선 포인트가 없는 왈츠 세트 {noWaypoint}개. " +
                "인스펙터에서 '동선 포인트 만들기'를 눌러주세요.");
        }
        else
        {
            _lastMessage = $"▶ {started}개 세트 시작됨 (대사 무시 켜짐)";
        }
    }

    /// <summary>진행 상태는 그대로 두고 조명만 켜고 끈다.</summary>
    private void SetLights(bool on)
    {
        if (_dialogueController == null)
        {
            _lastMessage = "▶ 씬 대화 컨트롤러를 찾지 못해 불을 켤 수 없습니다.";
            return;
        }

        _lightsOn = on;
        _dialogueController.TestSetLights(on);
    }

    private void SetWaltzSpeed(float value)
    {
        _waltzSpeed = Mathf.Clamp(value, 0.2f, 8f);
        _dirty = true;
        foreach (var set in _waltzSets)
        {
            if (set != null) set.MoveSpeed = _waltzSpeed;
        }
    }

    private void OnGUI()
    {
        if (!_open)
        {
            GUI.Label(new Rect(10, 10, 300, 24), $"[{_toggleKey}] 테스트 패널 열기");
            return;
        }

        EnsureStyles();

        GUILayout.BeginArea(new Rect(10, 10, 380, Screen.height - 20), GUI.skin.box);
        _scroll = GUILayout.BeginScrollView(_scroll);

        DrawSaveSection();
        GUILayout.Space(8);

        GUILayout.Label("■ [7-2] 테스트 패널", _headerStyle);
        GUILayout.Label($"닫기: {_toggleKey} 키 (닫으면 마우스 다시 잠김)", _labelStyle);
        GUILayout.Space(4);

        GUILayout.Label("키보드 단축키", _headerStyle);
        GUILayout.Label(
            "F2 무적  |  F3 왈츠시작  |  F4 불 켜기/끄기\n" +
            "F5 절단기  |  F6 전력복구(마네킹 삭제)\n" +
            "- / +  이동속도    [ / ]  마네킹 개수", _labelStyle);
        GUILayout.Space(6);

        DrawWaltzSection();
        GUILayout.Space(10);

        DrawTriggerSection();
        GUILayout.Space(10);

        DrawCheatSection();
        GUILayout.Space(10);

        DrawInfoSection();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // ---------- 수치 저장 ----------
    private void DrawSaveSection()
    {
        var settings = S7S2GimmickSettings.Get();

        if (settings == null)
        {
            GUILayout.Label("설정 파일 없음 — 바꾼 값이 저장되지 않습니다", _headerStyle);
            GUILayout.Label(
                "Assets/Resources/S7S2GimmickSettings.asset 이 있는지 확인하세요.", _labelStyle);
            return;
        }

        GUILayout.Label("■ 수치 저장", _headerStyle);
        GUILayout.Label(
            _dirty
                ? "● 바뀐 값이 있습니다. 저장하지 않으면 사라집니다."
                : "저장된 값과 같습니다.",
            _labelStyle);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("지금 값 저장하기", GUILayout.Height(28)))
            SaveSettings();

        if (GUILayout.Button("기획서 기준값으로", GUILayout.Height(28)))
        {
            settings.ResetToSpec();
            PullFromSettings(settings);
            PushToComponents();
            _dirty = true;
            _saveMessage = "기획서 기준값으로 되돌렸습니다. 저장하려면 '지금 값 저장하기'를 누르세요.";
        }

        GUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_saveMessage))
            GUILayout.Label(_saveMessage, _labelStyle);
    }

    /// <summary>
    /// 현재 값을 설정 에셋에 기록한다.
    /// 에셋은 씬이 아니라 파일이므로, 플레이 모드를 빠져나가도 값이 남는다.
    /// </summary>
    private void SaveSettings()
    {
        var settings = S7S2GimmickSettings.Get();
        if (settings == null)
        {
            _saveMessage = "설정 파일을 찾지 못해 저장하지 못했습니다.";
            return;
        }

        settings.waltzMoveSpeed = _waltzSpeed;
        settings.waltzStillDuration = _stillDuration;
        settings.waltzShakeDuration = _shakeDuration;
        settings.mazeMoveOutDuration = _moveOutDuration;
        settings.mazeHoldDuration = _holdDuration;
        settings.mazeReturnDuration = _returnDuration;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(settings);
        UnityEditor.AssetDatabase.SaveAssets();
        _dirty = false;
        _saveMessage = "저장 완료. 게임을 껐다 켜도 이 값이 유지됩니다.";
        Debug.Log(
            $"[7-2] 수치 저장됨 — 왈츠 속도 {_waltzSpeed:0.00}, " +
            $"멈춤 {_stillDuration:0.0}초, 흔들림 {_shakeDuration:0.0}초 / " +
            $"미로 {_moveOutDuration:0.00}·{_holdDuration:0.0}·{_returnDuration:0.00}초");
#else
        _saveMessage = "빌드된 게임에서는 저장할 수 없습니다. 유니티 에디터에서 조절해주세요.";
#endif
    }

    private void PullFromSettings(S7S2GimmickSettings settings)
    {
        _waltzSpeed = settings.waltzMoveSpeed;
        _stillDuration = settings.waltzStillDuration;
        _shakeDuration = settings.waltzShakeDuration;
        _moveOutDuration = settings.mazeMoveOutDuration;
        _holdDuration = settings.mazeHoldDuration;
        _returnDuration = settings.mazeReturnDuration;
    }

    /// <summary>슬라이더 값을 씬의 모든 마네킹에 적용한다.</summary>
    private void PushToComponents()
    {
        if (_waltzSets != null)
        {
            foreach (var set in _waltzSets)
            {
                if (set == null) continue;
                set.MoveSpeed = _waltzSpeed;
                set.StillDuration = _stillDuration;
                set.ShakeDuration = _shakeDuration;
            }
        }

        if (_triggers != null)
        {
            foreach (var t in _triggers)
            {
                if (t == null) continue;
                t.MoveOutDuration = _moveOutDuration;
                t.HoldDuration = _holdDuration;
                t.ReturnDuration = _returnDuration;
            }
        }
    }

    // ---------- 기믹 1) 왈츠 ----------
    private void DrawWaltzSection()
    {
        GUILayout.Label("── 기믹 1) 마네킹 왈츠 ──", _headerStyle);

        if (_waltzSets == null || _waltzSets.Length == 0)
        {
            GUILayout.Label("씬에 왈츠 마네킹이 없습니다.", _labelStyle);
            return;
        }

        // 이동 속도
        GUILayout.Label($"이동 속도: {_waltzSpeed:0.00}   (기본 2.00)", _labelStyle);
        float newSpeed = GUILayout.HorizontalSlider(_waltzSpeed, 0.2f, 8f);
        if (!Mathf.Approximately(newSpeed, _waltzSpeed))
        {
            _waltzSpeed = newSpeed;
            _dirty = true;
            foreach (var set in _waltzSets)
            {
                if (set != null) set.MoveSpeed = _waltzSpeed;
            }
        }

        // 정지 시간
        GUILayout.Label($"촬영 후 멈춤: {_stillDuration:0.0}초   (기획 2.0초)", _labelStyle);
        float newStill = GUILayout.HorizontalSlider(_stillDuration, 0f, 8f);
        if (!Mathf.Approximately(newStill, _stillDuration))
        {
            _stillDuration = newStill;
            _dirty = true;
            foreach (var set in _waltzSets)
            {
                if (set != null) set.StillDuration = _stillDuration;
            }
        }

        // 흔들림 시간
        GUILayout.Label($"흔들림: {_shakeDuration:0.0}초   (기획 1.0초)", _labelStyle);
        float newShake = GUILayout.HorizontalSlider(_shakeDuration, 0f, 4f);
        if (!Mathf.Approximately(newShake, _shakeDuration))
        {
            _shakeDuration = newShake;
            _dirty = true;
            foreach (var set in _waltzSets)
            {
                if (set != null) set.ShakeDuration = _shakeDuration;
            }
        }

        GUILayout.Space(4);

        // 마네킹 개수
        GUILayout.Label(
            $"마네킹 세트 개수: {_activeWaltzCount} / {_waltzSets.Length}", _labelStyle);

        int newCount = Mathf.RoundToInt(
            GUILayout.HorizontalSlider(_activeWaltzCount, 0, _waltzSets.Length));

        if (newCount != _activeWaltzCount)
        {
            _activeWaltzCount = newCount;
            ApplyWaltzCount();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("전부 켜기"))
        {
            _activeWaltzCount = _waltzSets.Length;
            ApplyWaltzCount();
        }
        if (GUILayout.Button("전부 끄기"))
        {
            _activeWaltzCount = 0;
            ApplyWaltzCount();
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>앞에서부터 N개만 켜고 나머지는 끈다.</summary>
    private void ApplyWaltzCount()
    {
        for (int i = 0; i < _waltzSets.Length; i++)
        {
            if (_waltzSets[i] != null)
                _waltzSets[i].gameObject.SetActive(i < _activeWaltzCount);
        }
    }

    // ---------- 기믹 2) 미로 ----------
    private void DrawTriggerSection()
    {
        GUILayout.Label("── 기믹 2) 마네킹 미로 ──", _headerStyle);

        if (_triggers == null || _triggers.Length == 0)
        {
            GUILayout.Label("씬에 트리거 마네킹이 없습니다.", _labelStyle);
            return;
        }

        GUILayout.Label($"나가는 시간: {_moveOutDuration:0.00}초   (기획 0.25초)", _labelStyle);
        float newOut = GUILayout.HorizontalSlider(_moveOutDuration, 0.05f, 2f);
        if (!Mathf.Approximately(newOut, _moveOutDuration))
        {
            _moveOutDuration = newOut;
            _dirty = true;
            foreach (var t in _triggers)
            {
                if (t != null) t.MoveOutDuration = _moveOutDuration;
            }
        }

        GUILayout.Label($"멈춰 있는 시간: {_holdDuration:0.0}초   (기획 3.0초)", _labelStyle);
        float newHold = GUILayout.HorizontalSlider(_holdDuration, 0f, 10f);
        if (!Mathf.Approximately(newHold, _holdDuration))
        {
            _holdDuration = newHold;
            _dirty = true;
            foreach (var t in _triggers)
            {
                if (t != null) t.HoldDuration = _holdDuration;
            }
        }

        GUILayout.Label($"돌아오는 시간: {_returnDuration:0.00}초   (기획 0.4초)", _labelStyle);
        float newReturn = GUILayout.HorizontalSlider(_returnDuration, 0.05f, 2f);
        if (!Mathf.Approximately(newReturn, _returnDuration))
        {
            _returnDuration = newReturn;
            _dirty = true;
            foreach (var t in _triggers)
            {
                if (t != null) t.ReturnDuration = _returnDuration;
            }
        }
    }

    // ---------- 테스트 단축 기능 ----------
    private void DrawCheatSection()
    {
        GUILayout.Label("── 테스트 기능 ──", _headerStyle);

        bool newInvincible = GUILayout.Toggle(
            S7S2Test.Invincible, " 무적 (마네킹에 닿아도 안 죽음)");
        if (newInvincible != S7S2Test.Invincible)
            S7S2Test.Invincible = newInvincible;

        GUILayout.Space(4);

        bool ignoreDialogue = GUILayout.Toggle(
            S7S2Test.IgnoreDialoguePause, " 대사 중에도 마네킹 움직이기");
        if (ignoreDialogue != S7S2Test.IgnoreDialoguePause)
            S7S2Test.IgnoreDialoguePause = ignoreDialogue;

        if (GUILayout.Button("왈츠 즉시 시작", GUILayout.Height(24)))
            ForceStartWaltz();

        if (!string.IsNullOrEmpty(_lastMessage))
            GUILayout.Label(_lastMessage, _labelStyle);

        // 불만 켜기 — 마네킹을 남겨둔 채 밝게 해서 동선을 확인할 때
        GUILayout.BeginHorizontal();
        GUILayout.Label(_lightsOn ? "불: 켜짐" : "불: 꺼짐 (어두움)", _labelStyle);

        if (GUILayout.Button(_lightsOn ? "불 끄기" : "불 켜기"))
            SetLights(!_lightsOn);

        if (GUILayout.Button("원래대로"))
        {
            if (_dialogueController != null)
                _dialogueController.TestRestoreLights();

            _lightsOn = _dialogueController != null && _dialogueController.IsPowerRestored;
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("※ 불만 켜므로 마네킹은 그대로 남습니다", _labelStyle);
        GUILayout.Space(4);

        if (GUILayout.Button("전력 복구 (불 켜고 마네킹 삭제)"))
        {
            if (_dialogueController != null)
            {
                _dialogueController.OnPowerRestored();
                _lightsOn = true;
            }
        }

        if (GUILayout.Button("비상용 절단기 즉시 획득"))
        {
            if (_cutterData != null && InventoryManager.Instance != null)
                InventoryManager.Instance.AddItem(_cutterData);
            else
                Debug.LogWarning("[테스트] 절단기 ItemData가 연결되지 않았습니다.");
        }

        if (GUILayout.Button("씬 처음부터 다시 시작"))
        {
            S7S2Test.Reset();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        if (GUILayout.Button("오브젝트 다시 찾기 (새로고침)"))
            Refresh();
    }

    // ---------- 현재 상태 ----------
    private void DrawInfoSection()
    {
        GUILayout.Label("── 현재 상태 ──", _headerStyle);

        int frozen = 0;
        int active = 0;
        if (_waltzSets != null)
        {
            foreach (var set in _waltzSets)
            {
                if (set == null || !set.gameObject.activeSelf) continue;
                if (set.IsActive) active++;
                if (set.IsFrozen) frozen++;
            }
        }

        int running = 0;
        if (_triggers != null)
        {
            foreach (var t in _triggers)
            {
                if (t != null && t.IsRunning) running++;
            }
        }

        GUILayout.Label($"움직이는 중인 왈츠 세트: {active}개", _labelStyle);
        GUILayout.Label($"촬영으로 멈춘 세트: {frozen}개", _labelStyle);
        GUILayout.Label($"작동 중인 트리거: {running}개", _labelStyle);
        GUILayout.Label($"기본 마네킹(장애물): {(_obstacles != null ? _obstacles.Length : 0)}개", _labelStyle);

        // 세트별 상태 — 안 움직일 때 원인을 바로 확인할 수 있다
        GUILayout.Space(6);
        GUILayout.Label("세트별 상태 (안 움직이면 여기 확인)", _headerStyle);

        if (_waltzSets == null || _waltzSets.Length == 0)
        {
            GUILayout.Label("왈츠 마네킹이 없습니다.", _labelStyle);
            return;
        }

        foreach (var set in _waltzSets)
        {
            if (set == null) continue;

            string reason = set.GetBlockReason();
            bool isProblem = reason.EndsWith("!");

            var style = new GUIStyle(_labelStyle);
            style.normal.textColor = isProblem
                ? new Color(1f, 0.45f, 0.45f)
                : (reason == "이동 중" ? new Color(0.6f, 1f, 0.6f) : _labelStyle.normal.textColor);

            GUILayout.Label($"· {set.name}  [포인트 {set.WaypointCount}개]  {reason}", style);
        }

        GUILayout.Space(6);
        GUILayout.Label("조작: 이동 방향키 / 상호작용 Space / 촬영 C", _labelStyle);
    }

    private void EnsureStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
            };
            _headerStyle.normal.textColor = Color.white;
        }

        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            _labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        }
    }
}
