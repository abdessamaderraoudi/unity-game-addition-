using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.Auth; // Added for FirebaseUser

public class HubSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject profileButton;
    [SerializeField] private GameObject settingsButton;
    [SerializeField] private GameObject quitButton;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject closeProfileButton;
    [SerializeField] private GameObject closeSettingsButton;
    [SerializeField] private TMP_Text profileText;
    [SerializeField] private FirebasePlayerDataManager firebasePlayerDataManager;

    void Start()
    {
        if (playButton == null || profileButton == null || settingsButton == null || quitButton == null ||
            profilePanel == null || settingsPanel == null || closeProfileButton == null || closeSettingsButton == null ||
            profileText == null || firebasePlayerDataManager == null)
        {
            Debug.LogError("HubSceneManager: Required references not assigned!");
            return;
        }

        playButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(StartGame);
        profileButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ShowProfile);
        settingsButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ShowSettings);
        quitButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(QuitGame);
        closeProfileButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseProfile);
        closeSettingsButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseSettings);

        // Ensure ProfilePanel and SettingsPanel are inactive by default
        profilePanel.SetActive(false);
        settingsPanel.SetActive(false);

        // Load player data on scene start
        FirebaseUser currentUser = FirebaseInitializer.Instance.Auth.CurrentUser;
        if (currentUser != null)
        {
            string firebasePlayerId = currentUser.UserId;
            firebasePlayerDataManager.LoadPlayerData(firebasePlayerId, OnPlayerDataLoaded);
        }
    }

    private void OnPlayerDataLoaded(PlayerProfile profile)
    {
        if (profile != null)
        {
            UpdateProfileDisplay(profile);
        }
        else
        {
            Debug.LogError("Failed to load player profile in HubScene.");
        }
    }

    void StartGame()
    {
        SceneManager.LoadScene("TestScene");
    }

    void ShowProfile()
    {
        UpdateProfileDisplay(); // Use current loaded profile
        profilePanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    void ShowSettings()
    {
        profilePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }

    void CloseProfile()
    {
        profilePanel.SetActive(false);
    }

    void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    void UpdateProfileDisplay(PlayerProfile profile = null)
    {
        if (profile == null && FirebaseInitializer.Instance.Auth.CurrentUser != null)
        {
            // Load profile if not provided (e.g., on initial scene load)
            firebasePlayerDataManager.LoadPlayerData(FirebaseInitializer.Instance.Auth.CurrentUser.UserId, UpdateProfileDisplay);
            return;
        }

        // Use profile data from Firebase
        int grade = (int)profile.schoolGrade; // Convert GradeLevel enum to int
        int stars = profile.rewardProfile?.score ?? 0; // Use stars from RewardData
        string badge = profile.achievements?.currentBadge ?? "None"; // Use badge from AchievementData
        profileText.text = $"Grade: {grade}\nStars: {stars}\nBadge: {badge}";
    }
}