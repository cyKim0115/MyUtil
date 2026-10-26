#if DEVELOPMENT_BUILD && !UNITY_EDITOR && CYKIM_PERF_STATS
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 개발 빌드 전용 성능 기록. 1초마다 프레임·CPU·GPU·렌더·메모리 수치를 한 줄로 로그(Android는 logcat)에 남긴다.
/// 줄 머리 <c>[PerfStats]</c>로 거르고 공백으로 나뉜 key=value를 읽는다.
/// GPU·스레드 시간은 FrameTimingManager 값이라 Player Settings의 Frame Timing Stats가 켜져 있어야 0이 아니다.
/// 켜려면 Scripting Define Symbols에 <c>CYKIM_PERF_STATS</c>를 넣는다. 그러면 개발 빌드에서 첫 씬 로드 뒤 스스로 만들어진다.
/// </summary>
public class PerfStatsLogger : MonoBehaviour
{
    private const string LOG_TAG = "[PerfStats]";
    private const float REPORT_INTERVAL = 1f;
    private const float BYTES_TO_MB = 1f / (1024f * 1024f);

    // 프레임 시간 구간 상한(ms). 마지막 칸은 그보다 긴 프레임
    private static readonly float[] HISTOGRAM_LIMITS_MS = { 17.5f, 20f, 25f, 33.4f, 50f };

    private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
    private readonly int[] _histogram = new int[HISTOGRAM_LIMITS_MS.Length + 1];
    private readonly StringBuilder _sb = new StringBuilder(512);

    private ProfilerRecorder _drawCalls;
    private ProfilerRecorder _batches;
    private ProfilerRecorder _setPassCalls;
    private ProfilerRecorder _triangles;
    private ProfilerRecorder _vertices;
    private ProfilerRecorder _gcAllocInFrame;
    private ProfilerRecorder _totalUsedMemory;
    private ProfilerRecorder _systemUsedMemory;
    private ProfilerRecorder _gcReservedMemory;
    private ProfilerRecorder _gfxUsedMemory;

    private float _elapsed;
    private float _sessionTime;
    private int _frames;
    private float _frameMsSum;
    private float _frameMsMax;

    private int _timingSamples;
    private double _cpuFrameMsSum;
    private double _cpuMainMsSum;
    private double _cpuRenderMsSum;
    private double _presentWaitMsSum;
    private double _gpuMsSum;

