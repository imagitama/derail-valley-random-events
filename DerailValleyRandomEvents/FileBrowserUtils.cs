using UnityEngine;

namespace DerailValleyRandomEvents;

public static class FileBrowserUtils
{
    public static void OpenFolder(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return;

        Application.OpenURL("file://" + absolutePath.Replace("\\", "/"));
    }
}
