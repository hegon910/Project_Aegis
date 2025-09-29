# 개발 모드 로그인 선택 기능 사용법

## 개요
FirebaseManager에 개발 모드에서 GPGS 로그인과 게스트 로그인을 선택할 수 있는 기능을 추가했습니다. 이를 통해 개발 단계에서는 두 가지 로그인 방식을 자유롭게 테스트할 수 있습니다.

## 주요 기능

### 1. 개발 모드 설정
- **enableDevelopmentMode**: 개발 모드 활성화/비활성화
- **showLoginChoiceInDevelopment**: 개발 모드에서 로그인 선택 UI 표시 여부

### 2. 로그인 선택 UI
- GPGS 로그인 버튼
- 게스트 로그인 버튼
- 설명 텍스트

### 3. 동작 방식
- **개발 모드 ON**: 로그인 선택 UI 표시 → 사용자가 선택
- **개발 모드 OFF**: 기존 로직 (GPGS 시도 → 실패 시 게스트)

## 설정 방법

### 1. Inspector 설정
FirebaseManager 컴포넌트의 Inspector에서 다음 항목들을 설정합니다:

```
Development Settings:
├── Enable Development Mode: ✓ (체크)
└── Show Login Choice In Development: ✓ (체크)

Development Login Choice UI:
├── Login Choice Panel: 로그인 선택 패널 GameObject
├── GPGS Login Button: GPGS 로그인 버튼
├── Guest Login Button: 게스트 로그인 버튼
├── Login Choice Title: 제목 텍스트
└── Login Choice Description: 설명 텍스트
```

### 2. UI 구성 예시
```
Canvas
├── LoginChoicePanel (Image - 반투명 배경)
│   ├── TitleText ("로그인 방식 선택")
│   ├── DescriptionText (설명)
│   ├── GPGSLoginButton ("GPGS 로그인")
│   └── GuestLoginButton ("게스트 로그인")
```

## 사용 시나리오

### 개발 단계
1. **enableDevelopmentMode = true**
2. **showLoginChoiceInDevelopment = true**
3. 앱 실행 시 로그인 선택 UI 표시
4. 원하는 로그인 방식 선택하여 테스트

### 출시 단계
1. **enableDevelopmentMode = false**
2. 기존 로직으로 동작 (GPGS → 실패 시 게스트)
3. 사용자는 자동으로 로그인 처리됨

## 런타임 제어

### 코드로 설정 변경
```csharp
// 개발 모드 활성화/비활성화
FirebaseManager.Instance.SetDevelopmentMode(true);

// 로그인 선택 UI 표시/숨김
FirebaseManager.Instance.SetShowLoginChoice(true);

// 로그인 선택 UI 다시 표시 (테스트용)
FirebaseManager.Instance.ShowLoginChoiceAgain();
```

## 장점

### 1. 개발 효율성
- **선택적 테스트**: 원하는 로그인 방식만 테스트 가능
- **빠른 전환**: GPGS ↔ 게스트 로그인 간 빠른 전환
- **디버깅 용이**: 각 로그인 방식의 동작을 개별적으로 확인

### 2. 출시 안정성
- **기존 로직 보존**: 출시 시 기존 동작 방식 유지
- **플래그 제어**: 간단한 설정으로 개발/출시 모드 전환
- **코드 중복 없음**: 하나의 스크립트에서 모든 로직 관리

### 3. 유지보수성
- **명확한 분리**: 개발용/출시용 로직이 명확히 구분
- **설정 기반**: Inspector에서 쉽게 설정 변경
- **확장 가능**: 추가 로그인 방식도 쉽게 추가 가능

## 주의사항

### 1. 빌드 설정
- **개발 빌드**: `enableDevelopmentMode = true`
- **출시 빌드**: `enableDevelopmentMode = false`
- 빌드 시 설정을 확인하여 올바른 모드로 빌드하세요

### 2. UI 설정
- 로그인 선택 UI가 제대로 연결되어 있는지 확인
- 버튼 이벤트가 자동으로 연결되므로 수동 설정 불필요
- UI가 보이지 않으면 Inspector 설정을 확인하세요

### 3. 테스트 환경
- **안드로이드 빌드**에서만 GPGS 로그인 테스트 가능
- 에디터에서는 GPGS 로그인이 동작하지 않습니다
- 게스트 로그인은 에디터에서도 테스트 가능

## 문제 해결

### 1. 로그인 선택 UI가 보이지 않는 경우
- `enableDevelopmentMode`가 true인지 확인
- `showLoginChoiceInDevelopment`가 true인지 확인
- `loginChoicePanel`이 Inspector에 연결되어 있는지 확인

### 2. 버튼이 동작하지 않는 경우
- 버튼이 Inspector에 올바르게 연결되어 있는지 확인
- 버튼의 Interactable이 true인지 확인
- 로그를 확인하여 이벤트가 제대로 연결되었는지 확인

### 3. 개발 모드가 동작하지 않는 경우
- FirebaseManager 인스턴스가 올바르게 생성되었는지 확인
- Start 메서드가 호출되었는지 확인
- 이미 로그인된 상태에서는 선택 UI가 표시되지 않습니다

이 기능을 통해 개발 단계에서는 유연하게 로그인 방식을 테스트하고, 출시 시에는 기존의 안정적인 로그인 플로우를 유지할 수 있습니다.
