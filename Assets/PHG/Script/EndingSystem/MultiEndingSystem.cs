using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 멀티 엔딩 시스템을 관리하는 클래스
/// 이미지에서 설명된 카르마 시스템과 회차별 엔딩 조건을 구현
/// </summary>
public class MultiEndingSystem : MonoBehaviour
{
    public static MultiEndingSystem Instance { get; private set; }

    [Header("엔딩 데이터")]
    [SerializeField] private List<EndingData> generalEndings = new List<EndingData>();
    [SerializeField] private List<EndingData> trueEndings = new List<EndingData>();
    [SerializeField] private List<EndingData> hiddenEndings = new List<EndingData>();

    [Header("카르마 계산 설정")]
    [SerializeField] private int victoryPoint = 1;
    [SerializeField] private int defeatPoint = -1;
    [SerializeField] private int drawPoint = 0;

    [Header("엔딩 조건")]
    [SerializeField] private int trueEndingMinScore2nd = 4;  // 2회차 진엔딩 최소 점수
    [SerializeField] private int trueEndingMinScore3rd = 5;  // 3회차 진엔딩 최소 점수
    [SerializeField] private int trueEndingMinKarma3rd = 80; // 3회차 진엔딩 최소 카르마

    // 챕터별 전투 결과 저장
    private List<ChapterBattleResult> chapterResults = new List<ChapterBattleResult>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 챕터 전투 결과를 기록합니다
    /// </summary>
    /// <param name="chapter">챕터 번호</param>
    /// <param name="battleResult">전투 결과 (승리/무승부/패배)</param>
    /// <param name="warSituation">전세 수치</param>
    public void RecordChapterResult(int chapter, GameOutcome battleResult, int warSituation)
    {
        var result = new ChapterBattleResult
        {
            chapter = chapter,
            outcome = battleResult,
            warSituation = warSituation,
            karmaPoints = GetKarmaPoints(battleResult)
        };

        // 기존 같은 챕터 결과가 있으면 교체
        var existingResult = chapterResults.FirstOrDefault(r => r.chapter == chapter);
        if (existingResult != null)
        {
            chapterResults.Remove(existingResult);
        }

        chapterResults.Add(result);
        
        Debug.Log($"[MultiEndingSystem] 챕터 {chapter} 결과 기록: {battleResult} (전세: {warSituation}, 카르마: {result.karmaPoints})");
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
    /// 현재까지의 총 카르마 점수 계산
    /// </summary>
    public int CalculateTotalKarma()
    {
        return chapterResults.Sum(r => r.karmaPoints);
    }

    /// <summary>
    /// 현재 카르마 수치 계산 (기본 50에서 시작)
    /// </summary>
    public int CalculateCurrentKarma()
    {
        return 50 + CalculateTotalKarma();
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
    /// 엔딩 타입 결정
    /// </summary>
    public EndingType DetermineEndingType()
    {
        var playthroughCount = DataManager.Instance.PlayerData.playthroughCount;
        var totalScore = CalculateTotalKarma();
        var currentKarma = CalculateCurrentKarma();

        Debug.Log($"[MultiEndingSystem] 엔딩 타입 결정 - 회차: {playthroughCount}, 총점수: {totalScore}, 카르마: {currentKarma}");

        // 1회차: 일반 엔딩만
        if (playthroughCount == 1)
        {
            return EndingType.General;
        }

        // 2회차 이상: 진엔딩 조건 확인
        if (playthroughCount == 2)
        {
            if (totalScore >= trueEndingMinScore2nd)
            {
                return EndingType.True;
            }
        }
        else if (playthroughCount >= 3)
        {
            // 3회차 이상: 진엔딩 또는 히든 엔딩
            if (totalScore >= trueEndingMinScore3rd && currentKarma >= trueEndingMinKarma3rd)
            {
                // 히든 엔딩 특별 조건 확인 (예: 특정 플래그 설정 등)
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
    /// 히든 엔딩 플래그 확인
    /// </summary>
    private bool HasHiddenEndingFlag()
    {
        // 예: 특정 이벤트에서 올바른 선택을 3회 이상 했는지 확인
        // 이는 GameData나 별도 플래그에서 확인 가능
        return false; // 임시로 false 반환
    }

    /// <summary>
    /// 엔딩 루트 결정 (승리/무승부/패배)
    /// </summary>
    public EndingRoute DetermineEndingRoute()
    {
        var totalScore = CalculateTotalKarma();
        
        if (totalScore >= 2) return EndingRoute.Victory;
        if (totalScore <= -2) return EndingRoute.Defeat;
        return EndingRoute.Truce;
    }

    /// <summary>
    /// 엔딩 분기 결정 (카르마 범위에 따라)
    /// </summary>
    public int DetermineEndingBranch(EndingRoute route)
    {
        var currentKarma = CalculateCurrentKarma();
        
        return route switch
        {
            EndingRoute.Victory => GetVictoryBranch(currentKarma),
            EndingRoute.Defeat => GetDefeatBranch(currentKarma),
            EndingRoute.Truce => GetTruceBranch(currentKarma),
            _ => 1
        };
    }

    private int GetVictoryBranch(int karma)
    {
        if (karma >= 81 && karma <= 100) return 1;
        if (karma >= 20 && karma <= 80) return 2;
        if (karma >= 0 && karma <= 19) return 3;
        return 2; // 기본값
    }

    private int GetDefeatBranch(int karma)
    {
        // 패배 루트 분기 로직 (필요시 구현)
        return 1;
    }

    private int GetTruceBranch(int karma)
    {
        // 무승부 루트 분기 로직 (필요시 구현)
        return 1;
    }

    /// <summary>
    /// 최종 엔딩 데이터 결정
    /// </summary>
    public EndingData GetFinalEndingData()
    {
        var endingType = DetermineEndingType();
        var endingRoute = DetermineEndingRoute();
        var branch = DetermineEndingBranch(endingRoute);

        Debug.Log($"[MultiEndingSystem] 최종 엔딩 결정 - 타입: {endingType}, 루트: {endingRoute}, 분기: {branch}");

        // 엔딩 데이터 리스트에서 적절한 엔딩 찾기
        var availableEndings = endingType switch
        {
            EndingType.General => generalEndings,
            EndingType.True => trueEndings,
            EndingType.Hidden => hiddenEndings,
            _ => generalEndings
        };

        // 루트와 분기에 맞는 엔딩 찾기
        var selectedEnding = availableEndings.FirstOrDefault(e => 
            e.route == endingRoute && e.branch == branch);

        if (selectedEnding == null)
        {
            Debug.LogWarning($"[MultiEndingSystem] 해당하는 엔딩을 찾을 수 없습니다. 기본 엔딩을 반환합니다.");
            return availableEndings.FirstOrDefault() ?? CreateDefaultEnding();
        }

        return selectedEnding;
    }

    /// <summary>
    /// 현재 시스템의 EndingEventData와 연동하여 엔딩 ID 결정
    /// </summary>
    public int GetEndingEventID()
    {
        var endingType = DetermineEndingType();
        var endingRoute = DetermineEndingRoute();
        var currentKarma = CalculateCurrentKarma();
        
        // 현재 EndingEventData.csv의 구조에 맞춰 엔딩 ID 결정
        int baseEndingID = 50001; // 기본 승리 루트
        
        // 루트에 따른 기본 ID 조정
        switch (endingRoute)
        {
            case EndingRoute.Victory:
                baseEndingID = 50001; // 승리 루트
                break;
            case EndingRoute.Truce:
                baseEndingID = 50019; // 무승부 루트 (예상)
                break;
            case EndingRoute.Defeat:
                baseEndingID = 50013; // 패배 루트 (예상)
                break;
        }
        
        // 카르마 범위에 따른 세부 엔딩 결정
        if (endingRoute == EndingRoute.Victory)
        {
            if (currentKarma >= 81 && currentKarma <= 100)
                return baseEndingID; // 높은 카르마 승리 엔딩
            else if (currentKarma >= 20 && currentKarma <= 80)
                return baseEndingID + 6; // 중간 카르마 승리 엔딩
            else
                return baseEndingID + 12; // 낮은 카르마 승리 엔딩
        }
        
        return baseEndingID;
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

        int endingID = GetEndingEventID();
        return DataManager.Instance.GetEndingData(endingID);
    }

    /// <summary>
    /// 기본 엔딩 생성 (안전장치)
    /// </summary>
    private EndingData CreateDefaultEnding()
    {
        return new EndingData
        {
            endingType = EndingType.General,
            route = EndingRoute.Truce,
            branch = 1,
            fullEndingData = null, // CutsceneData 대신 FullEndingData 사용
            title = "기본 엔딩",
            description = "엔딩 데이터를 찾을 수 없습니다."
        };
    }

    /// <summary>
    /// 챕터 결과 초기화 (새 게임 시작 시)
    /// </summary>
    public void ResetChapterResults()
    {
        chapterResults.Clear();
        Debug.Log("[MultiEndingSystem] 챕터 결과 초기화 완료");
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    [ContextMenu("Print Debug Info")]
    public void PrintDebugInfo()
    {
        Debug.Log("=== MultiEndingSystem 디버그 정보 ===");
        Debug.Log($"총 카르마 점수: {CalculateTotalKarma()}");
        Debug.Log($"현재 카르마: {CalculateCurrentKarma()}");
        Debug.Log($"엔딩 타입: {DetermineEndingType()}");
        Debug.Log($"엔딩 루트: {DetermineEndingRoute()}");
        Debug.Log($"엔딩 분기: {DetermineEndingBranch(DetermineEndingRoute())}");
        
        Debug.Log("챕터별 결과:");
        foreach (var result in chapterResults.OrderBy(r => r.chapter))
        {
            Debug.Log($"  챕터 {result.chapter}: {result.outcome} (전세: {result.warSituation}, 카르마: {result.karmaPoints})");
        }
    }

    /// <summary>
    /// FullEndingData를 CutsceneData로 변환 (임시 구현)
    /// TODO: 실제 이미지와 사운드 데이터를 연결하는 로직 구현 필요
    /// </summary>
    public CutsceneData ConvertToCutsceneData(FullEndingData fullEndingData)
    {
        if (fullEndingData == null) return null;
        
        // 임시로 기본 CutsceneData 생성
        // 실제로는 FullEndingData의 이미지, 텍스트, 사운드 정보를 사용해야 함
        var cutsceneData = ScriptableObject.CreateInstance<CutsceneData>();
        
        // 기본 설정
        cutsceneData.steps = new List<CutsceneStep>();
        
        // 텍스트 스텝 추가
        var textStep = new CutsceneStep();
        textStep.stepName = "엔딩 텍스트";
        textStep.enableDialogueEffect = true; // 대화 효과 활성화
        textStep.dialogueData = new DialogueEffectData
        {
            dialogue = fullEndingData.Text_Kr ?? "엔딩 텍스트가 없습니다.",
            typewriterSpeed = 0.05f
        };
        cutsceneData.steps.Add(textStep);
        
        // 이미지가 있다면 이미지 스텝 추가
        if (fullEndingData.cutSceneData != null && !string.IsNullOrEmpty(fullEndingData.cutSceneData.IMGName))
        {
            var imageStep = new CutsceneStep();
            imageStep.stepName = "엔딩 이미지";
            imageStep.enableImageEffect = true; // 이미지 효과 활성화
            // TODO: IMGName을 실제 Sprite로 로드하는 로직 필요
            imageStep.imageData = new ImageEffectData
            {
                fadeDuration = 1.0f
            };
            cutsceneData.steps.Add(imageStep);
        }
        
        return cutsceneData;
    }
}

/// <summary>
/// 챕터별 전투 결과 데이터
/// </summary>
[System.Serializable]
public class ChapterBattleResult
{
    public int chapter;
    public GameOutcome outcome;
    public int warSituation;
    public int karmaPoints;
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
/// 엔딩 데이터 클래스 - 기존 CutsceneData 대신 FullEndingData 사용
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
