using System;
using System.Collections.Generic;
using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Der Katalog: listet alle Gegenstandsarten, Waren und Bedürfnisse und trägt die
    /// Katalog-Einstellungen (ADR-0002, Entscheidung 1).
    /// </summary>
    /// <remarks>
    /// Enthält nur Listen und Einstellungen, keine Nachschlagetabellen: Ohne Domain Reload könnten
    /// nicht gespeicherte Felder eines ScriptableObjects zwischen zwei Play-Durchläufen alt bleiben.
    /// Die Tabellen baut der Laufzeit-Dienst bei jedem Start neu. Zur Laufzeit schreibgeschützt.
    /// </remarks>
    /// <example>
    /// Den Katalog prüfen (Editor, Build, Host-Start):
    /// <code>
    /// CatalogReport report = CatalogValidator.Validate(catalog, networkPrefabs);
    /// if (report.StartBlocked)
    /// {
    ///     Debug.LogError(report.ToString());
    /// }
    /// </code>
    /// </example>
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "Schutzhütte/Katalog")]
    public class ItemCatalog : ScriptableObject
    {
        [Tooltip("Alle Gegenstandsarten mit Körper.")]
        [SerializeField] private List<ItemKind> _kinds = new List<ItemKind>();

        [Tooltip("Alle Waren (ohne Körper).")]
        [SerializeField] private List<WareKind> _wares = new List<WareKind>();

        [Tooltip("Alle Gästebedürfnisse.")]
        [SerializeField] private List<NeedKind> _needs = new List<NeedKind>();

        [Tooltip("Grenzen und Prüfwerte für die Katalogprüfung.")]
        [SerializeField] private CatalogSettings _settings = new CatalogSettings();

        /// <summary>Alle Gegenstandsarten.</summary>
        public IReadOnlyList<ItemKind> Kinds => _kinds;

        /// <summary>Alle Waren.</summary>
        public IReadOnlyList<WareKind> Wares => _wares;

        /// <summary>Alle Bedürfnisse.</summary>
        public IReadOnlyList<NeedKind> Needs => _needs;

        /// <summary>Grenzen und Prüfwerte der Katalogprüfung.</summary>
        public CatalogSettings Settings => _settings;

#if UNITY_EDITOR
        // Befüllen nur im Editor (Tests und Editor-Skripte).
        internal void EditorSetContents(IEnumerable<ItemKind> kinds, IEnumerable<WareKind> wares,
            IEnumerable<NeedKind> needs)
        {
            _kinds = kinds != null ? new List<ItemKind>(kinds) : new List<ItemKind>();
            _wares = wares != null ? new List<WareKind>(wares) : new List<WareKind>();
            _needs = needs != null ? new List<NeedKind>(needs) : new List<NeedKind>();
        }
#endif
    }

    /// <summary>
    /// Einstellungen des Katalogs (GDD Gegenstandsarten, "Tuning Knobs"). Die Katalogprüfung
    /// rechnet damit; im Code stehen diese Zahlen nirgends.
    /// </summary>
    [Serializable]
    public class CatalogSettings
    {
        [Tooltip("max_solo_mass: Schwerere Arten müssen 'zu zweit' sein. Sicherer Bereich 20–40 kg.")]
        [SerializeField, Min(0f)] private float _maxSoloMassKg = 25f;

        [Tooltip("Prüfwert Haltehöhe in m. Muss dem Tragen entsprechen.")]
        [SerializeField, Min(0f)] private float _checkHoldHeightMeters = 1.28f;

        [Tooltip("Prüfwert Wurftempo in m/s. Muss dem Tragen entsprechen.")]
        [SerializeField, Min(0f)] private float _checkThrowSpeed = 8f;

        [Tooltip("floor_factor: Puffer der Untergrenze für die Abkühlzeit. Sicherer Bereich 4–10.")]
        [SerializeField, Min(0f)] private float _floorFactor = 6f;

        [Tooltip("max_path: längster Weg Herd → Stube in m. Sicherer Bereich 10–15 m.")]
        [SerializeField, Min(0f)] private float _maxPathMeters = 15f;

        [Tooltip("walk_speed_min: Lauftempo in m/s für die Untergrenze der Abkühlzeit.")]
        [SerializeField, Min(0f)] private float _walkSpeedMin = 4f;

        [Tooltip("Toleranz beim Hacken als Anteil (0,1 = 10 %). Sicherer Bereich 0,05–0,2.")]
        [SerializeField, Range(0f, 1f)] private float _chopTolerance = 0.1f;

        /// <summary>Höchstmasse in kg, die ein Spieler allein tragen darf (max_solo_mass).</summary>
        public float MaxSoloMassKg => _maxSoloMassKg;

        /// <summary>Prüfwert Haltehöhe in m, Grundlage des Prüffensters für die Bruchgeschwindigkeit.</summary>
        public float CheckHoldHeightMeters => _checkHoldHeightMeters;

        /// <summary>Prüfwert Wurftempo in m/s, obere Grenze des Prüffensters.</summary>
        public float CheckThrowSpeed => _checkThrowSpeed;

        /// <summary>Puffer der Untergrenze für die Abkühlzeit (floor_factor).</summary>
        public float FloorFactor => _floorFactor;

        /// <summary>Längster Weg Herd → Stube in m (max_path).</summary>
        public float MaxPathMeters => _maxPathMeters;

        /// <summary>Lauftempo in m/s für die Untergrenze der Abkühlzeit (walk_speed_min).</summary>
        public float WalkSpeedMin => _walkSpeedMin;

        /// <summary>Toleranz der Massenbilanz beim Hacken als Anteil (0,1 = 10 %).</summary>
        public float ChopTolerance => _chopTolerance;

#if UNITY_EDITOR
        // Befüllen nur im Editor (Tests und Editor-Skripte).
        internal void EditorSet(float maxSoloMassKg, float checkHoldHeightMeters, float checkThrowSpeed,
            float floorFactor, float maxPathMeters, float walkSpeedMin, float chopTolerance)
        {
            _maxSoloMassKg = maxSoloMassKg;
            _checkHoldHeightMeters = checkHoldHeightMeters;
            _checkThrowSpeed = checkThrowSpeed;
            _floorFactor = floorFactor;
            _maxPathMeters = maxPathMeters;
            _walkSpeedMin = walkSpeedMin;
            _chopTolerance = chopTolerance;
        }
#endif
    }
}
