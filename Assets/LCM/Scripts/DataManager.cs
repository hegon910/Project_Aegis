using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using JetBrains.Annotations;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    private UniTaskCompletionSource<bool> _isReady = new UniTaskCompletionSource<bool>();
    public UniTask IsReady => _isReady.Task;

    public List<EventStringData> StringDataList { get; private set; }
    public List<EventRewardData> RewardDataList { get; private set; }
    public List<SubEventData> SubEvents {  get; private set; }

    private Dictionary<int, ParameterEventData> eventDataDict;
    private Dictionary<int, ParameterEventStringData> eventStringDataDict;
    private Dictionary<int, ParameterRewardData> rewardDataDict;
    private Dictionary<int, string> characterNameDict; // 캐릭터 이름 룩업
    private Dictionary<int, string> rewardTypeDict; //보상 타입 룩업
    private Dictionary<int, string> choiceTextDict; //선택지 텍스트 룩업
    private Dictionary<int, string> eventDict; //이벤트타입 룩업
    private Dictionary<int, string> roundTypeDict; // 라운드타입 룩업
    private Dictionary<int, string> pageTypeDict; //페이지타입 룩업

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
        await LoadAllDataAsync();
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

    private async UniTask LoadAllDataAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        Debug.Log("데이터 비동기 로드 및 파싱 시작...");

        try
        {
            var stringTask = Csvparser.ParseAsync<EventStringData>("eventStringData", ct);
            var rewardTask = Csvparser.ParseAsync<EventRewardData>("eventRewardData", ct);

            var (loadedStringList, loadedRewardList) = await UniTask.WhenAll(stringTask, rewardTask);

            // 작업 완료 후 결과 할당
            StringDataList = loadedStringList;
            RewardDataList = loadedRewardList;

            Debug.Log($"데이터 로드 및 파싱 완료. 총 데이터: {StringDataList.Count + RewardDataList.Count}개");
        }
        catch (System.OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Debug.Log("데이터 로드 작업이 취소되었습니다 (GameObject 파괴).");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 로드 중 치명적인 오류 발생: {ex.Message}");
            throw; // 호출한 쪽으로 오류를 다시 던져서 처리를 위임
        }

    }

    public FullEventData GetFullEventData(int eventID)
    {
        // ParameterEventData.csv에서 기본 이벤트 정보 찾기
        if (!eventDataDict.TryGetValue(eventID, out var eventData))
        {
            Debug.LogError($"ID {eventID}에 해당하는 이벤트 데이터를 찾을 수 없습니다.");
            return null;
        }

        FullEventData fullData = new FullEventData();
        fullData.EventID = eventID;
        if (roundTypeDict.TryGetValue(eventData.RoundType, out var roundTypeStr))
        {
            fullData.RoundType = roundTypeStr;
        }
        if (pageTypeDict.TryGetValue(eventData.PageType, out var pageTypeStr))
        {
            fullData.PageType = pageTypeStr;
        }
        if (eventDict.TryGetValue(eventData.ChangeCondition, out var changeConditionStr))
        {
            fullData.ChangeCondition = changeConditionStr;
        }

        // ParameterEventStringData.csv에서 질문, 캐릭터, 배경 정보 찾기
        if (eventStringDataDict.TryGetValue(eventData.EventQuestion, out var questionString))
        {
            fullData.QuestionText = questionString.String_kr;
            fullData.BG = questionString.BG;
            fullData.SE = questionString.SoundEffect;
            fullData.IsFinish = questionString.IsFinishString == 1;

            // CharacterName(int)으로 실제 캐릭터 이름(string) 찾기
            if (characterNameDict.TryGetValue(questionString.CharacterName, out var characterName))
            {
                fullData.CharacterName = characterName;
            }
            fullData.CharacterImage = questionString.CharacterImage;
        }

        // ParameterEventDataList.csv에서 선택지 텍스트 찾기
        if (choiceTextDict.TryGetValue(eventData.LeftString, out var leftText))
        {
            fullData.LeftOptionText = leftText;
        }
        if (choiceTextDict.TryGetValue(eventData.RightString, out var rightText))
        {
            fullData.RightOptionText = rightText;
        }

        // ParameterRewardData.csv에서 보상 정보 찾기 및 조합
        fullData.LeftRewards = GetRewards(eventData.AcceptReward1);
        fullData.RightRewards = GetRewards(eventData.AcceptReward2); // DenyReward1이 오른쪽 보상일 가능성

        // 선택지 응답 텍스트 가져오기 (LeftAcceptString, DenyString1)
        if (eventStringDataDict.TryGetValue(eventData.AcceptString1, out var acceptString))
        {
            fullData.LeftAcceptString = acceptString.String_kr;
        }
        if (eventStringDataDict.TryGetValue(eventData.AcceptString2, out var acceptString2))
        {
            fullData.RightAcceptString = acceptString2.String_kr;
        }

        // NeedType 값 할당
        if (eventDict.TryGetValue(eventData.NeedType1, out var leftNeed))
        {
            fullData.LeftNeedType = leftNeed;
        }
        if (eventDict.TryGetValue(eventData.NeedType2, out var rightNeed))
        {
            fullData.RightNeedType = rightNeed;
        }

        return fullData;
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
            SubEvents = loadedSubEventActionList.Select(actionData => {

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
            // 데이터 구조 테스트 코드
            //foreach (var subEvent in SubEvents)
            //{
            //    Debug.Log($"[Index: {subEvent.Index}] " +
            //              $"팩넘버 : {subEvent.PackNumber}" +
            //              $"질문: {subEvent.QuestionString_kr} | " +
            //              $"캐릭터: {subEvent.CharacterName} | " +
            //              $"선택지1: {subEvent.LeftSelectString} | " +
            //              $"선택지2: {subEvent.RightSelectString} | " +
            //              $"엔딩텍스트: {subEvent.End_Text}");
            //}
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 초기화 실패: {ex.Message}");
            SubEvents = new List<SubEventData>();
        }
    }



    // ID를 통해 두 데이터를 합쳐서 새로운 구조의 EventData로 반환하는 함수
    public EventData GetEventDataById(int id)
    {

        if (StringDataList == null || RewardDataList == null)
        {
            return null;
        }

        var stringData = StringDataList.FirstOrDefault(data => data.ID == id);
        var rewardData = RewardDataList.FirstOrDefault(data => data.ID == id);

        if (stringData == null || rewardData == null)
        {
            Debug.LogError($"이벤트 데이터를 찾을수없습니다 : {id}");
            return null;
        }

        EventData eventData = new EventData();

        // 기본 정보
        eventData.eventName = $"이벤트 : {id}"; // CSV에 이벤트 이름이 없어 ID로 임시 생성
        eventData.dialogue = stringData.eventText;

        // 왼쪽 선택지 정보
        eventData.leftChoice = new EventChoice
        {
            choiceText = stringData.leftChoiceText,
            successCondition = rewardData.left_success_threshold,
            failCondition = rewardData.left_fail_threshold,
            successOutcome = new ChoiceOutcome { outcomeText = stringData.leftSuccessText },
            failOutcome = new ChoiceOutcome { outcomeText = stringData.leftFailText }
        };

        // 왼쪽 성공 효과
        if (rewardData.left_success_delta_politics != 0) eventData.leftChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.정치력, valueChange = rewardData.left_success_delta_politics });
        if (rewardData.left_success_delta_military != 0) eventData.leftChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.병력, valueChange = rewardData.left_success_delta_military });
        if (rewardData.left_success_delta_supplies != 0) eventData.leftChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.물자, valueChange = rewardData.left_success_delta_supplies });
        if (rewardData.left_success_delta_leadership != 0) eventData.leftChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.리더십, valueChange = rewardData.left_success_delta_leadership });
        if (rewardData.left_success_delta_war != 0) eventData.leftChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.전황, valueChange = rewardData.left_success_delta_war });
        if (rewardData.left_success_delta_karma != 0) eventData.leftChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.카르마, valueChange = rewardData.left_success_delta_karma });

        // 왼쪽 실패 효과
        if (rewardData.left_fail_delta_politics != 0) eventData.leftChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.정치력, valueChange = rewardData.left_fail_delta_politics });
        if (rewardData.left_fail_delta_military != 0) eventData.leftChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.병력, valueChange = rewardData.left_fail_delta_military });
        if (rewardData.left_fail_delta_supplies != 0) eventData.leftChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.물자, valueChange = rewardData.left_fail_delta_supplies });
        if (rewardData.left_fail_delta_leadership != 0) eventData.leftChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.리더십, valueChange = rewardData.left_fail_delta_leadership });
        if (rewardData.left_fail_delta_war != 0) eventData.leftChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.전황, valueChange = rewardData.left_fail_delta_war });
        if (rewardData.left_fail_delta_karma != 0) eventData.leftChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.카르마, valueChange = rewardData.left_fail_delta_karma });

        // 오른쪽 선택지 정보 매핑
        eventData.rightChoice = new EventChoice
        {
            choiceText = stringData.rightChoiceText,
            successCondition = rewardData.right_success_threshold,
            failCondition = rewardData.right_fail_threshold,
            successOutcome = new ChoiceOutcome { outcomeText = stringData.rightSuccessText },
            failOutcome = new ChoiceOutcome { outcomeText = stringData.rightFailText }
        };

        // 오른쪽 성공 효과
        if (rewardData.right_success_delta_politics != 0) eventData.rightChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.정치력, valueChange = rewardData.right_success_delta_politics });
        if (rewardData.right_success_delta_military != 0) eventData.rightChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.병력, valueChange = rewardData.right_success_delta_military });
        if (rewardData.right_success_delta_supplies != 0) eventData.rightChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.물자, valueChange = rewardData.right_success_delta_supplies });
        if (rewardData.right_success_delta_leadership != 0) eventData.rightChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.리더십, valueChange = rewardData.right_success_delta_leadership });
        if (rewardData.right_success_delta_war != 0) eventData.rightChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.전황, valueChange = rewardData.right_success_delta_war });
        if (rewardData.right_success_delta_karma != 0) eventData.rightChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.카르마, valueChange = rewardData.right_success_delta_karma });

        // 오른쪽 실패 효과
        if (rewardData.right_fail_delta_politics != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.정치력, valueChange = rewardData.right_fail_delta_politics });
        if (rewardData.right_fail_delta_military != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.병력, valueChange = rewardData.right_fail_delta_military });
        if (rewardData.right_fail_delta_supplies != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.물자, valueChange = rewardData.right_fail_delta_supplies });
        if (rewardData.right_fail_delta_leadership != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.리더십, valueChange = rewardData.right_fail_delta_leadership });
        if (rewardData.right_fail_delta_war != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.전황, valueChange = rewardData.right_fail_delta_war });
        if (rewardData.right_fail_delta_karma != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.카르마, valueChange = rewardData.right_fail_delta_karma });

        return eventData;
    }

    //테스트 코드
    public void LogFullEventData(int eventID)
    {
        FullEventData eventData = GetFullEventData(eventID);

        if (eventData == null)
        {
            Debug.LogError($"이벤트 ID {eventID}의 데이터를 찾을 수 없어 로그를 출력할 수 없습니다.");
            return;
        }

        string logMessage = $"--- Event ID: {eventData.EventID} ---";
        logMessage += $"\nRoundType: {eventData.RoundType}";
        logMessage += $"\nPageType: {eventData.PageType}";
        logMessage += $"\nChangeCondition: {eventData.ChangeCondition}";
        logMessage += $"\nQuestionText: {eventData.QuestionText}";
        logMessage += $"\nCharacterName: {eventData.CharacterName}";
        logMessage += $"\nCharacterImage: {eventData.CharacterImage}";
        logMessage += $"\nBG: {eventData.BG}";
        logMessage += $"\nSE: {eventData.SE}";
        logMessage += $"\nIsFinish: {eventData.IsFinish}";

        logMessage += "\n\n--- Choices & Rewards ---";
        logMessage += $"\nLeft Option: {eventData.LeftOptionText}";
        logMessage += $"\n - Response: {eventData.LeftAcceptString}";
        logMessage += $"\n - Need Type: {eventData.LeftNeedType}";
        logMessage += $"\n - Rewards:";
        foreach (var reward in eventData.LeftRewards)
        {
            logMessage += $"  -> {reward.RewardType}: {reward.RewardValue}";
        }

        logMessage += $"\nRight Option: {eventData.RightOptionText}";
        logMessage += $"\n - Response: {eventData.RightAcceptString}";
        logMessage += $"\n - Need Type: {eventData.RightNeedType}";
        logMessage += $"\n - Rewards:";
        foreach (var reward in eventData.RightRewards)
        {
            logMessage += $"  -> {reward.RewardType}: {reward.RewardValue}";
        }

        Debug.Log(logMessage);
    }
}



