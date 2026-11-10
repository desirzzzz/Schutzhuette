using System;
using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Meldedienst für Aufpralle. Nur der Host löst aus, und nur Host-Code hört zu
    /// (ADR-0001, Entscheidung 4).
    /// </summary>
    /// <remarks>
    /// Ein Meldedienst statt vieler einzelner Auswertungen, damit Zerbrechen, Geräusche und Gäste mit
    /// demselben Messwert rechnen. Zuhörer melden sich so an und ab:
    /// NetworkBehaviours in OnNetworkSpawn (nur bei IsServer) und OnNetworkDespawn,
    /// reine MonoBehaviours in OnEnable und OnDisable.
    /// </remarks>
    /// <example>
    /// <code>
    /// public override void OnNetworkSpawn()
    /// {
    ///     if (IsServer)
    ///     {
    ///         ImpactEvents.Reported += HandleImpact;
    ///     }
    /// }
    ///
    /// public override void OnNetworkDespawn()
    /// {
    ///     ImpactEvents.Reported -= HandleImpact;
    /// }
    /// </code>
    /// </example>
    public static class ImpactEvents
    {
        /// <summary>Ein Gegenstand ist beim Host aufgeprallt.</summary>
        public static event Action<ImpactReport> Reported;

        // Domain Reload ist aus: Zuhörer vom letzten Play dürfen nicht hängen bleiben.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Reported = null;
        }

        /// <summary>
        /// Meldet einen Aufprall an alle Zuhörer. Nur der Aufprall-Messer des Hosts ruft das auf.
        /// </summary>
        /// <remarks>
        /// Jeder Zuhörer wird einzeln abgesichert: Ein Fehler bei einem hält weder die anderen noch das
        /// Entfernen eines zerbrochenen Gegenstands auf. Die Liste der Zuhörer kostet je Aufprall eine
        /// kleine Allokation; bei wenigen Aufprallen pro Sekunde ist das vertretbar (ADR-0001).
        /// </remarks>
        /// <param name="report">Die Messung.</param>
        public static void Raise(in ImpactReport report)
        {
            Action<ImpactReport> listeners = Reported;
            if (listeners == null)
            {
                return;
            }

            foreach (Delegate listener in listeners.GetInvocationList())
            {
                try
                {
                    ((Action<ImpactReport>)listener)(report);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
    }
}
