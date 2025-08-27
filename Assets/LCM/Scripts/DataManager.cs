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

    public List<EventStringData> StringDataList { get; private set; }
    public List<EventRewardData> RewardDataList { get; private set; }
    public List<SubEventData> SubEvents {  get; private set; }

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

                string endText = string.Empty;
                endTextLookup.TryGetValue(listData.End_Num, out endText);

                // SubEventActionData의 CharacterName(정수)을 사용해 캐릭터 이름 조회
                string characterName = string.Empty;
                characterNameLookup.TryGetValue(actionData.CharacterName, out characterName);

                // 4. 새로운 SubEventData 객체 생성 및 값 채우기
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
            foreach (var subEvent in SubEvents)
            {
                Debug.Log($"[Index: {subEvent.Index}] " +
                          $"질문: {subEvent.QuestionString_kr} | " +
                          $"캐릭터: {subEvent.CharacterName} | " +
                          $"선택지1: {subEvent.LeftSelectString} | " +
                          $"선택지2: {subEvent.RightSelectString} | " +
                          $"엔딩텍스트: {subEvent.End_Text}");
            }
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

        EventData eventData = ScriptableObject.CreateInstance<EventData>();

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