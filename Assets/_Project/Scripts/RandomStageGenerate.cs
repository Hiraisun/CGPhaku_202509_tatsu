using UnityEngine;
using System.Collections.Generic;
using EditorAttributes;

public class RandomStageGenerate : MonoBehaviour
{
    [SerializeField] private GameObject asteroidPrefab;
    [SerializeField] private Vector2 stageSize = new(10, 10);
    [SerializeField] private List<Vector3> safeAreas = new(); // x, y, radius

    [Header("Test Parameter")]
    [SerializeField] private int asteroidCount = 10;
    [SerializeField] private Vector2 asteroidSizeRange = new(1, 2);
    private List<GameObject> asteroids = new List<GameObject>();

    [SerializeField] private Collider2D directLineCollider;


    // 難易度ごとのパラメータ辞書
    private static readonly Dictionary<int, (int asteroidCount, Vector2 asteroidSizeRange)> difficultyParams = new()
    {
        { 1, (15, new Vector2(1, 3)) },
        { 2, (20, new Vector2(1, 3)) },
        { 3, (30, new Vector2(0.5f, 2)) },
        { 4, (35, new Vector2(0.5f, 2)) },
        { 5, (40, new Vector2(0.5f, 2)) }
    };

    public void Initialize()
    {
        
    }

    public void GenerateStage(int difficulty)
    {
        ClearStage();

        (int asteroidCount, Vector2 asteroidSizeRange) = difficultyParams[difficulty];
        this.asteroidCount = asteroidCount;
        this.asteroidSizeRange = asteroidSizeRange;
        GenerateStage2();
    }

    private struct AsteroidData{
        public Vector2 pos;
        public float size;
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
    // 必ず直通ラインを遮るようにする
    public void GenerateStage2()
    {
        List<AsteroidData> asteroidDataList = new();

        // 直通ラインを遮るように生成
        bool occludedDirectLine = false;
        while (true){
            for (int i = 0; i < asteroidCount; i++)
            {
                AsteroidData asteroidData = new();
                // 位置決定
                do{
                    asteroidData.pos = new Vector2(Random.Range(-stageSize.x, stageSize.x), Random.Range(-stageSize.y, stageSize.y));
                } while (IsSafeArea(asteroidData.pos));
                // サイズ決定
                asteroidData.size = Mathf.Lerp(asteroidSizeRange.x, asteroidSizeRange.y, Mathf.PerlinNoise(asteroidData.pos.x, asteroidData.pos.y));

                asteroidDataList.Add(asteroidData);

                // 新たに直通ラインをさえぎったか?
                if (!occludedDirectLine){
                    RaycastHit2D hit = Physics2D.CircleCast(
                        asteroidData.pos, 
                        asteroidData.size*0.52f, 
                        Vector2.zero, 0, 
                        1 << directLineCollider.gameObject.layer
                    );
                    if (hit.collider != null){
                        occludedDirectLine = true;  
                    }
                }
            }

            if (occludedDirectLine){
                break;
            }else{
                Debug.Log("直通, 再生成");
                asteroidDataList.Clear();
            }
        }

        // 生成
        foreach (AsteroidData asteroidData in asteroidDataList)
        {
            GameObject asteroid = Instantiate(asteroidPrefab);
            asteroid.transform.position = new Vector3(asteroidData.pos.x, asteroidData.pos.y, 0);
            asteroid.transform.localScale = new Vector3(asteroidData.size, asteroidData.size, 1);
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

    /// <summary>
    /// 直接到達不可能かどうかをチェック
    /// </summary>
    private void CheckNotDirectlyReachable(){
        // Instantiate はフレーム最後に呼ばれるので、
        // 

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

    [SerializeField, Button("Generate Stage1")]
    void GenerateStage1Button() => GenerateStage1();
    [SerializeField, Button("Generate Stage2")]
    void GenerateStage2Button() => GenerateStage2();
    [SerializeField, Button("Clear Stage")]
    void ClearStageButton() => ClearStage();
}
