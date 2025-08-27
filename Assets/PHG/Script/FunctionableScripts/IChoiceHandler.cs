// IChoiceHandler.cs
// �� ������ ���� �������ּ���.

public interface IChoiceHandler
{
    // ���� Ȯ�� �� ȣ��
    void HandleChoice(bool isRightChoice);

    // ī�� �巡�� �� �Ķ���� �̸����� �� ȣ��
    void PreviewAffectedParameters(bool isRightChoice);

    // �̸����� ���� �� ȣ��
    void ClearParameterPreview();

    // ī�� �巡�� �� ȭ�� ��Ӱ� �� �� ȣ��
    void UpdateDimmer(float alpha);
}