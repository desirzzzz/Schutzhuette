namespace Schutzhuette.Items
{
    // Die Zahlen sind fest vergeben: Unity speichert Enums als Zahl im Asset.
    // Ein neuer Wert kommt ans Ende, bestehende Zahlen ändern sich nie.

    /// <summary>Wie viele Träger eine Gegenstandsart braucht (GDD Gegenstandsarten, Regel 1).</summary>
    public enum CarryMode
    {
        /// <summary>Ein Spieler trägt sie allein.</summary>
        Solo = 0,

        /// <summary>Sie bewegt sich nur, wenn zwei Spieler sie halten (wie die Truhe).</summary>
        Pair = 1,
    }

    /// <summary>Kategorie einer Gegenstandsart, genau eine je Art.</summary>
    public enum ItemCategory
    {
        /// <summary>Brennstoff, zum Beispiel ein Holzscheit.</summary>
        Fuel = 0,

        /// <summary>Rohstoff, zum Beispiel ein Holzklotz.</summary>
        RawMaterial = 1,

        /// <summary>Vorratspaket mit Portionen einer Ware.</summary>
        SupplyPack = 2,

        /// <summary>Fertiges Gericht (Teller oder Tasse).</summary>
        Dish = 3,

        /// <summary>Werkzeug, zum Beispiel die Axt.</summary>
        Tool = 4,

        /// <summary>Sperrgut, zum Beispiel die Trage.</summary>
        Bulky = 5,
    }

    /// <summary>Klanggruppe beim Aufprall.</summary>
    public enum ImpactSoundGroup
    {
        /// <summary>Harter, hölzerner Klang.</summary>
        Wood = 0,

        /// <summary>Dumpfer, weicher Klang.</summary>
        Soft = 1,
    }

    /// <summary>
    /// Handlung, die ein Werkzeug ermöglicht. Liegt in Foundation, damit die Interaktion (Core)
    /// nach unten liest und nicht umgekehrt (ADR-0002, Entscheidung 2).
    /// </summary>
    public enum ToolAction
    {
        /// <summary>Holz hacken.</summary>
        Chop = 0,

        /// <summary>Erste Hilfe leisten.</summary>
        FirstAid = 1,

        /// <summary>Licht geben (Stirnlampe).</summary>
        Light = 2,

        /// <summary>Funken (Funkgerät).</summary>
        Radio = 3,
    }

    /// <summary>Platz am Körper für Ausrüstung.</summary>
    public enum EquipmentSlot
    {
        /// <summary>Am Kopf, zum Beispiel die Stirnlampe.</summary>
        Head = 0,

        /// <summary>Am Gürtel, zum Beispiel das Funkgerät.</summary>
        Belt = 1,
    }
}
