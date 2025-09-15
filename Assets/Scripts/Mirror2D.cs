using UnityEngine;

public class Mirror2D : MonoBehaviour
{
    [SerializeField]
    private Vector2 startPointLocal;
    [SerializeField]
    private Vector2 endPointLocal;

    public Vector2 StartPoint => transform.TransformPoint(startPointLocal);
    public Vector2 EndPoint => transform.TransformPoint(endPointLocal);
    
    public Vector2 GetNormal(){
        // 線分の法線ベクトルを計算して返す
        Vector2 dir = EndPoint - StartPoint;
        Vector2 normal = new Vector2(-dir.y, dir.x).normalized;
        return normal;
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
