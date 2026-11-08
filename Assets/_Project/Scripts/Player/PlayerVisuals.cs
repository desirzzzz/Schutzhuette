using Schutzhuette.Carrying;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Schutzhuette.Player
{
    /// <summary>
    /// Darstellung der Spielfigur: Farbe je Spieler, leichtes Neigen und Wippen beim Laufen,
    /// und Arme, die sichtbar nach dem getragenen Gegenstand greifen.
    /// </summary>
    /// <remarks>
    /// Netcode: Hier wird nichts übertragen. Jeder Rechner rechnet die Darstellung selbst aus
    /// dem, was er ohnehin kennt: der (synchronisierten) Position der Figur, ob sie eine Kiste
    /// trägt (über das Eltern-Objekt der Kiste) und welches Truhenende sie hält
    /// (NetworkVariable der Truhe). Die Farbe folgt aus der Client-ID des Besitzers.
    /// Die eigene Figur blendet ihren Kopf für die eigene Kamera aus (nur Schatten).
    /// </remarks>
    public class PlayerVisuals : NetworkBehaviour
    {
        [Tooltip("Wurzel der Figur, wird zum Neigen und Wippen bewegt.")]
        [SerializeField] private Transform _figure;

        [Tooltip("Kopf und Augen: für die eigene Kamera nur als Schatten sichtbar.")]
        [SerializeField] private Renderer[] _headRenderers = new Renderer[0];

        [Tooltip("Teile, die die Spielerfarbe bekommen (Körper, Arme).")]
        [SerializeField] private Renderer[] _colorRenderers = new Renderer[0];

        [Tooltip("Schulter-Gelenke. Ihr Kind ist der Arm, der entlang der lokalen z-Achse zeigt.")]
        [SerializeField] private Transform _leftArmPivot;
        [SerializeField] private Transform _rightArmPivot;

        [Tooltip("Abstand der Hände zur Mitte des gegriffenen Gegenstands, seitlich (Meter).")]
        [SerializeField] private float _handSpread = 0.25f;

        [Tooltip("Armlänge im Leerlauf und höchste Länge beim Greifen (Meter).")]
        [SerializeField] private float _armRestLength = 0.6f;
        [SerializeField] private float _armMaxLength = 1.1f;

        [Tooltip("Stärke von Neigen (Grad pro m/s) und Wippen (Meter).")]
        [SerializeField] private float _leanPerSpeed = 3f;
        [SerializeField] private float _bobHeight = 0.05f;

        private PlayerCarry _carry;
        private HeavyCarryable _heldChest;
        private Vector3 _lastPosition;
        private float _walkPhase;
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>Wird von der Truhe aufgerufen: Diese Figur hält jetzt ein Ende von ihr.</summary>
        public void SetHeldChest(HeavyCarryable chest)
        {
            _heldChest = chest;
        }

        /// <summary>Wird von der Truhe aufgerufen: Diese Figur hält kein Ende von ihr mehr.</summary>
        public void ClearHeldChest(HeavyCarryable chest)
        {
            if (_heldChest == chest)
            {
                _heldChest = null;
            }
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            _carry = GetComponent<PlayerCarry>();
            _lastPosition = transform.position;

            ApplyColor(PlayerColor(OwnerClientId));

            // Eigener Kopf: würde sonst die Sicht verdecken, sein Schatten bleibt.
            if (IsOwner)
            {
                foreach (Renderer headRenderer in _headRenderers)
                {
                    headRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        private void LateUpdate()
        {
            if (!IsSpawned || _figure == null)
            {
                return;
            }

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 velocity = (transform.position - _lastPosition) / deltaTime;
            _lastPosition = transform.position;
            velocity.y = 0f;
            float speed = velocity.magnitude;

            // Neigen in Laufrichtung, leicht wackelig.
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            float pitch = Mathf.Clamp(localVelocity.z * _leanPerSpeed, -8f, 8f);
            float roll = Mathf.Clamp(-localVelocity.x * _leanPerSpeed, -8f, 8f);
            _figure.localRotation = Quaternion.Slerp(_figure.localRotation, Quaternion.Euler(pitch, 0f, roll), 10f * deltaTime);

            // Wippen im Schritt.
            float moving = Mathf.Clamp01(speed / 4f);
            _walkPhase += speed * deltaTime * 2.2f;
            _figure.localPosition = Vector3.up * (Mathf.Abs(Mathf.Sin(_walkPhase)) * _bobHeight * moving);

            if (TryGetHandTargets(out Vector3 leftTarget, out Vector3 rightTarget))
            {
                AimArm(_leftArmPivot, leftTarget, deltaTime);
                AimArm(_rightArmPivot, rightTarget, deltaTime);
            }
            else
            {
                float swing = Mathf.Sin(_walkPhase) * 25f * moving;
                HangArm(_leftArmPivot, swing, deltaTime);
                HangArm(_rightArmPivot, -swing, deltaTime);
            }
        }

        // Wohin greifen die Hände gerade? Kiste vor dem Körper oder das eigene Truhenende.
        private bool TryGetHandTargets(out Vector3 left, out Vector3 right)
        {
            left = right = Vector3.zero;

            if (_carry != null && _carry.TryGetHoldPoint(out Vector3 holdPoint))
            {
                Vector3 side = transform.right * _handSpread;
                left = holdPoint - side;
                right = holdPoint + side;
                return true;
            }

            if (_heldChest == null || !_heldChest.IsHeldBy(NetworkObject, out int end))
            {
                return false;
            }

            Vector3 hand;
            Vector3 sideways;
            if (_heldChest.TryGetPartner(NetworkObject, out NetworkObject partner))
            {
                // Dieselbe Hand-Stelle, an der die Truhe beim Tragen hängt.
                Vector3 toPartner = partner.transform.position - transform.position;
                toPartner.y = 0f;
                Vector3 direction = toPartner.sqrMagnitude > 0.0001f ? toPartner.normalized : transform.forward;
                hand = transform.position + direction * _heldChest.Reach + Vector3.up * _heldChest.HandHeight;
                sideways = Vector3.Cross(Vector3.up, direction) * _handSpread;
            }
            else
            {
                // Allein: Die Truhe liegt, die Hände greifen nach ihrem Ende.
                hand = _heldChest.GetEndPosition(end);
                sideways = transform.right * _handSpread;
            }

            left = hand - sideways;
            right = hand + sideways;
            return true;
        }

        // Arm zum Ziel drehen und so lang machen, dass die Hand ungefähr ankommt.
        private void AimArm(Transform pivot, Vector3 target, float deltaTime)
        {
            if (pivot == null)
            {
                return;
            }

            Vector3 toTarget = target - pivot.position;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion goal = Quaternion.LookRotation(toTarget, transform.up);
            pivot.rotation = Quaternion.Slerp(pivot.rotation, goal, 15f * deltaTime);
            SetArmLength(pivot, Mathf.Clamp(toTarget.magnitude, _armRestLength, _armMaxLength));
        }

        // Arm hängen lassen, beim Laufen vor und zurück schwingen.
        private void HangArm(Transform pivot, float swingDegrees, float deltaTime)
        {
            if (pivot == null)
            {
                return;
            }

            Vector3 down = Quaternion.AngleAxis(swingDegrees, transform.right) * Vector3.down;
            Quaternion goal = Quaternion.LookRotation(down, transform.forward);
            pivot.rotation = Quaternion.Slerp(pivot.rotation, goal, 10f * deltaTime);
            SetArmLength(pivot, _armRestLength);
        }

        // Das Kind des Gelenks ist eine Kapsel entlang z: Länge = 2 * Skalierung y.
        private static void SetArmLength(Transform pivot, float length)
        {
            if (pivot.childCount == 0)
            {
                return;
            }

            Transform arm = pivot.GetChild(0);
            Vector3 scale = arm.localScale;
            scale.y = length * 0.5f;
            arm.localScale = scale;
            arm.localPosition = new Vector3(0f, 0f, length * 0.5f);
        }

        private void ApplyColor(Color color)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            foreach (Renderer colorRenderer in _colorRenderers)
            {
                colorRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", color);
                colorRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        /// <summary>Feste Farbe je Spieler-Nummer: rot, blau, grün, gelb, dann wieder von vorn.</summary>
        public static Color PlayerColor(ulong clientId)
        {
            switch (clientId % 4)
            {
                case 0: return new Color(0.85f, 0.25f, 0.2f);
                case 1: return new Color(0.2f, 0.45f, 0.85f);
                case 2: return new Color(0.3f, 0.7f, 0.3f);
                default: return new Color(0.9f, 0.75f, 0.2f);
            }
        }
    }
}
