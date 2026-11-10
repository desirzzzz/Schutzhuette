using Unity.Collections;
using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Hält den Katalog zur Laufzeit und schlägt Kennungen nach. Sitzt am Objekt des NetworkManagers,
    /// das beim Szenenwechsel bestehen bleibt (ADR-0002, Entscheidung 6).
    /// </summary>
    /// <remarks>
    /// Echter globaler Dienst, deshalb als <see cref="Current"/> erreichbar. Nur Laufzeit-Code nutzt
    /// ihn; Prüfregeln, Editor-Code und Tests bekommen den Katalog als Parameter.
    /// Die Nachschlagetabellen entstehen in Awake bei jedem Start neu.
    /// </remarks>
    /// <example>
    /// <code>
    /// CatalogService catalog = CatalogService.Current;
    /// if (catalog != null &amp;&amp; catalog.TryGetWare(wareId, out WareKind ware))
    /// {
    ///     Debug.Log(ware.DisplayName);
    /// }
    /// </code>
    /// </example>
    [DisallowMultipleComponent]
    public class CatalogService : MonoBehaviour
    {
        private const string LogPrefix = "[Katalog] ";

        [Tooltip("Der Katalog mit allen Arten, Waren und Bedürfnissen.")]
        [SerializeField] private ItemCatalog _catalog;

        private CatalogLookup _lookup;

        /// <summary>Der laufende Dienst, oder null, solange keiner gestartet ist.</summary>
        public static CatalogService Current { get; private set; }

        /// <summary>Der Katalog dieses Dienstes.</summary>
        public ItemCatalog Catalog => _catalog;

        // Domain Reload ist aus: Der Verweis vom letzten Play darf nicht stehen bleiben.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
        }

        private void Awake()
        {
            _lookup = new CatalogLookup(_catalog);

            if (Current != null && Current != this)
            {
                Debug.LogWarning(LogPrefix + $"Es gibt schon einen CatalogService an „{Current.name}“. "
                    + $"Der an „{name}“ wird nicht benutzt.", this);
                return;
            }

            if (_catalog == null)
            {
                Debug.LogError(LogPrefix + $"Am CatalogService von „{name}“ ist kein Katalog eingetragen.", this);
            }

            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        /// <summary>Sucht eine Art. Kein Treffer: false und ein Fehler im Log.</summary>
        public bool TryGetKind(in FixedString32Bytes id, out ItemKind kind)
        {
            return _lookup.TryGetKind(id, out kind);
        }

        /// <summary>Sucht eine Ware. Kein Treffer: false und ein Fehler im Log.</summary>
        public bool TryGetWare(in FixedString32Bytes id, out WareKind ware)
        {
            return _lookup.TryGetWare(id, out ware);
        }

        /// <summary>Sucht ein Bedürfnis. Kein Treffer: false und ein Fehler im Log.</summary>
        public bool TryGetNeed(in FixedString32Bytes id, out NeedKind need)
        {
            return _lookup.TryGetNeed(id, out need);
        }

#if UNITY_EDITOR
        // Befüllen nur im Editor (Editor-Skripte).
        internal void EditorSetCatalog(ItemCatalog catalog) => _catalog = catalog;
#endif
    }
}
