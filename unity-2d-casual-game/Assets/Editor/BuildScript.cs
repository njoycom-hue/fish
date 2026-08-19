using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// CI 및 로컬에서 쓰는 배치 빌드 진입점.
    /// 예) Unity -quit -batchmode -executeMethod Game.EditorTools.BuildScript.BuildAndroid
    /// </summary>
    public static class BuildScript
    {
        private const string OutputDirectory = "Builds/Android";

        [MenuItem("Tools/2D Runner/Android APK 빌드", priority = 40)]
        public static void BuildApk() => Build(aab: false);

        [MenuItem("Tools/2D Runner/Android AAB 빌드 (스토어용)", priority = 41)]
        public static void BuildAab() => Build(aab: true);

        /// <summary>CI 에서 -executeMethod 로 호출하는 기본 진입점.</summary>
        public static void BuildAndroid()
        {
            bool aab = Environment.GetEnvironmentVariable("BUILD_AAB") == "true";
            Build(aab);
        }

        private static void Build(bool aab)
        {
            ProjectConfigurator.ConfigureAndroid();
            ApplyVersionFromEnvironment();
            ApplyKeystoreFromEnvironment();

            EditorUserBuildSettings.buildAppBundle = aab;

            Directory.CreateDirectory(OutputDirectory);
            string extension = aab ? "aab" : "apk";
            string outputPath = Path.Combine(
                OutputDirectory,
                $"{ProjectConfigurator.ProductName.Replace(" ", string.Empty)}-{PlayerSettings.bundleVersion}.{extension}");

            var options = new BuildPlayerOptions
            {
                scenes = EnabledScenes(),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] 빌드 성공 — {outputPath} ({summary.totalSize / 1024 / 1024} MB)");
                return;
            }

            // 배치 모드에서 실패를 조용히 넘기면 CI 가 초록으로 뜬다. 반드시 종료 코드를 남긴다.
            Debug.LogError($"[BuildScript] 빌드 실패 — {summary.result}, 오류 {summary.totalErrors}건");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        private static string[] EnabledScenes()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new BuildFailedException(
                    "빌드 설정에 씬이 없습니다. Tools > 2D Runner > 샘플 씬 생성 을 먼저 실행하세요.");
            }

            return scenes;
        }

        private static void ApplyVersionFromEnvironment()
        {
            string version = Environment.GetEnvironmentVariable("BUILD_VERSION");
            if (!string.IsNullOrEmpty(version))
            {
                PlayerSettings.bundleVersion = version;
            }

            string versionCode = Environment.GetEnvironmentVariable("BUILD_NUMBER");
            if (int.TryParse(versionCode, out int parsed))
            {
                PlayerSettings.Android.bundleVersionCode = parsed;
            }
        }

        private static void ApplyKeystoreFromEnvironment()
        {
            string keystorePath = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
            if (string.IsNullOrEmpty(keystorePath) || !File.Exists(keystorePath))
            {
                // 키스토어가 없으면 디버그 키로 빌드된다 — 스토어 업로드는 불가하지만 테스트는 된다.
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.LogWarning("[BuildScript] 키스토어가 없어 디버그 서명으로 빌드합니다.");
                return;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
            PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_NAME");
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");
        }
    }
}
