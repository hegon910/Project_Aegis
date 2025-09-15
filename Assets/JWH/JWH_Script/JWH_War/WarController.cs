using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public enum WarAction { None, Attack, Defend }

public class WarController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 100f;    // 초당 이동 속도
    [SerializeField] int forwardDist = 4;       // 전진 시 이동할 칸 수
    [SerializeField] int backwardDist = 1;      // 후퇴 시 이동할 칸 수
    [SerializeField] int direction = 1;         // 이동 방향 (플레이어: 1, 적: -1)

    [Header("Stats")]
    [SerializeField] private int maxHp = 5;
    [SerializeField] private int currentHp = 5;
    [SerializeField] private int attackPower = 1;

    WarGround ground;
    int currentIndex;
    Coroutine coMove;

    public int CurrentIndex => currentIndex;
    public int Direction => direction;
    public bool IsBusy => coMove != null;

    // --- 스탯 접근 프로퍼티 ---
    public int MaxHP { get => maxHp; set => maxHp = value; }
    public int CurrentHP { get => currentHp; set => currentHp = value; }
    public int AttackPower { get => attackPower; set => attackPower = value; }

    public void Init(WarGround ground, int startIndex)
    {
        this.ground = ground; //
        currentIndex = startIndex;
        GetComponent<RectTransform>().anchoredPosition = ground.GetGroundPos(currentIndex);
    }

    public void ResetState(WarGround ground, int startIndex)
    {
        currentHp = maxHp;
        Init(ground, startIndex);
        Debug.Log($"{gameObject.name}의 상태가 초기화되었습니다. (HP: {currentHp}, 위치: {startIndex})");
    }

    public void TakeDamage(int amount)
    {
        currentHp = Mathf.Max(0, currentHp - amount);
        Debug.Log($"{gameObject.name}이(가) {amount} 데미지를 받아 HP가 {currentHp}이(가) 됨");
    }

    public void DoAction(WarAction action, int extraForwardDist = 0)
    {
        int intendedIndex = currentIndex;
        switch (action)
        {
            case WarAction.Attack:
                intendedIndex = currentIndex + direction * (forwardDist + extraForwardDist);
                break;
            case WarAction.Defend:
                intendedIndex = currentIndex - direction * backwardDist;
                break;
        }
        int targetIndex = Mathf.Clamp(intendedIndex, 0, ground.LaneLength - 1);
        MoveTo(targetIndex);
    }

    public void CrushResult(int targetIndex) => MoveTo(targetIndex, true);

    void MoveTo(int targetIndex, bool isCrush = false)
    {
        if (coMove != null) StopCoroutine(coMove);
        coMove = StartCoroutine(Co_MoveTo(targetIndex, isCrush));
    }

    IEnumerator Co_MoveTo(int targetIndex, bool isCrush)
    {
        currentIndex = targetIndex;
        Vector2 targetPos = ground.GetGroundPos(targetIndex);
        RectTransform rect = GetComponent<RectTransform>();

        if (isCrush)
        {
            rect.DOAnchorPos(targetPos, 0.2f)
                .SetEase(Ease.OutQuad)
                .onComplete = () =>
                {
                    rect.DOShakePosition(0.1f, 5, 10, 90);
                };

            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            while (Vector2.Distance(rect.anchoredPosition, targetPos) > 1f)
            {
                rect.anchoredPosition = Vector2.MoveTowards(
                    rect.anchoredPosition, targetPos, moveSpeed * Time.deltaTime
                );
                yield return null;
            }
            rect.anchoredPosition = targetPos;
        }

        coMove = null;
    }
}