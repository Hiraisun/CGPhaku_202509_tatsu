using UnityEngine;
using System.Collections.Generic;

public class RandomStageGenerate : MonoBehaviour
{
    [SerializeField] private GameObject asteroidPrefab;
    [SerializeField] private Vector2 stageSize = new(10, 10);
    [SerializeField] private List<Vector3> safeAreas = new(); // x, y, radius

    [Header("Test Parameter")]
    [SerializeField] private int asteroidCount = 10;
    [SerializeField] private Vector2 asteroidSizeRange = new(1, 2);
    private List<GameObject> asteroids = new List<GameObject>();


    // 難易度ごとのパラメータ辞書
    private static readonly Dictionary<int, (int asteroidCount, Vector2 asteroidSizeRange)> difficultyParams = new()
    {
        { 1, (15, new Vector2(1, 3)) },
        { 2, (20, new Vector2(1, 3)) },
        { 3, (30, new Vector2(0.5f, 2)) },
        { 4, (35, new Vector2(0.5f, 2)) },
        { 5, (40, new Vector2(0.5f, 2)) }
    };

    public void GenerateStage(int difficulty)
    {
        ClearStage();

        (int asteroidCount, Vector2 asteroidSizeRange) = difficultyParams[difficulty];
        this.asteroidCount = asteroidCount;
        this.asteroidSizeRange = asteroidSizeRange;
        GenerateStage2();
    }

    // 案1: 完全ランダム
    public void GenerateStage1()
    {
        for (int i = 0; i < asteroidCount; i++)
        {
            GameObject asteroid = Instantiate(asteroidPrefab);
            Vector3 pos;

            do{
                pos = new Vector3(Random.Range(-stageSize.x, stageSize.x), Random.Range(-stageSize.y, stageSize.y), 0);
            } while (IsSafeArea(pos));

            asteroid.transform.position = pos;

            float size = Random.Range(asteroidSizeRange.x, asteroidSizeRange.y);
            asteroid.transform.localScale = new Vector3(size, size, 1);
            asteroids.Add(asteroid);
        }
    }

    // 案2: 位置ランダム、サイズはパーリンノイズ -> 採用
    public void GenerateStage2()
    {
        for (int i = 0; i < asteroidCount; i++)
        {
            GameObject asteroid = Instantiate(asteroidPrefab);

            Vector3 pos;
            do{
                pos = new Vector3(Random.Range(-stageSize.x, stageSize.x), Random.Range(-stageSize.y, stageSize.y), 0);
            } while (IsSafeArea(pos));
            asteroid.transform.position = pos;

            float size = Mathf.Lerp(asteroidSizeRange.x, asteroidSizeRange.y, Mathf.PerlinNoise(pos.x, pos.y));
            asteroid.transform.localScale = new Vector3(size, size, 1);

            asteroids.Add(asteroid);
        }
    }

    public void ClearStage()
    {
        foreach (GameObject asteroid in asteroids)
        {
            Destroy(asteroid);
        }
        asteroids.Clear();
    }

    private bool IsSafeArea(Vector2 pos)
    {
        foreach (Vector3 safeArea in safeAreas)
        {
            Vector2 safeAreaPos = safeArea;
            if ((pos - safeAreaPos).sqrMagnitude < safeArea.z * safeArea.z)
            {
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // ステージサイズをGizmoで表示（0,0中心）
        Gizmos.color = Color.yellow;
        Vector3 center = Vector3.zero;
        Vector3 size = new Vector3(stageSize.x*2, stageSize.y*2, 0.1f);
        Gizmos.DrawWireCube(center, size);

        // safe areas
        Gizmos.color = Color.green;
        foreach (Vector3 safeArea in safeAreas)
        {
            Gizmos.DrawWireSphere(new Vector3(safeArea.x, safeArea.y, 0), safeArea.z);
        }
    }

    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(RandomStageGenerate))]
    public class RandomStageGenerateEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            RandomStageGenerate randomStageGenerate = (RandomStageGenerate)target;
            if (GUILayout.Button("Generate Stage1"))
            {
                randomStageGenerate.GenerateStage1();
            }
            if (GUILayout.Button("Generate Stage2"))
            {
                randomStageGenerate.GenerateStage2();
            }
            if (GUILayout.Button("Clear Stage"))
            {
                randomStageGenerate.ClearStage();
            }
        }
    }
    #endif
}
