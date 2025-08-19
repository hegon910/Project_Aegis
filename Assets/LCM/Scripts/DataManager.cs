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
    public EventCardData GetEventDataById(int id)
    {
        var stringData = StringDataList.FirstOrDefault(data => data.id == id);
        var rewardData = RewardDataList.FirstOrDefault(data => data.id == id);

        if (stringData != null && rewardData != null)
        {
            return new EventCardData(stringData, rewardData);
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

public class EventCardData
{
    public int id;
    public string eventText;
    public string leftChoiceText;
    public string rightChoiceText;
    public string leftSuccessText;
    public string leftFailText;
    public string rightSuccessText;
    public string rightFailText;

    public int left_success_delta_politics;
    public int left_success_delta_military;
    public int left_success_delta_supplies;
    public int left_success_delta_leadership;
    public int left_success_delta_war;
    public int left_success_delta_karma;
    public int left_fail_delta_politics;
    public int left_fail_delta_military;
    public int left_fail_delta_supplies;
    public int left_fail_delta_leadership;
    public int left_fail_delta_war;
    public int left_fail_delta_karma;

    public int right_success_delta_politics;
    public int right_success_delta_military;
    public int right_success_delta_supplies;
    public int right_success_delta_leadership;
    public int right_success_delta_war;
    public int right_success_delta_karma;
    public int right_fail_delta_politics;
    public int right_fail_delta_military;
    public int right_fail_delta_supplies;
    public int right_fail_delta_leadership;
    public int right_fail_delta_war;
    public int right_fail_delta_karma;
    public EventCardData(EventStringData stringData, EventRewardData rewardData)
    {
        this.id = stringData.id;
        this.eventText = stringData.eventText;
        this.leftChoiceText = stringData.leftChoiceText;
        this.rightChoiceText = stringData.rightChoiceText;
        this.leftSuccessText = stringData.leftSuccessText;
        this.leftFailText = stringData.leftFailText;
        this.rightSuccessText = stringData.rightSuccessText;
        this.rightFailText = stringData.rightFailText;

        this.left_success_delta_politics = rewardData.left_success_delta_politics;
        this.left_success_delta_military = rewardData.left_success_delta_military;
        this.left_success_delta_supplies = rewardData.left_success_delta_supplies;
        this.left_success_delta_leadership = rewardData.left_success_delta_leadership;
        this.left_success_delta_war = rewardData.left_success_delta_war;
        this.left_success_delta_karma = rewardData.left_success_delta_karma;
        this.left_fail_delta_politics = rewardData.left_fail_delta_politics;
        this.left_fail_delta_military = rewardData.left_fail_delta_military;
        this.left_fail_delta_supplies = rewardData.left_fail_delta_supplies;
        this.left_fail_delta_war = rewardData.left_fail_delta_war;
        this.left_fail_delta_karma = rewardData.left_fail_delta_karma;

        this.right_success_delta_politics = rewardData.right_success_delta_politics;
        this.right_success_delta_military = rewardData.right_success_delta_military;
        this.right_success_delta_supplies = rewardData.right_success_delta_supplies;
        this.right_success_delta_leadership = rewardData.right_success_delta_leadership;
        this.right_success_delta_war = rewardData.right_success_delta_war;
        this.right_success_delta_karma = rewardData.right_success_delta_karma;
        this.right_fail_delta_politics = rewardData.right_fail_delta_politics;
        this.right_fail_delta_military = rewardData.right_fail_delta_military;
        this.right_fail_delta_supplies = rewardData.right_fail_delta_supplies;
        this.right_fail_delta_war = rewardData.right_fail_delta_war;
        this.right_fail_delta_karma = rewardData.right_fail_delta_karma;


    }
}


