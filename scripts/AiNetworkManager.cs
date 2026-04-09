using System;
using System.Threading.Tasks;
using Godot;

public partial class AiNetworkManager : Node, INetworkManager
{
    public int LocalPlayerId => 0; // 사람은 항상 0번
    public int ActivePlayerId => 0; // AI 모드에선 의미 없음
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
        if (playerId == 0)
        {
            OnMoveReceived?.Invoke(0, direction);
        }
    }

    public void SendHand(int playerId, HandType hand)
    {
        if (playerId == 0)
        {
            OnHandReceived?.Invoke(0, hand);
        }
    }

    private async void OnTurnChanged(int nextPlayerId)
    {
        if (nextPlayerId == 1 && GameManager.Instance.CurrentState == GameManager.GameState.Moving)
        {
            await Task.Delay(800); // AI 생각 시간
            if (!IsConnected) return;

            // AI 이동 전략: 70% 확률로 전진, 30% 확률로 후진 (단, 첫 턴은 무조건 전진)
            var dir = (_random.NextDouble() < 0.7) ? MoveDirection.Forward : MoveDirection.Backward;
            OnMoveReceived?.Invoke(1, dir);
        }
    }

    private async void OnStateChanged(GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.Duel)
        {
            await Task.Delay(1000); // 결투 패 고르는 시간
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
                if (GameManager.Instance.GetHandCount(1, h) > 0)
                {
                    selectedHand = h;
                    break;
                }
            }

            if (selectedHand.HasValue)
            {
                OnHandReceived?.Invoke(1, selectedHand.Value);
            }
        }
    }
}
