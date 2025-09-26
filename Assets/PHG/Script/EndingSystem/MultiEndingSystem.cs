using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 멀티 엔딩 시스템을 관리하는 클래스
/// 루프 시스템의 핵심 로직을 담당하며, 1-6장 사이클과 회차별 엔딩 조건을 구현
/// 데이터 테이블에서 직접 엔딩 데이터를 가져와 사용
/// </summary>
public class MultiEndingSystem : MonoBehaviour
{
    public static MultiEndingSystem Instance { get; private set; }

    [Header("카르마 계산 설정")]
    [SerializeField] private int victoryPoint = 1;
    [SerializeField] private int defeatPoint = -1;
    [SerializeField] private int drawPoint = 0;

    [Header("루프 시스템 설정")]
    [SerializeField] private int maxChapters = 6; // 최대 챕터 수
    [SerializeField] private int endingChapter = 6; // 엔딩이 발생하는 챕터

    [Header("엔딩 조건")]
    [SerializeField] private int trueEndingMinScore2nd = 4;  // 2회차 진엔딩 최소 점수
    [SerializeField] private int trueEndingMinScore3rd = 5;  // 3회차 진엔딩 최소 점수
    [SerializeField] private int trueEndingMinKarma3rd = 80; // 3회차 진엔딩 최소 카르마

    [Header("히든 엔딩 조건")]
    [SerializeField] private int hiddenEndingMinScore = 6; // 히든 엔딩 최소 점수
    [SerializeField] private int hiddenEndingMinKarma = 90; // 히든 엔딩 최소 카르마
    [SerializeField] private int hiddenEndingMinPlaythrough = 3; // 히든 엔딩 최소 회차

    [Header("엔딩 ID 매핑 (데이터 테이블 기반)")]
    [SerializeField] private int generalEndingBaseID = 50001; // 일반 엔딩 시작 ID
    [SerializeField] private int trueEndingBaseID = 50010;    // 진엔딩 시작 ID
    [SerializeField] private int hiddenEndingBaseID = 50020;  // 히든 엔딩 시작 ID

    // 챕터별 전투 결과 저장 (루프별로 관리)
    private Dictionary<int, List<ChapterBattleResult>> playthroughResults = new Dictionary<int, List<ChapterBattleResult>>();
    
    // 현재 루프 상태 추적
    private int currentPlaythrough = 1;
    private int currentChapter = 1;
    private bool isLoopCompleted = false;

    // 루프 완료 이벤트
    public static System.Action<int> OnLoopCompleted; // 회차 번호
    public static System.Action OnNewLoopStarted; // 새 루프 시작

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLoopSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 루프 시스템 초기화
    /// </summary>
    private void InitializeLoopSystem()
    {
        // DataManager에서 현재 상태 로드
        if (DataManager.Instance?.PlayerData != null)
        {
            currentPlaythrough = DataManager.Instance.PlayerData.playthroughCount;
            currentChapter = DataManager.Instance.PlayerData.currentChapter;
        }

        // 현재 회차의 결과 리스트 초기화
        if (!playthroughResults.ContainsKey(currentPlaythrough))
        {
            playthroughResults[currentPlaythrough] = new List<ChapterBattleResult>();
        }

        Debug.Log($"[MultiEndingSystem] 루프 시스템 초기화 - 회차: {currentPlaythrough}, 챕터: {currentChapter}");
    }

    /// <summary>
    /// 챕터 전투 결과를 기록합니다 (루프 시스템 통합)
    /// </summary>
    /// <param name="chapter">챕터 번호 (1-6)</param>
    /// <param name="battleResult">전투 결과 (승리/무승부/패배)</param>
    /// <param name="warSituation">전세 수치</param>
    public void RecordChapterResult(int chapter, GameOutcome battleResult, int warSituation)
    {
        // 챕터 범위 검증
        if (chapter < 1 || chapter > maxChapters)
        {
            Debug.LogError($"[MultiEndingSystem] 잘못된 챕터 번호: {chapter}. 1-{maxChapters} 범위여야 합니다.");
            return;
        }

        var result = new ChapterBattleResult
        {
            chapter = chapter,
            outcome = battleResult,
            warSituation = warSituation,
            karmaPoints = GetKarmaPoints(battleResult),
            playthrough = currentPlaythrough
        };

        // 현재 회차의 결과 리스트 가져오기
        if (!playthroughResults.ContainsKey(currentPlaythrough))
        {
            playthroughResults[currentPlaythrough] = new List<ChapterBattleResult>();
        }

        var currentResults = playthroughResults[currentPlaythrough];

        // 기존 같은 챕터 결과가 있으면 교체
        var existingResult = currentResults.FirstOrDefault(r => r.chapter == chapter);
        if (existingResult != null)
        {
            currentResults.Remove(existingResult);
        }

        currentResults.Add(result);
        currentChapter = chapter;

        Debug.Log($"[MultiEndingSystem] 챕터 {chapter} 결과 기록 (회차 {currentPlaythrough}): {battleResult} (전세: {warSituation}, 카르마: {result.karmaPoints})");

        // 6장 완료 시 루프 완료 체크
        if (chapter == endingChapter)
        {
            CheckLoopCompletion();
        }
    }

