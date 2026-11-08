# Graubox-Hütte (Prototyp)

Bauplan für die Graubox in der Szene `Assets/_Project/Scenes/Prototype.unity`.

**Wozu:** Die Hütte soll die Prototyp-Frage beantworten: *Macht es Spaß, zu zweit eine schwere Kiste
durch eine Hütte zu tragen?* Deshalb hat sie einen Hang zum Hochtragen, eine 90°-Ecke im Vorraum,
Türen, durch die man eine Last fädeln muss, eine absichtlich enge Tür und Möbel als Hindernisse.
Laut `docs/KONZEPT.md` gehören dazu: Stube, Küche, Lager, Eingang und ein Hang davor.

## Grundregeln

- **Maßstab:** 1 Unity-Einheit = 1 Meter.
- **Richtungen:** X = nach rechts, Z = nach vorn (weg von den Startplätzen), Y = nach oben.
- **Spielfigur:** 2 m hoch, 1 m breit, Augen auf 1,7 m. Der CharacterController schafft Stufen bis
  0,3 m und Rampen bis 45° (Werte aus dem Player-Prefab).
- **Türen:** alle 2,3 m hoch. Breite 1,4 m (Eingang 1,6 m, Lager → Küche 1,2 m). Bei 1 m breiter
  Figur bleibt eine Hand breit Luft: Zu zweit hintereinander tragen geht, nebeneinander nicht.
- **Wände:** außen 0,3 m dick, innen 0,2 m, 2,8 m hoch. Die Hütte steht auf einem Sockel, der
  Hüttenboden liegt deshalb auf **1,5 m Höhe**.
- **Dach:** vorerst eine flache Platte auf den Wänden (Unterkante 4,3 m, 0,4 m Überstand), dazu ein
  Dielenbelag auf dem Hüttenboden und fünf Lampen. Das setzt `HutLookBuilder` (Menü
  „Schutzhütte/Farbe und Licht setzen“) zusammen mit den Materialien. Ein richtiges Satteldach
  kommt später mit dem Low-Poly-Asset-Pack.

## Grundriss (von oben, vorne = unten)

```
 z=22    +----------------+-------------------------------+
         |  KÜCHE         |                     [Ofen]    |
         |  [Küchenzeile] =  Tür 1,4                      |
 z=18.5  +-----[1,2]------+                     [T2]      |
         |  LAGER         |     [T3]            [T2]      |
         |  [Regal]       |                     [T2]      |
 z=15.5  +-----[1,4]------+          STUBE                |
         |  VORRAUM       |                     [T1]      |
         |                =  Tür 1,4            [T1]      |
 z=12    +-----[1,6]------+-------------------------------+
               Eingang
 x=      -6               -1                              6

 z=10-12   Terrasse (Sockel, 1,5 m hoch)
 z=2-10    Hang: steigt von 0 auf 1,5 m, 10 m breit (x -7 bis 3)
 z=0       Startplätze (x -3, -1, 1, 3)
```

`[1,4]` = Türöffnung mit ihrer Breite in Metern, `=` = Tür in der Innenwand zur Stube,
`[T1]`/`[T2]` = Tische mit Bänken, `[T3]` = Tisch in der Mitte.

## Räume

| Raum | Innenmaß (x × z) | Zweck im Prototyp |
| --- | --- | --- |
| Vorraum | 4,6 × 3,1 m | Eingang. Wer zur Stube will, muss um 90° abbiegen |
| Lager | 4,6 × 2,8 m | Ziel für Kisten. Klein und eng, wie im Konzept |
| Küche | 4,6 × 3,1 m | Später Kochen. Vom Lager nur durch die enge Tür (1,2 m) |
| Stube | 6,6 × 9,4 m | Ofen und Tische. Der Weg zum Ofen führt um Tisch 3 herum |
| Terrasse | 14 × 2 m | Flacher Absatz vor der Tür |
| Hang | 10 × 8 m, 1,5 m Anstieg (rund 11°) | Tragen bergauf |

## Tragewege, die wir testen wollen

1. **Kiste ins Lager:** Startplatz → Hang hoch → Eingang → geradeaus durch den Vorraum → Lager.
2. **Schweres zum Ofen:** Startplatz → Hang → Eingang → 90°-Ecke im Vorraum → Stube → um Tisch 3
   herum → Ofen.
3. **Engstelle:** Lager → Küche (1,2 m) → Stube.

Wenn ein Weg nervt statt Spaß zu machen: Möbel verschieben ist ausdrücklich erlaubt.

## So baust du ein Teil

Jedes Teil ist ein Quader. Am schnellsten geht es so:

1. **GameObject → 3D Object → Cube**
2. Im Inspector **Position**, **Rotation** und **Scale** aus der Tabelle eintragen. Scale ist bei
   einem Unity-Würfel genau die Größe in Metern.
3. Alles unter ein leeres Objekt `Huette` auf Position (0, 0, 0) ziehen, damit die Hierarchy
   ordentlich bleibt.

