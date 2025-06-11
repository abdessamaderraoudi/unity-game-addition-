using UnityEngine;
using UnityEngine.UI;

public class SubmitButtonController : MonoBehaviour
{
    private AdditionGameManager gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<AdditionGameManager>();
        if (gameManager == null)
            Debug.LogError("SubmitButtonController: AdditionGameManager not found in scene!", this);

        GetComponent<Button>().onClick.AddListener(OnSubmit);
    }

    void OnSubmit()
    {
        if (gameManager != null)
            gameManager.CheckAnswer();
    }
}