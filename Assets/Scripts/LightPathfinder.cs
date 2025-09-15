using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    private const float EPS_SHRINK = 1e-4f;

    #region Inspector Fields / Results
    public Transform startPoint;
    public Transform endPoint;

    [Min(0), Tooltip("像（仮想光源）を展開する最大反射回数。指数で計算量ふえるよ")]
    public int maxReflections = 3;

    public LayerMask obstacleLayerMask;
    [Tooltip("遮蔽として扱う鏡の LayerMask（反射に使わない鏡は遮蔽）")]
    public LayerMask mirrorsLayerMask;
    public List<Mirror2D> mirrors = new();

    [Header("Debug/Result")]
    public List<Vector2> lastValidPath = new();
    public bool lastReachable;
    #endregion

    #region Lifecycle / Entry
    void Start()
    {
        AutoRegisterMirrors();
        FindPath();
    }
    
    void OnEnable()
    {
        // ゲーム中にMirror2Dが生成された場合の検出
        Mirror2D.OnMirrorCreated += OnMirrorCreated;
        Mirror2D.OnMirrorDestroyed += OnMirrorDestroyed;
    }
    
    void OnDisable()
    {
        Mirror2D.OnMirrorCreated -= OnMirrorCreated;
        Mirror2D.OnMirrorDestroyed -= OnMirrorDestroyed;
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
        lastReachable = IsReachableBidirectional(startPoint.position, endPoint.position, maxReflections, out lastValidPath);
    }
    
    /// <summary>
    /// シーン内のMirror2Dを自動で検出・登録する
    /// </summary>
    public void AutoRegisterMirrors()
    {
        // 既存のミラーリストをクリア
        mirrors.Clear();
        
        // シーン内の全てのMirror2Dを検索
        Mirror2D[] allMirrors = FindObjectsByType<Mirror2D>(FindObjectsSortMode.None);
        
        foreach (var mirror in allMirrors)
        {
            mirrors.Add(mirror);
        }
        
        Debug.Log($"LightPathfinder: {mirrors.Count}個のMirror2Dを自動登録しました");
    }
    
    /// <summary>
    /// Mirror2Dが生成された時のコールバック
    /// </summary>
    private void OnMirrorCreated(Mirror2D mirror)
    {
        if (!mirrors.Contains(mirror))
        {
            mirrors.Add(mirror);
            Debug.Log($"LightPathfinder: Mirror2D '{mirror.name}' を自動登録しました");
            // パスを再計算
            FindPath();
        }
    }
    
    /// <summary>
    /// Mirror2Dが削除された時のコールバック
    /// </summary>
    private void OnMirrorDestroyed(Mirror2D mirror)
    {
        if (mirrors.Contains(mirror))
        {
            mirrors.Remove(mirror);
            Debug.Log($"LightPathfinder: Mirror2D '{mirror.name}' を自動登録から削除しました");
            // パスを再計算
            FindPath();
        }
    }
    #endregion

    #region Main Search (Bidirectional)
    /// <summary>
    /// 最大 <paramref name="maxReflections"/> 回までの双方向探索で到達可能かを判定。
    /// </summary>
    public bool IsReachableBidirectional(Vector2 source, Vector2 target, int maxDepth, out List<Vector2> path)
    {
        path = null;
        // 初期フロンティア
        List<ImageNode> frontS = new List<ImageNode> { new ImageNode { position = source, mirrorSequence = new List<int>() } };
        List<ImageNode> frontT = new List<ImageNode> { new ImageNode { position = target, mirrorSequence = new List<int>() } };

        // 深さ0での直通チェック
        if (IsSegmentClear(source, target, obstacleLayerMask))
        {
            var nodeZero = new ImageNode { position = source, mirrorSequence = new List<int>() };
            if (ValidateAndBuildPath(source, target, nodeZero))
            {
                path = new List<Vector2> { source, target };
                return true;
            }
        }

        for (int depth = 1; depth <= maxDepth; depth++)
        {
            // 片側ずつ1段拡張（小さい方を先に拡張）
            bool expandSourceFirst = frontS.Count <= frontT.Count;
            if (expandSourceFirst)
            {
                var nextS = ExpandOneLayer(frontS, target);
                // 接続チェック：新規Sと既存/新規T
                if (TryConnectLayers(nextS, frontT, source, target, out path)) return true;
                frontS = nextS;
            }
            else
            {
                var nextT = ExpandOneLayer(frontT, source);
                if (TryConnectLayers(frontS, nextT, source, target, out path)) return true;
                frontT = nextT;
            }

            // 反対側も拡張
            if (expandSourceFirst)
            {
                var nextT = ExpandOneLayer(frontT, source);
                if (TryConnectLayers(frontS, nextT, source, target, out path)) return true;
                frontT = nextT;
            }
            else
            {
                var nextS = ExpandOneLayer(frontS, target);
                if (TryConnectLayers(nextS, frontT, source, target, out path)) return true;
                frontS = nextS;
            }
        }
        return false;
    }

    // 片側のフロンティアを1段だけ展開。facingPoint は法線前面チェックに使用。
    private List<ImageNode> ExpandOneLayer(List<ImageNode> frontier, Vector2 facingPoint)
    {
        List<ImageNode> next = new List<ImageNode>();
        int frontierCount = frontier.Count;
        int mirrorCount = mirrors.Count;
        for (int i = 0; i < frontierCount; i++)
        {
            for (int m = 0; m < mirrorCount; m++)
            {
                if (frontier[i].mirrorSequence.Count > 0 && frontier[i].mirrorSequence[frontier[i].mirrorSequence.Count - 1] == m)
                    continue;
                Mirror2D mirror = mirrors[m];
                Vector2 img = ReflectPointAcrossMirror(frontier[i].position, mirror);
                List<int> seq = new List<int>(frontier[i].mirrorSequence);
                seq.Add(m);
                ImageNode node = new ImageNode { position = img, mirrorSequence = seq };
                // 枝刈り：この段のミラーの法線正側に facingPoint がある想定のみ
                Vector2 a = mirror.StartPoint;
                Vector2 b = mirror.EndPoint;
                Vector2 hitOnLine = ClosestPointOnLine(a, b, facingPoint);
                Vector2 outv = (facingPoint - hitOnLine).normalized;
                Vector2 n = mirror.GetNormal();
                if (Vector2.Dot(outv, n) <= EPS_FRONT) continue;

                next.Add(node);
            }
        }
        return next;
    }

    // 2つのフロンティア集合の間で接続可能なペアを探し、見つかれば検証してパスを返す
    private bool TryConnectLayers(List<ImageNode> sideA, List<ImageNode> sideB, Vector2 source, Vector2 target, out List<Vector2> path)
    {
        path = null;
        for (int i = 0; i < sideA.Count; i++)
        {
            for (int j = 0; j < sideB.Count; j++)
            {
                Vector2 pa = sideA[i].position;
                Vector2 pb = sideB[j].position;
                if (!IsSegmentClear(pa, pb, obstacleLayerMask)) continue;

                // 鏡列合成: source側 seqA と target側 seqB を逆順に連結
                var seqA = sideA[i].mirrorSequence;
                var seqB = sideB[j].mirrorSequence;
                List<int> combined = new List<int>(seqA.Count + seqB.Count);
                combined.AddRange(seqA);
                for (int k = seqB.Count - 1; k >= 0; k--) combined.Add(seqB[k]);

                // 画像位置も合成: pa（= seqA 適用済み）に対し seqB を逆順で適用
                Vector2 combinedImage = pa;
                for (int k = seqB.Count - 1; k >= 0; k--)
                {
                    combinedImage = ReflectPointAcrossMirror(combinedImage, mirrors[seqB[k]]);
                }

                var nodeCombined = new ImageNode { position = combinedImage, mirrorSequence = combined };
                if (ValidateAndBuildPath(source, target, nodeCombined))
                {
                    path = BuildFullPathPoints(source, target, nodeCombined);
                    return true;
                }
            }
        }
        return false;
    }
    #endregion

    #region Geometry Helpers
    // 像のノード
    private struct ImageNode
    {
        public Vector2 position;
        public List<int> mirrorSequence;
    }

    private static Vector2 ClosestPointOnLine(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom < 1e-12f) return a;
        float t = Vector2.Dot(p - a, ab) / denom; // 無限直線なのでクランプ不要
        return a + t * ab;
    }

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

    private bool IntersectLineWithMirror(Vector2 p1, Vector2 p2, Mirror2D mirror, out Vector2 hit)
    {
        return LineLineIntersection(p1, p2, mirror.StartPoint, mirror.EndPoint, out hit);
    }

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

    private static Vector2 ReflectVector(Vector2 v, Vector2 normal)
    {
        Vector2 nn = normal.normalized;
        return v - 2f * Vector2.Dot(v, nn) * nn;
    }

    private static bool IsSegmentClear(Vector2 a, Vector2 b, LayerMask mask)
    {
        Vector2 dir = b - a;
        float len = dir.magnitude;
        if (len < 1e-6f) return true;
        dir /= len;
        Vector2 aa = a + dir * EPS_SHRINK;
        Vector2 bb = b - dir * EPS_SHRINK;
        return !Physics2D.Linecast(aa, bb, mask);
    }
    #endregion

    #region Validation / Path Build
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
            // 前面反射のみ許可
            if (Vector2.Dot(inc, n) > -EPS_FRONT) return false;
            Vector2 refl = ReflectVector(inc, n).normalized;
            Vector2 outv = (next - cur).normalized;
            if (Vector2.Dot(refl, outv) < 1f - EPS_REFLECT_MATCH) return false;
        }
        for (int i = 0; i < points.Count - 1; i++)
        {
            if (!IsSegmentClear(points[i], points[i + 1], obstacleLayerMask | mirrorsLayerMask)) return false;
        }
        return true;
    }

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
    #endregion

    #region Gizmos / Editor
    void OnDrawGizmos()
    {
        if (lastValidPath == null) return;
        if (lastValidPath.Count < 2) return;
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
            
            UnityEditor.EditorGUILayout.Space();
            UnityEditor.EditorGUILayout.LabelField("操作", UnityEditor.EditorStyles.boldLabel);
            
            if (GUILayout.Button("再探査（FindPath 実行）"))
            {
                pathfinder.FindPath();
                UnityEditor.EditorUtility.SetDirty(pathfinder);
            }
            
            if (GUILayout.Button("Mirror2Dを手動で再登録"))
            {
                pathfinder.AutoRegisterMirrors();
                UnityEditor.EditorUtility.SetDirty(pathfinder);
            }
        }
    }
    #endif
    #endregion
}
