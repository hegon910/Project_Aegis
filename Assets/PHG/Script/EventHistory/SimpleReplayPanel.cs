using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 간단한 회상 패널 UI
/// </summary>
public class SimpleReplayPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject replayCanvas;           // 회상용 캔버스
    [SerializeField] private GameObject replayPanel;           // 회상 패널
    [SerializeField] private Button openButton;                // 회상 열기 버튼
    [SerializeField] private Button closeButton;               // 회상 닫기 버튼
    [SerializeField] private ScrollRect eventScrollRect;       // 이벤트 스크롤
    [SerializeField] private Transform eventListParent;        // 이벤트 리스트 부모
    [SerializeField] private TextMeshProUGUI statusText;       // 상태 텍스트
    [SerializeField] private Button clearButton;               // 기록 초기화 버튼

    [Header("Font Settings")]
    [SerializeField] private TMPro.TMP_FontAsset koreanFont;   // 한글 폰트

    private SimpleEventHistoryManager historyManager;

    private void Start()
    {
        historyManager = SimpleEventHistoryManager.Instance;
        
        // UI 초기화
        if (replayCanvas != null)
            replayCanvas.SetActive(false);
        
        if (replayPanel != null)
            replayPanel.SetActive(false);

        // 버튼 이벤트 설정
        if (openButton != null)
            openButton.onClick.AddListener(OpenReplayPanel);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseReplayPanel);
        
        if (clearButton != null)
            clearButton.onClick.AddListener(ClearHistory);

        UpdateStatusText();
    }

    /// <summary>
    /// 회상 패널 열기
    /// </summary>
    public void OpenReplayPanel()
    {
        if (replayCanvas != null)
        {
            replayCanvas.SetActive(true);
            RefreshEventList();
        }
    }

    /// <summary>
    /// 회상 패널 닫기
    /// </summary>
    public void CloseReplayPanel()
    {
        if (replayCanvas != null)
        {
            replayCanvas.SetActive(false);
        }
    }

    /// <summary>
    /// 이벤트 리스트 새로고침
    /// </summary>
    private void RefreshEventList()
    {
        if (historyManager == null || eventListParent == null) return;

        // 기존 아이템들 제거
        foreach (Transform child in eventListParent)
        {
            Destroy(child.gameObject);
        }

        // 이벤트 기록 가져오기
        var events = historyManager.GetEventHistory();
        
        // 상태 텍스트 업데이트
        if (statusText != null)
        {
            statusText.text = $"총 {events.Count}개의 이벤트 기록";
        }

        // 이벤트 아이템들 생성
        for (int i = 0; i < events.Count; i++)
        {
            CreateEventItem(events[i], i);
        }
    }

    /// <summary>
    /// 이벤트 아이템 생성
    /// </summary>
    private void CreateEventItem(SimpleEventRecord eventRecord, int index)
    {
        // 이벤트 아이템 생성
        var eventItem = new GameObject($"EventItem_{index}");
        eventItem.transform.SetParent(eventListParent);

        // RectTransform 설정
        var rectTransform = eventItem.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.offsetMin = new Vector2(10, -80);
        rectTransform.offsetMax = new Vector2(-10, -10);
        rectTransform.anchoredPosition = new Vector2(0, -90 * index);

        // 배경 이미지
        var image = eventItem.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // 지문 텍스트 (제목으로 사용)
        var titleText = CreateText("TitleText", eventItem.transform, 
            eventRecord.isEndingMemoriar ? "[메모리어] " + eventRecord.dialogue : eventRecord.dialogue, 
            16, eventRecord.isEndingMemoriar ? Color.magenta : Color.white, 
            new Vector2(0, 0.6f), new Vector2(1, 1));

        // 이벤트 ID와 타입 정보
        var descText = CreateText("DescText", eventItem.transform, 
            $"이벤트 ID: {eventRecord.eventId} | 타입: {eventRecord.eventType}", 
            12, Color.gray, new Vector2(0, 0.3f), new Vector2(1, 0.6f));

        // 선택한 답변 텍스트
        var choiceText = CreateText("ChoiceText", eventItem.transform, $"선택: {eventRecord.selectedChoice}", 12, Color.yellow, new Vector2(0, 0), new Vector2(1, 0.3f));

        // 메타 정보 텍스트
        var metaText = CreateText("MetaText", eventItem.transform, 
            $"챕터 {eventRecord.chapter} | {eventRecord.eventType} | {eventRecord.timestamp}", 
            10, Color.cyan, new Vector2(0, 0), new Vector2(1, 0.1f));

        // 버튼 기능 추가
        var button = eventItem.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => OnEventClicked(eventRecord));
    }

    /// <summary>
    /// 텍스트 생성 헬퍼
    /// </summary>
    private TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        
        var rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = new Vector2(10, 5);
        rectTransform.offsetMax = new Vector2(-10, -5);

        var textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.font = koreanFont;
        textComponent.alignment = TextAlignmentOptions.Left;

        return textComponent;
    }

    /// <summary>
    /// 이벤트 클릭 처리
    /// </summary>
    private void OnEventClicked(SimpleEventRecord eventRecord)
    {
        Debug.Log($"[SimpleReplayPanel] 이벤트 클릭: {eventRecord.dialogue} (메모리어: {eventRecord.isEndingMemoriar})");
        // 여기서 이벤트 상세 정보를 표시할 수 있습니다.
    }

    /// <summary>
    /// 기록 초기화
    /// </summary>
    private void ClearHistory()
    {
        if (historyManager != null)
        {
            historyManager.ClearHistory();
            RefreshEventList();
        }
    }

    /// <summary>
    /// 상태 텍스트 업데이트
    /// </summary>
    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            if (historyManager != null)
            {
                var events = historyManager.GetEventHistory();
                statusText.text = $"총 {events.Count}개의 이벤트 기록";
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


