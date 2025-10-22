using UnityEngine;

/// <summary>
/// ゲーム全体のフロー制御と難易度管理を行うシングルトン
/// </summary>
public class GameManager : MonoBehaviour
{
    // シングルトンインスタンス
    private static GameManager _instance;
    // getter property
    public static GameManager Instance {
        get {
            // インスタンスがまだ存在しない(未登録)場合
            if (_instance == null) {
                // シーン内の既存オブジェクトを探す
                _instance = FindFirstObjectByType<GameManager>();

                // それでも見つからない場合 -> 自動生成
                if (_instance == null) {
                    GameObject singletonObject = new(typeof(GameManager).Name);
                    _instance = singletonObject.AddComponent<GameManager>();
                }

                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }
    
    #region Game State
    public enum GameState { Playing, Menu }
    public GameState CurrentState { get; private set; } = GameState.Menu;
    public int currentDifficulty = 1;
    #endregion
    
    #region Lifecycle
    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
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