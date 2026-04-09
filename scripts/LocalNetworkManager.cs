using System;
using Godot;

// 같은 화면에서 2인을 교대로 플레이하는 로컬 더미 네트워크 매니저.
// 이후 AblyNetworkManager로 교체 예정.
public partial class LocalNetworkManager : Node, INetworkManager
{
    public int LocalPlayerId => GameManager.PlayerA;
    public int ActivePlayerId { get; private set; } = GameManager.PlayerA;
    public bool IsConnected { get; private set; } = false;

    public event Action<int, MoveDirection> OnMoveReceived;
    public event Action<int, HandType> OnHandReceived;
    public event Action OnOpponentConnected;

    public void Connect()
    {
        IsConnected = true;
        // 로컬 모드에서는 즉시 상대도 연결된 것으로 처리
        OnOpponentConnected?.Invoke();
        GD.Print("[LocalNetworkManager] Connected (local dummy mode)");
    }

    public void Disconnect()
    {
        IsConnected = false;
    }

    public void SendMove(int playerId, MoveDirection direction)
    {
        GD.Print($"[LocalNetworkManager] SendMove p{playerId} {direction}");
        // 로컬이므로 즉시 수신 콜백 호출
        OnMoveReceived?.Invoke(playerId, direction);
    }

    public void SendHand(int playerId, HandType hand)
    {
        GD.Print($"[LocalNetworkManager] SendHand p{playerId} {hand}");
        OnHandReceived?.Invoke(playerId, hand);
    }

    // 로컬 테스트용: 활성 플레이어 수동 전환
    public void SetActivePlayer(int playerId)
    {
        ActivePlayerId = playerId;
    }

    public void ToggleActivePlayer()
    {
        ActivePlayerId = 1 - ActivePlayerId;
    }
}
