using UnityEngine;

/// <summary>
/// プラットフォーム非依存の入力インターフェース
/// </summary>
public interface IInput
{
    event System.Action<InputData> OnInputReceived;
    void Initialize();
    void Cleanup();
    bool IsInputActive { get; }
}

/// <summary>
/// プラットフォーム非依存の入力イベント定義
/// </summary>
public enum InputAction
{
    None,
    ConfirmPlacement,  // 配置確定
}

/// <summary>
/// 入力データの構造体
/// </summary>
[System.Serializable]
public struct InputData
{
    public Vector2 worldPosition;      // ワールド座標での位置
    public Vector2 screenPosition;     // スクリーン座標での位置
    public float rotation;             // 回転角度（度）
    public InputAction action;         // 実行するアクション
    
    public InputData(Vector2 worldPos, Vector2 screenPos, InputAction act, int type = 0, float rot = 0f, bool valid = false)
    {
        worldPosition = worldPos;
        screenPosition = screenPos;
        rotation = rot;
        action = act;
    }
}