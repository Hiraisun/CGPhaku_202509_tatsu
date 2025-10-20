using UnityEngine;
using TMPro;

public class InputPromptsUI : MonoBehaviour
{
    [SerializeField] private GameObject LeftClickPrompt;
    [SerializeField] private GameObject MiddleClickPrompt;
    [SerializeField] private GameObject RightClickPrompt;
    
    [SerializeField] private TextMeshProUGUI LeftClickPromptText;
    [SerializeField] private TextMeshProUGUI MiddleClickPromptText;
    [SerializeField] private TextMeshProUGUI RightClickPromptText;

    public void SetInputPrompts(ControlState state){
        switch (state){
            case ControlState.Mirror_Idle:
                LeftClickPrompt.SetActive(true);
                LeftClickPromptText.text = "ミラー位置決定";
                MiddleClickPrompt.SetActive(false);
                RightClickPrompt.SetActive(false);
                break;
            case ControlState.Mirror_Placing:
                LeftClickPrompt.SetActive(true);
                LeftClickPromptText.text = "配置確定";
                MiddleClickPrompt.SetActive(false);
                RightClickPrompt.SetActive(true);
                RightClickPromptText.text = "キャンセル";
                break;
            case ControlState.Disabled:
                LeftClickPrompt.SetActive(false);
                MiddleClickPrompt.SetActive(false);
                RightClickPrompt.SetActive(false);
                break;
        }
    }
}
