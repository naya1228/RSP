using Godot;

public partial class Main : Node2D
{
    private Button _singleplayerButton;
    private Button _multiplayerButton;
    private Button _settingsButton;
    private Button _exitButton;

    private readonly PackedScene _gameScene = GD.Load<PackedScene>("res://scenes/Game.tscn");
    private readonly PackedScene _uiScene = GD.Load<PackedScene>("res://scenes/UI.tscn");
    private readonly PackedScene _multiplayerPopupScene = GD.Load<PackedScene>("res://scenes/MultiplayerPopup.tscn");
    private readonly PackedScene _settingsPopupScene = GD.Load<PackedScene>("res://scenes/SettingsPopup.tscn");

    public override void _Ready()
    {
        _singleplayerButton = GetNode<Button>("Menu/SingleplayerButton");
        _multiplayerButton = GetNode<Button>("Menu/MultiplayerButton");
        _settingsButton = GetNode<Button>("Menu/SettingsButton");
        _exitButton = GetNode<Button>("Menu/ExitButton");

        _singleplayerButton.Pressed += OnSingleplayerPressed;
        _multiplayerButton.Pressed += OnMultiplayerPressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _exitButton.Pressed += OnExitPressed;

        var gm = new GameManager { Name = "GameManager" };
        AddChild(gm);
    }

    private void OnSingleplayerPressed()
    {
        GetNode("Menu").QueueFree();

        AddChild(_gameScene.Instantiate());
        AddChild(_uiScene.Instantiate());

        // AI 네트워크 매니저 생성 및 설정
        var aiManager = new AiNetworkManager();
        AddChild(aiManager);
        
        GameManager.Instance.SetNetworkManager(aiManager);
        GameManager.Instance.StartNewGame();
    }

    private void OnMultiplayerPressed()
    {
        if (GetNodeOrNull("MultiplayerPopup") != null) return;
        var popup = _multiplayerPopupScene.Instantiate();
        popup.Name = "MultiplayerPopup";
        AddChild(popup);
    }

    private void OnSettingsPressed()
    {
        if (GetNodeOrNull("SettingsPopup") != null) return;
        var popup = _settingsPopupScene.Instantiate();
        popup.Name = "SettingsPopup";
        AddChild(popup);
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }
}
