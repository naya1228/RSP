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

        // 결투 중에는 턴과 상관없이 내가 패를 낼 수 있어야 함
        if (gm.GetHandCount(0, hand) <= 0) return;
        
        gm.RequestHand(0, hand);

        SetStatusText("선택 완료! 상대방의 결정을 기다리는 중...");
        
        // 내가 패를 냈으므로 버튼 비활성화 (결과 나올 때까지)
        DisableHandButtons();
    }

    private void OnMovePressed(MoveDirection dir)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.CurrentState != GameManager.GameState.Moving) return;
        
        // 이동은 내 차례일 때만 가능
        if (gm.CurrentTurnPlayer != 0) return;
        
        gm.RequestMove(0, dir);
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        RefreshButtonStates();

        if (state == GameManager.GameState.Duel)
            SetStatusText("결투! 패를 선택하세요");
        else if (state == GameManager.GameState.Moving)
            SetStatusText(GameManager.Instance.CurrentTurnPlayer == 0 ? "나의 이동 차례" : "상대방 이동 중...");
        else if (state == GameManager.GameState.GameOver)
            SetStatusText("게임 종료");
    }

    private void OnTurnChanged(int playerId)
    {
        RefreshButtonStates();
        
        if (GameManager.Instance.CurrentState == GameManager.GameState.Moving)
            SetStatusText(playerId == 0 ? "나의 차례" : "상대방 차례...");
    }

    private void RefreshButtonStates()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        bool duel = gm.CurrentState == GameManager.GameState.Duel;
        bool moving = gm.CurrentState == GameManager.GameState.Moving;
        bool isMyTurn = gm.CurrentTurnPlayer == 0;

        // 결투 버튼: 결투 상태이면 턴과 상관없이 활성화
        if (_rockBtn != null) _rockBtn.Disabled = !duel;
        if (_paperBtn != null) _paperBtn.Disabled = !duel;
        if (_scissorsBtn != null) _scissorsBtn.Disabled = !duel;

        // 이동 버튼: 내 이동 차례일 때만 활성화
        if (_forwardBtn != null) _forwardBtn.Disabled = !(moving && isMyTurn);
        if (_backwardBtn != null) _backwardBtn.Disabled = !(moving && isMyTurn);
    }

    private void DisableHandButtons()
    {
        if (_rockBtn != null) _rockBtn.Disabled = true;
        if (_paperBtn != null) _paperBtn.Disabled = true;
        if (_scissorsBtn != null) _scissorsBtn.Disabled = true;
    }

    private void OnDuelResolved(int p0, HandType h0, int p1, HandType h1, int winner)
    {
        string result = winner == -1
            ? $"비김! (나:{h0} vs 상대:{h1})"
            : (winner == 0 ? $"승리! (나:{h0} vs 상대:{h1})" : $"패배! (나:{h0} vs 상대:{h1})");
        if (_resultLabel != null) _resultLabel.Text = result;
    }

    private void OnGameOver(int winner)
    {
        SetStatusText(winner == 0 ? "최종 승리!" : "최종 패배...");
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

        // 나(A): 구체적으로 표시
        if (_handsALabel != null)
            _handsALabel.Text = $"내 패 - 바위:{gm.GetHandCount(0, HandType.Rock)} 보:{gm.GetHandCount(0, HandType.Paper)} 가위:{gm.GetHandCount(0, HandType.Scissors)}";

        // 상대(B): 총합만 표시 (정보 은폐)
        if (_handsBLabel != null)
        {
            int totalB = gm.GetHandCount(1, HandType.Rock) + gm.GetHandCount(1, HandType.Paper) + gm.GetHandCount(1, HandType.Scissors);
            _handsBLabel.Text = $"상대방 남은 패: {totalB}장";
        }

        if (_streakLabel != null)
            _streakLabel.Text = $"나의 연패: {gm.LoseStreak[0]} / 상대 연패: {gm.LoseStreak[1]}";
    }
}
