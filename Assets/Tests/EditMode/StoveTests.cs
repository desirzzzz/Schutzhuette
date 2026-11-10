using NUnit.Framework;
using Schutzhuette.Hut;
using UnityEditor;
using UnityEngine;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Prüft die Rechenregel des Holzofens (<see cref="Stove.Step"/>) ohne Szene und ohne Netz.
    /// </summary>
    public class StoveTests
    {
        // Testwerte, absichtlich andere als im Asset StoveSettings, damit ein Tuning dort keinen Test bricht.
        private const float MaxHeat = 100f;
        private const float HeatRisePerSecond = 4f;
        private const float HeatDecayPerSecond = 1.5f;
        private const float Tolerance = 0.0001f;

        private StoveSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = CreateSettings(MaxHeat, HeatRisePerSecond, HeatDecayPerSecond);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void Burning_FuelDropsAndHeatRises()
        {
            // Arrange
            const float fuel = 10f;
            const float heat = 20f;

            // Act
            Stove.Step(fuel, heat, _settings, 1f, out float newFuel, out float newHeat);

            // Assert
            Assert.AreEqual(9f, newFuel, Tolerance);
            Assert.AreEqual(heat + HeatRisePerSecond, newHeat, Tolerance);
        }

        [Test]
        public void BurningNearMaxHeat_HeatStopsAtMaxHeat()
        {
            // Arrange
            const float heat = MaxHeat - 1f;

            // Act
            Stove.Step(10f, heat, _settings, 1f, out _, out float newHeat);

            // Assert
            Assert.AreEqual(MaxHeat, newHeat, Tolerance);
        }

        [Test]
        public void LastFuelBurnsUp_FuelStopsAtZero()
        {
            // Arrange: weniger Brennstoff übrig, als der Zeitschritt lang ist.
            const float fuel = 0.5f;

            // Act
            Stove.Step(fuel, 20f, _settings, 1f, out float newFuel, out _);

            // Assert
            Assert.AreEqual(0f, newFuel, Tolerance);
        }

        [Test]
        public void StoveOut_HeatDecays()
        {
            // Arrange
            const float heat = 50f;
            const float deltaTime = 2f;

            // Act
            Stove.Step(0f, heat, _settings, deltaTime, out float newFuel, out float newHeat);

            // Assert
            Assert.AreEqual(0f, newFuel, Tolerance);
            Assert.AreEqual(heat - HeatDecayPerSecond * deltaTime, newHeat, Tolerance);
        }

        [Test]
        public void StoveOutAndAlmostCold_HeatStopsAtZero()
        {
            // Arrange
            const float heat = 1f;

            // Act
            Stove.Step(0f, heat, _settings, 10f, out _, out float newHeat);

            // Assert
            Assert.AreEqual(0f, newHeat, Tolerance);
        }

        // Baut Ofen-Einstellungen mit festen Testwerten. Die Felder sind privat (SerializeField),
        // deshalb über SerializedObject. Heißt ein Feld einmal anders, schlägt der Test hier deutlich fehl.
        private static StoveSettings CreateSettings(float maxHeat, float heatRisePerSecond, float heatDecayPerSecond)
        {
            var settings = ScriptableObject.CreateInstance<StoveSettings>();
            var serialized = new SerializedObject(settings);
            SetFloat(serialized, "_maxHeat", maxHeat);
            SetFloat(serialized, "_heatRisePerSecond", heatRisePerSecond);
            SetFloat(serialized, "_heatDecayPerSecond", heatDecayPerSecond);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static void SetFloat(SerializedObject serialized, string fieldName, float value)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            Assert.IsNotNull(property, $"StoveSettings hat kein Feld \"{fieldName}\" mehr.");
            property.floatValue = value;
        }
    }
}
