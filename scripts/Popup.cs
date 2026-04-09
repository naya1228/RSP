using Godot;

public partial class Popup : CanvasLayer
{
    [Export] public string Title
    {
        get => _titleLabel?.Text ?? "";
        set { if (_titleLabel != null) _titleLabel.Text = value; }
    }

    private Label _titleLabel;
    private Button _closeButton;
    private HBoxContainer _tabBar;
    private VBoxContainer _content;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("Panel/Layout/TitleBar/TitleBackground/TitleLabel");
        _closeButton = GetNode<Button>("Panel/Layout/TitleBar/CloseButton");
        _tabBar = GetNode<HBoxContainer>("Panel/Layout/TabBar");
        _content = GetNode<VBoxContainer>("Panel/Layout/ContentArea/Content");

        _closeButton.Pressed += Close;
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
            Close();
    }

    // 탭 버튼 추가
    public void AddTab(string label)
    {
        var tab = new Button
        {
            Text = label,
            CustomMinimumSize = new Vector2(120, 44),
            ToggleMode = true,
        };
        _tabBar.GetNode<ColorRect>("TabBackground").AddChild(tab);
    }

    // 콘텐츠 노드 추가
    public void AddContent(Node node)
    {
        _content.AddChild(node);
    }

    public void Close()
    {
        QueueFree();
    }
}