ProBuilder ist noch nicht installiert. Für diese Grundform brauchst du es nicht. Zum Nachformen
später (Türrahmen, schräge Kanten) installierst du es über **Window → Package Manager → Unity
Registry → ProBuilder**.

Alle Werte sind Weltkoordinaten. Rotation ist (0, 0, 0), wo nichts anderes steht.

### Gelände

| Teil | Position (x, y, z) | Rotation | Scale (x, y, z) |
| --- | --- | --- | --- |
| Ground (gibt es schon, nur Scale ändern) | 0, 0, 0 | | 6, 1, 6 |
| Sockel | 0, 0.75, 16.5 | | 14, 1.5, 13 |
| Hang | -2, 0.5, 6.05 | -10.6, 0, 0 | 10, 0.5, 8.15 |

Der Hang ist ein gekippter Quader. Seine Oberkante läuft von (z 2, Höhe 0) bis (z 10, Höhe 1,5).
Die Teile, die unten im Boden oder hinten im Sockel stecken, sieht man nicht.

### Außenwände

| Teil | Position (x, y, z) | Scale (x, y, z) |
| --- | --- | --- |
| Wand vorne links | -5.15, 2.9, 12.15 | 1.7, 2.8, 0.3 |
| Wand vorne rechts | 1.65, 2.9, 12.15 | 8.7, 2.8, 0.3 |
| Sturz über Eingang | -3.5, 4.05, 12.15 | 1.6, 0.5, 0.3 |
| Wand hinten | 0, 2.9, 21.85 | 12, 2.8, 0.3 |
| Wand links | -5.85, 2.9, 17 | 0.3, 2.8, 10 |
| Wand rechts | 5.85, 2.9, 17 | 0.3, 2.8, 10 |

### Innenwand zur Stube (bei x = -1)

| Teil | Position (x, y, z) | Scale (x, y, z) |
| --- | --- | --- |
| Innenwand vorne | -1, 2.9, 12.65 | 0.2, 2.8, 0.7 |
| Innenwand Mitte | -1, 2.9, 16.85 | 0.2, 2.8, 4.9 |
| Innenwand hinten | -1, 2.9, 21.2 | 0.2, 2.8, 1.0 |
| Sturz Tür Vorraum → Stube | -1, 4.05, 13.7 | 0.2, 0.5, 1.4 |
| Sturz Tür Küche → Stube | -1, 4.05, 20.0 | 0.2, 0.5, 1.4 |

### Wand Vorraum / Lager (bei z = 15.5)

| Teil | Position (x, y, z) | Scale (x, y, z) |
| --- | --- | --- |
| Wand links | -4.95, 2.9, 15.5 | 1.5, 2.8, 0.2 |
| Wand rechts | -1.95, 2.9, 15.5 | 1.7, 2.8, 0.2 |
| Sturz | -3.5, 4.05, 15.5 | 1.4, 0.5, 0.2 |

### Wand Lager / Küche (bei z = 18.5, enge Tür)

| Teil | Position (x, y, z) | Scale (x, y, z) |
| --- | --- | --- |
| Wand links | -4.9, 2.9, 18.5 | 1.6, 2.8, 0.2 |
| Wand rechts | -2.0, 2.9, 18.5 | 1.8, 2.8, 0.2 |
| Sturz | -3.5, 4.05, 18.5 | 1.2, 0.5, 0.2 |

### Einrichtung (Platzhalter)

| Teil | Position (x, y, z) | Scale (x, y, z) |
| --- | --- | --- |
| Ofen (seit 2026-11-08 kein Würfel mehr, sondern das Prefab `Stove`, das der Host beim Start an diese Stelle setzt) | 2.2, 2.1, 20.9 | 1.4, 1.2, 1.4 |
| Tisch 1 | 4.0, 1.9, 14.5 | 1.0, 0.8, 2.4 |
| Bank 1 links | 2.9, 1.725, 14.5 | 0.4, 0.45, 2.4 |
| Bank 1 rechts | 5.1, 1.725, 14.5 | 0.4, 0.45, 2.4 |
| Tisch 2 | 4.0, 1.9, 18.0 | 1.0, 0.8, 2.4 |
| Bank 2 links | 2.9, 1.725, 18.0 | 0.4, 0.45, 2.4 |
| Bank 2 rechts | 5.1, 1.725, 18.0 | 0.4, 0.45, 2.4 |
| Tisch 3 (Mitte) | 1.0, 1.9, 16.5 | 1.2, 0.8, 1.2 |
| Regal im Lager | -5.3, 2.5, 17.0 | 0.6, 2.0, 2.4 |
| Küchenzeile | -3.4, 1.95, 21.3 | 3.0, 0.9, 0.7 |

## Prüfen, ob es passt

- Mit Play und „Host starten“ stehst du am Fuß des Hangs und schaust zur Hütte.
- Lauf alle drei Tragewege einmal ab. Bleibst du irgendwo hängen, sag mir die Stelle.
- Zu zweit: Passen beide Kapseln gleichzeitig durch den Vorraum? Sie sollen es knapp tun.
