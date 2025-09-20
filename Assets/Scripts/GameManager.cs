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
    #endregion
    
    #region Events
    public event System.Action<GameState> OnStateChanged;
    #endregion
    
    #region Unity Lifecycle
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
        ChangeState(GameState.Playing);
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
        // イベントの購読
        mirrorPlacer.OnMirrorPlaced += OnMirrorPlacedInternal;

        // 初期化
        mirrorPlacer.Initialize();

        // ステージ生成
        randomStageGenerate.GenerateStage(3);
        
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
        
        switch (newState)
        {
            case GameState.Playing:
                StartGame();
                break;
            case GameState.Success:
                break;
            case GameState.Defeat:
                break;
            case GameState.Menu:
                break;
        }
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
        }

        // 敗北条件
        if (mirrorPlacer.GetPlacedMirrorCount() >= maxMirrors)
        {
            ChangeState(GameState.Defeat);
            mirrorPlacer.DisablePlacement();
            testLasar.enableDebug = true;
        }
    }
    #endregion
}