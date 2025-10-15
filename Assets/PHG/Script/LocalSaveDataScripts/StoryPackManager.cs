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
        allStoryPacks.Clear();

        // DataManager에서 실제 SubEvents를 기반으로 팩 번호를 구성합니다.
        var subEvents = DataManager.Instance != null ? DataManager.Instance.FullSubEvents : null;
        if (subEvents != null && subEvents.Count > 0)
        {
            var distinctPacks = subEvents
                .Select(e => e.SubStoryPac)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            // 해금 목록 로드 (없으면 기본으로 1000001만 해금)
            var settings = DataManager.Instance?.PlayerSettings;
            var unlocked = settings?.unlockedStoryPackIds ?? new List<int> { 1000001 };

            foreach (var pack in distinctPacks)
            {
                // 각 팩 ID에 맞는 제목 설정
                string packTitle = GetPackTitle(pack);
                allStoryPacks.Add(new StoryPackInfo
                {
                    packID = pack,
                    packName = $"Story:\n {packTitle}",
                    isUnlocked = unlocked.Contains(pack)
                });
            }
        }
        else
        {
            // 안전장치: 데이터 미로딩 시 최소 1개 제공 (임시)
            allStoryPacks.Add(new StoryPackInfo { packID = 1001, packName = "스토리팩 : 기본 스토리", isUnlocked = true });
        }
      
       // allStoryPacks.Add(new StoryPackInfo { packID = 2, packName = "기본 스토리 팩 2", isUnlocked = true });
       // allStoryPacks.Add(new StoryPackInfo { packID = 3, packName = "기본 스토리 팩 3", isUnlocked = true });
       // allStoryPacks.Add(new StoryPackInfo { packID = 4, packName = "미래의 스토리 팩 4", isUnlocked = false });
       // allStoryPacks.Add(new StoryPackInfo { packID = 5, packName = "미래의 스토리 팩 5", isUnlocked = false });
        // 새로운 팩이 추가될 때마다 여기에 추가
        // [안전장치] 선택 목록이 비어있다면 기본 해금 팩(1000001)을 최소 1개 자동 선택
        var selectedIDsSafe = DataManager.Instance?.PlayerSettings?.selectedSubEventPackIDs;
        if (selectedIDsSafe != null && selectedIDsSafe.Count == 0)
        {
            int defaultPackId = 1000001;
            // 기본 팩이 해금되어 있지 않다면 해금 상태에 관계없이 우선 선택하여 최소 보장
            selectedIDsSafe.Add(defaultPackId);
            DataManager.Instance.SaveSettings();
            Debug.Log($"[StoryPackManager] 선택 목록이 비어 기본 팩 {defaultPackId}을(를) 자동 선택했습니다.");
        }
    }

    // 팩 ID에 맞는 제목을 반환하는 함수
    private string GetPackTitle(int packID)
    {
        switch (packID)
        {
            case 1000001:
                return "기본 스토리팩";
            case 1000002:
                return "두번째 스토리팩";
            default:
                return $"팩 {packID}";
        }
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

        // 저장된 선택 목록 불러오기 (없으면 기본값 강제: 1000001)
        if (DataManager.Instance.PlayerSettings.selectedSubEventPackIDs == null || DataManager.Instance.PlayerSettings.selectedSubEventPackIDs.Count == 0)
        {
            DataManager.Instance.PlayerSettings.selectedSubEventPackIDs = new List<int> { 1000001 };
            DataManager.Instance.SaveSettings();
        }
        // 현재 존재하는 팩 목록으로 선택 ID를 정규화 (옛 포맷/없는 팩 제거)
        var existingPackIds = new HashSet<int>(allStoryPacks.Select(p => p.packID));
        var normalized = DataManager.Instance.PlayerSettings.selectedSubEventPackIDs
            .Select(id => id < 1000 ? id + 1000 : id) // 혹시 남아있을 구(1,2) 포맷 보정
            .Where(id => existingPackIds.Contains(id))
            .Distinct()
            .ToList();

        if (normalized.Count == 0)
        {
            normalized = new List<int> { 1000001 };
        }
        DataManager.Instance.PlayerSettings.selectedSubEventPackIDs = normalized;
        DataManager.Instance.SaveSettings();
        List<int> selectedIDs = DataManager.Instance.PlayerSettings.selectedSubEventPackIDs;

        // 모든 스토리 팩 정보 순회 (잠금팩은 잠금 오버레이로 표시)
        foreach (var packInfo in allStoryPacks)
        {
            // 프리팹으로부터 카드 생성
            GameObject cardObject = Instantiate(storyPackCardPrefab, cardGridParent);
            StoryPackCard card = cardObject.GetComponent<StoryPackCard>();

            if (card != null)
            {
                // 카드 초기화 (ID, 이름, 잠금 상태, 선택 상태, 클릭 시 호출할 함수 전달)
                bool isSelected = packInfo.isUnlocked && selectedIDs.Contains(packInfo.packID);
                card.Initialize(packInfo, isSelected, OnPackCardClicked);
                storyPackCards.Add(card);
            }
        }
    }

    // 개별 스토리 팩 카드가 클릭되었을 때 호출될 함수
    private void OnPackCardClicked(int packID)
    {
        List<int> selectedIDs = DataManager.Instance.PlayerSettings.selectedSubEventPackIDs;
        var card = storyPackCards.FirstOrDefault(c => c.PackID == packID);
        
        if (selectedIDs.Contains(packID))
        {
            // 최소 1개는 선택되어야 하므로, 1개만 남았을 때는 해제 불가
            if (selectedIDs.Count <= 1)
            {
                Debug.Log("[StoryPackManager] 최소 1개의 서브이벤트팩을 선택해야 합니다.");
                // UI는 선택된 상태로 유지 (변경하지 않음)
                return;
            }
            selectedIDs.Remove(packID); // 이미 선택된 상태면 리스트에서 제거
            // UI 업데이트
            if (card != null)
            {
                card.SetSelected(false);
            }
        }
        else
        {
            selectedIDs.Add(packID); // 선택되지 않은 상태면 리스트에 추가
            // UI 업데이트
            if (card != null)
            {
                card.SetSelected(true);
            }
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
        var selected = DataManager.Instance.PlayerSettings.selectedSubEventPackIDs;
        selected.Clear();

        // [최소 1개 유지] 해금된 카드 중 첫 번째를 즉시 재선택
        // 기본은 스토리팩 1000001을 재선택 (해금 여부 무관, 선택 최소 보장)
        int defaultPackId = 1000001;
        var defaultCard = storyPackCards.FirstOrDefault(c => c.PackID == defaultPackId) ?? storyPackCards.FirstOrDefault(c => c.IsUnlocked);
        if (defaultCard != null)
        {
            selected.Add(defaultCard.PackID);
        }

        // 모든 카드의 선택 UI 갱신
        foreach (var card in storyPackCards)
        {
            bool isSelected = defaultCard != null && card.PackID == defaultCard.PackID;
            card.SetSelected(isSelected);
        }

        SaveSelection();
        Debug.Log("[StoryPackManager] 전체 해제 요청을 최소 1개 선택 규칙에 맞게 보정했습니다.");
    }

    // 변경된 선택 상태를 파일에 저장
    private void SaveSelection()
    {
        Debug.Log($"[StoryPackManager] 저장 전 선택된 팩: {string.Join(", ", DataManager.Instance.PlayerSettings.selectedSubEventPackIDs)}");
        DataManager.Instance.SaveSettings();
        Debug.Log($"[StoryPackManager] 저장 후 선택된 팩: {string.Join(", ", DataManager.Instance.PlayerSettings.selectedSubEventPackIDs)}");
        Debug.Log("스토리 팩 선택 정보가 저장되었습니다: " + string.Join(", ", DataManager.Instance.PlayerSettings.selectedSubEventPackIDs));
    }

    // 현재 선택된 팩 ID 리스트를 안전하게 반환 (설정 메모리 기반)
    public List<int> GetSelectedPackIDs()
    {
        var ids = DataManager.Instance != null && DataManager.Instance.PlayerSettings != null
            ? DataManager.Instance.PlayerSettings.selectedSubEventPackIDs
            : null;
        return new List<int>(ids ?? new List<int>());
    }

    // 서브이벤트팩 선택 패널을 닫기 전에 검증하는 함수
    public bool CanClosePanel()
    {
        List<int> selectedIDs = DataManager.Instance.PlayerSettings.selectedSubEventPackIDs;
        
        if (selectedIDs == null || selectedIDs.Count == 0)
        {
            // GameManager의 Confirmation Panel을 사용하여 알림 표시
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ShowConfirmation("최소 1개의 서브이벤트팩을 선택해야 합니다.", () => { });
            }
            return false;
        }
        
        return true;
    }

    // 서브이벤트팩 선택을 검증하고 통과 시 패널을 닫는 함수
    public void ValidateAndClosePanel()
    {
        List<int> selectedIDs = DataManager.Instance.PlayerSettings.selectedSubEventPackIDs;
        
        Debug.Log($"[StoryPackManager] ValidateAndClosePanel - 선택된 팩: {(selectedIDs != null ? string.Join(", ", selectedIDs) : "null")}, 개수: {selectedIDs?.Count ?? 0}");
        
        if (selectedIDs == null || selectedIDs.Count == 0)
        {
            // GameManager의 Confirmation Panel을 사용하여 알림 표시 (창 닫기 방지)
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ShowConfirmation("최소 1개의 서브이벤트팩을 선택해야 합니다.", () => { });
            }
            Debug.Log("[StoryPackManager] 서브이벤트팩이 선택되지 않아 창을 닫을 수 없습니다.");
            return;
        }
        
        // 검증 통과 시 패널 직접 닫기 (검증 로직을 우회)
        var gameManagerForClose = FindObjectOfType<GameManager>();
        if (gameManagerForClose != null)
        {
            gameManagerForClose.CloseSubEventPanelDirectly();
            Debug.Log("[StoryPackManager] 서브이벤트팩 검증 통과 - 창을 닫습니다.");
        }
    }
}