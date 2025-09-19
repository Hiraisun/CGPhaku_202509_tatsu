using System.Collections.Generic;
using UnityEngine;

public class ClearLaserProjector : MonoBehaviour
{
    public void ProjectLaser(List<Vector2> path)
    {
        // レーザーを投影
        Debug.Log($"ProjectLaser: {path.Count}個の点を投影");
    }
}
