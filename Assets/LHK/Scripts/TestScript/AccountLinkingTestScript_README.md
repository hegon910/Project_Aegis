# AccountLinkingTestScript 사용법

## 개요
이 스크립트는 안드로이드 빌드 환경에서 게스트 계정과 GPGS 계정 연동 기능을 테스트하기 위한 별도의 테스트 스크립트입니다.

## 주요 기능

### 1. 기본 테스트 기능
- **게스트 로그인 테스트**: 익명 로그인 기능 테스트
- **GPGS 로그인 테스트**: Google Play Games Services 로그인 테스트
- **계정 연동 테스트**: 게스트 계정을 GPGS 계정에 연결하는 기능 테스트
- **로그아웃 테스트**: 현재 로그인된 계정에서 로그아웃
- **상태 확인**: 현재 로그인 상태와 계정 정보 표시
- **데이터 리셋**: 테스트 데이터 초기화

### 2. 고급 테스트 기능
- **자동 테스트 시퀀스**: 순차적으로 모든 테스트를 자동 실행
- **성능 테스트**: 반복적인 로그인/로그아웃으로 성능 측정
- **에러 상황 테스트**: 잘못된 상황에서의 동작 확인

## 사용 방법

### 1. 스크립트 설정
1. 빈 GameObject를 생성하고 `AccountLinkingTestScript` 컴포넌트를 추가합니다.
2. 필요한 UI 요소들을 Inspector에서 연결합니다:
   - `guestLoginButton`: 게스트 로그인 버튼
   - `gpgsLoginButton`: GPGS 로그인 버튼
   - `linkAccountButton`: 계정 연동 버튼
   - `logoutButton`: 로그아웃 버튼
   - `resetDataButton`: 데이터 리셋 버튼
   - `showStatusButton`: 상태 표시 버튼
   - `statusText`: 로그 메시지를 표시할 Text 컴포넌트
   - `accountInfoText`: 계정 정보를 표시할 Text 컴포넌트
   - `statusScrollRect`: 로그 스크롤 영역
   - `testPanel`: 테스트 패널 GameObject
   - `toggleTestPanelButton`: 테스트 패널 토글 버튼

### 2. UI 구성 예시
```
Canvas
├── AccountLinkingTestPanel (Image - 반투명 배경)
│   ├── TestPanel (Image - 테스트 패널)
│   │   ├── GuestLoginButton
│   │   ├── GPGSLoginButton
│   │   ├── LinkAccountButton
│   │   ├── LogoutButton
│   │   ├── ResetDataButton
│   │   ├── ShowStatusButton
│   │   ├── StatusScrollRect
│   │   │   └── StatusText
│   │   └── AccountInfoText
│   └── ToggleTestPanelButton
```

### 3. 테스트 시나리오

#### 시나리오 1: 기본 게스트 → 연동 플로우
1. 게스트 로그인
2. 계정 연동
3. 로그아웃
4. GPGS 로그인

#### 시나리오 2: GPGS 우선 로그인
1. GPGS 로그인
2. 로그아웃
3. 게스트 로그인

#### 시나리오 3: 데이터 연동 테스트
1. 게스트 로그인
2. 게임 데이터 생성/수정
3. 계정 연동
4. 데이터 동기화 확인

## 주의사항

### 1. 빌드 환경
- **에디터에서는 동작하지 않습니다**
- 반드시 **안드로이드 빌드**에서 테스트해야 합니다
- GPGS 설정이 올바르게 되어 있어야 합니다

### 2. 테스트 전 준비사항
- Google Play Console에서 앱이 등록되어 있어야 합니다
- GPGS 설정이 완료되어 있어야 합니다
- Firebase 프로젝트 설정이 완료되어 있어야 합니다

### 3. 테스트 데이터 관리
- 테스트 중 생성된 데이터는 `ResetTestData()` 메서드로 초기화할 수 있습니다
- 실제 사용자 데이터와 혼동되지 않도록 주의하세요

## 디버그 정보

### 로그 메시지
- 모든 테스트 동작이 로그로 기록됩니다
- 타임스탬프와 함께 상세한 정보가 표시됩니다
- 스크롤 가능한 텍스트 영역에서 확인할 수 있습니다

### 계정 정보 표시
- 현재 로그인된 계정의 UID
- 로그인 타입 (Guest, GPGS, Linked)
- 계정 생성 시간
- 마지막 로그인 시간
- 표시 이름 및 이메일 (있는 경우)

## 문제 해결

### 1. GPGS 로그인 실패
- Google Play Games 앱이 설치되어 있는지 확인
- Google 계정이 올바르게 설정되어 있는지 확인
- 네트워크 연결 상태 확인

### 2. 계정 연동 실패
- 게스트 계정으로 로그인되어 있는지 확인
- GPGS 로그인이 성공했는지 확인
- Firebase 설정이 올바른지 확인

### 3. 데이터 동기화 문제
- Firebase Database 규칙 확인
- 네트워크 연결 상태 확인
- DataManager의 서버 업로드 로직 확인

## 추가 기능

### 자동 테스트
```csharp
// 자동 테스트 시퀀스 실행
testScript.RunAutomaticTestSequence();

// 성능 테스트 실행
testScript.RunPerformanceTest();

// 에러 상황 테스트 실행
testScript.TestErrorScenarios();
```

### 로그 관리
```csharp
// 로그 초기화
testScript.ClearLog();
```

이 테스트 스크립트를 통해 게스트 계정과 GPGS 계정 연동 기능을 안전하고 체계적으로 테스트할 수 있습니다.
