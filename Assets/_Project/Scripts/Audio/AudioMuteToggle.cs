using UnityEngine;
using UnityEngine.InputSystem;

namespace Schutzhuette.Audio
{
    /// <summary>
    /// Taste M schaltet den Ton dieses Spielfensters aus und wieder ein.
    /// </summary>
    /// <remarks>
    /// Testhilfe: Mit zwei Spielfenstern auf einem Rechner hört man sonst jedes Geräusch
    /// doppelt. Im Host-Fenster M drücken, dann hört man nur noch, was beim Client ankommt.
    /// Nur lokal. Setzt den Ton beim Beenden wieder auf laut, damit er im Editor nicht
    /// stumm bleibt.
    /// </remarks>
    public class AudioMuteToggle : MonoBehaviour
    {
        private bool _muted;
        private GUIStyle _style;

        private void Awake()
        {
            SetMuted(false);
        }

        private void OnDestroy()
        {
            AudioListener.volume = 1f;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                SetMuted(!_muted);
            }
        }

        private void SetMuted(bool muted)
        {
            _muted = muted;
            AudioListener.volume = muted ? 0f : 1f;
        }

        private void OnGUI()
        {
            if (!_muted)
            {
                return;
            }

            GUI.matrix = Matrix4x4.identity;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 20 };
            GUI.Label(new Rect(10f, Screen.height - 40f, 300f, 30f), "Ton aus (M)", _style);
        }
    }
}
