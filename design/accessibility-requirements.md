# Accessibility Requirements

> **Status**: Committed (Stufe und alle Abschnitte vom Entwickler freigegeben, noch nicht mit `/ux-review` geprüft)
> **Author**: Entwickler + ux-designer (`/ux-design accessibility`)
> **Last Updated**: 2026-11-10
> **Accessibility Tier Target**: Standard
> **Platform(s)**: PC (Steam, Windows); Maus und Tastatur, Gamepad teilweise
> **Linked Documents**: `design/gdd/systems-index.md`, `design/ux/interaction-patterns.md`
> **Template**: Accessibility Requirements

> **Wozu dieses Dokument:** Hinweise zur Barrierefreiheit für einzelne Bildschirme stehen in den
> UX-Dokumenten. Hier stehen die Zusagen für das ganze Spiel, die Übersicht je System, der Testplan und
> die Prüfungen. Widerspricht ein Feature einer Zusage hier, wird das Feature geändert, nicht die Zusage.
> **Aktualisieren:** nach jedem `/gate-check`, nach jeder Prüfung und wenn `systems-index.md` ein System
> dazubekommt.

---

## Accessibility Tier Definition

### Tier Definitions

| Stufe | Kernzusage | Aufwand |
|---|---|---|
| **Basic** | Spielwichtiger Text ist lesbar. Nichts hängt nur an Farbe. Musik, Effekte und Stimmen haben eigene Lautstärken. Keine Gefahr durch Blitze. | gering, vor allem Gestaltungsregeln |
| **Standard** | Basic plus: freie Tastenbelegung, Untertitel mit Sprecher, einstellbare Textgröße, mindestens ein Farbschwäche-Modus, keine Zeit-Eingabe, die sich nicht verlängern oder umschalten lässt. | mittel, eigene Umsetzung nötig |
| **Comprehensive** | Standard plus: Bildschirmleser für Menüs, Mono-Ton, Hilfen bei der Schwierigkeit, verschiebbares HUD, reduzierte Bewegung, sichtbare Hinweise für alle spielwichtigen Geräusche. | hoch |
| **Exemplary** | Comprehensive plus: Untertitel frei gestaltbar, hoher Kontrast, Hilfen gegen Überforderung, Vibration statt Ton, externe Prüfung. | sehr hoch, eigenes Budget |

### This Project's Commitment

**Target Tier**: Standard

**Rationale**: Schutzhütte ist ein Koop-Spiel in der Ich-Perspektive mit Zeitdruck, Physik und Stationen, an
denen man eine Taste gedrückt hält. Die größten Hürden liegen deshalb bei der Bewegung (langes Halten, Maus
und Tastatur gleichzeitig), beim Sehen (dunkle Nacht, Symbole über den Gästen, Übelkeit durch die wippende
Kamera) und beim Hören (Sprachchat und rauschendes Funkgerät im Nachteinsatz). Sprachausgabe, Kampf und
schnelle Reflexe gibt es nicht. Standard deckt diese Hürden ab, und vieles davon ist schon geplant: freie
Tastenbelegung, Halten als Umschalten (`design/gdd/interaktion.md`, offene Frage 3), Symbole statt nur Farbe
(`design/gdd/gaeste.md`). Comprehensive ist für einen einzelnen Entwickler bis zum Early Access (Februar 2028)
nicht zu schaffen. Mit nur Basic fehlte die freie Tastenbelegung, und Linkshänder und Spieler mit
eingeschränkter Beweglichkeit könnten schlechter mitspielen.

**Features explicitly in scope (beyond tier baseline)**:
- **Sichtfeld-Regler und „Kamerabewegung reduzieren“:** Ich-Perspektive mit wackelnden Lasten, davon wird
  manchen Spielern übel.
- **Mono-Ton** (aus Comprehensive vorgezogen): Im Nachteinsatz verrät die Richtung eines Geräuschs, wo der
  Vermisste ruft. Wer auf einem Ohr schlecht hört, verpasst das sonst. Die Audio-API von Unity dafür wird vor
  dem Bau in `docs/engine-reference/unity/` geprüft.

