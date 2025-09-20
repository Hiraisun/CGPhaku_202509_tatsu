using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 鏡配置システム - プラットフォーム非依存
/// </summary>
public class MirrorPlacer : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject mirrorPrefab;
    [SerializeField] private GameObject mirrorPreviewPrefab;
    
    // イベント
    public event System.Action OnMirrorPlaced;
    public event System.Action OnPlacementCancelled;
    
    // 内部状態
    // private IInput inputProvider;
    private Camera mainCamera;
    private List<GameObject> placedMirrors = new List<GameObject>();
    
    // 配置状態管理
    private enum PlacementState
    {
        Disabled,       // 操作無効,演出中など
        Idle,           // 操作有効,待機中
        MirrorPlacing,    // 鏡配置中 (新規配置 or 方向変更)

    }
    
    private PlacementState currentState;
    private Vector2 placementPosition; // 設置中の位置（ワールド座標）
    private GameObject previewMirrorObject;

    public void Initialize()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        // プレビュー鏡、未使用時は遠くに置いておく
        previewMirrorObject = Instantiate(mirrorPreviewPrefab, new Vector3(100, 100, 0), Quaternion.identity);

        currentState = PlacementState.Idle;
    }

    public void DisablePlacement()
    {
        currentState = PlacementState.Disabled;
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0)){
            Vector2 position = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            if (currentState == PlacementState.Idle)
            {
                currentState = PlacementState.MirrorPlacing;
                // 位置決定
                placementPosition = position;
                previewMirrorObject.transform.position = position;
            }
            else if (currentState == PlacementState.MirrorPlacing){ // 配置確定----------------
                // 状態をリセット
                currentState = PlacementState.Idle;
                // 実際の鏡を作成
                PlaceMirror(placementPosition, position);
                
            }
        }else if (Input.GetMouseButtonDown(1)){
            if (currentState == PlacementState.MirrorPlacing)
            {
                currentState = PlacementState.Idle;
                previewMirrorObject.transform.position = new Vector3(100, 100, 0);
                OnPlacementCancelled?.Invoke();
            }
        }

        if (currentState == PlacementState.MirrorPlacing){
            Vector2 position = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Quaternion rotation = CalculateMirrorRotation(placementPosition, position);
            previewMirrorObject.transform.rotation = rotation;
        }
    }
    
    private void PlaceMirror(Vector2 from, Vector2 to)
    {
        Quaternion rotation = CalculateMirrorRotation(from, to);
        GameObject newMirror = Instantiate(mirrorPrefab, from, rotation);
        placedMirrors.Add(newMirror);

        previewMirrorObject.transform.position = new Vector3(100, 100, 0);

        // イベント発火
        OnMirrorPlaced?.Invoke();
        return;
    }

    private Quaternion CalculateMirrorRotation(Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        return Quaternion.Euler(0, 0, rotation);
    }
    
    
    public int GetPlacedMirrorCount()
    {
        return placedMirrors.Count;
    }
    
    public List<GameObject> GetPlacedMirrors()
    {
        return new List<GameObject>(placedMirrors);
    }

    public void ClearAllMirrors()
    {
        foreach (GameObject mirror in placedMirrors)
        {
            DestroyImmediate(mirror);
        }
        placedMirrors.Clear();
    }

    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(MirrorPlacer))]
    public class MirrorPlacerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            MirrorPlacer mirrorPlacer = (MirrorPlacer)target;
            UnityEditor.EditorGUILayout.Space();
            if (GUILayout.Button("Mirror全削除"))
            {
                mirrorPlacer.ClearAllMirrors();
                UnityEditor.EditorUtility.SetDirty(mirrorPlacer);
            }
        }
    }
    #endif
}