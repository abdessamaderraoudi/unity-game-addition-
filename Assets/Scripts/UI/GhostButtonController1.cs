using UnityEngine;
using UnityEngine.UI;

public class GhostButtonController1 : MonoBehaviour
{
    public static GhostButtonController1 Instance;

    public Text text;
    private RectTransform rect;
    private CanvasGroup canvasGroup;

    public string CurrentSymbol { get; private set; } = "";

    void Awake()
    {
        Instance = this;
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (text == null) text = GetComponentInChildren<Text>();
        Hide();
    }

    public void Show(string symbol, Vector2 screenPosition)
    {
        CurrentSymbol = symbol;
        text.text = symbol;
        canvasGroup.alpha = 1f;
        rect.position = screenPosition;
    }

    public void Move(Vector2 screenPosition)
    {
        rect.position = screenPosition;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        CurrentSymbol = "";
    }
}
