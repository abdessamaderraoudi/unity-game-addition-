using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseTest : MonoBehaviour
{
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("✅ Firebase connecté !");

                DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;

                // Test : écrire une valeur simple dans la base de données
                reference.Child("testConnexion").SetValueAsync("Hello Shayma 👋").ContinueWithOnMainThread(writeTask =>
                {
                    if (writeTask.IsCompleted)
                    {
                        Debug.Log("✅ Donnée écrite avec succès !");
                    }
                    else
                    {
                        Debug.LogError("❌ Erreur lors de l’écriture : " + writeTask.Exception);
                    }
                });
            }
            else
            {
                Debug.LogError("❌ Firebase non disponible : " + task.Result.ToString());
            }
        });
    }
}
