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
    private const float EPS_SHRINK = 1e-4f;
    private const float DEDUP_QUANT = 1000f; // 重複削減用の位置量子化係数（約1e-3精度）

    // GC削減用の再利用バッファ
    private readonly List<Vector2> _tmpHitsReverse = new List<Vector2>(32);
    private readonly List<Vector2> _tmpPoints = new List<Vector2>(32);
    private readonly List<int> _tmpSeqA = new List<int>(16);
    private readonly List<int> _tmpSeqB = new List<int>(16);
    private readonly List<int> _tmpCombinedSeq = new List<int>(32);
    private static readonly RaycastHit2D[] _rayBuffer = new RaycastHit2D[1];

    private System.Diagnostics.Stopwatch stopwatch;

    public Transform startPoint;
    public Transform endPoint;

    [Min(0), Tooltip("像（仮想光源）を展開する最大反射回数。指数で計算量ふえるよ")]
    public int maxReflections = 3;

    public LayerMask obstacleLayerMask;
    [Tooltip("遮蔽として扱う鏡の LayerMask（反射に使わない鏡は遮蔽）")]
    public LayerMask mirrorsLayerMask;
    private LayerMask segmentCheckMask => obstacleLayerMask | mirrorsLayerMask;
    public List<Mirror2D> mirrors = new();

    [Header("Debug/Result")]
    [Tooltip("デバッグ情報をログ出力するか")]
    public bool enableDebugLog = false;
    public List<Vector2> lastValidPath = new();

    #region Lifecycle / Entry

    void Awake()
    {

    }

    void Start()
    {
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
    /// 現在の設定で光路探索を実行し、結果を <see cref="lastValidPath"/> に保存。
    /// </summary>
    public bool FindPath()
    {
        lastValidPath?.Clear();
        bool IsReachable = IsReachableBidirectional(startPoint.position, endPoint.position, maxReflections, lastValidPath);

        return IsReachable;
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
        
        foreach (Mirror2D mirror in allMirrors)
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
        }
    }
    #endregion

    #region Main Search (Bidirectional)
    /// <summary>
    /// 最大 <paramref name="maxReflections"/> 回までの双方向探索で到達可能かを判定。
    /// </summary>
    public bool IsReachableBidirectional(Vector2 source, Vector2 target, int maxDepth, List<Vector2> pathOut)
    {
        if (enableDebugLog)
        {
            // 時間計測開始
            stopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        pathOut.Clear();
        int totalNodesGenerated = 0; // 生成されたノード数
        int connectionChecks = 0; // 接続チェック数
        
        // この探索実行中のみ有効なミラー幾何キャッシュ
        MirrorGeo[] mirrorGeos = BuildMirrorGeometries();
        // ノードプール + 初期フロンティア（インデックス）
        List<ImageNode> poolS = new List<ImageNode>(64);
        List<ImageNode> poolT = new List<ImageNode>(64);
        List<int> frontS = new List<int>(16);
        List<int> frontT = new List<int>(16);
        poolS.Add(new ImageNode { position = source, parentIndex = -1, lastMirrorIndex = -1 });
        frontS.Add(0);
        poolT.Add(new ImageNode { position = target, parentIndex = -1, lastMirrorIndex = -1 });
        frontT.Add(0);

        // 再利用バッファ（フロンティア用インデックス）
        List<int> nextBuffer = new List<int>(Mathf.Max(16, mirrors.Count * 2));
        HashSet<(int,int,int)> seenBuffer = new HashSet<(int,int,int)>(256);
        totalNodesGenerated = 2; // source + target

        // 深さ0での直通チェック
        if (IsSegmentClear(source, target, segmentCheckMask))
        {
            _tmpCombinedSeq.Clear();
            if (ValidateAndBuildPath(source, target, source, _tmpCombinedSeq, mirrorGeos, pathOut))
            {
                LogDebugResult("直通成功", stopwatch, totalNodesGenerated, connectionChecks);
                return true;
            }
        }
        

        for (int depth = 1; depth <= maxDepth; depth++)
        {
            // 片側ずつ1段拡張（小さい方を先に拡張）
            bool expandSourceFirst = frontS.Count <= frontT.Count;
            if (expandSourceFirst)
            {
                ExpandOneLayer(frontS, target, mirrorGeos, nextBuffer, seenBuffer, poolS);
                totalNodesGenerated += nextBuffer.Count;
                // 接続チェック：新規Sと既存/新規T
                if (TryConnectLayers(nextBuffer, frontT, source, target, ref connectionChecks, mirrorGeos, poolS, poolT, pathOut)) 
                {
                    LogDebugResult($"深さ{depth}で成功", stopwatch, totalNodesGenerated, connectionChecks);
                    return true;
                }
                // バッファをスワップして再利用
                (frontS, nextBuffer) = (nextBuffer, frontS);
            }
            else
            {
                ExpandOneLayer(frontT, source, mirrorGeos, nextBuffer, seenBuffer, poolT);
                totalNodesGenerated += nextBuffer.Count;
                if (TryConnectLayers(frontS, nextBuffer, source, target, ref connectionChecks, mirrorGeos, poolS, poolT, pathOut)) 
                {
                    LogDebugResult($"深さ{depth}で成功", stopwatch, totalNodesGenerated, connectionChecks);
                    return true;
                }
                (frontT, nextBuffer) = (nextBuffer, frontT);
            }

            // 反対側も拡張
            if (expandSourceFirst)
            {
                ExpandOneLayer(frontT, source, mirrorGeos, nextBuffer, seenBuffer, poolT);
                totalNodesGenerated += nextBuffer.Count;
                if (TryConnectLayers(frontS, nextBuffer, source, target, ref connectionChecks, mirrorGeos, poolS, poolT, pathOut)) 
                {
                    LogDebugResult($"深さ{depth}で成功", stopwatch, totalNodesGenerated, connectionChecks);
                    return true;
                }
                (frontT, nextBuffer) = (nextBuffer, frontT);
            }
            else
            {
                ExpandOneLayer(frontS, target, mirrorGeos, nextBuffer, seenBuffer, poolS);
                totalNodesGenerated += nextBuffer.Count;
                if (TryConnectLayers(nextBuffer, frontT, source, target, ref connectionChecks, mirrorGeos, poolS, poolT, pathOut)) 
                {
                    LogDebugResult($"深さ{depth}で成功", stopwatch, totalNodesGenerated, connectionChecks);
                    return true;
                }
                (frontS, nextBuffer) = (nextBuffer, frontS);
            }
        }
        LogDebugResult("失敗", stopwatch, totalNodesGenerated, connectionChecks);
        return false;
    }

    private void LogDebugResult(string result, System.Diagnostics.Stopwatch stopwatch, int totalNodes, int connectionChecks)
    {
        if (enableDebugLog)
        {
            stopwatch.Stop();
            Debug.Log($"[LightPathfinder] {result}: {stopwatch.ElapsedMilliseconds}ms, ノード数: {totalNodes}, 接続チェック: {connectionChecks}");
        }
    }

    // 片側のフロンティアを1段だけ展開（インデックスベース）。facingPoint は法線前面チェックに使用。
    private void ExpandOneLayer(List<int> frontier, Vector2 facingPoint, MirrorGeo[] mirrorGeos, List<int> nextOut, HashSet<(int,int,int)> seen, List<ImageNode> pool)
    {
        nextOut.Clear();
        seen.Clear();
        int frontierCount = frontier.Count;
        int mirrorCount = mirrors.Count;
        for (int i = 0; i < frontierCount; i++)
        {
            int parentIdx = frontier[i];
            ImageNode parent = pool[parentIdx];
            for (int m = 0; m < mirrorCount; m++)
            {
                if (parent.lastMirrorIndex == m) continue; // 直近同一鏡の連続反射を抑止
                MirrorGeo mg = mirrorGeos[m];
                Vector2 img = ReflectPointAcrossMirror(parent.position, ref mg);
                // 重複削減
                int xq = Mathf.RoundToInt(img.x * DEDUP_QUANT);
                int yq = Mathf.RoundToInt(img.y * DEDUP_QUANT);
                (int,int,int) key = (m, xq, yq);
                if (seen.Contains(key)) continue;
                seen.Add(key);
                int newIndex = pool.Count;
                pool.Add(new ImageNode { position = img, parentIndex = parentIdx, lastMirrorIndex = m });
                nextOut.Add(newIndex);
            }
        }
    }

    // 2つのフロンティア集合の間で接続可能なペアを探し、見つかれば検証してパスを返す（インデックスベース）
    private bool TryConnectLayers(List<int> sideA, List<int> sideB, Vector2 source, Vector2 target, ref int connectionChecks, MirrorGeo[] mirrorGeos, List<ImageNode> poolA, List<ImageNode> poolB, List<Vector2> pathOut)
    {
        foreach (int idxA in sideA)
        {
            ImageNode nodeA = poolA[idxA];
            foreach (int idxB in sideB)
            {
                ImageNode nodeB = poolB[idxB];
                connectionChecks++;

                Vector2 pa = nodeA.position;
                Vector2 pb = nodeB.position;
                if (!IsSegmentClear(pa, pb, obstacleLayerMask)) continue;

                // 末尾ミラーが同一なら、直近で同一鏡の連続反射になるため枝刈り
                if (nodeA.lastMirrorIndex >= 0 && nodeA.lastMirrorIndex == nodeB.lastMirrorIndex) continue;

                // シーケンスを親チェーンから遅延構築
                ReconstructSequence(poolA, idxA, _tmpSeqA); // root->A 順
                ReconstructSequence(poolB, idxB, _tmpSeqB); // root->B 順

                // 画像位置合成: pa に対し seqB を逆順で適用
                Vector2 combinedImage = pa;
                for (int k = _tmpSeqB.Count - 1; k >= 0; k--)
                {
                    MirrorGeo mg = mirrorGeos[_tmpSeqB[k]];
                    combinedImage = ReflectPointAcrossMirror(combinedImage, ref mg);
                }

                // 連結シーケンス: seqA + reverse(seqB)
                _tmpCombinedSeq.Clear();
                _tmpCombinedSeq.AddRange(_tmpSeqA);
                for (int k = _tmpSeqB.Count - 1; k >= 0; k--) _tmpCombinedSeq.Add(_tmpSeqB[k]);


                if (ValidateAndBuildPath(source, target, combinedImage, _tmpCombinedSeq, mirrorGeos, pathOut))
                {
                    return true;
                }
            }
        }
        return false;
    }
    #endregion

    #region Geometry Helpers
    // ミラー幾何キャッシュ用の軽量構造体
    private struct MirrorGeo
    {
        public Vector2 a; // StartPoint
        public Vector2 b; // EndPoint
        public Vector2 n; // 法線（正規化済み）
    }

    // 像のノード（親インデックス方式）
    private struct ImageNode
    {
        public Vector2 position;
        public int parentIndex;       // 親ノードのインデックス（rootは-1）
        public int lastMirrorIndex;   // 親→自ノードの間で使われた鏡インデックス（rootは-1）
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

    // キャッシュ済み幾何を使う反射
    private static Vector2 ReflectPointAcrossMirror(Vector2 p, ref MirrorGeo mg)
    {
        Vector2 ap = p - mg.a;
        float dist = Vector2.Dot(ap, mg.n);
        return p - 2f * dist * mg.n;
    }

    private bool IntersectLineWithMirror(Vector2 p1, Vector2 p2, Mirror2D mirror, out Vector2 hit)
    {
        return LineLineIntersection(p1, p2, mirror.StartPoint, mirror.EndPoint, out hit);
    }

    // キャッシュ済み幾何を使う交差判定（無限直線 vs 線分ライン）
    private bool IntersectLineWithMirror(Vector2 p1, Vector2 p2, ref MirrorGeo mg, out Vector2 hit)
    {
        return LineLineIntersection(p1, p2, mg.a, mg.b, out hit);
    }

    /// <summary>
    /// 半直線と線分の交差判定
    /// </summary>
    private static bool LineLineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        Vector2 r = p2 - p1;
        Vector2 s = p4 - p3;
        float rxs = r.x * s.y - r.y * s.x; // 0なら平行, 交点なし
        if (Mathf.Abs(rxs) < EPS_PARALLEL) return false;
        
        Vector2 qp = p3 - p1;
        float t = (qp.x * s.y - qp.y * s.x) / rxs;
        float u = (qp.x * r.y - qp.y * r.x) / rxs;

        // 線分チェック
        if (t >= 0 && u >= 0 && u <= 1){
            intersection = p1 + t * r;
            return true;
        }

        intersection = Vector2.zero;
        return false;
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

    /// <summary>
    /// ベクトルの反射計算
    /// </summary>
    private static Vector2 ReflectVector(Vector2 v, Vector2 normal)
    {
        Vector2 nn = normal.normalized;
        return v - 2f * Vector2.Dot(v, nn) * nn;
    }

    /// <summary>
    /// エッジ上に障害物がないかチェック
    /// </summary>
    private static bool IsSegmentClear(Vector2 a, Vector2 b, LayerMask mask)
    {
        Vector2 direction = b - a;

        int hitCount = Physics2D.RaycastNonAlloc(a, direction, _rayBuffer, direction.magnitude, mask);
        return hitCount == 0;
    }
    #endregion

    #region Validation / Path Build
    /// <summary>
    /// ターゲットから仮想光源へ向かう直線を、像生成の鏡列を逆順にたどって交点を求め、
    /// 各交点が線分上であること・反射則が成り立つこと・障害物に遮られないことを検証します。
    /// </summary>
    private bool ValidateAndBuildPath(Vector2 source, Vector2 target, Vector2 virtualEndpoint, List<int> sequence, MirrorGeo[] mirrorGeos, List<Vector2> builtPathOut)
    {
        if (!BuildFullPathPoints(source, target, virtualEndpoint, sequence, mirrorGeos, builtPathOut))
        {
            return false;
        }
        
        if (builtPathOut.Count < 2) return false;

        for (int i = 1; i < builtPathOut.Count - 1; i++)
        {
            Vector2 prev = builtPathOut[i - 1];
            Vector2 cur = builtPathOut[i];
            Vector2 next = builtPathOut[i + 1];
            int mirrorIndex = i - 1;
            if (mirrorIndex < 0 || mirrorIndex >= sequence.Count) return false;
            MirrorGeo mg = mirrorGeos[sequence[mirrorIndex]];
            Vector2 inc = (cur - prev).normalized;
            Vector2 n = mg.n;
            // 前面反射のみ許可
            if (Vector2.Dot(inc, n) > -EPS_FRONT) return false;
            Vector2 refl = ReflectVector(inc, n).normalized;
            Vector2 outv = (next - cur).normalized;
            if (Vector2.Dot(refl, outv) < 1f - EPS_REFLECT_MATCH) return false;
        }
        for (int i = 0; i < builtPathOut.Count - 1; i++)
        {
            if (!IsSegmentClear(builtPathOut[i], builtPathOut[i + 1], segmentCheckMask)){
                builtPathOut.Clear();
                return false;
            }
        }

        return true;
    }

    private bool BuildFullPathPoints(Vector2 source, Vector2 target, Vector2 virtualEndpoint, List<int> seq, MirrorGeo[] mirrorGeos, List<Vector2> pointsOut)
    {
        Vector2 lineFrom = target;
        Vector2 lineTo = virtualEndpoint;
        _tmpHitsReverse.Clear();
        for (int i = seq.Count - 1; i >= 0; i--)
        {
            MirrorGeo mg = mirrorGeos[seq[i]];
            if (!IntersectLineWithMirror(lineFrom, lineTo, ref mg, out Vector2 hit)) return false;
            if (!IsOnSegment(hit, mg.a, mg.b)) return false;
            _tmpHitsReverse.Add(hit);
            virtualEndpoint = ReflectPointAcrossMirror(virtualEndpoint, ref mg);
            lineFrom = ReflectPointAcrossMirror(lineFrom, ref mg);
            lineTo = virtualEndpoint;
        }
        pointsOut.Clear();
        pointsOut.Add(source);
        for (int i = _tmpHitsReverse.Count - 1; i >= 0; i--) pointsOut.Add(_tmpHitsReverse[i]);
        pointsOut.Add(target);
        return true;
    }

    // 親チェーンからシーケンス(root->node)を復元
    private void ReconstructSequence(List<ImageNode> pool, int nodeIndex, List<int> outSeq)
    {
        outSeq.Clear();
        int cur = nodeIndex;
        while (cur >= 0)
        {
            ImageNode n = pool[cur];
            if (n.lastMirrorIndex >= 0) outSeq.Add(n.lastMirrorIndex);
            cur = n.parentIndex;
        }
        outSeq.Reverse();
    }

    // ミラー幾何を1回だけ構築
    private MirrorGeo[] BuildMirrorGeometries()
    {
        int count = mirrors.Count;
        MirrorGeo[] geos = new MirrorGeo[count];
        for (int i = 0; i < count; i++)
        {
            Vector2 a = mirrors[i].StartPoint;
            Vector2 b = mirrors[i].EndPoint;
            Vector2 dir = b - a;
            Vector2 n;

            dir.Normalize();
            n = new Vector2(-dir.y, dir.x);
            geos[i] = new MirrorGeo { a = a, b = b, n = n };
        }
        return geos;
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
