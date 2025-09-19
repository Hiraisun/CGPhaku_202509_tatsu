using System.Collections.Generic;
using UnityEngine;

public class ClearLaserProjector : MonoBehaviour
{
    [SerializeField] private GameObject laserPrefab;
    private GameObject laserObject;
    private LineRenderer lineRenderer;

    [SerializeField] private GameObject startParticlePrefab;

    void Awake()
    {
        laserObject = Instantiate(laserPrefab, Vector3.zero, Quaternion.identity);
        lineRenderer = laserObject.GetComponent<LineRenderer>();
    }

    public void ProjectLaser(List<Vector2> path)
    {
        lineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            lineRenderer.SetPosition(i, path[i]);
        }

        GameObject startParticleObject = Instantiate(startParticlePrefab, path[0], Quaternion.identity);
        if (path.Count > 1)
        {
            startParticleObject.transform.right = (path[1] - path[0]);
        }
    }
}
