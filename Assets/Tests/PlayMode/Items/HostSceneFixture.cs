// Die Helfer laden die echte Szene im Editor und starten dort einen Host.
#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using Schutzhuette.Items;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Gemeinsamer Auf- und Abbau für PlayMode-Tests, die in der echten Szene einen Host brauchen.
    /// Ohne eigenen Zustand: Jeder Test nennt seinen Port selbst.
    /// </summary>
    /// <remarks>
    /// Der Host lauscht nur auf dem eigenen Rechner und auf einem Test-Port, damit ein laufendes Spiel
    /// den Test nicht stört. Jeder Fehler im Log lässt einen Test scheitern (Regel des Test-Frameworks).
    /// </remarks>
    internal static class HostSceneFixture
    {
        /// <summary>Die Szene des Spiels, wie sie in der exe steckt.</summary>
        public const string SceneName = "Prototype";

        /// <summary>Erster Test-Port. Jede Testklasse nimmt einen eigenen Bereich ab hier.</summary>
        public const ushort FirstPort = 7791;

        private const string LocalAddress = "127.0.0.1";
        private const int FramesToSettle = 30;
        private const string EmptySceneName = "TestLeer";

        /// <summary>Lädt die Szene, startet den Host und wartet, bis die Startaufstellung liegt.</summary>
        /// <param name="port">Port für diesen Test.</param>
        public static IEnumerator StartHost(ushort port)
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            NetworkManager networkManager = NetworkManager.Singleton;
            Assert.IsNotNull(networkManager, $"In der Szene {SceneName} fehlt der NetworkManager.");
            Assert.IsNotNull(CatalogService.Current, "Es läuft kein CatalogService.");

            var transport = networkManager.GetComponent<UnityTransport>();
            Assert.IsNotNull(transport, "Am NetworkManager fehlt UnityTransport.");
            transport.SetConnectionData(LocalAddress, port, LocalAddress);

            Assert.IsTrue(networkManager.StartHost(), $"Der Host konnte auf Port {port} nicht starten.");
            for (int i = 0; i < FramesToSettle; i++)
            {
                yield return null;
            }
        }

        /// <summary>Beendet den Host, zerstört den NetworkManager und verlässt die echte Szene.</summary>
        public static IEnumerator StopHost()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                if (networkManager.IsListening)
                {
                    networkManager.Shutdown();
                }

                yield return null;
                yield return null;

                // Der NetworkManager überlebt Szenenwechsel, deshalb ausdrücklich zerstören.
                Object.Destroy(networkManager.gameObject);
                yield return null;
            }

            // Die echte Szene wieder verlassen, damit sie keinen anderen Test beeinflusst.
            Scene loaded = SceneManager.GetSceneByName(SceneName);
            if (loaded.IsValid() && loaded.isLoaded)
            {
                Scene empty = SceneManager.CreateScene(EmptySceneName + Time.frameCount);
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(loaded);
            }
        }

        /// <summary>Sucht eine Art des laufenden Katalogs über ihre Kennung.</summary>
        public static ItemKind FindKind(string id)
        {
            foreach (ItemKind kind in CatalogService.Current.Catalog.Kinds)
            {
                if (kind != null && kind.Id == id)
                {
                    return kind;
                }
            }

            Assert.Fail($"Art „{id}“ fehlt im Katalog.");
            return null;
        }

        /// <summary>Sucht eine Ware des laufenden Katalogs über ihre Kennung.</summary>
        public static WareKind FindWare(string id)
        {
            foreach (WareKind ware in CatalogService.Current.Catalog.Wares)
            {
                if (ware != null && ware.Id == id)
                {
                    return ware;
                }
            }

            Assert.Fail($"Ware „{id}“ fehlt im Katalog.");
            return null;
        }
    }
}
#endif
