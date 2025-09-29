using UnityEngine;

public class AsteroidAnim : MonoBehaviour
{
    [SerializeField] private Vector2 rotationSpeedRange = new(-10f, 10f);
    private float rotationSpeed;

    void Start()
    {
        rotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
    }

    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}