    /// <summary>
    /// 루프 완료 여부를 확인하고 처리합니다
    /// </summary>
    private void CheckLoopCompletion()
    {
        if (isLoopCompleted) return;

        var currentResults = playthroughResults[currentPlaythrough];
        
        // 6장까지 모든 챕터가 완료되었는지 확인
        bool allChaptersCompleted = currentResults.Count == maxChapters && 
                                   currentResults.All(r => r.chapter >= 1 && r.chapter <= maxChapters);

        if (allChaptersCompleted)
        {
            CompleteCurrentLoop();
        }
    }

    /// <summary>
    /// 현재 루프를 완료하고 다음 루프를 준비합니다
    /// </summary>
    private void CompleteCurrentLoop()
    {
        isLoopCompleted = true;
        
        Debug.Log($"[MultiEndingSystem] 루프 완료! 회차 {currentPlaythrough} 완료");
        
        // 루프 완료 이벤트 발생
        OnLoopCompleted?.Invoke(currentPlaythrough);
        
        // DataManager에 회차 증가 및 챕터 리셋
        if (DataManager.Instance?.PlayerData != null)
        {
            DataManager.Instance.PlayerData.playthroughCount++;
            DataManager.Instance.PlayerData.currentChapter = 1;
        }

        // 다음 루프 준비
        PrepareNextLoop();
    }

    /// <summary>
    /// 다음 루프를 준비합니다
    /// </summary>
    private void PrepareNextLoop()
    {
        currentPlaythrough++;
        currentChapter = 1;
        isLoopCompleted = false;

        // 새 회차의 결과 리스트 초기화
        playthroughResults[currentPlaythrough] = new List<ChapterBattleResult>();

        Debug.Log($"[MultiEndingSystem] 새 루프 준비 완료 - 회차: {currentPlaythrough}");
        
        // 새 루프 시작 이벤트 발생
        OnNewLoopStarted?.Invoke();
    }

    /// <summary>
    /// 현재 회차의 총 카르마 점수 계산
    /// </summary>
    public int CalculateCurrentPlaythroughKarma()
    {
        if (!playthroughResults.ContainsKey(currentPlaythrough))
            return 0;

        return playthroughResults[currentPlaythrough].Sum(r => r.karmaPoints);
    }

    /// <summary>
    /// 특정 회차의 총 카르마 점수 계산
    /// </summary>
    public int CalculatePlaythroughKarma(int playthrough)
    {
        if (!playthroughResults.ContainsKey(playthrough))
            return 0;

        return playthroughResults[playthrough].Sum(r => r.karmaPoints);
    }

    /// <summary>
    /// 현재 카르마 수치 계산 (실제 게임 카르마 값 사용)
    /// </summary>
    public int CalculateCurrentKarma()
    {
        if (GamePlayerStats.Instance != null)
        {
            return GamePlayerStats.Instance.GetStat(ParameterType.카르마);
        }
        return 50; // GamePlayerStats가 없을 경우 기본값
    }

    /// <summary>
    /// 전투 결과에 따른 카르마 포인트 계산
    /// </summary>
    public int GetKarmaPoints(GameOutcome outcome)
    {
        return outcome switch
        {
            GameOutcome.Victory => victoryPoint,
            GameOutcome.Defeat => defeatPoint,
            GameOutcome.Draw => drawPoint,
            _ => 0
        };
    }

    /// <summary>
    /// 현재 전세 수치에 따른 전투 결과 결정
    /// </summary>
    public GameOutcome DetermineBattleOutcome(int warSituation)
    {
        if (warSituation <= 19) return GameOutcome.Defeat;
        if (warSituation >= 81) return GameOutcome.Victory;
        return GameOutcome.Draw;
    }

