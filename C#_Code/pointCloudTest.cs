using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PointManager
{
    public CraneRailConstraint railConstraint;

    public PointManager(CraneParts parts)//pjh
    {
        this.pier = parts.pierCode;
        this.crane = parts.craneCode;

        this.railConstraint = parts.GetComponent<CraneRailConstraint>();

        group = GameObject.Find("pointCloudGroups");
        pointPoolGroup = new GameObject(parts.craneName + "pool");
        //pjh
        pointGroup = parts.pointCloudTransform;
        pointPoolGroup.transform.SetParent(parts.gameObject.transform);
        pointPoolGroup.transform.localPosition = Vector3.zero;
        pointPoolGroup.transform.localRotation = Quaternion.identity;
        //~pjh

        bUpdate = false;
    }

    public void UpdatePoints(uint numPoints, Vector3[] points, Color[] colors, int[] indices)
    {
        numPoints = System.Math.Min(numPoints, limitPoints);
        this.points = points;
        this.colors = colors;
        this.indices = indices;
        bUpdate = true;
    }

    public void ResetUpdatd()
    {
        bUpdate = false;
    }

    public bool IsUpdated()
    {
        return bUpdate;
    }

    public int pier;
    public int crane;

    // PointCloud
    bool bUpdate;
    public uint limitPoints = 650000;
    public uint numPoints;
    public Vector3[] points;
    public Color[] colors;
    public int[] indices;

    //GameObject
    public GameObject group;
    public GameObject pointGroup;
    public GameObject pointPoolGroup;
    public Queue<GameObject> pointGroups = new Queue<GameObject>();
    public Queue<GameObject> pointGroupsPool = new Queue<GameObject>();
}
public class pointCloudTest : MonoBehaviour
{
    public Material matVertex;
    public int queueSize = 10;
    public int poolSize = 100;
    private List<PointManager> manager = new List<PointManager>();
    private string textPointSize = "";
    private float pointSize = 0.3f;

    private Collider inscCollider;

    void Start()
    {
        //pjh
        Application.targetFrameRate = 20;//int.Parse(CsCore.Configuration.ReadConfigIni("TargetFrameRate" , "Frame", "30"));


        var craneInfo = GetComponent<CraneInfo>();
        for (int i = 0; i < craneInfo.keys.Count; i++)
        {
            manager.Add(new PointManager(craneInfo.craneGameObject[i].GetComponent<CraneParts>()));
        }
        //~pjh

        for (int i = 0; i < manager.Count; i++)
        {
            for (int j = 0; j < poolSize; j++)
            {
                PointManager pointManager = manager[i];
                Mesh mesh = new Mesh();

                GameObject pointFrame = new GameObject("point cloud");
                pointFrame.AddComponent<MeshFilter>();
                pointFrame.AddComponent<MeshRenderer>();
                pointFrame.GetComponent<Renderer>().material = matVertex;
                pointFrame.GetComponent<MeshFilter>().mesh = mesh;
                pointFrame.transform.SetParent(pointManager.pointPoolGroup.transform);
                pointFrame.SetActive(false);

                pointManager.pointGroupsPool.Enqueue(pointFrame);
            }
        }
        StartCoroutine(UpdateCoroutine());
    }

