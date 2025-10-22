using UnityEngine;
using System.Collections.Generic;
using EditorAttributes;

/// <summary>
/// 鏡配置状態の列挙型
/// </summary>
public enum PlacementState
{
    Disabled,       // 操作無効,演出中など
    Idle,           // 操作有効,待機中
    MirrorPlacing,  // 鏡配置中 (新規配置 or 方向変更)
}

/// <summary>
/// 鏡配置システム - プラットフォーム非依存
/// </summary>
public class MirrorPlacer : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject mirrorPrefab;
    [SerializeField] private GameObject mirrorPreviewPrefab;
    [SerializeField] private InputPromptsUI inputPromptsUI;
    
    // イベント
    public event System.Action OnPositionSet;
    public event System.Action OnMirrorPlaced;
    public event System.Action OnPlacementCancelled;
    
    // 内部状態
    // private IInput inputProvider;
    private Camera mainCamera;
    private List<GameObject> placedMirrors = new List<GameObject>();
    
    // 配置状態管理
    
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
        inputPromptsUI.SetInputPrompts(currentState);
    }

    public void DisablePlacement()
    {
        currentState = PlacementState.Disabled;
        inputPromptsUI.SetInputPrompts(currentState);
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0)){
            Vector2 position = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            if (currentState == PlacementState.Idle)
            {
                currentState = PlacementState.MirrorPlacing;
                inputPromptsUI.SetInputPrompts(currentState);
                // 位置決定
                placementPosition = position;
                previewMirrorObject.transform.position = position;
                OnPositionSet?.Invoke();
            }
            else if (currentState == PlacementState.MirrorPlacing){ // 配置確定----------------
                // 状態をリセット
                currentState = PlacementState.Idle;
                inputPromptsUI.SetInputPrompts(currentState);
                // 実際の鏡を作成
                PlaceMirror(placementPosition, position);
                OnMirrorPlaced?.Invoke();
                
            }
        }else if (Input.GetMouseButtonDown(1)){
            if (currentState == PlacementState.MirrorPlacing)
            {
                currentState = PlacementState.Idle;
                inputPromptsUI.SetInputPrompts(currentState);
                previewMirrorObject.transform.position = new Vector3(100, 100, 0);
                OnPlacementCancelled?.Invoke();
            }
        }

        // ホログラムミラー追従
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

    [SerializeField, Button("Mirror全削除")]
    void ClearAllMirrorsButton()
    {
        ClearAllMirrors();
    }
}