**Features explicitly out of scope**:
- **Zielhilfe** und **Zeitfenster-Anpassung für Schnelltasten:** kein Kampf, keine Quick-Time-Events. Beides
  trifft nicht zu und ist keine Lücke. Kommt eine zeitkritische Eingabe dazu, gilt die Regel in Motor
  Accessibility.

---

## Visual Accessibility

| Feature | Target Tier | Scope | Status | Implementation Notes |
|---|---|---|---|---|
| Mindest-Textgröße Menüs | Standard | alle Menüs | Not Started | mindestens 24 px bei 1080p, wächst mit der Auflösung |
| Mindest-Textgröße HUD | Standard | Hinweis am Fadenkreuz, Phase und Uhr, Gästeliste | Not Started | mindestens 20 px bei 1080p für alles Spielwichtige |
| Symbole über Gästen | Standard | Symbol, Geduldsring, Gesicht | Not Started | lesbar aus 10 m (`gaeste.md`, Visual/Audio). Die Größe in Pixeln legt `design/ux/hud.md` fest. |
| Text-Kontrast | Standard | alle Texte | Not Started | 4,5:1, große Schrift (ab 18 px oder 14 px fett) 3:1 (WCAG 2.1 AA). Der Hinweis am Fadenkreuz liegt über hellem Schnee und über dunkler Nacht, deshalb mit Schatten oder halbdurchsichtigem Kasten dahinter. In beiden Lagen messen. |
| UI-Größe | Standard | HUD und Menüs getrennt | Not Started | 75–150 %, Standard 100 %. Alle Bildschirme bei 75 % und 150 % prüfen. |
| Farbschwäche-Modus Rot-Grün (Protanopie, Deuteranopie) | Standard | alle Farbsignale | Not Started | ein Modus mit anderer Palette für Geduldsring, Stimmung und Wärme-Anzeige. Mit einem Simulator (z. B. Coblis) an Bildschirmfotos prüfen. |
| Blau-Gelb (Tritanopie) | Standard | alle Farbsignale | Not Started | kein eigener Modus. Abgedeckt durch die Regel „nie nur Farbe“ (Tabelle unten). |
| Nie nur Farbe | Basic | alles im Spiel und im UI | Not Started | Jedes Farbsignal hat einen Ersatz ohne Farbe, siehe Tabelle unten. |
| Helligkeit | Basic | global | Not Started | Die Nacht mit Stromausfall ist absichtlich dunkel. Regler mit Kalibrierbild (ein Symbol, das bei richtiger Einstellung gerade noch zu sehen ist), etwa −50 % bis +50 %. |
| Blitze und Flackern | Basic | Stromausfall, Gewitter, alle Effekte | Not Started | Licht flackert höchstens 3-mal pro Sekunde (WCAG 2.3.1). Warnhinweis zu Lichtempfindlichkeit beim ersten Start. |
| Kamerabewegung reduzieren | Standard | Kamerawippen beim Gehen, Halten und Tragen, Stöße (`CameraFeel`) | Not Started | ein Schalter in den Einstellungen. Die Bewegung der eigenen Figur bleibt. |
| Sichtfeld-Regler | ergänzt | Ich-Kamera | Not Started | 60–100°. Standardwert legt das Menü-Dokument fest. |
| Untertitel (an/aus, Sprecher, Größe) | Basic/Standard | — | N/A | Es gibt keine Sprachausgabe, nur Laute wie Murren und „Aua“. Die gehören zu Auditory Accessibility. |
| Hoher Kontrast, Untertitel frei gestaltbar | Comprehensive/Exemplary | — | Not Planned | über der gewählten Stufe |

### Color-as-Only-Indicator Audit

| Location | Color Signal | What It Communicates | Non-Color Backup | Status |
|---|---|---|---|---|
| Geduldsring über dem Gast | wird im letzten Drittel rot | Gast wird gleich ungeduldig | Füllstand des Rings (`gaeste.md`, UI Requirements) | Not Started |
| Stimmung des Gastes | Farbe des Gesichts | zufrieden, neutral, mault | Gesichtsausdruck, verschränkte Arme | Not Started |
| Gästetypen | eine Farbe je Typ (Vertical Slice) | Wanderer, Familie, Bergsteiger | Form: Rucksack, großes und kleines Paar, Helm mit Seil | Not Started |
| Wärme der Stube (HUD) | vermutlich warme und kalte Farben | wie warm die Hütte ist | Zahl oder Balken mit Thermometer-Symbol. Offen, wird in `design/ux/hud.md` entschieden. | Not Started |
| Namensschilder der Spieler | Farbe je Spieler | wer wer ist | Der Name steht im Schild. | Not Started |

