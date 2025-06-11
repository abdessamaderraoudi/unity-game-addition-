using UnityEngine;
using TMPro;
using ArabicSupport;

public class ArabicTextAutoFixer : MonoBehaviour
{
    private TMP_Text tmpText;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();

        if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
        {
            tmpText.text = ArabicFixer.Fix(tmpText.text);
        }
    }
}
