// Der Test lädt die echte Szene im Editor und startet dort einen Host.
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Schutzhuette.Items;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Startet in der echten Szene einen Host und prüft, was erscheint: Jeder Gegenstand hat eine Art,
    /// wiegt, was der Katalog sagt, und jedes Vorratspaket kennt seine Ware (GDD Gegenstandsarten,
    /// Kriterium 32 für die Host-Seite; ADR-0002, Verification 1 für die Warnung beim Host).
    /// </summary>
    /// <remarks>
    /// Jeder Fehler im Log lässt den Test scheitern (Regel des Test-Frameworks). Die Client-Seite von
    /// Kriterium 32 prüft der Freundestest über die Log-Zeilen von <see cref="SupplyPackState"/>.
    /// Der Host lauscht auf einem eigenen Port, damit ein laufendes Spiel den Test nicht stört.
    /// </remarks>
    public class HostSpawnTests
    {
        private const string SceneName = "Prototype";
        private const string LocalAddress = "127.0.0.1";
        private const ushort TestPort = 7791;
        private const int FramesToSettle = 30;
        private const float MassTolerance = 0.0001f;

        // Diese Warnung schreibt NGO, wenn ein Netzwert vor Spawn() geschrieben wird (ADR-0002, Verification 1).
        private const string NetworkVariableWarning = "NetworkVariable is written to";

        // Stand der Szene nach Story 007: Platzhalter, bis die Startaufstellung kommt.
        private const string CrateKindId = "vorratskiste";
        private const string ChestKindId = "truhe";
        private const string FirewoodKindId = "holzscheit";
        private const string TeaKindId = "tee";
        private const string CrateWareId = "ware_speck";
        private const string ChestWareId = "ware_mehl";
        private const int ExpectedTeaCount = 3;
        private const int ExpectedChestCount = 1;

        private readonly List<string> _warnings = new List<string>();
        private int _emptySceneCount;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.logMessageReceived -= CaptureWarnings;

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
                _emptySceneCount++;
                Scene empty = SceneManager.CreateScene("TestLeer" + _emptySceneCount);
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(loaded);
            }
        }

        [UnityTest]
        public IEnumerator HostStartInRealScene_SpawnsItemsWithKindsMassesAndWares()
        {
            // Arrange
            _warnings.Clear();
            Application.logMessageReceived += CaptureWarnings;
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            NetworkManager networkManager = NetworkManager.Singleton;
            Assert.IsNotNull(networkManager, $"In der Szene {SceneName} fehlt der NetworkManager.");
            Assert.IsNotNull(CatalogService.Current, "Es läuft kein CatalogService.");

            var transport = networkManager.GetComponent<UnityTransport>();
            Assert.IsNotNull(transport, "Am NetworkManager fehlt UnityTransport.");
            transport.SetConnectionData(LocalAddress, TestPort, LocalAddress);

            // Act
            Assert.IsTrue(networkManager.StartHost(), "Der Host konnte nicht starten.");
            for (int i = 0; i < FramesToSettle; i++)
            {
                yield return null;
            }

            // Assert: jeder Gegenstand einzeln
            var countByKind = new Dictionary<string, int>();
            foreach (NetworkObject spawned in networkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!spawned.TryGetComponent(out Item item))
                {
                    continue;
                }

                Assert.IsNotNull(item.Kind, $"„{spawned.name}“ hat keine Art.");
                string kindId = item.Kind.Id;
                countByKind.TryGetValue(kindId, out int count);
                countByKind[kindId] = count + 1;

                Assert.IsTrue(spawned.TryGetComponent(out Rigidbody body), $"„{spawned.name}“ hat keinen Rigidbody.");
                Assert.AreEqual(item.Kind.MassKg, body.mass, MassTolerance, $"Masse von „{spawned.name}“");

                if (kindId == CrateKindId || kindId == ChestKindId)
                {
                    Assert.IsTrue(spawned.TryGetComponent(out SupplyPackState pack),
                        $"„{spawned.name}“ hat keinen SupplyPackState.");
                    Assert.IsNotNull(pack.Ware, $"„{spawned.name}“ kennt seine Ware nicht.");
                    Assert.AreEqual(kindId == CrateKindId ? CrateWareId : ChestWareId, pack.Ware.Id,
                        $"Ware von „{spawned.name}“");
                }
            }

            // Assert: was insgesamt erschienen ist
            Assert.AreEqual(ExpectedTeaCount, CountOf(countByKind, TeaKindId), "Tassen");
            Assert.AreEqual(ExpectedChestCount, CountOf(countByKind, ChestKindId), "Truhen");
            Assert.Greater(CountOf(countByKind, CrateKindId), 0, "Vorratskisten");
            Assert.Greater(CountOf(countByKind, FirewoodKindId), 0, "Holzscheite");

            // Assert: Der Startwert der Ware kam ohne Warnung von NGO in den Netzwert.
            foreach (string warning in _warnings)
            {
                StringAssert.DoesNotContain(NetworkVariableWarning, warning);
            }
        }

        private static int CountOf(Dictionary<string, int> countByKind, string kindId)
        {
            return countByKind.TryGetValue(kindId, out int count) ? count : 0;
        }

        private void CaptureWarnings(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Warning)
            {
                _warnings.Add(condition);
            }
        }
    }
}
#endif