---

## Motor Accessibility

| Feature | Target Tier | Scope | Status | Implementation Notes |
|---|---|---|---|---|
| Freie Tastenbelegung | Standard | alle Aktionen, Tastatur/Maus und Gamepad getrennt | Not Started | Das neue Input System kann Aktionen umbelegen; die API wird vor dem Bau in `docs/engine-reference/unity/` geprüft. Warnung bei doppelter Belegung. Lokal je Spieler gespeichert. |
| Eingabe wechseln | Standard | jederzeit, ohne Neustart | Not Started | Der Hinweis am Fadenkreuz zeigt die Taste des gerade benutzten Geräts. |
| Halten als Umschalten | Standard | alle Halte-Aktionen | Not Started | Heute geplant: Holz hacken, Kurbel der Seilbahn, Bett machen, Erste Hilfe, Generator reparieren (`interaktion.md`). Einmal drücken startet, nochmal drücken stoppt. Aufheben, Tragen und Ablegen mit E sind schon Umschalten. Jede neue Halte-Aktion kommt in diese Liste. |
| Kein schnelles Drücken | Standard | Regel für alle Aktionen | Not Started | Keine Aktion verlangt mehr als 3 Drücke pro Sekunde über längere Zeit. Holz hacken ist Halten, nicht Hämmern. |
| Zeitfenster für Eingaben | Standard | — | N/A | Es gibt keine Quick-Time-Events und keine Rhythmus-Eingaben. Kommt eine dazu, braucht sie einen Regler für das Zeitfenster (0,5- bis 3-fach). |
| Empfindlichkeit, Y-Achse umkehren | ergänzt | Maus und Stick | Not Started | Grundausstattung in der Ich-Perspektive, getrennt für Maus und Stick |
| Auto-Laufen | Standard | Bewegung, vor allem der lange Weg im Nachteinsatz | Not Started | Taste einmal drücken, die Figur läuft geradeaus weiter, bis man wieder eine Bewegungstaste drückt. |
| Zielhilfe | Standard | — | N/A | kein Kampf, kein Zielen. Werfen ist nie nötig, alles lässt sich auch tragen. |
| Klettern und Springen | — | — | N/A | Es gibt kein Springen und keine Kletterstellen. |
| Einhand-Modus | — | Tragen zu zweit, Laufen mit Last | Not Planned | Tragen braucht Laufen und Umschauen gleichzeitig. Die freie Tastenbelegung hilft teilweise. Steht unter Known Intentional Limitations. |
| HUD verschieben | Comprehensive | — | Not Planned | über der gewählten Stufe |

---

## Cognitive Accessibility

