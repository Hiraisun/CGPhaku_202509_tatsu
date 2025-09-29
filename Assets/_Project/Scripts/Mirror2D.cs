using UnityEngine;
using System;

/// <summary>
/// 2D の有限長ミラー（線分）を表すコンポーネント。
/// ローカル空間の始点/終点からワールド座標の線分を生成し、法線や可視化を提供します。
/// </summary>
public class Mirror2D : MonoBehaviour
{
    [SerializeField, Tooltip("ローカル空間における線分の始点（オブジェクト原点からのオフセット）")]
    private Vector2 startPointLocal;
    [SerializeField, Tooltip("ローカル空間における線分の終点（オブジェクト原点からのオフセット）")]
    private Vector2 endPointLocal;

    public Vector2 StartPoint => transform.TransformPoint(startPointLocal);
    public Vector2 EndPoint => transform.TransformPoint(endPointLocal);
    
    // イベント通知用のデリゲート
    public static event Action<Mirror2D> OnMirrorCreated;
    public static event Action<Mirror2D> OnMirrorDestroyed;
    
    /// <summary>
    /// 線分の法線ベクトル（向きは左右いずれか）。
    /// </summary>
    public Vector2 GetNormal(){
        Vector2 dir = EndPoint - StartPoint;
        Vector2 normal = new Vector2(-dir.y, dir.x).normalized;
        return normal;
    }
    
    void Awake()
    {
        // オブジェクトが生成された時にイベントを発火
        OnMirrorCreated?.Invoke(this);
    }
    
    void OnDestroy()
    {
        // オブジェクトが削除される時にイベントを発火
        OnMirrorDestroyed?.Invoke(this);
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(StartPoint, 0.05f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(EndPoint, 0.05f);
        
        // 法線を表示
        Gizmos.color = Color.blue;
        Vector2 center = (StartPoint + EndPoint) * 0.5f;
        Vector2 normalEnd = center + (GetNormal() * 0.3f);
        Gizmos.DrawLine(center, normalEnd);
    }
}