// Csvparser를 위해 필요한 데이터 클래스들
[System.Serializable]
public class EventStringData
{
    public int ID { get; set; }
    public string eventText { get; set; }
    public string leftChoiceText { get; set; }
    public string rightChoiceText { get; set; }
    public string leftSuccessText { get; set; }
    public string leftFailText { get; set; }
    public string rightSuccessText { get; set; }
    public string rightFailText { get; set; }
    public string charactername { get; set; }
    public string PageType { get; set; }
    public string characterImage { get; set; }  
}

[System.Serializable]
public class EventRewardData
{
    public int ID { get; set; }
    public int left_success_delta_politics { get; set; }
    public int left_success_delta_military { get; set; }
    public int left_success_delta_supplies { get; set; }
    public int left_success_delta_leadership { get; set; }
    public int left_success_delta_war { get; set; }
    public int left_success_delta_karma { get; set; }
    public int left_fail_delta_politics { get; set; }
    public int left_fail_delta_military { get; set; }
    public int left_fail_delta_supplies { get; set; }
    public int left_fail_delta_leadership { get; set; }
    public int left_fail_delta_war { get; set; }
    public int left_fail_delta_karma { get; set; }
    public string left_success_threshold{ get; set; }
    public string left_fail_threshold{ get; set; }
    public int right_success_delta_politics { get; set; }
    public int right_success_delta_military { get; set; }
    public int right_success_delta_supplies { get; set; }
    public int right_success_delta_leadership { get; set; }
    public int right_success_delta_war { get; set; }
    public int right_success_delta_karma { get; set; }
    public int right_fail_delta_politics { get; set; }
    public int right_fail_delta_military { get; set; }
    public int right_fail_delta_supplies { get; set; }
    public int right_fail_delta_leadership { get; set; }
    public int right_fail_delta_war { get; set; }
    public int right_fail_delta_karma { get; set; }
    public string right_success_threshold{ get; set; }
    public string right_fail_threshold{ get; set; }
}

