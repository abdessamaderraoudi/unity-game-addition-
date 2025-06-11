using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Linq;

[System.Serializable]
public class Question
{
    public TextMeshProUGUI myText;
    public string question;
    public string[] choices;
    public int correctIndex;
    public string grade; // Nouveau champ pour le grade
    public string subject; // Optionnel: matière (math, français, etc.)
}

[System.Serializable]
public class QuestionDatabase
{
    public List<Question> questions;
}

public class QuizManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI scoreText;
    public Button[] choiceButtons;

    [Header("Quiz Configuration")]
    public TextAsset questionsJsonFile; // Glissez votre fichier JSON ici dans l'Inspector
    public int maxQuestionsPerQuiz = 5; // Nombre maximum de questions par quiz

    [Header("Quiz Data")]
    public List<Question> allQuestions = new List<Question>(); // Toutes les questions
    public List<Question> currentQuizQuestions = new List<Question>(); // Questions du quiz actuel

    [Header("Sound Effects")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip clickSound;

    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool answered = false;
    private bool quizInitialized = false;

    private DatabaseReference dbReference;
    private long startTime;

    private string studentId;
    private string linkedTeacherId;
    private string studentGrade; // Grade de l'étudiant
    private string activeTestId;
    private const string gameKey = "Problem Addition"; // change to game2 or game3 accordingly

    void Start()
    {
        startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        studentId = PlayerPrefs.GetString("student_uid", "");

        Debug.Log("🎮 Démarrage du QuizManager");
        Debug.Log("👤 Student ID: " + studentId);

        if (string.IsNullOrEmpty(studentId))
        {
            Debug.LogError("❌ Student ID non trouvé dans PlayerPrefs!");
            return;
        }

        // Charger les questions depuis le JSON
        LoadQuestionsFromJson();

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("✅ Firebase initialisé avec succès");
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                GetStudentTeacherAndFindTest();
            }
            else
            {
                Debug.LogError("❌ Firebase non disponible: " + task.Result);
            }
        });
    }

    void LoadQuestionsFromJson()
    {
        try
        {
            if (questionsJsonFile == null)
            {
                Debug.LogError("❌ Fichier JSON des questions non assigné!");
                CreateDefaultQuestions(); // Fallback vers les questions par défaut
                return;
            }

            string jsonContent = questionsJsonFile.text;
            Debug.Log("📄 Contenu JSON chargé: " + jsonContent.Substring(0, Mathf.Min(100, jsonContent.Length)) + "...");

            QuestionDatabase questionDB = JsonUtility.FromJson<QuestionDatabase>(jsonContent);

            if (questionDB != null && questionDB.questions != null)
            {
                allQuestions = questionDB.questions;
                Debug.Log($"✅ {allQuestions.Count} questions chargées depuis le JSON");

                // Afficher les grades disponibles
                var grades = allQuestions.Select(q => q.grade).Distinct().ToList();
                Debug.Log("📚 Grades disponibles: " + string.Join(", ", grades));
            }
            else
            {
                Debug.LogError("❌ Erreur lors du parsing du JSON");
                CreateDefaultQuestions();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Erreur lors du chargement du JSON: " + e.Message);
            CreateDefaultQuestions(); // Fallback vers les questions par défaut
        }
    }

    void GetStudentTeacherAndFindTest()
    {
        Debug.Log("🔍 Recherche des informations de l'étudiant...");

        dbReference.Child("users").Child(studentId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result.Exists)
            {
                var linkedTeacherValue = task.Result.Child("linkedTeacherId").Value;
                var gradeValue = task.Result.Child("schoolGrade").Value;

                if (linkedTeacherValue != null && gradeValue != null)
                {
                    linkedTeacherId = linkedTeacherValue.ToString().Trim();
                    studentGrade = gradeValue.ToString().Trim();

                    PlayerPrefs.SetString("linked_teacher_id", linkedTeacherId);
                    PlayerPrefs.SetString("grade", studentGrade);

                    Debug.Log($"👤 Grade de l'étudiant: {studentGrade}");

                    SearchForActiveTest();
                }
                else
                {
                    Debug.LogError("❌ linkedTeacherId ou grade non trouvé pour cet étudiant");
                }
            }
            else
            {
                Debug.LogError("❌ Étudiant non trouvé ou erreur Firebase: " + task.Exception?.Message);
            }
        });
    }

    void SearchForActiveTest()
    {
        Debug.Log("🔍 Recherche de tests actifs...");

        dbReference.Child("tests").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result.Exists)
            {
                Debug.Log("📚 Nombre de tests trouvés: " + task.Result.ChildrenCount);

                foreach (var test in task.Result.Children)
                {
                    var etatValue = test.Child("etatDeTest").Value;
                    var teacherValue = test.Child("teacherId").Value;

                    if (etatValue != null && teacherValue != null)
                    {
                        string etat = etatValue.ToString().Trim().ToLower();
                        string teacher = teacherValue.ToString().Trim();

                        Debug.Log($"🧪 TestID: {test.Key}");
                        Debug.Log($"🧪 TeacherID du test: {teacher}");
                        Debug.Log($"🧪 État du test: {etat}");
                        Debug.Log($"🧪 Professeur lié: {linkedTeacherId}");

                        if (etat == "actif" && teacher == linkedTeacherId)
                        {
                            activeTestId = test.Key;
                            PlayerPrefs.SetString("activeTestId", activeTestId);
                            PlayerPrefs.Save();
                            Debug.Log("✅ Test actif trouvé: " + activeTestId);
                            InitializeQuiz();
                            return;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Données manquantes pour le test {test.Key}");
                    }
                }

                Debug.LogWarning("⚠️ Aucun test actif trouvé pour ce professeur.");
                questionText.text = "Aucun test actif disponible pour le moment.";
            }
            else
            {
                Debug.LogError("❌ Erreur lors de la lecture des tests: " + task.Exception?.Message);
            }
        });
    }

    void InitializeQuiz()
    {
        if (quizInitialized)
        {
            Debug.LogWarning("⚠️ Quiz déjà initialisé!");
            return;
        }

        Debug.Log("🎯 Initialisation du quiz...");

        try
        {
            // Filtrer les questions par grade
            FilterQuestionsByGrade();

            // Vérifier que nous avons des questions pour ce grade
            if (currentQuizQuestions.Count == 0)
            {
                Debug.LogError($"❌ Aucune question disponible pour le grade {studentGrade}!");
                questionText.text = $"Aucune question disponible pour votre grade ({studentGrade}).";
                return;
            }

            // Mélanger les questions et limiter le nombre
            ShuffleAndLimitQuestions();

            // Configurer les boutons
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    int index = i;
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(index));
                }
                else
                {
                    Debug.LogError($"❌ Choice button {i} est null!");
                }
            }

            quizInitialized = true;
            currentQuestionIndex = 0;
            score = 0;

            ShowQuestion();
            UpdateScore();

            Debug.Log($"✅ Quiz initialisé avec {currentQuizQuestions.Count} questions pour le grade {studentGrade}!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Erreur lors de l'initialisation du quiz: " + e.Message);
        }
    }

    void FilterQuestionsByGrade()
    {
        currentQuizQuestions.Clear();

        if (string.IsNullOrEmpty(studentGrade))
        {
            Debug.LogError("❌ Grade de l'étudiant non défini!");
            return;
        }

        // Filtrer les questions par grade
        foreach (Question question in allQuestions)
        {
            if (question.grade == studentGrade)
            {
                currentQuizQuestions.Add(question);
            }
        }

        Debug.Log($"📚 {currentQuizQuestions.Count} questions trouvées pour le grade {studentGrade}");

        // Si aucune question trouvée pour le grade exact, essayer avec des grades similaires
        if (currentQuizQuestions.Count == 0)
        {
            Debug.LogWarning($"⚠️ Aucune question pour le grade {studentGrade}, recherche de grades similaires...");

            // Essayer de trouver des questions avec des grades numériques proches
            if (int.TryParse(studentGrade, out int gradeNum))
            {
                // Chercher grade-1 et grade+1
                foreach (Question question in allQuestions)
                {
                    if (int.TryParse(question.grade, out int questionGradeNum))
                    {
                        if (Mathf.Abs(questionGradeNum - gradeNum) <= 1)
                        {
                            currentQuizQuestions.Add(question);
                        }
                    }
                }
                Debug.Log($"📚 {currentQuizQuestions.Count} questions trouvées avec grades similaires");
            }
        }
    }

    void ShuffleAndLimitQuestions()
    {
        // Mélanger les questions
        for (int i = 0; i < currentQuizQuestions.Count; i++)
        {
            Question temp = currentQuizQuestions[i];
            int randomIndex = UnityEngine.Random.Range(i, currentQuizQuestions.Count);
            currentQuizQuestions[i] = currentQuizQuestions[randomIndex];
            currentQuizQuestions[randomIndex] = temp;
        }

        // Limiter le nombre de questions
        if (currentQuizQuestions.Count > maxQuestionsPerQuiz)
        {
            currentQuizQuestions = currentQuizQuestions.GetRange(0, maxQuestionsPerQuiz);
        }

        Debug.Log($"🎲 Questions mélangées, {currentQuizQuestions.Count} questions sélectionnées");
    }

    void CreateDefaultQuestions()
    {
        allQuestions.Clear();

        // Questions pour Grade 1
        allQuestions.Add(new Question
        {
            question = "Combien font 1 + 1 ?",
            choices = new string[] { "1", "2", "3", "4" },
            correctIndex = 1,
            grade = "1",
            subject = "math"
        });

        allQuestions.Add(new Question
        {
            question = "Quelle couleur fait jaune + rouge ?",
            choices = new string[] { "Vert", "Orange", "Violet", "Bleu" },
            correctIndex = 1,
            grade = "1",
            subject = "art"
        });

        // Questions pour Grade 2
        allQuestions.Add(new Question
        {
            question = "Combien font 5 + 3 ?",
            choices = new string[] { "7", "8", "9", "10" },
            correctIndex = 1,
            grade = "2",
            subject = "math"
        });

        allQuestions.Add(new Question
        {
            question = "Quel animal dit 'meow' ?",
            choices = new string[] { "Chien", "Chat", "Oiseau", "Poisson" },
            correctIndex = 1,
            grade = "2",
            subject = "science"
        });

        // Questions pour Grade 3
        allQuestions.Add(new Question
        {
            question = "Combien font 12 × 3 ?",
            choices = new string[] { "35", "36", "37", "38" },
            correctIndex = 1,
            grade = "3",
            subject = "math"
        });

        allQuestions.Add(new Question
        {
            question = "Quelle est la capitale de la France ?",
            choices = new string[] { "Londres", "Berlin", "Paris", "Madrid" },
            correctIndex = 2,
            grade = "3",
            subject = "geographie"
        });

        Debug.Log($"📝 {allQuestions.Count} questions par défaut créées");
    }

    void ShowQuestion()
    {
        if (!quizInitialized)
        {
            Debug.LogError("❌ Quiz non initialisé!");
            return;
        }

        answered = false;

        // Réinitialiser l'apparence des boutons
        foreach (var btn in choiceButtons)
        {
            if (btn != null)
            {
                btn.interactable = true;
                btn.GetComponent<Image>().color = Color.white;
            }
        }

        if (currentQuestionIndex < currentQuizQuestions.Count)
        {
            Question q = currentQuizQuestions[currentQuestionIndex];
            questionText.text = q.question;



            Debug.Log($"📋 Question {currentQuestionIndex + 1}/{currentQuizQuestions.Count}: {q.question}");

            for (int i = 0; i < choiceButtons.Length && i < q.choices.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    var buttonText = choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = q.choices[i];
                        choiceButtons[i].gameObject.SetActive(true);
                    }
                }
            }

            // Masquer les boutons non utilisés
            for (int i = q.choices.Length; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            EndQuiz();
        }
    }

    void EndQuiz()
    {
        Debug.Log("🏁 Quiz terminé!");
        questionText.text = $"Quiz terminé!\nScore final: {score}/{currentQuizQuestions.Count}\nGrade: {studentGrade}";

        foreach (var btn in choiceButtons)
        {
            if (btn != null)
            {
                btn.gameObject.SetActive(false);
            }
        }

        SaveResultToFirebase();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Vertical Addition");
    }

    public void OnChoiceSelected(int selectedIndex)
    {
        if (answered || !quizInitialized) return;

        answered = true;
        PlaySound(clickSound);

        Question q = currentQuizQuestions[currentQuestionIndex];

        // Désactiver tous les boutons
        foreach (var btn in choiceButtons)
        {
            if (btn != null) btn.interactable = false;
        }

        if (selectedIndex == q.correctIndex)
        {
            choiceButtons[selectedIndex].GetComponent<Image>().color = Color.green;
            PlaySound(correctSound);
            score++;
            Debug.Log("✅ Bonne réponse!");
        }
        else
        {
            choiceButtons[selectedIndex].GetComponent<Image>().color = Color.red;
            choiceButtons[q.correctIndex].GetComponent<Image>().color = Color.green;
            PlaySound(wrongSound);
            Debug.Log("❌ Mauvaise réponse!");
        }

        UpdateScore();
        StartCoroutine(NextQuestionAfterDelay());
    }

    IEnumerator NextQuestionAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        currentQuestionIndex++;
        ShowQuestion();
    }

    void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score * 1000} | Grade: {studentGrade}";
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void SaveResultToFirebase()
    {
        if (string.IsNullOrEmpty(activeTestId))
        {
            Debug.LogError("❌ Impossible de sauvegarder: activeTestId est vide");
            return;
        }

        int correct = score;
        int total = currentQuizQuestions.Count;
        int wrong = total - correct;
        long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Dictionary<string, object> resultData = new Dictionary<string, object>
        {
            { "score", (int)((float)correct / total * 100) },
            { "correctAnswers", correct },
            { "wrongAnswers", wrong },
            { "totalQuestions", total },
            { "grade", studentGrade },
            { "startedAt", startTime },
            { "finishedAt", endTime }
        };

        DatabaseReference resultRef = dbReference.Child("testResults").Child(activeTestId).Child(studentId);

        Debug.Log("💾 Sauvegarde des résultats...");

        resultRef.Child(gameKey).SetValueAsync(resultData).ContinueWithOnMainThread(saveTask =>
        {
            if (saveTask.IsCompletedSuccessfully)
            {
                Debug.Log($"✅ Résultat enregistré pour {gameKey} (Grade {studentGrade})!");
            }
            else
            {
                Debug.LogError($"❌ Erreur d'enregistrement: {saveTask.Exception?.Message}");
            }
        });
    }
}