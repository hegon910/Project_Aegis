# 게스트 로그인 및 계정 연동 시스템

## 📋 구현된 기능

### 1. 로그인 상태 관리
- **LoginType Enum**: None, GPGS, Guest, Linked
- **상태 추적**: 현재 로그인 타입을 PlayerPrefs에 저장하여 앱 재시작 시 복원
- **이벤트 시스템**: 로그인 상태 변경 시 UI 자동 업데이트

### 2. 익명 로그인 기능
- **GPGS 로그인 실패 시 자동 대체**: 익명 로그인으로 진행
- **사용자 선택**: GPGS 재시도 또는 게스트 로그인 선택 가능
- **Firebase Auth 통합**: 익명 계정 생성 및 관리

### 3. 팝업 시스템
- **통합 팝업 컨트롤러**: 모든 게스트 관련 팝업을 관리
- **GPGS 실패 팝업**: 재시도/게스트 로그인 선택
- **게스트 경고 팝업**: 메인 패널 진입 시 1회 표시
- **계정 연동 결과 팝업**: 성공/실패 알림

### 4. 계정 연동 기능
- **게스트 → GPGS 연동**: Firebase LinkWithCredential 사용
- **데이터 마이그레이션**: 로컬 데이터를 서버로 업로드
- **연동 상태 추적**: 마이그레이션 정보를 GameData에 저장

### 5. 데이터 관리
- **게스트 모드**: 로컬 전용 저장 (서버 동기화 없음)
- **정식 계정**: 로컬 + 서버 동기화
- **마이그레이션**: 게스트 데이터를 서버로 업로드 후 저장 방식 변경

## 🔧 사용 방법

### 1. 기본 설정
```csharp
// FirebaseManager 인스턴스가 씬에 있어야 함
// PopupController 인스턴스가 씬에 있어야 함
// AccountLinkingUI 컴포넌트를 환경설정 패널에 추가
```

### 2. 로그인 플로우
```csharp
// GPGS 로그인 시도
FirebaseManager.Instance.GPGSLogin();

// 게스트 로그인 직접 호출
FirebaseManager.Instance.AnonymousLogin();

// 계정 연동
FirebaseManager.Instance.LinkGuestToGPGS();
```

### 3. 로그인 상태 확인
```csharp
// 현재 로그인 타입 확인
LoginType currentType = FirebaseManager.CurrentLoginType;

// 게스트 계정 여부 확인
bool isGuest = FirebaseManager.IsGuestAccount;

// 연동된 계정 여부 확인
bool isLinked = FirebaseManager.IsLinkedAccount;
```

### 4. 데이터 저장
```csharp
// 저장 방식에 따른 자동 저장
DataManager.Instance.SaveData();

// 게스트 전용 로컬 저장
DataManager.Instance.SaveLocalOnly();

// 서버 동기화 저장
DataManager.Instance.SaveLocal();
```

## 🎮 UI 구성 요소

### 1. 환경설정 패널
- **계정 연동 버튼**: 게스트 계정일 때만 활성화
- **계정 상태 표시**: 현재 로그인 타입과 정보 표시
- **계정 정보 패널**: 로그인 상태에 따른 안내 메시지

### 2. 팝업 시스템
- **Alert Dialog**: 확인 버튼만 있는 알림
- **Confirm Dialog**: 확인/취소 버튼이 있는 선택
- **백그라운드 클릭**: Alert만 닫기 가능

## 🔄 플로우 다이어그램

```
시작 → GPGS 로그인 시도
  ↓
GPGS 성공? → YES → 정식 계정 로그인
  ↓ NO
팝업 표시 → 게스트 로그인 OR GPGS 재시도
  ↓ 게스트 선택
익명 로그인 → 게스트 계정 생성
  ↓
메인 패널 진입 → 게스트 경고 팝업 (1회)
  ↓
게임 플레이 → 로컬 전용 저장
  ↓
환경설정 → 계정 연동 버튼 클릭
  ↓
GPGS 로그인 → 계정 연동 → 데이터 마이그레이션
  ↓
연동 완료 → 서버 동기화 저장
```

## ⚠️ 주의사항

1. **에디터 환경**: GPGS 로그인 불가능, 자동으로 게스트 로그인으로 진행
2. **데이터 보안**: 게스트 데이터는 로컬에만 저장되므로 앱 삭제 시 손실
3. **마이그레이션**: 계정 연동 후에는 이전 게스트 데이터가 서버로 이동
4. **네트워크**: 계정 연동 시 네트워크 연결 필요

## 🐛 디버깅

### 로그 확인
```csharp
// 게스트 경고 플래그 초기화 (테스트용)
FirebaseManager.Instance.ResetGuestWarningFlag();

// UI 강제 업데이트 (테스트용)
AccountLinkingUI.Instance.ForceUpdateUI();
```

### 주요 로그 메시지
- `[FirebaseManager] 익명 로그인 시작`
- `[FirebaseManager] 게스트 데이터를 서버로 마이그레이션 시작`
- `[DataManager] 게스트 데이터 마이그레이션 완료`
- `[ConfirmDialog] GPGS 로그인 실패 팝업`

## 📱 플랫폼별 동작

- **Android**: GPGS 로그인 정상 동작
- **iOS**: GPGS 로그인 불가, 자동으로 게스트 로그인
- **Editor**: GPGS 로그인 불가, 자동으로 게스트 로그인

이 시스템을 통해 사용자는 GPGS 로그인이 실패하더라도 게스트 계정으로 게임을 즐길 수 있고, 나중에 계정을 연동하여 데이터를 보존할 수 있습니다.
