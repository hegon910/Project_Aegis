using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleReplayPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject replayCanvas;
    [SerializeField] private GameObject replayPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect eventScrollRect;
    [SerializeField] private Transform eventListParent;
    [SerializeField] private TextMeshProUGUI statusText;


    [Header("Prefab")]
    [SerializeField] private GameObject recordButtonPrefab;

    [Header("Detail Panel")]
    [SerializeField] private ReplayDetailHandler detailHandler;

    private SimpleEventHistoryManager historyManager;

    private void Start()
    {
        historyManager = SimpleEventHistoryManager.Instance;

        if (replayCanvas != null)
            replayCanvas.SetActive(false);

        if (replayPanel != null)
            replayPanel.SetActive(false);

        if (openButton != null)
            openButton.onClick.AddListener(OpenReplayPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseReplayPanel);

        UpdateStatusText();
    }

    public void OpenReplayPanel()
    {
        if (replayCanvas != null)
        {
            replayCanvas.SetActive(true);
            RefreshPlaythroughList();
        }
    }

    public void CloseReplayPanel()
    {
        if (replayCanvas != null)
        {
            replayCanvas.SetActive(false);
        }
    }

    private void RefreshPlaythroughList()
    {
        if (historyManager == null || eventListParent == null) return;

        foreach (Transform child in eventListParent)
        {
            Destroy(child.gameObject);
        }

        List<GamePlaythroughRecord> records = historyManager.GetPlaythroughHistory();

        // *** 추가된 코드: 로그를 통해 리스트의 개수 확인 ***
        Debug.Log($"[SimpleReplayPanel] 불러온 플레이 기록 수: {records.Count}");

        if (statusText != null)
        {
            statusText.text = $"총 {records.Count}개의 플레이 기록";
        }

        for (int i = records.Count - 1; i >= 0; i--)
        {
            CreatePlaythroughItem(records[i], records.Count - 1 - i);
        }
    }

    private void CreatePlaythroughItem(GamePlaythroughRecord record, int index)
    {
        GameObject recordItem = Instantiate(recordButtonPrefab, eventListParent);
        recordItem.name = $"RecordItem_{record.playDate}";

        var rectTransform = recordItem.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(1000f, 100f);
        }

        TextMeshProUGUI recordText = recordItem.GetComponentInChildren<TextMeshProUGUI>();

        if (recordText != null)
        {
            recordText.text = $"{record.playDate} | {record.playDuration} | {record.playthroughCount}회차 {record.outcome}";
        }
        else
        {
            Debug.LogError($"[SimpleReplayPanel] 버튼 프리팹({recordButtonPrefab.name})에서 TextMeshProUGUI를 찾을 수 없습니다.");
        }

        Button button = recordItem.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnRecordClicked(record));
        }
    }

    private void OnRecordClicked(GamePlaythroughRecord record)
    {
        if (detailHandler != null) // ReplayDetailHandler의 ShowDetails 호출
        {
            detailHandler.ShowDetails(record);
        }
        else
        {
            Debug.LogError("[SimpleReplayPanel] ReplayDetailHandler가 연결되지 않았습니다!");
        }
    }


    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            if (historyManager != null)
            {
                var records = historyManager.GetPlaythroughHistory();
                statusText.text = $"총 {records.Count}개의 플레이 기록";
            }
            else
            {
                statusText.text = "이벤트 기록 시스템을 찾을 수 없습니다.";
            }
        }
    }

    private void Update()
    {
        UpdateStatusText();
    }
}