//데이터 테이블
[System.Serializable]
public class ParameterEventData
{
    public int ID { get; set; }
    public int RoundType { get; set; }
    public int PageType { get; set; }
    public int ChangeCondition { get; set; }
    public int IsConditionSuccess { get; set; }
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
    public int AnotehrAcceptReward1 { get; set; }
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
    public string Chr_name{ get; set; }
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
//통합 데이터 클래스 
[System.Serializable]
public class FullEventData
{
    public int EventID { get; set; }
    public string RoundType { get; set; } //ParameterEventDataList에서 가져와야 하는값 (Appearance_Type)
    public string PageType { get; set; } // ParameterEventDataList에서 가져와야 하는값 (PageType)
    public string ChangeCondition { get; set; }// ParameterEventDataList에서 가져와야 하는값 (Parameter_type)
    public string QuestionText { get; set; }
    public string CharacterName { get; set; }
    public string CharacterImage { get; set; }
    public string BG { get; set; }
    public string SE { get; set; }
    public bool IsFinish { get; set; }
    public string LeftOptionText { get; set; }
    public string RightOptionText { get; set; }
    public List<RewardInfo> LeftRewards { get; set; }
    public List<RewardInfo> RightRewards { get; set; }
    public string LeftAcceptString { get; set; }
    public string RightAcceptString { get; set; }
    public string LeftNeedType { get; set; } 
    public string RightNeedType { get; set; } 
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
    public string PackList {  get; set; }
    public int IsFinish { get ; set; }
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
    public int NextRightSelectString { get;set; }
    public int RightSelectReward { get; set; }
    public int IsFinishString { get; set; }
    public int CharacterName { get; set; }
    public string CharacterImg { get; set; }
    public string BG {  get; set; }
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