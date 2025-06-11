// using Firebase.Database;
// using UnityEngine.UI;
// using Firebase.Extensions;
// using System.Collections;
// using UnityEngine;
// using ZXing;
// using System.Linq;

// public class QRAuthenticator : MonoBehaviour
// {
//     public GameObject authenticationPanel;
//     public Text statusText;
//     public RawImage cameraFeed;  // UI element to display the camera feed

//     private DatabaseReference dbReference;

//     private WebCamTexture backCameraTexture;
//     private IBarcodeReader barcodeReader;

//     private readonly Rect uvRectFlipped = new(1f, 0f, -1f, 1f);
//     private readonly Rect uvRectNormal = new(0f, 0f, 1f, 1f);


//     void Start()
//     {
//         FirebaseInitializer.Instance.InitializeFirebase(() =>
//         {
//             if (FirebaseInitializer.Instance.IsFirebaseInitialized)
//             {
//                 dbReference = FirebaseInitializer.Instance.DbReference;
//                 statusText.text = "Checking camera permissions...";

//                 if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
//                 {
//                     StartCoroutine(RequestCameraPermission());
//                 }
//                 else
//                 {
//                     SetupCamera();
//                 }
//             }
//         });
//     }

//     private IEnumerator RequestCameraPermission()
//     {
//         yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

//         if (Application.HasUserAuthorization(UserAuthorization.WebCam))
//         {
//             SetupCamera();
//         }
//         else
//         {
//             statusText.text = "Camera permission denied.";
//         }
//     }

//     private void SetupCamera()
//     {
//         statusText.text = "Initializing camera...";

//         string backCamName = WebCamTexture.devices.FirstOrDefault(device => !device.isFrontFacing).name;

//         if (string.IsNullOrEmpty(backCamName))
//         {
//             statusText.text = "No back camera found.";
//             return;
//         }

//         if (WebCamTexture.devices.Length == 0)
//         {
//             statusText.text = "No cameras found on device.";
//             return;
//         }

//         backCameraTexture = new WebCamTexture(backCamName, 960, 960);
        
//         backCameraTexture.requestedFPS = 30;
//         backCameraTexture.filterMode = FilterMode.Bilinear;

//         cameraFeed.texture = null; // Clear first
//         cameraFeed.texture = backCameraTexture;
//         cameraFeed.material = null;
//         cameraFeed.color = Color.white;

//         backCameraTexture.Play();

//         // Wait until the camera starts updating
//         StartCoroutine(AdjustCameraOrientation());

//         barcodeReader = new BarcodeReader
//         {
//             AutoRotate = false,
//             Options = new ZXing.Common.DecodingOptions
//             {
//                 TryHarder = true,
//                 TryInverted = true,
//                 PossibleFormats = new[] { BarcodeFormat.QR_CODE }
//             }
//         };

//         Debug.Log("Camera playing: " + backCameraTexture.isPlaying);
//         Debug.Log("Camera frame size: " + backCameraTexture.width + "x" + backCameraTexture.height);


//         StartCoroutine(ScanQRCode());
//     }

//     private IEnumerator AdjustCameraOrientation()
//     {
//         yield return new WaitUntil(() => backCameraTexture.width > 100);

//         float angle = backCameraTexture.videoRotationAngle;
//         if (!WebCamTexture.devices[0].isFrontFacing)
//         {
//             angle = -angle;
//         }
//         if (backCameraTexture.videoVerticallyMirrored)
//         {
//             angle += 180f;
//         }

//         cameraFeed.rectTransform.localEulerAngles = new Vector3(0, 0, angle);

//         bool needsFlip = (backCameraTexture.videoVerticallyMirrored && !WebCamTexture.devices[0].isFrontFacing)
//                       || (!backCameraTexture.videoVerticallyMirrored && WebCamTexture.devices[0].isFrontFacing);

//         cameraFeed.uvRect = needsFlip ? uvRectFlipped : uvRectNormal;
//     }

//     private IEnumerator ScanQRCode()
//     {
//         while (true)
//         {
//             if (backCameraTexture.didUpdateThisFrame)
//             {
//                 // Convert the camera frame to a color array
//                 Color32[] colors = backCameraTexture.GetPixels32();
//                 var barcodeResult = barcodeReader.Decode(colors, backCameraTexture.width, backCameraTexture.height);

//                 if (barcodeResult != null)
//                 {
//                     string uid = barcodeResult.Text;
//                     if (!string.IsNullOrEmpty(uid))
//                     {
//                         AuthenticateUser(uid);
//                         break;
//                     }
//                 }
//             }
//             yield return null;
//         }
//     }

//     private void AuthenticateUser(string rawData)
//     {
//         statusText.text = "Verifying QR Code...";

//         try
//         {
//             var qrPayload = JsonUtility.FromJson<PlayerQRPayload>(rawData);
//             if (qrPayload == null || string.IsNullOrEmpty(qrPayload.uid) || string.IsNullOrEmpty(qrPayload.pin))
//             {
//                 statusText.text = "Invalid QR Code format.";
//                 return;
//             }

//             string uid = qrPayload.uid;
//             string enteredPin = qrPayload.pin;

//             var pinRef = dbReference.Child("users").Child(uid).Child("password");
//             pinRef.GetValueAsync().ContinueWithOnMainThread(task =>
//             {
//                 if (task.IsCompleted && task.Result.Exists)
//                 {
//                     string storedPin = task.Result.Value.ToString();

//                     if (enteredPin == storedPin)
//                     {
//                         statusText.text = "Authentication successful!";
//                         LoadPlayerData(uid);
//                     }
//                     else
//                     {
//                         statusText.text = "Invalid PIN.";
//                     }
//                 }
//                 else
//                 {
//                     statusText.text = "Player not found.";
//                 }
//             });
//         }
//         catch
//         {
//             statusText.text = "QR Code data unreadable.";
//         }
//     }

//     private void LoadPlayerData(string uid)
//     {
//         statusText.text = "Welcome back! Loading your profile...";

//         DatabaseReference playerRef = dbReference.Child("users").Child(uid);
//         playerRef.GetValueAsync().ContinueWithOnMainThread(task =>
//         {
//             if (task.IsCompleted && task.Result.Exists)
//             {
//                 var playerData = task.Result;
//                 Debug.Log("Player data loaded.");

//                 // Proceed to the main game scene
//                 UnityEngine.SceneManagement.SceneManager.LoadScene("TestScene");
//             }
//             else
//             {
//                 statusText.text = "Failed to load player data.";
//             }
//         });
//     }
// }
