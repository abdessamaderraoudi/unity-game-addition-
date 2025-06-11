using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using ZXing;
using TMPro;
using System;

public class LoginScanner : MonoBehaviour
{
    public RawImage cameraView;
    public TextMeshProUGUI statusText;
    private WebCamTexture webcamTexture;
    private DatabaseReference dbReference;
    private bool scanning = true;
    private bool firebaseInitialized = false;

    void Start()
    {
        InitializeFirebase();

#if PLATFORM_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            statusText.text = "Autorisation caméra requise.";
            return;
        }
#endif

        InitializeCamera();
    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                firebaseInitialized = true;
                Debug.Log("✅ Firebase initialisé avec succès");
            }
            else
            {
                statusText.text = "Erreur Firebase : " + dependencyStatus.ToString();
                Debug.LogError("❌ Erreur d'initialisation Firebase : " + dependencyStatus);
            }
        });
    }

    void InitializeCamera()
    {
        webcamTexture = new WebCamTexture();
        cameraView.texture = webcamTexture;
        webcamTexture.Play();

        statusText.text = "Caméra démarrée. Scannez votre code QR.";
    }

    void Update()
    {
        if (!scanning || !firebaseInitialized || webcamTexture == null || webcamTexture.width < 100)
            return;

        try
        {
            IBarcodeReader reader = new BarcodeReader();
            var result = reader.Decode(webcamTexture.GetPixels32(), webcamTexture.width, webcamTexture.height);
            if (result != null)
            {
                scanning = false;
                webcamTexture.Stop();
                statusText.text = "Code détecté ! Vérification...";
                Debug.Log("🔍 Code scanné : " + result.Text);
                HandleScannedData(result.Text);
            }
        }
        catch (Exception ex)
        {
            statusText.text = "Erreur scan : " + ex.Message;
            Debug.LogError("❌ Erreur lors du scan : " + ex.Message);
        }
    }

    void HandleScannedData(string json)
    {
        try
        {
            Debug.Log("📄 Données reçues : " + json);
            var parsed = JsonUtility.FromJson<LoginData>(json);

            if (string.IsNullOrEmpty(parsed.uid) || string.IsNullOrEmpty(parsed.pin))
            {
                statusText.text = "Données invalides dans le QR code.";
                ResetScanning();
                return;
            }

            Debug.Log($"🔑 UID: {parsed.uid}, PIN: {parsed.pin}");
            AuthenticateUser(parsed.uid, parsed.pin);
        }
        catch (Exception ex)
        {
            statusText.text = "Code QR invalide.";
            Debug.LogError("❌ Erreur parsing JSON : " + ex.Message);
            ResetScanning();
        }
    }

    void AuthenticateUser(string uid, string pin)
    {
        statusText.text = "Vérification en cours...";
        Debug.Log($"🔍 Authentification pour UID: {uid}");

        dbReference.Child("users").Child(uid).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                statusText.text = "Erreur de connexion à la base de données.";
                Debug.LogError("❌ Erreur base de données : " + task.Exception);
                ResetScanning();
                return;
            }

            if (task.IsCompletedSuccessfully)
            {
                if (task.Result.Exists)
                {
                    var user = task.Result;
                    Debug.Log($"👤 Utilisateur trouvé : {user.GetRawJsonValue()}");

                    if (!user.Child("password").Exists || !user.Child("schoolGrade").Exists)
                    {
                        statusText.text = "Données utilisateur incomplètes.";
                        Debug.LogError("❌ Champs manquants dans la base de données");
                        ResetScanning();
                        return;
                    }

                    string dbPin = user.Child("password").Value.ToString();
                    string grade = user.Child("schoolGrade").Value.ToString();

                    Debug.Log($"🔐 PIN BDD: {dbPin}, PIN saisi: {pin}");

                    if (dbPin == pin)
                    {
                        statusText.text = "Connexion réussie ! 🎉";

                        PlayerPrefs.SetString("student_uid", uid);
                        PlayerPrefs.SetString("grade", grade);
                        PlayerPrefs.Save();

                        Debug.Log($"✅ Données sauvegardées - UID: {uid}, Grade: {grade}");

                        DelayedSceneLoad();
                    }
                    else
                    {
                        statusText.text = "Code PIN incorrect.";
                        Debug.LogWarning("⚠️ PIN incorrect");
                        ResetScanning();
                    }
                }
                else
                {
                    statusText.text = "Utilisateur non trouvé.";
                    Debug.LogWarning("⚠️ Utilisateur non trouvé dans la base");
                    ResetScanning();
                }
            }
            else
            {
                statusText.text = "Erreur lors de la vérification.";
                Debug.LogError("❌ Tâche non complétée correctement");
                ResetScanning();
            }
        });
    }

    void DelayedSceneLoad()
    {
        try
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Problem Addition");
            Debug.Log("✅ Redirection effectuée vers Problem Addition");
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Erreur lors du chargement de la scène : " + ex.Message);
            statusText.text = "Erreur : Scène 'game1' introuvable.";
        }
    }

    void ResetScanning()
    {
        scanning = true;
        if (webcamTexture != null)
        {
            webcamTexture.Play();
        }
        statusText.text = "Prêt à scanner un nouveau code.";
    }

    void OnDestroy()
    {
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
        }
    }

    [Serializable]
    public class LoginData
    {
        public string uid;
        public string pin;
    }
}
