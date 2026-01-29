using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraneWireControl : MonoBehaviour
{
    public Material material;
    [Header("Wire Positions")]
    public Transform position1;
    public Transform position2;

    [Header("Settings")]
    public int numWire = 1; // 0으로 설정되는 것 방지
    public float wireWidth = 0.01f;
    public float width1;
    public float width2;

    // 최적화를 위해 GameObject 대신 LineRenderer를 직접 캐싱
    private List<LineRenderer> listLineRenderers = new List<LineRenderer>();

    private CraneRailConstraint crc;

    // Start is called before the first frame update
    void Start()
    {
        crc = GetComponent<CraneRailConstraint>();

        // 리스트 초기화
        listLineRenderers.Clear();

        // 안전 장치: numWire가 0보다 작으면 1로 설정
        if (numWire < 1) numWire = 1;

        for (int i = 0; i < numWire; i++)
        {
            GameObject obj = new GameObject("wire_" + i); // 디버깅 편의를 위해 이름 지정
            obj.transform.SetParent(transform);

            LineRenderer lr = obj.AddComponent<LineRenderer>();

            // 초기 설정 (Update에서 반복할 필요 없는 것들)
            lr.material = material;
            lr.widthCurve = AnimationCurve.Linear(0, wireWidth, 1, wireWidth); // WidthCurve 재사용 고려 가능하지만 Start에서는 OK
            lr.positionCount = 2;
            lr.useWorldSpace = true; // 월드 좌표계 사용 명시

            listLineRenderers.Add(lr);
        }
    }

    private void Update()
    {
        if (crc == null || !crc.fixPosition)
        {
            Wire__C();
        }
        else
        {
            return;
        }
    }

    // Update is called once per frame
    public void Wire__C()
    {
        // 예외 처리: 위치 정보가 없으면 실행 안 함
        if (position1 == null || position2 == null) return;

        int count = listLineRenderers.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            LineRenderer lineRenderer = listLineRenderers[i];

            // 런타임에 너비를 조정하고 싶다면 여기에 두되, 고정값이라면 Start로 옮기는 것이 좋음
            // lineRenderer.widthMultiplier = wireWidth; // 단일 값 변경은 widthMultiplier 추천

            float offset = 0f;

            // 0으로 나누기 방지 로직
            if (count > 1)
            {
                // 기존 로직 유지: (i - (N-1) * 0.5) / (N-1) * 0.5
                // 범위를 -0.25 ~ 0.25 로 좁히는 로직으로 보임
                offset = (i - (count - 1) * 0.5f) / (count - 1) * 0.5f;
            }
            else
            {
                // 와이어가 1개일 때는 중앙(0)
                offset = 0f;
            }

            // 위치 갱신
            // 주의: position1.right는 월드 회전 기준임.
            lineRenderer.SetPosition(0, position1.position + position1.right * offset * width1);
            lineRenderer.SetPosition(1, position2.position + position2.right * offset * width2);
        }
    }
}