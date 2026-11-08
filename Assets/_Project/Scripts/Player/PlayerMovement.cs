using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Schutzhuette.Player
{
    /// <summary>
    /// Laufen mit WASD (oder linkem Stick) plus Schwerkraft, nur für die eigene Figur.
    /// </summary>
    /// <remarks>
    /// Netcode: Die eigene Figur bewegt der Spieler selbst (Owner-Autorität).
    /// Der NetworkTransform auf dem Prefab steht auf "Owner" und schickt die
    /// Position an Host und alle anderen. Auf fremden Figuren läuft diese
    /// Komponente nicht, dort setzt nur der NetworkTransform die Position.
    /// Collider: Die eigene Figur kollidiert über den CharacterController.
    /// Fremde Figuren behalten ihren CapsuleCollider, damit man nicht durch
    /// sie hindurchläuft. Pro Figur ist immer genau einer der beiden aktiv.
    /// </remarks>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        [Tooltip("Laufgeschwindigkeit in Metern pro Sekunde.")]
        [SerializeField] private float _walkSpeed = 4f;

        [Tooltip("Schwerkraft in m/s² (negativ = nach unten).")]
        [SerializeField] private float _gravity = -9.81f;

        private CharacterController _controller;
        private CapsuleCollider _proxyCollider;
        private InputAction _moveAction;
        private float _verticalVelocity;

        // Komponenten, die das Laufen bremsen oder begrenzen (z. B. schweres Tragen).
        private IMovementModifier[] _modifiers = new IMovementModifier[0];

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            _controller = GetComponent<CharacterController>();
            _proxyCollider = GetComponent<CapsuleCollider>();

            _controller.enabled = IsOwner;
            if (_proxyCollider != null)
            {
                _proxyCollider.enabled = !IsOwner;
            }

            if (!IsOwner)
            {
                return;
            }

            _modifiers = GetComponents<IMovementModifier>();

            // Projektweite Input Actions (InputSystem_Actions, Map "Player").
            if (InputSystem.actions != null)
            {
                _moveAction = InputSystem.actions.FindAction("Player/Move");
            }

            if (_moveAction == null)
            {
                Debug.LogWarning("[Player] Input Action \"Player/Move\" nicht gefunden.", this);
                return;
            }

            _moveAction.Enable();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || _moveAction == null)
            {
                return;
            }

            // Eingabe relativ zur Blickrichtung des Körpers.
            Vector2 input = _moveAction.ReadValue<Vector2>();
            Vector3 move = transform.right * input.x + transform.forward * input.y;

            float speed = _walkSpeed;
            foreach (IMovementModifier modifier in _modifiers)
            {
                speed *= modifier.SpeedMultiplier;
            }

            Vector3 horizontalDelta = Vector3.ClampMagnitude(move, 1f) * (speed * Time.deltaTime);
            foreach (IMovementModifier modifier in _modifiers)
            {
                horizontalDelta = modifier.ConstrainMove(transform.position, horizontalDelta);
            }

            // Am Boden leicht andrücken, damit isGrounded stabil bleibt.
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += _gravity * Time.deltaTime;

            _controller.Move(horizontalDelta + Vector3.up * (_verticalVelocity * Time.deltaTime));
        }
    }
}
