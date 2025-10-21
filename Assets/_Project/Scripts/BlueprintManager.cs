using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlueprintManager : MonoBehaviour
{
    // 線分データ構造
    public struct LineSegment
    {
        public Vector2 start;
        public Vector2 end;
    }

    // 円弧データ構造
    public struct ArcData
    {
        public Vector2 center;
        public float radius;
        public float startAngle;
        public float endAngle;
    }

    [SerializeField] private Material lineMaterial;

    // 保持するデータリスト
    private List<LineSegment> lines = new();
    private List<ArcData> arcs = new();

    void Start(){
        lines.Add(new LineSegment{start = new Vector2(-1, -1), end = new Vector2(1, 1)});
        arcs.Add(new ArcData{center = new Vector2(0, 0), radius = 2, startAngle = 0, endAngle = 60});
    }

    void OnEnable(){
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable(){
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    // TODO:実装中
    void AddLine(Vector2 start, Vector2 end){
        Camera cam = Camera.main;
        if (cam == null) cam = Camera.current;
        if (cam != null)
        {
            Vector3 worldStart = cam.ScreenToWorldPoint(new Vector3(start.x, start.y, cam.nearClipPlane));
            Vector3 worldEnd = cam.ScreenToWorldPoint(new Vector3(end.x, end.y, cam.nearClipPlane));
            lines.Add(new LineSegment 
            {
                start = (Vector2)worldStart,
                end = (Vector2)worldEnd
            });
        }
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        lineMaterial.SetPass(0);
        GL.PushMatrix();
        
        
        // 線分の描画
        GL.Begin(GL.LINES);
        GL.Color(lineMaterial.color);
        foreach (var line in lines)
        {
            GL.Vertex(line.start);
            GL.Vertex(line.end);
        }
        GL.End();

        // 円弧の描画
        foreach (var arc in arcs)
        {
            GL.Begin(GL.LINE_STRIP);
            float step = 5f;  // 5°刻み
            int segments = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(arc.endAngle - arc.startAngle) / step));
            float angleStep = (arc.endAngle - arc.startAngle) / segments;
            for (int i = 0; i <= segments; i++){
                float angle = arc.startAngle + angleStep * i;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 point = arc.center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * arc.radius;
                GL.Vertex(point);}
            GL.End();
        }

        GL.PopMatrix();
    }

    
}
