using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Eine Ware wie Speck, Mehl oder Tee: ein schlanker Datentyp ohne Körper. Waren gibt es nur als
    /// Bestand im Lager und als Inhalt eines Vorratspakets (GDD Gegenstandsarten, Regel 3).
    /// </summary>
    /// <remarks>
    /// Die Kennung beginnt immer mit "ware_", damit sie nie mit einer Art zusammenfällt
    /// (Ware "ware_tee", Art "tee"). Zur Laufzeit schreibgeschützt.
    /// </remarks>
    [CreateAssetMenu(fileName = "WareKind", menuName = "Schutzhütte/Ware")]
    public class WareKind : ScriptableObject
    {
        [Tooltip("Feste Kennung, beginnt mit ware_, zum Beispiel ware_tee. Höchstens 29 Zeichen.")]
        [SerializeField] private string _id = "";

        [Tooltip("Anzeigename auf Deutsch.")]
        [SerializeField] private string _displayName = "";

        /// <summary>Feste, eindeutige Kennung, zum Beispiel "ware_tee".</summary>
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
