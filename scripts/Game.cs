using Godot;
using System.Collections.Generic;

// 이동 씬: 보드 시각화 + 이동 관련 UI
public partial class Game : Node2D
{
	private const int TileCount = 10;
	private const int TileSize = 80;
	private const int TileSpacing = 10;
	private const int BoardStartX = 120;
	private const int BoardY = 360;

	private const float JumpDuration = 0.35f;
	private const float JumpHeight = 120f;

	private Button[] _tiles = new Button[TileCount];
	private ColorRect _playerA;
	private ColorRect _playerB;

	private Tween _tweenA;
	private Tween _tweenB;
	private bool _isAnimating;

	// 이동 씬 UI Labels
	private Label _statusLabel;
	private Label _handsALabel;
	private Label _handsBLabel;
	private Label _streakLabel;

	public override void _Ready()
	{
		// 타일 생성
		for (int i = 0; i < TileCount; i++)
		{
			int captured = i;
			var btn = new Button
			{
				Size = new Vector2(TileSize, TileSize),
				Position = new Vector2(BoardStartX + i * (TileSize + TileSpacing), BoardY),
				Name = $"Tile{i}",
				Disabled = true,
				ClipText = false,
				Text = ""
			};
			SetTileStyle(btn, GetTileColor(i), true);
			AddChild(btn);
			_tiles[i] = btn;

			var label = new Label
			{
				Text = i.ToString(),
				Position = new Vector2(TileSize / 2 - 6, TileSize / 2 - 10),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			btn.AddChild(label);

			btn.Pressed += () => OnTilePressed(captured);
		}

		// 플레이어 A (파란색)
		_playerA = new ColorRect
		{
			Size = new Vector2(TileSize * 0.5f, TileSize * 0.5f),
			Color = new Color(0.2f, 0.4f, 1.0f),
			Name = "PlayerA",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddChild(_playerA);

		// 플레이어 B (빨간색)
		_playerB = new ColorRect
		{
			Size = new Vector2(TileSize * 0.5f, TileSize * 0.5f),
			Color = new Color(1.0f, 0.3f, 0.3f),
			Name = "PlayerB",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddChild(_playerB);

		// UI Labels
		_statusLabel  = MakeLabel(new Vector2(40, 20), fontSize: 20);
		_handsALabel  = MakeLabel(new Vector2(40, 50));
		_handsBLabel  = MakeLabel(new Vector2(40, 78));
		_streakLabel  = MakeLabel(new Vector2(40, 106));

		var gm = GameManager.Instance;
		if (gm != null)
		{
			gm.OnBoardChanged           += UpdateBoard;
			gm.OnBoardChanged           += RefreshLabels;
			gm.OnTurnChanged            += OnTurnChanged;
			gm.OnStateChanged           += OnStateChanged;
			gm.OnGameOver               += OnGameOver;
			gm.OnEnhancedPickRequired   += OnEnhancedPickRequired;
			UpdateBoard();
			RefreshLabels();
			OnTurnChanged(gm.CurrentTurnPlayer);
		}
	}

	public override void _ExitTree()
	{
		var gm = GameManager.Instance;
		if (gm != null)
		{
			gm.OnBoardChanged           -= UpdateBoard;
			gm.OnBoardChanged           -= RefreshLabels;
			gm.OnTurnChanged            -= OnTurnChanged;
			gm.OnStateChanged           -= OnStateChanged;
			gm.OnGameOver               -= OnGameOver;
			gm.OnEnhancedPickRequired   -= OnEnhancedPickRequired;
		}
	}

	private Label MakeLabel(Vector2 pos, int fontSize = 15)
	{
		var lbl = new Label { Position = pos };
		lbl.AddThemeFontSizeOverride("font_size", fontSize);
		lbl.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		AddChild(lbl);
		return lbl;
	}

	// ── 이벤트 핸들러 ──────────────────────────────────────

	private void OnStateChanged(GameManager.GameState state)
	{
		if (state == GameManager.GameState.Moving)
		{
			var gm = GameManager.Instance;
			SetStatus(gm?.CurrentTurnPlayer == GameManager.PlayerA ? "나의 차례" : "상대방 차례...");
			// 타일 강조 복원
			if (gm != null) HighlightReachable(gm.CurrentTurnPlayer);
		}
	}

	private void OnTurnChanged(int playerId)
	{
		HighlightReachable(playerId);
		SetStatus(playerId == GameManager.PlayerA ? "나의 차례" : "상대방 차례...");
	}

	private void HighlightReachable(int playerId)
	{
		for (int i = 0; i < TileCount; i++)
			SetTileStyle(_tiles[i], GetTileColor(i), true);

		if (playerId == GameManager.PlayerA)
		{
			foreach (var idx in GameManager.Instance.GetReachableTiles())
				SetTileStyle(_tiles[idx], new Color(1f, 1f, 0f), false);
		}
	}

	private void OnTilePressed(int idx)
	{
		if (_isAnimating) return;

		var gm = GameManager.Instance;
		if (gm == null) return;
		if (gm.CurrentState != GameManager.GameState.Moving) return;
		if (gm.CurrentTurnPlayer != GameManager.PlayerA) return;

		gm.TryMoveToTile(idx);
	}

	private void RefreshLabels()
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

	private void SetStatus(string text) { if (_statusLabel != null) _statusLabel.Text = text; }

	private void OnEnhancedPickRequired(int playerId)
	{
		if (playerId != GameManager.PlayerA) return;

		var types = new[] { HandType.EnhancedRock, HandType.EnhancedPaper, HandType.EnhancedScissors };
		var items = new ItemSelectPopup.ItemOption[types.Length];
		for (int i = 0; i < types.Length; i++) items[i] = GameManager.BuildCardOption(types[i]);

		var popup = new ItemSelectPopup();
		AddChild(popup);
		popup.Selected += (int idx) =>
		{
			GameManager.Instance?.PickEnhancedCard(GameManager.PlayerA, types[idx]);
		};
		popup.Open("강화패를 선택하세요!", items);
	}

	private void OnGameOver(int winner, GameManager.GameOverReason reason)
	{
		int loser = 1 - winner;
		var loserNode = loser == GameManager.PlayerA ? _playerA : _playerB;

		SetStatus(winner == GameManager.PlayerA ? "승리!" : "패배...");

		// 모든 타일 비활성화
		for (int i = 0; i < TileCount; i++)
			SetTileStyle(_tiles[i], GetTileColor(i), true);

		var tween = CreateTween();

		if (reason == GameManager.GameOverReason.ReachedStart)
		{
			// 1초 대기 후 시작칸 패배: 타일 위로 밀려나며 페이드 아웃
			tween.TweenInterval(1.0f);
			tween.SetParallel(true);
			tween.TweenProperty(loserNode, "position:y", loserNode.Position.Y - 250f, 0.6f)
				 .SetTrans(Tween.TransitionType.Linear);
			tween.TweenProperty(loserNode, "modulate:a", 0f, 0.6f)
				 .SetTrans(Tween.TransitionType.Linear);
		}
		else
		{
			// 1초 대기 후 연패 탈락: 아래로 떨어지며 페이드 아웃
			tween.TweenInterval(1.0f);
			tween.SetParallel(true);
			tween.TweenProperty(loserNode, "position:y", loserNode.Position.Y + 300f, 0.7f)
				 .SetTrans(Tween.TransitionType.Linear);
			tween.TweenProperty(loserNode, "modulate:a", 0f, 0.7f)
				 .SetTrans(Tween.TransitionType.Linear);
		}
	}

	// ── 타일 스타일 헬퍼 ────────────────────────────────────

	private void SetTileStyle(Button btn, Color color, bool disabled)
	{
		btn.Disabled = disabled;

		var styleNormal = new StyleBoxFlat { BgColor = color };
		btn.AddThemeStyleboxOverride("normal", styleNormal);

		var styleDisabled = new StyleBoxFlat { BgColor = color };
		btn.AddThemeStyleboxOverride("disabled", styleDisabled);

		var styleHover = new StyleBoxFlat { BgColor = color.Lightened(0.2f) };
		btn.AddThemeStyleboxOverride("hover", styleHover);

		var stylePressed = new StyleBoxFlat { BgColor = color.Darkened(0.15f) };
		btn.AddThemeStyleboxOverride("pressed", stylePressed);
	}

	// ── 보드 시각화 ────────────────────────────────────────

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

	private Vector2 GetTargetPosition(int posIndex, bool isPlayerA, bool overlapping)
	{
		var center = GetTileCenter(posIndex);
		if (overlapping)
		{
			return isPlayerA
				? center - new Vector2(TileSize * 0.3f, TileSize * 0.25f)
				: center + new Vector2(TileSize * 0.05f, -TileSize * 0.25f);
		}
		return center - new Vector2(TileSize * 0.25f, TileSize * 0.25f);
	}

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

		bool needAnimA = _playerA.Position.DistanceTo(targetA) > 1f;
		bool needAnimB = _playerB.Position.DistanceTo(targetB) > 1f;

		if (!needAnimA && !needAnimB)
		{
			_playerA.Position = targetA;
			_playerB.Position = targetB;
			return;
		}

		_isAnimating = true;
		int pending = 0;

		void OnTweenDone() { if (--pending == 0) _isAnimating = false; }

		if (needAnimA) { pending++; CreateJumpTween(_playerA, targetA, true).Finished += OnTweenDone; }
		else _playerA.Position = targetA;

		if (needAnimB) { pending++; CreateJumpTween(_playerB, targetB, false).Finished += OnTweenDone; }
		else _playerB.Position = targetB;
	}
}
