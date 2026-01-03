using UnityEngine;

namespace Util
{
    public static class Debug
    {
        [HideInCallstack]
        [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }

        [HideInCallstack]
        [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
        public static void Log(object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.Log(message, context);
        }

        [HideInCallstack]
        [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
        public static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        [HideInCallstack]
        [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
        public static void LogWarning(object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }

        [HideInCallstack]
        [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
        public static void LogError(object message)
        {
            UnityEngine.Debug.LogError(message);
        }

        [HideInCallstack]
        [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
        public static void LogError(object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError(message, context);
        }
    }
}