    /// <summary>
    /// 엔딩 타입 결정 (루프 시스템 통합)
    /// </summary>
    public EndingType DetermineEndingType()
    {
        var totalScore = CalculateCurrentPlaythroughKarma();
        var currentKarma = CalculateCurrentKarma();

        Debug.Log($"[MultiEndingSystem] 엔딩 타입 결정 - 회차: {currentPlaythrough}, 총점수: {totalScore}, 카르마: {currentKarma}");

        // 1회차: 일반 엔딩만
        if (currentPlaythrough == 1)
        {
            return EndingType.General;
        }

        // 2회차 이상: 진엔딩 조건 확인
        if (currentPlaythrough == 2)
        {
            if (totalScore >= trueEndingMinScore2nd)
            {
                return EndingType.True;
            }
        }
        else if (currentPlaythrough >= 3)
        {
            // 3회차 이상: 진엔딩 또는 히든 엔딩
            if (totalScore >= trueEndingMinScore3rd && currentKarma >= trueEndingMinKarma3rd)
            {
                // 히든 엔딩 특별 조건 확인
                if (HasHiddenEndingFlag())
                {
                    return EndingType.Hidden;
                }
                return EndingType.True;
            }
        }

        return EndingType.General;
    }

    /// <summary>
    /// 히든 엔딩 플래그 확인 (강화된 조건)
    /// </summary>
    private bool HasHiddenEndingFlag()
    {
        var totalScore = CalculateCurrentPlaythroughKarma();
        var currentKarma = CalculateCurrentKarma();

        // 기본 히든 엔딩 조건
        bool basicCondition = totalScore >= hiddenEndingMinScore && 
                             currentKarma >= hiddenEndingMinKarma && 
                             currentPlaythrough >= hiddenEndingMinPlaythrough;

        if (!basicCondition) return false;

        // 추가 조건들 (예: 특정 이벤트에서 올바른 선택을 3회 이상 등)
        bool specialCondition = CheckSpecialHiddenEndingConditions();

        return basicCondition && specialCondition;
    }

    /// <summary>
    /// 히든 엔딩을 위한 특별 조건들을 확인합니다
    /// </summary>
    private bool CheckSpecialHiddenEndingConditions()
    {
        // 예: 특정 이벤트에서 올바른 선택을 3회 이상 했는지 확인
        // PlaythroughHistory를 통해 확인 가능
        if (PlaythroughHistory.Instance != null)
        {
            // 예시: 특정 이벤트 ID들에서 성공한 횟수 확인
            var specialEventIds = new int[] { 1001, 1002, 1003 }; // 예시 ID들
            int successCount = 0;

            foreach (var eventId in specialEventIds)
            {
                // HCW의 PlaythroughHistory는 성공/실패 구분이 없으므로 단순히 완료 여부만 확인
                if (PlaythroughHistory.Instance.HasCompletedEvent(eventId))
                {
                    successCount++;
                }
            }

            return successCount >= 3; // 3회 이상 성공해야 함
        }

        return true; // PlaythroughHistory가 없으면 기본적으로 true
    }

    /// <summary>
    /// 엔딩 루트 결정 (승리/무승부/패배) - 스프레드시트 기준
    /// </summary>
    public EndingRoute DetermineEndingRoute()
    {
        var totalScore = CalculateCurrentPlaythroughKarma();
        
        // 스프레드시트 기준: 승리(1001), 무승부(1002), 패배는 명시되지 않음
        // 전세 수치에 따른 루트 결정
        var currentKarma = CalculateCurrentKarma();
        
        // 전세 수치가 81 이상이면 승리, 20-80이면 무승부, 19 이하면 패배로 간주
        if (currentKarma >= 81) return EndingRoute.Victory;
        if (currentKarma >= 20) return EndingRoute.Truce;
        return EndingRoute.Defeat;
    }

    /// <summary>
    /// 엔딩 분기 결정 (카르마 범위에 따라) - 스프레드시트 기준
    /// </summary>
    public int DetermineEndingBranch(EndingRoute route)
    {
        var currentKarma = CalculateCurrentKarma();
        
        return route switch
        {
            EndingRoute.Victory => GetVictoryBranch(currentKarma),
            EndingRoute.Defeat => GetDefeatBranch(currentKarma),
            EndingRoute.Truce => GetTruceBranch(currentKarma),
            _ => 2001 // 기본값
        };
    }

    private int GetVictoryBranch(int karma)
    {
        // 승리 루트 분기: 2001(81이상), 2002(20-80), 2003(19이하)
        if (karma >= 81) return 2001;
        if (karma >= 20) return 2002;
        return 2003;
    }

    private int GetDefeatBranch(int karma)
    {
        // 패배 루트 분기 (스프레드시트에 명시되지 않음)
        if (karma >= 0 && karma <= 19) return 2001;
        if (karma >= 20 && karma <= 80) return 2002;
        return 2003; // 기본값
    }

