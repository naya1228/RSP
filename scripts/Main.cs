using Godot;

public partial class Main : Node2D
{
    private Button _singleplayerButton;
    private Button _multiplayerButton;
    private Button _settingsButton;
    private Button _exitButton;

    private readonly PackedScene _gameScenePacked   = GD.Load<PackedScene>("res://scenes/Game.tscn");
    private readonly PackedScene _duelScenePacked   = GD.Load<PackedScene>("res://scenes/Duel.tscn");
    private readonly PackedScene _multiplayerPopupScene = GD.Load<PackedScene>("res://scenes/MultiplayerPopup.tscn");
    private readonly PackedScene _settingsPopupScene    = GD.Load<PackedScene>("res://scenes/SettingsPopup.tscn");

    private Node2D _gameInstance;
    private Node2D _duelInstance;

    public override void _Ready()
    {
        _singleplayerButton = GetNode<Button>("Menu/SingleplayerButton");
        _multiplayerButton  = GetNode<Button>("Menu/MultiplayerButton");
        _settingsButton     = GetNode<Button>("Menu/SettingsButton");
        _exitButton         = GetNode<Button>("Menu/ExitButton");

        _singleplayerButton.Pressed += OnSingleplayerPressed;
        _multiplayerButton.Pressed  += OnMultiplayerPressed;
        _settingsButton.Pressed     += OnSettingsPressed;
        _exitButton.Pressed         += OnExitPressed;

        var gm = new GameManager { Name = "GameManager" };
        AddChild(gm);
    }

    private void OnSingleplayerPressed()
    {
        GetNode("Menu").QueueFree();

        // 이동 씬 + 결투 씬 모두 준비, 결투 씬은 숨김
        _gameInstance = (Node2D)_gameScenePacked.Instantiate();
        _duelInstance = (Node2D)_duelScenePacked.Instantiate();

        AddChild(_gameInstance);
        AddChild(_duelInstance);
        _duelInstance.Visible = false;

        // AI 네트워크 매니저
        var aiManager = new AiNetworkManager();
        AddChild(aiManager);

        GameManager.Instance.SetNetworkManager(aiManager);
        GameManager.Instance.OnStateChanged += OnGameStateChanged;
        GameManager.Instance.StartNewGame();
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (_gameInstance == null || _duelInstance == null) return;

        bool showDuel = state == GameManager.GameState.Duel
                     || state == GameManager.GameState.PickEnhanced
                     || state == GameManager.GameState.GameOver;

        _gameInstance.Visible = !showDuel;
        _duelInstance.Visible = showDuel;
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
