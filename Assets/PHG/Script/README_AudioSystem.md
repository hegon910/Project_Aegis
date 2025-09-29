# CSV 기반 음향 재생 시스템

이 시스템은 Resources 폴더의 CSV 파일들에서 BG_ID와 SFX_ID를 읽어서 해당하는 음향을 자동으로 재생하는 기능을 제공합니다.

## 주요 구성 요소

### 1. AudioDataManager.cs
- MainBGData.csv와 MainSFXData.csv를 읽어서 오디오 데이터를 관리
- ID를 통해 BGM과 SFX 오디오 클립을 찾는 기능 제공
- Resources/Audio/BGM/과 Resources/Audio/SFX/ 폴더에서 오디오 파일을 자동 로드

### 2. AudioManager.cs (확장됨)
- 기존 AudioManager에 CSV 기반 음향 재생 메서드 추가
- `PlayBGMByID(int bgId)`: BGM ID로 BGM 재생
- `PlaySFXByID(int sfxId)`: SFX ID로 SFX 재생
- `PlayAudioFromEventData(int bgId, int sfxId)`: 이벤트 데이터에서 음향 재생

### 3. EventAudioPlayer.cs
- 이벤트 데이터와 연동하여 음향을 재생하는 유틸리티 클래스
- 이벤트 ID로부터 자동으로 BG_ID와 SFX_ID를 찾아서 재생

### 4. AudioTestExample.cs
- 시스템 테스트를 위한 예시 스크립트
- 키보드 입력으로 다양한 음향 재생 테스트 가능

## 사용 방법

### 1. 기본 설정
1. AudioDataManager와 EventAudioPlayer를 씬에 배치
2. Resources/Audio/BGM/ 폴더에 BGM 오디오 파일들을 배치
3. Resources/Audio/SFX/ 폴더에 SFX 오디오 파일들을 배치
4. 오디오 파일명은 CSV의 BGName, SFXName과 정확히 일치해야 함

### 2. 코드에서 사용하기

```csharp
// BGM 재생
AudioManager.Instance.PlayBGMByID(100000001); // Story1 재생

// SFX 재생
AudioManager.Instance.PlaySFXByID(1000000001); // Door 사운드 재생

// 이벤트 데이터에서 음향 재생
EventAudioPlayer.Instance.PlayAudioFromEventData(100000001, 1000000001);

// 볼륨 조절과 함께 SFX 재생
AudioManager.Instance.PlaySFXByID(1000000001, 0.5f);
```

### 3. 이벤트 데이터와 연동
MainEventData.csv에서 BG_ID와 SFX_ID를 읽어서 자동으로 음향을 재생하려면:

```csharp
// 이벤트 ID로 음향 재생 (DataManager와 연동 필요)
EventAudioPlayer.Instance.PlayAudioForEvent(eventId);
```

## 파일 구조
```
Resources/
├── MainBGData.csv          # BGM ID와 이름 매핑
├── MainSFXData.csv         # SFX ID와 이름 매핑
├── MainEventData.csv       # 이벤트 데이터 (BG_ID, SFX_ID 포함)
└── Audio/
    ├── BGM/
    │   ├── Story1.wav
    │   ├── Story2.wav
    │   └── ...
    └── SFX/
        ├── Door.wav
        ├── FoodStep.wav
        └── ...
```

## 주의사항
1. 오디오 파일명은 CSV의 이름과 정확히 일치해야 합니다
2. AudioDataManager는 씬 시작 시 자동으로 CSV 데이터를 로드합니다
3. 오디오 파일이 없으면 경고 메시지가 출력되지만 게임은 계속 실행됩니다
4. ID가 0인 경우 음향을 재생하지 않습니다 (None 처리)

## 테스트
AudioTestExample 스크립트를 씬에 추가하고 다음 키를 사용하여 테스트할 수 있습니다:
- B키: BGM 재생 테스트
- S키: SFX 재생 테스트  
- E키: 이벤트 음향 재생 테스트
- 스페이스바: BGM 정지
