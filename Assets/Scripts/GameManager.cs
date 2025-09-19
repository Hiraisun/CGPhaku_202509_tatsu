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
    public GameState CurrentState { get; private set; } = GameState.Playing;

    #endregion
    
    #region Game Configuration
    [Header("Game Settings")]
    public int maxMirrors = 5;
    #endregion
    
    #region System References
    [Header("System References")]
    public LightPathfinder pathfinder;
    public MirrorPlacer mirrorPlacer;
    public IInput inputProvider;
    public ClearLaserProjector clearLaserProjector;
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
    }
    
    void Start()
    {
        InitializeGame();
    }
    
    void Update()
    {

    }
    #endregion
    
    #region Game Initialization
    private void InitializeGame()
    {
        // システム参照の取得
        if (pathfinder == null)
            pathfinder = FindFirstObjectByType<LightPathfinder>();
        
        if (mirrorPlacer == null)
            mirrorPlacer = FindFirstObjectByType<MirrorPlacer>();
        
        // 入力プロバイダーの取得
        inputProvider = FindFirstObjectByType<InputPC>();
        
        // 初期状態の設定
        
        
        // イベントの購読
        mirrorPlacer.OnMirrorPlaced += OnMirrorPlacedInternal;
        
        Debug.Log("GameManager initialized");
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
        }
    }
    
    private void StartGame()
    {
        // ゲーム開始時の処理
    }
    #endregion
    
    #region Game Logic
    
    private void OnMirrorPlacedInternal()
    {
        
        // 経路の再計算
        bool IsReachable = pathfinder.FindPath();
        
        // 勝利条件のチェック
        if (IsReachable)
        {
            clearLaserProjector.ProjectLaser(pathfinder.lastValidPath);
            ChangeState(GameState.Success);
        }
    }
    #endregion
    
    #region Public Methods
    public void StartNewGame()
    {
        ChangeState(GameState.Playing);
    }
    #endregion
    
    #region Debug
    [ContextMenu("Force Victory")]
    private void ForceVictory()
    {
        ChangeState(GameState.Success);
    }
    
    [ContextMenu("Force Defeat")]
    private void ForceDefeat()
    {
        ChangeState(GameState.Defeat);
    }
    #endregion
}