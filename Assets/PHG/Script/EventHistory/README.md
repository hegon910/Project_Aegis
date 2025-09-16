# 이벤트 기록 및 회상 시스템

Unity 프로젝트를 위한 이벤트 기록, 회상, 멀티 엔딩 시스템입니다. EventManager를 직접 수정하지 않고 참조형식으로 동작합니다.

## 주요 기능

### 1. 이벤트 기록 시스템
- 파라미터 이벤트와 서브 이벤트 자동 기록
- 이벤트별 상세 정보 저장 (제목, 설명, 선택지, 파라미터 변화 등)
- 챕터별, 타입별 이벤트 분류
- 로컬 저장 및 로드

### 2. 회상 시스템
- 개별 이벤트 회상
- 챕터별 이벤트 순차 회상
- 타입별 이벤트 회상
- 자동 진행 및 속도 조절

### 3. 멀티 엔딩 시스템
- 파라미터 기반 엔딩
- 이벤트 기반 엔딩
- 선택 기반 엔딩
- 혼합형 엔딩
- 엔딩 진행률 추적

## 시스템 구조

```
EventHistory/
├── EventHistoryData.cs              # 데이터 구조 정의
├── EventHistoryManager.cs           # 이벤트 기록 관리
├── EventHistoryIntegration.cs       # 통합 매니저
├── Extensions/
│   └── EventManagerExtensions.cs    # EventManager 확장
├── UI/
│   ├── EventHistoryUI.cs           # 메인 UI 컨트롤러
│   ├── EventItemUI.cs              # 이벤트 아이템 UI
│   └── EventChoiceUI.cs            # 선택지 UI
├── ReplaySystem/
│   └── EventReplayManager.cs       # 회상 시스템
├── EndingSystem/
│   ├── EndingData.cs               # 엔딩 데이터 구조
│   └── EndingManager.cs            # 엔딩 관리
└── Examples/
    └── EventHistoryExample.cs      # 사용 예시
```

## 사용법

### 1. 기본 설정

```csharp
// EventHistoryIntegration을 씬에 추가
var integration = FindObjectOfType<EventHistoryIntegration>();
if (integration == null)
{
    var go = new GameObject("EventHistoryIntegration");
    integration = go.AddComponent<EventHistoryIntegration>();
}

// EventManager에 이벤트 기록 기능 활성화
EventManager.Instance.EnableEventHistory();
```

### 2. 이벤트 기록 조회

```csharp
// 모든 이벤트 가져오기
var allEvents = integration.GetEventHistory();

// 특정 챕터의 이벤트 가져오기
var chapterEvents = integration.GetEventsByChapter(1);

// 완료된 이벤트만 가져오기
var completedEvents = historyManager.GetCompletedEvents();
```

### 3. 이벤트 회상

```csharp
// 개별 이벤트 회상
await integration.ReplayEvent(eventRecord);

// 챕터 회상
await integration.ReplayChapter(1);

// 타입별 회상
var replayManager = EventReplayManager.Instance;
await replayManager.ReplayEventsByType(EventType.ParameterEvent);
```

### 4. 엔딩 시스템

```csharp
// 해금된 엔딩 조회
var unlockedEndings = integration.GetUnlockedEndings();

// 엔딩 시청
integration.ViewEnding(endingData);

// 엔딩 완료
integration.CompleteEnding(endingData);
```

## UI 설정

### 1. EventHistoryUI 설정
- `historyPanel`: 이벤트 기록 패널
- `eventDetailPanel`: 이벤트 상세 패널
- `eventItemPrefab`: 이벤트 아이템 프리팹
- `choiceItemPrefab`: 선택지 아이템 프리팹

### 2. 필터링 옵션
- 챕터별 필터
- 타입별 필터 (파라미터/서브 이벤트)
- 완료된 이벤트만 표시

## 데이터 구조

### EventRecord
```csharp
public class EventRecord
{
    public int eventId;                    // 이벤트 ID
    public EventType eventType;            // 이벤트 타입
    public int chapter;                    // 발생한 챕터
    public long timestamp;                 // 발생 시간
    public string eventTitle;              // 이벤트 제목
    public string eventDescription;        // 이벤트 설명
    public List<EventChoice> choices;      // 선택지들
    public EventChoice selectedChoice;     // 선택한 선택지
    public Dictionary<ParameterType, int> parameterChanges; // 파라미터 변화
    public bool isCompleted;               // 완료 여부
}
```

### EndingData
```csharp
public class EndingData
{
    public string endingId;                // 엔딩 고유 ID
    public string endingName;              // 엔딩 이름
    public string endingDescription;       // 엔딩 설명
    public EndingType endingType;          // 엔딩 타입
    public Dictionary<ParameterType, int> requiredParameters; // 필요한 파라미터
    public List<int> requiredEvents;       // 필요한 이벤트들
    public List<EndingChoiceRequirement> requiredChoices; // 필요한 선택들
}
```

## 이벤트 시스템

### EventHistoryManager 이벤트
- `OnEventRecorded`: 이벤트가 기록될 때
- `OnEventCompleted`: 이벤트가 완료될 때
- `OnChapterCompleted`: 챕터가 완료될 때

### EventReplayManager 이벤트
- `OnReplayStarted`: 회상이 시작될 때
- `OnReplayCompleted`: 회상이 완료될 때
- `OnReplayPaused`: 회상이 일시정지될 때
- `OnReplayResumed`: 회상이 재개될 때

### EndingManager 이벤트
- `OnEndingUnlocked`: 엔딩이 해금될 때
- `OnEndingViewed`: 엔딩이 시청될 때
- `OnEndingCompleted`: 엔딩이 완료될 때

## 주의사항

1. **EventManager 수정 금지**: EventManager를 직접 수정하지 않고 확장 메서드를 사용합니다.
2. **참조형식 개발**: 모든 기능은 참조형식으로 구현되어 기존 코드와 충돌하지 않습니다.
3. **데이터 동기화**: DataManager의 PlayerData와 동기화되어 저장됩니다.
4. **메모리 관리**: 대량의 이벤트 기록 시 메모리 사용량을 고려해야 합니다.

## 확장 가능성

1. **멀티 엔딩**: 다양한 조건의 엔딩을 쉽게 추가할 수 있습니다.
2. **회상 모드**: 다양한 회상 방식 (빠르게, 자동 진행 등)을 지원합니다.
3. **UI 커스터마이징**: UI 컴포넌트들을 자유롭게 커스터마이징할 수 있습니다.
4. **데이터 내보내기**: 이벤트 기록을 외부 파일로 내보낼 수 있습니다.

## 문제 해결

### 일반적인 문제
1. **이벤트가 기록되지 않음**: EventManager.EnableEventHistory()가 호출되었는지 확인
2. **UI가 표시되지 않음**: EventHistoryUI 컴포넌트가 올바르게 설정되었는지 확인
3. **엔딩이 해금되지 않음**: 엔딩 조건이 올바르게 설정되었는지 확인

### 디버그 모드
```csharp
// EventHistoryIntegration에서 디버그 로깅 활성화
integration.enableDebugLogging = true;

// 시스템 상태 확인
Debug.Log(integration.GetSystemStatus());
```


