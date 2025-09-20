using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mirrorCountText;
    [SerializeField] private GameObject successPanel;
    [SerializeField] private GameObject failedPanel;
    [SerializeField] private GameObject retryButton;

    public event System.Action OnRetryRequested;

    public void UpdateMirrorCountText(int count)
    {
        mirrorCountText.text = new string('●', count);
    }

    public void ShowSuccessPanel(){
        successPanel.SetActive(true);
        retryButton.SetActive(true);
    }

    public void ShowFailedPanel(){
        failedPanel.SetActive(true);
        retryButton.SetActive(true);
    }


    public void OnRetryButtonClicked(){
        OnRetryRequested?.Invoke();
    }
}
