using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

// 각 스토리 팩의 정보를 담는 간단한 구조체
public struct StoryPackInfo
{
    public int packID;
    public string packName;
    public bool isUnlocked;
}

public class StoryPackManager : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private Button selectAllButton;
    [SerializeField] private Button deselectAllButton;
    [SerializeField] private Transform cardGridParent; // 스토리 팩 카드가 생성될 부모 Grid 객체
    [SerializeField] private GameObject storyPackCardPrefab; // 개별 스토리 팩 카드 프리팹

    // UI 카드들을 관리하기 위한 리스트
    private List<StoryPackCard> storyPackCards = new List<StoryPackCard>();
    // 게임 내 모든 스토리 팩 정보 (현재는 임시 데이터)
    private List<StoryPackInfo> allStoryPacks = new List<StoryPackInfo>();

    void Start()
    {
        // 버튼에 기능 연결
        selectAllButton.onClick.AddListener(OnSelectAll);
        deselectAllButton.onClick.AddListener(OnDeselectAll);

        // 게임에 존재하는 모든 스토리 팩 정보를 설정합니다.
        // TODO: 향후 게임 데이터나 언락 시스템에서 이 정보를 가져오도록 수정해야 합니다.
        InitializeStoryPackData();

        // UI 생성 및 초기화
        PopulateGrid();
    }

    // 임시로 스토리 팩 데이터를 초기화하는 함수
    private void InitializeStoryPackData()
    {
        allStoryPacks.Add(new StoryPackInfo { packID = 1000001, packName = "기본 스토리 팩 1", isUnlocked = true });
       // allStoryPacks.Add(new StoryPackInfo { packID = 2, packName = "기본 스토리 팩 2", isUnlocked = true });
       // allStoryPacks.Add(new StoryPackInfo { packID = 3, packName = "기본 스토리 팩 3", isUnlocked = true });
       // allStoryPacks.Add(new StoryPackInfo { packID = 4, packName = "미래의 스토리 팩 4", isUnlocked = false });
       // allStoryPacks.Add(new StoryPackInfo { packID = 5, packName = "미래의 스토리 팩 5", isUnlocked = false });
        // 새로운 팩이 추가될 때마다 여기에 추가
    }

    // 스토리 팩 데이터 기반으로 UI 카드들을 생성하고 배치하는 함수
    private void PopulateGrid()
    {
        // 기존에 생성된 카드들 삭제
        foreach (Transform child in cardGridParent)
        {
            Destroy(child.gameObject);
        }
        storyPackCards.Clear();

        // 저장된 선택 목록 불러오기
        List<int> selectedIDs = DataManager.Instance.PlayerSettings.selectedSubEventPackIDs;

        // 모든 스토리 팩 정보 순회
        foreach (var packInfo in allStoryPacks)
        {
            // 프리팹으로부터 카드 생성
            GameObject cardObject = Instantiate(storyPackCardPrefab, cardGridParent);
            StoryPackCard card = cardObject.GetComponent<StoryPackCard>();

            if (card != null)
            {
                // 카드 초기화 (ID, 이름, 잠금 상태, 선택 상태, 클릭 시 호출할 함수 전달)
                bool isSelected = selectedIDs.Contains(packInfo.packID);
                card.Initialize(packInfo, isSelected, OnPackCardClicked);
                storyPackCards.Add(card);
            }
        }
    }

    // 개별 스토리 팩 카드가 클릭되었을 때 호출될 함수
    private void OnPackCardClicked(int packID)
    {
        List<int> selectedIDs = DataManager.Instance.PlayerSettings.selectedSubEventPackIDs;

        if (selectedIDs.Contains(packID))
        {
            selectedIDs.Remove(packID); // 이미 선택된 상태면 리스트에서 제거
        }
        else
        {
            selectedIDs.Add(packID); // 선택되지 않은 상태면 리스트에 추가
        }

        // 변경사항 저장
        SaveSelection();
    }

    // '전체 선택' 버튼 클릭 시
    private void OnSelectAll()
    {
        List<int> selectedIDs = DataManager.Instance.PlayerSettings.selectedSubEventPackIDs;
        selectedIDs.Clear();

        // 잠금 해제된 모든 팩을 선택 목록에 추가
        foreach (var card in storyPackCards)
        {
            if (card.IsUnlocked)
            {
                selectedIDs.Add(card.PackID);
                card.SetSelected(true); // UI 즉시 업데이트
            }
        }

        SaveSelection();
    }

    // '전체 해제' 버튼 클릭 시
    private void OnDeselectAll()
    {
        DataManager.Instance.PlayerSettings.selectedSubEventPackIDs.Clear();

        // 모든 카드의 선택 UI 해제
        foreach (var card in storyPackCards)
        {
            card.SetSelected(false);
        }

        SaveSelection();
    }

    // 변경된 선택 상태를 파일에 저장
    private void SaveSelection()
    {
        DataManager.Instance.SaveSettings();
        Debug.Log("스토리 팩 선택 정보가 저장되었습니다: " + string.Join(", ", DataManager.Instance.PlayerSettings.selectedSubEventPackIDs));
    }
}