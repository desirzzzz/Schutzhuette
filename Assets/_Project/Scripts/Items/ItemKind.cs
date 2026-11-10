using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Eine Gegenstandsart, zum Beispiel "Holzscheit": Kennung, Masse, Trageart und Eigenschaften.
    /// Tragen, Ofen, Lager, Kochen und Gäste lesen diese Daten, statt eigene Werte zu führen
    /// (GDD Gegenstandsarten, Regeln 1 und 2; ADR-0002).
    /// </summary>
    /// <remarks>
    /// Zur Laufzeit schreibgeschützt: Die Methoden zum Befüllen gibt es nur im Editor.
    /// Zustand eines einzelnen Gegenstands (zum Beispiel die Ware in einer Kiste) gehört an den
    /// Gegenstand, nie in die Art.
    /// </remarks>
    /// <example>
    /// Eine Art an ihrer Eigenschaft erkennen, nie an Kennung oder Namen:
    /// <code>
    /// if (kind.Fuel.Enabled)
    /// {
    ///     float seconds = kind.Fuel.BurnSeconds;
    /// }
    /// </code>
    /// </example>
    [CreateAssetMenu(fileName = "ItemKind", menuName = "Schutzhütte/Gegenstandsart")]
    public class ItemKind : ScriptableObject
    {
        [Header("Pflichtfelder")]
        [Tooltip("Feste Kennung, zum Beispiel holzscheit. Nur a–z, 0–9 und _, höchstens 29 Zeichen. Ändert sich nie.")]
        [SerializeField] private string _id = "";

        [Tooltip("Anzeigename auf Deutsch.")]
        [SerializeField] private string _displayName = "";

        [Tooltip("Masse in kg, größer als 0. Der Rigidbody übernimmt sie beim Erscheinen.")]
        [SerializeField, Min(0f)] private float _massKg = 1f;

        [Tooltip("Allein tragbar oder nur zu zweit.")]
        [SerializeField] private CarryMode _carryMode = CarryMode.Solo;

        [Tooltip("Kategorie, genau eine.")]
        [SerializeField] private ItemCategory _category = ItemCategory.RawMaterial;

        [Tooltip("Klanggruppe beim Aufprall.")]
        [SerializeField] private ImpactSoundGroup _impactSound = ImpactSoundGroup.Wood;

        [Tooltip("Das Netzobjekt, das in der Welt erscheint. Jede Art hat ihr eigenes Prefab.")]
        [SerializeField] private NetworkObject _prefab;

        [Header("Optionale Eigenschaften")]
        [SerializeField] private FuelBlock _fuel;
        [SerializeField] private ChopBlock _chop;
        [SerializeField] private SupplyPackBlock _supplyPack;
        [SerializeField] private DishBlock _dish;
        [SerializeField] private FragileBlock _fragile;
        [SerializeField] private ToolBlock _tool;
        [SerializeField] private EquipmentBlock _equipment;

        /// <summary>Feste, eindeutige Kennung, zum Beispiel "holzscheit". Nur sie geht übers Netz.</summary>
        public string Id => _id;

        /// <summary>Anzeigename auf Deutsch.</summary>
        public string DisplayName => _displayName;

        /// <summary>Masse in kg. Einzige Quelle für das Gewicht.</summary>
        public float MassKg => _massKg;

        /// <summary>Allein tragbar oder nur zu zweit.</summary>
        public CarryMode CarryMode => _carryMode;

        /// <summary>Kategorie der Art.</summary>
        public ItemCategory Category => _category;

        /// <summary>Klanggruppe beim Aufprall.</summary>
        public ImpactSoundGroup ImpactSound => _impactSound;

        /// <summary>Das Netzobjekt, das in der Welt erscheint.</summary>
        public NetworkObject Prefab => _prefab;

        /// <summary>Eigenschaft "brennbar".</summary>
        public FuelBlock Fuel => _fuel;

        /// <summary>Eigenschaft "hackbar".</summary>
        public ChopBlock Chop => _chop;

        /// <summary>Eigenschaft "Vorratspaket".</summary>
        public SupplyPackBlock SupplyPack => _supplyPack;

        /// <summary>Eigenschaft "Gericht".</summary>
        public DishBlock Dish => _dish;

        /// <summary>Eigenschaft "zerbrechlich".</summary>
        public FragileBlock Fragile => _fragile;

        /// <summary>Eigenschaft "Werkzeug".</summary>
        public ToolBlock Tool => _tool;

        /// <summary>Eigenschaft "Ausrüstung".</summary>
        public EquipmentBlock Equipment => _equipment;

#if UNITY_EDITOR
        // Befüllen nur im Editor (Tests und Editor-Skripte). In der exe gibt es diese Methoden nicht:
        // Ruft Spielcode sie auf, schlägt der Build fehl.

        internal void EditorSetBasics(string id, string displayName, float massKg, CarryMode carryMode,
            ItemCategory category, ImpactSoundGroup impactSound, NetworkObject prefab)
        {
            _id = id;
            _displayName = displayName;
            _massKg = massKg;
            _carryMode = carryMode;
            _category = category;
            _impactSound = impactSound;
            _prefab = prefab;
        }

        internal void EditorSetFuel(FuelBlock fuel) => _fuel = fuel;

        internal void EditorSetChop(ChopBlock chop) => _chop = chop;

        internal void EditorSetSupplyPack(SupplyPackBlock supplyPack) => _supplyPack = supplyPack;

        internal void EditorSetDish(DishBlock dish) => _dish = dish;

        internal void EditorSetFragile(FragileBlock fragile) => _fragile = fragile;

        internal void EditorSetTool(ToolBlock tool) => _tool = tool;

        internal void EditorSetEquipment(EquipmentBlock equipment) => _equipment = equipment;
#endif
    }
}
