using UnityEngine;

/// <summary>
/// PC用のマウス入力実装
/// </summary>
public class InputPC : MonoBehaviour, IInput
{
    [Header("Input Settings")]
    public float rotationSensitivity = 1.0f;
    
    // IInput実装
    public event System.Action<InputData> OnInputReceived;
    public bool IsInputActive { get; private set; }
    
    // 内部状態
    private Camera mainCamera;
    
    void Start()
    {
        Initialize();
    }
    
    void Update()
    {
        HandleMouseInput();
    }
    
    void OnDestroy()
    {
        Cleanup();
    }
    
    public void Initialize()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        IsInputActive = true;
    }
    
    public void Cleanup()
    {
        IsInputActive = false;
    }
    
    private void HandleMouseInput()
    {
        if (!IsInputActive) return;
        
        Vector2 mousePos = Input.mousePosition;
        Vector2 worldPos = ScreenToWorldPosition(mousePos);
        
        // マウスボタンが押された時
        if (Input.GetMouseButtonDown(0))
        {
            OnMouseDown(mousePos, worldPos);
        }
    }
    
    private void OnMouseDown(Vector2 screenPos, Vector2 worldPos)
    {
        Debug.Log("OnMouseDown");
        var inputData = new InputData(
            worldPos,
            screenPos,
            InputAction.ConfirmPlacement
        );
        
        OnInputReceived?.Invoke(inputData);
    }
    
    private Vector2 ScreenToWorldPosition(Vector2 screenPos)
    {
        if (mainCamera == null) return Vector2.zero;
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, mainCamera.nearClipPlane));
    }
}
