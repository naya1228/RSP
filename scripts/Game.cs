using Godot;
using System.Collections.Generic;

// 보드 및 플레이어 시각화
public partial class Game : Node2D
{
	private const int TileCount = 10;
	private const int TileSize = 80;
	private const int TileSpacing = 10;
	private const int BoardStartX = 120;
	private const int BoardY = 360;

	private const float JumpDuration = 0.35f;
	private const float JumpHeight = 120f; // 타일 크기 1.5배

	private ColorRect[] _tiles = new ColorRect[TileCount];
	private ColorRect _playerA;
	private ColorRect _playerB;

	private Tween _tweenA;
	private Tween _tweenB;
	private bool _isAnimating;

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
			GameManager.Instance.OnTurnChanged += OnTurnChanged;
			UpdateBoard();
			OnTurnChanged(GameManager.Instance.CurrentTurnPlayer);
		}
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
		{
			GameManager.Instance.OnBoardChanged -= UpdateBoard;
			GameManager.Instance.OnTurnChanged -= OnTurnChanged;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouseBtn) return;

		var gm = GameManager.Instance;
		if (gm == null || gm.CurrentState != GameManager.GameState.Moving) return;
		if (gm.CurrentTurnPlayer != GameManager.PlayerA) return;

		// 애니메이션 중에는 입력 무시
		if (_isAnimating) return;

		// 뷰포트 좌표 → 로컬 좌표 변환
		var localPos = ToLocal(GetViewport().GetScreenTransform().AffineInverse() * mouseBtn.Position);

		foreach (var idx in gm.GetReachableTiles())
		{
			var tileRect = new Rect2(
				BoardStartX + idx * (TileSize + TileSpacing),
				BoardY,
				TileSize,
				TileSize
			);
			if (tileRect.HasPoint(localPos))
			{
				gm.TryMoveToTile(idx);
				return;
			}
		}
	}

	private void OnTurnChanged(int playerId)
	{
		// 모든 타일 원래 색으로 초기화
		for (int i = 0; i < TileCount; i++)
			_tiles[i].Color = GetTileColor(i);

		// 내 차례이면 이동 가능 칸 노란색으로 표시
		if (playerId == GameManager.PlayerA)
		{
			foreach (var idx in GameManager.Instance.GetReachableTiles())
				_tiles[idx].Color = new Color(1f, 1f, 0f);
		}
	}

	private Color GetTileColor(int index)
	{
		if (index == 0) return new Color(0.1f, 0.3f, 0.7f);
		if (index == 9) return new Color(0.7f, 0.2f, 0.2f);
		if (index == 3 || index == 6) return new Color(0.9f, 0.8f, 0.2f);
		return new Color(0.6f, 0.6f, 0.6f);
	}

	private Vector2 GetTileCenter(int index)
	{
		float x = BoardStartX + index * (TileSize + TileSpacing) + TileSize / 2f;
		float y = BoardY + TileSize / 2f;
		return new Vector2(x, y);
	}

	/// <summary>
	/// 논리 위치 기준 시각 목표 위치 계산 (겹침 오프셋 포함)
	/// </summary>
	private Vector2 GetTargetPosition(int posIndex, bool isPlayerA, bool overlapping)
	{
		var center = GetTileCenter(posIndex);
		if (overlapping)
		{
			if (isPlayerA)
				return center - new Vector2(TileSize * 0.3f, TileSize * 0.25f);
			else
				return center + new Vector2(TileSize * 0.05f, -TileSize * 0.25f);
		}
		else
		{
			return center - new Vector2(TileSize * 0.25f, TileSize * 0.25f);
		}
	}

	/// <summary>
	/// 포물선 Tween: X는 선형 이동, Y는 4*h*t*(1-t) 포물선으로 호를 그리며 착지
	/// </summary>
	private Tween CreateJumpTween(ColorRect node, Vector2 target, bool isPlayerA)
	{
		ref var tweenRef = ref (isPlayerA ? ref _tweenA : ref _tweenB);
		if (tweenRef != null && tweenRef.IsValid())
			tweenRef.Kill();

		var startPos = node.Position;

		var tween = CreateTween();
		tween.TweenMethod(Callable.From((float t) =>
		{
			float x = Mathf.Lerp(startPos.X, target.X, t);
			float y = Mathf.Lerp(startPos.Y, target.Y, t) - 4f * JumpHeight * t * (1f - t);
			node.Position = new Vector2(x, y);
		}), 0f, 1f, JumpDuration);

		tweenRef = tween;
		return tween;
	}

	private void UpdateBoard()
	{
		if (GameManager.Instance == null) return;

		var posA = GameManager.Instance.PlayerPositions[GameManager.PlayerA];
		var posB = GameManager.Instance.PlayerPositions[GameManager.PlayerB];

		bool overlapping = posA == posB;

		var targetA = GetTargetPosition(posA, true, overlapping);
		var targetB = GetTargetPosition(posB, false, overlapping);

		// 위치가 이미 목표와 같으면 (초기 배치 등) 즉시 반영
		bool needAnimA = _playerA.Position.DistanceTo(targetA) > 1f;
		bool needAnimB = _playerB.Position.DistanceTo(targetB) > 1f;

		if (!needAnimA && !needAnimB)
		{
			_playerA.Position = targetA;
			_playerB.Position = targetB;
			return;
		}

		_isAnimating = true;

		// 각 플레이어에 대해 점프 Tween 생성
		Tween lastTween = null;
		if (needAnimA)
			lastTween = CreateJumpTween(_playerA, targetA, true);
		else
			_playerA.Position = targetA;

		if (needAnimB)
			lastTween = CreateJumpTween(_playerB, targetB, false);
		else
			_playerB.Position = targetB;

		// 마지막 tween 완료 시 _isAnimating 해제
		if (lastTween != null)
			lastTween.Finished += () => _isAnimating = false;
	}
}
