namespace Schutzhuette.Items
{
    /// <summary>
    /// Zustand eines Gegenstands beim Host (GDD Gegenstandsarten, "States and Transitions";
    /// ADR-0001, Entscheidung 2). Der Zustand geht nicht ins Netz: Nur der Host kennt ihn.
    /// </summary>
    public enum ItemPhysicsState
    {
        /// <summary>Liegt: Der Körper ruht oder kommt gerade zur Ruhe.</summary>
        Resting = 0,

        /// <summary>Getragen: hängt an einer Spielfigur und ist kinematisch.</summary>
        Carried = 1,

        /// <summary>Fällt: losgelassen, geworfen oder angestoßen. Nur in diesem Zustand kann etwas zerbrechen.</summary>
        Falling = 2,
    }
}
