using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Die einzigen Wege, Gegenstände erscheinen und verschwinden zu lassen. Nur beim Host
    /// (ADR-0002, Entscheidung 5; GDD Gegenstandsarten, "Edge Cases").
    /// </summary>
    /// <remarks>
    /// Eine Stelle statt vieler, damit jede Art vor dem Erscheinen geprüft wird und jeder Gegenstand
    /// auf dieselbe Weise entfernt wird. Zerbrechen, Verbrennen und die Startaufstellung nutzen sie.
    /// Der Kochzeitpunkt für Gerichte kommt dazu, sobald es die Saison-Uhr gibt (ADR-0003).
    /// </remarks>
    /// <example>
    /// <code>
    /// NetworkObject crate = ItemSpawning.Spawn(crateKind, position, rotation, speckWare);
    /// if (crate != null)
    /// {
    ///     // ... später:
    ///     ItemSpawning.Remove(crate);
    /// }
    /// </code>
    /// </example>
    public static class ItemSpawning
    {
        private const string LogPrefix = "[Items] ";

        /// <summary>
        /// Prüft, ob eine Art erscheinen darf. Reine Funktion, ohne Szene und ohne Netz testbar.
        /// </summary>
        /// <param name="kind">Die Art, die erscheinen soll.</param>
        /// <param name="ware">Ware für ein Vorratspaket ohne feste Ware, sonst null.</param>
        /// <param name="catalog">Der Katalog, in dem Art und Ware stehen müssen.</param>
        /// <param name="packWare">Die Ware, die das Paket enthalten wird, oder null, wenn die Art kein Paket ist.</param>
        /// <param name="error">Der Grund, wenn die Art nicht erscheinen darf.</param>
        /// <returns>true, wenn die Art erscheinen darf.</returns>
        public static bool CanSpawn(ItemKind kind, WareKind ware, ItemCatalog catalog, out WareKind packWare,
            out string error)
        {
            packWare = null;
            error = null;

            if (kind == null)
            {
                error = "Es wurde keine Art angegeben.";
                return false;
            }

            string label = $"Art „{kind.Id}“ (Asset „{kind.name}“)";

            if (catalog == null)
            {
                error = $"{label}: Es gibt keinen Katalog, gegen den sich die Art prüfen lässt.";
                return false;
            }

            if (!Contains(catalog.Kinds, kind))
            {
                error = $"{label}: Die Art steht nicht im Katalog.";
                return false;
            }

            if (!CatalogValidator.IsValidId(kind.Id))
            {
                error = $"{label}: Die Kennung ist ungültig.";
                return false;
            }

            if (!(kind.MassKg > 0f))
            {
                error = $"{label}: Die Masse muss größer als 0 kg sein.";
                return false;
            }

            if (kind.Prefab == null)
            {
                error = $"{label}: Es ist kein Prefab zugewiesen.";
                return false;
            }

            SupplyPackBlock pack = kind.SupplyPack;
            if (!pack.Enabled)
            {
                return true;
            }

            if (pack.Portions <= 0)
            {
                error = $"{label}: Ein Vorratspaket braucht mindestens 1 Portion.";
                return false;
            }

            // Eine feste Ware (der Kanister enthält immer Wasser) geht vor.
            WareKind chosen = pack.FixedWare != null ? pack.FixedWare : ware;
            if (chosen == null)
            {
                error = $"{label}: Ein Vorratspaket ohne feste Ware braucht eine Ware-Angabe. "
                    + "Wer ein Paket erzeugt, muss die Ware nennen.";
                return false;
            }

            if (!Contains(catalog.Wares, chosen) || !CatalogValidator.IsValidId(chosen.Id))
            {
                error = $"{label}: Die Ware „{chosen.Id}“ (Asset „{chosen.name}“) steht nicht im Katalog "
                    + "oder hat eine ungültige Kennung.";
                return false;
            }

            packWare = chosen;
            return true;
        }

        /// <summary>
        /// Nur beim Host: lässt einen Gegenstand der Art erscheinen. Eine ungültige Art erscheint nicht;
        /// der Grund steht als Fehler im Log.
        /// </summary>
        /// <param name="kind">Die Art.</param>
        /// <param name="position">Position in Weltkoordinaten.</param>
        /// <param name="rotation">Drehung in Weltkoordinaten.</param>
        /// <param name="ware">Ware für ein Vorratspaket ohne feste Ware, sonst null.</param>
        /// <returns>Das gespawnte Netzobjekt, oder null, wenn nichts erschienen ist.</returns>
        public static NetworkObject Spawn(ItemKind kind, Vector3 position, Quaternion rotation, WareKind ware = null)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsServer)
            {
                Debug.LogError(LogPrefix + "Gegenstände erscheinen nur beim Host (ItemSpawning.Spawn).");
                return null;
            }

            CatalogService service = CatalogService.Current;
            ItemCatalog catalog = service != null ? service.Catalog : null;
            if (!CanSpawn(kind, ware, catalog, out WareKind packWare, out string error))
            {
                Debug.LogError(LogPrefix + "Erscheint nicht. " + error);
                return null;
            }

            NetworkObject item = Object.Instantiate(kind.Prefab, position, rotation);

            // Nur Pakete ohne feste Ware tragen ihre Ware am Gegenstand; bei fester Ware steht sie in der Art.
            if (packWare != null && kind.SupplyPack.FixedWare == null)
            {
                if (!item.TryGetComponent(out SupplyPackState packState))
                {
                    Debug.LogError(LogPrefix + $"Erscheint nicht. Art „{kind.Id}“: Dem Prefab „{kind.Prefab.name}“ "
                        + "fehlt die Komponente SupplyPackState für die Ware.");
                    Object.Destroy(item.gameObject);
                    return null;
                }

                // Startwert vor Spawn(), damit er schon in der Spawn-Nachricht steht.
                packState.SetInitialWare(new FixedString32Bytes(packWare.Id));
            }

            item.Spawn(destroyWithScene: true);
            return item;
        }

        /// <summary>
        /// Nur beim Host: entfernt einen Gegenstand für alle Spieler.
        /// </summary>
        /// <remarks>
        /// Erst die Kollisionsprüfung auf "Discrete", dann Despawn: Beim Despawn macht NGO den Körper
        /// kinematisch, und ein kinematischer Körper mit durchgehender Kollisionsprüfung löst eine
        /// Warnung aus (ADR-0001). Läuft das Netz nicht mehr (Host beendet), wird das Objekt nur zerstört.
        /// </remarks>
        /// <param name="item">Der Gegenstand. null wird ignoriert.</param>
        public static void Remove(NetworkObject item)
        {
            if (item == null)
            {
                return;
            }

            if (item.TryGetComponent(out Rigidbody body))
            {
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            if (!item.IsSpawned)
            {
                Object.Destroy(item.gameObject);
                return;
            }

            NetworkManager networkManager = item.NetworkManager;
            if (networkManager == null || !networkManager.IsServer)
            {
                Debug.LogError(LogPrefix + $"„{item.name}“ lässt sich nur beim Host entfernen (ItemSpawning.Remove).",
                    item);
                return;
            }

            item.Despawn(true);
        }

        private static bool Contains<T>(System.Collections.Generic.IReadOnlyList<T> list, T entry) where T : Object
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == entry)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
