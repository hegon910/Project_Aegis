# 업적 해금 알림 패널 설정 가이드

업적이 해금될 때 왼쪽에서 슬라이드로 나타나는 알림 패널을 설정하는 방법입니다.

## 🚀 빠른 설정

### 자동 설정 (권장)
- **GameManager가 자동으로 AchievementNotificationManager를 초기화합니다**
- **UI 요소들이 자동으로 생성됩니다**
- **별도의 수동 설정이 필요하지 않습니다**

### 수동 설정 (고급 사용자용)

1. **메인 Canvas 하위에 빈 GameObject 생성**
   - 이름: `AchievementNotificationPanel`
   - `AchievementNotificationPanel` 스크립트 추가

2. **UI 요소 구조 생성**
   ```
   AchievementNotificationPanel
   ├── Background (Image) - 배경
   ├── AchievementIcon (Image) - 업적 아이콘
   ├── TitleText (TextMeshPro) - "업적 해금!" 텍스트
   └── AchievementNameText (TextMeshPro) - 업적 이름
   ```

3. **Inspector 설정**

**AchievementNotificationPanel 스크립트 설정:**
- `Panel Rect`: AchievementNotificationPanel의 RectTransform
- `Achievement Icon`: 업적 아이콘 이미지 컴포넌트
- `Title Text`: "업적 해금!" 텍스트 컴포넌트
- `Achievement Name Text`: 업적 이름 텍스트 컴포넌트

**애니메이션 설정:**
- `Slide In Duration`: 0.5 (슬라이드 인 시간)
- `Display Duration`: 3.0 (표시 시간)
- `Slide Out Duration`: 0.3 (슬라이드 아웃 시간)
- `Slide Ease`: OutBack (슬라이드 이징)

**패널 설정:**
- `Panel Width`: 400 (패널 너비)
- `Panel Height`: 120 (패널 높이)
- `Slide In Distance`: 450 (슬라이드 인 거리)

### 3. UI 스타일링

**Background Image 설정:**
- Color: 반투명 검은색 (예: RGBA(0, 0, 0, 0.8))
- Image Type: Sliced (둥근 모서리를 위해)
- Source Image: 둥근 모서리 스프라이트

**Text 스타일:**
- Title Text: 굵은 폰트, 흰색, 중앙 정렬
- Achievement Name Text: 일반 폰트, 노란색, 중앙 정렬

**아이콘 설정:**
- Achievement Icon: 64x64 크기, 왼쪽 정렬

## 🎮 사용법

### 자동 작동
- 업적이 해금되면 자동으로 알림이 표시됩니다
- `AchievementNotificationManager`가 `AchievementManager.OnAchievementUnlocked` 이벤트를 구독하여 작동
- **게임 중 다른 캔버스가 비활성화되어도 업적 알림은 계속 작동합니다**

### 수동 테스트
- Inspector에서 "Test Achievement Notification" 컨텍스트 메뉴 클릭
- 테스트 업적 알림이 표시됩니다

### 데이터 리셋
- `ProgressResetService.ResetProgressAndHistory()` 호출 시 업적 데이터도 함께 초기화됩니다
- `resetAchievements` 매개변수로 업적 초기화 여부를 제어할 수 있습니다

### 애니메이션 제어
```csharp
// AchievementNotificationManager를 통한 제어
AchievementNotificationManager.Instance.EnsureNotificationSystemActive();
AchievementNotificationManager.Instance.TestNotification();

// 직접 패널 제어
notificationPanel.StopAnimation();
notificationPanel.ShowAchievementUnlocked(achievementData);
```

## 🎨 커스터마이징

### 애니메이션 시간 조정
- `slideInDuration`: 슬라이드 인 속도
- `displayDuration`: 화면에 표시되는 시간
- `slideOutDuration`: 슬라이드 아웃 속도

### 위치 조정
- `slideInDistance`: 화면 밖에서 들어오는 거리
- `visiblePosition`: 화면에 표시되는 최종 위치

### 패널 크기 조정
- `panelWidth`: 패널 너비
- `panelHeight`: 패널 높이

## 📝 주의사항

1. **자동 초기화**: GameManager가 자동으로 시스템을 초기화합니다
2. **DOTween**: DOTween 패키지가 필요합니다
3. **영구 캔버스**: 업적 알림 캔버스는 DontDestroyOnLoad로 설정되어 씬 전환 시에도 유지됩니다
4. **높은 우선순위**: 캔버스 Sort Order가 100으로 설정되어 다른 UI보다 위에 표시됩니다

## 🔧 문제 해결

### 알림이 나타나지 않는 경우
- AchievementManager가 올바르게 설정되어 있는지 확인
- AchievementNotificationManager.Instance가 null인지 확인
- Inspector에서 "Check Notification System Status" 컨텍스트 메뉴로 상태 확인

### 캔버스가 비활성화되는 경우
- AchievementNotificationManager가 자동으로 캔버스를 다시 활성화합니다
- `EnsureNotificationSystemActive()` 메서드로 강제 활성화 가능

### 애니메이션이 부자연스러운 경우
- DOTween 패키지 설치 확인
- 애니메이션 시간과 이징 설정 조정
- 패널 크기와 위치 재조정

### 데이터 리셋 관련
- `ProgressResetService.ResetProgressAndHistory(resetAchievements: true)` 호출 시 업적도 초기화됩니다
- 업적만 초기화하려면 `AchievementManager.Instance.ResetAllAchievements()` 직접 호출
