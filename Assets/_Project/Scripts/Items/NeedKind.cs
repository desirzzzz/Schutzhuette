using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Ein Gästebedürfnis, zum Beispiel "etwas Warmes trinken". Gerichte verweisen darauf,
    /// Gästetypen ebenfalls (ADR-0002, Entscheidung 1). Welche Bedürfnisse es gibt, legt das
    /// Gäste-Dokument fest.
    /// </summary>
    /// <remarks>
    /// Die Kennung beginnt immer mit "beduerfnis_". Zur Laufzeit schreibgeschützt.
    /// </remarks>
    [CreateAssetMenu(fileName = "NeedKind", menuName = "Schutzhütte/Bedürfnis")]
    public class NeedKind : ScriptableObject
    {
        [Tooltip("Feste Kennung, beginnt mit beduerfnis_. Höchstens 29 Zeichen.")]
        [SerializeField] private string _id = "";

        [Tooltip("Anzeigename auf Deutsch.")]
        [SerializeField] private string _displayName = "";

        /// <summary>Feste, eindeutige Kennung, zum Beispiel "beduerfnis_warm_trinken".</summary>
        public string Id => _id;

        /// <summary>Anzeigename auf Deutsch.</summary>
        public string DisplayName => _displayName;

#if UNITY_EDITOR
        // Befüllen nur im Editor (Tests und Editor-Skripte).
        internal void EditorSet(string id, string displayName)
        {
            _id = id;
            _displayName = displayName;
        }
#endif
    }
}
