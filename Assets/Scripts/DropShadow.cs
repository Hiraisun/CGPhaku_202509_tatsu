using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DropShadow : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer parentRenderer;

    private Vector3 shadowOffset = new(-0.1f, -0.2f, 0f);
    [SerializeField] private Material shadowMaterial;

    private GameObject shadowObject;
    private SpriteRenderer shadowRenderer;

    void Start()
    {
        parentRenderer.sortingLayerName = "Objects";

        shadowObject = new("Shadow");
        shadowObject.transform.parent = transform;
        shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
  
        UpdateShadowPosition();
        shadowObject.transform.localRotation = Quaternion.identity;
        shadowObject.transform.localScale = Vector3.one;

        shadowRenderer.sprite = parentRenderer.sprite;
        shadowRenderer.material = shadowMaterial;
        shadowRenderer.sortingLayerName = "Shadows";
        shadowRenderer.sortingOrder = parentRenderer.sortingOrder;
    }

    void Update()
    {
        // 親の位置に追従
        UpdateShadowPosition();
    }

    private void UpdateShadowPosition()
    {
        shadowObject.transform.position = transform.position + shadowOffset;
        shadowObject.transform.rotation = transform.rotation;
    }

    void OnValidate()
    {
        parentRenderer = GetComponent<SpriteRenderer>();
    }
}
