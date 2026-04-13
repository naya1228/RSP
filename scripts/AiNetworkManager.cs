using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class AiNetworkManager : Node, INetworkManager
{
    public int LocalPlayerId => GameManager.PlayerA; // 사람은 항상 A
    public int ActivePlayerId => GameManager.PlayerA; // AI 모드에선 의미 없음
    public bool IsConnected { get; private set; } = false;

    public event Action<int, MoveDirection> OnMoveReceived;
    public event Action<int, HandType> OnHandReceived;
    public event Action OnOpponentConnected;

    private Random _random = new Random();

    public void Connect()
    {
        IsConnected = true;
        // AI는 항상 준비되어 있으므로 즉시 연결됨 처리
        OnOpponentConnected?.Invoke();

        // GameManager의 턴 변경 이벤트를 감시하여 AI 차례일 때 행동 시작
        GameManager.Instance.OnTurnChanged += OnTurnChanged;
        GameManager.Instance.OnStateChanged += OnStateChanged;
    }

    public void Disconnect()
    {
        IsConnected = false;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= OnTurnChanged;
            GameManager.Instance.OnStateChanged -= OnStateChanged;
        }
    }

    // 사람이 보낸 입력 처리
    public void SendMove(int playerId, MoveDirection direction)
    {
        if (playerId == GameManager.PlayerA)
        {
            OnMoveReceived?.Invoke(GameManager.PlayerA, direction);
        }
    }

    public void SendHand(int playerId, HandType hand)
    {
        if (playerId == GameManager.PlayerA)
        {
            OnHandReceived?.Invoke(GameManager.PlayerA, hand);
        }
    }

    private async void OnTurnChanged(int nextPlayerId)
    {
        if (nextPlayerId == GameManager.PlayerB && GameManager.Instance.CurrentState == GameManager.GameState.Moving)
        {
            await Task.Delay(800); // AI 생각 시간
            if (!IsConnected) return;

            // AI 이동 전략: 첫 턴은 무조건 전진, 이후 70% 전진 / 30% 후진
            var isFirstTurn = GameManager.Instance.TurnCount[GameManager.PlayerB] == 0;
            var dir = (isFirstTurn || _random.NextDouble() < 0.7) ? MoveDirection.Forward : MoveDirection.Backward;
            OnMoveReceived?.Invoke(GameManager.PlayerB, dir);
        }
    }

    private async void OnStateChanged(GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.Duel)
        {
            GD.Print("[AI] OnStateChanged(Duel) → AiSelectHand 시작");
            await AiSelectHand();
        }
    }

    private async Task AiSelectHand()
    {
        await Task.Delay(1000); // 결투 패 고르는 시간
        GD.Print("[AI] 1초 딜레이 완료");
        if (!IsConnected) return;

        // AI 결투 전략: 남은 패 중 랜덤 선택
        HandType? selectedHand = null;
        var hands = new HandType[] { HandType.Rock, HandType.Paper, HandType.Scissors };

        // 셔플 후 첫 번째 가능한 패 선택
        for (int i = 0; i < 3; i++)
        {
            int r = _random.Next(i, 3);
            var temp = hands[i];
            hands[i] = hands[r];
            hands[r] = temp;
        }

        foreach (var h in hands)
        {
            if (GameManager.Instance.GetHand(GameManager.PlayerB).Contains(h))
            {
                selectedHand = h;
                break;
            }
        }

        if (selectedHand.HasValue)
        {
            GD.Print($"[AI] 패 선택: {selectedHand.Value}");
            OnHandReceived?.Invoke(GameManager.PlayerB, selectedHand.Value);
        }
        else
        {
            GD.Print($"[AI] 선택 가능한 패 없음! 손패={GameManager.Instance.GetHand(GameManager.PlayerB).Count}장");
        }
    }
}
