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
    [SerializeField] private int trueEndingMinKarma3rd = 81; // 3회차 진엔딩 최소 카르마

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

    /*******************데이터를 이벤트 데이터에 맞게 변형 및 분류 나누기******************************/
    public void InitializeEndings(Dictionary<int, FullEndingData> allEndingData)
    {
        generalEndings.Clear();
        trueEndings.Clear();
        hiddenEndings.Clear();

        foreach (var fullData in allEndingData.Values)
        {
            EndingRoute route = ConvertCodeToRoute(fullData.EndingString);
            int branch = ConvertCodeToBranch(fullData.Karma_Rate);
            EndingType type = DetermineEndingTypeFromData(fullData.ID);

            var newEndingData = new EndingData
            {
                endingType = type,
                route = route,
                branch = branch,
                requiredPlaythrough = fullData.PlayThrough,
                fullEndingData = fullData,
                title = fullData.Text_Kr, 
                description = fullData.Text_Kr
            };

            switch (type)
            {
                case EndingType.General:
                    generalEndings.Add(newEndingData);
                    break;
                case EndingType.True:
                    trueEndings.Add(newEndingData);
                    break;
            }
        }
    }

    private EndingType DetermineEndingTypeFromData(int ID)
    {
        if (ID/10000 == 5) return EndingType.General;

        return EndingType.True;
    }

    private EndingRoute ConvertCodeToRoute(string code)
    {
        return code switch
        {
            "1001" => EndingRoute.Victory,
            "1003" => EndingRoute.Defeat,
            "1002" => EndingRoute.Truce,
            _ => EndingRoute.Truce
        };
    }

    private int ConvertCodeToBranch(int code)
    {
        return code % 10;
    }

    /*********************************************************************************************************/


    /// 엔딩 타입 결정
    public EndingType DetermineEndingType()
    {
        var playthroughCount = DataManager.Instance.PlayerData.playthroughCount;
        var totalScore = CalculateTotalBattlePoint();
        var currentKarma = CalculateCurrentKarma();

        // 2회차 이상: 진엔딩 조건 확인
        if (playthroughCount == 2 && totalScore >= trueEndingMinScore2nd)
        {
            return EndingType.True;
        }
        else if (playthroughCount == 3 && 
            totalScore >= trueEndingMinScore3rd && 
            currentKarma >= trueEndingMinKarma3rd && 
            DataManager.Instance.PlayerData.realEnding3ChoiceCount == 3)
        {
            return EndingType.True;
        }

        return EndingType.General;
    }

    public int CalculateTotalBattlePoint()
    {
        return chapterResults.Sum(r => r.BattlePoints);
    }

    /// 현재 카르마 수치 계산 (기본 50에서 시작)
    public int CalculateCurrentKarma()
    {
        if (DataManager.Instance?.PlayerData != null)
        {
            return DataManager.Instance.PlayerData.karma;
        }
        return 50;
    }

    /// 엔딩 루트 결정 (승리/무승부/패배)
    public EndingRoute DetermineEndingRoute()
    {
        var totalScore = CalculateTotalBattlePoint();

        if (totalScore >= 2) return EndingRoute.Victory;
        if (totalScore <= -2) return EndingRoute.Defeat;
        return EndingRoute.Truce;
    }

    /// 엔딩 분기 결정 (카르마 범위에 따라)
    public int DetermineEndingBranch(EndingRoute route)
    {
        var currentKarma = CalculateCurrentKarma();

        return route switch
        {
            EndingRoute.Victory => GetKarmaBranch(currentKarma),
            EndingRoute.Defeat => GetKarmaBranch(currentKarma),
            EndingRoute.Truce => GetKarmaBranch(currentKarma),
            _ => 1
        };
    }

    //챕터 결과 기록
    public void RecordChapterResult(int chapter, GameOutcome battleResult)
    {
        var result = new ChapterBattleResult
        {
            chapter = chapter,
            outcome = battleResult,
            BattlePoints = GetBattlePoints(battleResult)
        };

        // 기존 같은 챕터 결과가 있으면 교체
        var existingResult = chapterResults.FirstOrDefault(r => r.chapter == chapter);
        if (existingResult != null)
        {
            chapterResults.Remove(existingResult);
        }

        chapterResults.Add(result);

        Debug.Log($"[MultiEndingSystem] 챕터 {chapter} 결과 기록: {battleResult} , 전투 포인트: {result.BattlePoints})");
    }

    //전투 결과에 따른 전투 포인트 계산
    public int GetBattlePoints(GameOutcome outcome)
    {
        return outcome switch
        {
            GameOutcome.Victory => victoryPoint,
            GameOutcome.Defeat => defeatPoint,
            GameOutcome.Draw => drawPoint,
            _ => 0
        };
    }

    //카르마 수치에 따른 엔딩 결정 로직
    private int GetKarmaBranch(int karma)
    {
        if (karma >= 81) return 1;
        if (karma >= 20 && karma <= 80) return 2;
        if (karma >= 0 && karma <= 19) return 3;
        return 2; 
    }

    /// 최종 엔딩 데이터 결정
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
            e.route == endingRoute && 
            e.branch == branch &&
            e.requiredPlaythrough == DataManager.Instance.PlayerData.playthroughCount);

        if (selectedEnding == null)
        {
            Debug.LogWarning($"[MultiEndingSystem] 해당하는 엔딩을 찾을 수 없습니다. 기본 엔딩을 반환합니다.");
            return availableEndings.FirstOrDefault() ?? CreateDefaultEnding();
        }

        return selectedEnding;
    }


    public FullEndingData GetFullEndingData(EndingData selectedEnding)
    {
        if (DataManager.Instance == null || selectedEnding == null) return null;

        string endingStringCode = ConvertRouteToEndingStringCode(selectedEnding.route);

        int karmaRateCode = ConvertBranchToKarmaRateCode(selectedEnding.branch);

        return DataManager.Instance.FindFullEndingData(endingStringCode, karmaRateCode);
    }

    private string ConvertRouteToEndingStringCode(EndingRoute route)
    {
        return route switch
        {
            EndingRoute.Victory => "1001",
            EndingRoute.Truce => "1002", // CSV의 실제 값 확인 필요
            EndingRoute.Defeat => "1003", // CSV의 실제 값 확인 필요
            _ => "1002" // 기본값
        };
    }

    private int ConvertBranchToKarmaRateCode(int branch)
    {
        // branch 번호와 Karma_Rate 코드가 1:1로 매핑됨을 가정합니다.
        return branch switch
        {
            1 => 2001,
            2 => 2002,
            3 => 2003,
            _ => 2002 // 안전 장치
        };
    }

    /// 기본 엔딩 생성 (안전장치)
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

    /// 챕터 결과 초기화 (새 게임 시작 시)
    public void ResetChapterResults()
    {
        chapterResults.Clear();
        Debug.Log("[MultiEndingSystem] 챕터 결과 초기화 완료");
    }

    /// 디버그 정보 출력
    [ContextMenu("Print Debug Info")]
    public void PrintDebugInfo()
    {
        Debug.Log("=== MultiEndingSystem 디버그 정보 ===");
        Debug.Log($"총 카르마 점수: {CalculateTotalBattlePoint()}");
        Debug.Log($"현재 카르마: {CalculateCurrentKarma()}");
        Debug.Log($"엔딩 타입: {DetermineEndingType()}");
        Debug.Log($"엔딩 루트: {DetermineEndingRoute()}");
        Debug.Log($"엔딩 분기: {DetermineEndingBranch(DetermineEndingRoute())}");
        
        Debug.Log("챕터별 결과:");
        foreach (var result in chapterResults.OrderBy(r => r.chapter))
        {
            Debug.Log($"  챕터 {result.chapter}: {result.outcome} (전세: {result.warSituation}, 카르마: {result.BattlePoints})");
        }
    }

    /// FullEndingData를 CutsceneData로 변환 (임시 구현)
    /// TODO: 실제 이미지와 사운드 데이터를 연결하는 로직 구현 필요
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
    public int BattlePoints;
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
