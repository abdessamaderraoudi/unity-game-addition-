using UnityEngine;
using TMPro;
using UnityEngine.UI;
// using System;


public class Balloon : MonoBehaviour
{
   [SerializeField] private TextMeshProUGUI questionText; 
    [SerializeField] private TextMeshProUGUI feedbackText; 
    [SerializeField] private TextMeshProUGUI scoreText; 
    [SerializeField] private TextMeshProUGUI highScoreText; 
    [SerializeField] private TextMeshProUGUI streakText; 
    [SerializeField] private Image progressBar; // For progress toward a goal 
    [SerializeField] private Button[] numberButtons; 
    [SerializeField] private Button submitButton; 
    [SerializeField] private Button resetButton; 
    // [SerializeField] private AudioSource correctSound;
    //  [SerializeField] private AudioSource incorrectSound; 
    //  [SerializeField] private Animator feedbackAnimator; // For animating feedback text



private int number1;
private int number2;
private int correctAnswer;
private string currentInput = "";
private int score = 0;
private int highScore = 0;
private int streak = 0;
private int questionsAnswered = 0;
private const int questionsGoal = 10; // Goal for progress bar (e.g., 10 correct answers)
private float timeLeft = 10f;
private bool isQuestionActive = true;

void Start()
{
    UpdateScoreDisplay();
    UpdateHighScoreDisplay();
    UpdateStreakDisplay();
    UpdateProgressBar();
    GenerateNewQuestion();
    SetupButtons();
}

void Update()
{
    if (isQuestionActive)
    {
        timeLeft -= Time.deltaTime;
        feedbackText.text = $"Time: {Mathf.Ceil(timeLeft)}s";
        if (timeLeft <= 0)
        {
            isQuestionActive = false;
            feedbackText.text = "Time's up! Let's try another one!";
            feedbackText.color = Color.red;
            // incorrectSound.Play();
            streak = 0;
            UpdateStreakDisplay();
            Invoke(nameof(GenerateNewQuestion), 2f);
        }
    }
}

void GenerateNewQuestion()
{
    number1 = Random.Range(1, 10);
    number2 = Random.Range(1, 10);
    if(Mathf.Max(number1, number2) == number1){
        correctAnswer = number1 - number2;
    }
    else{
        correctAnswer = number2 - number1;
    }
    // correctAnswer = Mathf.Max(number1, number2) - Mathf.Max(number1, number2);
    // correctAnswer = number2 - number1;
    questionText.text = $"{number1} + ? = {number2}";
    currentInput = "";
    timeLeft = 10f;
    isQuestionActive = true;
    feedbackText.text = $"Time: {Mathf.Ceil(timeLeft)}s";
    feedbackText.color = Color.black;
    UpdateInputDisplay();
}

void SetupButtons()
{
    for (int i = 0; i < numberButtons.Length; i++)
    {
        int number = i;
        numberButtons[i].onClick.AddListener(() => OnNumberButtonClick(number));
    }
    submitButton.onClick.AddListener(OnSubmitButtonClick);
    resetButton.onClick.AddListener(OnResetButtonClick);
}

void OnNumberButtonClick(int number)
{
    if (isQuestionActive && currentInput.Length < 2)
    {
        currentInput += number.ToString();
        UpdateInputDisplay();
        numberButtons[number].GetComponent<Image>().color = Color.cyan;
        Invoke(nameof(ResetButtonColor), 0.3f);
    }
}

void ResetButtonColor()
{
    foreach (Button btn in numberButtons)
    {
        btn.GetComponent<Image>().color = new Color(0.7f, 0.9f, 1f); // Light blue for buttons
    }
}

void UpdateInputDisplay()
{
    questionText.text = $"{number1} + {number2} = {currentInput}";
}

void OnSubmitButtonClick()
{
    if (!isQuestionActive) return;

    if (int.TryParse(currentInput, out int playerAnswer))
    {
        isQuestionActive = false;
        questionsAnswered++;
        UpdateProgressBar();
        if (playerAnswer == correctAnswer)
        {
            score++;
            streak++;
            if (score > highScore)
            {
                highScore = score;
                UpdateHighScoreDisplay();
            }
            UpdateScoreDisplay();
            UpdateStreakDisplay();
            string[] correctMessages = { "Awesome job!", "You're a star!", "Fantastic!", "Keep it up!" };
            feedbackText.text = correctMessages[Random.Range(0, correctMessages.Length)];
            feedbackText.color = Color.green;
            // correctSound.Play();
            // feedbackAnimator.SetTrigger("Pop"); // Trigger animation
            Invoke(nameof(GenerateNewQuestion), 2f);
        }
        else
        {
            string[] incorrectMessages = { "Nice try! Let's do another!", "Almost there!", "Keep trying!" };
            feedbackText.text = incorrectMessages[Random.Range(0, incorrectMessages.Length)];
            feedbackText.color = Color.red;
            // incorrectSound.Play();
            streak = 0;
            UpdateStreakDisplay();
            currentInput = "";
            UpdateInputDisplay();
            Invoke(nameof(GenerateNewQuestion), 2f);
        }
    }
    else
    {
        feedbackText.text = "Please enter a number!";
        feedbackText.color = Color.red;
    }
}

void OnResetButtonClick()
{
    currentInput = "";
    UpdateInputDisplay();
    feedbackText.text = $"Time: {Mathf.Ceil(timeLeft)}s";
    feedbackText.color = Color.black;
}

void UpdateScoreDisplay()
{
    scoreText.text = $"Score: {score}";
}

void UpdateHighScoreDisplay()
{
    highScoreText.text = $"High Score: {highScore}";
}

void UpdateStreakDisplay()
{
    streakText.text = $"Streak: {streak}";
}

void UpdateProgressBar()
{
    float progress = (float)questionsAnswered / questionsGoal;
    progressBar.fillAmount = Mathf.Clamp01(progress);
    if (questionsAnswered >= questionsGoal)
    {
        feedbackText.text = "You reached the goal! Amazing!";
        feedbackText.color = Color.blue;
        questionsAnswered = 0; // Reset for next goal
        Invoke(nameof(GenerateNewQuestion), 2f);
    }
}

}
