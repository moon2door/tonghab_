using UnityEngine;

public class CraneRailConstraint : MonoBehaviour
{
    [Header("Rail Reference Points")]
    public Transform railStart; // A 오브젝트 (시작점)
    public Transform railEnd;   // B 오브젝트 (끝점)

    [Header("Settings")]
    public bool fixPosition = true; // 켜고 끌 수 있는 옵션
    
    void LateUpdate()
    {
        if (!fixPosition || railStart == null || railEnd == null) return;

        Vector3 currentPos = transform.position;

        Vector3 correctedPos = GetProjectedPosition(railStart.position, railEnd.position, currentPos);

        transform.position = correctedPos;
    }

    private Vector3 GetProjectedPosition(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;

        float sqrLenAB = ab.sqrMagnitude;

        if (sqrLenAB == 0) return a; // A와 B가 같은 점일 경우 에러 방지

        float t = Vector3.Dot(ap, ab) / sqrLenAB;

        t = Mathf.Clamp01(t);

        return a + ab * t;
    }
}