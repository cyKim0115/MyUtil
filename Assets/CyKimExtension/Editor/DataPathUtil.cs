using UnityEditor;
using UnityEngine;
using System.Diagnostics;

public static class DataPathUtil
{
    [MenuItem("Tools/Data/Data Path Open")]
    private static void OpenDataPath()
    {
        string path = Application.persistentDataPath;

#if UNITY_EDITOR_OSX
        bool openInsidesOfFolder = false;

        string macPath = path.Replace("\\", "/");

        if (System.IO.Directory.Exists(macPath))
        {
            openInsidesOfFolder = true;
        }

        if (!macPath.StartsWith("\""))
        {
            macPath = "\"" + macPath;
        }

        if (!macPath.EndsWith("\""))
        {
            macPath += "\"";
        }

        string arguments = (openInsidesOfFolder ? "" : "-R ") + macPath;

        try
        {
            Process.Start("open", arguments);
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            e.HelpLink = "";
        }

#elif UNITY_EDITOR_WIN
        bool openInsidesOfFolder = false;

        string winPath = path.Replace("/", "\\");

        if (System.IO.Directory.Exists(winPath))
            openInsidesOfFolder = true;

        try
        {
            System.Diagnostics.Process.Start("explorer.exe", (openInsidesOfFolder ? "/root," : "/select,") + "\"" + winPath + "\"");
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            e.HelpLink = "";
        }
#endif

        UnityEngine.Debug.Log($"Opened Data Path: {path}");
    }
}