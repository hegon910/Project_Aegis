// IChoiceHandler.cs
// 이 파일을 새로 생성해주세요.

public interface IChoiceHandler
{
    // 선택 확정 시 호출
    void HandleChoice(bool isRightChoice);

    // 카드 드래그 중 파라미터 미리보기 시 호출
    void PreviewAffectedParameters(bool isRightChoice);

    // 미리보기 해제 시 호출
    void ClearParameterPreview();

    // 카드 드래그 중 화면 어둡게 할 때 호출
    void UpdateDimmer(float alpha);
}