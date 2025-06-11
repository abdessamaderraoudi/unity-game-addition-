// using UnityEngine;
// using UnityEngine.SceneManagement;
// using Firebase.Auth;
// using Firebase.Extensions;
// using Firebase;

// public class SplashScreenController : MonoBehaviour
// {
//     public string firebasePlayerId;
//     [SerializeField] private FirebasePlayerDataManager firebasePlayerDataManager;
//     [SerializeField] private GameObject authenticationPanel;
//     [SerializeField] private GameObject loadingText;

//     void Start()
//     {
//         FirebaseInitializer.Instance.InitializeFirebase(() =>
//         {
//             if (FirebaseInitializer.Instance.IsFirebaseInitialized)
//             {
//                 FirebaseUser currentUser = FirebaseInitializer.Instance.Auth.CurrentUser;

//                 if (currentUser != null)
//                 {
//                     string firebasePlayerId = currentUser.UserId;
//                     Debug.Log("User already signed in: " + firebasePlayerId);

//                     firebasePlayerDataManager.LoadPlayerData(firebasePlayerId, OnPlayerDataLoaded);
//                 }
//                 else
//                 {
//                     Debug.Log("No user is currently signed in.");
//                     ShowAuthenticationWidget();
//                 }
//             }
//         });
//     }


//     private void OnPlayerDataLoaded(PlayerProfile profile)
//     {
//         if (profile != null)
//         {
//             Debug.Log("Player data loaded successfully.");
//             SceneManager.LoadScene("TestScene");
//         }
//         else
//         {
//             Debug.Log("Player data load failed.");
//             ShowAuthenticationWidget();
//         }
//     }

//     private void ShowAuthenticationWidget()
//     {
//         if (loadingText != null)
//             loadingText.SetActive(false);

//         Debug.Log("Redirecting to authentication screen.");
//         authenticationPanel.SetActive(true);
//     }
// }
