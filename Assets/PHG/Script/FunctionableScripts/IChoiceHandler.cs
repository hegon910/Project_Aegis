using UnityEngine;

public interface IChoiceHandler
{
  //  bool CanMakeChoice { get; }
    void HandleChoice(bool isRightChoice);
    void PreviewAffectedParameters(bool isRightChoice);
    void ClearParameterPreview();
    void UpdateDimmer(float alpha);

    // [�߰�] ī�� �巡�� �� ������ �̸����� UI�� ������Ʈ�ϱ� ���� ���
    void UpdateChoicePreview(string text, Color color);
}