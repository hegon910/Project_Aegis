using UnityEngine;
using DG.Tweening;

public class UIFlowSimulator : MonoBehaviour
{
    [Header("��Ȳ ī��")]
    public SituationCardController situationCard;

    [Header("ī�� ���� ������ �ð�")]
    public float nextCardDelay = 0.3f;

    void Start()
    {
        // �����ϸ� ù ī�� �ٷ� �����ֱ�
        situationCard.Show("This is Test Situation");
    }

    // ������ �Ϸ�Ǹ� �� �Լ��� ȣ���մϴ�.
    public void ProceedToNext()
    {
        // 1. ���� ��Ȳ ī�� �����
        situationCard.Hide();

        // 2. ������ �ð� �ڿ� �� ī�� �����ֱ�
        DOVirtual.DelayedCall(nextCardDelay, () => {

            // ���⼭ ���߿� ���ο� �����͸� �޾ƿ� �ؽ�Ʈ�� �ٲٸ� �˴ϴ�.
            // ������ ���� �ؽ�Ʈ�� �ٽ� �����ݴϴ�.
            situationCard.Show("There is New Situation");
        });
    }
}
