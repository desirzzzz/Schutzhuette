using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Schutzhuette.Carrying
{
    /// <summary>
    /// Aufheben, Ablegen und Werfen für die eigene Spielfigur, dazu Fadenkreuz und Tastenhinweis.
    /// E hebt auf oder legt ab, Linksklick wirft.
    /// </summary>
    /// <remarks>
    /// Netcode: Der Spieler entscheidet nicht selbst. Er schickt eine Bitte per RPC an den
    /// Host ([Rpc(SendTo.Server)]), der Host prüft sie und führt sie aus (Architektur-Regel 2).
    /// Beim Ablegen und Werfen schickt der Spieler seine eigene Sicht der Kistenposition mit:
    /// Bei ihm klebt die Kiste ohne Verzögerung vor der Figur, der Host sieht die Figur
    /// dagegen um den Ping verzögert. Der Host übernimmt die Position nur, wenn sie
    /// plausibel nah an der Figur liegt.
    /// </remarks>
    public class PlayerCarry : NetworkBehaviour
    {
        [Tooltip("Kopf-Punkt, von dem aus gezielt wird (derselbe wie bei PlayerLook).")]
        [SerializeField] private Transform _cameraMount;

        [Tooltip("Wie weit man eine Kiste anvisieren kann, in Metern.")]
        [SerializeField] private float _interactRange = 2.5f;

        [Tooltip("Haltepunkt der Kiste relativ zur Spielfigur (Mitte der Kapsel).")]
        [SerializeField] private Vector3 _holdLocalPosition = new Vector3(0f, 0.2f, 0.9f);

        [Tooltip("Wurfgeschwindigkeit in Blickrichtung, in m/s.")]
        [SerializeField] private float _throwSpeed = 8f;

        [Tooltip("Höchste Wurfgeschwindigkeit, die der Host annimmt.")]
        [SerializeField] private float _maxThrowSpeed = 15f;

        [Tooltip("Höchster Abstand Figur–Kiste, den der Host bei Bitten annimmt.")]
        [SerializeField] private float _maxServerDistance = 4f;

        private InputAction _interactAction;
        private InputAction _throwAction;
        private CharacterController _controller;
        private Carryable _lookTarget;
        private bool _cursorWasLocked;
        private GUIStyle _hintStyle;

        /// <summary>Der Gegenstand, den diese Figur gerade trägt, sonst null. Stimmt auf Host und Clients.</summary>
        public Carryable HeldItem => GetComponentInChildren<Carryable>();

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                return;
            }

            _controller = GetComponent<CharacterController>();

            if (InputSystem.actions != null)
            {
                _interactAction = InputSystem.actions.FindAction("Player/Interact");
                _throwAction = InputSystem.actions.FindAction("Player/Attack");
                _interactAction?.Enable();
                _throwAction?.Enable();
            }

            if (_interactAction == null || _throwAction == null)
            {
                Debug.LogWarning("[Carry] Input Actions \"Player/Interact\" oder \"Player/Attack\" nicht gefunden.", this);
            }
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            // Nicht in dem Frame reagieren, in dem ein Klick die Maus erst einfängt,
            // sonst wirft der Klick zum Einfangen gleich die Kiste.
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            bool inputAllowed = locked && _cursorWasLocked;
            _cursorWasLocked = locked;

            Carryable held = HeldItem;
            _lookTarget = held == null ? FindLookTarget() : null;

            if (!inputAllowed)
            {
                return;
            }

            if (_interactAction != null && _interactAction.WasPressedThisFrame())
            {
                if (held != null)
                {
                    DropRpc(held.transform.position, held.transform.rotation, Vector3.zero);
                }
                else if (_lookTarget != null)
                {
                    PickUpRpc(_lookTarget.NetworkObject);
                }
            }
            else if (held != null && _throwAction != null && _throwAction.WasPressedThisFrame())
            {
                // Eigene Laufgeschwindigkeit mitgeben, sonst wirkt ein Wurf aus dem Laufen zu schwach.
                Vector3 velocity = _cameraMount.forward * _throwSpeed;
                if (_controller != null)
                {
                    velocity += _controller.velocity;
                }

                DropRpc(held.transform.position, held.transform.rotation, velocity);
            }
        }

        // Kiste in Blickrichtung, die gerade frei ist, sonst null.
        private Carryable FindLookTarget()
        {
            if (_cameraMount == null)
            {
                return null;
            }

            // Der Strahl startet im eigenen CharacterController. Collider, in denen ein
            // Strahl beginnt, trifft er nicht, deshalb stört die eigene Figur nicht.
            if (!Physics.Raycast(_cameraMount.position, _cameraMount.forward, out RaycastHit hit,
                    _interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            var item = hit.collider.GetComponentInParent<Carryable>();
            return item != null && !item.IsCarried ? item : null;
        }

        // Läuft auf dem Host. Nur der Besitzer dieser Figur darf sie aufrufen.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void PickUpRpc(NetworkObjectReference itemReference)
        {
            if (HeldItem != null)
            {
                return;
            }

            if (!itemReference.TryGet(out NetworkObject itemObject)
                || !itemObject.TryGetComponent(out Carryable item)
                || item.IsCarried)
            {
                return;
            }

            if (Vector3.Distance(item.transform.position, transform.position) > _maxServerDistance)
            {
                return;
            }

            item.AttachTo(NetworkObject, _holdLocalPosition);
        }

        // Läuft auf dem Host. velocity = Vector3.zero heißt ablegen, sonst werfen.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void DropRpc(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            Carryable item = HeldItem;
            if (item == null)
            {
                return;
            }

            // Unplausible Position: lieber die eigene Sicht des Hosts nehmen.
            if (Vector3.Distance(position, transform.position) > _maxServerDistance)
            {
                position = item.transform.position;
            }

            item.Detach(position, rotation, Vector3.ClampMagnitude(velocity, _maxThrowSpeed));
        }

        private void OnGUI()
        {
            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            GUI.matrix = Matrix4x4.identity;
            _hintStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
            };

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            GUI.Label(new Rect(centerX - 20f, centerY - 15f, 40f, 30f), "+", _hintStyle);

            string hint = HeldItem != null
                ? "E: ablegen     Linksklick: werfen"
                : _lookTarget != null ? "E: aufheben" : null;

            if (hint != null)
            {
                GUI.Label(new Rect(centerX - 200f, Screen.height - 70f, 400f, 30f), hint, _hintStyle);
            }
        }
    }
}
