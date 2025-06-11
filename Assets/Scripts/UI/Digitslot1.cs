using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DigitSlot1 : MonoBehaviour, IDropHandler
{
    public TMP_InputField slotText;

    void Start()
    {
        if (slotText == null)
            Debug.LogWarning("DigitSlot: slotText is not assigned!", this);
        else
            slotText.text = "";
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Drop detected on: " + gameObject.name);

        string symbol = GhostButtonController1.Instance.CurrentSymbol;
        if (!string.IsNullOrEmpty(symbol))
        {
            slotText.text += symbol;
            Debug.Log("Symbol Set: " + symbol);
        }
    }

}
