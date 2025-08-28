using UnityEngine;

public class WarCardBinder : MonoBehaviour
{
    [SerializeField] WarTurnManager wturnMgr;
    [SerializeField] ChoiceCardSwipe wswipe;

    void WReset()
    {
        wturnMgr = FindObjectOfType<WarTurnManager>();
        wswipe = GetComponent<ChoiceCardSwipe>();
    }

    void WAwake()
    {
        // ��=����, ��=���, ��=��ų
        wswipe.onSwipeLeft.AddListener(() => wturnMgr.OnClick_PlayerAttack());
        wswipe.onSwipeRight.AddListener(() => wturnMgr.OnClick_PlayerDefend());
        // swipe.onSwipeUp.AddListener(() => turnMgr.OnClick_PlayerSkill(skillId));
    }
}