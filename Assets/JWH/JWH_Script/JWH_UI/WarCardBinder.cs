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
        // 좌=공격, 우=방어, 위=스킬
        wswipe.onSwipeLeft.AddListener(() => wturnMgr.OnClick_PlayerAttack());
        wswipe.onSwipeRight.AddListener(() => wturnMgr.OnClick_PlayerDefend());
        wswipe.onSwipeUp.AddListener(() => wturnMgr.OnClick_PlayerSkill());
    }
}