using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 배포용 Windows 빌드. 에디터 메뉴(SushiSurvival/Build/Windows 64-bit)나 배치 모드로 부른다:
///   Unity.exe -batchmode -quit -projectPath unity -executeMethod BuildTools.BuildWindowsCli
/// 결과는 unity/Build/Windows/ 에 만들어지고(저장소에 올라가지 않는다), zip으로 묶는 건 tools/package-windows.sh 가 한다.
/// </summary>
public static class BuildTools
{
    private const string ExeName = "WasabiSurvival.exe";
    private const string ProductName = "와사비를 먹으면 강해지는 군요";

    /// <summary>프로젝트 루트(unity/) 기준 출력 폴더.</summary>
    private static string OutputDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "Windows"));

    [MenuItem("SushiSurvival/Build/Windows 64-bit")]
    public static void BuildWindowsMenu()
    {
        BuildReport report = BuildWindows();
        EditorUtility.DisplayDialog("Windows 빌드",
            report.summary.result == BuildResult.Succeeded
                ? $"빌드 성공\n{OutputDir}"
                : $"빌드 실패: {report.summary.result}", "확인");
    }

    /// <summary>배치 모드 진입점. 성공하면 종료 코드 0, 아니면 1로 끝낸다.</summary>
    public static void BuildWindowsCli()
    {
        BuildReport report = BuildWindows();
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    private static BuildReport BuildWindows()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
            throw new InvalidOperationException("Build Settings에 켜진 씬이 없습니다.");

        string outputDir = OutputDir;
        if (Directory.Exists(outputDir))
            Directory.Delete(outputDir, true);
        Directory.CreateDirectory(outputDir);

        // 창 제목과 저장 경로에 쓰이는 제품 이름을 빌드하는 동안만 바꾸고 끝나면 되돌린다
        // (ProjectSettings 파일이 빌드 때문에 바뀌어 저장소에 변경으로 잡히지 않게).
        string previousProductName = PlayerSettings.productName;
        PlayerSettings.productName = ProductName;

        try
        {
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"[BuildTools] 빌드 시작: 씬 {scenes.Length}개 → {options.locationPathName}");
            BuildReport report = BuildPipeline.BuildPlayer(options);

            BuildSummary summary = report.summary;
            Debug.Log($"[BuildTools] 결과: {summary.result}, 크기 {summary.totalSize / (1024f * 1024f):0.0}MB, " +
                      $"오류 {summary.totalErrors}개, 경고 {summary.totalWarnings}개, 소요 {summary.totalTime}");

            if (summary.result == BuildResult.Succeeded)
                RemoveShipExcludedFolders(outputDir);

            return report;
        }
        finally
        {
            PlayerSettings.productName = previousProductName;
        }
    }

    /// <summary>Unity가 "배포하지 말라"고 표시한 디버그용 폴더를 지운다.</summary>
    private static void RemoveShipExcludedFolders(string outputDir)
    {
        foreach (string dir in Directory.GetDirectories(outputDir))
        {
            if (!dir.EndsWith("_DoNotShip", StringComparison.Ordinal) &&
                !dir.EndsWith("_ButDontShipItWithYourGame", StringComparison.Ordinal))
                continue;

            Directory.Delete(dir, true);
            Debug.Log($"[BuildTools] 배포 제외 폴더 삭제: {Path.GetFileName(dir)}");
        }
    }
}
