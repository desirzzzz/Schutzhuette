using System;
using UnityEngine;

namespace Schutzhuette.Items
{
    // Optionale Eigenschaften einer Gegenstandsart (ADR-0002, Entscheidung 2).
    // Jede Eigenschaft ist ein fester Block mit dem Schalter "Enabled". Eine neue Eigenschaft
    // ist ein neuer Block. Code erkennt eine Art an ihrem Block (kind.Fuel.Enabled),
    // nie an Kennung oder Namen.
    //
    // Die Konstruktoren gibt es nur im Editor (Tests und Editor-Skripte): Datenobjekte
    // werden zur Laufzeit nie geschrieben.

    /// <summary>Brennbar: Der Ofen verbraucht die Art und brennt dafür länger.</summary>
    [Serializable]
    public struct FuelBlock
    {
        [Tooltip("An: Die Art ist brennbar.")]
        [SerializeField] private bool _enabled;

        [Tooltip("So viele Sekunden brennt ein Stück im Ofen.")]
        [SerializeField, Min(0f)] private float _burnSeconds;

        /// <summary>Ob die Art brennbar ist.</summary>
        public bool Enabled => _enabled;

        /// <summary>Brenndauer eines Stücks in Sekunden.</summary>
        public float BurnSeconds => _burnSeconds;

#if UNITY_EDITOR
        internal FuelBlock(bool enabled, float burnSeconds)
        {
            _enabled = enabled;
            _burnSeconds = burnSeconds;
        }
#endif
    }

    /// <summary>Hackbar: Beim Hacken entstehen daraus mehrere Stück einer anderen Art.</summary>
    [Serializable]
    public struct ChopBlock
    {
        [Tooltip("An: Die Art lässt sich hacken.")]
        [SerializeField] private bool _enabled;

        [Tooltip("Diese Art entsteht beim Hacken.")]
        [SerializeField] private ItemKind _result;

        [Tooltip("So viele Stück entstehen beim Hacken.")]
        [SerializeField, Min(0)] private int _count;

        /// <summary>Ob die Art hackbar ist.</summary>
        public bool Enabled => _enabled;

        /// <summary>Die Art, die beim Hacken entsteht.</summary>
        public ItemKind Result => _result;

        /// <summary>Wie viele Stück beim Hacken entstehen.</summary>
        public int Count => _count;

#if UNITY_EDITOR
        internal ChopBlock(bool enabled, ItemKind result, int count)
        {
            _enabled = enabled;
            _result = result;
            _count = count;
        }
#endif
    }

    /// <summary>Vorratspaket: enthält Portionen einer Ware.</summary>
    [Serializable]
    public struct SupplyPackBlock
    {
        [Tooltip("An: Die Art ist ein Vorratspaket.")]
        [SerializeField] private bool _enabled;

        [Tooltip("So viele Portionen enthält ein Paket.")]
        [SerializeField, Min(0)] private int _portions;

        [Tooltip("Feste Ware (der Kanister enthält immer Wasser). Leer: Wer das Paket erzeugt, legt die Ware fest.")]
        [SerializeField] private WareKind _fixedWare;

        /// <summary>Ob die Art ein Vorratspaket ist.</summary>
        public bool Enabled => _enabled;

        /// <summary>Portionen je Paket.</summary>
        public int Portions => _portions;

        /// <summary>Feste Ware des Pakets, oder null, wenn die Ware beim Erscheinen festgelegt wird.</summary>
        public WareKind FixedWare => _fixedWare;

#if UNITY_EDITOR
        internal SupplyPackBlock(bool enabled, int portions, WareKind fixedWare)
        {
            _enabled = enabled;
            _portions = portions;
            _fixedWare = fixedWare;
        }
#endif
    }

    /// <summary>Gericht: erfüllt ein Gästebedürfnis und kühlt ab.</summary>
    [Serializable]
    public struct DishBlock
    {
        [Tooltip("An: Die Art ist ein fertiges Gericht.")]
        [SerializeField] private bool _enabled;

        [Tooltip("Welches Bedürfnis das Gericht erfüllt. Legt das Gäste-Dokument fest, bis dahin leer.")]
        [SerializeField] private NeedKind _need;

        [Tooltip("Sekunden, bis das Gericht kalt ist. 0 oder weniger: Es kühlt nie ab.")]
        [SerializeField] private float _coolTimeSeconds;

        /// <summary>Ob die Art ein Gericht ist.</summary>
        public bool Enabled => _enabled;

        /// <summary>Das Bedürfnis, das dieses Gericht erfüllt.</summary>
        public NeedKind Need => _need;

        /// <summary>Abkühlzeit in Sekunden. 0 oder weniger heißt: kühlt nie ab.</summary>
        public float CoolTimeSeconds => _coolTimeSeconds;

#if UNITY_EDITOR
        internal DishBlock(bool enabled, NeedKind need, float coolTimeSeconds)
        {
            _enabled = enabled;
            _need = need;
            _coolTimeSeconds = coolTimeSeconds;
        }
#endif
    }

    /// <summary>Zerbrechlich: zerbricht bei einem zu schnellen Aufprall.</summary>
    [Serializable]
    public struct FragileBlock
    {
        [Tooltip("An: Die Art kann zerbrechen.")]
        [SerializeField] private bool _enabled;

        [Tooltip("Prallt die Art schneller auf als so viele m/s, zerbricht sie.")]
        [SerializeField, Min(0f)] private float _breakSpeed;

        /// <summary>Ob die Art zerbrechlich ist.</summary>
        public bool Enabled => _enabled;

        /// <summary>Bruchgeschwindigkeit in m/s.</summary>
        public float BreakSpeed => _breakSpeed;

#if UNITY_EDITOR
        internal FragileBlock(bool enabled, float breakSpeed)
        {
            _enabled = enabled;
            _breakSpeed = breakSpeed;
        }
#endif
    }

    /// <summary>Werkzeug: ermöglicht eine Handlung.</summary>
    [Serializable]
    public struct ToolBlock
    {
        [Tooltip("An: Die Art ist ein Werkzeug.")]
        [SerializeField] private bool _enabled;

        [Tooltip("Diese Handlung ermöglicht das Werkzeug.")]
        [SerializeField] private ToolAction _action;

        /// <summary>Ob die Art ein Werkzeug ist.</summary>
        public bool Enabled => _enabled;

        /// <summary>Die Handlung, die das Werkzeug ermöglicht.</summary>
        public ToolAction Action => _action;

#if UNITY_EDITOR
        internal ToolBlock(bool enabled, ToolAction action)
        {
            _enabled = enabled;
            _action = action;
        }
#endif
    }

    /// <summary>Ausrüstung: wird beim Aufheben am Körper angelegt statt in der Hand getragen.</summary>
    [Serializable]
    public struct EquipmentBlock
    {
        [Tooltip("An: Die Art ist Ausrüstung.")]
        [SerializeField] private bool _enabled;

        [Tooltip("An diesem Platz sitzt die Ausrüstung.")]
        [SerializeField] private EquipmentSlot _slot;

        /// <summary>Ob die Art Ausrüstung ist.</summary>
        public bool Enabled => _enabled;

        /// <summary>Der Platz am Körper.</summary>
        public EquipmentSlot Slot => _slot;

#if UNITY_EDITOR
        internal EquipmentBlock(bool enabled, EquipmentSlot slot)
        {
            _enabled = enabled;
            _slot = slot;
        }
#endif
    }
}
