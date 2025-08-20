using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public List<EventStringData> StringDataList { get; private set; }
    public List<EventRewardData> RewardDataList { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadAllData()
    {
        TextAsset stringCsv = Resources.Load<TextAsset>("eventStringData");
        StringDataList = Csvparser.Parse<EventStringData>(stringCsv);

        TextAsset rewardCsv = Resources.Load<TextAsset>("eventRewardData");
        RewardDataList = Csvparser.Parse<EventRewardData>(rewardCsv);
    }

    // ID를 통해 두 데이터를 합쳐서 새로운 구조의 EventData로 반환하는 함수
    public EventData GetEventDataById(int id)
    {
        var stringData = StringDataList.FirstOrDefault(data => data.id == id);
        var rewardData = RewardDataList.FirstOrDefault(data => data.id == id);

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
        if (rewardData.left_success_delta_war != 0) eventData.leftChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.전세, valueChange = rewardData.left_success_delta_war });
        if (rewardData.left_success_delta_karma != 0) eventData.leftChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.카르마, valueChange = rewardData.left_success_delta_karma });

        // 왼쪽 실패 효과
        if (rewardData.left_fail_delta_politics != 0) eventData.leftChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.정치력, valueChange = rewardData.left_fail_delta_politics });
        if (rewardData.left_fail_delta_military != 0) eventData.leftChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.병력, valueChange = rewardData.left_fail_delta_military });
        if (rewardData.left_fail_delta_supplies != 0) eventData.leftChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.물자, valueChange = rewardData.left_fail_delta_supplies });
        if (rewardData.left_fail_delta_leadership != 0) eventData.leftChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.리더십, valueChange = rewardData.left_fail_delta_leadership });
        if (rewardData.left_fail_delta_war != 0) eventData.leftChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.전세, valueChange = rewardData.left_fail_delta_war });
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
        if (rewardData.right_success_delta_war != 0) eventData.rightChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.전세, valueChange = rewardData.right_success_delta_war });
        if (rewardData.right_success_delta_karma != 0) eventData.rightChoice.successOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.카르마, valueChange = rewardData.right_success_delta_karma });

        // 오른쪽 실패 효과
        if (rewardData.right_fail_delta_politics != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.정치력, valueChange = rewardData.right_fail_delta_politics });
        if (rewardData.right_fail_delta_military != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.병력, valueChange = rewardData.right_fail_delta_military });
        if (rewardData.right_fail_delta_supplies != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.물자, valueChange = rewardData.right_fail_delta_supplies });
        if (rewardData.right_fail_delta_leadership != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.리더십, valueChange = rewardData.right_fail_delta_leadership });
        if (rewardData.right_fail_delta_war != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.전세, valueChange = rewardData.right_fail_delta_war });
        if (rewardData.right_fail_delta_karma != 0) eventData.rightChoice.failOutcome.parameterChanges.Add(new ParameterChange { parameterType = ParameterType.카르마, valueChange = rewardData.right_fail_delta_karma });

        return eventData;
    }
}

// Csvparser를 위해 필요한 데이터 클래스들
[System.Serializable]
public class EventStringData
{
    public int id { get; set; }
    public string eventText { get; set; }
    public string leftChoiceText { get; set; }
    public string rightChoiceText { get; set; }
    public string leftSuccessText { get; set; }
    public string leftFailText { get; set; }
    public string rightSuccessText { get; set; }
    public string rightFailText { get; set; }
}

[System.Serializable]
public class EventRewardData
{
    public int id { get; set; }
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