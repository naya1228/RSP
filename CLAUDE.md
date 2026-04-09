# RSP 프로젝트

Godot 4 C# 기반 온라인 2인 가위바위보 게임.

## 하네스: RSP 개발팀

**목표:** Godot 4 C# + Ably 기반 데스크톱/모바일 게임 개발을 에이전트 팀으로 처리

**기술 스택:**
- 엔진: Godot 4 (C# / .NET)
- 멀티플레이어: Ably (WebSocket pub/sub)
- 동기화: 호스트-클라이언트 구조
- 결투 공정성: 커밋-리빌 패턴 (SHA256 해시)
- 배포: 데스크톱 + 모바일 (직접 공유)

**에이전트 팀:**

| 에이전트 | 역할 |
|---------|------|
| unity-dev | Godot 4 C# 구현, Ably 네트워크, WebGL 빌드 |
| parrying | 코드 리뷰, 설계 고민, 페어 프로그래밍 (메스가키 스타일) |
| daki | 기술 스펙 검증, 공식 문서 확인 |

**스킬:**

| 스킬 | 용도 | 사용 에이전트 |
|------|------|-------------|
| rsp-dev | RSP 게임 개발 전반 오케스트레이션 | 전체 팀 |

**실행 규칙:**
- 구현, 코드 리뷰, 기술 스펙 확인 등 개발 작업은 `rsp-dev` 스킬로 처리
- 기획 문서 작성/수정은 스킬 없이 직접 처리
- 모든 에이전트는 `model: "opus"` 사용
- 중간 산출물: `_workspace/` 디렉토리
- 기획 문서: `docs/game-design.md`

**디렉토리 구조:**
```
.claude/
├── agents/
│   ├── unity-dev.md
│   ├── parrying.md
│   └── daki.md
└── skills/
    └── rsp-dev/
        └── SKILL.md
docs/
└── game-design.md
scenes/
scripts/
assets/
├── sprites/
└── audio/
addons/
```

**변경 이력:**

| 날짜 | 변경 내용 | 대상 | 사유 |
|------|----------|------|------|
| 2026-04-06 | 초기 구성 | 전체 | RSP 프로젝트 시작 |
| 2026-04-08 | Unity → Godot 4 전환 | 전체 | 엔진 변경 (Photon → Ably, GDScript → C#, WebGL → 데스크톱/모바일) |
