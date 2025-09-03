using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using JetBrains.Annotations;
using static DataManager;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    private UniTaskCompletionSource<bool> _isReady = new UniTaskCompletionSource<bool>();
    public UniTask IsReady => _isReady.Task;

    public List<SubEventData> SubEvents { get; private set; }

    public Dictionary<int, ParameterEventData> eventDataDict;
    public Dictionary<int, ParameterEventStringData> eventStringDataDict;
    public Dictionary<int, ParameterRewardData> rewardDataDict;
    public Dictionary<int, string> characterNameDict; // 캐릭터 이름 룩업
    public Dictionary<int, string> rewardTypeDict; //보상 타입 룩업
    public Dictionary<int, string> choiceTextDict; //선택지 텍스트 룩업
    public Dictionary<int, string> eventDict; //이벤트타입 룩업
    public Dictionary<int, string> roundTypeDict; // 라운드타입 룩업
    public Dictionary<int, string> pageTypeDict; //페이지타입 룩업

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

    public async UniTask InitializeDataAsync()
    {
        try
        {
            Debug.Log("이벤트 데이터 로딩 시작");
            var eventDataTask = Csvparser.ParseAsync<ParameterEventData>("ParameterEventData");
            Debug.Log("이벤트 스트링 데이터 로딩 시작");
            var eventStringDataTask = Csvparser.ParseAsync<ParameterEventStringData>("ParameterEventStringData");
            Debug.Log("보상 데이터 로딩 시작");
            var rewardDataTask = Csvparser.ParseAsync<ParameterRewardData>("ParameterRewardData");
            Debug.Log("이벤트 스트링 데이터 리스트 데이터 로딩 시작");
            var characterTask = Csvparser.ParseAsync<CharacterData>("ParameterEventStringDataList");
            Debug.Log("보상 데이터 리스트 로딩 시작");
            var rewardTypeTask = Csvparser.ParseAsync<RewardTypeData>("ParameterRewardDataList");
            Debug.Log("이벤트 데이터 리스트 로딩 시작");
            var choTextTask = Csvparser.ParseAsync<EventDataList>("ParameterEventDataList");

            var (eventList, eventStringList, rewardList, characterList, rewardTypeList, choTextList) =
            await UniTask.WhenAll(eventDataTask, eventStringDataTask, rewardDataTask, characterTask, rewardTypeTask, choTextTask);

            Debug.Log("모든 파일 로딩 완료");

            eventDataDict = eventList.ToDictionary(e => e.ID, e => e);
            eventStringDataDict = eventStringList.ToDictionary(e => e.ID, e => e);
            rewardDataDict = rewardList.ToDictionary(r => r.ID, r => r);
            characterNameDict = characterList.ToDictionary(c => c.Chr_index, c => c.Chr_name);
            rewardTypeDict = rewardTypeList.ToDictionary(t => t.RewardType_index, t => t.RewardType);
            choiceTextDict = choTextList.ToDictionary(c => c.Cho_Num, c => c.Cho_txt);
            roundTypeDict = choTextList.GroupBy(a => a.Appearance_Num)
                                   .ToDictionary(g => g.Key, g => g.First().Appearance_Type);

            pageTypeDict = choTextList.GroupBy(p => p.PageType_Num)
                                      .ToDictionary(g => g.Key, g => g.First().PageType);

            eventDict = choTextList.GroupBy(c => c.Parameter_Num)
                                       .ToDictionary(g => g.Key, g => g.First().Parameter_type);

            _isReady.TrySetResult(true);
            Debug.Log("모든 이벤트 데이터가 성공적으로 로드되었습니다.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 로드 실패: {ex.Message}");
            _isReady.TrySetException(ex);
        }
    }

    public async UniTask SubIntializeDataAsync()
    {
        await LoadSubAllDataAsync();
    }

    private async UniTask LoadSubAllDataAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        try
        {
            // 두 개의 원본 CSV 파일을 비동기로 로드
            var subEventDataListTask = Csvparser.ParseAsync<SubEventDataList>("SubEventDataList", ct);
            var subEventActionDataTask = Csvparser.ParseAsync<SubEventActionData>("SubEventData", ct);

            var (loadedSubEventList, loadedSubEventActionList) = await UniTask.WhenAll(subEventDataListTask, subEventActionDataTask);

            // StringNum을 키로 하는 이벤트 정보 룩업 테이블 생성
            var subEventListLookup = loadedSubEventList
                .Where(s => s.StringNum != 0)
                .ToDictionary(s => s.StringNum, s => s);
            // Cho_Num을 키로 하는 선택지 텍스트 룩업 테이블 생성
            var choiceTextLookup = loadedSubEventList
                .Where(s => s.Cho_Num != 0 && !string.IsNullOrEmpty(s.Cho_Text))
                .ToDictionary(s => s.Cho_Num, s => s.Cho_Text);
            // End_Num을 키로 하는 엔딩 텍스트 룩업 테이블 추가
            var endTextLookup = loadedSubEventList
                .Where(s => s.End_Num != 0 && !string.IsNullOrEmpty(s.End_Text))
                .ToDictionary(s => s.End_Num, s => s.End_Text);
            //Chr_index를 키로 하는 캐릭터 이름 룩업 테이블 추가
            var characterNameLookup = loadedSubEventList
                .Where(s => s.Chr_index != 0 && !string.IsNullOrEmpty(s.Chr_Name))
                .ToDictionary(s => s.Chr_index, s => s.Chr_Name);

            // SubEventActionData를 순회하며 데이터를 통합합니다.
            SubEvents = loadedSubEventActionList.Select(actionData =>
            {

                // EndingString으로 SubEventDataList 정보 조회
                if (!subEventListLookup.TryGetValue(actionData.EndingString, out var listData))
                {
                    return null; // 매칭되는 데이터가 없으면 통합하지 않음
                }

                // LeftSelectString과 RightSelectString으로 선택지 텍스트 조회
                string leftText = string.Empty;
                choiceTextLookup.TryGetValue(actionData.LeftSelectString, out leftText);

                string rightText = string.Empty;
                choiceTextLookup.TryGetValue(actionData.RightSelectString, out rightText);

                // SubEventActionData의 CharacterName(정수)을 사용해 캐릭터 이름 조회
                string characterName = string.Empty;
                characterNameLookup.TryGetValue(actionData.CharacterName, out characterName);

                string endText = string.Empty;
                endTextLookup.TryGetValue(listData.End_Num, out endText);

                // 새로운 SubEventData 객체 생성 및 값 채우기
                return new SubEventData
                {
                    // SubEventActionData에서 가져올 필드
                    Index = actionData.Index,
                    PackNumber = actionData.PackNumber,
                    GroupNumber = actionData.GroupNumber,
                    NextLeftSelectString = actionData.NextLeftSelectString.ToString(),
                    NextRightSelectString = actionData.NextRightSelectString.ToString(),
                    IsFinish = actionData.IsFinishString == 1,
                    CharacterImg = actionData.CharacterImg,
                    BG = actionData.BG,
                    SE = actionData.SE,
                    QuestionString_kr = actionData.QuestionString_kr,

                    // SubEventDataList에서 가져올 필드
                    LeftSelectString = leftText,
                    RightSelectString = rightText,
                    CharacterName = characterName,
                    End_Text = endText
                };
            }).Where(e => e != null).ToList();

            Debug.Log($"데이터 로드 및 통합 완료! 총 {SubEvents.Count}개의 이벤트가 준비되었습니다.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 초기화 실패: {ex.Message}");
            SubEvents = new List<SubEventData>();
        }
    }



    // ID를 통해 두 데이터를 합쳐서 새로운 구조의 EventData로 반환하는 함수
    public EventData GetEventDataById(int eventID)
    {
        // ParameterEventData.csv에서 기본 이벤트 정보 찾기
        if (!eventDataDict.TryGetValue(eventID, out var eventData))
        {
            Debug.LogError($"ID {eventID}에 해당하는 이벤트 데이터를 찾을 수 없습니다.");
            return null;
        }

        EventData fullEventData = new EventData
        {
            id = eventID,
            eventName = $"Event_{eventID}" // 임시 이름
        };

        bool hasChangeCondition = eventDataDict.TryGetValue(eventData.ChangeCondition, out var anotherEventData);
        bool isConditionSuccess = hasChangeCondition && anotherEventData.IsConditionSuccess;

        // 대화, 배경, 효과음, 캐릭터 정보 설정
        int questionId = isConditionSuccess ? eventData.AnotherEventQuestion : eventData.EventQuestion;
        if (eventStringDataDict.TryGetValue(questionId, out var questionString))
        {
            fullEventData.dialogue = questionString.String_kr;
            fullEventData.BG = questionString.BG;
            fullEventData.SE = questionString.SoundEffect;
            fullEventData.CharacterImage = questionString.CharacterImage;
            if (characterNameDict.TryGetValue(questionString.CharacterName, out var characterName))
            {
                fullEventData.CharacterName = characterName;
            }
        }

        // 왼쪽 선택지 설정
        fullEventData.leftChoice = new EventChoice();
        int leftChoiceId = isConditionSuccess ? eventData.AnotherLeftString : eventData.LeftString;
        if (choiceTextDict.TryGetValue(leftChoiceId, out var leftText))
        {
            fullEventData.leftChoice.choiceText = leftText;
        }

        // 왼쪽 성공 결과 설정
        fullEventData.leftChoice.successOutcome = new ChoiceOutcome();
        int leftSuccessStringId = isConditionSuccess ? eventData.AnotherAcceptString1 : eventData.AcceptString1;
        int leftSuccessRewardId = isConditionSuccess ? eventData.AnotherAcceptReward1 : eventData.AcceptReward1;
        if (eventStringDataDict.TryGetValue(leftSuccessStringId, out var leftSuccessString))
        {
            fullEventData.leftChoice.successOutcome.outcomeText = leftSuccessString.String_kr;
        }
        fullEventData.leftChoice.successOutcome.parameterChanges.AddRange(ConvertRewardsToParameterChanges(GetRewards(leftSuccessRewardId)));

        // 왼쪽 실패 결과 설정
        fullEventData.leftChoice.failOutcome = new ChoiceOutcome();
        int leftFailStringId = isConditionSuccess ? eventData.AnotherDenyString1 : eventData.DenyString1;
        int leftFailRewardId = isConditionSuccess ? eventData.AnotherDenyReward1 : eventData.DenyReward1;
        if (eventStringDataDict.TryGetValue(leftFailStringId, out var leftFailString))
        {
            fullEventData.leftChoice.failOutcome.outcomeText = leftFailString.String_kr;
        }
        fullEventData.leftChoice.failOutcome.parameterChanges.AddRange(ConvertRewardsToParameterChanges(GetRewards(leftFailRewardId)));


        // 오른쪽 선택지 설정
        fullEventData.rightChoice = new EventChoice();
        int rightChoiceId = isConditionSuccess ? eventData.AnotherRightString : eventData.RightString;
        if (choiceTextDict.TryGetValue(rightChoiceId, out var rightText))
        {
            fullEventData.rightChoice.choiceText = rightText;
        }

        // 오른쪽 성공 결과 설정
        fullEventData.rightChoice.successOutcome = new ChoiceOutcome();
        int rightSuccessStringId = isConditionSuccess ? eventData.AnotherAcceptString2 : eventData.AcceptString2;
        int rightSuccessRewardId = isConditionSuccess ? eventData.AnotherAcceptReward2 : eventData.AcceptReward2;
        if (eventStringDataDict.TryGetValue(rightSuccessStringId, out var rightSuccessString))
        {
            fullEventData.rightChoice.successOutcome.outcomeText = rightSuccessString.String_kr;
        }
        fullEventData.rightChoice.successOutcome.parameterChanges.AddRange(ConvertRewardsToParameterChanges(GetRewards(rightSuccessRewardId)));

        // 오른쪽 실패 결과 설정
        fullEventData.rightChoice.failOutcome = new ChoiceOutcome();
        int rightFailStringId = isConditionSuccess ? eventData.AnotherDenyString2 : eventData.DenyString2;
        int rightFailRewardId = isConditionSuccess ? eventData.AnotherDenyReward2 : eventData.DenyReward2;
        if (eventStringDataDict.TryGetValue(rightFailStringId, out var rightFailString))
        {
            fullEventData.rightChoice.failOutcome.outcomeText = rightFailString.String_kr;
        }
        fullEventData.rightChoice.failOutcome.parameterChanges.AddRange(ConvertRewardsToParameterChanges(GetRewards(rightFailRewardId)));

        // 성공 조건 생성 (isConditionSuccess와 무관하게 항상 원래 이벤트의 조건 타입을 따름)
        int leftNeedType = isConditionSuccess ? eventData.AnotherNeedType1 : eventData.NeedType1;
        int leftNeedValue = isConditionSuccess ? eventData.AnotehrNeedValue1 : eventData.NeedValue1;
        int rightNeedType = isConditionSuccess ? eventData.AnotherNeedType2 : eventData.NeedType2;
        int rightNeedValue = isConditionSuccess ? eventData.AnotherNeedValue2 : eventData.NeedValue2;

        fullEventData.leftChoice.condition = CreateSuccessConditionForChoice(eventData.ConditionType, leftNeedType, leftNeedValue, eventData.ChangeCondition);
        fullEventData.rightChoice.condition = CreateSuccessConditionForChoice(eventData.ConditionType, rightNeedType, rightNeedValue, eventData.ChangeCondition);


        // 기타 데이터 설정 (IsConditionSuccess와 무관한 필드)
        if (roundTypeDict.TryGetValue(eventData.RoundType, out var roundTypeStr))
        {
            fullEventData.RoundType = roundTypeStr;
        }
        if (eventDict.TryGetValue(eventData.ConditionType, out var changeConditionStr))
        {
            fullEventData.ConditionType = changeConditionStr;
        }

        return fullEventData;
    }
    
    /// <summary>
    /// CSV 데이터 기반으로 다양한 성공 조건 객체를 생성합니다.
    /// </summary>
    private SuccessCondition CreateSuccessConditionForChoice(int conditionType, int needType, int needValue, int changeCondition)
    {
        switch (conditionType)
        {
            case 1: // 파라미터 이벤트 기록
            case 2: // 서브 이벤트 기록
                return new HistorySuccessCondition { requiredEventID = changeCondition };
            case 3: // 전투 결과
                return new BattleResultSuccessCondition { requiredBattleResult = needValue };
            case 4: // 엔딩
                return new EndingSuccessCondition { requiredEndingID = needValue };
            case 0: // 조건 없음 -> NeedType을 확인하여 파라미터 또는 무조건 성공으로 분기
            default:
                if (needType == 0) // NeedType이 0이면 무조건 성공
                {
                    return new GuaranteedSuccessCondition();
                }
                else // NeedType이 0이 아니면 파라미터 조건
                {
                    if (eventDict.TryGetValue(needType, out var paramName))
                    {
                        return new ParameterSuccessCondition
                        {
                            targetParameter = GetParameterType(paramName),
                            requiredValue = needValue
                        };
                    }
                    else
                    {
                        // 해당하는 파라미터 이름을 찾지 못할 경우 안전하게 무조건 성공으로 처리
                        Debug.LogWarning($"SuccessCondition 생성 실패: NeedType {needType}에 해당하는 파라미터를 eventDict에서 찾을 수 없습니다. GuaranteedSuccessCondition으로 대체합니다.");
                        return new GuaranteedSuccessCondition();
                    }
                }
        }
    }
    private List<RewardInfo> GetRewards(int rewardID)
    {
        var rewards = new List<RewardInfo>();
        if (rewardID == 0) return rewards; // 보상 ID가 0이면 빈 리스트 반환

        if (rewardDataDict.TryGetValue(rewardID, out var rewardData))
        {
            // 여러 개의 보상을 리스트에 추가
            if (rewardData.RewardType1 != 0 && rewardTypeDict.TryGetValue(rewardData.RewardType1, out var type1))
                rewards.Add(new RewardInfo { RewardType = type1, RewardValue = rewardData.RewardValue1 });
            if (rewardData.RewardType2 != 0 && rewardTypeDict.TryGetValue(rewardData.RewardType2, out var type2))
                rewards.Add(new RewardInfo { RewardType = type2, RewardValue = rewardData.RewardValue2 });
            if (rewardData.RewardType3 != 0 && rewardTypeDict.TryGetValue(rewardData.RewardType3, out var type3))
                rewards.Add(new RewardInfo { RewardType = type3, RewardValue = rewardData.RewardValue3 });
            if (rewardData.RewardType4 != 0 && rewardTypeDict.TryGetValue(rewardData.RewardType4, out var type4))
                rewards.Add(new RewardInfo { RewardType = type4, RewardValue = rewardData.RewardValue4 });
            if (rewardData.RewardType5 != 0 && rewardTypeDict.TryGetValue(rewardData.RewardType5, out var type5))
                rewards.Add(new RewardInfo { RewardType = type5, RewardValue = rewardData.RewardValue5 });
        }
        return rewards;
    }

    // RewardInfo 리스트를 ParameterChange 리스트로 변환하는 헬퍼 함수
    private List<ParameterChange> ConvertRewardsToParameterChanges(List<RewardInfo> rewards)
    {
        List<ParameterChange> changes = new List<ParameterChange>();
        if (rewards == null)
        {
            return changes;
        }

        foreach (var reward in rewards)
        {
            ParameterType type = GetParameterType(reward.RewardType);

            changes.Add(new ParameterChange
            {
                parameterType = type,
                valueChange = reward.RewardValue
            });
        }
        return changes;
    }

    private ParameterType GetParameterType(string rewardType)
    {
        return rewardType switch
        {
            "정치력" => ParameterType.정치력,
            "병력" => ParameterType.병력,
            "물자" => ParameterType.물자,
            "리더십" => ParameterType.리더십,
            "전세" => ParameterType.전황,
            "카르마" => ParameterType.카르마,
        };
    }


    //데이터 테이블
    [System.Serializable]
    public class ParameterEventData
    {
        public int ID { get; set; }
        public int RoundType { get; set; }
        public int PageType { get; set; }
        public int ConditionType { get; set; }
        public int ChangeCondition { get; set; }
        public bool IsConditionSuccess { get; set; }
        public int EventQuestion { get; set; }
        public int LeftString { get; set; }
        public int NeedType1 { get; set; }
        public int NeedValue1 { get; set; }
        public int AcceptReward1 { get; set; }
        public int DenyReward1 { get; set; }
        public int AcceptString1 { get; set; }
        public int DenyString1 { get; set; }
        public int RightString { get; set; }
        public int NeedType2 { get; set; }
        public int NeedValue2 { get; set; }
        public int AcceptReward2 { get; set; }
        public int DenyReward2 { get; set; }
        public int AcceptString2 { get; set; }
        public int DenyString2 { get; set; }
        public int AnotherEventQuestion { get; set; }
        public int AnotherLeftString { get; set; }
        public int AnotherNeedType1 { get; set; }
        public int AnotehrNeedValue1 { get; set; }
        public int AnotherAcceptReward1 { get; set; }
        public int AnotherDenyReward1 { get; set; }
        public int AnotherAcceptString1 { get; set; }
        public int AnotherDenyString1 { get; set; }
        public int AnotherRightString { get; set; }
        public int AnotherNeedType2 { get; set; }
        public int AnotherNeedValue2 { get; set; }
        public int AnotherAcceptReward2 { get; set; }
        public int AnotherDenyReward2 { get; set; }
        public int AnotherAcceptString2 { get; set; }
        public int AnotherDenyString2 { get; set; }
    }
    [System.Serializable]
    public class ParameterRewardData
    {
        public int ID { get; set; }
        public int RewardType1 { get; set; }
        public int RewardValue1 { get; set; }
        public int RewardType2 { get; set; }
        public int RewardValue2 { get; set; }
        public int RewardType3 { get; set; }
        public int RewardValue3 { get; set; }
        public int RewardType4 { get; set; }
        public int RewardValue4 { get; set; }
        public int RewardType5 { get; set; }
        public int RewardValue5 { get; set; }
    }
    [System.Serializable]
    public class ParameterEventStringData
    {
        public int ID { get; set; }
        public string BG { get; set; }
        public string SoundEffect { get; set; }
        public int CharacterName { get; set; }
        public string CharacterImage { get; set; }
        public int IsFinishString { get; set; }
        public string String_kr { get; set; }
    }
    //룩업 테이블용 클래스 
    [System.Serializable]
    public class CharacterData
    {
        public string Chr_name { get; set; }
        public int Chr_index { get; set; }
    }

    [System.Serializable]
    public class RewardTypeData
    {
        public string RewardType { get; set; }
        public int RewardType_index { get; set; }
    }

    [System.Serializable]
    public class EventDataList
    {
        public int Appearance_Num { get; set; }
        public string Appearance_Type { get; set; }
        public int PageType_Num { get; set; }
        public string PageType { get; set; }
        public int Parameter_Num { get; set; }
        public string Parameter_type { get; set; }
        public int Event_Num { get; set; }
        public string Event_Type { get; set; }
        public int Cho_Num { get; set; }
        public string Cho_txt { get; set; }
    }
    [System.Serializable]
    public class RewardInfo
    {
        public string RewardType { get; set; }
        public int RewardValue { get; set; }
    }

    [System.Serializable]
    public class SubEventDataList
    {
        public int StringNum { get; set; }
        public int PackNum { get; set; }
        public string PackList { get; set; }
        public int IsFinish { get; set; }
        public bool SubStory_End { get; set; }
        public int Chr_index { get; set; }
        public string Chr_Name { get; set; }
        public int End_Num { get; set; }
        public string End_Text { get; set; }
        public int Cho_Num { get; set; }
        public string Cho_Text { get; set; }
    }

    [System.Serializable]
    public class SubEventActionData
    {
        public int Index { get; set; }
        public int PackNumber { get; set; }
        public int GroupNumber { get; set; }
        public int EndingString { get; set; }
        public int LeftSelectString { get; set; }
        public int NextLeftSelectString { get; set; }
        public int LeftSelectReward { get; set; }
        public int RightSelectString { get; set; }
        public int NextRightSelectString { get; set; }
        public int RightSelectReward { get; set; }
        public int IsFinishString { get; set; }
        public int CharacterName { get; set; }
        public string CharacterImg { get; set; }
        public string BG { get; set; }
        public string SE { get; set; }
        public string QuestionString_kr { get; set; }
    }

    [System.Serializable]
    public class SubEventData
    {
        // SubEventActionData에서 가져올 필드
        public int Index { get; set; }
        public int PackNumber { get; set; }
        public int GroupNumber { get; set; }
        public string NextLeftSelectString { get; set; } //SubEventData 에서 인덱스값에 맞춤
        public string NextRightSelectString { get; set; } //SubEventData 에서 인덱스값에 맞춤
        public bool IsFinish { get; set; } //IsFinishString (int)를 bool값으로 변환
        public string CharacterImg { get; set; }
        public string BG { get; set; }
        public string SE { get; set; }
        public string QuestionString_kr { get; set; }

        //public string LeftSelectReward { get; set; } 미정
        //public string RightSelectReward { get; set; } 미정

        //SubEventDataList에서 가져올 필드
        public string LeftSelectString { get; set; }
        public string RightSelectString { get; set; }
        public string CharacterName { get; set; }
        public string End_Text { get; set; }
    }
}