using UnityEngine;
using System.Collections.Generic;
using EditorAttributes;

public enum ControlState
{
    Disabled,       // 操作無効,演出中など
    // 鏡モード
    Mirror_Idle,     // 待機中
    Mirror_Placing,  // 位置決定済、方向選択中
    // 直線描画モード
    Ruler_Idle,      // 待機中
    Ruler_Placing,   // 始点決定済、終点選択中
    // 円描画モード
    Compass_Idle,    // 待機中
    Compass_Placing, // 中心点決定済
    Compass_Radius,  // 描画中

}

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
    private ControlState currentState;
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

        currentState = ControlState.Mirror_Idle;
        inputPromptsUI.SetInputPrompts(currentState);
    }

    public void DisablePlacement()
    {
        currentState = ControlState.Disabled;
        inputPromptsUI.SetInputPrompts(currentState);
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0)){ // 左クリック : 鏡位置決定、方向選択
            Vector2 position = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            if (currentState == ControlState.Mirror_Idle){
                currentState = ControlState.Mirror_Placing;
                inputPromptsUI.SetInputPrompts(currentState);
                // 位置決定
                placementPosition = position;
                previewMirrorObject.transform.position = position;
                OnPositionSet?.Invoke();
            }else if (currentState == ControlState.Mirror_Placing){ // 配置確定----------------
                // 状態をリセット
                currentState = ControlState.Mirror_Idle;
                inputPromptsUI.SetInputPrompts(currentState);
                // 実際の鏡を作成
                PlaceMirror(placementPosition, position);
                OnMirrorPlaced?.Invoke();
                
            }else if(currentState == ControlState.Ruler_Placing){
                
            }
        }else if (Input.GetMouseButtonDown(1)){ // 右クリック : キャンセル操作など
            if (currentState == ControlState.Mirror_Placing)
            {
                currentState = ControlState.Mirror_Idle;
                inputPromptsUI.SetInputPrompts(currentState);
                previewMirrorObject.transform.position = new Vector3(100, 100, 0);
                OnPlacementCancelled?.Invoke();
            }
        }

        // ホログラムミラー追従
        if (currentState == ControlState.Mirror_Placing){
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



    public void Button_Mirror(){
        currentState = ControlState.Mirror_Idle;
        inputPromptsUI.SetInputPrompts(currentState);
    }
    public void Button_Ruler(){
        currentState = ControlState.Ruler_Idle;
        inputPromptsUI.SetInputPrompts(currentState);
    }
    public void Button_Compass(){
        currentState = ControlState.Compass_Idle;
        inputPromptsUI.SetInputPrompts(currentState);
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