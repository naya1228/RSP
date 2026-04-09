# Popup 컴포넌트 사용법

## 파일 위치
- 씬: `scenes/Popup.tscn`
- 스크립트: `scripts/Popup.cs`

## 구조

```
Popup (CanvasLayer)
├── Overlay          ← 반투명 배경 (클릭 차단)
└── Panel
    └── Layout (VBox)
        ├── TitleBar     ← 제목 텍스트 + 우측 닫기(✕) 버튼
        ├── TabBar       ← 탭 버튼 영역
        └── ContentArea  ← 실제 내용 영역
```

## 닫기 동작
- 닫기 버튼(✕) 클릭
- `ESC` 키 입력
- 코드에서 `popup.Close()` 호출

## 코드에서 인스턴스화

```csharp
var popupScene = GD.Load<PackedScene>("res://scenes/Popup.tscn");
var popup = popupScene.Instantiate<Popup>();

popup.Title = "멀티플레이";
popup.AddTab("방 만들기");
popup.AddTab("방 참가");
popup.AddContent(myContentNode);

AddChild(popup);
```

## 탭 추가

```csharp
popup.AddTab("탭 이름");
```

## 콘텐츠 추가

콘텐츠 영역(`ContentArea/Content`)에 원하는 노드를 추가한다.

```csharp
var label = new Label { Text = "내용" };
popup.AddContent(label);
```

## 스타일

버튼 스타일은 `assets/menu_theme.tres`를 공유한다.
닫기 버튼 포함 모든 버튼이 동일한 테마를 따른다.
