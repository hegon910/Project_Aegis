// ChoiceCardBinder.cs

using UnityEngine;

public class ChoiceCardBinder : MonoBehaviour
{
    [SerializeField] BattleTurnManager turnMgr;
    [SerializeField] ChoiceCardSwipe swipe;

    void Reset()
    {
        turnMgr = FindObjectOfType<BattleTurnManager>();
        swipe = GetComponent<ChoiceCardSwipe>();
    }

    void Awake()
    {
        // 좌=공격, 우=방어, 위=스킬
        swipe.onSwipeLeft.AddListener(() => turnMgr.OnClick_PlayerAttack());
        swipe.onSwipeRight.AddListener(() => turnMgr.OnClick_PlayerDefend());

        // TODO: 추후 스킬 시스템이 구현되면 아래 주석을 해제하고, 사용할 스킬 정보를 넘겨주세요.
        // swipe.onSwipeUp.AddListener(() => turnMgr.OnClick_PlayerSkill(skillId));
    }
}