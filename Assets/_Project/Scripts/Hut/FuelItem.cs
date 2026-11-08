using UnityEngine;

namespace Schutzhuette.Hut
{
    /// <summary>
    /// Macht einen tragbaren Gegenstand zu Brennstoff für den Ofen, zum Beispiel ein Holzscheit.
    /// </summary>
    /// <remarks>
    /// Landet der Gegenstand in der Ofenöffnung, verbrennt ihn der Host (<see cref="Stove"/>).
    /// </remarks>
    public class FuelItem : MonoBehaviour
    {
        [Tooltip("So viele Sekunden brennt der Ofen länger, wenn er diesen Gegenstand bekommt.")]
        [SerializeField, Min(0f)] private float _burnSeconds = 45f;

        /// <summary>Brenndauer in Sekunden, die dieser Gegenstand dem Ofen gibt.</summary>
        public float BurnSeconds => _burnSeconds;
    }
}
