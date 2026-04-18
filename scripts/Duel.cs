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

        // 손패 패널 (화면 하단 중앙, 카드 130x210 고려)
        _handPanel = new HBoxContainer { Position = new Vector2(280, 490) };
        _handPanel.AddThemeConstantOverride("separation", 14);
        AddChild(_handPanel);

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnStateChanged += OnStateChanged;
            gm.OnDuelResolved += OnDuelResolved;
            gm.OnGameOver += OnGameOver;
            gm.OnCardDrawn += OnCardDrawn;
            gm.OnBoardChanged += RefreshInfoLabels;
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
                SetStatus(_isBlindActive ? "결투! (블라인드!) 패를 선택하세요" : "결투! 패를 선택하세요");
                if (gm != null && gm.IsEmergency(GameManager.PlayerA)) ShowEmergencyPopup();
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

    private void OnGameOver(int winner, GameManager.GameOverReason _)
    {
        SetStatus(winner == GameManager.PlayerA ? "최종 승리!" : "최종 패배...");
        DisableHandButtons();
    }

    private void ShowEmergencyPopup()
    {
        if (GetNodeOrNull("EmergencyPopup") != null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        var options = gm.GetEmergencyOptions();
        var items = new ItemSelectPopup.ItemOption[options.Length];
        for (int i = 0; i < options.Length; i++) items[i] = GameManager.BuildCardOption(options[i]);

        var popup = new ItemSelectPopup { Name = "EmergencyPopup" };
        AddChild(popup);
        popup.Selected += (int idx) =>
        {
            GameManager.Instance?.EmergencyPickHand(GameManager.PlayerA, options[idx]);
            SetStatus("긴급 픽 완료! 상대방의 결정을 기다리는 중...");
        };
        popup.Open("긴급! 덱/손패가 없습니다", items);
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
                var btn = MakeCardButton("?", "???", new Color(0.4f, 0.4f, 0.4f), false);
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
                var btn = MakeCardButton(GameManager.GetCardName(card), GameManager.GetCardDesc(card), GameManager.GetCardColor(card), enh);
                btn.Disabled = !duel;
                var captured = card;
                btn.Pressed += () => OnHandPressed(captured);
                _handPanel.AddChild(btn);
                _cardButtons.Add(btn);
            }
        }
    }

    private const int CardW = 130;
    private const int CardH = 210;
    private const int CardIllustSize = 110;

    private static Button MakeCardButton(string name, string desc, Color color, bool enhanced)
    {
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(CardW, CardH),
            Text = "",
            ClipText = false
        };
        SetHandCardStyle(btn, enhanced);

        var vbox = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        vbox.AddThemeConstantOverride("separation", 4);
        btn.AddChild(vbox);
        vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // 상단: 이름
        var nameLabel = new Label
        {
            Text = name,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 18);
        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        nameLabel.AddThemeConstantOverride("outline_size", 2);
        nameLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
        vbox.AddChild(nameLabel);

        // 중간: 일러스트 (현재는 컬러블록, 110x110)
        var illust = new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(CardIllustSize, CardIllustSize),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        vbox.AddChild(illust);

        // 하단: 설명
        var descLabel = new Label
        {
            Text = desc,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            AutowrapMode = TextServer.AutowrapMode.Word,
            CustomMinimumSize = new Vector2(CardW - 12, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        descLabel.AddThemeFontSizeOverride("font_size", 12);
        descLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        vbox.AddChild(descLabel);

        return btn;
    }

    private static void SetHandCardStyle(Button btn, bool enhanced)
    {
        var border = enhanced ? new Color(1.0f, 0.9f, 0.2f) : new Color(0.3f, 0.3f, 0.35f);
        int bw = enhanced ? 3 : 2;

        StyleBoxFlat Make(Color bg) => new()
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = bw, BorderWidthRight = bw,
            BorderWidthTop = bw, BorderWidthBottom = bw,
            ContentMarginLeft = 6, ContentMarginRight = 6,
            ContentMarginTop = 6, ContentMarginBottom = 6,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        };

        btn.AddThemeStyleboxOverride("normal",   Make(new Color(0.15f, 0.15f, 0.2f)));
        btn.AddThemeStyleboxOverride("hover",    Make(new Color(0.25f, 0.25f, 0.35f)));
        btn.AddThemeStyleboxOverride("pressed",  Make(new Color(0.1f, 0.1f, 0.15f)));
        btn.AddThemeStyleboxOverride("disabled", Make(new Color(0.12f, 0.12f, 0.15f)));
        btn.AddThemeStyleboxOverride("focus",    Make(new Color(0.25f, 0.25f, 0.35f)));
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
