using UnityEngine;
using System.Collections.Generic;

public class TestLasar : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform startPoint;
    [SerializeField] private LightPathfinder pathfinder;

    [Header("Debug")]
    public bool enableDebug = false;

    private LayerMask obstacleLayerMask;
    private LayerMask mirrorsLayerMask;
    private List<Vector2> currentPath = new List<Vector2>();

    // 数値安定用の閾値
    private const float EPS_PARALLEL = 1e-9f;

    void Start()
    {
        obstacleLayerMask = pathfinder.obstacleLayerMask;
        mirrorsLayerMask = pathfinder.mirrorsLayerMask;
    }

    void Update()
    {
        if (enableDebug)
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mouseWorldPos - (Vector2)startPoint.position).normalized;
            
            currentPath = CalculateLightPath((Vector2)startPoint.position, direction);
            DrawPath();
        }
    }

    /// <summary>
    /// 光のパスを計算する（Mirror2Dとの交点チェックと反射を繰り返し）
    /// </summary>
    private List<Vector2> CalculateLightPath(Vector2 startPos, Vector2 direction)
    {
        List<Vector2> path = new List<Vector2>();
        path.Add(startPos);

        Vector2 currentPos = startPos;
        Vector2 currentDir = direction;
        int maxReflections = 10; // 無限ループ防止
        int reflectionCount = 0;

        while (reflectionCount < maxReflections)
        {
            // 最も近いMirror2Dとの交点を探す
            Mirror2D closestMirror = null;
            Vector2 closestIntersection = Vector2.zero;
            float closestDistance = float.MaxValue;

            foreach (Mirror2D mirror in pathfinder.mirrors)
            {
                if (LineSegmentIntersection(currentPos, currentPos + currentDir * 1000f, 
                    mirror.StartPoint, mirror.EndPoint, out Vector2 intersection))
                {
                    float distance = Vector2.Distance(currentPos, intersection);
                    if (distance < closestDistance && distance > 0.001f) // 現在位置と重複しないように
                    {
                        closestDistance = distance;
                        closestMirror = mirror;
                        closestIntersection = intersection;
                    }
                }
            }

            // 交点が見つからない場合は遠くを指定
            if (closestMirror == null) closestIntersection = currentPos + currentDir * 100f;


            // 交点までの障害物をチェック
            if (IsSegmentClear(currentPos, closestIntersection, out Vector2 obstacle))
            {
                // 障害物なし
                if (closestMirror != null)
                {
                    // 反射
                    path.Add(closestIntersection);
                    currentPos = closestIntersection;
                    currentDir = ReflectVector(currentDir, closestMirror.GetNormal());
                    reflectionCount++;
                }else{
                    //終了
                    path.Add(closestIntersection);
                    break;
                }
            }else{
                // 障害物あり
                path.Add(obstacle);
                break;
            }
        }

        return path;
    }

    /// <summary>
    /// 線分と線分の交差判定
    /// </summary>
    private bool LineSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        Vector2 r = p2 - p1;
        Vector2 s = p4 - p3;
        float rxs = r.x * s.y - r.y * s.x;
        if (Mathf.Abs(rxs) < EPS_PARALLEL) return false;
        
        Vector2 qp = p3 - p1;
        float t = (qp.x * s.y - qp.y * s.x) / rxs;
        float u = (qp.x * r.y - qp.y * r.x) / rxs;

        if (t >= 0 && t <= 1 && u >= 0 && u <= 1){
            intersection = p1 + t * r;
            return true;
        }

        intersection = Vector2.zero;
        return false;
    }

    /// <summary>
    /// ベクトルの反射計算
    /// </summary>
    private static Vector2 ReflectVector(Vector2 v, Vector2 normal)
    {
        Vector2 nn = normal.normalized;
        return v - 2f * Vector2.Dot(v, nn) * nn;
    }

    /// <summary>
    /// 線分が障害物に遮られていないかチェック
    /// </summary>
    private bool IsSegmentClear(Vector2 start, Vector2 end, out Vector2 obstacle)
    {
        Vector2 direction = end - start;
        float distance = direction.magnitude;

        LayerMask mask = obstacleLayerMask | mirrorsLayerMask;
        RaycastHit2D hit = Physics2D.Raycast(start, direction, distance, mask);

        obstacle = hit.point;
        return hit.collider == null;
    }

    /// <summary>
    /// 計算されたパスをLineRendererで描画
    /// </summary>
    private void DrawPath()
    {
        if (lineRenderer == null || currentPath == null || currentPath.Count < 2) return;

        lineRenderer.positionCount = currentPath.Count;
        for (int i = 0; i < currentPath.Count; i++)
        {
            lineRenderer.SetPosition(i, new Vector3(currentPath[i].x, currentPath[i].y, 0));
        }
    }
}
