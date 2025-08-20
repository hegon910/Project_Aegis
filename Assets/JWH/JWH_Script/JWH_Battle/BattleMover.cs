using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMover : MonoBehaviour
{
    [SerializeField] BattleGround ground;
    [SerializeField] int currentIndex = 0; // ���� ĭ
    [SerializeField] float moveSpeed = 5f; // �̵� �ӵ�
    //[SerializeField] float verticalOffset = 0f;//ĳ���� ���� �ø���

    bool isMoving = false;
    Vector3 targetPos;

    public bool IsMoving => isMoving;

    void Start()
    {
        if (ground == null) ground = FindObjectOfType<BattleGround>();
        transform.position = ground.GetGroundPos(currentIndex);
        //transform.position = GroundTop(currentIndex);
    }

    public bool TryMoveBy(int step)
    {
        if (isMoving) return false; // �̵� ���̸� �Է� ����

        int nextIndex = Mathf.Clamp(currentIndex + step, 0, ground.LaneLength - 1);
        if (nextIndex == currentIndex) return false;

        currentIndex = nextIndex;
        targetPos = ground.GetGroundPos(currentIndex);
        StartCoroutine(MoveToTarget());
        return true;
    }

    //Vector3 GroundTop(int index)//ĳ���� ���� �ø���
    //{
    //    Vector3 center = ground.GetGroundPos(index);
    //    return center + new Vector3(0f, ground.CellSize * 0.5f + verticalOffset, 0f);
    //}

    System.Collections.IEnumerator MoveToTarget()
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
        isMoving = false;
    }
}
