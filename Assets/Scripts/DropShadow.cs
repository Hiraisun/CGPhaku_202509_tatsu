using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DropShadow : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer parentRenderer;

    [SerializeField] private Vector3 shadowOffset = new(-0.1f, -0.1f, 0f);
    [SerializeField] private Color shadowColor = new(0, 0, 0, 1f);

    private GameObject shadowObject;
    private SpriteRenderer shadowRenderer;

    void Start()
    {
        parentRenderer.sortingLayerName = "Objects";

        shadowObject = new("Shadow");
        shadowObject.transform.parent = transform;
        shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
  
        shadowObject.transform.position = transform.position + shadowOffset;
        shadowObject.transform.localRotation = Quaternion.identity;
        shadowObject.transform.localScale = Vector3.one;

        shadowRenderer.sprite = parentRenderer.sprite;
        shadowRenderer.color = shadowColor;
        shadowRenderer.sortingLayerName = "Shadows";
        shadowRenderer.sortingOrder = parentRenderer.sortingOrder;
    }

    void OnValidate()
    {
        parentRenderer = GetComponent<SpriteRenderer>();

    }
}
