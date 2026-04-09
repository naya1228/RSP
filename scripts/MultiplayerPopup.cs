using Godot;

public partial class MultiplayerPopup : CanvasLayer
{
    private Button _closeButton;
    private Button _createTab;
    private Button _joinTab;
    private VBoxContainer _createContent;
    private VBoxContainer _joinContent;
    private Label _roomCodeLabel;
    private LineEdit _codeInput;
    private Button _createButton;
    private Button _joinButton;

    public override void _Ready()
    {
        _closeButton = GetNode<Button>("Panel/Layout/TitleBar/CloseButton");
        _createTab = GetNode<Button>("Panel/Layout/TabBar/TabBackground/CreateTab");
        _joinTab = GetNode<Button>("Panel/Layout/TabBar/TabBackground/JoinTab");
        _createContent = GetNode<VBoxContainer>("Panel/Layout/ContentArea/CreateContent");
        _joinContent = GetNode<VBoxContainer>("Panel/Layout/ContentArea/JoinContent");
        _roomCodeLabel = GetNode<Label>("Panel/Layout/ContentArea/CreateContent/RoomCodeLabel");
        _codeInput = GetNode<LineEdit>("Panel/Layout/ContentArea/JoinContent/CodeInput");
        _createButton = GetNode<Button>("Panel/Layout/ContentArea/CreateContent/CreateButton");
        _joinButton = GetNode<Button>("Panel/Layout/ContentArea/JoinContent/JoinButton");

        _closeButton.Pressed += Close;
        _createTab.Pressed += () => SwitchTab(0);
        _joinTab.Pressed += () => SwitchTab(1);
        _createButton.Pressed += OnCreateRoom;
        _joinButton.Pressed += OnJoinRoom;

        SwitchTab(0);
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
            Close();
    }

    private void SwitchTab(int index)
    {
        _createContent.Visible = index == 0;
        _joinContent.Visible = index == 1;
        _createTab.ButtonPressed = index == 0;
        _joinTab.ButtonPressed = index == 1;
    }

    private void OnCreateRoom()
    {
        // TODO: Ably 연결 후 실제 방 코드 생성으로 교체
        string code = GenerateRoomCode();
        _roomCodeLabel.Text = $"방 코드: {code}";
        _createButton.Disabled = true;
        _createButton.Text = "대기 중...";
    }

    private void OnJoinRoom()
    {
        string code = _codeInput.Text.Trim().ToUpper();
        if (code.Length == 0)
        {
            _codeInput.PlaceholderText = "코드를 입력해주세요";
            return;
        }
        // TODO: Ably 연결 후 실제 방 참가로 교체
        GD.Print($"[MultiplayerPopup] 방 참가 시도: {code}");
    }

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new System.Random();
        var code = new char[6];
        for (int i = 0; i < 6; i++)
            code[i] = chars[rng.Next(chars.Length)];
        return new string(code);
    }

    public void Close()
    {
        QueueFree();
    }
}