| Feature | Target Tier | Scope | Status | Implementation Notes |
|---|---|---|---|---|
| Schwierigkeit „Gemütlich / Normal“ | Standard | Gäste, Ereignisse | Not Started | Der Host wählt in der Vorbereitung vor jeder Saison. „Gemütlich“: Gäste sind geduldiger, Ereignisse kommen seltener. Die Faktoren stehen in den Datenobjekten (Architektur-Regel 4) und werden im Spieltest eingestellt. Saisonlänge und Phasen bleiben gleich. Feste Teile: Physik, Tragen zu zweit, Wärme des Ofens. |
| Pause | Basic | alle Spielzustände | Not Started | nur solo (`saison-ablauf.md`, Regel 11). Im Koop läuft die Zeit weiter, wenn jemand das Menü öffnet, weil alle dieselbe Saison spielen. Steht unter Known Intentional Limitations. |
| Hilfe zum Nachlesen | Standard | Tutorial, Hinweise | Not Started | Hilfeseite im Menü mit Steuerung und allen Bedürfnis-Symbolen. Jeder Tutorial-Hinweis ist dort später wieder zu finden (Tutorial: Alpha). |
| Was gerade zu tun ist | Standard | Gäste, Saison | Not Started | ohne Tastendruck immer sichtbar: Symbole über den Gästen, Liste der offenen Wünsche im HUD, aktuelle Phase mit Vorwarnung 30 s vor dem Wechsel (`saison-ablauf.md`, UI Requirements) |
| Sichtbare Hinweise für Geräusche | Standard | alle spielwichtigen Geräusche | Not Started | siehe Gameplay-Critical SFX Audit |
| Lesezeit | Standard | Meldungen und Hinweise | Not Started | Meldungen, auf die man reagieren muss, verschwinden nie von selbst (z. B. Beitritt abgelehnt, „Verbindung getrennt.“). Der Hinweis am Fadenkreuz bleibt, solange man hinschaut. Reine Rückmeldungen wie „+6“ beim Trinkgeld dürfen verschwinden. |
| Belastung je System | Comprehensive | jedes System | Not Started | trotzdem in der Per-Feature Accessibility Matrix mitgezählt. Mehr als 4 Dinge gleichzeitig ist ein Anlass zur Prüfung, keine harte Grenze. |
| Weg finden | Standard | Hütte, Nachteinsatz | Not Started | Die Hütte ist klein. Im Nachteinsatz ist die Suche nach dem Vermissten die Aufgabe, deshalb gibt es dorthin **keinen Wegpunkt**. Die Hütte ist draußen immer an ihren Lichtern zu erkennen, Markierungen der Mitspieler zeigen den Weg, und der Hilferuf hat eine Richtungsanzeige (Auditory). |

---

## Auditory Accessibility

| Feature | Target Tier | Scope | Status | Implementation Notes |
|---|---|---|---|---|
| Markieren und Rufe | ergänzt | Absprache ohne Stimme, vor allem im Nachteinsatz | Not Started | Eine Taste markiert die Stelle, auf die man schaut, für alle sichtbar. Ein Rad mit Rufen, Vorschlag: „Hierher!“, „Gefunden!“, „Hilfe!“, „Holz!“. Jeder Ruf erscheint als Text mit dem Namen des Spielers und als kurzer Laut. Draußen kommen Markierungen und Rufe nur über das Funkgerät an, wie die Stimme. Regeln im Detail: Dokumente zu Nachteinsatz und Sprachchat. Läuft über den Host wie jede Spielhandlung (Architektur-Regel 2). |
| Getrennte Lautstärken | Basic | Gesamt, Musik, Effekte, Sprachchat, UI | Not Started | 0–100 %, gespeichert je Spieler, im Hauptmenü und im Pausenmenü. Heute gibt es nur einen Stummschalter (`AudioMuteToggle`). |
| Lautstärke je Mitspieler | ergänzt | Sprachchat | Not Started | einzelne Mitspieler leiser stellen oder stummschalten |
| Mono-Ton | Comprehensive (vorgezogen) | gesamte Ausgabe | Not Started | siehe Accessibility Tier Definition. Beide Seiten gleich laut, nicht einfach addiert. |
| Keine Info nur in hohen Tönen | Standard | alle spielwichtigen Signale | Not Started | Kein wichtiges Signal nur über 4 kHz, weil Hörgeräte hohe Töne oft filtern. Das Rauschen des Funkgeräts darf Rufe und Stimme nicht ganz verdecken. |
| Richtungsanzeige für Geräusche | Comprehensive (teilweise vorgezogen) | nur der Hilferuf des Vermissten | Not Started | Pfeil am Bildschirmrand in Richtung des Rufs, siehe Tabelle unten |
| Untertitel für Sprache | Basic | — | N/A | Es gibt keine Sprachausgabe. Der Sprachchat der Spieler bekommt keine Untertitel (Known Intentional Limitations). Ersatz sind Markieren und Rufe. |
| Einblendungen für alle Geräusche | Comprehensive | — | Not Planned | nur für die Geräusche mit „Ja“ in der Tabelle unten |

### Gameplay-Critical SFX Audit

