# RSP 프로젝트 온보딩 가이드

## 한 줄 요약
Godot 4 C# 기반 2인 온라인 가위바위보 대전 게임. 10칸 징검다리 위에서 이동하다 만나면 패로 결투.

## 기술 스택
- 엔진: Godot 4 (.NET 버전 필수)
- 언어: C# (.NET 8)
- 멀티플레이어: Ably WebSocket (현재 미연결, LocalNetworkManager 더미로 대체 중)
- 배포: 데스크톱 + 모바일 직접 공유

## 게임 규칙 요약
- 10칸 다리, A는 0번 B는 9번에서 시작
- 첫 턴 2칸 전진, 이후 매 턴 전진/후진 1칸 선택
- 같은 칸에서 만나면 결투 발생
- 패 구성: 가위2/바위2/보2, 소모되면 선택 불가
- 3연패 또는 상대 출발점 도달 시 승리
- 결투는 커밋-리빌 패턴 (동시 공개, 치팅 방지)

전체 규칙: `docs/game-design.md`

---

## 파일 구조

```
scenes/
  Main.tscn          # 메인 메뉴 (싱글/멀티/설정/종료 버튼)
  Game.tscn          # 게임 보드 (타일/캐릭터는 Game.cs가 코드로 생성)
  UI.tscn            # 게임 HUD (버튼, 레이블)
  MenuButton.tscn    # 공용 버튼 컴포넌트
  Popup.tscn         # 팝업 베이스 템플릿 (제목/탭/내용/닫기)
  MultiplayerPopup.tscn  # 멀티플레이 팝업 (방 만들기/참가)
  SettingsPopup.tscn     # 설정 팝업 (볼륨 슬라이더)

scripts/
  Main.cs            # 메뉴 버튼 이벤트, 씬 전환
  Game.cs            # 보드 렌더링 (타일 10개, 플레이어 도형)
  UI.cs              # HUD 버튼/레이블 연결
  GameManager.cs     # 싱글턴. 게임 상태 전체 관리 (위치/패/턴/결투)
  INetworkManager.cs # 네트워크 인터페이스 + HandType/MoveDirection enum
  LocalNetworkManager.cs  # 로컬 더미 (같은 화면 2인 교대 테스트용)
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

## 현재 상태 (2026-04-09 기준)

### 완료
- 메인 메뉴 (4개 버튼, 공용 테마 적용)
- 싱글플레이 → AI와 게임 씬 전환 (AI 상대 대전 가능)
- 멀티플레이 팝업 (방 코드 생성/입력 UI, Ably 미연결)
- 설정 팝업 (볼륨 슬라이더)
- GameManager (이동/결투/패 소모/승리 조건 전부 구현)
- 입력 체계 (이동: 턴제, 결투: 동시 입력 및 정보 은폐 구현)

### 미완료 (TODO)
- [ ] Ably 연결 (AblyNetworkManager.cs 구현)
- [ ] 효과 칸 효과 내용 (기획 미정)
- [ ] 3턴마다 랜덤 패 1장 추가
- [ ] 결투 커밋-리빌 실제 구현 (멀티플레이 시 치팅 방지용)
- [ ] 스프라이트 에셋 교체 (현재 기본 도형)

---

## 핵심 패턴

### 씬 전환
`Main.cs`에서 `GD.Load<PackedScene>()` 후 `Instantiate()` → `AddChild()`

### 게임 상태
`GameManager.Instance`로 어디서든 접근. 이벤트로 UI/보드에 변경 통보.
```csharp
GameManager.Instance.OnBoardChanged += UpdateBoard;
```

### 네트워크 교체
`LocalNetworkManager` → `AblyNetworkManager`로 교체 시 `Main.cs`에서 인스턴스만 바꾸면 됨. `INetworkManager` 인터페이스 구현.

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
- `MultiplayerPopup.tscn`, `SettingsPopup.tscn`은 에디터가 일부 내용을 잘라낸 이력 있음. 씬 내용이 비어 보이면 해당 `.cs` 스크립트 기준으로 노드 트리 재구성 필요
- C# 컴파일을 위해 반드시 **Godot 4 .NET 버전** 사용
