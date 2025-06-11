using UnityEngine;
using UnityEngine.UI;

public class FloatAndFade : MonoBehaviour
{
    public float floatSpeed = 50f;
    public float fadeDuration = 2f;

    private float elapsedTime = 0f;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (rectTransform == null)
        {
            Debug.LogWarning("❌ Aucun RectTransform trouvé sur l'objet " + gameObject.name);
            enabled = false; // désactive ce script
            return;
        }
    }


    void Update()
    {
        elapsedTime += Time.deltaTime;

        // Monter verticalement
        rectTransform.anchoredPosition += new Vector2(0, floatSpeed * Time.deltaTime);

        // Réduire l’opacité progressivement
        canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);

        // Détruire après fade complet
        if (elapsedTime >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}
