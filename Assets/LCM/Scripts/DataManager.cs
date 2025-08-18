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

    // ID를 통해 두 데이터를 합쳐서 반환하는 함수
    public EventData GetEventDataById(int id)
    {
        var stringData = StringDataList.FirstOrDefault(data => data.id == id);
        var rewardData = RewardDataList.FirstOrDefault(data => data.id == id);

        if (stringData != null && rewardData != null)
        {
            return new EventData(stringData, rewardData);
        }
        return null;
    }
}

[System.Serializable]
public class EventStringData
{
    public int id { get; set; }
    public string eventText { get; set; }
    public string leftChoiceText { get; set; }
    public string rightChoiceText { get; set; }
    public string successText { get; set; }
    public string failText { get; set; }
}

[System.Serializable]
public class EventRewardData
{
    public int id { get; set; }
    public int left_delta_politics { get; set; }
    public int left_delta_military { get; set; }
    public string left_success_threshold_politics { get; set; }
    public string left_fail_threshold_politics { get; set; }
    public int right_delta_politics { get; set; }
    public int right_delta_military { get; set; }
    public string right_success_threshold_politics { get; set; }
    public string right_fail_threshold_politics { get; set; }
}

public class EventData
{
    public int id;
    public string eventText;
    public string leftChoiceText;
    public string rightChoiceText;
    public string successText;
    public string failText;
    public int left_delta_politics;
    public int left_delta_military;
    public EventData(EventStringData stringData, EventRewardData rewardData)
    {
        this.id = stringData.id;
        this.eventText = stringData.eventText;
        this.leftChoiceText = stringData.leftChoiceText;
        this.rightChoiceText = stringData.rightChoiceText;
        this.successText = stringData.successText;
        this.failText = stringData.failText;

        this.left_delta_politics = rewardData.left_delta_politics;
        this.left_delta_military = rewardData.left_delta_military;
    }
}


