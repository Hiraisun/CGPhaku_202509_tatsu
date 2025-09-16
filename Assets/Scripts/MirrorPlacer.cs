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
    
    // 内部状態
    private IInput inputProvider;
    private Camera mainCamera;
    private List<GameObject> placedMirrors = new List<GameObject>();
    
    void Start()
    {
        Initialize();
    }
    
    void OnDestroy()
    {
        Cleanup();
    }
    
    private void Initialize()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        // 入力プロバイダーの取得
        inputProvider = GameManager.Instance.inputProvider;
        inputProvider.OnInputReceived += HandleInput;
        

        Debug.Log("MirrorPlacer initialized");
    }
    
    private void Cleanup()
    {
        inputProvider.OnInputReceived -= HandleInput;
    }
    
    private void HandleInput(InputData inputData)
    {
        Debug.Log("HandleInput");
        switch (inputData.action)
        {
            case InputAction.ConfirmPlacement:
                ConfirmMirrorPlacement(inputData);
                break;
        }
    }
    
    private void ConfirmMirrorPlacement(InputData inputData)
    {
        // 実際の鏡を作成
        CreateActualMirror(inputData.worldPosition, inputData.rotation);
        
        // イベント発火
        OnMirrorPlaced?.Invoke();
        Debug.Log("Mirror placed successfully");
    }
    
    private void CreateActualMirror(Vector2 worldPosition, float rotation)
    {
        if (mirrorPrefab == null) return;
        
        GameObject newMirror = Instantiate(mirrorPrefab, worldPosition, Quaternion.Euler(0, 0, rotation));
        placedMirrors.Add(newMirror);
    }
    
    public int GetPlacedMirrorCount()
    {
        return placedMirrors.Count;
    }
    
    public List<GameObject> GetPlacedMirrors()
    {
        return new List<GameObject>(placedMirrors);
    }
}