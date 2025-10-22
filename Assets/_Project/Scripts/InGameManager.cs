using UnityEngine;

/// <summary>
/// RandomGameScene内のゲームロジックと状態管理を行うシングルトン
/// </summary>
public class InGameManager : MonoBehaviour
{
    public static InGameManager Instance { get; private set; }
    
    #region Game State
    public enum InGameState { Playing, Success, Defeat }
    public InGameState CurrentState { get; private set; } = InGameState.Playing;
    #endregion
    
    #region Game Configuration
    [Header("Game Settings")]
    public int maxMirrors = 5;
    #endregion
    
    #region System References
    [Header("System References")]
    public LightPathfinder pathfinder;
    public MirrorPlacer mirrorPlacer;
    public ClearLaserProjector clearLaserProjector;
    public RandomStageGenerate randomStageGenerate;
    public UIManager uiManager;
    public TestLasar testLasar;
    public SoundPlayer soundPlayer;
    #endregion
    
    #region Events
    public event System.Action<InGameState> OnStateChanged;
    #endregion
    
    #region Unity Lifecycle
    void Awake()
    {
        // シングルトンパターン（DontDestroyOnLoadなし）
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    void OnDestroy()
    {
        // イベントの購読解除
        if (mirrorPlacer != null)
        {
            mirrorPlacer.OnMirrorPlaced -= OnMirrorPlaced;
        }
        if (uiManager != null)
        {
            uiManager.OnRetryRequested -= OnRetryRequested;
        }

        // インスタンスのクリア
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion
    
    #region Game Initialization
    private void Initialize()
    {
        // システム参照の取得
        pathfinder = FindFirstObjectByType<LightPathfinder>();
        mirrorPlacer = FindFirstObjectByType<MirrorPlacer>();
        clearLaserProjector = FindFirstObjectByType<ClearLaserProjector>();
        randomStageGenerate = FindFirstObjectByType<RandomStageGenerate>();
        uiManager = FindFirstObjectByType<UIManager>();
        testLasar = FindFirstObjectByType<TestLasar>();
        soundPlayer = FindFirstObjectByType<SoundPlayer>();
        
        // イベントの購読
        mirrorPlacer.OnMirrorPlaced += OnMirrorPlaced;
        uiManager.OnRetryRequested += OnRetryRequested;

        // 初期化
        mirrorPlacer.Initialize();
        soundPlayer.Initialize();
        testLasar.Initialize();
        uiManager.UpdateDifficultyText(GameManager.Instance.currentDifficulty);
        uiManager.UpdateMirrorCountText(maxMirrors);

        randomStageGenerate.GenerateStage(GameManager.Instance.currentDifficulty);

        Debug.Log("InGameManager: Initialized");
    }
    #endregion
    
    #region State Management
    public void ChangeState(InGameState newState)
    {
        if (CurrentState == newState) return;
        
        Debug.Log($"InGameState changed: {CurrentState} -> {newState}");
        
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }
    #endregion
    
    #region Game Logic
    private void OnMirrorPlaced()
    {
        int mirrorCount = mirrorPlacer.GetPlacedMirrorCount();
        int remainMirrorCount = maxMirrors - mirrorCount;
        uiManager.UpdateMirrorCountText(remainMirrorCount);

        // 経路の再計算
        bool IsReachable = pathfinder.FindPath();
        
        // 勝利条件のチェック
        if (IsReachable)
        {
            clearLaserProjector.ProjectLaser(pathfinder.lastValidPath);
            ChangeState(InGameState.Success);

            mirrorPlacer.DisablePlacement();
            testLasar.enableDebug = true;
            uiManager.ShowSuccessPanel();
        }
        else if (mirrorPlacer.GetPlacedMirrorCount() >= maxMirrors) // 敗北
        {
            ChangeState(InGameState.Defeat);
            mirrorPlacer.DisablePlacement();
            testLasar.enableDebug = true;
            uiManager.ShowFailedPanel();
        }
    }

    // リトライ
    private void OnRetryRequested()
    {
        // GameManagerに難易度更新とシーン遷移を依頼
        GameManager.Instance.OnRetryFromInGame(CurrentState == InGameState.Success);
    }
    #endregion
}

