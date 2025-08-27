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
        // ��=����, ��=���, ��=��ų
        swipe.onSwipeLeft.AddListener(() => turnMgr.OnClick_PlayerAttack());
        swipe.onSwipeRight.AddListener(() => turnMgr.OnClick_PlayerDefend());
        // swipe.onSwipeUp.AddListener(() => turnMgr.OnClick_PlayerSkill(skillId));
    }
}
