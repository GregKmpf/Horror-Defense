using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Unity opens an empty scene on a fresh clone because the last opened scene is kept in Library, which isn't
// in git. This opens the first scene in Build Settings instead, so the project always starts on the game.
[InitializeOnLoad]
static class OpenDefaultScene
{
    const string CheckedKey = "OpenDefaultScene.Checked";

    static OpenDefaultScene()
    {
        if (Application.isBatchMode || SessionState.GetBool(CheckedKey, false))
        {
            return;
        }

        SessionState.SetBool(CheckedKey, true);
        EditorApplication.delayCall += OpenIfEmpty;
    }

    static void OpenIfEmpty()
    {
        bool isEmptyScene = string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path);
        if (!isEmptyScene || EditorSceneManager.GetActiveScene().isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                EditorSceneManager.OpenScene(scene.path);
                return;
            }
        }
    }
}
