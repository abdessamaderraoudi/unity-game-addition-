using System.Collections;
using System.Collections.Generic;
using ArabicSupport;
using UnityEngine.UI;
using UnityEngine;

public class fixArabic : MonoBehaviour
{
    void Start()
    {
        Text textComponent = GetComponent<Text>();
        textComponent.text = ArabicFixer.Fix(textComponent.text);
    }

    void Update()
    {

    }
}