| Sound Effect | What It Communicates | Visual Backup | Caption Required | Status |
|---|---|---|---|---|
| Hilferuf des Vermissten | Richtung, in der er liegt | keiner, der Vermisste ist im Sturm nicht zu sehen | Ja: „[Hilferuf]“ mit Pfeil am Bildschirmrand in seine Richtung | Not Started |
| Ofen knistert leiser | Feuer geht bald aus | Flamme wird kleiner, Wärme im HUD | Nein | Not Started |
| Murren | Gast mault | Gesicht, verschränkte Arme | Nein | Not Started |
| „Aua“ | Gast wurde getroffen | Gast zuckt zusammen | Nein | Not Started |
| Münzklimpern | Trinkgeld | „+6“ über dem Gast | Nein | Not Started |
| Wind wird stärker | Phase wechselt | Phase und Vorwarnung im HUD | Nein | Not Started |
| Ereignisse (Fenster schlägt auf, Strom fällt aus, …) | Handlung nötig | Regel: jedes Ereignis braucht ein sichtbares Zeichen | wird im Dokument zu den Ereignissen geprüft | Not Started |
| Seilbahn: Kiste kommt an | Kiste abholen | Regel: Lampe an der Station oder Meldung | wird im Dokument zur Seilbahn geprüft | Not Started |
| Neuer Gast an der Tür | Platz und Bedürfnis | Gast ist sichtbar, Zähler im HUD | Nein | Not Started |
| Funkgerät (Stimme der Mitspieler) | Absprache draußen | Rufe als Text | nicht möglich (Known Intentional Limitations) | Not Started |

---

## Platform Accessibility API Integration

| Platform | API / Standard | Features Planned | Status | Notes |
|---|---|---|---|---|
| Steam (PC) | Steam Input | Gamepad-Umbelegung über Steam, zusätzlich zur eigenen | Not Started | ersetzt nicht die Umbelegung im Spiel für Tastatur und Maus (Motor Accessibility) |
| PC (Bildschirmleser) | Windows-Sprachausgabe, NVDA | keine | Not Planned | Stufe Comprehensive. Laut `docs/engine-reference/unity/VERSION.md` brachte Unity 6.3 LTS Bildschirmleser-Unterstützung für den Desktop. Ob und wie das in 6.6 mit der UI dieses Spiels funktioniert, ist dort nicht belegt (NOT SOURCEABLE). Siehe Open Questions. |
| Xbox, PlayStation, iOS, Android | — | — | N/A | keine Konsolen, kein Handy (CLAUDE.md, „Bewusst NICHT bauen“) |

---

## Per-Feature Accessibility Matrix

> Neue Systeme in `systems-index.md` bekommen hier eine Zeile. **Addressed:** „Geplant“ heißt, jede Hürde
> hat eine Anforderung in diesem Dokument; gebaut ist davon noch nichts. „Teilweise“ heißt, ein Teil steht
> unter Known Intentional Limitations. „Offen“ heißt, das System hat noch kein Design-Dokument oder eine Frage
> ist ungeklärt.

