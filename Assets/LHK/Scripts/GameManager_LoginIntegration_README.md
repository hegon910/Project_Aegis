# GameManager와 FirebaseManager 로그인 통합

## 문제점 발견
기존 코드에서 **GameManager와 FirebaseManager가 독립적으로 로그인을 처리**하여 중복 로그인 시도가 발생할 수 있었습니다.

### 기존 문제점
1. **GameManager (라인 172-173)**: `PlayGamesPlatform.Instance.Authenticate(OnAuthenticated)` 호출
2. **GameManager (라인 903)**: `OnAuthenticated`에서 `FirebaseManager.Instance.GPGSLogin()` 호출  
3. **FirebaseManager**: 자체적으로 GPGS 로그인 처리

→ **중복 로그인 시도** 발생 가능

## 해결 방안

### 1. GameManager 수정사항

#### A. Start() 메서드 수정
```csharp
// 기존: 직접 GPGS 인증 시도
if (Application.platform == RuntimePlatform.Android) 
    PlayGamesPlatform.Instance.Authenticate(OnAuthenticated);
else 
    OnAuthenticated(SignInStatus.Success);

// 수정: FirebaseManager 초기화 대기 후 로그인 처리
if (FirebaseManager.Instance != null)
{
    HandleLoginFlow();
}
else
{
    FirebaseManager.OnFirebaseManagerInitialized += HandleLoginFlow;
}
```

#### B. 새로운 메서드 추가
- **`HandleLoginFlow()`**: FirebaseManager 초기화 후 로그인 플로우 처리
- **`OnLoginStateChanged()`**: FirebaseManager의 로그인 상태 변경 이벤트 처리

#### C. GameState.Login 상태 처리 수정
```csharp
case GameState.Login:
    // FirebaseManager를 통해 로그인 처리 (중복 방지)
    Debug.Log("[GameManager] Login 상태 - FirebaseManager를 통해 로그인 처리");
    if (FirebaseManager.Instance != null)
    {
        // FirebaseManager가 개발 모드에서 로그인 선택 UI를 표시하도록 함
    }
    break;
```

### 2. 통합된 로그인 흐름

#### 개발 모드 (enableDevelopmentMode = true)
1. **GameManager.Start()** → FirebaseManager 초기화 대기
2. **FirebaseManager 초기화 완료** → `HandleLoginFlow()` 호출
3. **FirebaseManager** → 로그인 선택 UI 표시
4. **사용자 선택** → GPGS 또는 게스트 로그인
5. **로그인 완료** → `OnLoginStateChanged()` → 메인 메뉴로 이동

#### 출시 모드 (enableDevelopmentMode = false)
1. **GameManager.Start()** → FirebaseManager 초기화 대기
2. **FirebaseManager 초기화 완료** → `HandleLoginFlow()` 호출
3. **FirebaseManager** → 자동 GPGS 로그인 시도 → 실패 시 게스트 로그인
4. **로그인 완료** → `OnLoginStateChanged()` → 메인 메뉴로 이동

## 장점

### 1. 중복 방지
- **단일 책임**: FirebaseManager만 로그인 처리
- **중복 제거**: GameManager의 직접적인 GPGS 호출 제거
- **일관성**: 모든 로그인 로직이 FirebaseManager에 집중

### 2. 개발 효율성
- **통합 관리**: 로그인 관련 모든 로직이 FirebaseManager에 있음
- **디버깅 용이**: 로그인 플로우를 한 곳에서 추적 가능
- **유지보수성**: 로그인 로직 변경 시 FirebaseManager만 수정

### 3. 확장성
- **새로운 로그인 방식**: FirebaseManager에만 추가하면 됨
- **이벤트 기반**: GameManager는 로그인 상태 변경만 감지
- **느슨한 결합**: GameManager와 FirebaseManager 간 의존성 최소화

## 주의사항

### 1. 이벤트 구독 해제
GameManager가 파괴될 때 이벤트 구독을 해제해야 합니다:
```csharp
private void OnDestroy()
{
    FirebaseManager.OnFirebaseManagerInitialized -= HandleLoginFlow;
    FirebaseManager.OnLoginStateChanged -= OnLoginStateChanged;
}
```

### 2. 초기화 순서
- FirebaseManager가 GameManager보다 먼저 초기화되어야 함
- 씬에서 FirebaseManager GameObject의 순서 확인 필요

### 3. 호환성
- 기존 `OnAuthenticated` 메서드는 호환성을 위해 유지
- 하지만 실제로는 사용되지 않음 (경고 로그 출력)

## 테스트 시나리오

### 1. 개발 모드 테스트
1. `enableDevelopmentMode = true` 설정
2. 앱 실행 → 로그인 선택 UI 표시 확인
3. GPGS/게스트 로그인 선택 → 메인 메뉴 이동 확인

### 2. 출시 모드 테스트  
1. `enableDevelopmentMode = false` 설정
2. 앱 실행 → 자동 GPGS 로그인 시도 확인
3. 실패 시 게스트 로그인 → 메인 메뉴 이동 확인

### 3. 이미 로그인된 상태 테스트
1. 이전에 로그인된 상태로 앱 실행
2. 로그인 선택 UI 없이 바로 메인 메뉴로 이동 확인

이 통합을 통해 로그인 플로우가 더 안정적이고 유지보수하기 쉬워졌습니다.
