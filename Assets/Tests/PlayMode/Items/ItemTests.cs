// Die Tests befüllen Datenobjekte über Methoden, die es nur im Editor gibt.
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Schutzhuette.Items;
using UnityEngine;
using UnityEngine.TestTools;

namespace Schutzhuette.Tests
{
    /// <summary>
    /// Prüft das Etikett <see cref="Item"/> im laufenden Spielmodus: GDD Gegenstandsarten,
    /// Kriterium 7 (die Masse kommt aus dem Katalog) und Kriterium 8 (ein Gegenstand ohne Art
    /// meldet genau einen Fehler mit seinem Namen).
    /// </summary>
    public class ItemTests
    {
        // Werte aus Kriterium 7: Das Prefab wiegt 1 kg, der Katalog sagt 3 kg.
        private const float PrefabMassKg = 1f;
        private const float CatalogMassKg = 3f;
        private const float Tolerance = 0.0001f;

        private const string KindId = "holzscheit_test";
        private const string ObjectWithKind = "TestHolzscheit";
        private const string ObjectWithoutKind = "TestOhneArt";
        private const string ObjectWithoutItem = "TestOhneEtikett";

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator ItemWithKind_RigidbodyTakesMassFromCatalog()
        {
            // Arrange
            ItemKind kind = NewKind(CatalogMassKg);
            GameObject template = NewTemplate(ObjectWithKind, kind);

            // Act: Erst beim Einschalten läuft Awake, wie beim Erscheinen im Spiel.
            GameObject instance = Activate(template);
            yield return null;

            // Assert
            Assert.AreEqual(CatalogMassKg, instance.GetComponent<Rigidbody>().mass, Tolerance);
            Assert.IsTrue(Item.HasKind(instance.transform));
        }

        [UnityTest]
        public IEnumerator ItemWithoutKind_LogsExactlyOneErrorNamingTheObjectAndKeepsPrefabMass()
        {
            // Arrange: genau ein Fehler mit dem Objektnamen wird erwartet.
            GameObject template = NewTemplate(ObjectWithoutKind, null);
            LogAssert.Expect(LogType.Error, new Regex(ObjectWithoutKind));

            // Act
            GameObject instance = Activate(template);
            yield return null;

            // Assert
            Assert.IsFalse(Item.HasKind(instance.transform));
            Assert.AreEqual(PrefabMassKg, instance.GetComponent<Rigidbody>().mass, Tolerance);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ObjectWithoutItemComponent_ReportLogsExactlyOneErrorNamingTheObject()
        {
            // Arrange: ein Objekt ganz ohne Etikett, wie es das Tragen beim Start meldet.
            var bare = new GameObject(ObjectWithoutItem);
            _created.Add(bare);
            LogAssert.Expect(LogType.Error, new Regex(ObjectWithoutItem));

            // Act
            Item.ReportIfMissing(bare.transform);
            yield return null;

            // Assert
            Assert.IsFalse(Item.HasKind(bare.transform));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ObjectWithItemComponent_ReportLogsNothing()
        {
            // Arrange
            ItemKind kind = NewKind(CatalogMassKg);
            GameObject instance = Activate(NewTemplate(ObjectWithKind, kind));

            // Act
            Item.ReportIfMissing(instance.transform);
            yield return null;

            // Assert
            LogAssert.NoUnexpectedReceived();
        }

        private ItemKind NewKind(float massKg)
        {
            var kind = ScriptableObject.CreateInstance<ItemKind>();
            _created.Add(kind);
            kind.EditorSetBasics(KindId, "Testscheit", massKg, CarryMode.Solo, ItemCategory.Fuel,
                ImpactSoundGroup.Wood, null);
            return kind;
        }

        // Ausgeschaltete Vorlage mit Rigidbody und Etikett. Ausgeschaltet läuft Awake noch nicht.
        private GameObject NewTemplate(string name, ItemKind kind)
        {
            var template = new GameObject(name);
            _created.Add(template);
            template.SetActive(false);

            var body = template.AddComponent<Rigidbody>();
            body.mass = PrefabMassKg;
            body.useGravity = false;

            template.AddComponent<Item>().EditorSetKind(kind);
            return template;
        }

        private GameObject Activate(GameObject template)
        {
            GameObject instance = Object.Instantiate(template);
            _created.Add(instance);
            instance.SetActive(true);
            return instance;
        }
    }
}
#endif
