using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mirrorCountText;


    public void UpdateMirrorCountText(int count)
    {
        mirrorCountText.text = new string('●', count);
    }
}
