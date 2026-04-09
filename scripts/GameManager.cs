using System;
using Godot;

// 게임 상태 관리 싱글턴 (Autoload 또는 Main에서 생성)
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Lobby,
        Moving,
        Duel,
        GameOver
    }

    // 맵: 10칸 (인덱스 0 ~ 9)
    public const int BoardSize = 10;

    // 각 플레이어 초기 패 보유량
    private const int InitialHandCount = 2;

    // 상태
    public GameState CurrentState { get; private set; } = GameState.Lobby;

    // 위치: 0 = A 출발점, 9 = B 출발점
    public int[] PlayerPositions { get; private set; } = new int[2] { 0, 9 };

    // 잔여 패: [playerId, handType] = 횟수
    public int[,] HandCounts { get; private set; } = new int[2, 3];

    // 연패 카운트
    public int[] LoseStreak { get; private set; } = new int[2];

    // 턴 카운트 (각 플레이어별)
    public int[] TurnCount { get; private set; } = new int[2];

    // 현재 턴 플레이어 (0 = A, 1 = B)
    public int CurrentTurnPlayer { get; private set; } = 0;

    // 결투 중 각 플레이어가 낸 패 (-1 이면 미선택)
    private int[] _duelHands = new int[2] { -1, -1 };

    // 승자
    public int WinnerId { get; private set; } = -1;

    // 이벤트
    public event Action<GameState> OnStateChanged;
    public event Action OnBoardChanged;
    public event Action<int> OnTurnChanged; // 새 턴 플레이어 id
    public event Action<int, HandType, int, HandType, int> OnDuelResolved; // p0Hand, p1Hand, winner(-1 = 비김)
    public event Action<int> OnGameOver; // winner id

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public void StartNewGame()
    {
        PlayerPositions[0] = 0;
        PlayerPositions[1] = 9;

        for (int p = 0; p < 2; p++)
        {
            for (int h = 0; h < 3; h++)
                HandCounts[p, h] = InitialHandCount;
            LoseStreak[p] = 0;
            TurnCount[p] = 0;
        }

        _duelHands[0] = -1;
        _duelHands[1] = -1;
        WinnerId = -1;
        CurrentTurnPlayer = 0;

        ChangeState(GameState.Moving);
        OnBoardChanged?.Invoke();
        OnTurnChanged?.Invoke(CurrentTurnPlayer);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    // 이동 처리 (전진/후진)
    public bool TryMove(int playerId, MoveDirection direction)
    {
        if (CurrentState != GameState.Moving) return false;
        if (playerId != CurrentTurnPlayer) return false;

        // 첫 턴은 무조건 2칸 전진
        int step = TurnCount[playerId] == 0 ? 2 : 1;

        int dir = (playerId == 0) ? 1 : -1; // A는 +, B는 -
        if (direction == MoveDirection.Backward) dir = -dir;

        int newPos = PlayerPositions[playerId] + dir * step;
        newPos = Mathf.Clamp(newPos, 0, BoardSize - 1);
        PlayerPositions[playerId] = newPos;
        TurnCount[playerId]++;

        OnBoardChanged?.Invoke();

        // 상대 출발점 도달 체크
        int opponentStart = (playerId == 0) ? 9 : 0;
        if (newPos == opponentStart)
        {
            EndGame(playerId);
            return true;
        }

        // 같은 칸 만나면 결투
        if (PlayerPositions[0] == PlayerPositions[1])
        {
            ChangeState(GameState.Duel);
            _duelHands[0] = -1;
            _duelHands[1] = -1;
        }
        else
        {
            // 턴 교체
            CurrentTurnPlayer = 1 - CurrentTurnPlayer;
            OnTurnChanged?.Invoke(CurrentTurnPlayer);
        }

        return true;
    }

    // 결투 중 패 선택
    public bool SubmitHand(int playerId, HandType hand)
    {
        if (CurrentState != GameState.Duel) return false;
        if (HandCounts[playerId, (int)hand] <= 0) return false;
        if (_duelHands[playerId] != -1) return false;

        _duelHands[playerId] = (int)hand;

        // 양쪽 다 냈으면 결과 판정
        if (_duelHands[0] != -1 && _duelHands[1] != -1)
        {
            ResolveDuel();
        }

        return true;
    }

    private void ResolveDuel()
    {
        HandType h0 = (HandType)_duelHands[0];
        HandType h1 = (HandType)_duelHands[1];

        // 패 소모
        HandCounts[0, _duelHands[0]]--;
        HandCounts[1, _duelHands[1]]--;

        int winner = CompareHands(h0, h1); // -1 비김, 0 p0승, 1 p1승

        if (winner == -1)
        {
            // 비김: 재결투, 연패 카운트 유지
            _duelHands[0] = -1;
            _duelHands[1] = -1;
            OnDuelResolved?.Invoke(0, h0, 1, h1, -1);
            OnBoardChanged?.Invoke();
            // 둘 다 패 소진 확인
            if (!HasAnyHand(0) || !HasAnyHand(1))
            {
                // 둘 다 못 낼 상황이면 게임 종료 처리 생략 (간단 구현)
            }
            return;
        }

        int loser = 1 - winner;
        LoseStreak[winner] = 0;
        LoseStreak[loser]++;

        // 패자 1칸 후퇴 (자기 출발점 방향)
        int backDir = (loser == 0) ? -1 : 1;
        int newPos = Mathf.Clamp(PlayerPositions[loser] + backDir, 0, BoardSize - 1);
        PlayerPositions[loser] = newPos;

        OnDuelResolved?.Invoke(0, h0, 1, h1, winner);
        OnBoardChanged?.Invoke();

        // 3연패 탈락
        if (LoseStreak[loser] >= 3)
        {
            EndGame(winner);
            return;
        }

        // 결투 종료, 이동 페이즈로
        _duelHands[0] = -1;
        _duelHands[1] = -1;
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

    public bool HasAnyHand(int playerId)
    {
        for (int h = 0; h < 3; h++)
            if (HandCounts[playerId, h] > 0) return true;
        return false;
    }

    public int GetHandCount(int playerId, HandType hand)
    {
        return HandCounts[playerId, (int)hand];
    }

    private void EndGame(int winner)
    {
        WinnerId = winner;
        ChangeState(GameState.GameOver);
        OnGameOver?.Invoke(winner);
    }
}
