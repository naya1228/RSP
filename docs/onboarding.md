# RSP 프로젝트 온보딩 가이드

## 한 줄 요약
Godot 4 C# 기반 2인 가위바위보 대전 게임. 10칸 징검다리 위에서 이동하다 만나면 패로 결투.

## 기술 스택
- 엔진: Godot 4 (.NET 버전 필수)
- 언어: C# (.NET 8)
- 멀티플레이어: Ably WebSocket (미구현 — 현재 AiNetworkManager로 AI 대전만 가능)
- 배포: 데스크톱 + 모바일 직접 공유

## 게임 규칙 요약
- 10칸 다리, A는 0번 B는 9번에서 시작
- 첫 턴: 1칸 또는 2칸 전진 선택 / 이후 매 턴: 전진 1칸 or 후진 1칸
- 같은 칸에서 만나면 결투 발생
- 초기 손패 4장 (바위/보/가위 보장 + 랜덤 1장), 덱 2장 남음
- 보충 카운터 3마다 덱에서 1장 드로우 (1회차 강화패 보장, 이후 70% 강화)
- 3결투 2:1 달성 시 강화패 1장 픽 (★바위/★보/★가위 중 선택)
- MaxLoseStreak 이상 연패 or 상대 출발점 도달 시 승리

전체 규칙: `docs/game-design.md`

---

## 파일 구조

```
scenes/
  Main.tscn          # 메인 메뉴 (싱글/멀티/설정/종료 버튼)
  Game.tscn          # 이동 씬 (타일/캐릭터는 Game.cs가 코드로 생성)
  Duel.tscn          # 결투 씬 (패 선택, 결과 표시, 강화패 픽)
  UI.tscn            # 미사용 (Game/Duel 씬으로 기능 분리됨)
  MenuButton.tscn    # 공용 버튼 컴포넌트
  Popup.tscn         # 팝업 베이스 템플릿 (제목/탭/내용/닫기)
  MultiplayerPopup.tscn  # 멀티플레이 팝업 (방 만들기/참가)
  SettingsPopup.tscn     # 설정 팝업 (볼륨 슬라이더)

scripts/
  Main.cs            # 메뉴 버튼 이벤트, 씬 전환, NetworkManager 주입
  Game.cs            # 이동 씬: 보드 렌더링, 타일 클릭 이동 입력
  Duel.cs            # 결투 씬: 손패 카드 버튼, 결과 표시, 강화패 픽 UI
  UI.cs              # 미사용 (빈 파일, Game/Duel로 기능 이전)
  GameManager.cs     # 싱글턴. 게임 상태 전체 관리 (위치/손패/덱/턴/결투/강화)
  INetworkManager.cs # 네트워크 인터페이스 + HandType/MoveDirection enum
  AiNetworkManager.cs    # AI 대전 구현 (이동 70%전진, 결투 랜덤 선택, 강화패 랜덤 픽)
  LocalNetworkManager.cs # 로컬 더미 (현재 미사용)
  Popup.cs           # 팝업 베이스 스크립트
  MultiplayerPopup.cs    # 멀티플레이 팝업 로직
  SettingsPopup.cs       # 설정 팝업 로직

assets/
  menu_theme.tres    # 공용 버튼 스타일 (색상/테두리/폰트)

docs/
  game-design.md     # 전체 기획서
  onboarding.md      # 이 파일
  popup-usage.md     # Popup 컴포넌트 사용법
```

---

## 현재 상태 (2026-04-15 기준)

### 완료
- 메인 메뉴 (4개 버튼, 공용 테마 적용)
- 싱글플레이 → AI와 게임 씬 전환 (AI 대전 완전 동작)
- 이동 씬(Game.tscn) / 결투 씬(Duel.tscn) 분리
- 멀티플레이 팝업 (방 코드 생성/입력 UI, Ably 미연결)
- 설정 팝업 (볼륨 슬라이더)
- GameManager (이동/결투/패 소모/승리 조건 전부 구현)
- 패 보충 시스템 (3턴마다 드로우, 강화패 변환 포함)
- 강화패 시스템 (★바위/★보/★가위 효과, 3결투 2:1 조건 픽)
- 블라인드 결투 (강화 보 효과 시 패 종류 숨김)

### 미완료 (TODO)
- [ ] Ably 연결 (AblyNetworkManager.cs 구현)
- [ ] 효과 칸 효과 내용 (기획 미정)
- [ ] 결투 커밋-리빌 실제 구현 (멀티플레이 시 치팅 방지용)
- [ ] 스프라이트 에셋 교체 (현재 기본 도형)
- [ ] 로컬 2인 모드 (한 화면 동시 플레이) — 심리전 보호를 위해 폐기 검토 중

---

## 핵심 패턴

### 씬 전환
`Main.cs`에서 `GD.Load<PackedScene>()` 후 `Instantiate()` → `AddChild()`

### 게임 상태
`GameManager.Instance`로 어디서든 접근. 이벤트로 UI/보드에 변경 통보.
```csharp
GameManager.Instance.OnBoardChanged += UpdateBoard;
GameManager.Instance.OnStateChanged += OnStateChanged;
```

`GameManager.GameState` 흐름:
```
Lobby → Moving ↔ Duel → PickEnhanced → Moving → ... → GameOver
```

### 씬 구조 (이동/결투 분리)
- `Moving` 상태 → `Game.tscn` 활성
- `Duel` / `PickEnhanced` 상태 → `Duel.tscn` 활성
- 씬 전환은 `GameManager.OnStateChanged` 이벤트 기반

### 네트워크 교체
`AiNetworkManager` → `AblyNetworkManager`로 교체 시 `Main.cs`에서 인스턴스만 바꾸면 됨. `INetworkManager` 인터페이스 구현.

### 팝업 열기
```csharp
var popup = GD.Load<PackedScene>("res://scenes/MultiplayerPopup.tscn").Instantiate();
AddChild(popup);
```
ESC 또는 ✕ 버튼으로 닫힘 (`QueueFree()`).

### 공용 스타일
버튼 스타일 변경은 `assets/menu_theme.tres` 하나만 수정하면 전체 반영.

---

## 주의사항
- `.tscn` 파일은 Godot 에디터가 열면 자동으로 재포맷함 (uid 추가 등) — 정상 동작
- `UI.tscn` / `UI.cs`는 더 이상 사용하지 않음 (`Game.cs`, `Duel.cs`로 기능 분리됨)
- `MultiplayerPopup.tscn`, `SettingsPopup.tscn`은 에디터가 일부 내용을 잘라낸 이력 있음. 씬 내용이 비어 보이면 해당 `.cs` 스크립트 기준으로 노드 트리 재구성 필요
- C# 컴파일을 위해 반드시 **Godot 4 .NET 버전** 사용
