using System;

public enum HandType
{
    Rock,
    Paper,
    Scissors
}

public enum MoveDirection
{
    Forward,
    Backward
}

// 네트워크 매니저 추상화. 로컬 더미 / Ably 구현을 교체 가능하도록 분리
public interface INetworkManager
{
    // 내가 어느 플레이어인지 (0 = A, 1 = B)
    int LocalPlayerId { get; }

    // 현재 "활성" 플레이어 (LocalNetworkManager에서 같은 화면 교대 입력에 사용)
    int ActivePlayerId { get; }

    // 연결 / 세션
    void Connect();
    void Disconnect();
    bool IsConnected { get; }

    // 입력 송신
    void SendMove(int playerId, MoveDirection direction);
    void SendHand(int playerId, HandType hand);

    // 이벤트
    event Action<int, MoveDirection> OnMoveReceived;
    event Action<int, HandType> OnHandReceived;
    event Action OnOpponentConnected;
}
