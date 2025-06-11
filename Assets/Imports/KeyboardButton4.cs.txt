using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class KeyboardButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Text buttonText;
    public string symbol;

    [SerializeField] private Canvas dragCanvas;

    void Start()
    {
        if (buttonText != null)
            buttonText.text = symbol;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        GhostButtonController.Instance.Show(symbol, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        GhostButtonController.Instance.Move(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GhostButtonController.Instance.Hide();
    }
}