| System | Visual Concerns | Motor Concerns | Cognitive Concerns | Auditory Concerns | Addressed | Notes |
|---|---|---|---|---|---|---|
| 1 Netzwerk-Sitzung | Fehlermeldungen lesbar | — | Meldungen verschwinden nicht von selbst | — | Geplant | Lesezeit (Cognitive) |
| 2 Bewegung und Kamera | Übelkeit durch Wippen und Stöße | Maus und Tastatur gleichzeitig | — | Schritte nur Atmosphäre | Geplant | Bewegung reduzieren, Sichtfeld, Empfindlichkeit, Auto-Laufen |
| 3 Tragen und Werfen | Kamerastöße mit Last | Laufen und Umschauen mit Last | — | Aufprall nur Rückmeldung | Geplant | E ist schon Umschalten, Werfen nie nötig |
| 4 Tragen zu zweit | sehen, wohin der Partner zieht | braucht beide Hände | Absprache nötig | Absprache meist per Stimme | Teilweise | Markieren und Rufe ja; Einhand-Modus nein (Limitation) |
| 5 Holzofen und Wärme | Wärme eventuell über Farbe | — | Brennzeit im Kopf behalten | Zustand am Knistern hörbar | Geplant | Flamme wird kleiner, Wärme im HUD mit Zahl oder Balken |
| 6 Geräusche | — | — | — | nur ein Stummschalter | Geplant | Lautstärken, Mono, keine hohen Töne als einziges Signal |
| 7 Gegenstandsarten | warm/kalt erkennen | — | viele Dinge auseinanderhalten | — | Geplant | Name im Hinweis, warm über Dampf statt Farbe |
| 8 Interaktion | Hinweis über Schnee und Nacht | Halte-Aktionen | Grund, warum es nicht geht | — | Geplant | Umschalten, Kontrast, Grund steht im Hinweis |
| 9 Saison-Ablauf | Phase und Uhr | — | Zeitdruck; Pause nur solo | Wind kündigt Phase an | Teilweise | „Gemütlich“, Vorwarnung im HUD; keine Pause im Koop (Limitation) |
| 10 Steam-Lobby und Einladen | Menüs, Namensschilder | Menüs mit Gamepad | Meldungen bleiben stehen | — | Geplant | |
| 11 Gäste und Bedürfnisse | Symbol, Ring, Stimmung, Typfarben | — | je Gast Bedürfnis, Geduld, Stimmung; volle Stube heißt mehr als 4 Dinge gleichzeitig | Murren, „Aua“ | Geplant | **Belastung über 4 bei voller Stube**, ausgeglichen durch die Liste im HUD und „Gemütlich“ |
| 12 Kochen und Gerichte | Garzustand eventuell nur über Farbe | eventuell Halte-Schritte | 2–4 Schritte je Gericht merken | „Fertig“-Ton | Offen | noch kein GDD. Es gelten „nie nur Farbe“, „Halten als Umschalten“, Rezept nachlesbar, jedes Signal sichtbar |
| 13 Lager und Vorräte | Kisteninhalt | — | Bestand merken | — | Offen | noch kein GDD. Inhalt steht im Hinweis (`gegenstandsarten.md`) |
| 14 Holz hacken | — | Halten | — | — | Geplant | Umschalten |
| 15 Betten und Erste Hilfe | rotes Kreuz | Halten | — | — | Geplant | Umschalten, Kreuzform |
| 16 Materialseilbahn | schaukelnde Kiste | Kurbel halten | — | Ankunft nur hörbar | Offen | Umschalten ja; sichtbares Zeichen bei der Ankunft im GDD klären |
| 17 Wetter und Eskalation | Sicht absichtlich schlecht, Blitze | — | steigender Druck | Wind | Offen | Blitze höchstens 3-mal pro Sekunde; Sichtweite im Spieltest prüfen |
| 18 Ereignisse (mit Strom und Licht) | Dunkelheit, Flackern | eventuell Halten (Generator) | — | Ereignisse oft nur hörbar | Offen | Flackerregel, „jedes Ereignis sichtbar“, Umschalten |
| 19 Nachteinsatz | dunkel, Schneesturm, Stirnlampe | Trage zu zweit, langer Weg | Orientierung | Funk, Hilferuf | Teilweise | Rufe, Richtungspfeil, Hüttenlichter, Auto-Laufen, Helligkeit; Stimme ohne Untertitel (Limitation) |
| 20 Solo-Hüttenhund | noch unklar | noch unklar | noch unklar | noch unklar | Offen | beim Design prüfen |
| 21 Figuren-Animation und Ragdoll | Kamera beim Hinfallen | — | — | — | Geplant | Bewegung reduzieren gilt auch hier |
| 22 Bewertung | Edelweiß als Zahl und Symbol | — | Gründe der Bewertung verständlich | — | Offen | |
| 23 Hüttenkasse und Upgrades | Menütexte | — | Upgrades vergleichen | — | Offen | |
| 24 Sprachchat und Funkgerät | — | Sprechtaste wäre eine Halte-Taste | — | Hauptproblem für Gehörlose und Spieler ohne Mikro | Geplant | Rufe, Lautstärke je Mitspieler, Sprechtaste auch als Umschalten oder offenes Mikro |
| 25 HUD | alles Sichtbare | — | was gerade zu tun ist | — | Geplant | über `design/ux/hud.md` |
| 26 Menüs | Textgröße, Kontrast | Gamepad, Umbelegung | Hilfeseite | Lautstärken | Geplant | |
| 27 Spielstand speichern | — | — | Einstellungen zur Barrierefreiheit bleiben gespeichert | — | Geplant | |
| 28 Kraxe | — | noch unklar | — | — | Offen | Alpha, beim Design prüfen |
| 29 Ausdauer und Rutschen auf Eis | Eis nicht nur an der Farbe erkennbar | Ausdauer eventuell Halten | — | — | Offen | Alpha, beim Design prüfen |
| 30 Stolpern und Mitreißen | Kamera beim Stolpern | — | — | — | Offen | Alpha; Bewegung reduzieren muss greifen |
| 31 Musik | — | — | — | eigene Lautstärke | Geplant | |
| 32 Tutorial | — | — | Hinweise nachlesbar | — | Geplant | Hilfeseite |
| 33 Weitere Hütten | — | — | — | — | N/A | Full Vision |
| 34 Kosmetik | Spieler nicht nur an der Farbe unterscheiden | — | — | — | Geplant | Namensschild deckt es ab |

