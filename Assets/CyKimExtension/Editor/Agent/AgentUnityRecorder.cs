#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>
/// Agent-only Unity Recorder helper. Call via MCP execute_code, e.g. AgentUnityRecorder.StartMovie(10f).
/// Output defaults to project-root Recordings/ (gitignored). Requires Play Mode for Game View capture.
/// 저해상도/커스텀 해상도 녹화 시 Game View selectedSizeIndex를 저장했다가 Stop/자동종료 때 원복한다.
/// </summary>
public static class AgentUnityRecorder
{
    private const string MenuPathStart = "Tools/Agent/Recorder/Start Movie 10s (defaults)";
    private const string MenuPathStop = "Tools/Agent/Recorder/Stop";
    private const string PrefsLastOutput = "AgentUnityRecorder.LastOutputPath";
    private const string PrefsLastMode = "AgentUnityRecorder.LastMode";

    /// <summary>Default portrait mobile Game View size.</summary>
    public const int DefaultWidth = 1080;
    public const int DefaultHeight = 1920;

    private static RecorderController _controller;
    private static string _pendingPathNoExt;

    private static bool _hasSavedGameViewSize;
    private static int _savedGameViewSizeIndex;
    private static bool _restoreGameViewAfterRecording;
    private static bool _watchRecordingEnd;

