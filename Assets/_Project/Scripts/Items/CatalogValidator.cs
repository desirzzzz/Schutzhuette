using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Prüft den Katalog. Reine Funktion: Sie liest nur, was man ihr übergibt, und läuft deshalb
    /// ohne Szene in EditMode-Tests, im Build-Skript und vor dem Host-Start
    /// (GDD Gegenstandsarten, "Edge Cases" und Kriterien 2 und 5; ADR-0002, Entscheidung 7).
    /// </summary>
    /// <remarks>
    /// Die Netzwerk-Prefab-Liste ist ein Parameter, damit die Regel "jedes Prefab steht in der Liste"
    /// ohne Szene prüfbar bleibt. Nutzt nie den Laufzeit-Dienst des Katalogs.
    /// </remarks>
    /// <example>
    /// <code>
    /// CatalogReport report = CatalogValidator.Validate(catalog, networkPrefabs);
    /// if (report.StartBlocked)
    /// {
    ///     Debug.LogError(report.ToString());
    ///     return; // Host nicht starten
    /// }
    /// </code>
    /// </example>
    public static class CatalogValidator
    {
        /// <summary>Höchste Länge einer Kennung. So viele Zeichen fasst FixedString32Bytes (ADR-0002).</summary>
        public const int MaxIdLength = 29;

        /// <summary>Damit beginnt die Kennung jeder Ware.</summary>
        public const string WarePrefix = "ware_";

        /// <summary>Damit beginnt die Kennung jedes Bedürfnisses.</summary>
        public const string NeedPrefix = "beduerfnis_";

        // Ab dieser Abweichung in kg gilt eine Prefab-Masse als anders als im Katalog.
        private const float MassTolerance = 0.0001f;

        private const string KindLabel = "Art";
        private const string WareLabel = "Ware";
        private const string NeedLabel = "Bedürfnis";

        private enum EntryType
        {
            Kind,
            Ware,
            Need,
        }

        /// <summary>
        /// Prüft alle Arten, Waren und Bedürfnisse des Katalogs.
        /// </summary>
        /// <param name="catalog">Der Katalog. Fehlt er, ist das ein Fehler.</param>
        /// <param name="networkPrefabs">Die Netzwerk-Prefab-Liste des NetworkManagers. Fehlt sie, ist das ein Fehler.</param>
        /// <returns>Fehler und Hinweise. Bei mindestens einem Fehler ist <see cref="CatalogReport.StartBlocked"/> wahr.</returns>
        public static CatalogReport Validate(ItemCatalog catalog, NetworkPrefabsList networkPrefabs)
        {
            var report = new CatalogReport();

            if (catalog == null)
            {
                report.AddError("Es ist kein Katalog zugewiesen.");
                return report;
            }

            if (networkPrefabs == null)
            {
                report.AddError("Es wurde keine Netzwerk-Prefab-Liste übergeben. "
                    + "Ob jedes Prefab darin steht, lässt sich so nicht prüfen.");
            }

            // Wer welche Kennung benutzt, über Arten, Waren und Bedürfnisse hinweg.
            var idOwners = new OwnerMap<string>(StringComparer.Ordinal);

            var knownWares = new HashSet<WareKind>();
            foreach (WareKind ware in catalog.Wares)
            {
                if (ware != null)
                {
                    knownWares.Add(ware);
                }
            }

            ValidateKinds(catalog.Kinds, catalog.Settings, networkPrefabs, knownWares, idOwners, report);
            ValidateWares(catalog.Wares, idOwners, report);
            ValidateNeeds(catalog.Needs, idOwners, report);
            ReportDuplicateIds(idOwners, report);

            return report;
        }

        private static void ValidateKinds(IReadOnlyList<ItemKind> kinds, CatalogSettings settings,
            NetworkPrefabsList networkPrefabs, HashSet<WareKind> knownWares, OwnerMap<string> idOwners,
            CatalogReport report)
        {
            // Welche Arten welches Prefab benutzen: Jede Art braucht ihr eigenes.
            var prefabOwners = new OwnerMap<NetworkObject>(null);

            for (int i = 0; i < kinds.Count; i++)
            {
                ItemKind kind = kinds[i];
                if (kind == null)
                {
                    report.AddError(EmptyEntry("den Arten", i));
                    continue;
                }

                string label = Describe(KindLabel, kind.Id, kind.name);

                CheckId(kind.Id, EntryType.Kind, label, Owner(KindLabel, kind.name), idOwners, report);
                CheckDisplayName(kind.DisplayName, label, report);

                // "Nicht größer als 0" statt "kleiner gleich 0": So fällt auch NaN auf.
                if (!(kind.MassKg > 0f))
                {
                    report.AddError($"{label}: Die Masse muss größer als 0 kg sein (ist {Format(kind.MassKg)}).");
                }

                CheckPrefab(kind, label, networkPrefabs, prefabOwners, report);
                CheckSupplyPack(kind.SupplyPack, label, knownWares, report);
                CheckFragile(kind.Fragile, label, settings, report);
            }

            foreach (NetworkObject prefab in prefabOwners.Keys)
            {
                IReadOnlyList<string> owners = prefabOwners.OwnersOf(prefab);
                if (owners.Count > 1)
                {
                    report.AddError($"Das Prefab „{prefab.name}“ gehört zu mehreren Arten: "
                        + $"{string.Join(", ", owners)}. Jede Art braucht ihr eigenes Prefab.");
                }
            }
        }

        private static void ValidateWares(IReadOnlyList<WareKind> wares, OwnerMap<string> idOwners,
            CatalogReport report)
        {
            for (int i = 0; i < wares.Count; i++)
            {
                WareKind ware = wares[i];
                if (ware == null)
                {
                    report.AddError(EmptyEntry("den Waren", i));
                    continue;
                }

                string label = Describe(WareLabel, ware.Id, ware.name);
                CheckId(ware.Id, EntryType.Ware, label, Owner(WareLabel, ware.name), idOwners, report);
                CheckDisplayName(ware.DisplayName, label, report);
            }
        }

        private static void ValidateNeeds(IReadOnlyList<NeedKind> needs, OwnerMap<string> idOwners,
            CatalogReport report)
        {
            for (int i = 0; i < needs.Count; i++)
            {
                NeedKind need = needs[i];
                if (need == null)
                {
                    report.AddError(EmptyEntry("den Bedürfnissen", i));
                    continue;
                }

                string label = Describe(NeedLabel, need.Id, need.name);
                CheckId(need.Id, EntryType.Need, label, Owner(NeedLabel, need.name), idOwners, report);
                CheckDisplayName(need.DisplayName, label, report);
            }
        }

        private static void CheckId(string id, EntryType type, string label, string owner,
            OwnerMap<string> idOwners, CatalogReport report)
        {
            if (string.IsNullOrEmpty(id))
            {
                report.AddError($"{label}: Die Kennung ist leer.");
                return;
            }

            idOwners.Add(id, owner);

            if (!IsValidId(id))
            {
                report.AddError($"{label}: Die Kennung passt nicht zum Muster. Erlaubt sind nur a–z, 0–9 und _, "
                    + $"höchstens {MaxIdLength} Zeichen (sie hat {id.Length}).");
                return;
            }

            bool hasWarePrefix = id.StartsWith(WarePrefix, StringComparison.Ordinal);
            bool hasNeedPrefix = id.StartsWith(NeedPrefix, StringComparison.Ordinal);

            switch (type)
            {
                case EntryType.Kind when hasWarePrefix || hasNeedPrefix:
                    report.AddError($"{label}: Die Kennung einer Art darf nicht mit „{WarePrefix}“ oder "
                        + $"„{NeedPrefix}“ beginnen.");
                    break;
                case EntryType.Ware when !hasWarePrefix:
                    report.AddError($"{label}: Die Kennung einer Ware muss mit „{WarePrefix}“ beginnen.");
                    break;
                case EntryType.Need when !hasNeedPrefix:
                    report.AddError($"{label}: Die Kennung eines Bedürfnisses muss mit „{NeedPrefix}“ beginnen.");
                    break;
            }
        }

        /// <summary>
        /// Ob eine Kennung dem Muster ^[a-z0-9_]{1,29}$ folgt (ADR-0002, Entscheidung 4). Nur solche
        /// Kennungen passen in den Netz-Datentyp FixedString32Bytes. Präfixe prüft <see cref="Validate"/>.
        /// </summary>
        /// <param name="id">Die Kennung. null oder leer ist ungültig.</param>
        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaxIdLength)
            {
                return false;
            }

            foreach (char c in id)
            {
                bool allowed = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!allowed)
                {
                    return false;
                }
            }

            return true;
        }

        private static void CheckDisplayName(string displayName, string label, CatalogReport report)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                report.AddError($"{label}: Der Anzeigename ist leer.");
            }
        }

        private static void CheckPrefab(ItemKind kind, string label, NetworkPrefabsList networkPrefabs,
            OwnerMap<NetworkObject> prefabOwners, CatalogReport report)
        {
            NetworkObject prefab = kind.Prefab;
            if (prefab == null)
            {
                report.AddError($"{label}: Es ist kein Prefab zugewiesen.");
                return;
            }

            prefabOwners.Add(prefab, label);

            if (networkPrefabs != null && !networkPrefabs.Contains(prefab.gameObject))
            {
                report.AddError($"{label}: Das Prefab „{prefab.name}“ steht nicht in der Netzwerk-Prefab-Liste. "
                    + "Ohne Eintrag kann es im Netz nicht erscheinen.");
            }

            // Das Prefab verrät die Art (ADR-0002, Entscheidung 3): Sein Etikett muss auf genau diese Art
            // zeigen, sonst bekäme der Gegenstand im Spiel einen falschen oder gar keinen Steckbrief.
            if (!prefab.TryGetComponent(out Item item))
            {
                report.AddError($"{label}: Dem Prefab „{prefab.name}“ fehlt die Komponente Item.");
            }
            else if (item.Kind != kind)
            {
                string other = item.Kind != null ? Describe(KindLabel, item.Kind.Id, item.Kind.name) : "keine Art";
                report.AddError($"{label}: Die Komponente Item am Prefab „{prefab.name}“ zeigt auf {other} "
                    + "statt auf diese Art.");
            }

            // Der Katalog gilt, die Masse wird beim Erscheinen überschrieben (GDD Edge Case, Kriterium 7).
            if (prefab.TryGetComponent(out Rigidbody body) && Math.Abs(body.mass - kind.MassKg) > MassTolerance)
            {
                report.AddHint($"{label}: Die Rigidbody-Masse im Prefab „{prefab.name}“ ({Format(body.mass)} kg) "
                    + $"weicht vom Katalog ab ({Format(kind.MassKg)} kg). Der Katalog gilt, die Masse wird beim "
                    + "Erscheinen überschrieben.");
            }
        }

        private static void CheckSupplyPack(SupplyPackBlock pack, string label, HashSet<WareKind> knownWares,
            CatalogReport report)
        {
            if (!pack.Enabled)
            {
                return;
            }

            if (pack.Portions <= 0)
            {
                report.AddError($"{label}: Ein Vorratspaket braucht mindestens 1 Portion (hat {pack.Portions}).");
            }

            WareKind fixedWare = pack.FixedWare;
            if (fixedWare != null && !knownWares.Contains(fixedWare))
            {
                report.AddError($"{label}: Die feste Ware {Describe(WareLabel, fixedWare.Id, fixedWare.name)} "
                    + "steht nicht im Katalog.");
            }
        }

        // Prüffenster für die Bruchgeschwindigkeit (GDD Formel 2, Kriterium 13). Nur Hinweise, nie Fehler.
        private static void CheckFragile(FragileBlock fragile, string label, CatalogSettings settings,
            CatalogReport report)
        {
            if (!fragile.Enabled)
            {
                return;
            }

            float holdHeight = settings.CheckHoldHeightMeters;
            float throwSpeed = settings.CheckThrowSpeed;
            BreakSpeedCheck check = BreakRule.CheckBreakSpeed(fragile.BreakSpeed, holdHeight, throwSpeed,
                BreakRule.ProjectGravity);

            switch (check)
            {
                case BreakSpeedCheck.BreaksWhenPutDown:
                    report.AddHint($"{label}: Mit der Bruchgeschwindigkeit {Format(fragile.BreakSpeed)} m/s gilt: "
                        + "Ablegen zerbricht. Sie sollte mindestens "
                        + $"{Format(BreakRule.MinBreakSpeed(holdHeight, BreakRule.ProjectGravity))} m/s sein "
                        + $"(Haltehöhe {Format(holdHeight)} m).");
                    break;
                case BreakSpeedCheck.StraightThrowsDoNotBreak:
                    report.AddHint($"{label}: Mit der Bruchgeschwindigkeit {Format(fragile.BreakSpeed)} m/s gilt: "
                        + "gerade Würfe zerbrechen nicht. Sie sollte höchstens "
                        + $"{Format(BreakRule.MaxBreakSpeed(throwSpeed))} m/s sein "
                        + $"(Wurftempo {Format(throwSpeed)} m/s).");
                    break;
            }
        }

        private static void ReportDuplicateIds(OwnerMap<string> idOwners, CatalogReport report)
        {
            foreach (string id in idOwners.Keys)
            {
                IReadOnlyList<string> owners = idOwners.OwnersOf(id);
                if (owners.Count > 1)
                {
                    report.AddError($"Die Kennung „{id}“ ist mehrfach vergeben: {string.Join(", ", owners)}. "
                        + "Jede Kennung darf es nur einmal geben, auch zwischen Arten, Waren und Bedürfnissen.");
                }
            }
        }

        // "Art „holzscheit“ (Asset „Holzscheit“)", bei leerer Kennung "Art ohne Kennung (Asset „…“)".
        private static string Describe(string typeLabel, string id, string assetName)
        {
            string asset = AssetText(assetName);
            return string.IsNullOrEmpty(id)
                ? $"{typeLabel} ohne Kennung ({asset})"
                : $"{typeLabel} „{id}“ ({asset})";
        }

        private static string Owner(string typeLabel, string assetName)
        {
            return $"{typeLabel} ({AssetText(assetName)})";
        }

        private static string AssetText(string assetName)
        {
            return string.IsNullOrEmpty(assetName) ? "Asset ohne Namen" : $"Asset „{assetName}“";
        }

        private static string EmptyEntry(string listText, int index)
        {
            return $"Der Katalog enthält bei {listText} einen leeren Eintrag (Position {index + 1}).";
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Merkt sich zu einem Schlüssel alle Besitzer, in der Reihenfolge des Eintragens.
        /// So kommen die Meldungen bei jedem Lauf in derselben Reihenfolge.
        /// </summary>
        private sealed class OwnerMap<TKey>
        {
            private readonly Dictionary<TKey, List<string>> _owners;
            private readonly List<TKey> _keys = new List<TKey>();

            public OwnerMap(IEqualityComparer<TKey> comparer)
            {
                _owners = new Dictionary<TKey, List<string>>(comparer);
            }

            public IReadOnlyList<TKey> Keys => _keys;

            public void Add(TKey key, string owner)
            {
                if (!_owners.TryGetValue(key, out List<string> owners))
                {
                    owners = new List<string>();
                    _owners.Add(key, owners);
                    _keys.Add(key);
                }

                owners.Add(owner);
            }

            public IReadOnlyList<string> OwnersOf(TKey key) => _owners[key];
        }
    }
}
