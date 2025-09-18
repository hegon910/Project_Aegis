# OptionController 사용 가이드

## 개요
OptionController는 게임의 옵션창을 관리하는 스크립트입니다. GameState에 따라 버튼을 자동으로 활성화/비활성화하고, 팁 메시지를 자동으로 표시하며, 사운드 볼륨을 조절할 수 있습니다.

## 주요 기능

### 1. GameState 기반 버튼 관리
- **MainMenu 상태**: MainMenu 버튼을 비활성화
- **InEventCycle 상태**: ResetData 버튼을 비활성화

### 2. 자동 팁 메시지 표시
- 20개의 한국어 팁 메시지를 순환하며 표시
- 페이드 인/아웃 효과와 함께 자동 재생
- 표시 간격과 페이드 시간 조절 가능

### 3. 사운드 볼륨 조절
- 배경음(BGM) 슬라이더
- 효과음(SFX) 슬라이더
- 실시간 볼륨 조절 및 GameData에 자동 저장

## 사용 방법

### 1. 컴포넌트 설정
OptionController를 OptionCanvas 또는 OptionPanel에 붙이고 다음 UI 요소들을 인스펙터에서 할당하세요:

#### 필수 UI 요소
- `mainMenuButton`: 메인 메뉴로 돌아가는 버튼
- `resetDataButton`: 데이터를 리셋하는 버튼
- `bgmVolumeSlider`: 배경음 볼륨 조절 슬라이더
- `sfxVolumeSlider`: 효과음 볼륨 조절 슬라이더

#### 선택적 UI 요소
- `tipText`: 팁 메시지를 표시할 TextMeshProUGUI
- `bgmVolumeText`: 배경음 볼륨 퍼센트를 표시할 TextMeshProUGUI
- `sfxVolumeText`: 효과음 볼륨 퍼센트를 표시할 TextMeshProUGUI

### 2. 팁 메시지 설정
인스펙터에서 `tipMessages` 리스트에 원하는 메시지를 추가하거나 수정할 수 있습니다. 기본적으로 20개의 한국어 팁이 포함되어 있습니다.

### 3. 팁 표시 설정
- `tipDisplayInterval`: 팁 표시 간격 (기본값: 3초)
- `tipFadeInDuration`: 페이드 인 시간 (기본값: 0.5초)
- `tipFadeOutDuration`: 페이드 아웃 시간 (기본값: 0.5초)

## 코드 예시

### 팁 표시 활성화/비활성화
```csharp
OptionController optionController = GetComponent<OptionController>();
optionController.SetTipDisplayEnabled(false); // 팁 표시 비활성화
```

### 버튼 상태 새로고침
```csharp
optionController.RefreshButtonStates();
```

## 의존성

### 필수 컴포넌트
- `GameManager`: GameState 변경 이벤트 구독
- `DataManager`: 설정 데이터 저장/로드
- `AudioManager`: 실제 오디오 볼륨 조절

### 필요한 스크립트
- `GameData.cs`: 게임 데이터 구조 (settings 포함)
- `GameSettings.cs`: 오디오 설정 구조
- `AudioManager.cs`: 오디오 관리

## 주의사항

1. **AudioManager 설정**: AudioManager에 BGM과 SFX용 AudioSource를 할당해야 합니다.
2. **DataManager 초기화**: DataManager가 먼저 초기화되어야 합니다.
3. **UI 할당**: 모든 필수 UI 요소를 인스펙터에서 할당해야 합니다.
4. **GameState 이벤트**: GameManager의 OnGameStateChanged 이벤트를 통해 상태 변경을 감지합니다.

## 확장 가능성

- 추가적인 옵션 설정 (해상도, 언어 등)을 쉽게 추가할 수 있습니다.
- 팁 메시지 시스템을 다른 UI 요소에도 활용할 수 있습니다.
- 사운드 외의 다른 설정도 동일한 패턴으로 구현할 수 있습니다.
