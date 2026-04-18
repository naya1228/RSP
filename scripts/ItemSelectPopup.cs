using Godot;

// 로그라이크식 아이템 선택 팝업 (범용 컴포넌트)
// 사용: 인스턴스 생성 → AddChild → Open(title, options) 호출
// 선택 시 Selected(int index) 시그널 발생 후 자체 QueueFree
// 취소 불가 — 반드시 하나 선택해야 닫힘
public partial class ItemSelectPopup : Control
{
    public struct ItemOption
    {
        public string Name;
        public string Desc;
        public Color Color;
    }

    [Signal] public delegate void SelectedEventHandler(int index);

    private const int CardWidth = 160;
    private const int CardHeight = 220;
    private const int CardColorBlockHeight = 80;
    private const int CardSpacing = 20;
    private const int PanelPadding = 30;

    public override void _Ready()
    {
        AnchorRight = 1f;
        AnchorBottom = 1f;
        OffsetLeft = 0f;
        OffsetTop = 0f;
        OffsetRight = 0f;
        OffsetBottom = 0f;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public void Open(string title, ItemOption[] options)
    {
        if (options == null || options.Length == 0) { QueueFree(); return; }

        // RemoveChild 먼저 → 트리에서 즉시 분리 후 QueueFree (같은 프레임 재생성 시 이름 충돌 방지)
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        Visible = true;

        // ── Overlay ───────────────────────────────────────────
        var overlay = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.7f),
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 0f,
            OffsetTop = 0f,
            OffsetRight = 0f,
            OffsetBottom = 0f,
            MouseFilter = MouseFilterEnum.Stop
        };
        AddChild(overlay);

        // ── CenterContainer: Panel 자동 중앙 정렬 ─────────────
        var center = new CenterContainer
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 0f,
            OffsetTop = 0f,
            OffsetRight = 0f,
            OffsetBottom = 0f,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(center);

        // ── Panel ─────────────────────────────────────────────
        var panel = new PanelContainer();
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f),
            BorderColor = new Color(0.5f, 0.5f, 0.7f),
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            ContentMarginLeft = PanelPadding,
            ContentMarginRight = PanelPadding,
            ContentMarginTop = PanelPadding,
            ContentMarginBottom = PanelPadding
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        center.AddChild(panel);

        // Panel 내부: VBox (타이틀 + 카드 컨테이너)
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
        panel.AddChild(vbox);

        // ── Title ─────────────────────────────────────────────
        var titleLabel = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 24);
        titleLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        vbox.AddChild(titleLabel);

        // ── Cards Container ───────────────────────────────────
        var cards = new HBoxContainer();
        cards.AddThemeConstantOverride("separation", CardSpacing);
        vbox.AddChild(cards);

        // ── Cards ─────────────────────────────────────────────
        for (int i = 0; i < options.Length; i++)
        {
            int ci = i; // 클로저 캡처
            var cardBtn = CreateCard(options[i]);
            cardBtn.Pressed += () =>
            {
                EmitSignal(SignalName.Selected, ci);
                QueueFree();
            };
            cards.AddChild(cardBtn);
        }

        // 첫 카드에 포커스 (키보드/게임패드 네비게이션)
        (cards.GetChild(0) as Button)?.CallDeferred(Control.MethodName.GrabFocus);
    }

    private Button CreateCard(ItemOption opt)
    {
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(CardWidth, CardHeight),
            Text = "",
            ClipText = false
        };
        SetCardStyle(btn);

        // 버튼 내부 레이아웃 컨테이너
        var vbox = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        vbox.AddThemeConstantOverride("separation", 6);
        btn.AddChild(vbox);
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // 상단 컬러 블록
        var colorBlock = new ColorRect
        {
            Color = opt.Color,
            CustomMinimumSize = new Vector2(CardWidth, CardColorBlockHeight),
            MouseFilter = MouseFilterEnum.Ignore
        };
        vbox.AddChild(colorBlock);

        // 이름 Label
        var nameLabel = new Label
        {
            Text = opt.Name,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 18);
        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        nameLabel.AddThemeConstantOverride("outline_size", 2);
        nameLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
        vbox.AddChild(nameLabel);

        // 구분선
        var separator = new ColorRect
        {
            Color = new Color(0.4f, 0.4f, 0.45f),
            CustomMinimumSize = new Vector2(CardWidth - 16, 2),
            MouseFilter = MouseFilterEnum.Ignore
        };
        vbox.AddChild(separator);

        // 설명 Label
        var descLabel = new Label
        {
            Text = opt.Desc,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            AutowrapMode = TextServer.AutowrapMode.Word,
            CustomMinimumSize = new Vector2(CardWidth - 16, 0),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        descLabel.AddThemeFontSizeOverride("font_size", 14);
        descLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.9f));
        vbox.AddChild(descLabel);

        return btn;
    }

    private void SetCardStyle(Button btn)
    {
        var border = new Color(0.4f, 0.4f, 0.5f);
        btn.AddThemeStyleboxOverride("normal",   MakeCardStyle(new Color(0.15f, 0.15f, 0.2f),  border));
        btn.AddThemeStyleboxOverride("hover",    MakeCardStyle(new Color(0.25f, 0.25f, 0.35f), border));
        btn.AddThemeStyleboxOverride("pressed",  MakeCardStyle(new Color(0.1f,  0.1f,  0.15f), border));
        btn.AddThemeStyleboxOverride("focus",    MakeCardStyle(new Color(0.25f, 0.25f, 0.35f), border));
        btn.AddThemeStyleboxOverride("disabled", MakeCardStyle(new Color(0.15f, 0.15f, 0.2f),  border));
    }

    private StyleBoxFlat MakeCardStyle(Color bg, Color border) => new StyleBoxFlat
    {
        BgColor = bg,
        BorderColor = border,
        BorderWidthLeft = 2,
        BorderWidthRight = 2,
        BorderWidthTop = 2,
        BorderWidthBottom = 2,
        ContentMarginLeft = 0,
        ContentMarginRight = 0,
        ContentMarginTop = 0,
        ContentMarginBottom = 0,
        CornerRadiusTopLeft = 4,
        CornerRadiusTopRight = 4,
        CornerRadiusBottomLeft = 4,
        CornerRadiusBottomRight = 4
    };
}
