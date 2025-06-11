using UnityEngine;
using Firebase;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using Random = UnityEngine.Random;

using System;


public class AdditionGameManager : MonoBehaviour
{
    [SerializeField] private GameObject columnsPanel;
    [SerializeField] private GameObject columnPrefab;
    [SerializeField] private GameObject digitSlotPrefab;
    [SerializeField] private TMP_Text carryText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text scoreText;

    // ✅ Variables pour compter les réponses et tentatives
    public int correctAnswersCount = 0;
    public int incorrectAnswersCount = 0;
    public int totalAttempts = 0; // ✅ Nouvelle variable pour compter toutes les tentatives
    private const int PROBLEMS_TO_COMPLETE = 2;

    [SerializeField] private Button submitButton;
    [SerializeField] private EnemyGirlController enemyGirlController; // Reference to EnemyGirlController
    [SerializeField] private Slider timerSlider; // Timer progress bar
    [SerializeField] private AudioSource audioSource; // Audio component
    [SerializeField] private AudioClip greenLightSound; // Sound for Green Light
    [SerializeField] private AudioClip redLightSong; // Song for Red Light verification
    [SerializeField] private AudioClip correctSound; // Sound for correct answer
    [SerializeField] private AudioClip penaltySound; // Sound for penalty
    [SerializeField] private GameObject enemyGirlObject;

    private List<GameObject> columns = new List<GameObject>();
    private List<DigitSlot> digitSlots = new List<DigitSlot>();
    private int num1, num2, answer;
    private List<int> correctAnswerDigits;
    private List<int> userAnswerDigits;
    private List<int> carryValues;
    private int score = 0;
    private int currentGrade;
    private int numSlots;
    private int problemsSolved = 0; // ✅ Garde cette variable pour les problèmes correctement résolus
    private const int problemsToEnd = 5;
    private bool isGreenLight = true; // Start with Green Light
    private bool isRedLightActive = false;
    public bool IsRedLightActive() => isRedLightActive;
    private string currentUserId;

    private bool canSubmit = true; // Start with submit enabled for Verify
    private float greenTimeRemaining; // Track time for the timer
    private float redTimeRemaining; // Track time for Red Light

    // ✅ Variables Firebase
    private DatabaseReference dbReference;
    private string activeTestId;
    private string studentId;
    private int studentGrade;
    private long startTime;
    private long endTime;
    private const string gameKey = "Vertical Addition"; // Identifiant du jeu
    public TextMeshProUGUI SlotText;


    void Start()
    {
        carryText.text = "TEST 123";

        // ✅ Initialisation Firebase
        InitializeFirebase();

        studentId = PlayerPrefs.GetString("student_uid", "");
        currentGrade = int.Parse(PlayerPrefs.GetString("grade", "6"));
        studentGrade = currentGrade;

        // ✅ Récupérer l'ID du test actif et enregistrer l'heure de début
        activeTestId = PlayerPrefs.GetString("activeTestId", "");
        startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Debug.Log($"🎮 Début du jeu Addition - Grade: {studentGrade}, TestID: {activeTestId}");

        if (columnsPanel == null || columnPrefab == null || digitSlotPrefab == null || submitButton == null ||
            enemyGirlController == null || timerSlider == null || audioSource == null)
        {
            Debug.LogError("AdditionGameManager: One or more required references not assigned!");
            return;
        }

        if (digitSlotPrefab.GetComponent<DigitSlot>() == null)
        {
            Debug.LogError("AdditionGameManager: digitSlotPrefab does not have a DigitSlot component! Assigned prefab: " + digitSlotPrefab.name);
            return;
        }

        scoreText.text = "score: 0";
        submitButton.GetComponentInChildren<TMP_Text>().text = "Verify"; // Set initial button text
        submitButton.GetComponent<Image>().color = new Color(0.29f, 0.81f, 0.31f); // #4CAF50 green
        submitButton.onClick.AddListener(CheckAnswer);
        submitButton.interactable = true;

        enemyGirlController.TurnBack(); // Start turned (Green Light)
        Debug.Log("Start: EnemyGirl turned back for Green Light.");
        if (audioSource != null && greenLightSound != null) audioSource.PlayOneShot(greenLightSound);
        currentUserId = PlayerPrefs.GetString("CurrentUserId", "");
        GenerateProblem();
        StartCoroutine(GreenLightTimerCoroutine());
        Debug.Log("Start: Initialized game with Green Light.");
        enemyGirlObject.SetActive(true);
    }

