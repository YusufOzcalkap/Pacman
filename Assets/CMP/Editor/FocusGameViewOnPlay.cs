using System;
using UnityEditor;

namespace CMP.Editor
{
    /// <summary>
    /// Play'e basıldığı anda oyun görünümünü klavye odağına alır.
    ///
    /// Editörde odak başka bir penceredeyken (Scene, Inspector, Project...) klavye
    /// girdisi oyuna hiç ulaşmaz; oyuncu ekrana bir kez tıklayana kadar "klavye
    /// çalışmıyor" sanır. Bu script o ilk tıklamayı gereksiz kılar.
    ///
    /// Yalnızca editörde çalışır; build'lerde pencere odağı zaten oyundadır.
    /// </summary>
    [InitializeOnLoad]
    public static class FocusGameViewOnPlay
    {
        static FocusGameViewOnPlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            // Önce Simulator, sonra Game: ikisi de açıksa Game görünümü kazanır,
            // çünkü klavye girdisini en güvenilir ileten o.
            FocusViewIfOpen("UnityEditor.DeviceSimulation.SimulatorWindow,UnityEditor.DeviceSimulatorModule");
            FocusViewIfOpen("UnityEditor.GameView,UnityEditor");
        }

        private static void FocusViewIfOpen(string qualifiedTypeName)
        {
            var viewType = Type.GetType(qualifiedTypeName);
            if (viewType != null)
            {
                EditorWindow.FocusWindowIfItsOpen(viewType);
            }
        }
    }
}
