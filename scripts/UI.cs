using Godot;

// 게임 UI: 가위/바위/보 버튼, 이동 버튼, 상태 표시
public partial class UI : CanvasLayer
{
    private Button _rockBtn;
    private Button _paperBtn;
    private Button _scissorsBtn;
    private Button _forwardBtn;
    private Button _backwardBtn;

    private Label _statusLabel;
    private Label _resultLabel;
    private Label _handsALabel;
    private Label _handsBLabel;
    private Label _streakLabel;

    public override void _Ready()
    {
        _rockBtn = GetNodeOrNull<Button>("Root/HandPanel/RockButton");
        _paperBtn = GetNodeOrNull<Button>("Root/HandPanel/PaperButton");
        _scissorsBtn = GetNodeOrNull<Button>("Root/HandPanel/ScissorsButton");
        _forwardBtn = GetNodeOrNull<Button>("Root/MovePanel/ForwardButton");
        _backwardBtn = GetNodeOrNull<Button>("Root/MovePanel/BackwardButton");

        _statusLabel = GetNodeOrNull<Label>("Root/StatusLabel");
        _resultLabel = GetNodeOrNull<Label>("Root/ResultLabel");
        _handsALabel = GetNodeOrNull<Label>("Root/HandsALabel");
        _handsBLabel = GetNodeOrNull<Label>("Root/HandsBLabel");
        _streakLabel = GetNodeOrNull<Label>("Root/StreakLabel");

        if (_rockBtn != null) _rockBtn.Pressed += () => OnHandPressed(HandType.Rock);
        if (_paperBtn != null) _paperBtn.Pressed += () => OnHandPressed(HandType.Paper);
        if (_scissorsBtn != null) _scissorsBtn.Pressed += () => OnHandPressed(HandType.Scissors);
        if (_forwardBtn != null) _forwardBtn.Pressed += () => OnMovePressed(MoveDirection.Forward);
        if (_backwardBtn != null) _backwardBtn.Pressed += () => OnMovePressed(MoveDirection.Backward);

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnStateChanged += OnStateChanged;
            gm.OnBoardChanged += RefreshLabels;
            gm.OnTurnChanged += OnTurnChanged;
            gm.OnDuelResolved += OnDuelResolved;
            gm.OnGameOver += OnGameOver;
            RefreshLabels();
            OnStateChanged(gm.CurrentState);
        }
    }

    public override void _ExitTree()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnStateChanged -= OnStateChanged;
            gm.OnBoardChanged -= RefreshLabels;
            gm.OnTurnChanged -= OnTurnChanged;
            gm.OnDuelResolved -= OnDuelResolved;
            gm.OnGameOver -= OnGameOver;
        }
    }

    private void OnHandPressed(HandType hand)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.CurrentState != GameManager.GameState.Duel) return;

        // 로컬 2인 모드: 현재 턴 플레이어가 낸다고 가정
        int p = gm.CurrentTurnPlayer;
        if (gm.GetHandCount(p, hand) <= 0) return;
        gm.SubmitHand(p, hand);

        // 듀얼 모드에서 한 명 제출 후에는 상대 차례로 전환
        if (gm.CurrentState == GameManager.GameState.Duel)
        {
            // 다른 플레이어에게 입력 권한 넘기기 (로컬 2인 교대)
            // GameManager 내부에서 CurrentTurnPlayer가 유지됨, 여기선 임시 토글만
            SetStatusText($"Player {1 - p} - 패 선택");
        }
    }

    private void OnMovePressed(MoveDirection dir)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.CurrentState != GameManager.GameState.Moving) return;
        gm.TryMove(gm.CurrentTurnPlayer, dir);
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        bool duel = state == GameManager.GameState.Duel;
        bool moving = state == GameManager.GameState.Moving;

        if (_rockBtn != null) _rockBtn.Disabled = !duel;
        if (_paperBtn != null) _paperBtn.Disabled = !duel;
        if (_scissorsBtn != null) _scissorsBtn.Disabled = !duel;
        if (_forwardBtn != null) _forwardBtn.Disabled = !moving;
        if (_backwardBtn != null) _backwardBtn.Disabled = !moving;

        if (state == GameManager.GameState.Duel)
            SetStatusText("결투! 패를 선택하세요");
        else if (state == GameManager.GameState.Moving)
            SetStatusText($"Player {GameManager.Instance.CurrentTurnPlayer} 이동 차례");
        else if (state == GameManager.GameState.GameOver)
            SetStatusText("게임 종료");
    }

    private void OnTurnChanged(int playerId)
    {
        SetStatusText($"Player {playerId} 차례");
    }

    private void OnDuelResolved(int p0, HandType h0, int p1, HandType h1, int winner)
    {
        string result = winner == -1
            ? $"비김! ({h0} vs {h1})"
            : $"Player {winner} 승리 ({h0} vs {h1})";
        if (_resultLabel != null) _resultLabel.Text = result;
    }

    private void OnGameOver(int winner)
    {
        SetStatusText($"게임 종료! Player {winner} 최종 승리");
        if (_rockBtn != null) _rockBtn.Disabled = true;
        if (_paperBtn != null) _paperBtn.Disabled = true;
        if (_scissorsBtn != null) _scissorsBtn.Disabled = true;
        if (_forwardBtn != null) _forwardBtn.Disabled = true;
        if (_backwardBtn != null) _backwardBtn.Disabled = true;
    }

    private void SetStatusText(string text)
    {
        if (_statusLabel != null) _statusLabel.Text = text;
    }

    private void RefreshLabels()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (_handsALabel != null)
            _handsALabel.Text = $"A 패 - 바위:{gm.GetHandCount(0, HandType.Rock)} 보:{gm.GetHandCount(0, HandType.Paper)} 가위:{gm.GetHandCount(0, HandType.Scissors)}";
        if (_handsBLabel != null)
            _handsBLabel.Text = $"B 패 - 바위:{gm.GetHandCount(1, HandType.Rock)} 보:{gm.GetHandCount(1, HandType.Paper)} 가위:{gm.GetHandCount(1, HandType.Scissors)}";
        if (_streakLabel != null)
            _streakLabel.Text = $"연패 A:{gm.LoseStreak[0]} / B:{gm.LoseStreak[1]}";
    }
}
