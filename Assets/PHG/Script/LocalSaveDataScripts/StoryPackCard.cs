using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryPackCard : MonoBehaviour
{
    [Header("카드 UI 요소 연결")]
    [SerializeField] private Button cardButton;
    [SerializeField] private GameObject checkmarkIcon; // 선택 시 활성화될 체크 아이콘
    [SerializeField] private GameObject lockOverlay;   // 잠금 시 활성화될 자물쇠 오버레이
    [SerializeField] private TextMeshProUGUI packNameText; // 팩 이름을 표시할 텍스트

    public int PackID { get; private set; }
    public bool IsUnlocked { get; private set; }

    private Action<int> onClickCallback;

    private void Awake()
    {
        cardButton.onClick.AddListener(OnCardClick);
    }

    // StoryPackManager가 카드를 생성할 때 호출하여 초기 정보를 설정하는 함수
    public void Initialize(StoryPackInfo info, bool isSelected, Action<int> callback)
    {
        this.PackID = info.packID;
        this.IsUnlocked = info.isUnlocked;
        this.onClickCallback = callback;

        if (packNameText != null)
        {
            packNameText.text = info.packName;
        }

        SetLocked(!info.isUnlocked);
        SetSelected(isSelected);
    }

    // 카드를 클릭했을 때의 동작
    private void OnCardClick()
    {
        // 잠겨있지 않을 때만 동작
        if (IsUnlocked)
        {
            // 내가 선택되었음을 Manager에게 알림
            onClickCallback?.Invoke(PackID);
            // UI 업데이트는 Manager에서 처리하도록 함
        }
    }

    // 외부에서 호출하여 선택 상태 UI를 변경하는 함수
    public void SetSelected(bool selected)
    {
        if (checkmarkIcon != null)
        {
            checkmarkIcon.SetActive(selected);
        }
    }

    // 외부에서 호출하여 잠금 상태 UI를 변경하는 함수
    public void SetLocked(bool locked)
    {
        IsUnlocked = !locked;
        if (lockOverlay != null)
        {
            lockOverlay.SetActive(locked);
        }
        cardButton.interactable = !locked; // 잠겼으면 버튼 비활성화
    }
}