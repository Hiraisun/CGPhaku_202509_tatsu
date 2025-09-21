using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mirrorCountText;
    [SerializeField] private TextMeshProUGUI difficultyText;

    [SerializeField] private GameObject successPanel;
    [SerializeField] private GameObject failedPanel;
    [SerializeField] private GameObject retryButton;

    public event System.Action OnRetryRequested;

    public void UpdateDifficultyText(int difficulty)
    {
        // ★★★☆☆ みたいな表記
        difficultyText.text = new string('★', difficulty) + new string('☆', 5 - difficulty);
        Debug.Log("DifficultyText: " + difficultyText.text);
    }

    public void UpdateMirrorCountText(int count)
    {
        // ●●●○○ みたいな表記
        mirrorCountText.text = new string('●', count) + new string('○', 5 - count);
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
