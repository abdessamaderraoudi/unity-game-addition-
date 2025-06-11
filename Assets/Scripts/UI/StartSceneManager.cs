using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject playButton;

    void Start()
    {
        if (playButton == null)
        {
            Debug.LogError("StartSceneManager: Play button reference not assigned!");
            return;
        }

        playButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(StartGame);
    }

    void StartGame()
    {
        SceneManager.LoadScene("TestScene");
    }
}