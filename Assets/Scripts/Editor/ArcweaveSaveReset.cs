using Arcweave;
using UnityEditor;
using UnityEngine;

public static class ArcweaveSaveReset
{
    private const string MENU_PATH = "Tools/Arcweave/Reset Saved Progress";

    [MenuItem(MENU_PATH)]
    private static void ResetSavedProgress()
    {
        new ArcweaveProgressStore(ArcweavePlayer.SAVE_KEY).Clear();

        Debug.Log("Arcweave saved progress cleared. Press Play to start with default values.");
    }

    [MenuItem(MENU_PATH, true)]
    private static bool CanResetSavedProgress()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
