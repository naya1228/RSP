using System;
using System.Collections.Generic;
using Godot;

// 게임 상태 관리 싱글턴 (Autoload 또는 Main에서 생성)
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public const int PlayerA = 0;
    public const int PlayerB = 1;

    public enum GameState
    {
        Lobby,
        Moving,
        Duel,
        GameOver
    }

    // 맵: 10칸 (인덱스 0 ~ 9)
    public const int BoardSize = 10;

    // 상태
    public GameState CurrentState { get; private set; } = GameState.Lobby;

    // 위치: 0 = A 출발점, 9 = B 출발점
    public int[] PlayerPositions { get; private set; } = new int[2] { 0, 9 };

    // 플레이어 손패 (순서 있음)
    private List<HandType>[] _playerHands = { new List<HandType>(), new List<HandType>() };
    // 플레이어 덱 (남은 카드)
    private List<HandType>[] _playerDecks = { new List<HandType>(), new List<HandType>() };
    private bool _isFirstDuel = true;
    private Random _rng = new Random();

    // 연패 카운트
    public int[] LoseStreak { get; private set; } = new int[2];

    // 턴 카운트 (각 플레이어별)
    public int[] TurnCount { get; private set; } = new int[2];

    // 현재 턴 플레이어
    public int CurrentTurnPlayer { get; private set; } = PlayerA;

    // 결투 중 각 플레이어가 낸 패 (-1 이면 미선택)
    private int[] _duelHands = new int[2] { -1, -1 };

    // 승자
    public int WinnerId { get; private set; } = -1;

    // 네트워크 매니저 (AI, Local, Ably 모두 대응)
    private INetworkManager _networkManager;

    // 이벤트
    public event Action<GameState> OnStateChanged;
    public event Action OnBoardChanged;
    public event Action<int> OnTurnChanged; // 새 턴 플레이어 id
    public event Action<int, HandType, int, HandType, int> OnDuelResolved; // p0Hand, p1Hand, winner(-1 = 비김)
    public event Action<int> OnGameOver; // winner id
    public event Action<int> OnCardDrawn; // 드로우한 플레이어 id

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (_networkManager != null)
        {
            _networkManager.OnMoveReceived -= OnMoveReceived;
            _networkManager.OnHandReceived -= OnHandReceived;
        }
        if (Instance == this) Instance = null;
    }

    public void SetNetworkManager(INetworkManager nm)
    {
        if (_networkManager != null)
        {
            _networkManager.OnMoveReceived -= OnMoveReceived;
            _networkManager.OnHandReceived -= OnHandReceived;
        }

        _networkManager = nm;
        _networkManager.OnMoveReceived += OnMoveReceived;
        _networkManager.OnHandReceived += OnHandReceived;
        _networkManager.Connect();
    }

    private void OnMoveReceived(int playerId, MoveDirection direction)
    {
        TryMove(playerId, direction);
    }

    private void OnHandReceived(int playerId, HandType hand)
    {
        SubmitHand(playerId, hand);
    }

    public void RequestMove(int playerId, MoveDirection direction)
    {
        _networkManager?.SendMove(playerId, direction);
    }

    public void RequestHand(int playerId, HandType hand)
    {
        _networkManager?.SendHand(playerId, hand);
    }

    // 내 플레이어가 이동 가능한 칸 목록
    public List<int> GetReachableTiles()
    {
        int pos = PlayerPositions[PlayerA];
        var result = new List<int>();

        if (TurnCount[PlayerA] == 0)
        {
            // 첫 턴: 1칸 또는 2칸 전진
            if (pos + 1 <= 9) result.Add(pos + 1);
            if (pos + 2 <= 9) result.Add(pos + 2);
        }
        else
        {
            // 이후: 전진 1칸, 후진 1칸
            if (pos + 1 <= 9) result.Add(pos + 1);
            if (pos - 1 >= 0) result.Add(pos - 1);
        }

        return result;
    }

    public void StartNewGame()
    {
        PlayerPositions[PlayerA] = 0;
        PlayerPositions[PlayerB] = 9;

        for (int p = 0; p < 2; p++)
        {
            LoseStreak[p] = 0;
            TurnCount[p] = 0;
            InitPlayerDeck(p);
        }

        _isFirstDuel = true;
        _duelHands[PlayerA] = -1;
        _duelHands[PlayerB] = -1;
        WinnerId = -1;
        CurrentTurnPlayer = PlayerA;

        ChangeState(GameState.Moving);
        OnBoardChanged?.Invoke();
        OnTurnChanged?.Invoke(CurrentTurnPlayer);
    }

    private void InitPlayerDeck(int playerId)
    {
        // 전체 덱 [R,R,P,P,S,S]
        var full = new List<HandType> { HandType.Rock, HandType.Rock, HandType.Paper, HandType.Paper, HandType.Scissors, HandType.Scissors };
        // 보장 3장 (각 타입 1장)
        var hand = new List<HandType> { HandType.Rock, HandType.Paper, HandType.Scissors };
        full.Remove(HandType.Rock); full.Remove(HandType.Paper); full.Remove(HandType.Scissors);
        // 4번째 카드: 남은 풀에서 랜덤
        int i = _rng.Next(full.Count);
        hand.Add(full[i]);
        full.RemoveAt(i);
        // 손패 셔플
        for (int j = hand.Count - 1; j > 0; j--)
        {
            int k = _rng.Next(j + 1);
            (hand[j], hand[k]) = (hand[k], hand[j]);
        }
        _playerHands[playerId] = hand;
        _playerDecks[playerId] = full; // 2장 남음
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    // 손패 조회
    public IReadOnlyList<HandType> GetHand(int playerId) => _playerHands[playerId].AsReadOnly();
    public int GetDeckCount(int playerId) => _playerDecks[playerId].Count;

    // 기존 호환 (AI용)
    public int GetHandCount(int playerId, HandType hand)
    {
        int c = 0;
        foreach (var h in _playerHands[playerId]) if (h == hand) c++;
        return c;
    }

    // 드로우
    private bool DrawCard(int playerId)
    {
        var deck = _playerDecks[playerId];
        if (deck.Count == 0) return false;
        int idx = _rng.Next(deck.Count);
        _playerHands[playerId].Add(deck[idx]);
        deck.RemoveAt(idx);
        OnCardDrawn?.Invoke(playerId);
        return true;
    }

    // 이동 처리 (전진/후진)
    public bool TryMove(int playerId, MoveDirection direction)
    {
        if (CurrentState != GameState.Moving) return false;
        if (playerId != CurrentTurnPlayer) return false;

        // 첫 턴은 무조건 2칸 전진
        int step = TurnCount[playerId] == 0 ? 2 : 1;

        int dir = (playerId == PlayerA) ? 1 : -1; // A는 +, B는 -
        if (direction == MoveDirection.Backward) dir = -dir;

        int newPos = PlayerPositions[playerId] + dir * step;
        newPos = Mathf.Clamp(newPos, 0, BoardSize - 1);
        PlayerPositions[playerId] = newPos;
        TurnCount[playerId]++;

        OnBoardChanged?.Invoke();

        // 상대 출발점 도달 체크
        int opponentStart = (playerId == PlayerA) ? 9 : 0;
        if (newPos == opponentStart)
        {
            EndGame(playerId);
            return true;
        }

        // 같은 칸 만나면 결투
        if (PlayerPositions[PlayerA] == PlayerPositions[PlayerB])
        {
            _duelHands[PlayerA] = -1;
            _duelHands[PlayerB] = -1;
            if (!_isFirstDuel) { DrawCard(PlayerA); DrawCard(PlayerB); }
            _isFirstDuel = false;
            ChangeState(GameState.Duel);
        }
        else
        {
            CurrentTurnPlayer = 1 - CurrentTurnPlayer;
            OnTurnChanged?.Invoke(CurrentTurnPlayer);
        }

        return true;
    }

    // 타일 인덱스로 직접 이동 (UI 클릭용)
    public bool TryMoveToTile(int targetTile)
    {
        if (CurrentState != GameState.Moving) return false;
        if (CurrentTurnPlayer != PlayerA) return false;
        if (!GetReachableTiles().Contains(targetTile)) return false;

        PlayerPositions[PlayerA] = targetTile;
        TurnCount[PlayerA]++;

        OnBoardChanged?.Invoke();

        // 상대 출발점 도달 체크
        if (targetTile == 9)
        {
            EndGame(PlayerA);
            return true;
        }

        // 같은 칸 만나면 결투
        if (PlayerPositions[PlayerA] == PlayerPositions[PlayerB])
        {
            _duelHands[PlayerA] = -1;
            _duelHands[PlayerB] = -1;
            if (!_isFirstDuel) { DrawCard(PlayerA); DrawCard(PlayerB); }
            _isFirstDuel = false;
            ChangeState(GameState.Duel);
        }
        else
        {
            CurrentTurnPlayer = 1 - CurrentTurnPlayer;
            OnTurnChanged?.Invoke(CurrentTurnPlayer);
        }

        return true;
    }

    // 결투 중 패 선택
    public bool SubmitHand(int playerId, HandType hand)
    {
        if (CurrentState != GameState.Duel) return false;
        if (!_playerHands[playerId].Contains(hand)) return false;
        if (_duelHands[playerId] != -1) return false;

        _duelHands[playerId] = (int)hand;

        if (_duelHands[PlayerA] != -1 && _duelHands[PlayerB] != -1)
        {
            ResolveDuel();
        }

        return true;
    }

    private void ResolveDuel()
    {
        HandType h0 = (HandType)_duelHands[PlayerA];
        HandType h1 = (HandType)_duelHands[PlayerB];

        // 패 소모
        _playerHands[PlayerA].Remove(h0);
        _playerHands[PlayerB].Remove(h1);

        int winner = CompareHands(h0, h1); // -1 비김, 0 p0승, 1 p1승

        if (winner == -1)
        {
            GD.Print("[Draw] 비김 감지");
            _duelHands[PlayerA] = -1;
            _duelHands[PlayerB] = -1;
            DrawCard(PlayerA); DrawCard(PlayerB);
            GD.Print($"[Draw] 손패 A={_playerHands[PlayerA].Count}장, B={_playerHands[PlayerB].Count}장, 덱A={_playerDecks[PlayerA].Count}, 덱B={_playerDecks[PlayerB].Count}");
            OnDuelResolved?.Invoke(PlayerA, h0, PlayerB, h1, -1);
            GD.Print("[Draw] OnDuelResolved 완료");
            OnBoardChanged?.Invoke();
            GD.Print("[Draw] ChangeState(Duel) 호출");
            ChangeState(GameState.Duel);
            GD.Print("[Draw] ChangeState(Duel) 완료");
            return;
        }

        int loser = 1 - winner;
        LoseStreak[winner] = 0;
        LoseStreak[loser]++;

        // 패자 1칸 후퇴 (자기 출발점 방향)
        int backDir = (loser == PlayerA) ? -1 : 1;
        int newPos = Mathf.Clamp(PlayerPositions[loser] + backDir, 0, BoardSize - 1);
        PlayerPositions[loser] = newPos;

        OnDuelResolved?.Invoke(PlayerA, h0, PlayerB, h1, winner);
        OnBoardChanged?.Invoke();

        // 3연패 탈락
        if (LoseStreak[loser] >= 3)
        {
            EndGame(winner);
            return;
        }

        _duelHands[PlayerA] = -1;
        _duelHands[PlayerB] = -1;
        ChangeState(GameState.Moving);
        CurrentTurnPlayer = 1 - CurrentTurnPlayer;
        OnTurnChanged?.Invoke(CurrentTurnPlayer);
    }

    // -1 비김, 0 p0승, 1 p1승
    private static int CompareHands(HandType a, HandType b)
    {
        if (a == b) return -1;
        bool aWin =
            (a == HandType.Rock && b == HandType.Scissors) ||
            (a == HandType.Scissors && b == HandType.Paper) ||
            (a == HandType.Paper && b == HandType.Rock);
        return aWin ? 0 : 1;
    }

    public bool HasAnyHand(int playerId) => _playerHands[playerId].Count > 0;

    private void EndGame(int winner)
    {
        WinnerId = winner;
        ChangeState(GameState.GameOver);
        OnGameOver?.Invoke(winner);
    }
}
