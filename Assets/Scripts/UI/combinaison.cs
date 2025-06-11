using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Random = UnityEngine.Random;
using System;

public class combinaison : MonoBehaviour
{
    [SerializeField] public DigitSlot1 slot1;
    [SerializeField] public DigitSlot1 slot2;

    [SerializeField] public TextMeshProUGUI targetText;
    [SerializeField] public TextMeshProUGUI resultText;
    [SerializeField] public TextMeshProUGUI timerText;
    [SerializeField] public Button nextButton;

    private int totalQuestions = 5;
    private int currentQuestion = 0;
    private int correctAnswers = 0;
    private int totalPossibleCombinations = 0;
    private int totalAttempts = 0; // Total des tentatives effectuées
    private int correctAnswersCount = 0; // Réponses correctes
    private int incorrectAnswersCount = 0; // Réponses incorrectes

    private int score = 0;
    private int currentGrade;
    // ✅ Variables Firebase
    private DatabaseReference dbReference;
    private string activeTestId;
    private string studentId;
    private int studentGrade;
    private long startTime;
    private long endTime;
    private const string gameKey = "Composition Addition"; // Identifiant du jeu

    private int targetNumber;
    private List<(int, int)> possibleCombinations;
    private HashSet<string> triedCombinations = new HashSet<string>();
    private int attemptsForThisNumber = 0;
    private int maxAttemptsForThisNumber = 0;

    private float timePerAttempt = 30f;
    private float currentTime = 0f;
    private bool waitingForInput = false;
    private bool gameFinished = false;

    private List<QuestionResult> allResults = new List<QuestionResult>();
    private List<UserAttempt> currentAttempts = new List<UserAttempt>();

    [System.Serializable]
    public class UserAttempt
    {
        public int value1;
        public int value2;
        public bool isCorrect;
    }

    [System.Serializable]
    public class QuestionResult
    {
        public int targetNumber;
        public List<UserAttempt> attempts = new List<UserAttempt>();
    }

