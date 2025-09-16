using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 鏡配置システム - プラットフォーム非依存
/// </summary>
public class MirrorPlacer : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject mirrorPrefab;
    
    // イベント
    public event System.Action OnMirrorPlaced;
    public event System.Action OnPlacementCancelled;
    
    // 内部状態
    private IInput inputProvider;
    private Camera mainCamera;
    private List<GameObject> placedMirrors = new List<GameObject>();
    
    // 配置状態管理
    private enum PlacementState
    {
        Idle,           // 待機中
        PositionSet,    // 位置決定済み（方向待ち）
    }
    
    private PlacementState currentState = PlacementState.Idle;
    private Vector2 placementPosition;
    private GameObject currentPreviewMirror;
    
    void Start()
    {
        // initialize
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        // 入力プロバイダーの取得
        inputProvider = GameManager.Instance.inputProvider;
        inputProvider.Initialize();
        
        // イベント購読
        inputProvider.OnPositionInput += HandlePositionInput;
        inputProvider.OnDirectionInput += HandleDirectionInput;
        inputProvider.OnCancelInput += HandleCancelInput;
        
        // 初期状態を設定
        inputProvider.SetPlacementState(PlacementPhase.Idle);
    }
    
    void Update()
    {
    }
    
    void OnDestroy()
    {
        // cleanup
        if (inputProvider != null)
        {
            inputProvider.OnPositionInput -= HandlePositionInput;
            inputProvider.OnDirectionInput -= HandleDirectionInput;
            inputProvider.OnCancelInput -= HandleCancelInput;
            inputProvider.Cleanup();
        }
    }
    
    
    private void HandlePositionInput(Vector2 position)
    {
        if (currentState == PlacementState.Idle)
        {
            // 位置決定
            placementPosition = position;
            currentState = PlacementState.PositionSet;
            inputProvider.SetPlacementState(PlacementPhase.PositionSet);
        }
    }
    
    private void HandleDirectionInput(Vector2 direction)
    {
        if (currentState == PlacementState.PositionSet)
        {
            // 回転決定（確定）
            // 方向から回転を計算
            Vector2 dir = direction - placementPosition;
            float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            // 実際の鏡を作成
            GameObject newMirror = Instantiate(mirrorPrefab, placementPosition, Quaternion.Euler(0, 0, rotation));
            placedMirrors.Add(newMirror);
            
            // 状態をリセット
            currentState = PlacementState.Idle;
            inputProvider.SetPlacementState(PlacementPhase.Idle);

            // イベント発火
            OnMirrorPlaced?.Invoke();
        }
    }
    
    private void HandleCancelInput()
    {
        if (currentState != PlacementState.Idle)
        {
            currentState = PlacementState.Idle;
            inputProvider.SetPlacementState(PlacementPhase.Idle);
            OnPlacementCancelled?.Invoke();
            Debug.Log("Placement cancelled");
        }
    }
    
    public int GetPlacedMirrorCount()
    {
        return placedMirrors.Count;
    }
    
    public List<GameObject> GetPlacedMirrors()
    {
        return new List<GameObject>(placedMirrors);
    }
    
    // スクリーン座標をワールド座標に変換
    private Vector2 ScreenToWorldPosition(Vector2 screenPos)
    {
        if (mainCamera == null) return Vector2.zero;
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, mainCamera.nearClipPlane));
    }
}