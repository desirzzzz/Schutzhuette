// Die Tests laden die echte Szene im Editor und starten dort einen Host.
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Schutzhuette.Carrying;
using Schutzhuette.Items;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Prüft das Zerbrechen und die Kollisionsprüfung im Flug mit einem echten Host: GDD Gegenstandsarten,
    /// Kriterium 15 (liegende Tasse, getroffen von einer Kiste) und die Anforderung, dass Würfe bis
    /// 15 m/s keine dünne Wand durchfliegen (ADR-0001, Verification 1).
    /// </summary>
    /// <remarks>
    /// Die Gegenstände erscheinen über die echte Ausgabestelle und werden über das echte Loslassen
    /// (<see cref="Carryable.Detach"/>) geworfen. Gespielt wird auf einer eigenen Plattform hoch über
    /// der Hütte, damit die Karte den Test nicht beeinflusst. Jeder Physikschritt dauert 0,02 s.
    /// Jeder Fehler im Log lässt einen Test scheitern; Warnungen zur Kollisionsprüfung werden mitgezählt.
    /// </remarks>
    public class BreakOnImpactTests
    {
        private const ushort FirstPort = HostSceneFixture.FirstPort + 1;

        private const string TeaKindId = "tee";
        private const string CrateKindId = "vorratskiste";
        private const string CrateWareId = "ware_speck";

        // Maße der Graubox-Gegenstände in m.
        private const float CupHalfSize = 0.06f;
        private const float CrateHalfSize = 0.3f;

        // Test-Plattform: weit über der Karte, damit nichts anderes mitspielt.
        private const float PlatformTopY = 300f;
        private const float PlatformSize = 8f;
        private const float PlatformThickness = 0.2f;
        private const float SpawnGap = 0.01f;

        // Kriterium 15: Kiste mit 8 m/s waagrecht gegen die Tasse, höchstens 10 Physikschritte.
        private const float HitSpeed = 8f;
        private const float HitGap = 0.05f;
        private const int MaxStepsUntilRemoved = 10;

        // Kriterium 15: Kiste fällt aus Haltehöhe (ergibt 5,01 m/s), die Tasse steht nach 50 Schritten noch.
        private const float HoldHeight = 1.28f;
        private const int StepsAfterDrop = 50;
        private const float MinFallDistance = 1f;

        // Würfe bis 15 m/s gegen eine 0,2 m dicke Wand.
        private const float FastThrowSpeed = 15f;
        private const float ThrowHeight = 1f;
        private const float WallDistance = 2f;
        private const float WallThickness = 0.2f;
        private const float WallHeight = 3f;
        private const int FlightSteps = 40;

        // Schneller als jeder Wurf: 20 m/s sind 0,4 m pro Physikschritt. Mit dem Start bei 0,3 m liegen
        // die Schritte bei 0,7, 1,1, 1,5, 1,9 und 2,3 m, also genau vor und hinter der Wand.
        private const float SkipSpeed = 20f;
        private const float SkipStartZ = 0.3f;

        private const int MaxStepsToRest = 200;

        // Unity warnt so, wenn ein kinematischer Körper die durchgehende Kollisionsprüfung bekommt.
        private const string DetectionWarning = "ollision detection";

        private readonly List<string> _warnings = new List<string>();
        private readonly WaitForFixedUpdate _nextPhysicsStep = new WaitForFixedUpdate();
        private int _testCount;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _warnings.Clear();
            Application.logMessageReceived += CaptureWarnings;

            // Jeder Test bekommt seinen eigenen Port.
            yield return HostSceneFixture.StartHost((ushort)(FirstPort + _testCount));
            _testCount++;

            BuildPlatform();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.logMessageReceived -= CaptureWarnings;

            // Mit der Szene verschwinden auch Plattform, Wand und alle Test-Gegenstände.
            yield return HostSceneFixture.StopHost();
        }

        [UnityTest]
        public IEnumerator RestingCupHitByCrateAtEightMetersPerSecond_IsRemovedWithinTenPhysicsSteps()
        {
            // Arrange: eine Tasse, die ruhig auf dem Boden liegt
            NetworkObject cup = SpawnTea(OnPlatform(0f, CupHalfSize + SpawnGap, 0f));
            yield return WaitUntilResting(cup);
            Assert.AreEqual(ItemPhysicsState.Resting, cup.GetComponent<ImpactReporter>().State);

            // Act: eine Kiste direkt daneben, waagrecht mit 8 m/s losgelassen
            Vector3 cratePosition = OnPlatform(-(CupHalfSize + CrateHalfSize + HitGap), CrateHalfSize + SpawnGap, 0f);
            NetworkObject crate = SpawnCrate(cratePosition);
            crate.GetComponent<Carryable>().Detach(cratePosition, Quaternion.identity, Vector3.right * HitSpeed);

            int steps = 0;
            while (steps < MaxStepsUntilRemoved && IsAlive(cup))
            {
                yield return _nextPhysicsStep;
                steps++;
            }

            // Assert
            Assert.IsFalse(IsAlive(cup), $"Die Tasse steht nach {steps} Physikschritten noch.");
            Assert.IsTrue(IsAlive(crate), "Die Kiste darf nicht verschwinden.");
            AssertNoDetectionWarnings();
        }

        [UnityTest]
        public IEnumerator RestingCupHitByCrateFallingFromHoldHeight_StillExistsAfterFiftyPhysicsSteps()
        {
            // Arrange
            NetworkObject cup = SpawnTea(OnPlatform(0f, CupHalfSize + SpawnGap, 0f));
            yield return WaitUntilResting(cup);

            // Act: Die Unterkante der Kiste hängt eine Haltehöhe über der Tasse, dann fällt sie.
            Vector3 above = OnPlatform(0f, 2f * CupHalfSize + HoldHeight + CrateHalfSize, 0f);
            NetworkObject crate = SpawnCrate(above);
            crate.GetComponent<Carryable>().Detach(above, Quaternion.identity, Vector3.zero);

            for (int i = 0; i < StepsAfterDrop; i++)
            {
                yield return _nextPhysicsStep;
            }

            // Assert
            Assert.IsTrue(IsAlive(cup), "Die Tasse ist zerbrochen, obwohl die Kiste nur aus Haltehöhe fiel.");
            Assert.Less(crate.transform.position.y, above.y - MinFallDistance, "Die Kiste ist nicht gefallen.");
            AssertNoDetectionWarnings();
        }

        [UnityTest]
        public IEnumerator CupThrownAtFifteenMetersPerSecond_BreaksAtThinWallAndNeverPassesIt()
        {
            // Arrange
            BuildWall();
            Vector3 start = OnPlatform(0f, ThrowHeight, 0f);
            NetworkObject cup = SpawnTea(start);

            // Act
            cup.GetComponent<Carryable>().Detach(start, Quaternion.identity, Vector3.forward * FastThrowSpeed);

            float farthest = start.z;
            for (int i = 0; i < FlightSteps && IsAlive(cup); i++)
            {
                yield return _nextPhysicsStep;
                if (IsAlive(cup))
                {
                    farthest = Mathf.Max(farthest, cup.transform.position.z);
                }
            }

            // Assert: an der Wand zerbrochen, nie dahinter gewesen
            Assert.IsFalse(IsAlive(cup), "Die Tasse hätte an der Wand zerbrechen müssen.");
            Assert.Less(farthest, WallDistance + WallThickness, "Die Tasse war hinter der Wand.");
            AssertNoDetectionWarnings();
        }

        [UnityTest]
        public IEnumerator CupAtTwentyMetersPerSecond_DoesNotSkipThinWall()
        {
            // Arrange: schneller als jeder Wurf, wie nach einem langen Fall. Pro Physikschritt sind das
            // 0,4 m, mehr als Wand und Tasse zusammen tief sind (0,32 m). Der Startpunkt ist so gewählt,
            // dass die Tasse ohne durchgehende Kollisionsprüfung von 1,9 m direkt auf 2,3 m spränge,
            // also über die Wand (2,0 bis 2,2 m) hinweg, ohne sie je zu berühren.
            BuildWall();
            Vector3 start = OnPlatform(0f, ThrowHeight, SkipStartZ);
            NetworkObject cup = SpawnTea(start);

            // Act
            cup.GetComponent<Carryable>().Detach(start, Quaternion.identity, Vector3.forward * SkipSpeed);

            float farthest = start.z;
            for (int i = 0; i < FlightSteps && IsAlive(cup); i++)
            {
                yield return _nextPhysicsStep;
                if (IsAlive(cup))
                {
                    farthest = Mathf.Max(farthest, cup.transform.position.z);
                }
            }

            // Assert: an der Wand zerbrochen, nie dahinter gewesen
            Assert.IsFalse(IsAlive(cup), "Die Tasse hat die Wand übersprungen, statt an ihr zu zerbrechen.");
            Assert.Less(farthest, WallDistance + WallThickness, "Die Tasse war hinter der Wand.");
            AssertNoDetectionWarnings();
        }

        [UnityTest]
        public IEnumerator CrateThrownAtFifteenMetersPerSecond_NeverPassesThinWall()
        {
            // Arrange
            BuildWall();
            Vector3 start = OnPlatform(0f, ThrowHeight, 0f);
            NetworkObject crate = SpawnCrate(start);

            // Act
            crate.GetComponent<Carryable>().Detach(start, Quaternion.identity, Vector3.forward * FastThrowSpeed);

            float farthest = start.z;
            for (int i = 0; i < FlightSteps; i++)
            {
                yield return _nextPhysicsStep;
                farthest = Mathf.Max(farthest, crate.transform.position.z);
            }

            // Assert: Die Mitte der Kiste hat die Vorderseite der Wand nie erreicht.
            Assert.IsTrue(IsAlive(crate), "Die Kiste darf nicht verschwinden.");
            Assert.Less(farthest, WallDistance, "Die Kiste ist in oder durch die Wand geflogen.");
            AssertNoDetectionWarnings();
        }

        // ---------- Helfer ----------

        private static Vector3 OnPlatform(float x, float heightAbovePlatform, float z)
        {
            return new Vector3(x, PlatformTopY + heightAbovePlatform, z);
        }

        private static void BuildPlatform()
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "TestPlattform";
            platform.transform.position = new Vector3(0f, PlatformTopY - PlatformThickness * 0.5f, 0f);
            platform.transform.localScale = new Vector3(PlatformSize, PlatformThickness, PlatformSize);
        }

        // Eine 0,2 m dicke Wand quer zur Wurfrichtung, wie eine Innenwand der Hütte.
        private static void BuildWall()
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "TestWand";
            wall.transform.position = OnPlatform(0f, WallHeight * 0.5f, WallDistance + WallThickness * 0.5f);
            wall.transform.localScale = new Vector3(PlatformSize, WallHeight, WallThickness);
        }

        private static NetworkObject SpawnTea(Vector3 position)
        {
            NetworkObject tea = ItemSpawning.Spawn(HostSceneFixture.FindKind(TeaKindId), position, Quaternion.identity);
            Assert.IsNotNull(tea, "Die Tasse ist nicht erschienen.");
            return tea;
        }

        private static NetworkObject SpawnCrate(Vector3 position)
        {
            NetworkObject crate = ItemSpawning.Spawn(HostSceneFixture.FindKind(CrateKindId), position,
                Quaternion.identity, HostSceneFixture.FindWare(CrateWareId));
            Assert.IsNotNull(crate, "Die Kiste ist nicht erschienen.");
            return crate;
        }

        private IEnumerator WaitUntilResting(NetworkObject item)
        {
            var body = item.GetComponent<Rigidbody>();
            for (int i = 0; i < MaxStepsToRest && !body.IsSleeping(); i++)
            {
                yield return _nextPhysicsStep;
            }

            Assert.IsTrue(body.IsSleeping(), $"„{item.name}“ kam nicht zur Ruhe.");
        }

        private static bool IsAlive(NetworkObject item)
        {
            return item != null && item.IsSpawned;
        }

        private void AssertNoDetectionWarnings()
        {
            foreach (string warning in _warnings)
            {
                StringAssert.DoesNotContain(DetectionWarning, warning);
            }
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
