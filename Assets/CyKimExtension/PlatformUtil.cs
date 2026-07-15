namespace Util
{
    public static class PlatformUtil
    {
        public static bool IsEditor()
        {
#if UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        public static bool IsDev()
        {
#if DEV
            return true;
#else
            return false;
#endif
        }

        public static bool IsDevOrEditor()
        {
            return IsEditor() || IsDev();
        }

        public static bool IsReal()
        {
#if MARKET_BUILD
            return true;
#else
            return false;
#endif
        }

        public static string GetPlatformType()
        {
#if UNITY_EDITOR
            return "Editor";
#elif UNITY_IOS || UNITY_IPHONE
            return "iOS";
#elif UNITY_ANDROID
            return "Android";
#else
            return $"Unknown ({UnityEngine.Application.platform})";
#endif
        }
    }
}
