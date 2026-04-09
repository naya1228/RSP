using System.Collections.Generic;
using System.Linq;
using Godot;

// 게임 UI: 가위/바위/보 버튼, 이동 버튼, 상태 표시
public partial class UI : CanvasLayer
{
    private HBoxContainer _handPanel;
    private readonly List<Button> _cardButtons = new List<Button>();

    private Label _statusLabel;
    private Label _resultLabel;
    private Label _handsALabel;
    private Label _handsBLabel;
    private Label _streakLabel;

    public override void _Ready()
    {
        _handPanel = GetNodeOrNull<HBoxContainer>("Root/HandPanel");

        _statusLabel = GetNodeOrNull<Label>("Root/StatusLabel");
        _resultLabel = GetNodeOrNull<Label>("Root/ResultLabel");
        _handsALabel = GetNodeOrNull<Label>("Root/HandsALabel");
        _handsBLabel = GetNodeOrNull<Label>("Root/HandsBLabel");
        _streakLabel = GetNodeOrNull<Label>("Root/StreakLabel");

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnStateChanged += OnStateChanged;
            gm.OnBoardChanged += RefreshLabels;
            gm.OnTurnChanged += OnTurnChanged;
            gm.OnDuelResolved += OnDuelResolved;
            gm.OnGameOver += OnGameOver;
            gm.OnCardDrawn += OnCardDrawn;
            RefreshLabels();
            RefreshHandCards();
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
            gm.OnCardDrawn -= OnCardDrawn;
        }
    }

    private void OnHandPressed(HandType hand)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.CurrentState != GameManager.GameState.Duel) return;

        // 결투 중에는 턴과 상관없이 내가 패를 낼 수 있어야 함
        if (!gm.GetHand(GameManager.PlayerA).Contains(hand)) return;

        gm.RequestHand(GameManager.PlayerA, hand);

        SetStatusText("선택 완료! 상대방의 결정을 기다리는 중...");

        // 내가 패를 냈으므로 버튼 비활성화 (결과 나올 때까지)
        DisableHandButtons();
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        RefreshHandCards();
        RefreshButtonStates();

        if (state == GameManager.GameState.Duel)
            SetStatusText("결투! 패를 선택하세요");
        else if (state == GameManager.GameState.Moving)
            SetStatusText(GameManager.Instance.CurrentTurnPlayer == GameManager.PlayerA ? "나의 이동 차례" : "상대방 이동 중...");
        else if (state == GameManager.GameState.GameOver)
            SetStatusText("게임 종료");
    }

    private void OnTurnChanged(int playerId)
    {
        RefreshButtonStates();

        if (GameManager.Instance.CurrentState == GameManager.GameState.Moving)
            SetStatusText(playerId == GameManager.PlayerA ? "나의 차례" : "상대방 차례...");
    }

    private void OnCardDrawn(int playerId)
    {
        if (playerId == GameManager.PlayerA) RefreshHandCards();
    }

    private void RefreshHandCards()
    {
        if (_handPanel == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        // 기존 카드 버튼 제거
        foreach (var btn in _cardButtons)
            btn.QueueFree();
        _cardButtons.Clear();

        bool duel = gm.CurrentState == GameManager.GameState.Duel;
        var hand = gm.GetHand(GameManager.PlayerA);

        foreach (var cardType in hand)
        {
            var btn = new Button();
            btn.CustomMinimumSize = new Vector2(100, 70);
            btn.Text = cardType switch {
                HandType.Rock => "바위",
                HandType.Paper => "보",
                HandType.Scissors => "가위",
                _ => "?"
            };

            // 색상 구분
            var style = new StyleBoxFlat();
            style.BgColor = cardType switch {
                HandType.Rock => new Color(0.5f, 0.5f, 0.5f),
                HandType.Paper => new Color(0.3f, 0.5f, 0.9f),
                HandType.Scissors => new Color(0.9f, 0.3f, 0.3f),
                _ => new Color(0.8f, 0.8f, 0.8f)
            };
            style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
            style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 8;
            btn.AddThemeStyleboxOverride("normal", style);

            var captured = cardType;
            btn.Pressed += () => OnHandPressed(captured);
            btn.Disabled = !duel;

            _handPanel.AddChild(btn);
            _cardButtons.Add(btn);
        }

        // HandsALabel: 손패 요약 + 덱 남은 수
        if (_handsALabel != null)
            _handsALabel.Text = $"내 패: {hand.Count}장 | 덱: {gm.GetDeckCount(GameManager.PlayerA)}장";
    }

    private void RefreshButtonStates()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        bool duel = gm.CurrentState == GameManager.GameState.Duel;

        // 카드 버튼: 결투 상태이면 활성화
        foreach (var btn in _cardButtons)
            btn.Disabled = !duel;
    }

    private void DisableHandButtons()
    {
        foreach (var btn in _cardButtons)
            btn.Disabled = true;
    }

    private void OnDuelResolved(int p0, HandType h0, int p1, HandType h1, int winner)
    {
        string result = winner == -1
            ? $"비김! (나:{h0} vs 상대:{h1})"
            : (winner == GameManager.PlayerA ? $"승리! (나:{h0} vs 상대:{h1})" : $"패배! (나:{h0} vs 상대:{h1})");
        if (_resultLabel != null) _resultLabel.Text = result;

        RefreshHandCards();
    }

    private void OnGameOver(int winner)
    {
        SetStatusText(winner == GameManager.PlayerA ? "최종 승리!" : "최종 패배...");
        DisableHandButtons();
    }

    private void SetStatusText(string text)
    {
        if (_statusLabel != null) _statusLabel.Text = text;
    }

    private void RefreshLabels()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        RefreshHandCards();

        // 상대(B): 총합만 표시 (정보 은폐)
        if (_handsBLabel != null)
            _handsBLabel.Text = $"상대방 남은 패: {gm.GetHand(GameManager.PlayerB).Count}장";

        if (_streakLabel != null)
            _streakLabel.Text = $"나의 연패: {gm.LoseStreak[GameManager.PlayerA]} / 상대 연패: {gm.LoseStreak[GameManager.PlayerB]}";
    }
}
