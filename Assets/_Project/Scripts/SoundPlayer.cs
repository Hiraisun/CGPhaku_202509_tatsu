using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip setPositionSound;
    [SerializeField] private AudioClip mirrorPlacedSound;
    [SerializeField] private AudioClip placementCancelledSound;

    [SerializeField] private AudioClip startSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip defeatSound;
    
    private AudioSource audioSource;

    public void Initialize()
    {
        audioSource = GetComponent<AudioSource>();

        InGameManager.Instance.mirrorPlacer.OnPositionSet += OnPositionSet;
        InGameManager.Instance.mirrorPlacer.OnMirrorPlaced += OnMirrorPlaced;
        InGameManager.Instance.mirrorPlacer.OnPlacementCancelled += OnPlacementCancelled;
        InGameManager.Instance.OnStateChanged += OnStateChanged;
        
        // ゲーム開始時の効果音を再生
        OnStart();
    }

    void OnDestroy()
    {
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.mirrorPlacer.OnPositionSet -= OnPositionSet;
            InGameManager.Instance.mirrorPlacer.OnMirrorPlaced -= OnMirrorPlaced;
            InGameManager.Instance.mirrorPlacer.OnPlacementCancelled -= OnPlacementCancelled;
            InGameManager.Instance.OnStateChanged -= OnStateChanged;
        }
    }

    private void OnStateChanged(InGameManager.InGameState state){
        switch (state){
            case InGameManager.InGameState.Playing:
                OnStart();
                break;
            case InGameManager.InGameState.Success:
                OnSuccess();
                break;
            case InGameManager.InGameState.Defeat:
                OnDefeat();
                break;
        }
    }


    private void OnPositionSet(){
        audioSource.PlayOneShot(setPositionSound);
    }

    private void OnMirrorPlaced(){
        audioSource.PlayOneShot(mirrorPlacedSound);
    }

    private void OnPlacementCancelled(){
        audioSource.PlayOneShot(placementCancelledSound);
    }

    private void OnStart(){
        audioSource.PlayOneShot(startSound);
    }

    private void OnSuccess(){
        audioSource.PlayOneShot(successSound);
    }

    private void OnDefeat(){
        audioSource.PlayOneShot(defeatSound);
    }


}
