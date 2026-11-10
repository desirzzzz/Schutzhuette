using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Die Ware eines einzelnen Vorratspakets. Sitzt nur an Paketen ohne feste Ware: Bei ihnen legt
    /// fest, wer das Paket erzeugt, was drin ist (GDD Gegenstandsarten, Regel 2; ADR-0002, Entscheidung 5).
    /// </summary>
    /// <remarks>
    /// Die Ware gehört zum einzelnen Gegenstand, nicht zur Art, deshalb ist sie ein Netzwert, den nur der
    /// Host schreibt. Übers Netz geht nur die Kennung als kurzer Text, nie das Datenobjekt.
    /// Warum der Startwert in zwei Schritten: Der Host legt ihn vor Spawn() in ein normales Feld und
    /// überträgt ihn in OnNetworkSpawn in den Netzwert. OnNetworkSpawn läuft, bevor NGO die
    /// Spawn-Nachricht baut. So steht die Ware schon in dieser Nachricht, und der Client kennt sie sofort.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (crate.TryGetComponent(out SupplyPackState pack) &amp;&amp; pack.Ware != null)
    /// {
    ///     Debug.Log($"In der Kiste ist {pack.Ware.DisplayName}.");
    /// }
    /// </code>
    /// </example>
    public class SupplyPackState : NetworkBehaviour
    {
        private const string LogPrefix = "[Items] ";

        // Als Feld-Initialisierer, weil NGO die Variable vor OnNetworkSpawn braucht.
        // Standardrechte: Der Host schreibt, alle lesen.
        private readonly NetworkVariable<FixedString32Bytes> _wareId = new NetworkVariable<FixedString32Bytes>();

        // Startwert beim Host, gesetzt vor Spawn().
        private FixedString32Bytes _initialWareId;

        /// <summary>Kennung der Ware, zum Beispiel "ware_speck". Auf allen Rechnern gleich.</summary>
        public FixedString32Bytes WareId => _wareId.Value;

        /// <summary>Die Ware aus dem Katalog, oder null, wenn die Kennung dort unbekannt ist.</summary>
        public WareKind Ware { get; private set; }

        /// <summary>
        /// Nur beim Host und nur vor Spawn(): legt fest, welche Ware das Paket enthält.
        /// </summary>
        /// <param name="wareId">Kennung der Ware, zum Beispiel "ware_speck".</param>
        public void SetInitialWare(FixedString32Bytes wareId)
        {
            if (IsSpawned)
            {
                Debug.LogError(LogPrefix + $"„{name}“ ist schon gespawnt. Die Ware lässt sich nur vor Spawn() setzen.",
                    this);
                return;
            }

            _initialWareId = wareId;
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Vor dem Anmelden setzen: Der Setter ruft OnValueChanged beim Host sofort selbst auf.
                _wareId.Value = _initialWareId;
            }

            _wareId.OnValueChanged += HandleWareChanged;

            // Der erste Abgleich beim Spawn löst OnValueChanged nicht aus, deshalb einmal selbst übernehmen.
            Resolve(_wareId.Value);
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            _wareId.OnValueChanged -= HandleWareChanged;
        }

        private void HandleWareChanged(FixedString32Bytes previous, FixedString32Bytes current)
        {
            Resolve(current);
        }

        // Schlägt die Kennung im Katalog nach. Eine unbekannte Kennung meldet der Katalog als Fehler.
        private void Resolve(FixedString32Bytes wareId)
        {
            Ware = null;
            string side = IsServer ? "Host" : "Client";

            if (wareId.IsEmpty)
            {
                Debug.LogError(LogPrefix + $"„{name}“ hat keine Ware ({side}).", this);
                return;
            }

            CatalogService catalog = CatalogService.Current;
            if (catalog == null)
            {
                Debug.LogError(LogPrefix + $"„{name}“: Es läuft kein CatalogService, die Ware {wareId} bleibt unbekannt.",
                    this);
                return;
            }

            if (catalog.TryGetWare(wareId, out WareKind ware))
            {
                Ware = ware;

                // Je eine Zeile bei Host und Client: So lässt sich im Log vergleichen, ob beide dieselbe Ware lesen.
                Debug.Log(LogPrefix + $"{name} (NetworkObjectId {NetworkObjectId}): Ware {wareId} ({side}).");
            }
        }
    }
}
