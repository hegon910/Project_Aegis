using UnityEngine;

public interface IChoiceHandler
{
  //  bool CanMakeChoice { get; }
    void HandleChoice(bool isRightChoice);
    void PreviewAffectedParameters(bool isRightChoice);
    void ClearParameterPreview();
    void UpdateDimmer(float alpha);

    // [추가] 카드 드래그 시 선택지 미리보기 UI를 업데이트하기 위한 통로
    void UpdateChoicePreview(string text, Color color);
}