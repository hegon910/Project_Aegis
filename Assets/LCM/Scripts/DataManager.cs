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

    public List<SubEventData> SubEvents { get; private set; }

    //메인 이벤트 
    public Dictionary<int, MainEventData> mainEventDataDict;
    public Dictionary<int, AnswerData> answerDataDict;
    //메인 룩업 테이블
    private Dictionary<int, MainCharacterData> CharacterDataDict;
    private Dictionary<long, BGData> bgDataDict;
    private Dictionary<long, SFXData> sfxDataDict;
    private Dictionary<long, MainCharacterImgData> characterImgDataDict;
    //파라미터 이벤트
    //메인 이벤트 데이터


    public Dictionary<int, MainEventData> mainEventDict;
    public Dictionary<int, AnswerIDData> answerIDDict;


    //파라미터 이벤트 
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
        await MainEventInitializeDataAsync();
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

    public async UniTask MainEventInitializeDataAsync()
    {
        try
        {
            Debug.Log("이벤트 데이터 로딩 시작");

            // MainEventData11.csv와 AnswerID.csv를 비동기로 로드합니다.
            var mainEventTask = Csvparser.ParseAsync<MainEventData>("MainEventData");
            var answerTask = Csvparser.ParseAsync<AnswerData>("MainAnswerID");
            //룩업 테이블 로딩
            var bgDataTask = Csvparser.ParseAsync<BGData>("MainBGData");
            var sfxDataTask = Csvparser.ParseAsync<SFXData>("MainSFXData");
            var characterDataTask = Csvparser.ParseAsync<MainCharacterData>("MainCharacterData");
            var characterImgDataTask = Csvparser.ParseAsync<MainCharacterImgData>("MainCharacterImgData");

            var (mainEventList, answerList, characterList ,bgList, sfxList, characterImgList) =
            await UniTask.WhenAll(mainEventTask, answerTask, characterDataTask , bgDataTask, sfxDataTask, characterImgDataTask);

            Debug.Log("모든 파일 로딩 완료");

            // 새로운 딕셔너리로 데이터를 구성합니다.
            mainEventDataDict = mainEventList.ToDictionary(e => e.ID, e => e);
            answerDataDict = answerList.ToDictionary(a => a.AnswerID, a => a);
            //룩업 데이터 구성
            CharacterDataDict = characterList.ToDictionary(e => e.Chr_ID, e => e);  
            bgDataDict = bgList.ToDictionary(bg => bg.BG_ID, bg => bg);
            sfxDataDict = sfxList.ToDictionary(sfx => sfx.SFX_ID, sfx => sfx);
            characterImgDataDict = characterImgList.ToDictionary(c => c.CharacterImg_ID, c => c);


            _isReady.TrySetResult(true);
            Debug.Log("모든 이벤트 데이터가 성공적으로 로드되었습니다.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 로드 실패: {ex.Message}");
            _isReady.TrySetException(ex);
        }
    }


    // 메인 이벤트 데이터 
    public async UniTask LoadMainEventDataAsync()
    {
        try
        {
            Debug.Log("메인 이벤트 데이터 로딩 시작");
            var mainEventTask = Csvparser.ParseAsync<MainEventData>("MainEventData11");
            var answerIDTask = Csvparser.ParseAsync<AnswerIDData>("AnswerID");

            var (mainEventList, answerIDList) = await UniTask.WhenAll(mainEventTask, answerIDTask);

            // Dictionary로 변환하여 메모리에 저장
            mainEventDict = mainEventList.ToDictionary(e => e.ID, e => e);
            answerIDDict = answerIDList.ToDictionary(a => a.AnswerID, a => a);

            Debug.Log("메인 이벤트 및 선택지 데이터 로딩 완료");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"메인 이벤트 데이터 로드 실패: {ex.Message}");
        }
    }

    private void LoadAllData()
    {
        mainEventDict = new Dictionary<int, MainEventData>();
        answerIDDict = new Dictionary<int, AnswerIDData>();
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

    // ID를 통해 메인 이벤트 데이터 구성 및 반환
    public NewMainEventData GetMainEventDataById(int eventID)
    {
        if (!mainEventDataDict.TryGetValue(eventID, out var rawData))
        {
            Debug.LogError($"[DataManager] ID {eventID}에 해당하는 이벤트 데이터를 찾을 수 없습니다.");
            return null;
        }

        var fullEventData = new NewMainEventData
        {
            id = rawData.ID,
            dialogue = rawData.Text_kr, 
        };

        //Chr_ID를 사용하여 캐릭터 이름 할당
        if(CharacterDataDict.TryGetValue(rawData.CharacterName, out var characterData))
        {
            fullEventData.characterData = characterData;
        }

        // BG_ID를 사용하여 BG 데이터를 직접 찾아서 할당합니다.
        if (bgDataDict.TryGetValue(rawData.BG_ID, out var bgData))
        {
            fullEventData.bgData = bgData;
        }

        // SFX_ID를 사용하여 SFX 데이터를 직접 찾아서 할당합니다.
        if (sfxDataDict.TryGetValue(rawData.SFX_ID, out var sfxData))
        {
            fullEventData.sfxData = sfxData;
        }

        // CharacterImg_ID를 사용하여 캐릭터 이미지 데이터를 직접 찾아서 할당합니다.
        if (characterImgDataDict.TryGetValue(rawData.CharacterImg_ID, out var characterImgData))
        {
            fullEventData.characterImgData = characterImgData;
        }

        // 왼쪽 및 오른쪽 선택지 구성
        fullEventData.leftChoice = CreateMainChoice(rawData.AnswerLeftID);
        fullEventData.rightChoice = CreateMainChoice(rawData.AnswerRightID);

        Debug.Log($"--- 이벤트 데이터 로드: ID {fullEventData.id} ---");
        Debug.Log($"Dialogue: {fullEventData.dialogue}");
        Debug.Log($"BG: {fullEventData.bgData.BGName}, Character: {fullEventData.characterData.Chr_Name} ({fullEventData.characterImgData.IMGName})");

        if (fullEventData.leftChoice != null)
        {
            Debug.Log($"- Left Choice: {fullEventData.leftChoice.choiceText}");
            Debug.Log($"  Next Event ID: {fullEventData.leftChoice.nextEventID}");
            Debug.Log($"  Outcome Changes: {fullEventData.leftChoice.outcome.parameterChanges.Count} 개");
        }

        if (fullEventData.rightChoice != null)
        {
            Debug.Log($"- Right Choice: {fullEventData.rightChoice.choiceText}");
            Debug.Log($"  Next Event ID: {fullEventData.rightChoice.nextEventID}");
            Debug.Log($"  Outcome Changes: {fullEventData.rightChoice.outcome.parameterChanges.Count} 개");
        }

        return fullEventData;
    }

    private NewEventChoice CreateMainChoice(int answerID)
    {
        var choice = new NewEventChoice();

        choice.outcome = new ChoiceOutcome
        {
            parameterChanges = new List<ParameterChange>()
        };

        if (answerDataDict.TryGetValue(answerID, out var answerData))
        {
            choice.choiceText = answerData.Text_KR;
            choice.nextEventID = answerData.NextTextID;
            //선택지 보상치 적용 단
        }
        else
        {
            choice.choiceText = "선택지 데이터를 찾을 수 없습니다.";
        }

        return choice;
    }



    // ID를 통해 파라미터 이벤트 데이터를 구성하고 반환하는 함수
    public EventData GetEventDataById(int eventID)
    {
        if (!eventDataDict.TryGetValue(eventID, out var rawData))
        {
            Debug.LogError($"[DataManager] ID {eventID}에 해당하는 이벤트 데이터를 찾을 수 없습니다.");
            return null;
        }


        // 분기 조건 확인: ChangeCondition 이벤트가 과거에 성공적으로 완료되었는지 여부
        bool isBranchTriggered = rawData.ConditionType == 1 && PlaythroughHistory.Instance.HasCompletedEvent(rawData.ChangeCondition);

        var fullEventData = new EventData
        {
            id = eventID,
            //eventName = $"Event_{eventID}" // 임시 이름
        };


        // 분기 여부에 따라 적절한 질문 ID 선택
        int questionId = isBranchTriggered ? rawData.AnotherEventQuestion : rawData.EventQuestion;
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
        else
        {
            Debug.LogError($"[DataManager] 질문 ID {questionId}에 해당하는 스트링 데이터를 찾을 수 없습니다.");
        }

        // 왼쪽 및 오른쪽 선택지 구성
        fullEventData.leftChoice = CreateChoice(rawData, isBranchTriggered, true);
        fullEventData.rightChoice = CreateChoice(rawData, isBranchTriggered, false);

        // 기타 데이터 설정
        if (roundTypeDict.TryGetValue(rawData.RoundType, out var roundTypeStr))
        {
            fullEventData.RoundType = roundTypeStr;
        }
        if (eventDict.TryGetValue(rawData.ConditionType, out var conditionTypeStr))
        {
            fullEventData.ConditionType = conditionTypeStr;
        }

        return fullEventData;
    }

    /// <summary>
    /// 단일 선택지(EventChoice) 객체를 생성합니다.
    /// </summary>
    /// <param name="rawData">CSV에서 읽어온 원본 이벤트 데이터</param>
    /// <param name="isBranch">분기된 경로의 선택지를 생성할지 여부</param>
    /// <param name="isLeft">왼쪽 선택지인지 여부</param>
    /// <returns>생성된 EventChoice 객체</returns>
    private EventChoice CreateChoice(ParameterEventData rawData, bool isBranch, bool isLeft)
    {
        var choice = new EventChoice();

        // 선택지 텍스트, 성공/실패 결과 ID, 보상 ID 등을 분기 및 좌/우에 따라 결정
        int choiceTextId      = isLeft ? (isBranch ? rawData.AnotherLeftString : rawData.LeftString)
                                       : (isBranch ? rawData.AnotherRightString : rawData.RightString);
        int successStringId   = isLeft ? (isBranch ? rawData.AnotherAcceptString1 : rawData.AcceptString1)
                                       : (isBranch ? rawData.AnotherAcceptString2 : rawData.AcceptString2);
        int failStringId      = isLeft ? (isBranch ? rawData.AnotherDenyString1 : rawData.DenyString1)
                                       : (isBranch ? rawData.AnotherDenyString2 : rawData.DenyString2);
        int successRewardId   = isLeft ? (isBranch ? rawData.AnotherAcceptReward1 : rawData.AcceptReward1)
                                       : (isBranch ? rawData.AnotherAcceptReward2 : rawData.AcceptReward2);
        int failRewardId      = isLeft ? (isBranch ? rawData.AnotherDenyReward1 : rawData.DenyReward1)
                                       : (isBranch ? rawData.AnotherDenyReward2 : rawData.DenyReward2);
        int needType          = isLeft ? (isBranch ? rawData.AnotherNeedType1 : rawData.NeedType1)
                                       : (isBranch ? rawData.AnotherNeedType2 : rawData.NeedType2);
        int needValue         = isLeft ? (isBranch ? rawData.AnotehrNeedValue1 : rawData.NeedValue1) // 'Anotehr' 오타 대응
                                       : (isBranch ? rawData.AnotherNeedValue2 : rawData.NeedValue2);

        // 선택지 텍스트 설정
        if (choiceTextDict.TryGetValue(choiceTextId, out var text))
        {
            choice.choiceText = text;
        }

        // 성공/실패 결과 Outcome 객체 생성
        choice.successOutcome = CreateOutcome(successStringId, successRewardId);
        choice.failOutcome = CreateOutcome(failStringId, failRewardId);

        // 성공 조건 객체 생성
        choice.condition = CreateSuccessCondition(needType, needValue);

        return choice;
    }

    /// <summary>
    /// 결과(Outcome) 객체를 생성합니다.
    /// </summary>
    private ChoiceOutcome CreateOutcome(int stringId, int rewardId)
    {
        var outcome = new ChoiceOutcome();
        if (eventStringDataDict.TryGetValue(stringId, out var outcomeString))
        {
            outcome.outcomeText = outcomeString.String_kr;
        }

        outcome.parameterChanges.AddRange(ConvertRewardsToParameterChanges(GetRewards(rewardId)));
        return outcome;

    }

    /// <summary>
    /// CSV 데이터 기반으로 다양한 성공 조건 객체를 생성합니다.
    /// </summary>
    private SuccessCondition CreateSuccessCondition(int needType, int needValue)
    {
        switch (needType)
        {
            case 0: // 없음
                return new GuaranteedSuccessCondition();
            case 1: // 정치력
                return new ParameterSuccessCondition { targetParameter = ParameterType.정치력, requiredValue = needValue };
            case 2: // 병력
                return new ParameterSuccessCondition { targetParameter = ParameterType.병력, requiredValue = needValue };
            case 3: // 물자
                return new ParameterSuccessCondition { targetParameter = ParameterType.물자, requiredValue = needValue };
            case 4: // 리더십
                return new ParameterSuccessCondition { targetParameter = ParameterType.리더십, requiredValue = needValue };
            default:
                Debug.LogWarning($"[DataManager] 알 수 없는 NeedType: {needType}. GuaranteedSuccessCondition으로 처리합니다.");
                return new GuaranteedSuccessCondition();
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

    // PlayerPrefs를 사용하여 회차 기록을 관리하는 클래스
    public class PlaythroughHistory
    {
        public static PlaythroughHistory Instance { get; private set; } = new PlaythroughHistory();

        private const string CompletedEventsKey = "CompletedEvents";
        private HashSet<int> completedEvents;

        // 생성자에서 데이터 로드
        private PlaythroughHistory()
        {
            Load();
        }

        private void Load()
        {
            completedEvents = new HashSet<int>();
            string savedEvents = PlayerPrefs.GetString(CompletedEventsKey, "");
            if (!string.IsNullOrEmpty(savedEvents))
            {
                foreach (var idStr in savedEvents.Split(','))
                {
                    if (int.TryParse(idStr, out int id))
                    {
                        completedEvents.Add(id);
                    }
                }
            }
            Debug.Log($"[PlaythroughHistory] 로드 완료. 완료된 이벤트 {completedEvents.Count}개");
        }

        private void Save()
        {
            string eventIds = string.Join(",", completedEvents);
            PlayerPrefs.SetString(CompletedEventsKey, eventIds);
            PlayerPrefs.Save(); // 확실한 저장을 위해 호출
            Debug.Log($"[PlaythroughHistory] 저장 완료. 현재 완료된 이벤트: {eventIds}");
        }

        public bool HasCompletedEvent(int eventId) => completedEvents.Contains(eventId);

        public void AddCompletedEvent(int eventId)
        {
            if (completedEvents.Add(eventId)) // 새로운 이벤트일 경우에만 저장
            {
                Save();
            }
        }

        public void ClearHistory()
        {
            completedEvents.Clear();
            PlayerPrefs.DeleteKey(CompletedEventsKey);
            PlayerPrefs.Save();
            Debug.Log("[PlaythroughHistory] 모든 기록이 삭제되었습니다.");
        }
    }
}