    private int GetTruceBranch(int karma)
    {
        // 무승부 루트 분기: 2001(50이상), 2002(0-49)
        if (karma >= 50) return 2001;
        return 2002;
    }

    /// <summary>
    /// 데이터 테이블에서 직접 엔딩 시퀀스의 첫 번째 ID를 결정합니다
    /// </summary>
    public int GetEndingEventID()
    {
        var endingType = DetermineEndingType();
        var endingRoute = DetermineEndingRoute();
        var currentKarma = CalculateCurrentKarma();
        
        // 스프레드시트에 따른 엔딩 시퀀스 매핑 (실제 스프레드시트 데이터 기반)
        var endingSequenceMap = new Dictionary<(EndingRoute, int), int>
        {
            // 승리 루트 (1001) - 스프레드시트 데이터 기반
            { (EndingRoute.Victory, 2001), 50001 }, // 높은 카르마 (81이상)
            { (EndingRoute.Victory, 2002), 50007 }, // 중간 카르마 (20-80)
            { (EndingRoute.Victory, 2003), 50013 }, // 낮은 카르마 (19이하)
            
            // 무승부 루트 (1002) - 스프레드시트 데이터 기반
            { (EndingRoute.Truce, 2001), 50078 }, // 높은 카르마 (50이상)
            { (EndingRoute.Truce, 2002), 50085 }, // 낮은 카르마 (0-49)
            
            // 패배 루트는 스프레드시트에서 확인되지 않음 - 기본값 사용
            { (EndingRoute.Defeat, 2001), 50001 }, // 기본값
            { (EndingRoute.Defeat, 2002), 50001 }, // 기본값
            { (EndingRoute.Defeat, 2003), 50001 }, // 기본값
        };
        
        // 카르마에 따른 분류 (스프레드시트 기준)
        int karmaCategory = endingRoute switch
        {
            EndingRoute.Victory => GetKarmaCategory(currentKarma),
            EndingRoute.Truce => GetTruceKarmaCategory(currentKarma),
            EndingRoute.Defeat => GetKarmaCategory(currentKarma), // 패배도 승리와 동일한 카테고리 사용
            _ => 2001
        };
        
        // 시퀀스의 첫 번째 ID 결정
        var key = (endingRoute, karmaCategory);
        int firstEndingID = endingSequenceMap.ContainsKey(key) ? endingSequenceMap[key] : 50001;
        
        Debug.Log($"[MultiEndingSystem] 엔딩 시퀀스 첫 번째 ID 결정: {firstEndingID} (타입: {endingType}, 루트: {endingRoute}, 카르마: {currentKarma}, 카테고리: {karmaCategory})");
        
        return firstEndingID;
    }
    
    /// <summary>
    /// 카르마 수치에 따른 카테고리 반환 (스프레드시트 기준)
    /// </summary>
    private int GetKarmaCategory(int karma)
    {
        // 스프레드시트 기준: 81이상=2001, 20-80=2002, 19이하=2003
        if (karma >= 81) return 2001; // 높은 카르마 (승리 루트)
        if (karma >= 20) return 2002; // 중간 카르마 (승리 루트)
        return 2003; // 낮은 카르마 (승리 루트)
    }
    
    /// <summary>
    /// 무승부 루트의 카르마 수치에 따른 카테고리 반환 (스프레드시트 기준)
    /// </summary>
    private int GetTruceKarmaCategory(int karma)
    {
        // 무승부 루트 기준: 50이상=2001, 0-49=2002
        if (karma >= 50) return 2001; // 높은 카르마 (무승부 루트)
        return 2002; // 낮은 카르마 (무승부 루트)
    }

    /// <summary>
    /// 엔딩 타입에 따른 기본 ID 반환
    /// </summary>
    private int GetBaseEndingID(EndingType endingType)
    {
        return endingType switch
        {
            EndingType.General => generalEndingBaseID,
            EndingType.True => trueEndingBaseID,
            EndingType.Hidden => hiddenEndingBaseID,
            _ => generalEndingBaseID
        };
    }

    /// <summary>
    /// 엔딩 루트에 따른 오프셋 반환
    /// </summary>
    private int GetRouteOffset(EndingRoute endingRoute)
    {
        return endingRoute switch
        {
            EndingRoute.Victory => 0,   // 승리 루트는 기본 ID
            EndingRoute.Truce => 10,    // 무승부 루트는 +10
            EndingRoute.Defeat => 20,   // 패배 루트는 +20
            _ => 0
        };
    }

