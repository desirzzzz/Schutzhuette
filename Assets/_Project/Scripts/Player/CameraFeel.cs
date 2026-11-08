using UnityEngine;

namespace Schutzhuette.Player
{
    /// <summary>
    /// Lässt das Gewicht in der eigenen Kamera spüren: leichtes Wippen und Schwanken im
    /// Schrittrhythmus, stärker mit Last, und ein kurzer Ruck, wenn etwas an einem zieht
    /// oder die Last irgendwo hängen bleibt.
    /// </summary>
    /// <remarks>
    /// Nur Darstellung auf dem eigenen Rechner, nichts davon geht ins Netz.
    /// <see cref="PlayerLook"/> hängt diese Komponente zur Laufzeit an die eigene Figur.
    /// Sie verschiebt und dreht die Kamera nur relativ zu ihrem Halter, das Umschauen
    /// (Halter drehen) bleibt davon unberührt.
    /// </remarks>
    public class CameraFeel : MonoBehaviour
    {
        [Tooltip("Wippen ohne und mit voller Last, in Metern.")]
        [SerializeField] private float _bobHeight = 0.008f;
        [SerializeField] private float _loadedBobHeight = 0.07f;

        [Tooltip("Seitliches Schwanken ohne und mit voller Last, in Grad.")]
        [SerializeField] private float _swayDegrees = 0.2f;
        [SerializeField] private float _loadedSwayDegrees = 2.8f;

        [Tooltip("Schritte pro Meter (wie schnell das Wippen geht).")]
        [SerializeField] private float _stepsPerMeter = 0.9f;

        [Tooltip("Wie schnell ein Ruck abklingt (pro Sekunde).")]
        [SerializeField] private float _kickDamping = 9f;

        private Transform _camera;
        private Vector3 _lastPosition;
        private float _phase;
        private float _load;
        private float _frameLoad;
        private Vector3 _kick;

        /// <summary>Die Kamera, die bewegt wird (Kind des Kopf-Halters).</summary>
        public void SetCamera(Transform cameraTransform)
        {
            _camera = cameraTransform;
            _lastPosition = transform.position;
        }

        /// <summary>
        /// Meldet für dieses Frame, wie schwer eine Last wirkt: 0 = nichts, 1 = sehr schwer.
        /// Mehrere Meldungen im selben Frame: Die schwerste zählt. Ohne Meldung gilt 0.
        /// </summary>
        public void AddLoad(float load)
        {
            _frameLoad = Mathf.Max(_frameLoad, Mathf.Clamp01(load));
        }

        /// <summary>
        /// Kurzer Ruck in eine Richtung (Weltkoordinaten). Stärke etwa 0 bis 1.
        /// </summary>
        public void Kick(Vector3 worldDirection, float strength)
        {
            Vector3 local = transform.InverseTransformDirection(worldDirection);
            local.y = 0f;
            if (local.sqrMagnitude > 0.0001f)
            {
                _kick += local.normalized * Mathf.Clamp01(strength) * 0.08f;
            }
        }

        private void LateUpdate()
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);

            // Last weich nachziehen, damit Aufheben und Ablegen nicht ruckartig wirken.
            _load = Mathf.MoveTowards(_load, _frameLoad, 2f * deltaTime);
            _frameLoad = 0f;

            if (_camera == null)
            {
                return;
            }
            Vector3 movement = transform.position - _lastPosition;
            _lastPosition = transform.position;
            movement.y = 0f;
            float speed = movement.magnitude / deltaTime;
            float moving = Mathf.Clamp01(speed / 3f);

            // Ein Wipp-Zyklus pro Schritt, Schwanken einmal pro zwei Schritte.
            _phase += speed * deltaTime * _stepsPerMeter * Mathf.PI;
            float bobHeight = Mathf.Lerp(_bobHeight, _loadedBobHeight, _load);
            float swayDegrees = Mathf.Lerp(_swayDegrees, _loadedSwayDegrees, _load);
            float bob = (Mathf.Abs(Mathf.Sin(_phase)) - 0.5f) * 2f * bobHeight * moving;
            float sway = Mathf.Sin(_phase * 0.5f) * swayDegrees * moving;

            _kick = Vector3.Lerp(_kick, Vector3.zero, 1f - Mathf.Exp(-_kickDamping * deltaTime));

            _camera.localPosition = new Vector3(_kick.x, bob, _kick.z);
            _camera.localRotation = Quaternion.Euler(_kick.z * 60f, 0f, sway - _kick.x * 60f);
        }
    }
}
