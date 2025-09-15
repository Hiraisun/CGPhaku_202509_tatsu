using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 2D におけるライトパス探索。
/// 指定回数まで鏡で反射した経路が、障害物に遮られずにターゲットへ到達できるかを判定。
/// </summary>
public class LightPathfinder : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;

    [Min(0), Tooltip("像（仮想光源）を展開する最大反射回数。指数で計算量ふえるよ")]
    public int maxReflections = 3;

    public LayerMask obstacleLayerMask;
    public List<Mirror2D> mirrors = new();

    [Header("Debug/Result")]
    public bool drawWhenNoPath = true;
    public List<Vector2> lastValidPath = new();
    public bool lastReachable;



    void Start()
    {
        FindPath();
    }

    /// <summary>
    /// 現在の設定で光路探索を実行し、結果を <see cref="lastValidPath"/> と <see cref="lastReachable"/> に保存。
    /// </summary>
    public void FindPath()
    {
        if (lastValidPath != null) lastValidPath.Clear();
        lastReachable = IsReachable(startPoint.position, endPoint.position, maxReflections, out lastValidPath);
    }

    /// <summary>
    /// 最大 <paramref name="maxReflections"/> 回まで展開し、到達可能か判定。
    /// 成功時は通過点リスト（source -> ミラー交点... -> target）を返す。
    /// </summary>
    public bool IsReachable(Vector2 source, Vector2 target, int maxReflections, out List<Vector2> path)
    {
        path = null;
        // 1) 仮想光源を BFS で展開（指数成長に注意）。
        var images = GenerateImagesBFS(source, maxReflections);
        for (int i = 0; i < images.Count; i++)
        {
            var node = images[i];
            // 2) 仮想光源からターゲットへの直線が障害物に遮られないかを粗判定。
            if (!Physics2D.Linecast(node.position, target, obstacleLayerMask))
            {
                // 3) 反射点が各ミラー線分上か、反射則/遮蔽が満たされるかの厳密検証。
                if (ValidateAndBuildPath(source, target, node))
                {
                    path = BuildFullPathPoints(source, target, node);
                    return true;
                }
            }
        }
        return false;
    }

    private struct ImageNode
    {
        public Vector2 position;
        public List<int> mirrorSequence;
    }

    /// <summary>
    /// ソースから始めて、各深さで全ミラーに関して点の鏡映を生成し列挙します（BFS）。
    /// </summary>
    private List<ImageNode> GenerateImagesBFS(Vector2 source, int maxDepth)
    {
        var list = new List<ImageNode>();
        list.Add(new ImageNode { position = source, mirrorSequence = new List<int>() });
        var frontier = new List<ImageNode> { list[0] };
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            var next = new List<ImageNode>();
            for (int i = 0; i < frontier.Count; i++)
            {
                for (int m = 0; m < mirrors.Count; m++)
                {
                    var mirror = mirrors[m];
                    Vector2 img = ReflectPointAcrossMirror(frontier[i].position, mirror);
                    var seq = new List<int>(frontier[i].mirrorSequence);
                    seq.Add(m);
                    var node = new ImageNode { position = img, mirrorSequence = seq };
                    next.Add(node);
                    list.Add(node);
                }
            }
            frontier = next;
        }
        return list;
    }

    /// <summary>
    /// 点 p を鏡（線分 ab の無限直線）に対して鏡映した位置を返します。
    /// </summary>
    private Vector2 ReflectPointAcrossMirror(Vector2 p, Mirror2D mirror)
    {
        Vector2 a = mirror.StartPoint;
        Vector2 b = mirror.EndPoint;
        Vector2 dir = (b - a);
        if (dir.sqrMagnitude < 1e-12f) return p;
        dir.Normalize();
        Vector2 n = new Vector2(-dir.y, dir.x);
        Vector2 ap = p - a;
        float dist = Vector2.Dot(ap, n);
        return p - 2f * dist * n;
    }

    /// <summary>
    /// ターゲットから仮想光源へ向かう直線を、像生成の鏡列を逆順にたどって交点を求め、
    /// 各交点が線分上であること・反射則が成り立つこと・障害物に遮られないことを検証します。
    /// </summary>
    private bool ValidateAndBuildPath(Vector2 source, Vector2 target, ImageNode image)
    {
        var points = BuildFullPathPoints(source, target, image);
        if (points == null || points.Count < 2) return false;
        for (int i = 1; i < points.Count - 1; i++)
        {
            var prev = points[i - 1];
            var cur = points[i];
            var next = points[i + 1];
            if (Physics2D.Linecast(prev, cur, obstacleLayerMask)) return false;
            int mirrorIndex = i - 1;
            if (mirrorIndex < 0 || mirrorIndex >= image.mirrorSequence.Count) return false;
            var mirror = mirrors[image.mirrorSequence[mirrorIndex]];
            Vector2 inc = (cur - prev).normalized;
            Vector2 n = mirror.GetNormal();
            Vector2 refl = ReflectVector(inc, n).normalized;
            Vector2 outv = (next - cur).normalized;
            if (Vector2.Dot(refl, outv) < 1f - 1e-3f)
            {
                Vector2 refl2 = ReflectVector(inc, -n).normalized;
                if (Vector2.Dot(refl2, outv) < 1f - 1e-3f) return false;
            }
        }
        for (int i = 0; i < points.Count - 1; i++)
        {
            if (Physics2D.Linecast(points[i], points[i + 1], obstacleLayerMask)) return false;
        }
        return true;
    }

    /// <summary>
    /// 検証用に、source -> 反射点群 -> target の順で通過点リストを構築します。
    /// </summary>
    private List<Vector2> BuildFullPathPoints(Vector2 source, Vector2 target, ImageNode image)
    {
        var seq = image.mirrorSequence;
        Vector2 virtualEndpoint = image.position;
        Vector2 lineFrom = target;
        Vector2 lineTo = virtualEndpoint;
        var hitsReverse = new List<Vector2>();
        for (int i = seq.Count - 1; i >= 0; i--)
        {
            var mirror = mirrors[seq[i]];
            if (!IntersectLineWithMirror(lineFrom, lineTo, mirror, out Vector2 hit)) return null;
            if (!IsOnSegment(hit, mirror.StartPoint, mirror.EndPoint)) return null;
            hitsReverse.Add(hit);
            virtualEndpoint = ReflectPointAcrossMirror(virtualEndpoint, mirror);
            lineTo = virtualEndpoint;
        }
        var points = new List<Vector2>();
        points.Add(source);
        for (int i = hitsReverse.Count - 1; i >= 0; i--) points.Add(hitsReverse[i]);
        points.Add(target);
        return points;
    }

    private bool IntersectLineWithMirror(Vector2 p1, Vector2 p2, Mirror2D mirror, out Vector2 hit)
    {
        return LineLineIntersection(p1, p2, mirror.StartPoint, mirror.EndPoint, out hit);
    }

    /// <summary>
    /// 無限直線 p1-p2 と p3-p4 の交点を求めます（平行時は false）。
    /// </summary>
    private static bool LineLineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        Vector2 r = p2 - p1;
        Vector2 s = p4 - p3;
        float rxs = r.x * s.y - r.y * s.x;
        if (Mathf.Abs(rxs) < 1e-9f) return false;
        Vector2 qp = p3 - p1;
        float t = (qp.x * s.y - qp.y * s.x) / rxs;
        intersection = p1 + t * r;
        return true;
    }

    /// <summary>
    /// 点 p が線分 ab 上にあるか（ε許容）
    /// </summary>
    private static bool IsOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        float eps = 1e-4f;
        float ab = (b - a).sqrMagnitude;
        float ap = (p - a).sqrMagnitude;
        float pb = (b - p).sqrMagnitude;
        if (ap + pb > ab + 1e-3f) return false;
        Vector2 abv = (b - a);
        Vector2 apv = (p - a);
        float cross = Mathf.Abs(abv.x * apv.y - abv.y * apv.x);
        return cross <= eps;
    }

    /// <summary>
    /// ベクトル v を法線 normal で反射したベクトルを返す。
    /// </summary>
    private static Vector2 ReflectVector(Vector2 v, Vector2 normal)
    {
        return v - 2f * Vector2.Dot(v, normal.normalized) * normal.normalized;
    }

    void OnDrawGizmos()
    {
        if (lastValidPath == null) return;
        if (lastValidPath.Count < 2 && !drawWhenNoPath) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < lastValidPath.Count - 1; i++)
        {
            Gizmos.DrawLine(lastValidPath[i], lastValidPath[i + 1]);
        }
    }

    /// <summary>
    /// インスペクタ上で値が変更された際に、パラメータの整合性を保つための検証処理。
    /// </summary>
    void OnValidate()
    {
        if (maxReflections < 0) maxReflections = 0;
        if (mirrors == null) mirrors = new List<Mirror2D>();
    }

    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(LightPathfinder))]
    public class LightPathfinderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LightPathfinder pathfinder = (LightPathfinder)target;
            if (GUILayout.Button("再探査（FindPath 実行）"))
            {
                pathfinder.FindPath();
                UnityEditor.EditorUtility.SetDirty(pathfinder);
            }
        }
    }
#endif
}
