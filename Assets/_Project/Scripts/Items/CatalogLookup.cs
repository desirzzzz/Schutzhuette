using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Nachschlagetabellen des Katalogs: Kennung → Art, Ware oder Bedürfnis. Normale C#-Klasse ohne
    /// Szene, deshalb in EditMode-Tests prüfbar (ADR-0002, Entscheidung 6; GDD Kriterium 27).
    /// </summary>
    /// <remarks>
    /// Die Tabellen entstehen bei jedem Start neu aus dem Katalog und stehen nie im Asset.
    /// Kennungen kommen zur Laufzeit nur vom Host. Eine unbekannte Kennung heißt deshalb, dass Host und
    /// Client verschiedene Katalogstände haben: Das ist ein Fehler im Log, nie eine Exception.
    /// </remarks>
    /// <example>
    /// <code>
    /// var lookup = new CatalogLookup(catalog);
    /// if (lookup.TryGetWare(wareId, out WareKind ware))
    /// {
    ///     label.text = ware.DisplayName;
    /// }
    /// </code>
    /// </example>
    public sealed class CatalogLookup
    {
        private const string LogPrefix = "[Katalog] ";

        private readonly Dictionary<FixedString32Bytes, ItemKind> _kinds = new Dictionary<FixedString32Bytes, ItemKind>();
        private readonly Dictionary<FixedString32Bytes, WareKind> _wares = new Dictionary<FixedString32Bytes, WareKind>();
        private readonly Dictionary<FixedString32Bytes, NeedKind> _needs = new Dictionary<FixedString32Bytes, NeedKind>();

        /// <summary>Baut die Tabellen aus dem Katalog. Einträge mit ungültiger Kennung fehlen darin.</summary>
        /// <param name="catalog">Der Katalog. null ergibt leere Tabellen.</param>
        public CatalogLookup(ItemCatalog catalog)
        {
            if (catalog == null)
            {
                return;
            }

            foreach (ItemKind kind in catalog.Kinds)
            {
                if (kind != null)
                {
                    Add(_kinds, kind.Id, kind);
                }
            }

            foreach (WareKind ware in catalog.Wares)
            {
                if (ware != null)
                {
                    Add(_wares, ware.Id, ware);
                }
            }

            foreach (NeedKind need in catalog.Needs)
            {
                if (need != null)
                {
                    Add(_needs, need.Id, need);
                }
            }
        }

        /// <summary>Sucht eine Art. Kein Treffer: false und ein Fehler im Log.</summary>
        public bool TryGetKind(in FixedString32Bytes id, out ItemKind kind)
        {
            return TryGet(_kinds, id, "Art", out kind);
        }

        /// <summary>Sucht eine Ware. Kein Treffer: false und ein Fehler im Log.</summary>
        public bool TryGetWare(in FixedString32Bytes id, out WareKind ware)
        {
            return TryGet(_wares, id, "Ware", out ware);
        }

        /// <summary>Sucht ein Bedürfnis. Kein Treffer: false und ein Fehler im Log.</summary>
        public bool TryGetNeed(in FixedString32Bytes id, out NeedKind need)
        {
            return TryGet(_needs, id, "Bedürfnis", out need);
        }

        // Ungültige oder doppelte Kennungen meldet die Katalogprüfung; hier werden sie nur übersprungen.
        // Bei doppelten gilt der erste Eintrag.
        private static void Add<T>(Dictionary<FixedString32Bytes, T> table, string id, T entry)
        {
            if (!CatalogValidator.IsValidId(id))
            {
                return;
            }

            var key = new FixedString32Bytes(id);
            if (!table.ContainsKey(key))
            {
                table.Add(key, entry);
            }
        }

        private static bool TryGet<T>(Dictionary<FixedString32Bytes, T> table, in FixedString32Bytes id,
            string typeLabel, out T entry) where T : class
        {
            if (table.TryGetValue(id, out entry))
            {
                return true;
            }

            entry = null;
            Debug.LogError(LogPrefix + $"Unbekannte Kennung „{id}“ ({typeLabel}). Host und Client haben "
                + "vermutlich verschiedene Katalogstände.");
            return false;
        }
    }
}
