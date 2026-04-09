using Godot;

// 루트 씬. 로비와 게임 씬 전환을 관리
public partial class Main : Node2D
{
    [Export] public NodePath LobbyPath;
    [Export] public NodePath StartButtonPath;

    private GameManager _gameManager;
    private LocalNetworkManager _network;
    private Node _currentGameScene;

    private Control _lobby;
    private Button _startButton;

    public override void _Ready()
    {
        // 매니저 인스턴스 생성
        _gameManager = new GameManager { Name = "GameManager" };
        AddChild(_gameManager);

        _network = new LocalNetworkManager { Name = "NetworkManager" };
        AddChild(_network);
        _network.Connect();

        _lobby = GetNodeOrNull<Control>("Lobby");
        _startButton = GetNodeOrNull<Button>("Lobby/StartButton");

        if (_startButton != null)
        {
            _startButton.Pressed += OnStartPressed;
        }

        _gameManager.OnGameOver += OnGameOver;
    }

    private void OnStartPressed()
    {
        if (_lobby != null) _lobby.Visible = false;

        // Game.tscn과 UI.tscn을 동적으로 추가
        var gameScene = GD.Load<PackedScene>("res://scenes/Game.tscn");
        var uiScene = GD.Load<PackedScene>("res://scenes/UI.tscn");

        if (gameScene != null)
        {
            var gameInst = gameScene.Instantiate();
            AddChild(gameInst);
            _currentGameScene = gameInst;
        }

        if (uiScene != null)
        {
            var uiInst = uiScene.Instantiate();
            AddChild(uiInst);
        }

        _gameManager.StartNewGame();
    }

    private void OnGameOver(int winnerId)
    {
        GD.Print($"[Main] Game Over. Winner: Player {winnerId}");
    }
}
