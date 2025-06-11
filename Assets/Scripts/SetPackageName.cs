using UnityEditor;
using UnityEditor.Build;

public class SetPackageName
{
    [MenuItem("Tools/Configurer le Package Name")]
    public static void SetAndroidPackageName()
    {
        string packageName = "com.shayma.monjeueducatif";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, packageName);
        UnityEngine.Debug.Log("✅ Package Name défini : " + packageName);
    }
}
