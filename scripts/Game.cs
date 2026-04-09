using Godot;

// 보드 및 플레이어 시각화
public partial class Game : Node2D
{
	private const int TileCount = 10;
	private const int TileSize = 80;
	private const int TileSpacing = 10;
	private const int BoardStartX = 120;
	private const int BoardY = 360;

	private ColorRect[] _tiles = new ColorRect[TileCount];
	private ColorRect _playerA;
	private ColorRect _playerB;

	public override void _Ready()
	{
		// 타일 생성
		for (int i = 0; i < TileCount; i++)
		{
			var rect = new ColorRect
			{
				Size = new Vector2(TileSize, TileSize),
				Position = new Vector2(BoardStartX + i * (TileSize + TileSpacing), BoardY),
				Color = GetTileColor(i),
				Name = $"Tile{i}"
			};
			AddChild(rect);
			_tiles[i] = rect;

			// 타일 번호 라벨
			var label = new Label
			{
				Text = i.ToString(),
				Position = new Vector2(TileSize / 2 - 6, TileSize / 2 - 10),
			};
			rect.AddChild(label);
		}

		// 플레이어 A (파란색)
		_playerA = new ColorRect
		{
			Size = new Vector2(TileSize * 0.5f, TileSize * 0.5f),
			Color = new Color(0.2f, 0.4f, 1.0f),
			Name = "PlayerA"
		};
		AddChild(_playerA);

		// 플레이어 B (빨간색)
		_playerB = new ColorRect
		{
			Size = new Vector2(TileSize * 0.5f, TileSize * 0.5f),
			Color = new Color(1.0f, 0.3f, 0.3f),
			Name = "PlayerB"
		};
		AddChild(_playerB);

		if (GameManager.Instance != null)
		{
			GameManager.Instance.OnBoardChanged += UpdateBoard;
			UpdateBoard();
		}
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
		{
			GameManager.Instance.OnBoardChanged -= UpdateBoard;
		}
	}

	private Color GetTileColor(int index)
	{
		// 양 끝: 출발점 (진한 녹색)
		if (index == 0) return new Color(0.1f, 0.3f, 0.7f);
		if (index == 9) return new Color(0.7f, 0.2f, 0.2f);
		// 효과 칸: 3, 6
		if (index == 3 || index == 6) return new Color(0.9f, 0.8f, 0.2f);
		// 일반
		return new Color(0.6f, 0.6f, 0.6f);
	}

	private Vector2 GetTileCenter(int index)
	{
		float x = BoardStartX + index * (TileSize + TileSpacing) + TileSize / 2f;
		float y = BoardY + TileSize / 2f;
		return new Vector2(x, y);
	}

	private void UpdateBoard()
	{
		if (GameManager.Instance == null) return;

		var posA = GameManager.Instance.PlayerPositions[0];
		var posB = GameManager.Instance.PlayerPositions[1];

		var centerA = GetTileCenter(posA);
		var centerB = GetTileCenter(posB);

		// 겹칠 경우 좌우로 살짝 오프셋
		if (posA == posB)
		{
			_playerA.Position = centerA - new Vector2(TileSize * 0.3f, TileSize * 0.25f);
			_playerB.Position = centerB + new Vector2(TileSize * 0.05f, -TileSize * 0.25f);
		}
		else
		{
			_playerA.Position = centerA - new Vector2(TileSize * 0.25f, TileSize * 0.25f);
			_playerB.Position = centerB - new Vector2(TileSize * 0.25f, TileSize * 0.25f);
		}
	}
}
