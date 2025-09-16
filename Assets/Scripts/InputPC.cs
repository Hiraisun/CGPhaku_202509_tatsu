using UnityEngine;

/// <summary>
/// PC用のマウス入力実装
/// </summary>
public class InputPC : MonoBehaviour, IInput
{
    [Header("Input Settings")]
    public float rotationSensitivity = 1.0f;
    
    // IInput実装
    public event System.Action<Vector2> OnPositionInput;
    public event System.Action<Vector2> OnDirectionInput;
    public event System.Action OnCancelInput;
    public bool IsInputActive { get; private set; }
    
    // 内部状態
    private Camera mainCamera;
    private PlacementPhase currentPhase = PlacementPhase.Idle;
    
    void Start()
    {
        Initialize();
    }
    
    void Update()
    {
        if (!IsInputActive) return;
        
        Vector2 mousePos = Input.mousePosition;
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        
        // 左クリック（位置決定/方向決定）
        if (Input.GetMouseButtonDown(0))
        {
            if (currentPhase == PlacementPhase.Idle)
            {
                OnPositionInput?.Invoke(worldPos);
            }
            else if (currentPhase == PlacementPhase.PositionSet)
            {
                OnDirectionInput?.Invoke(worldPos);
            }
        }
        
        // 右クリック（キャンセル）
        if (Input.GetMouseButtonDown(1))
        {
            OnCancelInput?.Invoke();
        }
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
    
    public void SetPlacementState(PlacementPhase phase)
    {
        currentPhase = phase;
    }
}
