using UnityEngine;
using DG.Tweening;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public enum WarAction { None, Attack, Defend }

public class WarController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 100f;
    [SerializeField] int forwardDist = 4;
    [SerializeField] int backwardDist = 1;
    [SerializeField] int direction = 1;

    [Header("Stats")]
    [SerializeField] private int maxHp = 5;
    [SerializeField] private int currentHp = 5;
    [SerializeField] private int attackPower = 1;

    WarGround ground;
    int currentIndex;

    

    // 이동 취소를 위한 CancellationTokenSource
    private CancellationTokenSource moveCts;

    public int CurrentIndex => currentIndex;
    public int Direction => direction;

    public int MaxHP { get => maxHp; set => maxHp = value; }
    public int CurrentHP { get => currentHp; set => currentHp = value; }
    public int AttackPower { get => attackPower; set => attackPower = value; }

    public void Init(WarGround ground, int startIndex)
    {
        this.ground = ground;
        currentIndex = startIndex;
        GetComponent<RectTransform>().anchoredPosition = ground.GetGroundPos(currentIndex);
    }

    public void ResetState(WarGround ground, int startIndex)
    {
        currentHp = maxHp;
        Init(ground, startIndex);
    }

    public void TakeDamage(int amount)
    {
        currentHp = Mathf.Max(0, currentHp - amount);
    }

    public async UniTask DoActionAsync(WarAction action, int extraForwardDist = 0)
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
        await MoveToAsync(targetIndex);
    }

    public async UniTask CrushResultAsync(int targetIndex)
    {
        await MoveToAsync(targetIndex, true);
    }

    public void StopMovement()
    {
        moveCts?.Cancel();
    }

    private async UniTask MoveToAsync(int targetIndex, bool isCrush = false)
    {
        moveCts?.Cancel();
        moveCts = new CancellationTokenSource();
        var token = moveCts.Token;

        RectTransform rect = GetComponent<RectTransform>();

        try
        {
            if (isCrush)
            {
                Vector2 targetPos = ground.GetGroundPos(targetIndex);
                currentIndex = targetIndex;
                await rect.DOAnchorPos(targetPos, 0.2f).SetEase(Ease.OutQuad).ToUniTask(cancellationToken: token);
                await rect.DOShakePosition(0.1f, 5, 10, 90).ToUniTask(cancellationToken: token);
            }
            else
            {
                Vector2 targetPos = ground.GetGroundPos(targetIndex);
                while (Vector2.Distance(rect.anchoredPosition, targetPos) > 1f)
                {
                    token.ThrowIfCancellationRequested(); // 작업 취소 확인
                    rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, targetPos, moveSpeed * Time.deltaTime);

                    float currentX = rect.anchoredPosition.x;
                    float totalGridWidth = ground.LaneLength * ground.CellSize;
                    float remainingSpace = rect.rect.width - totalGridWidth;
                    float leftEdgeX = -rect.rect.width * rect.pivot.x;
                    float centeredStartX = leftEdgeX + (remainingSpace / 2);

                    currentIndex = Mathf.Clamp(Mathf.FloorToInt((currentX - centeredStartX) / ground.CellSize), 0, ground.LaneLength - 1);

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
                rect.anchoredPosition = targetPos;
                currentIndex = targetIndex;
            }
        }
        catch (OperationCanceledException)
        {
            // 이동이 중단되면 여기로 옵니다.
        }
        finally
        {
            moveCts?.Dispose();
            moveCts = null;
        }
    }


}