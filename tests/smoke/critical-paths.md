# Schnelltest: kritische Wege

**Zweck**: 10–15 Prüfungen in unter 15 Minuten, vor jedem Freundestest und jeder Übergabe an QA.
**Start über**: `/smoke-check` (liest diese Datei)
**Pflege**: Neue Einträge, sobald ein neues Kernsystem gebaut ist.
**Womit**: die aktuelle `Builds/Win64/Schutzhuette.exe`. Punkte mit „zu zweit“ nur mit dem Freund oder mit
zwei Fenstern (jedes mit eigenem `-logFile`).

## Stabilität (immer)

1. Die exe startet ohne Absturz, die Knöpfe „Host starten“ und „Beitreten“ sind zu sehen.
2. „Host starten“: Die Figur erscheint am Startplatz. Im Log steht „[Crates] … ausgelegt“.
3. Zu zweit: Der zweite Spieler tritt per IP bei. Im Log des Hosts steht „[Spawn] Client 1 angenommen“.

## Kernmechanik (pro Schritt ergänzen)

4. Laufen und Umschauen in der Ich-Perspektive; den Hang zur Hütte hinauf ohne Hängenbleiben.
5. Eine Kiste aufheben, tragen, ablegen und werfen.
6. Zu zweit: Die schwere Truhe bewegt sich nur, wenn beide sie halten.
7. Holz in den Ofen: Die Wärme steigt; ohne Nachlegen geht der Ofen aus.
8. Eine Kiste rutscht vom Hüttenplatz den Hang hinunter; zu Fuß zur Talstation und wieder hinauf.

## Spielstand

9. Speichern und Laden: entfällt, es gibt noch kein Speichersystem.

## Leistung

10. Keine sichtbaren Ruckler (Ziel 60 Bilder pro Sekunde).
11. Nach etwa 5 Minuten Spielen wächst der Speicher im Task-Manager nicht immer weiter.
