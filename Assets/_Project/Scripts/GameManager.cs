using UnityEngine;

/// <summary>
/// ゲーム全体のフロー制御と難易度管理を行うシングルトン
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    #region Game State
    public enum GameState { Playing, Menu }
    public GameState CurrentState { get; private set; } = GameState.Menu;
    public int currentDifficulty = 1;
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
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    public void OnStartButtonClicked()
    {
        currentDifficulty = 1;
        ChangeState(GameState.Playing);
        StartGame();
    }
    #endregion
    
    #region State Management
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        
        Debug.Log($"GameState changed: {CurrentState} -> {newState}");
        
        CurrentState = newState;
    }
    
    private void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("RandomGameScene");
    }
    #endregion
    
    #region Retry Logic
    /// <summary>
    /// InGameManagerからのリトライリクエストを処理
    /// </summary>
    /// <param name="wasSuccess">成功後のリトライかどうか</param>
    public void OnRetryFromInGame(bool wasSuccess)
    {
        if (wasSuccess)
        {
            if (currentDifficulty < 5) currentDifficulty++; // 難易度上昇
        }
        else
        {
            if (currentDifficulty > 1) currentDifficulty--; // 難易度下降
        }

        ChangeState(GameState.Playing);
        StartGame();
    }
    #endregion
}