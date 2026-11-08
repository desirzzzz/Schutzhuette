using Schutzhuette.Carrying;
using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Hut
{
    /// <summary>
    /// Holzofen: verbraucht Brennstoff, solange er brennt, und macht die Hütte warm.
    /// Ohne Nachschub geht er aus und die Wärme sinkt. Holz kommt hinein, indem man
    /// einen <see cref="FuelItem"/> vor die Ofentür legt oder wirft.
    /// </summary>
    /// <remarks>
    /// Netcode: Brennstoff und Wärme sind NetworkVariables, die nur der Host ändert
    /// (Architektur-Regel 2). Licht, Feuer und Anzeige liest jeder Rechner daraus.
    /// Holz "verbrennt" nur der Host: Landet ein Scheit im Auslöser vor der Ofentür,
    /// zählt er die Brenndauer dazu und entfernt den Scheit für alle (Despawn).
    /// Der Auslöser ist ein Trigger-Collider auf diesem Objekt, der Ofenkörper ein
    /// eigenes Kind-Objekt mit normalem Collider.
    /// </remarks>
    public class Stove : NetworkBehaviour
    {
        [Tooltip("Werte dieses Ofens (Brenndauer, Vorrat, Wärme).")]
        [SerializeField] private StoveSettings _settings;

        [Tooltip("Licht, das leuchtet, solange der Ofen brennt.")]
        [SerializeField] private Light _fireLight;

        [Tooltip("Sichtbares Feuer in der Ofentür, nur an, solange der Ofen brennt.")]
        [SerializeField] private GameObject _fireVisual;

        private readonly NetworkVariable<float> _fuelSeconds = new NetworkVariable<float>();
        private readonly NetworkVariable<float> _heat = new NetworkVariable<float>();

        private float _baseLightIntensity = 1f;
        private GUIStyle _hudStyle;

        /// <summary>Brennt der Ofen gerade? Stimmt auf Host und Clients.</summary>
        public bool IsBurning => _fuelSeconds.Value > 0f;

        /// <summary>Restlicher Brennstoff in Sekunden.</summary>
        public float FuelSeconds => _fuelSeconds.Value;

        /// <summary>Wärme in Prozent der höchsten Wärme.</summary>
        public float HeatPercent => _settings != null ? _heat.Value / _settings.MaxHeat * 100f : 0f;

        private void Awake()
        {
            if (_fireLight != null)
            {
                _baseLightIntensity = _fireLight.intensity;
            }
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            if (IsServer && _settings != null)
            {
                _fuelSeconds.Value = _settings.StartFuelSeconds;
            }

            if (_settings == null)
            {
                Debug.LogWarning("[Ofen] Keine StoveSettings eingetragen.", this);
            }

            UpdateFireVisuals();
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsServer && _settings != null)
            {
                Step(_fuelSeconds.Value, _heat.Value, _settings, Time.deltaTime, out float fuel, out float heat);
                _fuelSeconds.Value = fuel;
                _heat.Value = heat;
            }

            UpdateFireVisuals();
        }

        /// <summary>
        /// Ein Zeitschritt des Ofens: Brennt er, sinkt der Brennstoff und die Wärme steigt.
        /// Ist er aus, sinkt nur die Wärme. Reine Rechnung ohne Zustand.
        /// </summary>
        public static void Step(float fuelSeconds, float heat, StoveSettings settings, float deltaTime,
            out float newFuelSeconds, out float newHeat)
        {
            if (fuelSeconds > 0f)
            {
                newFuelSeconds = Mathf.Max(0f, fuelSeconds - deltaTime);
                newHeat = Mathf.Min(settings.MaxHeat, heat + settings.HeatRisePerSecond * deltaTime);
            }
            else
            {
                newFuelSeconds = 0f;
                newHeat = Mathf.Max(0f, heat - settings.HeatDecayPerSecond * deltaTime);
            }
        }

        /// <summary>Nur Host: Brennstoff nachlegen.</summary>
        /// <returns>false, wenn der Ofen schon voll ist.</returns>
        public bool TryAddFuel(float seconds)
        {
            if (!IsServer || _settings == null || _fuelSeconds.Value >= _settings.MaxFuelSeconds)
            {
                return false;
            }

            _fuelSeconds.Value = Mathf.Min(_settings.MaxFuelSeconds, _fuelSeconds.Value + seconds);
            return true;
        }

        // Ein Scheit fällt oder fliegt in die Ofenöffnung.
        private void OnTriggerEnter(Collider other)
        {
            TryBurn(other);
        }

        // Ein Scheit liegt schon in der Öffnung, der Ofen hat wieder Platz.
        private void OnTriggerStay(Collider other)
        {
            TryBurn(other);
        }

        // Läuft nur auf dem Host.
        private void TryBurn(Collider other)
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            var fuel = other.GetComponentInParent<FuelItem>();
            if (fuel == null)
            {
                return;
            }

            // Getragene Scheite haben keine Kollision, zur Sicherheit trotzdem prüfen.
            if (fuel.TryGetComponent(out Carryable item) && item.IsCarried)
            {
                return;
            }

            if (!fuel.TryGetComponent(out NetworkObject fuelObject) || !fuelObject.IsSpawned)
            {
                return;
            }

            if (!TryAddFuel(fuel.BurnSeconds))
            {
                return;
            }

            fuelObject.Despawn(true);
            Debug.Log($"[Ofen] Holz nachgelegt, Brennstoff jetzt {FuelSeconds:0} s.");
        }

        // Licht und Feuer folgen dem Zustand. Das leichte Flackern ist nur lokal.
        private void UpdateFireVisuals()
        {
            bool burning = IsBurning;

            if (_fireVisual != null && _fireVisual.activeSelf != burning)
            {
                _fireVisual.SetActive(burning);
            }

            if (_fireLight != null)
            {
                _fireLight.enabled = burning;
                if (burning)
                {
                    float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 3f, 0f);
                    _fireLight.intensity = _baseLightIntensity * flicker;
                }
            }
        }

        private void OnGUI()
        {
            if (!IsSpawned)
            {
                return;
            }

            GUI.matrix = Matrix4x4.identity;
            _hudStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperRight,
                fontSize = 20,
            };

            int seconds = Mathf.CeilToInt(FuelSeconds);
            string text = IsBurning
                ? $"Ofen: brennt {seconds / 60}:{seconds % 60:00} · Wärme {HeatPercent:0} %"
                : $"Ofen: AUS · Wärme {HeatPercent:0} %";

            GUI.Label(new Rect(Screen.width - 520f, 10f, 510f, 30f), text, _hudStyle);
        }
    }
}
