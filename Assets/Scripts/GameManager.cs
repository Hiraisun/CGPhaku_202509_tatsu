using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ゲーム全体の状態管理、フロー制御、イベント管理を行うシングルトン
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    #region Game State
    public enum GameState { Playing, Victory, Defeat, Menu }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    public int MirrorsPlaced { get; private set; }
    public bool IsReachable { get; private set; }
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
    #endregion
    
    #region Events
    public event System.Action<GameState> OnStateChanged;
    public event System.Action OnMirrorPlaced; //鏡配置時のイベント
    public event System.Action OnReachabilityChanged; //経路変更時のイベント
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
            case GameState.Victory:
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
        MirrorsPlaced++;
        OnMirrorPlaced?.Invoke();
        
        // 経路の再計算
        if (pathfinder != null)
        {
            pathfinder.FindPath();
            bool wasReachable = IsReachable;
            IsReachable = pathfinder.lastReachable;
            
            if (IsReachable != wasReachable)
            {
                OnReachabilityChanged?.Invoke();
                Debug.Log($"Reachability changed: {IsReachable}");
            }
            
            // 勝利条件のチェック
            if (IsReachable)
            {
                ChangeState(GameState.Victory);
            }
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
        ChangeState(GameState.Victory);
    }
    
    [ContextMenu("Force Defeat")]
    private void ForceDefeat()
    {
        ChangeState(GameState.Defeat);
    }
    #endregion
}