    public void UpdatePoints(int pier, int crane, uint _numPoints, Vector3[] _vertices, Color[] _colors, int[] _indices)
    {
        foreach (PointManager mgr in manager)
        {
            if (mgr.pier == pier && mgr.crane == crane)
            {
                mgr.UpdatePoints(_numPoints, _vertices, _colors, _indices);
                break;
            }
        }
    }
    readonly WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1);
    //pjh
    IEnumerator UpdateCoroutine()
    {
        while (true)
        {
            textPointSize = CsCore.Configuration.ReadConfigIni("PointSize", "Value");
            float.TryParse(textPointSize, out pointSize);

            // [추가] Insc 태그를 가진 오브젝트의 콜라이더 찾기 (캐싱)
            if (inscCollider == null)
            {
                GameObject obj = GameObject.FindGameObjectWithTag("Insc");
                if (obj != null)
                {
                    inscCollider = obj.GetComponent<Collider>();
                }
            }

            for (int i = 0; i < manager.Count; i++)
            {
                PointManager pointManager = manager[i];
                if (pointManager.IsUpdated())
                {
                    //pjh
                    var points = pointManager.points;
                    var colors = pointManager.colors;
                    // var indices = pointManager.indices; // [수정] 필터링된 인덱스를 새로 생성하므로 기존 인덱스는 사용하지 않음
                    //~pjh
                    pointManager.ResetUpdatd();

                    try
                    {
                        if (points.Length != colors.Length) continue;//pjh

                        GameObject pointFrame = pointManager.pointGroupsPool.Dequeue();

                        matVertex.SetFloat("_PointSize", pointSize);
                        pointFrame.SetActive(true);

                        // [추가] 포인트 필터링 로직 시작
                        List<Vector3> validPoints = new List<Vector3>(points.Length);
                        List<Color> validColors = new List<Color>(colors.Length);
                        List<int> validIndices = new List<int>(points.Length);

                        if (inscCollider != null && pointManager.pointGroup != null)
                        {
                            // 포인트들이 로컬 좌표계(pointManager.pointGroup 기준)에 있으므로 변환을 위해 Transform 가져오기
                            Transform originTr = pointManager.pointGroup.transform;

                            for (int k = 0; k < points.Length; k++)
                            {
                                Vector3 pt = points[k];
                                // 로컬 좌표 -> 월드 좌표 변환
                                Vector3 worldPt = originTr.TransformPoint(pt);

                                // 1차: AABB(경계 상자) 검사 (빠른 성능)
                                if (inscCollider.bounds.Contains(worldPt))
                                {
                                    // 2차: 정밀 검사 (포인트가 콜라이더 내부 혹은 표면에 있으면 제외)
                                    if (inscCollider.ClosestPoint(worldPt) == worldPt)
                                    {
                                        continue; // 렌더링 리스트에 추가하지 않음
                                    }
                                }

                                // 유효한 포인트만 리스트에 추가
                                validPoints.Add(pt);
                                validColors.Add(colors[k]);
                                validIndices.Add(validIndices.Count); // 0부터 순차적으로 인덱스 생성
                            }
                        }
                        else
                        {
                            // 콜라이더가 없거나 기준 Transform이 없으면 원본 그대로 사용
                            validPoints.AddRange(points);
                            validColors.AddRange(colors);
                            for (int k = 0; k < points.Length; k++) validIndices.Add(k);
                        }
                        // [추가] 포인트 필터링 로직 끝

                        Mesh mesh = pointFrame.GetComponent<MeshFilter>().mesh;
                        mesh.Clear();

                        // [수정] 필터링된 리스트를 배열로 변환하여 적용
                        mesh.vertices = validPoints.ToArray();
                        mesh.colors = validColors.ToArray();
                        mesh.SetIndices(validIndices.ToArray(), MeshTopology.Points, 0);

                        pointFrame.GetComponent<MeshFilter>().mesh = mesh;
                        pointFrame.transform.SetParent(pointManager.pointGroup.transform);
                        //pjh
                        pointFrame.transform.localPosition = Vector3.zero;
                        pointFrame.transform.localRotation = Quaternion.identity;
                        pointFrame.transform.localScale = Vector3.one;
                        //

                        // 1. 만약 레일 제약조건이 있고 활성화되어 있다면 보정 실행
                        if (pointManager.railConstraint != null && pointManager.railConstraint.fixPosition
                            && pointManager.railConstraint.railStart != null && pointManager.railConstraint.railEnd != null)
                        {
                            // 현재 크레인의 위치 (오차가 있을 수 있음)
                            Vector3 currentCranePos = pointManager.railConstraint.transform.position;

                            // 크레인이 가야 할 정확한 레일 위 위치 계산 (미리 계산)
                            Vector3 correctCranePos = pointManager.railConstraint.GetProjectedPosition(
                                pointManager.railConstraint.railStart.position,
                                pointManager.railConstraint.railEnd.position,
                                currentCranePos
                            );

                            // 오차 벡터 계산 (보정될 위치 - 현재 위치)
                            Vector3 correctionDiff = correctCranePos - currentCranePos;

                            // 포인트 클라우드도 그 오차만큼 미리 이동시킴
                            pointFrame.transform.position += correctionDiff;
                        }

                        // CES
                        if (pointManager.group != null)
                            pointFrame.transform.SetParent(pointManager.group.transform);

                        pointManager.pointGroups.Enqueue(pointFrame);

                        while (pointManager.pointGroups.Count > queueSize)
                        {
                            GameObject gameObject = pointManager.pointGroups.Dequeue();
                            gameObject.transform.SetParent(pointManager.pointPoolGroup.transform);
                            gameObject.SetActive(false);

                            // CES
                            gameObject.transform.localPosition = Vector3.zero;
                            gameObject.transform.localRotation = Quaternion.identity;
                            gameObject.transform.localScale = Vector3.one;

                            pointManager.pointGroupsPool.Enqueue(pointFrame);
                        }
                    }
                    catch (System.InvalidOperationException e)
                    {
                        Debug.Log(e.ToString());
                    }
                }
            }
            yield return wait;
        }
    }
}