    /// <summary>
    /// 카르마 범위에 따른 분기 오프셋 반환 (스프레드시트 기준)
    /// </summary>
    private int GetBranchOffset(EndingRoute endingRoute, int karma)
    {
        if (endingRoute == EndingRoute.Victory)
        {
            if (karma >= 81) return 2001;      // 높은 카르마
            else if (karma >= 20) return 2002;  // 중간 카르마
            else return 2003;                   // 낮은 카르마
        }
        else if (endingRoute == EndingRoute.Truce)
        {
            if (karma >= 50) return 2001;      // 높은 카르마
            else return 2002;                   // 낮은 카르마
        }
        else // Defeat
        {
            if (karma >= 0 && karma <= 19) return 2001;        // 매우 낮은 카르마
            else if (karma >= 20 && karma <= 80) return 2002;  // 중간 카르마
            else return 2003;                                   // 높은 카르마 (패배 루트에서)
        }
    }

    /// <summary>
    /// DataManager의 FullEndingData와 연동하여 엔딩 데이터 가져오기
    /// </summary>
    public FullEndingData GetFullEndingData()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[MultiEndingSystem] DataManager가 null입니다.");
            return null;
        }

        if (DataManager.Instance.FullendingDataDict == null)
        {
            Debug.LogError("[MultiEndingSystem] FullendingDataDict가 초기화되지 않았습니다.");
            return null;
        }

        int endingID = GetEndingEventID();
        Debug.Log($"[MultiEndingSystem] 결정된 엔딩 ID: {endingID}");
        
        // FullendingDataDict에서 직접 가져오기
        if (DataManager.Instance.FullendingDataDict.TryGetValue(endingID, out var fullEndingData))
        {
            Debug.Log($"[MultiEndingSystem] 엔딩 데이터 로드 성공:");
            Debug.Log($"  - ID: {fullEndingData.ID}");
            Debug.Log($"  - Text_Kr: {fullEndingData.Text_Kr}");
            Debug.Log($"  - BG_ID: {fullEndingData.bgData?.BG_ID ?? -1}");
            Debug.Log($"  - CutScene_ID: {fullEndingData.cutSceneData?.EndingCutScene_ID ?? -1}");
            Debug.Log($"  - SFX_ID: {fullEndingData.sfxData?.SFX_ID ?? -1}");
        }
        else
        {
            Debug.LogError($"[MultiEndingSystem] 엔딩 ID {endingID}에 대한 데이터를 FullendingDataDict에서 찾을 수 없습니다.");
            // 폴백: 기존 GetEndingData 메서드 사용
            fullEndingData = DataManager.Instance.GetEndingData(endingID);
            if (fullEndingData != null)
            {
                Debug.Log("[MultiEndingSystem] 폴백 메서드로 엔딩 데이터를 찾았습니다.");
            }
        }
        
        return fullEndingData;
    }

    // ===== 호환성을 위한 메서드들 =====
    
    /// <summary>
    /// 호환성을 위한 GetFinalEndingData 메서드 (기존 코드와의 호환성 유지)
    /// </summary>
    public EndingData GetFinalEndingData()
    {
        var endingType = DetermineEndingType();
        var endingRoute = DetermineEndingRoute();
        var branch = DetermineEndingBranch(endingRoute);
        var fullEndingData = GetFullEndingData();

        return new EndingData
        {
            endingType = endingType,
            route = endingRoute,
            branch = branch,
            fullEndingData = fullEndingData,
            title = $"엔딩 - {endingType}",
            description = $"회차 {currentPlaythrough}의 {endingRoute} 루트 {branch}분기 엔딩"
        };
    }

    /// <summary>
    /// 데이터 테이블 기반으로 CutsceneData를 동적 생성합니다
    /// </summary>
    public CutsceneData CreateCutsceneDataFromTable()
    {
        var fullEndingData = GetFullEndingData();
        if (fullEndingData == null)
        {
            Debug.LogError("[MultiEndingSystem] FullEndingData를 가져올 수 없습니다.");
            return null;
        }

        return ConvertToCutsceneData(fullEndingData);
    }

    /// <summary>
    /// 연속된 엔딩 시퀀스를 CutsceneData로 변환 (데이터 테이블 기반)
    /// </summary>
    public CutsceneData ConvertToCutsceneData(FullEndingData fullEndingData)
    {
        if (fullEndingData == null) 
        {
            Debug.LogError("[MultiEndingSystem] FullEndingData가 null입니다.");
            return null;
        }
        
        Debug.Log($"[MultiEndingSystem] CutsceneData 변환 시작:");
        Debug.Log($"  - 시작 ID: {fullEndingData.ID}");
        
        var cutsceneData = ScriptableObject.CreateInstance<CutsceneData>();
        cutsceneData.steps = new List<CutsceneStep>();
        
        // 연속된 엔딩 시퀀스 로드
        var endingSequence = LoadEndingSequence(fullEndingData.ID);
        
        // 1. 첫 번째 배경 이미지 스텝 (시퀀스의 첫 번째 엔딩에서만)
        if (endingSequence.Count > 0 && endingSequence[0].bgData != null && !string.IsNullOrEmpty(endingSequence[0].bgData.BGName))
        {
            var bgStep = new CutsceneStep();
            bgStep.stepName = "엔딩 배경 시작";
            bgStep.enableImageEffect = true;
            bgStep.imageData = new ImageEffectData
            {
                fadeDuration = 2.0f // 처음 배경은 천천히 페이드인
            };
            cutsceneData.steps.Add(bgStep);
        }
        
        // 2. 텍스트 스텝들 (각 엔딩의 텍스트만)
        for (int i = 0; i < endingSequence.Count; i++)
        {
            var endingData = endingSequence[i];
            
            if (!string.IsNullOrEmpty(endingData.Text_Kr))
            {
                var textStep = new CutsceneStep();
                textStep.stepName = "엔딩 텍스트";
                textStep.enableDialogueEffect = true;
                textStep.dialogueData = new DialogueEffectData
                {
                    dialogue = endingData.Text_Kr,
                    typewriterSpeed = 0.05f
                };
                cutsceneData.steps.Add(textStep);
                Debug.Log($"[MultiEndingSystem] 텍스트 스텝 추가 (ID: {endingData.ID}): '{endingData.Text_Kr}'");
            }
            
            // 사운드 이펙트 (있다면)
            if (endingData.sfxData != null && !string.IsNullOrEmpty(endingData.sfxData.SFXName))
            {
                var sfxStep = new CutsceneStep();
                sfxStep.stepName = "엔딩 사운드";
                sfxStep.enableSoundEffect = true;
                sfxStep.soundData = new SoundEffectData
                {
                    // TODO: SFXName을 실제 AudioClip으로 로드하는 로직 필요
                };
                cutsceneData.steps.Add(sfxStep);
            }
        }
        
        // 3. 마지막 컷신 이미지 스텝 (시퀀스의 마지막 엔딩에서만)
        if (endingSequence.Count > 0)
        {
            var lastEndingData = endingSequence[endingSequence.Count - 1];
            if (lastEndingData.cutSceneData != null && !string.IsNullOrEmpty(lastEndingData.cutSceneData.IMGName))
            {
                var cutsceneStep = new CutsceneStep();
                cutsceneStep.stepName = "엔딩 컷신 마지막";
                cutsceneStep.enableImageEffect = true;
                cutsceneStep.imageData = new ImageEffectData
                {
                    fadeDuration = 1.5f // 마지막 컷신은 적당히 페이드인
                };
                cutsceneData.steps.Add(cutsceneStep);
            }
        }
        
        Debug.Log($"[MultiEndingSystem] 최종 CutsceneData 스텝 수: {cutsceneData.steps.Count}");
        return cutsceneData;
    }
    
    /// <summary>
    /// 연속된 엔딩 시퀀스를 로드합니다 (스프레드시트 데이터 기반)
    /// </summary>
    private List<FullEndingData> LoadEndingSequence(int startingID)
    {
        var sequence = new List<FullEndingData>();
        
        if (DataManager.Instance?.FullendingDataDict == null)
        {
            Debug.LogError("[MultiEndingSystem] DataManager의 FullendingDataDict가 초기화되지 않았습니다.");
            return sequence;
        }
        
        // 스프레드시트에 따른 엔딩 시퀀스 매핑 (실제 데이터 기반)
        var endingSequences = new Dictionary<int, int[]>
        {
            // 1001 엔딩 시퀀스들 (승리 루트) - 스프레드시트 데이터 기반
            { 50001, new int[] { 50001, 50002, 50003, 50004, 50005, 50006 } }, // 2001 카르마 (81이상)
            { 50007, new int[] { 50007, 50008, 50009, 50010, 50011, 50012 } }, // 2002 카르마 (20-80)
            { 50013, new int[] { 50013, 50014, 50015, 50016, 50017, 50018 } }, // 2003 카르마 (19이하)
            
            // 1002 엔딩 시퀀스들 (무승부 루트) - 스프레드시트 데이터 기반
            { 50078, new int[] { 50078, 50079, 50080, 50081, 50082, 50083, 50084 } }, // 2001 카르마 (50-100)
            { 50085, new int[] { 50085, 50086 } }, // 2002 카르마 (0-49)
        };
        
        // 시작 ID에 해당하는 시퀀스 찾기
        int[] sequenceIDs = null;
        foreach (var kvp in endingSequences)
        {
            if (kvp.Value.Contains(startingID))
            {
                sequenceIDs = kvp.Value;
                break;
            }
        }
        
        if (sequenceIDs == null)
        {
            Debug.LogWarning($"[MultiEndingSystem] 시작 ID {startingID}에 해당하는 시퀀스를 찾을 수 없습니다. 단일 엔딩으로 처리합니다.");
            sequenceIDs = new int[] { startingID };
        }
        
        // 시퀀스의 각 ID에 대해 데이터 로드 (FullendingDataDict 사용)
        foreach (int id in sequenceIDs)
        {
            if (DataManager.Instance.FullendingDataDict.TryGetValue(id, out var endingData))
            {
                sequence.Add(endingData);
                Debug.Log($"[MultiEndingSystem] 시퀀스 ID {id} 로드: '{endingData.Text_Kr}'");
            }
            else
            {
                Debug.LogWarning($"[MultiEndingSystem] ID {id}에 대한 엔딩 데이터를 FullendingDataDict에서 찾을 수 없습니다.");
            }
        }
        
        return sequence;
    }

    /// <summary>
    /// 루프 시스템 리셋 (새 게임 시작 시)
    /// </summary>
    public void ResetLoopSystem()
    {
        playthroughResults.Clear();
        currentPlaythrough = 1;
        currentChapter = 1;
        isLoopCompleted = false;

        // 새 회차의 결과 리스트 초기화
        playthroughResults[currentPlaythrough] = new List<ChapterBattleResult>();

        Debug.Log("[MultiEndingSystem] 루프 시스템 리셋 완료");
    }

    /// <summary>
    /// 현재 루프 상태 정보 반환
    /// </summary>
    public LoopStatus GetCurrentLoopStatus()
{
    int completedChapters = 0;
    if (playthroughResults.ContainsKey(currentPlaythrough))
    {
        var results = playthroughResults[currentPlaythrough];
        // 연속된 챕터의 완료 수 계산
        for (int i = 1; i <= maxChapters; i++)
        {
            if (results.Any(r => r.chapter == i))
            {
                completedChapters = i;
            }
            else
            {
                break;
            }
        }
    }

    return new LoopStatus
    {
        currentPlaythrough = currentPlaythrough,
        currentChapter = currentChapter,
        isLoopCompleted = isLoopCompleted,
        totalKarma = CalculateCurrentPlaythroughKarma(),
        currentKarma = CalculateCurrentKarma(),
        completedChapters = completedChapters
    };
}

    /// <summary>
    /// 특정 회차의 상세 정보 반환
    /// </summary>
    public PlaythroughInfo GetPlaythroughInfo(int playthrough)
    {
        if (!playthroughResults.ContainsKey(playthrough))
        {
            return new PlaythroughInfo { playthrough = playthrough, isCompleted = false };
        }

        var results = playthroughResults[playthrough];
        return new PlaythroughInfo
        {
            playthrough = playthrough,
            isCompleted = results.Count == maxChapters,
            totalKarma = results.Sum(r => r.karmaPoints),
            chapterResults = results.ToList(),
            endingType = playthrough == currentPlaythrough ? DetermineEndingType() : EndingType.General
        };
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    [ContextMenu("Print Debug Info")]
    public void PrintDebugInfo()
    {
        Debug.Log("=== MultiEndingSystem 디버그 정보 ===");
        Debug.Log($"현재 회차: {currentPlaythrough}");
        Debug.Log($"현재 챕터: {currentChapter}");
        Debug.Log($"루프 완료 여부: {isLoopCompleted}");
        Debug.Log($"현재 회차 총 카르마: {CalculateCurrentPlaythroughKarma()}");
        Debug.Log($"현재 카르마: {CalculateCurrentKarma()}");
        Debug.Log($"엔딩 타입: {DetermineEndingType()}");
        Debug.Log($"엔딩 루트: {DetermineEndingRoute()}");
        Debug.Log($"엔딩 분기: {DetermineEndingBranch(DetermineEndingRoute())}");
        Debug.Log($"결정된 엔딩 ID: {GetEndingEventID()}");
        
        Debug.Log("현재 회차 챕터별 결과:");
        if (playthroughResults.ContainsKey(currentPlaythrough))
        {
            foreach (var result in playthroughResults[currentPlaythrough].OrderBy(r => r.chapter))
            {
                Debug.Log($"  챕터 {result.chapter}: {result.outcome} (전세: {result.warSituation}, 카르마: {result.karmaPoints})");
            }
        }
    }

    /// <summary>
    /// 현재 챕터를 강제로 설정합니다 (테스트용)
    /// </summary>
    public void SetCurrentChapter(int chapter)
    {
        if (chapter < 1 || chapter > maxChapters)
        {
            Debug.LogError($"[MultiEndingSystem] 잘못된 챕터 번호: {chapter}. 1-{maxChapters} 범위여야 합니다.");
            return;
        }

        currentChapter = chapter;
        
        // DataManager와 동기화
        if (DataManager.Instance?.PlayerData != null)
        {
            DataManager.Instance.PlayerData.currentChapter = chapter;
        }

        Debug.Log($"[MultiEndingSystem] 현재 챕터를 {chapter}로 설정했습니다.");
    }

    public void SetCurrentPlaythrough(int playthrough)
{
    if (playthrough < 1)
    {
        Debug.LogError($"[MultiEndingSystem] 잘못된 회차 번호: {playthrough}. 1 이상이어야 합니다.");
        return;
    }

    currentPlaythrough = playthrough;
    
    // DataManager와 동기화
    if (DataManager.Instance?.PlayerData != null)
    {
        DataManager.Instance.PlayerData.playthroughCount = playthrough;
    }

    // 현재 회차의 결과 리스트 초기화 (없으면)
    if (!playthroughResults.ContainsKey(currentPlaythrough))
    {
        playthroughResults[currentPlaythrough] = new List<ChapterBattleResult>();
    }

    Debug.Log($"[MultiEndingSystem] 현재 회차를 {playthrough}로 설정했습니다.");
}

    // 테스트용 메서드들
    [ContextMenu("Test Chapter 1 Victory")]
    public void TestChapter1Victory()
    {
        RecordChapterResult(1, GameOutcome.Victory, 85);
        PrintDebugInfo();
    }

    [ContextMenu("Test Chapter 2 Victory")]
    public void TestChapter2Victory()
    {
        RecordChapterResult(2, GameOutcome.Victory, 90);
        PrintDebugInfo();
    }

    [ContextMenu("Test Complete Loop")]
    public void TestCompleteLoop()
    {
        for (int i = 1; i <= 6; i++)
        {
            RecordChapterResult(i, GameOutcome.Victory, 80 + i);
        }
        PrintDebugInfo();
    }

    [ContextMenu("Test Create Cutscene Data")]
    public void TestCreateCutsceneData()
    {
        var cutsceneData = CreateCutsceneDataFromTable();
        if (cutsceneData != null)
        {
            Debug.Log($"[테스트] CutsceneData 생성 성공! 스텝 수: {cutsceneData.steps.Count}");
        }
        else
        {
            Debug.LogError("[테스트] CutsceneData 생성 실패!");
        }
    }
}

