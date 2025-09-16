using UnityEngine;

/// <summary>
/// プラットフォーム非依存の入力インターフェース
/// </summary>
public interface IInput
{
    // 純粋な入力イベント（状態に関係なく発火）
    event System.Action<Vector2> OnPositionInput;
    event System.Action<Vector2> OnDirectionInput;
    event System.Action OnCancelInput;
    
    // 現在の状態を設定（MirrorPlacerが状態を教える）
    void SetPlacementState(PlacementPhase phase);
    
    // 初期化・クリーンアップ
    void Initialize();
    void Cleanup();
    bool IsInputActive { get; }
}

/// <summary>
/// 配置フェーズの定義
/// </summary>
public enum PlacementPhase
{
    Idle,           // 待機中
    PositionSet     // 位置決定済み
}