using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 2D におけるライトパス探索。
/// 指定回数まで鏡で反射した経路が、障害物に遮られずにターゲットへ到達できるかを判定。
/// </summary>
public class LightPathfinder : MonoBehaviour
{
    // 数値安定用の閾値
    private const float EPS_PARALLEL = 1e-9f;
    private const float EPS_FRONT = 1e-4f;
    private const float EPS_REFLECT_MATCH = 3e-3f;
    private const float EPS_SEGMENT = 1e-3f;
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
        lastValidPath?.Clear();
        if (startPoint == null || endPoint == null)
        {
            lastReachable = false;
            return;
        }
        if (mirrors == null) mirrors = new List<Mirror2D>();
        lastReachable = IsReachable(startPoint.position, endPoint.position, maxReflections, out lastValidPath);
    }

    /// <summary>
    /// 最大 <paramref name="maxReflections"/> 回まで展開し、到達可能か判定。
    /// 成功時は通過点リスト（source -> ミラー交点... -> target）を返す。
    /// </summary>
    public bool IsReachable(Vector2 source, Vector2 target, int maxReflections, out List<Vector2> path)
    {
        path = null;
        // 1) 仮想光源を BFS で展開（指数成長に注意）。法線方向のみの軽量枝刈りを適用。
        List<ImageNode> images = GenerateImagesBFS(source, target, maxReflections);
        for (int i = 0; i < images.Count; i++)
        {
            ImageNode node = images[i];
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

    // 像のノード
    private struct ImageNode
    {
        public Vector2 position;
        public List<int> mirrorSequence;
    }

    /// <summary>
    /// ソースから始めて、各深さで全ミラーに関して点の鏡映を生成し列挙します（BFS）。
    /// </summary>
    private List<ImageNode> GenerateImagesBFS(Vector2 source, Vector2 target, int maxDepth)
    {
        // 生成した全像ノードを格納するリスト
        List<ImageNode> list = new List<ImageNode>();
        // 最初のノードを追加
        list.Add(new ImageNode { position = source, mirrorSequence = new List<int>() });

        // 現在のフロンティア（この深さで展開する像ノード群）
        List<ImageNode> frontier = new List<ImageNode> { list[0] };

        // 指定された深さまでBFSで展開
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            // 次のフロンティアを格納するリスト
            List<ImageNode> next = new List<ImageNode>();
            // 現在のフロンティアの各ノードについて
            int frontierCount = frontier.Count;
            for (int i = 0; i < frontierCount; i++)
            {
                // 全ミラーに対して鏡映を生成
                int mirrorCount = mirrors.Count;
                for (int m = 0; m < mirrorCount; m++)
                {
                    // 直前と同じミラーの連続使用は枝刈り
                    if (frontier[i].mirrorSequence.Count > 0 
                    && frontier[i].mirrorSequence[frontier[i].mirrorSequence.Count - 1] == m)
                        continue;
                    // m番目のミラーを取得
                    Mirror2D mirror = mirrors[m];
                    // 現在のノードの位置をこのミラーで鏡映
                    Vector2 img = ReflectPointAcrossMirror(frontier[i].position, mirror);
                    // ミラー列を複製し、今回のミラーを追加
                    List<int> seq = new List<int>(frontier[i].mirrorSequence);
                    seq.Add(m);
                    // 新しい像ノードを作成
                    ImageNode node = new ImageNode { position = img, mirrorSequence = seq };
                    // 軽量枝刈り: ターゲットがこのミラーの法線正側にある場合のみ採用
                    Vector2 a = mirror.StartPoint;
                    Vector2 b = mirror.EndPoint;
                    Vector2 hitOnLine = ClosestPointOnLine(a, b, target);
                    Vector2 outv = (target - hitOnLine).normalized;
                    Vector2 n = mirror.GetNormal();
                    if (Vector2.Dot(outv, n) <= EPS_FRONT)
                        continue;
                    // 次のフロンティアと全体リストに追加
                    next.Add(node);
                    list.Add(node);
                }
            }
            // 次の深さのフロンティアに更新
            frontier = next;
        }
        // 全ての像ノードを返す
        return list;
    }

    private static Vector2 ClosestPointOnLine(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom < 1e-12f) return a;
        float t = Vector2.Dot(p - a, ab) / denom; // 無限直線なのでクランプ不要
        return a + t * ab;
    }

    /// <summary>
    /// 点 p を鏡（線分 ab の無限直線）に対して鏡映した位置を返します。
    /// </summary>
    private static Vector2 ReflectPointAcrossMirror(Vector2 p, Mirror2D mirror)
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
            int mirrorIndex = i - 1;
            if (mirrorIndex < 0 || mirrorIndex >= image.mirrorSequence.Count) return false;
            var mirror = mirrors[image.mirrorSequence[mirrorIndex]];
            Vector2 inc = (cur - prev).normalized;
            Vector2 n = mirror.GetNormal();
            // 前面反射のみ許可: 入射方向が鏡法線に対して負向き（正面側）から来ている必要がある
            if (Vector2.Dot(inc, n) > -EPS_FRONT) return false;
            Vector2 refl = ReflectVector(inc, n).normalized;
            Vector2 outv = (next - cur).normalized;
            if (Vector2.Dot(refl, outv) < 1f - EPS_REFLECT_MATCH) return false;
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
            lineFrom = ReflectPointAcrossMirror(lineFrom, mirror);
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
        if (Mathf.Abs(rxs) < EPS_PARALLEL) return false;
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
        float ab = (b - a).sqrMagnitude;
        float ap = (p - a).sqrMagnitude;
        float pb = (b - p).sqrMagnitude;
        if (ap + pb > ab + EPS_SEGMENT) return false;
        Vector2 abv = (b - a);
        Vector2 apv = (p - a);
        float cross = Mathf.Abs(abv.x * apv.y - abv.y * apv.x);
        return cross <= EPS_SEGMENT;
    }

    /// <summary>
    /// ベクトル v を法線 normal で反射したベクトルを返す。
    /// </summary>
    private static Vector2 ReflectVector(Vector2 v, Vector2 normal)
    {
        Vector2 nn = normal.normalized;
        return v - 2f * Vector2.Dot(v, nn) * nn;
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