    private long _drawCallsSum;
    private long _batchesSum;
    private long _setPassSum;
    private long _trianglesSum;
    private long _verticesSum;
    private long _gcAllocSum;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var go = new GameObject(nameof(PerfStatsLogger));
        DontDestroyOnLoad(go);
        go.AddComponent<PerfStatsLogger>();
    }

    private void OnEnable()
    {
        _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        _setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        _vertices = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        _gcAllocInFrame = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        _totalUsedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
        _systemUsedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
        _gcReservedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
        _gfxUsedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Gfx Used Memory");

        Debug.Log($"{LOG_TAG} header device={SystemInfo.deviceModel.Replace(' ', '_')} gpu={SystemInfo.graphicsDeviceName.Replace(' ', '_')} " +
                  $"api={SystemInfo.graphicsDeviceType} screen={Screen.width}x{Screen.height} " +
                  $"quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} frameTimingStats={FrameTimingManager.IsFeatureEnabled()}");
    }

    private void OnDisable()
    {
        _drawCalls.Dispose();
        _batches.Dispose();
        _setPassCalls.Dispose();
        _triangles.Dispose();
        _vertices.Dispose();
        _gcAllocInFrame.Dispose();
        _totalUsedMemory.Dispose();
        _systemUsedMemory.Dispose();
        _gcReservedMemory.Dispose();
        _gfxUsedMemory.Dispose();
    }

    private void Update()
    {
        var frameMs = Time.unscaledDeltaTime * 1000f;
        _frames++;
        _frameMsSum += frameMs;
        if (frameMs > _frameMsMax)
            _frameMsMax = frameMs;
        _histogram[GetHistogramIndex(frameMs)]++;

        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, _frameTimings) > 0)
        {
            var timing = _frameTimings[0];
            _timingSamples++;
            _cpuFrameMsSum += timing.cpuFrameTime;
            _cpuMainMsSum += timing.cpuMainThreadFrameTime;
            _cpuRenderMsSum += timing.cpuRenderThreadFrameTime;
            _presentWaitMsSum += timing.cpuMainThreadPresentWaitTime;
            _gpuMsSum += timing.gpuFrameTime;
        }

        _drawCallsSum += _drawCalls.LastValue;
        _batchesSum += _batches.LastValue;
        _setPassSum += _setPassCalls.LastValue;
        _trianglesSum += _triangles.LastValue;
        _verticesSum += _vertices.LastValue;
        _gcAllocSum += _gcAllocInFrame.LastValue;

        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed < REPORT_INTERVAL)
            return;

        _sessionTime += _elapsed;
        Report();
        ResetInterval();
    }

    private void Report()
    {
        var frames = Mathf.Max(_frames, 1);
        var samples = Mathf.Max(_timingSamples, 1);

        _sb.Clear();
        _sb.Append(LOG_TAG)
            .Append(" t=").Append(_sessionTime.ToString("F1"))
            .Append(" scene=").Append(SceneManager.GetActiveScene().name)
            .Append(" target=").Append(Application.targetFrameRate)
            .Append(" fps=").Append((_frames / _elapsed).ToString("F1"))
            .Append(" frameAvg=").Append((_frameMsSum / frames).ToString("F2"))
            .Append(" frameMax=").Append(_frameMsMax.ToString("F2"))
            .Append(" cpu=").Append((_cpuFrameMsSum / samples).ToString("F2"))
            .Append(" main=").Append((_cpuMainMsSum / samples).ToString("F2"))
            .Append(" render=").Append((_cpuRenderMsSum / samples).ToString("F2"))
            .Append(" wait=").Append((_presentWaitMsSum / samples).ToString("F2"))
            .Append(" gpu=").Append((_gpuMsSum / samples).ToString("F2"))
            .Append(" dc=").Append(_drawCallsSum / frames)
            .Append(" batches=").Append(_batchesSum / frames)
            .Append(" setpass=").Append(_setPassSum / frames)
            .Append(" tris=").Append(_trianglesSum / frames)
            .Append(" verts=").Append(_verticesSum / frames)
            .Append(" gcKB=").Append((_gcAllocSum / 1024f / frames).ToString("F2"))
            .Append(" totalMB=").Append((_totalUsedMemory.LastValue * BYTES_TO_MB).ToString("F1"))
            .Append(" sysMB=").Append((_systemUsedMemory.LastValue * BYTES_TO_MB).ToString("F1"))
            .Append(" gcResMB=").Append((_gcReservedMemory.LastValue * BYTES_TO_MB).ToString("F1"))
            .Append(" gfxMB=").Append((_gfxUsedMemory.LastValue * BYTES_TO_MB).ToString("F1"))
            .Append(" hist=");

        for (var i = 0; i < _histogram.Length; i++)
        {
            if (i > 0)
                _sb.Append(',');
            _sb.Append(_histogram[i]);
        }

        Debug.Log(_sb.ToString());
    }

    private void ResetInterval()
    {
        _elapsed = 0f;
        _frames = 0;
        _frameMsSum = 0f;
        _frameMsMax = 0f;
        _timingSamples = 0;
        _cpuFrameMsSum = 0;
        _cpuMainMsSum = 0;
        _cpuRenderMsSum = 0;
        _presentWaitMsSum = 0;
        _gpuMsSum = 0;
        _drawCallsSum = 0;
        _batchesSum = 0;
        _setPassSum = 0;
        _trianglesSum = 0;
        _verticesSum = 0;
        _gcAllocSum = 0;
        System.Array.Clear(_histogram, 0, _histogram.Length);
    }

    private static int GetHistogramIndex(float frameMs)
    {
        for (var i = 0; i < HISTOGRAM_LIMITS_MS.Length; i++)
        {
            if (frameMs <= HISTOGRAM_LIMITS_MS[i])
                return i;
        }

        return HISTOGRAM_LIMITS_MS.Length;
    }
}
#endif