    public static string DefaultOutputDirectory =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));

    [MenuItem(MenuPathStart)]
    public static void StartMovieMenu()
    {
        Debug.Log(StartMovie(10f));
    }

    [MenuItem(MenuPathStart, true)]
    private static bool ValidateStartMovieMenu() => false; // Agent 전용

    [MenuItem(MenuPathStop)]
    public static void StopMenu()
    {
        Debug.Log(Stop());
    }

    [MenuItem(MenuPathStop, true)]
    private static bool ValidateStopMenu() => false; // Agent 전용

    /// <summary>
    /// Start MP4 recording. durationSeconds &lt;= 0 means manual (call Stop).
    /// cameraTag null = Game View; otherwise tagged camera (CaptureUI on).
    /// quality: Low / Medium / High (default High). Use Medium for webhook-friendly size.
    /// </summary>
    public static string StartMovie(
        float durationSeconds = 10f,
        int width = DefaultWidth,
        int height = DefaultHeight,
        float frameRate = 30f,
        string fileName = null,
        bool captureAudio = false,
        string cameraTag = null,
        string quality = "High")
    {
        if (!EditorApplication.isPlaying)
            return "error=not_in_play_mode (enter Play Mode before recording Game View)";

        if (IsRecording())
            Stop();

        EnsureOutputDirectory();
        BeginGameViewSizeGuard(width, height);

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseName = string.IsNullOrWhiteSpace(fileName) ? $"movie_{stamp}" : SanitizeFileName(fileName);
        _pendingPathNoExt = Path.Combine(DefaultOutputDirectory, baseName);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "AgentMovieRecorder";
        movie.Enabled = true;
        movie.CaptureAudio = captureAudio;
        movie.OutputFile = _pendingPathNoExt;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = ParseQuality(quality)
        };
        movie.ImageInputSettings = CreateImageInput(width, height, cameraTag);

        controllerSettings.AddRecorderSettings(movie);
        controllerSettings.FrameRate = frameRate;
        controllerSettings.CapFrameRate = true;
        controllerSettings.ExitPlayMode = false;

        if (durationSeconds > 0f)
            controllerSettings.SetRecordModeToTimeInterval(0f, durationSeconds);
        else
            controllerSettings.SetRecordModeToManual();

        _controller = new RecorderController(controllerSettings);
        _controller.PrepareRecording();
        _controller.StartRecording();
        StartWatchRecordingEnd();

        var outputPath = _pendingPathNoExt + ".mp4";
        EditorPrefs.SetString(PrefsLastOutput, outputPath);
        EditorPrefs.SetString(PrefsLastMode, "movie");

        return FormatStatus(
            "started",
            outputPath,
            durationSeconds,
            width,
            height,
            frameRate,
            string.IsNullOrEmpty(cameraTag) ? "GameView" : $"CameraTag:{cameraTag}");
    }

    /// <summary>
    /// Start PNG image sequence. durationSeconds &lt;= 0 means manual (call Stop).
    /// Files: {fileName}_0001.png, ...
    /// </summary>
    public static string StartImageSequence(
        float durationSeconds = 5f,
        int width = DefaultWidth,
        int height = DefaultHeight,
        float frameRate = 30f,
        string fileName = null,
        string cameraTag = null)
    {
        if (!EditorApplication.isPlaying)
            return "error=not_in_play_mode (enter Play Mode before recording Game View)";

        if (IsRecording())
            Stop();

        EnsureOutputDirectory();
        BeginGameViewSizeGuard(width, height);

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseName = string.IsNullOrWhiteSpace(fileName) ? $"seq_{stamp}" : SanitizeFileName(fileName);
        _pendingPathNoExt = Path.Combine(DefaultOutputDirectory, baseName);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var image = ScriptableObject.CreateInstance<ImageRecorderSettings>();
        image.name = "AgentImageRecorder";
        image.Enabled = true;
        image.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        image.OutputFile = _pendingPathNoExt;
        image.imageInputSettings = CreateImageInput(width, height, cameraTag);

        controllerSettings.AddRecorderSettings(image);
        controllerSettings.FrameRate = frameRate;
        controllerSettings.CapFrameRate = true;
        controllerSettings.ExitPlayMode = false;

        if (durationSeconds > 0f)
            controllerSettings.SetRecordModeToTimeInterval(0f, durationSeconds);
        else
            controllerSettings.SetRecordModeToManual();

        _controller = new RecorderController(controllerSettings);
        _controller.PrepareRecording();
        _controller.StartRecording();
        StartWatchRecordingEnd();

        EditorPrefs.SetString(PrefsLastOutput, _pendingPathNoExt + "_####.png");
        EditorPrefs.SetString(PrefsLastMode, "image_sequence");

        return FormatStatus(
            "started",
            _pendingPathNoExt + "_####.png",
            durationSeconds,
            width,
            height,
            frameRate,
            string.IsNullOrEmpty(cameraTag) ? "GameView" : $"CameraTag:{cameraTag}");
    }

    public static string Stop()
    {
        StopWatchRecordingEnd();

        if (_controller == null)
        {
            RestoreGameViewSizeIfNeeded();
            return $"idle recording=false last={GetLastOutputPath()}";
        }

        var wasRecording = _controller.IsRecording();
        if (wasRecording)
            _controller.StopRecording();

        _controller = null;
        RestoreGameViewSizeIfNeeded();
        var path = GetLastOutputPath();
        return $"stopped wasRecording={wasRecording} path={path} exists={OutputExists(path)}";
    }

    public static bool IsRecording()
    {
        return _controller != null && _controller.IsRecording();
    }

    public static string GetStatus()
    {
        var path = GetLastOutputPath();
        return $"recording={IsRecording()} path={path} exists={OutputExists(path)} mode={EditorPrefs.GetString(PrefsLastMode, "")}";
    }

    public static string GetLastOutputPath()
    {
        return EditorPrefs.GetString(PrefsLastOutput, string.Empty);
    }

    public static string EnsureOutputDirectory()
    {
        var dir = DefaultOutputDirectory;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    private static void BeginGameViewSizeGuard(int width, int height)
    {
        // 기본(1080×1920)이 아니면 저해상도/커스텀 녹화로 보고, 종료 시 Game View 사이즈 원복.
        _restoreGameViewAfterRecording = width != DefaultWidth || height != DefaultHeight;

        if (!_restoreGameViewAfterRecording)
            return;

        if (_hasSavedGameViewSize)
            return;

        if (!TryGetGameViewSelectedSizeIndex(out var index))
            return;

        _savedGameViewSizeIndex = index;
        _hasSavedGameViewSize = true;
    }

    private static void RestoreGameViewSizeIfNeeded()
    {
        if (!_restoreGameViewAfterRecording || !_hasSavedGameViewSize)
        {
            _restoreGameViewAfterRecording = false;
            _hasSavedGameViewSize = false;
            return;
        }

        if (TrySetGameViewSelectedSizeIndex(_savedGameViewSizeIndex))
        {
            Debug.Log($"[AgentUnityRecorder] Restored Game View size index {_savedGameViewSizeIndex}");
        }

        _restoreGameViewAfterRecording = false;
        _hasSavedGameViewSize = false;
    }

    private static void StartWatchRecordingEnd()
    {
        if (_watchRecordingEnd)
            return;

        _watchRecordingEnd = true;
        EditorApplication.update += WatchRecordingEnd;
    }

    private static void StopWatchRecordingEnd()
    {
        if (!_watchRecordingEnd)
            return;

        _watchRecordingEnd = false;
        EditorApplication.update -= WatchRecordingEnd;
    }

    private static void WatchRecordingEnd()
    {
        // TimeInterval 자동 종료 시 Stop()을 안 불러도 Game View를 원복한다.
        if (_controller == null)
        {
            StopWatchRecordingEnd();
            RestoreGameViewSizeIfNeeded();
            return;
        }

        if (_controller.IsRecording())
            return;

        _controller = null;
        StopWatchRecordingEnd();
        RestoreGameViewSizeIfNeeded();
    }

    private static bool TryGetGameViewSelectedSizeIndex(out int index)
    {
        index = -1;
        var gameView = FindGameViewWindow();
        if (gameView == null)
            return false;

        var prop = gameView.GetType().GetProperty(
            "selectedSizeIndex",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop == null)
            return false;

        index = (int)prop.GetValue(gameView);
        return true;
    }

    private static bool TrySetGameViewSelectedSizeIndex(int index)
    {
        var gameView = FindGameViewWindow();
        if (gameView == null)
            return false;

        var prop = gameView.GetType().GetProperty(
            "selectedSizeIndex",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop == null || !prop.CanWrite)
            return false;

        prop.SetValue(gameView, index);
        gameView.Repaint();
        return true;
    }

    private static EditorWindow FindGameViewWindow()
    {
        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null)
            return null;

        var windows = Resources.FindObjectsOfTypeAll(gameViewType);
        return windows != null && windows.Length > 0 ? windows[0] as EditorWindow : null;
    }

    private static CoreEncoderSettings.VideoEncodingQuality ParseQuality(string quality)
    {
        if (string.Equals(quality, "Low", StringComparison.OrdinalIgnoreCase))
            return CoreEncoderSettings.VideoEncodingQuality.Low;
        if (string.Equals(quality, "Medium", StringComparison.OrdinalIgnoreCase))
            return CoreEncoderSettings.VideoEncodingQuality.Medium;
        return CoreEncoderSettings.VideoEncodingQuality.High;
    }

    private static ImageInputSettings CreateImageInput(int width, int height, string cameraTag)
    {
        if (string.IsNullOrEmpty(cameraTag))
        {
            return new GameViewInputSettings
            {
                OutputWidth = width,
                OutputHeight = height
            };
        }

        return new CameraInputSettings
        {
            Source = ImageSource.TaggedCamera,
            CameraTag = cameraTag,
            CaptureUI = true,
            OutputWidth = width,
            OutputHeight = height
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName.Trim());
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static bool OutputExists(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        if (path.Contains("####"))
        {
            var dir = Path.GetDirectoryName(path);
            var prefix = Path.GetFileName(path).Replace("_####.png", string.Empty);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return false;
            var matches = Directory.GetFiles(dir, prefix + "_*.png");
            return matches.Length > 0;
        }

        return File.Exists(path);
    }

    private static string FormatStatus(
        string state,
        string path,
        float durationSeconds,
        int width,
        int height,
        float frameRate,
        string source)
    {
        return
            $"{state} recording={IsRecording()} path={path} duration={durationSeconds}s " +
            $"{width}x{height}@{frameRate}fps source={source}";
    }
}
#endif