    void Start()
    {
        // ✅ Initialisation Firebase
        InitializeFirebase();

        studentId = PlayerPrefs.GetString("student_uid", "");
        activeTestId = PlayerPrefs.GetString("activeTestId", "");
        currentGrade = int.Parse(PlayerPrefs.GetString("grade", "2"));
        studentGrade = currentGrade;

        // Enregistrer l'heure de début
        startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(NextQuestion);
        nextButton.interactable = false;
        currentQuestion = 0;
        correctAnswers = 0;
        totalPossibleCombinations = 0;
        totalAttempts = 0;
        correctAnswersCount = 0;
        incorrectAnswersCount = 0;
        NextQuestion();
    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(dependencyTask =>
        {
            if (dependencyTask.IsCompletedSuccessfully)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("✅ Firebase initialisé avec succès pour Combinaison Game");
            }
            else
            {
                Debug.LogError($"❌ Erreur d'initialisation Firebase: {dependencyTask.Exception?.Message}");
            }
        });
    }

    void Update()
    {
        if (gameFinished) return;
        if (waitingForInput)
        {
            currentTime -= Time.deltaTime;
            timerText.text = $"Temps : {Mathf.CeilToInt(currentTime)}s";

            if (currentTime <= 0f)
            {
                waitingForInput = false;
                StartCoroutine(RegisterAttempt(false, 0, 0, true)); // Temps écoulé, tentative fausse
            }
            else
            {
                int value1, value2;
                bool valid1 = int.TryParse(slot1.slotText.text, out value1);
                bool valid2 = int.TryParse(slot2.slotText.text, out value2);

                if (valid1 && valid2)
                {
                    waitingForInput = false;
                    StartCoroutine(CheckCombination(value1, value2));
                }
            }
        }
    }

    void GenerateNewTarget()
    {
        // ✅ Génération selon la complexité basée sur studentGrade
        int minRange, maxRange;

        switch (studentGrade)
        {
            case 1: // Très facile
                minRange = 2;
                maxRange = 8;
                timePerAttempt = 45f; // Plus de temps
                break;
            case 2: // Facile
                minRange = 3;
                maxRange = 12;
                timePerAttempt = 40f;
                break;
            case 3: // Moyen-facile
                minRange = 4;
                maxRange = 16;
                timePerAttempt = 35f;
                break;
            case 4: // Moyen
                minRange = 5;
                maxRange = 20;
                timePerAttempt = 30f;
                break;
            case 5: // Difficile
                minRange = 6;
                maxRange = 25;
                timePerAttempt = 25f;
                break;
            case 6: // Très difficile
                minRange = 8;
                maxRange = 30;
                timePerAttempt = 20f; // Moins de temps
                break;
            default:
                minRange = 2;
                maxRange = 18;
                timePerAttempt = 30f;
                break;
        }

        targetNumber = Random.Range(minRange, maxRange + 1);
        targetText.text = "" + targetNumber;
        resultText.text = "";
        nextButton.interactable = false;
        triedCombinations.Clear();
        possibleCombinations = new List<(int, int)>();

        for (int i = 1; i <= targetNumber / 2; i++)
        {
            int j = targetNumber - i;
            if (j >= i)
            {
                possibleCombinations.Add((i, j));
            }
        }
        maxAttemptsForThisNumber = possibleCombinations.Count;
        totalPossibleCombinations += maxAttemptsForThisNumber;
        attemptsForThisNumber = 0;
        StartNextAttempt();
    }

    void StartNextAttempt()
    {
        if (attemptsForThisNumber >= maxAttemptsForThisNumber)
        {
            resultText.text = "Passe au nombre suivant !";
            nextButton.interactable = true;
            return;
        }
        currentTime = timePerAttempt;
        waitingForInput = true;
        nextButton.interactable = true; // Permettre de passer à tout moment
        ClearSlots();
    }

    IEnumerator CheckCombination(int value1, int value2)
    {
        totalAttempts++; // Incrémenter le total des tentatives

        string key = $"{Mathf.Min(value1, value2)}-{Mathf.Max(value1, value2)}";
        bool isCorrect = (value1 + value2 == targetNumber) && possibleCombinations.Exists(c => c.Item1 == Mathf.Min(value1, value2) && c.Item2 == Mathf.Max(value1, value2));

        if (isCorrect && !triedCombinations.Contains(key))
        {
            resultText.text = "Bravo !";
            triedCombinations.Add(key);
            correctAnswers++;
            correctAnswersCount++; // Incrémenter les réponses correctes
        }
        else if (!isCorrect)
        {
            resultText.text = "Essaie encore !";
            incorrectAnswersCount++; // Incrémenter les réponses incorrectes
        }
        else if (triedCombinations.Contains(key))
        {
            resultText.text = "Déjà essayé !";
            incorrectAnswersCount++; // Considérer comme incorrect
        }

        attemptsForThisNumber++;
        nextButton.interactable = true;

        yield return new WaitForSeconds(2f);
        resultText.text = "";
        StartNextAttempt();
    }

    IEnumerator RegisterAttempt(bool isCorrect, int value1, int value2, bool timeOut = false)
    {
        totalAttempts++; // Incrémenter le total des tentatives
        incorrectAnswersCount++; // Temps écoulé = incorrect

        if (timeOut)
            resultText.text = "Temps écoulé !";
        else
            resultText.text = "Faux !";

        attemptsForThisNumber++;
        nextButton.interactable = true;

        yield return new WaitForSeconds(2f);
        StartNextAttempt();
    }

    public void ClearSlots()
    {
        slot1.slotText.text = "";
        slot2.slotText.text = "";
    }

    public void NextQuestion()
    {
        if (gameFinished) return;
        currentQuestion++;
        if (currentQuestion > totalQuestions)
        {
            ShowFinalResult();
            return;
        }
        GenerateNewTarget();
    }

    void ShowFinalResult()
    {
        gameFinished = true;
        waitingForInput = false;
        timerText.text = "";
        targetText.text = "Fin du jeu !";
        resultText.text = $"Score : {correctAnswers} / {totalPossibleCombinations}";
        nextButton.interactable = false;

        // ✅ Sauvegarder les résultats dans Firebase
        SaveResultToFirebase();

        // Ici tu peux aussi afficher le détail des réponses si tu veux
        Debug.Log("Détail des réponses :");
        foreach (var q in allResults)
        {
            Debug.Log($"Cible : {q.targetNumber}");
            foreach (var att in q.attempts)
            {
                Debug.Log($"  {att.value1} + {att.value2} = {(att.isCorrect ? "Correct" : "Faux")}");
            }
        }
    }

    // ✅ Fonction de sauvegarde Firebase
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
        Debug.Log($"💾 Sauvegarde des résultats Combinaison Game...");
        Debug.Log($"📊 Score: {scorePercentage}%, Correct: {correctAnswersCount}, Wrong: {incorrectAnswersCount}, Total: {totalAttempts}");

        resultRef.Child(gameKey).SetValueAsync(resultData).ContinueWithOnMainThread(saveTask =>
        {
            if (saveTask.IsCompletedSuccessfully)
            {
                Debug.Log($"✅ Résultat Combinaison Game enregistré pour {gameKey} (Grade {studentGrade})!");
                Debug.Log($"📈 Données sauvegardées: {scorePercentage}% - {correctAnswersCount}/{totalAttempts} correctes");
            }
            else
            {
                Debug.LogError($"❌ Erreur d'enregistrement Combinaison Game: {saveTask.Exception?.Message}");
            }
        });
    }
}