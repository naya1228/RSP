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

    // 강화패 픽 UI
    private PanelContainer _enhancedPickPanel;
    private readonly List<Button> _enhancedPickButtons = new List<Button>();

    // 블라인드 상태에서 실제 손패 매핑 (셔플된 순서 → 실제 패)
    private List<HandType> _blindShuffledHand;
    private bool _isBlindActive = false;

    public override void _Ready()
    {
        _handPanel = GetNodeOrNull<HBoxContainer>("Root/HandPanel");

        _statusLabel = GetNodeOrNull<Label>("Root/StatusLabel");
        _resultLabel = GetNodeOrNull<Label>("Root/ResultLabel");
        _handsALabel = GetNodeOrNull<Label>("Root/HandsALabel");
        _handsBLabel = GetNodeOrNull<Label>("Root/HandsBLabel");
        _streakLabel = GetNodeOrNull<Label>("Root/StreakLabel");

        CreateEnhancedPickPanel();

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnStateChanged += OnStateChanged;
            gm.OnBoardChanged += RefreshLabels;
            gm.OnTurnChanged += OnTurnChanged;
            gm.OnDuelResolved += OnDuelResolved;
            gm.OnGameOver += OnGameOver;
            gm.OnCardDrawn += OnCardDrawn;
            gm.OnEnhancedPickRequired += OnEnhancedPickRequired;
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
            gm.OnEnhancedPickRequired -= OnEnhancedPickRequired;
        }
    }

    private void CreateEnhancedPickPanel()
    {
        // 강화패 픽 팝업 패널 생성
        _enhancedPickPanel = new PanelContainer();
        _enhancedPickPanel.Visible = false;
        _enhancedPickPanel.CustomMinimumSize = new Vector2(400, 150);

        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;

        var label = new Label();
        label.Text = "강화패를 선택하세요!";
        label.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(label);

        var hbox = new HBoxContainer();
        hbox.Alignment = BoxContainer.AlignmentMode.Center;

        var enhancedTypes = new (HandType type, string text, Color color)[]
        {
            (HandType.EnhancedRock, "★바위", new Color(0.7f, 0.7f, 0.3f)),
            (HandType.EnhancedPaper, "★보", new Color(0.3f, 0.7f, 1.0f)),
            (HandType.EnhancedScissors, "★가위", new Color(1.0f, 0.5f, 0.5f)),
        };

        foreach (var (type, text, color) in enhancedTypes)
        {
            var btn = new Button();
            btn.CustomMinimumSize = new Vector2(110, 70);
            btn.Text = text;

            var style = new StyleBoxFlat();
            style.BgColor = color;
            style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
            style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 8;
            btn.AddThemeStyleboxOverride("normal", style);

            var captured = type;
            btn.Pressed += () => OnEnhancedPickPressed(captured);
            hbox.AddChild(btn);
            _enhancedPickButtons.Add(btn);
        }

        vbox.AddChild(hbox);
        _enhancedPickPanel.AddChild(vbox);

        // Root에 추가
        var root = GetNodeOrNull<Control>("Root");
        if (root != null)
        {
            root.AddChild(_enhancedPickPanel);
            _enhancedPickPanel.AnchorLeft = 0.5f;
            _enhancedPickPanel.AnchorTop = 0.3f;
            _enhancedPickPanel.AnchorRight = 0.5f;
            _enhancedPickPanel.AnchorBottom = 0.3f;
            _enhancedPickPanel.GrowHorizontal = Control.GrowDirection.Both;
            _enhancedPickPanel.GrowVertical = Control.GrowDirection.Both;
        }
    }

    private void OnEnhancedPickPressed(HandType enhancedHand)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.CurrentState != GameManager.GameState.PickEnhanced) return;

        gm.PickEnhancedCard(GameManager.PlayerA, enhancedHand);
        _enhancedPickPanel.Visible = false;
    }

    private void OnEnhancedPickRequired(int playerId)
    {
        if (playerId == GameManager.PlayerA)
        {
            // 사람이 픽해야 함 → 팝업 표시
            _enhancedPickPanel.Visible = true;
            foreach (var btn in _enhancedPickButtons)
                btn.Disabled = false;
        }
        // AI는 AiNetworkManager에서 처리
    }

    private void OnHandPressed(HandType hand)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.CurrentState != GameManager.GameState.Duel) return;

        // 블라인드 상태면 실제 패를 알 수 없으므로, 클릭한 인덱스의 실제 패를 전송
        // (OnHandPressed는 실제 hand가 넘어오므로 그대로 사용)
        if (!gm.GetHand(GameManager.PlayerA).Contains(hand)) return;

        DisableHandButtons();
        SetStatusText("선택 완료! 상대방의 결정을 기다리는 중...");

        gm.RequestHand(GameManager.PlayerA, hand);
    }

    private void OnHandPressedBlind(int index)
    {
        // 블라인드 모드: 인덱스로 랜덤 매핑된 실제 패를 전송
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.CurrentState != GameManager.GameState.Duel) return;
        if (_blindShuffledHand == null || index >= _blindShuffledHand.Count) return;

        var actualHand = _blindShuffledHand[index];
        if (!gm.GetHand(GameManager.PlayerA).Contains(actualHand)) return;

        DisableHandButtons();
        SetStatusText("선택 완료! (블라인드) 상대방의 결정을 기다리는 중...");

        gm.RequestHand(GameManager.PlayerA, actualHand);
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        var gm = GameManager.Instance;

        // 블라인드 효과 체크
        _isBlindActive = (state == GameManager.GameState.Duel && gm != null && gm.IsBlindDuel(GameManager.PlayerA));

        RefreshHandCards();
        RefreshButtonStates();

        if (state == GameManager.GameState.Duel)
        {
            if (_isBlindActive)
                SetStatusText("결투! (블라인드!) 패를 선택하세요");
            else
                SetStatusText("결투! 패를 선택하세요");
            _enhancedPickPanel.Visible = false;
        }
        else if (state == GameManager.GameState.Moving)
        {
            SetStatusText(gm.CurrentTurnPlayer == GameManager.PlayerA ? "나의 이동 차례" : "상대방 이동 중...");
            _enhancedPickPanel.Visible = false;
            _isBlindActive = false;
        }
        else if (state == GameManager.GameState.PickEnhanced)
        {
            SetStatusText("강화패를 선택하세요!");
        }
        else if (state == GameManager.GameState.GameOver)
        {
            SetStatusText("게임 종료");
            _enhancedPickPanel.Visible = false;
        }
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

        // 블라인드 모드: 손패를 셔플해서 "?" 표시
        if (_isBlindActive && duel)
        {
            _blindShuffledHand = new List<HandType>(hand);
            // 셔플
            var rng = new System.Random();
            for (int i = _blindShuffledHand.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (_blindShuffledHand[i], _blindShuffledHand[j]) = (_blindShuffledHand[j], _blindShuffledHand[i]);
            }

            for (int idx = 0; idx < _blindShuffledHand.Count; idx++)
            {
                var btn = new Button();
                btn.CustomMinimumSize = new Vector2(100, 70);
                btn.Text = "?";

                var style = new StyleBoxFlat();
                style.BgColor = new Color(0.4f, 0.4f, 0.4f);
                style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
                style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 8;
                btn.AddThemeStyleboxOverride("normal", style);

                var capturedIdx = idx;
                btn.Pressed += () => OnHandPressedBlind(capturedIdx);
                btn.Disabled = !duel;

                _handPanel.AddChild(btn);
                _cardButtons.Add(btn);
            }
        }
        else
        {
            _blindShuffledHand = null;

            foreach (var cardType in hand)
            {
                var btn = new Button();
                btn.CustomMinimumSize = new Vector2(100, 70);
                btn.Text = cardType switch {
                    HandType.Rock => "바위",
                    HandType.Paper => "보",
                    HandType.Scissors => "가위",
                    HandType.EnhancedRock => "★바위",
                    HandType.EnhancedPaper => "★보",
                    HandType.EnhancedScissors => "★가위",
                    _ => "?"
                };

                // 색상 구분 (강화패는 더 밝은 색)
                bool enhanced = GameManager.IsEnhanced(cardType);
                var baseType = GameManager.GetBase(cardType);
                var style = new StyleBoxFlat();
                style.BgColor = baseType switch {
                    HandType.Rock => enhanced ? new Color(0.7f, 0.7f, 0.3f) : new Color(0.5f, 0.5f, 0.5f),
                    HandType.Paper => enhanced ? new Color(0.3f, 0.7f, 1.0f) : new Color(0.3f, 0.5f, 0.9f),
                    HandType.Scissors => enhanced ? new Color(1.0f, 0.5f, 0.5f) : new Color(0.9f, 0.3f, 0.3f),
                    _ => new Color(0.8f, 0.8f, 0.8f)
                };
                style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
                style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 8;

                // 강화패에 테두리 추가
                if (enhanced)
                {
                    style.BorderWidthTop = style.BorderWidthBottom =
                    style.BorderWidthLeft = style.BorderWidthRight = 3;
                    style.BorderColor = new Color(1.0f, 0.9f, 0.2f); // 금색 테두리
                }

                btn.AddThemeStyleboxOverride("normal", style);

                var captured = cardType;
                btn.Pressed += () => OnHandPressed(captured);
                btn.Disabled = !duel;

                _handPanel.AddChild(btn);
                _cardButtons.Add(btn);
            }
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
        // 강화패 표시명
        string h0Name = GetHandDisplayName(h0);
        string h1Name = GetHandDisplayName(h1);

        string result = winner == -1
            ? $"비김! (나:{h0Name} vs 상대:{h1Name})"
            : (winner == GameManager.PlayerA ? $"승리! (나:{h0Name} vs 상대:{h1Name})" : $"패배! (나:{h0Name} vs 상대:{h1Name})");
        if (_resultLabel != null) _resultLabel.Text = result;

        RefreshHandCards();
    }

    private static string GetHandDisplayName(HandType h)
    {
        return h switch {
            HandType.Rock => "바위",
            HandType.Paper => "보",
            HandType.Scissors => "가위",
            HandType.EnhancedRock => "★바위",
            HandType.EnhancedPaper => "★보",
            HandType.EnhancedScissors => "★가위",
            _ => "?"
        };
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
            _streakLabel.Text = $"나의 연패: {gm.LoseStreak[GameManager.PlayerA]}/{gm.MaxLoseStreak[GameManager.PlayerA]} / 상대 연패: {gm.LoseStreak[GameManager.PlayerB]}/{gm.MaxLoseStreak[GameManager.PlayerB]}";
    }
}
