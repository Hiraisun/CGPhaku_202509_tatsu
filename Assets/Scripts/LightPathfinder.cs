using UnityEngine;
using System.Collections.Generic;

public class LightPathfinder : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public int maxReflections;
    public LayerMask obstacleLayerMask;
    public List<Mirror2D> mirrors = new();

    void Start()
    {
        FindPath();
    }

    void FindPath()
    {

    }
}
