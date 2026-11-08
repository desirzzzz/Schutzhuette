using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Schutzhuette.Player
{
    /// <summary>
    /// Ich-Perspektive für die eigene Figur: Maus links/rechts dreht den Körper,
    /// hoch/runter neigt nur die Kamera. Hängt die Szenen-Kamera an den Kopf.
    /// </summary>
    /// <remarks>
    /// Netcode: Nur die Körperdrehung (Y-Achse) wird über den NetworkTransform
    /// geteilt. Das Neigen der Kamera bleibt lokal, die anderen sehen es nicht.
    /// Esc gibt den Mauszeiger frei (für den Trennen-Knopf), ein Linksklick
    /// fängt ihn wieder ein.
    /// </remarks>
    public class PlayerLook : NetworkBehaviour
    {
        [Tooltip("Kopf-Punkt auf Augenhöhe. Hier hängt die Kamera der eigenen Figur.")]
        [SerializeField] private Transform _cameraMount;

        [Tooltip("Grad pro Pixel Mausbewegung.")]
        [SerializeField] private float _mouseSensitivity = 0.1f;

        [Tooltip("Grad pro Sekunde bei voll ausgelenktem Stick.")]
        [SerializeField] private float _stickDegreesPerSecond = 180f;

        [Tooltip("Wie weit man nach unten/oben schauen kann, in Grad.")]
        [SerializeField, Range(10f, 89f)] private float _maxPitch = 80f;

        private InputAction _lookAction;
        private IMovementModifier[] _modifiers = new IMovementModifier[0];
        private Transform _camera;
        private Vector3 _cameraHomePosition;
        private Quaternion _cameraHomeRotation;
        private float _pitch;

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                return;
            }

            _modifiers = GetComponents<IMovementModifier>();

            if (InputSystem.actions != null)
            {
                _lookAction = InputSystem.actions.FindAction("Player/Look");
                _lookAction?.Enable();
            }

            if (_lookAction == null)
            {
                Debug.LogWarning("[Player] Input Action \"Player/Look\" nicht gefunden.", this);
            }

            AttachSceneCamera();
            SetCursorLocked(true);
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            // Kamera vor dem Zerstören der Figur zurück in die Szene holen,
            // sonst verschwindet sie beim Trennen mit.
            DetachSceneCamera();
            SetCursorLocked(false);
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            HandleCursorToggle();

            if (_lookAction == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 raw = _lookAction.ReadValue<Vector2>();

            // Maus liefert Pixel pro Frame, ein Stick liefert -1..1. Deshalb unterschiedlich skalieren.
            bool fromPointer = _lookAction.activeControl != null && _lookAction.activeControl.device is Pointer;
            Vector2 degrees = fromPointer
                ? raw * _mouseSensitivity
                : raw * (_stickDegreesPerSecond * Time.deltaTime);

            // Drehen kann begrenzt sein, z. B. wenn eine getragene Kiste sonst in die Wand schwenkt.
            float yaw = degrees.x;
            foreach (IMovementModifier modifier in _modifiers)
            {
                yaw = modifier.ConstrainYaw(transform.position, transform.rotation, yaw);
            }

            transform.Rotate(0f, yaw, 0f);

            _pitch = Mathf.Clamp(_pitch - degrees.y, -_maxPitch, _maxPitch);
            if (_cameraMount != null)
            {
                _cameraMount.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void HandleCursorToggle()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
            }
            else if (Cursor.lockState != CursorLockMode.Locked
                     && Mouse.current != null
                     && Mouse.current.leftButton.wasPressedThisFrame)
            {
                SetCursorLocked(true);
            }
        }

        private void AttachSceneCamera()
        {
            Camera sceneCamera = Camera.main;
            if (sceneCamera == null || _cameraMount == null)
            {
                Debug.LogWarning("[Player] Keine Main Camera oder kein Kamera-Halter gefunden.", this);
                return;
            }

            _camera = sceneCamera.transform;
            _cameraHomePosition = _camera.position;
            _cameraHomeRotation = _camera.rotation;
            _camera.SetParent(_cameraMount, false);
            _camera.localPosition = Vector3.zero;
            _camera.localRotation = Quaternion.identity;
        }

        private void DetachSceneCamera()
        {
            if (_camera == null)
            {
                return;
            }

            _camera.SetParent(null, false);
            _camera.SetPositionAndRotation(_cameraHomePosition, _cameraHomeRotation);
            _camera = null;
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
