using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Android 출시에 필요한 플레이어 설정을 코드로 고정한다.
    /// ProjectSettings 를 손으로 만지면 팀원마다 값이 갈리므로 한 번에 적용하도록 만들었다.
    /// </summary>
    public static class ProjectConfigurator
    {
        public const string PackageName = "com.duruone.casual2d";
        public const string ProductName = "Tap Runner";
        public const string CompanyName = "duruone";

        [MenuItem("Tools/2D Runner/Android 설정 적용", priority = 20)]
        public static void ConfigureAndroid()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);

            // 세로 고정 캐주얼 게임.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Google Play 는 64비트 바이너리를 요구하므로 IL2CPP + ARM64 가 사실상 필수다.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // 2D 게임은 리니어 대신 감마로 두면 저사양 기기 호환이 넓다.
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
            {
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
            });

            // 60fps 목표. vSync 를 끄고 targetFrameRate 로 제어한다.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            AssetDatabase.SaveAssets();
            Debug.Log($"[ProjectConfigurator] Android 설정 완료 — {PackageName}");
        }

        [MenuItem("Tools/2D Runner/저장 데이터 초기화", priority = 21)]
        public static void ClearSaveData()
        {
            Core.SaveSystem.Clear();
            Debug.Log("[ProjectConfigurator] 저장 데이터를 초기화했습니다.");
        }
    }
}