    // ✅ Initialisation Firebase
    void InitializeFirebase()
    {
        try
        {
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
            Debug.Log("✅ Firebase Database initialisée avec succès");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erreur d'initialisation Firebase: {e.Message}");
        }
    }

    void GenerateProblem()
    {
        // Generate num1 and num2 based on grade
        switch (currentGrade)
        {
            case 1:
                num1 = Random.Range(0, 10);
                num2 = Random.Range(0, 10 - num1);
                break;
            case 2:
                num1 = Random.Range(0, 10);
                num2 = Random.Range(0, 10 - num1);
                break;
            case 3:
                num1 = Random.Range(10, 50);
                num2 = Random.Range(10, 99 - num1);
                break;
            case 4:
                int units1 = Random.Range(5, 10);
                int units2 = Random.Range(5, 10);
                num1 = Random.Range(10, 50) + units1;
                num2 = Random.Range(10, 50) + units2;
                break;
            case 5:
                units1 = Random.Range(5, 10);
                units2 = Random.Range(5, 10);
                int tens1 = Random.Range(5, 10);
                int tens2 = Random.Range(5, 10);
                num1 = Random.Range(100, 500) + tens1 * 10 + units1;
                num2 = Random.Range(100, 500) + tens2 * 10 + units2;
                break;
            case 6:
                units1 = Random.Range(5, 10);
                units2 = Random.Range(5, 10);
                tens1 = Random.Range(5, 10);
                tens2 = Random.Range(5, 10);
                int hundreds1 = Random.Range(1, 5);
                int hundreds2 = Random.Range(1, 5);
                num1 = hundreds1 * 100 + tens1 * 10 + units1;
                num2 = hundreds2 * 100 + tens2 * 10 + units2;
                break;
        }

        answer = num1 + num2;

        // Debug the values
        Debug.Log($"GenerateProblem: num1 = {num1}, num2 = {num2}, answer = {answer}");

        // Calculate numSlots based on the answer's digit count
        int answerDigitCount = answer == 0 ? 1 : Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(answer)) + 1);
        int minSlots = currentGrade == 2 ? 1 : (currentGrade == 3 || currentGrade == 4) ? 2 : (currentGrade == 5) ? 3 : 4;
        numSlots = Mathf.Max(minSlots, answerDigitCount);

        Debug.Log($"GenerateProblem: answerDigitCount = {answerDigitCount}, minSlots = {minSlots}, numSlots = {numSlots}");

        correctAnswerDigits = new List<int>();
        int tempAnswer = answer;
        while (tempAnswer > 0 || correctAnswerDigits.Count < numSlots)
        {
            correctAnswerDigits.Insert(0, tempAnswer % 10);
            tempAnswer /= 10;
        }
        while (correctAnswerDigits.Count < numSlots) correctAnswerDigits.Insert(0, 0);

        Debug.Log($"GenerateProblem: correctAnswerDigits = {string.Join(", ", correctAnswerDigits)}");

        // Clear existing columns and slots
        foreach (var column in columns)
        {
            if (column != null)
                Destroy(column);
        }
        columns.Clear();
        digitSlots.Clear();

        userAnswerDigits = new List<int>(new int[numSlots]);
        carryValues = new List<int>(new int[numSlots]);

        // Extract digits for num1 and num2 (left-to-right order, no Reverse)
        List<int> num1DigitList = num1.ToString().PadLeft(numSlots, '0').Select(c => int.Parse(c.ToString())).ToList();
        List<int> num2DigitList = num2.ToString().PadLeft(numSlots, '0').Select(c => int.Parse(c.ToString())).ToList();

        Debug.Log($"GenerateProblem: num1DigitList = {string.Join(", ", num1DigitList)}, num2DigitList = {string.Join(", ", num2DigitList)}");

        // Create columns and slots
        for (int i = 0; i < numSlots; i++)
        {
            Debug.Log($"Creating column and slot for index {i}");
            // Instantiate a column
            GameObject columnObj = Instantiate(columnPrefab, columnsPanel.transform);
            columns.Add(columnObj);

            // Set num1 digit
            Transform num1DigitTransform = columnObj.transform.Find("Num1Digit");
            if (num1DigitTransform != null)
            {
                TMP_Text num1DigitText = num1DigitTransform.GetComponent<TMP_Text>();
                num1DigitText.text = num1DigitList[i].ToString();
            }

            // Set num2 digit with "+" on the leftmost digit
            Transform num2DigitTransform = columnObj.transform.Find("Num2Group/Num2Digit");
            if (num2DigitTransform != null)
            {
                TMP_Text num2DigitText = num2DigitTransform.GetComponent<TMP_Text>();
                num2DigitText.text = num2DigitList[i].ToString();
            }

            // Toggle '+' symbol visibility - CHANGED: Show on leftmost column (i == 0)
            Transform plusSymbolTransform = columnObj.transform.Find("Num2Group/PlusSymbol");
            if (plusSymbolTransform != null)
            {
                plusSymbolTransform.gameObject.SetActive(i == 0); // Show on leftmost column (first column)
            }

            // Replace SlotPlaceholder with DigitSlot
            Transform slotPlaceholder = columnObj.transform.Find("SlotPlaceholder");
            if (slotPlaceholder != null)
            {
                GameObject slotObj = Instantiate(digitSlotPrefab, slotPlaceholder.parent);
                slotObj.transform.SetSiblingIndex(slotPlaceholder.GetSiblingIndex());
                Destroy(slotPlaceholder.gameObject);

                RectTransform slotRect = slotObj.GetComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(100, 100); // Increased to 100x100 pixels
                slotRect.localScale = Vector3.one; // Ensure no scaling issues
                slotRect.anchoredPosition = Vector2.zero; // Reset position relative to parent

                DigitSlot slot = slotObj.GetComponent<DigitSlot>();
                if (slot == null)
                {
                    Debug.LogError("AdditionGameManager: DigitSlot component not found on instantiated prefab!");
                    continue;
                }
                if (slot.SlotText == null)
                {
                    Debug.LogError($"AdditionGameManager: SlotText not assigned on DigitSlot {i}!");
                    continue;
                }
                // slotIndex should reflect right-to-left order for addition logic
                slot.slotIndex = numSlots - 1 - i; // Rightmost column (i = numSlots - 1) is slotIndex 0 (units)
                digitSlots.Add(slot);
                userAnswerDigits[numSlots - 1 - i] = -1;
            }
        }

        Debug.Log($"GenerateProblem: Created {columns.Count} columns and {digitSlots.Count} slots");

        carryText.text = "";
        feedbackText.text = "";
        carryValues = new List<int>(new int[numSlots]);
        submitButton.interactable = false;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(columnsPanel.GetComponent<RectTransform>());
        if (columnsPanel.GetComponent<HorizontalLayoutGroup>() != null)
        {
            columnsPanel.GetComponent<HorizontalLayoutGroup>().CalculateLayoutInputHorizontal();
            columnsPanel.GetComponent<HorizontalLayoutGroup>().SetLayoutHorizontal();
        }
        StartCoroutine(DelayedLayoutUpdate());
    }

    IEnumerator DelayedLayoutUpdate()
    {
        yield return new WaitForEndOfFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(columnsPanel.GetComponent<RectTransform>());
    }

    IEnumerator GreenLightTimerCoroutine()
    {
        greenTimeRemaining = 25f; //Fixed 15 seconds for Green Light
        timerSlider.value = 1f; // Start full
        timerSlider.GetComponentInChildren<Image>().color = Color.green; // Green for Green Light
        while (greenTimeRemaining > 0 && isGreenLight)
        {
            greenTimeRemaining -= Time.deltaTime;
            timerSlider.value = greenTimeRemaining / 25f;
            yield return null;
        }
        if (isGreenLight) // Time ran out
        {
            feedbackText.text = "Time's Up! Too Slow!";
            feedbackText.color = Color.red;
            if (audioSource != null && penaltySound != null) audioSource.PlayOneShot(penaltySound);
            score = Mathf.Max(0, score - 1);
            scoreText.text = $"Score: {score}";
            StartCoroutine(RestartProblem());
        }
    }

    private IEnumerator RedLightTimerCoroutine()
    {
        redTimeRemaining = 4f;
        isRedLightActive = true;

        timerSlider.value = 1f;
        timerSlider.GetComponentInChildren<Image>().color = Color.red;

        // ✅ Make sure she starts facing forward
        if (enemyGirlController != null)
        {
            enemyGirlController.TurnForward();
        }

        while (redTimeRemaining > 0 && !isGreenLight)
        {
            redTimeRemaining -= Time.deltaTime;
            timerSlider.value = redTimeRemaining / 4f;

            // ✅ Keep her forward during red
            if (enemyGirlController != null)
            {
                enemyGirlController.EnsureForwardDuringRed();
            }

            yield return null;
        }

        isRedLightActive = false;

        if (!isGreenLight)
        {
            Debug.Log("Red Light: Time ran out, resetting to Green Light.");
            feedbackText.text = "Time's Up! Verification Failed!";
            feedbackText.color = Color.red;

            if (audioSource != null && penaltySound != null)
                audioSource.PlayOneShot(penaltySound);

            score = Mathf.Max(0, score - 1);
            scoreText.text = $"score: {score}";
            StartCoroutine(RestartProblem());
        }
    }

    public void UpdateCarry(int slotIndex, int digit)
    {
        Debug.Log($"🔍 UpdateCarry called: slotIndex={slotIndex}, digit={digit}");

        if (slotIndex < 0 || slotIndex >= digitSlots.Count)
        {
            Debug.LogError($"❌ SlotIndex invalide: {slotIndex}, numSlots: {digitSlots.Count}");
            return;
        }

        // Mettre à jour userAnswerDigits avec la nouvelle valeur
        userAnswerDigits[slotIndex] = digit;
        Debug.Log($"📝 UserAnswerDigits mis à jour: [{string.Join(", ", userAnswerDigits)}]");

        // Réinitialiser tous les carries
        for (int i = 0; i < carryValues.Count; i++)
        {
            carryValues[i] = 0;
        }

        // Recalculer tous les carries de droite à gauche (depuis les unités = index 0)
        for (int i = 0; i < numSlots; i++)
        {
            // Obtenir les chiffres de num1 et num2 pour cette position
            // Position 0 = unités, Position 1 = dizaines, etc.
            int columnValue = (int)Mathf.Pow(10, i);
            int digit1 = (num1 / columnValue) % 10;
            int digit2 = (num2 / columnValue) % 10;

            Debug.Log($"🧮 Position {i} (10^{i}): num1 digit={digit1}, num2 digit={digit2}");

            // Calculer la somme pour cette colonne
            int sum = digit1 + digit2;

            // Ajouter le carry de la colonne précédente (plus à droite)
            if (i > 0)
            {
                sum += carryValues[i - 1];
            }

            Debug.Log($"🧮 Position {i}: sum={sum} (avant carry: {digit1}+{digit2}{(i > 0 ? $"+{carryValues[i - 1]}" : "")})");

            // Vérifier ce que l'utilisateur a entré dans ce slot
            int userDigit = userAnswerDigits[i];
            bool hasUserInput = userDigit != -1;

            Debug.Log($"👤 Position {i}: user digit={userDigit}, hasInput={hasUserInput}");

            if (hasUserInput)
            {
                int expectedDigit = sum % 10;
                Debug.Log($"✅ Position {i}: expected={expectedDigit}, user={userDigit}");

                // Si l'utilisateur a entré le bon chiffre
                if (userDigit == expectedDigit)
                {
                    // Calculer le carry pour la position suivante
                    carryValues[i] = sum / 10;
                    Debug.Log($"✅ Position {i}: CORRECT! carry généré = {carryValues[i]}");
                }
                else
                {
                    // Mauvaise réponse - arrêter le calcul des carries
                    Debug.Log($"❌ Position {i}: INCORRECT! Arrêt du calcul carry");
                    break;
                }
            }
            else
            {
                // Pas d'entrée - arrêter le calcul
                Debug.Log($"⭕ Position {i}: Pas d'entrée, arrêt du calcul carry");
                break;
            }
        }

        // Construire l'affichage du carry
        BuildCarryDisplay();

        // Vérifier si tous les slots sont remplis
        bool allSlotsFilled = userAnswerDigits.All(d => d != -1);
        submitButton.interactable = allSlotsFilled && canSubmit;

        Debug.Log($"🎯 Tous les slots remplis: {allSlotsFilled}, Submit enabled: {submitButton.interactable}");
    }

    private void BuildCarryDisplay()
    {
        string carryDisplay = "";

        // Pour l'affichage, on va de gauche à droite (positions hautes vers basses)
        // Mais on affiche le carry qui vient de la position précédente (plus à droite)
        for (int displayPos = numSlots - 1; displayPos >= 1; displayPos--)
        {
            // Le carry à afficher vient de la position displayPos-1
            int carryValue = carryValues[displayPos - 1];

            if (carryValue > 0)
            {
                carryDisplay += carryValue.ToString();
            }
            else
            {
                carryDisplay += " "; // Espace pour maintenir l'alignement
            }

            // Ajouter un espace entre les positions (sauf pour la dernière)
            if (displayPos > 1)
            {
                carryDisplay += "  "; // Double espace pour l'alignement
            }
        }

        carryText.text = carryDisplay;
        Debug.Log($"🔢 Carry display: '{carryDisplay}'");
        Debug.Log($"🔢 Carry values array: [{string.Join(", ", carryValues)}]");

        // Debug supplémentaire pour comprendre l'affichage
        for (int i = 0; i < carryValues.Count; i++)
        {
            if (carryValues[i] > 0)
            {
                Debug.Log($"🔢 Carry[{i}] = {carryValues[i]} (généré par position {i}, affiché au-dessus de position {i + 1})");
            }
        }
    }




    public void CheckAnswer()
    {
        if (isGreenLight) // Player clicked during Green Light — start Red Light
        {
            StopCoroutine(GreenLightTimerCoroutine());
            isGreenLight = false;
            canSubmit = true;

            // Activate main enemy girl object
            enemyGirlObject.SetActive(true);

            // Activate the child GameObject holding the EnemyGirlController if not already
            if (enemyGirlController != null && !enemyGirlController.gameObject.activeSelf)
            {
                enemyGirlController.gameObject.SetActive(true);
            }

            Debug.Log($"EnemyGirl activeSelf: {enemyGirlObject.activeSelf}, activeInHierarchy: {enemyGirlObject.activeInHierarchy}");
            Debug.Log($"enemyGirlController GameObject active? {enemyGirlController.gameObject.activeInHierarchy}");

            // ✅ Turn forward at Red Light start
            if (enemyGirlController != null)
            {
                enemyGirlController.TurnForward();
            }

            if (audioSource != null && redLightSong != null)
                audioSource.PlayOneShot(redLightSong); // 4-second red song

            submitButton.GetComponentInChildren<TMP_Text>().text = "Next";
            submitButton.GetComponent<Image>().color = new Color(1f, 0.34f, 0.13f); // orange

            // ✅ Start Red Light
            StartCoroutine(RedLightTimerCoroutine());
            return;
        }

        // ------------------------------------
        // Second click: Player submits answer during Red Light
        // ------------------------------------
        StopCoroutine(RedLightTimerCoroutine());

        // ✅ INCRÉMENTER LES TENTATIVES TOTALES DÈS LE DÉBUT
        totalAttempts++;
        Debug.Log($"✅ Tentative #{totalAttempts} effectuée");

        for (int i = 0; i < digitSlots.Count; i++)
        {
            if (digitSlots[i] == null || digitSlots[i].SlotText == null)
            {
                Debug.LogError($"AdditionGameManager: DigitSlot {i} or SlotText is null!");
                continue;
            }

            userAnswerDigits[i] = digitSlots[i].SlotText.text != "" ? int.Parse(digitSlots[i].SlotText.text) : -1;
            Debug.Log($"Slot {i} value: {userAnswerDigits[i]}");
        }

        if (!userAnswerDigits.All(d => d != -1))
        {
            Debug.LogWarning("Not all slots filled.");
            return;
        }

        int userAnswer = 0;
        for (int i = 0; i < userAnswerDigits.Count; i++)
            userAnswer += userAnswerDigits[i] * (int)Mathf.Pow(10, userAnswerDigits.Count - 1 - i);

        Debug.Log($"User Answer Calculated: {userAnswer} (from {string.Join(", ", userAnswerDigits)})");
        Debug.Log($"Correct Answer: {answer} (from {string.Join(", ", correctAnswerDigits)})");

        bool isAnswerCorrect = true;
        for (int i = 0; i < digitSlots.Count; i++)
        {
            if (userAnswerDigits[i] != correctAnswerDigits[i])
            {
                isAnswerCorrect = false;
                break;
            }
        }

        if (!isAnswerCorrect)
        {
            // ✅ Compter les réponses incorrectes
            incorrectAnswersCount++;

            feedbackText.text = "Wrong Answer! Try Again!";
            feedbackText.color = Color.red;

            if (audioSource != null && penaltySound != null)
                audioSource.PlayOneShot(penaltySound);

            scoreText.text = $"score: {score}";

            Debug.Log($"Réponses correctes: {correctAnswersCount}, Réponses incorrectes: {incorrectAnswersCount}");
            Debug.Log($"Tentatives totales: {totalAttempts}/{PROBLEMS_TO_COMPLETE}");

            // ✅ Vérifier si 5 tentatives sont atteintes (réponse incorrecte)
            if (totalAttempts >= PROBLEMS_TO_COMPLETE)
            {
                Debug.Log("Jeu terminé! 5 tentatives effectuées.");
                // ✅ Sauvegarder avant de changer de scène
                SaveResultToFirebase();
                UnityEngine.SceneManagement.SceneManager.LoadScene("Composition Addition");

                return;
            }

            StartCoroutine(RestartProblem());
            return;
        }

        // ✅ Correct answer
        correctAnswersCount++;
        problemsSolved++; // Compter les problèmes correctement résolus

        feedbackText.text = "Correct!";
        feedbackText.color = Color.green;

        if (audioSource != null && correctSound != null)
            audioSource.PlayOneShot(correctSound);

        if (userAnswer == answer)
        {
            score++;
            scoreText.text = $"score: {score}";
        }

        Debug.Log($"✅ Problème résolu correctement #{problemsSolved}");
        Debug.Log($"Réponses correctes: {correctAnswersCount}, Réponses incorrectes: {incorrectAnswersCount}");
        Debug.Log($"Tentatives totales: {totalAttempts}/{PROBLEMS_TO_COMPLETE}");

        // ✅ Vérifier si 5 tentatives sont atteintes (réponse correcte)
        if (totalAttempts >= PROBLEMS_TO_COMPLETE)
        {
            Debug.Log("Jeu terminé! 5 tentatives effectuées.");
            // ✅ Sauvegarder avant de changer de scène
            SaveResultToFirebase();
            StartCoroutine(WaitAndLoadNextScene());
        }
        else
        {
            StartCoroutine(NextProblem());
        }
    }

    // ✅ Méthode pour sauvegarder les résultats dans Firebase
    void SaveResultToFirebase()
    {
        if (string.IsNullOrEmpty(activeTestId))
        {
            Debug.LogError("❌ Impossible de sauvegarder: activeTestId est vide");
            return;
        }

        if (dbReference == null)
        {
            Debug.LogError("❌ Impossible de sauvegarder: Firebase Database non initialisée");
            return;
        }

        // Calculer le score en pourcentage basé sur les réponses correctes
        int scorePercentage = totalAttempts > 0 ? (int)((float)correctAnswersCount / totalAttempts * 100) : 0;
        endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Dictionary<string, object> resultData = new Dictionary<string, object>
        {
            { "score", scorePercentage },
            { "correctAnswers", correctAnswersCount },
            { "wrongAnswers", incorrectAnswersCount },
            { "totalQuestions", totalAttempts }, // Total des tentatives effectuées
            { "grade", studentGrade },
            { "startedAt", startTime },
            { "finishedAt", endTime },
        };

        DatabaseReference resultRef = dbReference.Child("testResults").Child(activeTestId).Child(studentId);

        Debug.Log($"💾 Sauvegarde des résultats Addition Game...");
        Debug.Log($"📊 Score: {scorePercentage}%, Correct: {correctAnswersCount}, Wrong: {incorrectAnswersCount}, Total: {totalAttempts}");

        resultRef.Child(gameKey).SetValueAsync(resultData).ContinueWithOnMainThread(saveTask =>
        {
            if (saveTask.IsCompletedSuccessfully)
            {
                Debug.Log($"✅ Résultat Addition Game enregistré pour {gameKey} (Grade {studentGrade})!");
                Debug.Log($"📈 Données sauvegardées: {scorePercentage}% - {correctAnswersCount}/{totalAttempts} correctes");
            }
            else
            {
                Debug.LogError($"❌ Erreur d'enregistrement Addition Game: {saveTask.Exception?.Message}");
            }
        });
    }

    // ✅ Coroutine pour attendre un peu avant de changer de scène
    IEnumerator WaitAndLoadNextScene()
    {
        yield return new WaitForSeconds(1.5f); // Attendre un peu pour que la sauvegarde se termine
        SceneManager.LoadScene("Composition Addition");
    }

    IEnumerator RestartProblem()
    {
        yield return new WaitForSeconds(2f);
        isGreenLight = true;
        canSubmit = true;

        enemyGirlController.TurnBack();

        submitButton.GetComponentInChildren<TMP_Text>().text = "Verify";
        submitButton.GetComponent<Image>().color = new Color(0.29f, 0.81f, 0.31f); // green
        if (audioSource != null && greenLightSound != null) audioSource.PlayOneShot(greenLightSound);
        GenerateProblem();
        StartCoroutine(GreenLightTimerCoroutine());
    }

    IEnumerator NextProblem()
    {
        yield return new WaitForSeconds(1f);
        isGreenLight = true;
        canSubmit = true;

        enemyGirlController.TurnBack(); // ✅ AFTER setting green light

        submitButton.GetComponentInChildren<TMP_Text>().text = "Verify";
        submitButton.GetComponent<Image>().color = new Color(0.29f, 0.81f, 0.31f); // green
        if (audioSource != null && greenLightSound != null) audioSource.PlayOneShot(greenLightSound);
        GenerateProblem();
        StartCoroutine(GreenLightTimerCoroutine());
    }
}