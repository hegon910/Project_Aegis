using UnityEngine;
using DG.Tweening;

public class UIFlowSimulator : MonoBehaviour
{
    [Header("상황 카드")]
    public SituationCardController situationCard;

    [Header("카드 등장 딜레이 시간")]
    public float nextCardDelay = 0.3f;

    void Start()
    {
        // 시작하면 첫 카드 바로 보여주기
        situationCard.Show("This is Test Situation");
    }

    // 선택이 완료되면 이 함수를 호출합니다.
    public void ProceedToNext()
    {
        // 1. 현재 상황 카드 숨기기
        situationCard.Hide();

        // 2. 정해진 시간 뒤에 새 카드 보여주기
        DOVirtual.DelayedCall(nextCardDelay, () => {

            // 여기서 나중에 새로운 데이터를 받아와 텍스트를 바꾸면 됩니다.
            // 지금은 같은 텍스트로 다시 보여줍니다.
            situationCard.Show("There is New Situation");
        });
    }
}
