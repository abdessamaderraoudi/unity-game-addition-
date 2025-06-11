using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Database;

public class FirebaseManager : MonoBehaviour
{
    private FirebaseApp firebaseApp;
    private DatabaseReference databaseReference;
    private bool isFirebaseInitialized = false;

    void Start()
    {
        // Initialize Firebase and test connection
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"Firebase initialization failed: {task.Exception}");
                return;
            }

            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                firebaseApp = FirebaseApp.DefaultInstance;
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                isFirebaseInitialized = true;
                Debug.Log("Firebase initialized successfully!");

                // Test write to database
                TestDatabaseConnection();
            }
            else
            {
                Debug.LogError($"Firebase unavailable: {dependencyStatus}");
            }
        });
    }

    private void TestDatabaseConnection()
    {
        if (databaseReference != null)
        {
            databaseReference.Child("test").Child("connection").SetValueAsync("Unity Connected!")
                .ContinueWithOnMainThread(task =>
                {
                    if (task.Exception != null)
                    {
                        Debug.LogError($"Failed to write test data: {task.Exception}");
                    }
                    else
                    {
                        Debug.Log("Test data written to Firebase!");
                        ReadTestData(); // Read back to confirm
                    }
                });
        }
    }

    private void ReadTestData()
    {
        databaseReference.Child("test").Child("connection").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"Failed to read test data: {task.Exception}");
            }
            else
            {
                DataSnapshot snapshot = task.Result;
                string value = snapshot.Value?.ToString() ?? "null";
                Debug.Log($"Read from Firebase: {value}");
            }
        });
    }

    public bool IsFirebaseInitialized => isFirebaseInitialized;

    public DatabaseReference Database => databaseReference;
}