/// <summary>
/// 챕터별 전투 결과 데이터 (루프 시스템 통합)
/// </summary>
[System.Serializable]
public class ChapterBattleResult
{
    public int chapter;
    public GameOutcome outcome;
    public int warSituation;
    public int karmaPoints;
    public int playthrough; // 회차 정보 추가
}

/// <summary>
/// 현재 루프 상태 정보
/// </summary>
[System.Serializable]
public class LoopStatus
{
    public int currentPlaythrough;
    public int currentChapter;
    public bool isLoopCompleted;
    public int totalKarma;
    public int currentKarma;
    public int completedChapters;
}

/// <summary>
/// 특정 회차의 상세 정보
/// </summary>
[System.Serializable]
public class PlaythroughInfo
{
    public int playthrough;
    public bool isCompleted;
    public int totalKarma;
    public List<ChapterBattleResult> chapterResults;
    public EndingType endingType;
}

/// <summary>
/// 엔딩 타입 열거형
/// </summary>
public enum EndingType
{
    General,    // 일반 엔딩
    True,       // 진엔딩
    Hidden      // 히든 엔딩
}

/// <summary>
/// 엔딩 루트 열거형
/// </summary>
public enum EndingRoute
{
    Victory,    // 승리 루트
    Truce,      // 무승부 루트
    Defeat      // 패배 루트
}

/// <summary>
/// 엔딩 데이터 클래스 (호환성 유지)
/// </summary>
[System.Serializable]
public class EndingData
{
    [Header("엔딩 기본 정보")]
    public EndingType endingType;
    public EndingRoute route;
    public int branch;
    
    [Header("엔딩 컨텐츠")]
    public CutsceneData cutsceneData; // 기존 호환성을 위해 유지
    public FullEndingData fullEndingData; // 새로운 필드 추가
    public string title;
    [TextArea(3, 5)]
    public string description;
    
    [Header("해금 조건")]
    public int requiredPlaythrough;
    public int requiredMinScore;
    public int requiredMinKarma;
    public bool requiresSpecialFlag;
}
