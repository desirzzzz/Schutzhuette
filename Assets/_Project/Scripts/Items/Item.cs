using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Das Etikett an jedem Gegenstands-Prefab: Es sagt, welche Art (<see cref="ItemKind"/>) der
    /// Gegenstand ist (ADR-0002, Entscheidung 3: "Das Prefab verrät die Art").
    /// </summary>
    /// <remarks>
    /// Kein Netzwert nötig: NGO erzeugt beim Client dasselbe Prefab, also kennt jeder Rechner die Art.
    /// In Awake übernimmt der Rigidbody die Masse aus dem Katalog, auf jedem Rechner. Der Katalog ist
    /// die einzige Quelle für das Gewicht; die Masse im Prefab wird überschrieben.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (hit.collider.TryGetComponent(out Item item) &amp;&amp; item.Kind != null &amp;&amp; item.Kind.Fuel.Enabled)
    /// {
    ///     float seconds = item.Kind.Fuel.BurnSeconds;
    /// }
    /// </code>
    /// </example>
    [DisallowMultipleComponent]
    public class Item : MonoBehaviour
    {
        private const string LogPrefix = "[Items] ";

        [Tooltip("Die Art dieses Gegenstands. Ohne Art lässt er sich nicht aufheben.")]
        [SerializeField] private ItemKind _kind;

        /// <summary>Die Art dieses Gegenstands, oder null, wenn keine eingetragen ist.</summary>
        public ItemKind Kind => _kind;

        private void Awake()
        {
            if (_kind == null)
            {
                Debug.LogError(LogPrefix + $"„{name}“ hat keine Art: An der Komponente Item ist kein "
                    + "Steckbrief eingetragen. Der Gegenstand lässt sich nicht aufheben.", this);
                return;
            }

            // Eine ungültige Masse (0 oder kleiner) meldet die Katalogprüfung; hier bleibt dann die
            // Masse aus dem Prefab stehen, weil der Rigidbody keine Masse von 0 verträgt.
            if (_kind.MassKg > 0f && TryGetComponent(out Rigidbody body))
            {
                body.mass = _kind.MassKg;
            }
        }

        /// <summary>Hat das Objekt ein Etikett mit eingetragener Art? Nur dann ist es ein Gegenstand.</summary>
        /// <param name="target">Eine Komponente am Objekt, zum Beispiel das Tragbare.</param>
        public static bool HasKind(Component target)
        {
            return target != null && target.TryGetComponent(out Item item) && item._kind != null;
        }

        /// <summary>
        /// Für Komponenten, die einen Gegenstand voraussetzen (zum Beispiel das Tragen): meldet einen
        /// Fehler mit dem Objektnamen, wenn das Etikett ganz fehlt. Ein Etikett ohne Art meldet sich selbst.
        /// </summary>
        /// <param name="owner">Die Komponente, die das Etikett braucht.</param>
        public static void ReportIfMissing(Component owner)
        {
            if (owner != null && !owner.TryGetComponent(out Item _))
            {
                Debug.LogError(LogPrefix + $"„{owner.name}“ hat keine Art: Die Komponente Item fehlt. "
                    + "Der Gegenstand lässt sich nicht aufheben.", owner);
            }
        }

#if UNITY_EDITOR
        // Befüllen nur im Editor (Tests und Editor-Skripte).
        internal void EditorSetKind(ItemKind kind) => _kind = kind;
#endif
    }
}
