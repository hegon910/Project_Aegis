using UnityEngine;
using UnityEngine.UI;

// �� ��ũ��Ʈ�� Canvas Scaler�� �ִ� GameObject�� �پ�� �ϹǷ�,
// �ڵ����� �ش� ������Ʈ�� �ִ��� Ȯ���ϰ� ������ �߰����ݴϴ�.
[RequireComponent(typeof(CanvasScaler))]
public class DynamicCanvasMatcher : MonoBehaviour
{
    // �����̳ʰ� �۾��� ���� �ػ�
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);

    private CanvasScaler canvasScaler;

    // ȭ�� ũ�� ������ �����ϱ� ���� ����
    private int lastScreenWidth = 0;
    private int lastScreenHeight = 0;

    void Awake()
    {
        // CanvasScaler ������Ʈ�� ������
        canvasScaler = GetComponent<CanvasScaler>();
        // ������ �� �� �� ����
        AdjustMatchValue();
    }

    void Update()
    {
        // ������ ��� �ǽð����� �ػ󵵰� ����� ���� ���
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            AdjustMatchValue();
        }
    }

    private void AdjustMatchValue()
    {
        // ���� ȭ�� ũ�� ����
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        // ���� ȭ�� ���� ���
        float referenceAspect = referenceResolution.x / referenceResolution.y;
        // ���� ȭ�� ���� ���
        float currentAspect = (float)Screen.width / (float)Screen.height;

        // ���� ȭ���� ���غ��� ���η� �дٸ� (��: ����3, �º� ���� ���)
        if (currentAspect > referenceAspect)
        {
            // Match ���� 1 (Height ����)�� ����
            canvasScaler.matchWidthOrHeight = 1f;
        }
        else // ���ذ� ���ų� ���η� ��ٸ� (��: �Ϲ� ����Ʈ��)
        {
            // Match ���� 0.5 (�߰�)�� ����
            canvasScaler.matchWidthOrHeight = 0.5f;
        }
    }
}