---

## Accessibility Test Plan

> Zuständig ist bei allen Tests der Entwickler; Claude hilft mit Bildschirmfotos und Messungen.

| Feature | Test Method | Test Cases | Pass Criteria | Responsible | Status |
|---|---|---|---|---|---|
| Ohne Ton spielen | manuell | eine ganze Saison mit Ton aus | alles Spielwichtige ist sichtbar | Entwickler | Not Started |
| Ohne Stimme | manuell, zu zweit | Nachteinsatz nur mit Markieren und Rufen | Vermisster gefunden und zurückgebracht | Entwickler | Not Started |
| Text-Kontrast | Bildschirmfotos, Kontrast messen | Hinweis am Fadenkreuz über Schnee und bei Nacht, alle Menüs | Text 4,5:1, große Schrift 3:1 | Entwickler | Not Started |
| Farbschwäche | Farbschwäche-Simulator auf Bildschirmfotos | Stube mit allen Ring- und Stimmungszuständen, mit und ohne Farbschwäche-Modus | kein Zustand nur an der Farbe erkennbar | Entwickler | Not Started |
| Tastenbelegung | manuell | alles umbelegen, eine Saison spielen, neu starten | alles geht, Belegung bleibt gespeichert, Doppelbelegung wird gemeldet | Entwickler | Not Started |
| Halten als Umschalten | manuell | jede Halte-Aktion im Umschalt-Modus | nichts verlangt noch Dauerhalten | Entwickler | Not Started |
| Bewegung reduzieren | manuell | gehen, tragen, halten, hinfallen | kein Wippen, keine Stöße | Entwickler | Not Started |
| Flackern | Aufnahme, Bilder zählen | Stromausfall, Gewitter | höchstens 3 Blitze pro Sekunde | Entwickler | Not Started |
| Mono | manuell, ein Ohrhörer | Nachteinsatz | Hilferuf über den Pfeil findbar | Entwickler | Not Started |
| Gemütlich | Freundestest | eine Saison „Gemütlich“ | spürbar weniger Druck, trotzdem Chaos | Entwickler | Not Started |
| Mit Betroffenen | Freundestest | fragen, ob jemand eine Farbschwäche hat oder schlecht hört | sie spielen ohne Nachfragen zu Farben oder Geräuschen | Entwickler | Not Started |

---

## Known Intentional Limitations

