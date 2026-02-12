using UnityEngine;
using System.Collections;

public class CranePointConstraint : MonoBehaviour
{
    [Header("Settings")]
    public bool fixPosition = true; // 위치 고정 활성화 여부

    [Tooltip("게임 시작 후 이 시간(초) 동안은 위치 변경을 허용하고, 그 뒤에 고정합니다.")]
    public float lockDelay = 10.0f;

    [Header("References")]
    public CraneWireControl cwc;

    private Vector3 lockedPositionXZ;
    private bool isLocked = false; // 실제로 고정이 시작되었는지 여부
    public bool YLock = false; // [수정] Y축 고정 옵션

    public bool IsLocked => isLocked;

    private void Start()
    {
        if (cwc == null)
        {
            cwc = GetComponent<CraneWireControl>();
        }

        StartCoroutine(InitializeLock());
    }

    private IEnumerator InitializeLock()
    {
        yield return new WaitForSeconds(lockDelay);

        lockedPositionXZ = transform.position; // 이 시점의 Y값도 함께 저장됨
        isLocked = true;

        //Debug.Log($"[CranePointConstraint] {name} 위치 고정 완료: {lockedPositionXZ}");
    }

    void LateUpdate()
    {
        if (!isLocked || !fixPosition)
        {
            if (cwc != null) cwc.Wire__C(); // 와이어는 계속 업데이트
            return;
        }

        Vector3 currentPos = transform.position;

        float targetY = YLock ? lockedPositionXZ.y : currentPos.y;

        Vector3 correctedPos = new Vector3(lockedPositionXZ.x, targetY, lockedPositionXZ.z);

        transform.position = correctedPos;

        if (cwc != null)
        {
            cwc.Wire__C();
        }
    }

    public Vector3 GetLockedPosition()
    {
        if (!isLocked) return transform.position;

        // [수정] 외부에서 위치를 가져올 때도 YLock 상태 반영
        float returnY = YLock ? lockedPositionXZ.y : transform.position.y;

        return new Vector3(lockedPositionXZ.x, returnY, lockedPositionXZ.z);
    }

    public void ResetLockPosition()
    {
        lockedPositionXZ = transform.position;
        isLocked = true;
    }
}