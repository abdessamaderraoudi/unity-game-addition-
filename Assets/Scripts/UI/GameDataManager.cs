using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    public int CurrentGrade { get; set; } = 2;
    public int Stars { get; set; } = 0;
    public int ProblemsSolved { get; set; } = 0;
    public string CurrentBadge { get; set; } = "None";
    public const int ProblemsToEnd = 5;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateBadge()
    {
        if (Stars >= 20)
            CurrentBadge = "Addition Master";
        else if (Stars >= 10)
            CurrentBadge = "Math Star";
        else if (Stars >= 5)
            CurrentBadge = "Beginner";
        else
            CurrentBadge = "None";
    }

    public void ResetProgress()
    {
        Stars = 0;
        ProblemsSolved = 0;
        CurrentBadge = "None";
    }
}