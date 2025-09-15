# 간단한 이벤트 회상 시스템

기존 게임에 최소한의 코드로 이벤트 회상 기능을 추가하는 시스템입니다.

## 📁 파일 구조

```
Assets/PHG/Script/EventHistory/
├── SimpleEventRecord.cs          # 이벤트 기록 데이터
├── SimpleEventHistoryManager.cs  # 이벤트 기록 관리
├── SimpleReplayPanel.cs          # 회상 패널 UI
└── README_Simple.md              # 사용법 가이드
```

## 🚀 빠른 설정

### 1. 기본 오브젝트 생성

1. **Hierarchy에서 빈 GameObject 생성**
   - 이름: `EventHistorySystem`

2. **스크립트 추가**
   - `SimpleEventHistoryManager` 스크립트 추가

### 2. 회상 UI 설정

1. **새 Canvas 생성**
   - `UI > Canvas` 생성
   - 이름: `ReplayCanvas`
   - Canvas Scaler 설정: Scale With Screen Size (1920x1080)

2. **회상 패널 생성**
   - Canvas 하위에 빈 GameObject 생성
   - 이름: `ReplayPanel`
   - `SimpleReplayPanel` 스크립트 추가

3. **UI 요소들 생성**
   ```
   ReplayCanvas
   ├── ReplayPanel
   │   ├── Background (Image)
   │   ├── TitleText (TextMeshPro)
   │   ├── EventScrollView (ScrollRect)
   │   │   └── Viewport
   │   │       └── Content (이벤트 리스트)
   │   ├── StatusText (TextMeshPro)
   │   ├── CloseButton (Button)
   │   └── ClearButton (Button)
   ```

### 3. Inspector 설정

**SimpleReplayPanel 설정:**
- `Replay Canvas`: ReplayCanvas 오브젝트
- `Replay Panel`: ReplayPanel 오브젝트
- `Open Button`: 회상 열기 버튼 (기존 게임 UI에)
- `Close Button`: 닫기 버튼
- `Event Scroll Rect`: 스크롤 뷰
- `Event List Parent`: Content 오브젝트
- `Status Text`: 상태 텍스트
- `Clear Button`: 초기화 버튼
- `Korean Font`: 한글 폰트 (선택사항)

## 🎮 사용법

### 1. 자동 이벤트 기록
- 게임이 시작되면 자동으로 이벤트가 기록됩니다
- EventManager의 이벤트들이 자동으로 감지되어 기록됩니다

### 2. 회상 패널 열기
- `Open Button`을 클릭하면 회상 패널이 열립니다
- 기록된 모든 이벤트가 목록으로 표시됩니다

### 3. 이벤트 확인
- 각 이벤트 아이템을 클릭하면 상세 정보를 확인할 수 있습니다
- 이벤트 제목, 설명, 선택한 답변, 발생 시간이 표시됩니다

### 4. 기록 초기화
- `Clear Button`을 클릭하면 모든 기록이 초기화됩니다

## 🔧 커스터마이징

### 1. 이벤트 정보 수정
`SimpleEventHistoryManager.cs`의 `OnParameterEvent`와 `OnSubEvent` 메서드에서:
- 이벤트 제목과 설명을 CSV에서 가져오도록 수정
- 선택한 답변을 실제 사용자 선택에 따라 저장

### 2. UI 스타일 수정
`SimpleReplayPanel.cs`의 `CreateEventItem` 메서드에서:
- 이벤트 아이템의 색상, 크기, 레이아웃 수정
- 폰트 크기와 색상 조정

### 3. 추가 기능
- 챕터별 필터링
- 이벤트 타입별 필터링
- 검색 기능
- 정렬 기능

## ⚠️ 주의사항

1. **EventManager 의존성**: 기존 EventManager가 있어야 작동합니다
2. **DataManager 의존성**: DataManager의 PlayerData를 사용합니다
3. **폰트 설정**: 한글 폰트를 사용하려면 Inspector에서 설정해야 합니다

## 📝 예시 코드

### 회상 패널 열기
```csharp
// 다른 스크립트에서 회상 패널 열기
var replayPanel = FindObjectOfType<SimpleReplayPanel>();
if (replayPanel != null)
{
    replayPanel.OpenReplayPanel();
}
```

### 이벤트 기록 수동 추가
```csharp
// 수동으로 이벤트 기록 추가
var historyManager = SimpleEventHistoryManager.Instance;
if (historyManager != null)
{
    var record = new SimpleEventRecord(1001, "Parameter", 1, "테스트 이벤트", "설명", "선택한 답변");
    // historyManager에 추가하는 메서드가 필요하면 추가 구현
}
```

이제 간단하고 실용적인 이벤트 회상 시스템이 완성되었습니다!

