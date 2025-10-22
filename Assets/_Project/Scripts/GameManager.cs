using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ゲーム全体の状態管理、フロー制御、イベント管理を行うシングルトン
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    #region Game State
    public enum GameState { Playing, Success, Defeat, Menu }
    public GameState CurrentState { get; private set; } = GameState.Menu;
    public int currentDifficulty = 1;

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
    public event System.Action<GameState> OnStateChanged;
    #endregion
    
    #region Unity Lifecycle
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitReset()
    {
        Debug.Log("GameManager: InitReset");
        Instance = null;
    }


    void Awake()
    {
        // シングルトンパターン
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // シーン読み込み完了イベントの購読
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // RandomGameSceneが読み込まれた場合のみ初期化を実行
        if (scene.name == "RandomGameScene")
        {
            InitializeBaseGame();
        }
    }
    
    public void OnStartButtonClicked()
    {
        currentDifficulty = 1;
        ChangeState(GameState.Playing);
        StartGame();
    }
    #endregion
    
    #region Game Initialization
    private void InitializeBaseGame()
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
        mirrorPlacer.OnMirrorPlaced += OnMirrorPlacedInternal;
        uiManager.OnRetryRequested += OnRetryRequestedInternal;

        // 初期化
        mirrorPlacer.Initialize();
        soundPlayer.Initialize();
        uiManager.UpdateDifficultyText(currentDifficulty);
        uiManager.UpdateMirrorCountText(5);

        randomStageGenerate.GenerateStage(currentDifficulty);

        Debug.Log("GameManager: InGame initialized");
    }
    #endregion
    
    #region State Management
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        
        Debug.Log($"GameState changed: {CurrentState} -> {newState}");
        
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        
    }
    
    private void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("RandomGameScene");
    }
    #endregion
    
    #region Game Logic
    private void OnMirrorPlacedInternal()
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
            ChangeState(GameState.Success);

            mirrorPlacer.DisablePlacement();
            testLasar.enableDebug = true;
            uiManager.ShowSuccessPanel();
        }
        else if (mirrorPlacer.GetPlacedMirrorCount() >= maxMirrors) // 敗北
        {
            ChangeState(GameState.Defeat);
            mirrorPlacer.DisablePlacement();
            testLasar.enableDebug = true;
            uiManager.ShowFailedPanel();
        }
    }

    // リトライ
    private void OnRetryRequestedInternal()
    {
        if (CurrentState == GameState.Success){
            if (currentDifficulty < 5) currentDifficulty++; //難易度上昇
        }else if (CurrentState == GameState.Defeat){
            if (currentDifficulty > 1) currentDifficulty--; //難易度下降
        }

        ChangeState(GameState.Playing);
        StartGame();
    }
    #endregion
}