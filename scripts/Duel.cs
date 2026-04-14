using System.Collections.Generic;
using System.Linq;
using Godot;

// 결투 전용 씬: 손패 선택, 결과 표시, 강화패 픽
public partial class Duel : Node2D
{
    private Label _statusLabel;
    private Label _resultLabel;
    private Label _handsALabel;
    private Label _handsBLabel;
    private Label _streakLabel;
    private HBoxContainer _handPanel;
    private readonly List<Button> _cardButtons = new();
    private PanelContainer _enhancedPickPanel;
    private readonly List<Button> _enhancedPickButtons = new();
    private List<HandType> _blindShuffledHand;
    private bool _isBlindActive = false;

    public override void _Ready()
    {
        // 전체 배경
        var bg = new ColorRect
        {
            Color = new Color(0.08f, 0.10f, 0.16f, 1f),
            Size = new Vector2(1280, 720)
        };
        AddChild(bg);

        // 상태 레이블
        _statusLabel = MakeLabel(new Vector2(40, 20), fontSize: 22);
        _resultLabel = MakeLabel(new Vector2(40, 60));
        _handsALabel = MakeLabel(new Vector2(40, 100));
        _handsBLabel = MakeLabel(new Vector2(40, 130));
        _streakLabel = MakeLabel(new Vector2(40, 160));

        // 손패 패널 (화면 하단 중앙)
        _handPanel = new HBoxContainer { Position = new Vector2(320, 560) };
        _handPanel.AddThemeConstantOverride("separation", 20);
        AddChild(_handPanel);

        // 강화패 픽 패널
        CreateEnhancedPickPanel();

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnStateChanged += OnStateChanged;
            gm.OnDuelResolved += OnDuelResolved;
            gm.OnGameOver += OnGameOver;
            gm.OnCardDrawn += OnCardDrawn;
            gm.OnBoardChanged += RefreshInfoLabels;
            gm.OnEnhancedPickRequired += OnEnhancedPickRequired;
            RefreshInfoLabels();
            OnStateChanged(gm.CurrentState);
        }
    }

    public override void _ExitTree()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnStateChanged -= OnStateChanged;
            gm.OnDuelResolved -= OnDuelResolved;
            gm.OnGameOver -= OnGameOver;
            gm.OnCardDrawn -= OnCardDrawn;
            gm.OnBoardChanged -= RefreshInfoLabels;
            gm.OnEnhancedPickRequired -= OnEnhancedPickRequired;
        }
    }

    private Label MakeLabel(Vector2 pos, int fontSize = 16)
    {
        var lbl = new Label { Position = pos };
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        lbl.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        AddChild(lbl);
        return lbl;
    }

    private void CreateEnhancedPickPanel()
    {
        _enhancedPickPanel = new PanelContainer
        {
            Visible = false,
            CustomMinimumSize = new Vector2(420, 160),
            Position = new Vector2(430, 280)
        };

        var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        var title = new Label { Text = "강화패를 선택하세요!" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var hbox = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        var picks = new (HandType type, string text, Color color)[]
        {
            (HandType.EnhancedRock,     "★바위", new Color(0.7f, 0.7f, 0.3f)),
            (HandType.EnhancedPaper,    "★보",   new Color(0.3f, 0.7f, 1.0f)),
            (HandType.EnhancedScissors, "★가위", new Color(1.0f, 0.5f, 0.5f)),
        };
        foreach (var (type, text, color) in picks)
        {
            var btn = new Button { CustomMinimumSize = new Vector2(120, 70), Text = text };
            var style = new StyleBoxFlat { BgColor = color };
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
        AddChild(_enhancedPickPanel);
    }

    // ── 이벤트 핸들러 ──────────────────────────────────────

    private void OnStateChanged(GameManager.GameState state)
    {
        var gm = GameManager.Instance;
        _isBlindActive = (state == GameManager.GameState.Duel && gm != null && gm.IsBlindDuel(GameManager.PlayerA));

        RefreshHandCards();
        RefreshInfoLabels();

        switch (state)
        {
            case GameManager.GameState.Duel:
                _enhancedPickPanel.Visible = false;
                SetStatus(_isBlindActive ? "결투! (블라인드!) 패를 선택하세요" : "결투! 패를 선택하세요");
                break;
            case GameManager.GameState.PickEnhanced:
                SetStatus("강화패를 선택하세요!");
                break;
            case GameManager.GameState.GameOver:
                SetStatus(gm?.WinnerId == GameManager.PlayerA ? "최종 승리!" : "최종 패배...");
                DisableHandButtons();
                break;
        }
    }

    private void OnCardDrawn(int playerId)
    {
        if (playerId == GameManager.PlayerA) RefreshHandCards();
    }

    private void OnDuelResolved(int p0, HandType h0, int p1, HandType h1, int winner)
    {
        string h0n = HandName(h0), h1n = HandName(h1);
        if (_resultLabel != null)
            _resultLabel.Text = winner == -1
                ? $"비김! (나:{h0n} vs 상대:{h1n})"
                : (winner == GameManager.PlayerA ? $"승리! (나:{h0n} vs 상대:{h1n})" : $"패배! (나:{h0n} vs 상대:{h1n})");
        RefreshHandCards();
    }

    private void OnGameOver(int winner)
    {
        SetStatus(winner == GameManager.PlayerA ? "최종 승리!" : "최종 패배...");
        DisableHandButtons();
    }

    private void OnEnhancedPickRequired(int playerId)
    {
        if (playerId != GameManager.PlayerA) return;
        _enhancedPickPanel.Visible = true;
        foreach (var btn in _enhancedPickButtons) btn.Disabled = false;
    }

    private void OnEnhancedPickPressed(HandType hand)
    {
        var gm = GameManager.Instance;
        if (gm?.CurrentState != GameManager.GameState.PickEnhanced) return;
        gm.PickEnhancedCard(GameManager.PlayerA, hand);
        _enhancedPickPanel.Visible = false;
    }

    private void OnHandPressed(HandType hand)
    {
        var gm = GameManager.Instance;
        if (gm?.CurrentState != GameManager.GameState.Duel) return;
        if (!gm.GetHand(GameManager.PlayerA).Contains(hand)) return;
        DisableHandButtons();
        SetStatus("선택 완료! 상대방의 결정을 기다리는 중...");
        gm.RequestHand(GameManager.PlayerA, hand);
    }

    private void OnHandPressedBlind(int index)
    {
        var gm = GameManager.Instance;
        if (gm?.CurrentState != GameManager.GameState.Duel) return;
        if (_blindShuffledHand == null || index >= _blindShuffledHand.Count) return;
        var actual = _blindShuffledHand[index];
        if (!gm.GetHand(GameManager.PlayerA).Contains(actual)) return;
        DisableHandButtons();
        SetStatus("선택 완료! (블라인드) 상대방의 결정을 기다리는 중...");
        gm.RequestHand(GameManager.PlayerA, actual);
    }

    // ── UI 갱신 ───────────────────────────────────────────

    private void RefreshHandCards()
    {
        if (_handPanel == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        foreach (var btn in _cardButtons) btn.QueueFree();
        _cardButtons.Clear();

        bool duel = gm.CurrentState == GameManager.GameState.Duel;
        var hand = gm.GetHand(GameManager.PlayerA);

        if (_isBlindActive && duel)
        {
            _blindShuffledHand = new List<HandType>(hand);
            var rng = new System.Random();
            for (int i = _blindShuffledHand.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (_blindShuffledHand[i], _blindShuffledHand[j]) = (_blindShuffledHand[j], _blindShuffledHand[i]);
            }
            for (int i = 0; i < _blindShuffledHand.Count; i++)
            {
                var btn = MakeCardButton("?", new Color(0.4f, 0.4f, 0.4f), false);
                btn.Disabled = !duel;
                var idx = i;
                btn.Pressed += () => OnHandPressedBlind(idx);
                _handPanel.AddChild(btn);
                _cardButtons.Add(btn);
            }
        }
        else
        {
            _blindShuffledHand = null;
            foreach (var card in hand)
            {
                bool enh = GameManager.IsEnhanced(card);
                var baseType = GameManager.GetBase(card);
                Color bg = baseType switch {
                    HandType.Rock     => enh ? new Color(0.7f,0.7f,0.3f) : new Color(0.5f,0.5f,0.5f),
                    HandType.Paper    => enh ? new Color(0.3f,0.7f,1.0f) : new Color(0.3f,0.5f,0.9f),
                    HandType.Scissors => enh ? new Color(1.0f,0.5f,0.5f) : new Color(0.9f,0.3f,0.3f),
                    _                 => new Color(0.8f,0.8f,0.8f)
                };
                var btn = MakeCardButton(HandName(card), bg, enh);
                btn.Disabled = !duel;
                var captured = card;
                btn.Pressed += () => OnHandPressed(captured);
                _handPanel.AddChild(btn);
                _cardButtons.Add(btn);
            }
        }
    }

    private static Button MakeCardButton(string text, Color bgColor, bool enhanced)
    {
        var btn = new Button { CustomMinimumSize = new Vector2(100, 70), Text = text };
        var style = new StyleBoxFlat { BgColor = bgColor };
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
        style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 8;
        if (enhanced)
        {
            style.BorderWidthTop = style.BorderWidthBottom =
            style.BorderWidthLeft = style.BorderWidthRight = 3;
            style.BorderColor = new Color(1.0f, 0.9f, 0.2f);
        }
        btn.AddThemeStyleboxOverride("normal", style);
        return btn;
    }

    private void RefreshInfoLabels()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (_handsALabel != null)
            _handsALabel.Text = $"내 패: {gm.GetHand(GameManager.PlayerA).Count}장 | 덱: {gm.GetDeckCount(GameManager.PlayerA)}장";
        if (_handsBLabel != null)
            _handsBLabel.Text = $"상대방 남은 패: {gm.GetHand(GameManager.PlayerB).Count}장";
        if (_streakLabel != null)
            _streakLabel.Text = $"나의 연패: {gm.LoseStreak[GameManager.PlayerA]}/{gm.MaxLoseStreak[GameManager.PlayerA]} / 상대 연패: {gm.LoseStreak[GameManager.PlayerB]}/{gm.MaxLoseStreak[GameManager.PlayerB]}";
    }

    private void DisableHandButtons()
    {
        foreach (var btn in _cardButtons) btn.Disabled = true;
    }

    private void SetStatus(string text) { if (_statusLabel != null) _statusLabel.Text = text; }

    private static string HandName(HandType h) => h switch {
        HandType.Rock           => "바위",
        HandType.Paper          => "보",
        HandType.Scissors       => "가위",
        HandType.EnhancedRock   => "★바위",
        HandType.EnhancedPaper  => "★보",
        HandType.EnhancedScissors => "★가위",
        _                       => "?"
    };
}
