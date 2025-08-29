using UnityEngine;

public class WarCardBinder : MonoBehaviour
{
    [SerializeField] WarTurnManager wturnMgr;
    [SerializeField] ChoiceCardSwipe wswipe;

    void Reset()
    {
        wturnMgr = FindObjectOfType<WarTurnManager>();
        wswipe = GetComponent<ChoiceCardSwipe>();
    }

    void Awake()
    {
        // ��=����, ��=���, ��=��ų
        wswipe.onSwipeLeft.AddListener(() => wturnMgr.OnClick_PlayerAttack());
        wswipe.onSwipeRight.AddListener(() => wturnMgr.OnClick_PlayerDefend());
        wswipe.onSwipeUp.AddListener(() => wturnMgr.OnClick_PlayerSkill());
    }
}