| Feature | Tier Required | Why Not Included | Risk / Impact | Mitigation |
|---|---|---|---|---|
| Pause im Koop | Basic | Alle spielen dieselbe Saison. Hält einer an, steht sie für alle still (`saison-ablauf.md`, Regel 11). | Spieler, die kurz weg müssen oder eine Pause brauchen | Menü öffnen geht jederzeit, „Gemütlich“, der Host kann die Saison abbrechen (mit Rückfrage); solo gibt es Pause |
| Untertitel für den Sprachchat | Standard (Untertitel) | Spracherkennung ist teuer und beim Datenschutz heikel | Gehörlose und schwerhörige Spieler verstehen ihre Mitspieler nicht | Markieren und Rufe als Text mit Name, Steam-Chat |
| Einhand-Modus | — | Tragen braucht Laufen und Umschauen gleichzeitig | Spieler mit nur einer nutzbaren Hand | freie Tastenbelegung, Auto-Laufen, Halten als Umschalten |
| Wegpunkt zum Vermissten | Standard (Weg finden) | Die Suche ist die Aufgabe des Nachteinsatzes | Spieler mit Orientierungsschwierigkeiten | Richtungspfeil beim Hilferuf, Markierungen der Mitspieler, Hüttenlichter |
| Bildschirmleser für Menüs | Comprehensive | über der gewählten Stufe; ohne Sehen ist ein 3D-Spiel in der Ich-Perspektive ohnehin kaum spielbar | blinde Spieler | offene Frage, ob Unity 6.6 das günstig mitbringt (Open Questions) |
| Einblendungen für alle Geräusche, HUD verschieben, hoher Kontrast | Comprehensive/Exemplary | über der gewählten Stufe | gehörlose und sehbehinderte Spieler | Alles Spielwichtige hat einen sichtbaren Ersatz (SFX Audit); UI-Größe 75–150 % |

---

## Audit History

| Date | Auditor | Type | Scope | Findings Summary | Status |
|---|---|---|---|---|---|
| — | — | — | — | Noch keine Prüfung. Geplant: `/ux-review design/accessibility-requirements.md`, danach vor dem Freundestest mit einer ganzen Saison (Ende Vertical Slice) eine interne Prüfung nach dem Accessibility Test Plan. | — |

---

## External Resources

| Resource | URL | Relevance |
|---|---|---|
| WCAG 2.1 | https://www.w3.org/TR/WCAG21/ | Kontrast, Textgröße, Blitze (2.3.1) |
| Game Accessibility Guidelines | https://gameaccessibilityguidelines.com | Checkliste speziell für Spiele, nach Aufwand sortiert |
| AbleGamers Player Panel | https://ablegamers.org/player-panel/ | Tests mit Spielern mit Behinderung |
| Colour Blindness Simulator (Coblis) | https://www.color-blindness.com/coblis-color-blindness-simulator/ | Bildschirmfotos auf Farbschwäche prüfen |
| Accessible Games Database | https://accessible.games | Beispiele aus anderen Spielen |
| CVAA | https://www.fcc.gov/consumers/guides/21st-century-communications-and-video-accessibility-act-cvaa | US-Gesetz zu Kommunikationsfunktionen wie Sprachchat (siehe Open Questions) |

---

## Open Questions

| Question | Owner | Deadline | Resolution |
|---|---|---|---|
| Es gibt kein Dokument zum Spielerlebnis über die Zeit. `design/player-journey.md` aus der Vorlage `.claude/docs/templates/player-journey.md` anlegen. | Entwickler + ux-designer | vor `/ux-design hud` | offen |
| Bildschirmleser in Unity 6.6: Lässt sich die Desktop-Unterstützung aus 6.3 mit der UI dieses Spiels günstig nutzen? | Claude (Recherche) | vor dem Bau der Menüs | offen |
| Unity-API für Tastenbelegung (Input System) und Mono-Ton in `docs/engine-reference/unity/` prüfen | Claude | vor dem Bau der Einstellungen | offen |
| Muss der Sprachchat nach US-Recht (CVAA) barrierefrei sein? Das Dokument trifft dazu keine Rechtsaussage. | Entwickler | vor dem Einbau des Sprachchats | offen |
| Wie stark wirkt „Gemütlich“ (Geduld der Gäste, Häufigkeit der Ereignisse)? | Entwickler (Spieltest) | beim Bau von Gästen und Ereignissen | offen |
| Endgültige Liste der Rufe, und wie Rufe und Markierungen über das Funkgerät laufen | GDDs Nachteinsatz und Sprachchat | bei deren Design | offen |
| Sprechtaste oder offenes Mikro als Voreinstellung? | GDD Sprachchat | bei dessen Design | offen |
| Wärme der Stube im HUD als Zahl oder Balken? | `design/ux/hud.md` | bei dessen Design | offen |
| Standardwert für das Sichtfeld | UX-Dokument der Menüs | bei dessen Design | offen |
