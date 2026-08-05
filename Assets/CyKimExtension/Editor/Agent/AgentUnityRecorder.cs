#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>
/// Agent-only Unity Recorder helper. Call via MCP execute_code, e.g. AgentUnityRecorder.StartMovie(10f).
/// Output defaults to project-root Recordings/ (gitignored). Requires Play Mode for Game View capture.
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
        if (_controller == null)
            return $"idle recording=false last={GetLastOutputPath()}";

        var wasRecording = _controller.IsRecording();
        if (wasRecording)
            _controller.StopRecording();

        _controller = null;
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
