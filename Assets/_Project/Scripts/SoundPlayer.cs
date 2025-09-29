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

        GameManager.Instance.mirrorPlacer.OnPositionSet += OnPositionSet;
        GameManager.Instance.mirrorPlacer.OnMirrorPlaced += OnMirrorPlaced;
        GameManager.Instance.mirrorPlacer.OnPlacementCancelled += OnPlacementCancelled;
        GameManager.Instance.OnStateChanged += OnStateChanged;
    }

    void OnDestroy()
    {
        GameManager.Instance.mirrorPlacer.OnPositionSet -= OnPositionSet;
        GameManager.Instance.mirrorPlacer.OnMirrorPlaced -= OnMirrorPlaced;
        GameManager.Instance.mirrorPlacer.OnPlacementCancelled -= OnPlacementCancelled;
        GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameManager.GameState state){
        switch (state){
            case GameManager.GameState.Playing:
                OnStart();
                break;
            case GameManager.GameState.Success:
                OnSuccess();
                break;
            case GameManager.GameState.Defeat:
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
