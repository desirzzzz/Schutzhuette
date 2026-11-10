using System.Collections.Generic;
using System.Text;

namespace Schutzhuette.Items
{
    /// <summary>
    /// Ergebnis der Katalogprüfung: Fehler sperren Build und Host-Start, Hinweise nicht
    /// (GDD Gegenstandsarten, "Edge Cases"; ADR-0002, Entscheidung 7).
    /// </summary>
    /// <example>
    /// <code>
    /// CatalogReport report = CatalogValidator.Validate(catalog, networkPrefabs);
    /// if (report.StartBlocked)
    /// {
    ///     foreach (string error in report.Errors)
    ///     {
    ///         Debug.LogError(error);
    ///     }
    /// }
    /// </code>
    /// </example>
    public sealed class CatalogReport
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _hints = new List<string>();

        /// <summary>Fehler. Jeder nennt die Kennung oder den Asset-Namen des Eintrags.</summary>
        public IReadOnlyList<string> Errors => _errors;

        /// <summary>Hinweise. Sie sperren nichts.</summary>
        public IReadOnlyList<string> Hints => _hints;

        /// <summary>Wahr, sobald es mindestens einen Fehler gibt: Build und Host dürfen nicht starten.</summary>
        public bool StartBlocked => _errors.Count > 0;

        internal void AddError(string message) => _errors.Add(message);

        internal void AddHint(string message) => _hints.Add(message);

        /// <summary>Alle Meldungen als mehrzeiliger Text, für Log und Fehleranzeige.</summary>
        public override string ToString()
        {
            var text = new StringBuilder();
            text.Append("Katalogprüfung: ").Append(_errors.Count).Append(" Fehler, ")
                .Append(_hints.Count).Append(" Hinweise");

            foreach (string error in _errors)
            {
                text.Append("\n  Fehler: ").Append(error);
            }

            foreach (string hint in _hints)
            {
                text.Append("\n  Hinweis: ").Append(hint);
            }

            return text.ToString();
        }
    }
}
