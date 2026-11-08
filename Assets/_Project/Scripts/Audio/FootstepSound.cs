using Schutzhuette.Carrying;
using Schutzhuette.Player;
using UnityEngine;

namespace Schutzhuette.Audio
{
    /// <summary>
    /// Schritte einer Spielfigur: auf Holz unter dem Dach, im Schnee draußen. Mit Last
    /// klingen sie tiefer und lauter und kommen seltener.
    /// </summary>
    /// <remarks>
    /// Netcode: Hier geht nichts übers Netz. Jeder Rechner erzeugt die Schritte aller Figuren
    /// selbst aus dem, was er ohnehin kennt: der (über NetworkTransform synchronisierten)
    /// Position der Figur, der getragenen Kiste (Eltern-Objekt, von NGO übertragen) und dem
    /// gehaltenen Truhenende (NetworkVariable der Truhe). So hört jeder alle Schritte, ohne
    /// dass für jeden Schritt eine Nachricht nötig ist.
    /// Ein Schritt fällt alle paar Meter zurückgelegter Strecke. Wer langsamer läuft
    /// (mit Last), macht also von selbst seltener Schritte.
    /// </remarks>
    public class FootstepSound : MonoBehaviour
    {
        [Tooltip("Geräusch-Einstellungen (Clips, Lautstärken, Schrittlänge).")]
        [SerializeField] private SoundSettings _settings;

        private AudioSource _source;
        private PlayerCarry _carry;
        private PlayerVisuals _visuals;
        private Vector3 _lastPosition;
        private bool _hasLastPosition;
        private float _distance;
        private int _lastWoodClip = -1;
        private int _lastSnowClip = -1;

        private void Awake()
        {
            _carry = GetComponent<PlayerCarry>();
            _visuals = GetComponent<PlayerVisuals>();

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 1f;
            _source.rolloffMode = AudioRolloffMode.Logarithmic;
            _source.minDistance = 1f;
            _source.maxDistance = _settings != null ? _settings.StepMaxDistance : 20f;
            _source.dopplerLevel = 0f;
        }

        private void LateUpdate()
        {
            if (_settings == null)
            {
                return;
            }

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 position = transform.position;
            Vector3 delta = position - _lastPosition;
            _lastPosition = position;
            if (!_hasLastPosition)
            {
                _hasLastPosition = true;
                return;
            }

            float verticalSpeed = delta.y / deltaTime;
            delta.y = 0f;
            float moved = delta.magnitude;

            // Sprung an eine neue Stelle (Spawn) oder Fallen: keine Schritte.
            if (moved > 2f || Mathf.Abs(verticalSpeed) > 3f)
            {
                return;
            }

            float load = LoadFactor();
            float stepLength = Mathf.Lerp(_settings.StepLength, _settings.LoadedStepLength, load);

            // Steht die Figur, kommt der erste Schritt nach dem Losgehen bald, nicht erst nach einem ganzen Schritt.
            if (moved / deltaTime < 0.2f)
            {
                _distance = Mathf.Min(_distance, stepLength * 0.6f);
                return;
            }

            _distance += moved;
            if (_distance >= stepLength)
            {
                _distance -= stepLength;
                PlayStep(load);
            }
        }

        // 0 = ohne Last, 1 = volle Last. Eine Kiste trägt man allein, die Truhe zu zweit.
        private float LoadFactor()
        {
            float mass = 0f;

            if (_carry != null)
            {
                Carryable item = _carry.HeldItem;
                if (item != null)
                {
                    mass = item.Mass;
                }
            }

            HeavyCarryable chest = _visuals != null ? _visuals.HeldChest : null;
            if (chest != null && chest.TryGetComponent(out Rigidbody chestBody))
            {
                mass = Mathf.Max(mass, chestBody.mass / Mathf.Max(1, chest.HolderCount));
            }

            return Mathf.Clamp01(mass / _settings.FullLoadMass);
        }

        private void PlayStep(float load)
        {
            bool indoors = Shelter.IsUnderRoof(transform.position, _settings.RoofCheckHeight);
            AudioClip clip = indoors
                ? OneShotAudio.PickClip(_settings.WoodSteps, ref _lastWoodClip)
                : OneShotAudio.PickClip(_settings.SnowSteps, ref _lastSnowClip);

            if (clip == null)
            {
                return;
            }

            _source.pitch = Mathf.Lerp(_settings.StepPitch, _settings.LoadedStepPitch, load) * Random.Range(0.93f, 1.07f);
            _source.PlayOneShot(clip, Mathf.Lerp(_settings.StepVolume, _settings.LoadedStepVolume, load));
        }
    }
}
