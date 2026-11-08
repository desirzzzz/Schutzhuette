# Steam-Lobby und Einladen

> **Status**: Approved (Design-Review 2026-11-08, siehe `design/gdd/reviews/steam-lobby-review-log.md`)
> **Author**: Entwickler + Claude
> **Last Updated**: 2026-11-08
> **Last Verified**: 2026-11-08
> **Implements Pillar**: Teamwork unter Druck (Freunde finden ohne Hürden zusammen)

## Summary

Steam-Lobby und Einladen bringt Freunde ohne IP-Adresse und Portfreigabe zusammen: Der Host startet über
Steam eine private Lobby (bis zur Steam-Seite mit der Test-App 480), lädt Freunde ein, und ein Klick auf die
Einladung bringt sie in die Hütte. Es gelten dieselben Regeln wie beim IP-Weg (Version, höchstens 4
Spieler, Beitritt nur zwischen den Saisons), dazu eine Zeitgrenze von 20 s und Steam-Namen über den
Figuren. Es existiert, damit der Freundestest einfach wird und fremde Playtester in der Demo-Phase
überhaupt mitspielen können.

> **Quick reference** — Layer: `Core` · Priority: `Vertical Slice` · Key deps: `Netzwerk-Sitzung`

## Overview

**Steam-Lobby und Einladen** bringt Freunde ohne Umwege in dieselbe Hütte. Der Host startet ein Spiel und
lädt seine Freunde über die Steam-Freundesliste ein. Ein Klick auf die Einladung, und man steht mit in der
Hütte. Es braucht keine IP-Adresse, keine Portfreigabe und keine Firewall-Regel. Genau diese Hürden machen
den Freundestest heute mühsam, und für fremde Playtester in der Demo-Phase wären sie unüberwindbar. Das
Gefühl soll sein: „Hey, komm rein“, und schon ist man zusammen.

Technisch besteht das System aus zwei Teilen:

- **Steam-Lobby:** wer gerade zusammen spielt und wer eingeladen ist
- **Steam-Transport:** wie Netcode for GameObjects die Daten über Steams Netzwerk schickt statt direkt über
  IP und Port

Bis zur Steam-Seite läuft alles über Valves **kostenlose Test-App 480 („Spacewar“)**. Erst dann bekommt das
Spiel seine eigene App-ID. Das Beitreten über IP bleibt als Ausweg erhalten, zum Beispiel wenn Steam nicht
läuft oder zum Testen mit zwei Fenstern.

Für alle Beitritte gelten dieselben Regeln wie heute: dieselbe Version, höchstens 4 Spieler, Beitritt nur
zwischen den Saisons. Welche Bibliothek den Steam-Transport für Netcode for GameObjects liefert und ob sie
mit Unity 6.6 läuft, legt eine Architektur-Entscheidung fest, bevor gebaut wird (ADR, noch nicht
angelegt).

## Detailed Design

### Core Rules

1. **Zwei Wege zum Hosten.** Im Menü gibt es „Spiel starten“ über Steam, das ist der Standard, wenn Steam
   läuft. Daneben gibt es „Über IP hosten“ als Ausweg. Ein Host nutzt genau einen Weg: Wer über Steam
   hostet, ist nicht über IP erreichbar, und umgekehrt.
2. **Steam-Anbindung:** Beim Start meldet sich das Spiel bei Steam an. Bis zur Steam-Seite nutzt es die
   App-ID 480. Die App-ID steht in einer Einstellung, damit der Wechsel auf die eigene ID in Schritt 14
   ohne Code-Änderung geht. Läuft Steam nicht, ist der Steam-Weg ausgegraut, mit dem Hinweis „Steam läuft
   nicht, über IP spielen“.
3. **Lobby:** Wer über Steam hostet, legt eine **private** Steam-Lobby an, sichtbar nur mit Einladung und
   mit `lobby_slots` Plätzen (höchstens 4). Die Lobby trägt zwei Daten: `game=schutzhuette` und
   `version=<Spielversion>`. So
   landet man nie in einer fremden Spacewar-Lobby, und eine falsche Version fällt früh auf. Der Host
   besitzt die Lobby.
4. **Einladen:** Jeder Spieler in der Hütte kann Freunde einladen, über das Steam-Overlay (Shift+Tab) oder
   über den Knopf „Freunde einladen“ im Menü. Ohne Einladung tritt niemand bei, auch keine Freunde.
5. **Beitreten:**
   - Der Freund nimmt die Einladung an, im Overlay oder im Steam-Chat. **Bis zur eigenen App-ID startet er
     Schutzhütte vorher selbst**: Mit der Test-App 480 würde Steam beim Annehmen sonst Spacewar starten.
     Ab der eigenen App-ID startet Steam das Spiel selbst und übergibt die Lobby (`+connect_lobby`); das
     wird in Schritt 14 geprüft.
   - Das Spiel tritt der Lobby bei und verbindet sich über den Steam-Transport mit dem Host.
   - Dann prüft der Host wie heute: dieselbe Version, ein freier Platz, keine laufende Saison.
   - Wird der Beitritt abgelehnt, sieht der Freund den Grund und verlässt die Lobby.
6. **Während einer Saison:** Die Lobby bleibt offen, Einladungen gehen weiter raus. Ein Beitritt wird aber
   von der Freigabe abgelehnt, mit „Gerade läuft eine Saison. Bitte nach der Bewertung erneut beitreten.“
   Danach verlässt der Freund die Lobby. Die Einladung bleibt gültig, er kann sie nach der Bewertung
   erneut annehmen.
7. **Kein Spielstatus:** Die Steam-Freundesliste zeigt nur den Spielnamen, bis zur eigenen App-ID also
   „Spacewar“. Phase und Spielerzahl erscheinen dort nicht.
8. **Namen über den Figuren:** Jeder Spieler schickt **nach** dem Beitritt per RPC seinen Steam-Namen an den
   Host (höchstens 128 Byte, nicht in den Verbindungsdaten: Die fassen nur 64 Byte und gehören der
   Versionsprüfung). Der Host berechnet `display_name` und verteilt ihn an alle, und über jeder Figur steht ein kleines Namensschild. Wer über IP beitritt, heißt
   „Spieler 1“ bis „Spieler 4“, nach seinem Platz. Die Namen dienen nur der Anzeige.
9. **IP-Weg:** Er bleibt wie heute (Adresse, Port 7777, Versionsprüfung, dieselben Regeln). Neu ist nur die
   Meldung bei Zeitüberschreitung: „Der Host antwortet nicht.“ statt „Verbindung getrennt.“
10. **Ende:** Beendet der Host die Sitzung, endet die Lobby für alle. Wer geht, verlässt die Lobby, sein
    Platz wird frei.

### States and Transitions

Host:

| Zustand | Bedeutung | Übergänge |
| --- | --- | --- |
| Kein Steam | Steam läuft nicht oder die Anmeldung scheitert | → Hostet über IP (Ausweg) |
| Bereit | Bei Steam angemeldet, Menü offen | → Hostet über Steam („Spiel starten“), → Hostet über IP |
| Hostet über Steam | Private Lobby offen, Einladungen möglich | → Bereit (Sitzung beendet, Lobby geschlossen) |
| Hostet über IP | Wie heute, ohne Lobby | → Bereit (Sitzung beendet) |

Beitretender:

| Zustand | Bedeutung | Übergänge |
| --- | --- | --- |
| Eingeladen | Einladung liegt vor | → Tritt bei (angenommen) |
| Tritt bei | Lobby betreten, Verbindung über den Steam-Transport | → Im Spiel (Host nimmt an), → Abgelehnt (Version, voll, Saison läuft, Zeitüberschreitung) |
| Im Spiel | Figur ist erschienen | → Menü (verlassen oder Host beendet) |
| Abgelehnt | Grund wird angezeigt, Lobby verlassen | → Tritt bei (Einladung erneut annehmen) |

### Interactions with Other Systems

| System | Was es liefert oder nutzt |
| --- | --- |
| Netzwerk-Sitzung | Wählt den Transport (Steam oder IP). Die Freigabe (Version, Plätze) bleibt dieselbe und gilt für beide Wege. |
| Saison-Ablauf | Beitrittssperre während der Saison, mit derselben Meldung wie beim IP-Weg |
| Menüs | Wahl zwischen Steam und IP, „Freunde einladen“, Meldungen (Steam läuft nicht, abgelehnt mit Grund) |
| HUD und Figuren | Namensschild über jeder Figur |
| Sprachchat und Funkgerät | Unabhängig von Steam, braucht nur die laufende Sitzung |

> `systems-designer`, `gameplay-programmer` und `engine-programmer` wurden nicht befragt (Lean-Modus). Vor
> dem Bau selbst prüfen.

## Formulas

Werte vorgeschlagen von `systems-designer` (2026-11-08), gegen die Netzwerkwerte in `Prototype.unity`
(`ClientConnectionBufferTimeout` 10 s, `ConnectTimeoutMS` 1000, `MaxConnectAttempts` 60) und
`PlayerSpawnApproval` geprüft, vom Entwickler freigegeben.

### 1. Zeitgrenze beim Beitreten (nur Steam)

Die Formel `join_timeout` ist definiert als:

`join_timeout = t_connect + t_load + n_rt * rtt_max`

Abbruchregel: `join_failed = (t_now - t_join_start >= join_timeout) AND NOT player_spawned`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| t_connect | float | 5–15 s (Start 10) | constant | Lobby betreten und Steam-Verbindung über das Relay aufbauen (typisch 1–3 s) |
| t_load | float | 3–15 s (Start 7) | constant | Hütten-Szene laden, Objekte übernehmen |
| n_rt | int | 10 | constant | Hin- und Rückwege bis zur Figur (Version, Freigabe, Szenenabgleich, Spawn), mit Reserve |
| rtt_max | float | 0,1–0,5 s (Start 0,3) | constant | Höchster eingeplanter Ping, das Dreifache der getesteten 100 ms |
| t_join_start | float | ≥ 0 s | calculated | Echtzeit (nicht `game_time`) beim Absenden der Lobby-Anfrage. Ein Spielstart durch Steam beim Annehmen der Einladung zählt nicht mit; die Zeit beginnt erst nach der Steam-Anmeldung. |
| t_now | float | ≥ t_join_start | calculated | Aktuelle Echtzeit |
| player_spawned | bool | ja/nein | calculated | Die eigene Figur ist erschienen |
| join_timeout | float | 11–60 s (Start 20) | data file | **Ein einzelner Wert** `joinTimeoutSeconds` in den Netzwerk-Einstellungen (im Inspector 11–60). Die Formel ist seine Begründung und steht im Tooltip; das Spiel rechnet sie nicht. |

**Output Range:** Begrenzt auf 11–60 s. Die Untergrenze liegt über dem Puffer des Hosts
(`ClientConnectionBufferTimeout` 10 s), damit dessen Ablehnungsgrund zuerst ankommt. Die Obergrenze ist der
Zeitpunkt, an dem der Transport aufgibt (60 Versuche × 1 s). **Der IP-Weg nutzt diese Zeitgrenze nicht**,
dort bleibt es bei den 60 s des Transports (Entscheidung des Entwicklers).

**Example:** 10 + 7 + 10 × 0,3 = 20 s. Ein typischer Beitritt mit 100 ms Ping dauert etwa 2 + 4 + 10 × 0,1
= 7 s, die Reserve ist also etwa dreifach. Steht nach 20,0 s keine Figur, wechselt der Spieler zu
„Abgelehnt“ mit „Zeitüberschreitung: Der Host antwortet nicht.“ und verlässt die Lobby.

### 2. Lobby-Plätze

Die Formel `lobby_slots` ist definiert als:

`lobby_slots = count{ i in [0, min(n_spawn, max_players)) : spawn_i gesetzt }`

Die Steam-Lobby bekommt genau `lobby_slots` Plätze; der Host zählt mit, wie in der Freigabe.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| n_spawn | int | 0–8 | data file | Länge der Spawnpunkt-Liste in der Szene |
| max_players | int | 1–4 | data file | Höchstzahl Spieler (`_maxPlayers`) |
| spawn_i | bool | gesetzt/leer | data file | Ob Eintrag i der Liste gesetzt ist |
| lobby_slots | int | 0–4 | calculated | Plätze einschließlich Host; gilt für Lobby und Freigabe gleich |

**Output Range:** Höchstens 4. Bei 0 wird das Hosten verweigert mit „Konfigurationsfehler: Keine
Spawnpunkte gesetzt.“ (sonst hätte der Host selbst keinen Platz). Bei 1 passt nur der Host hinein, Einladungen scheitern mit „voll“. Lobby und
Freigabe nutzen **dieselbe Funktion**; sie ersetzt das heutige `SlotCount`, das leere Einträge mitzählt,
während `FindFreeSlot` sie überspringt.

**Example:** 4 Punkte und `max_players` 4 ergeben 4. Ist Punkt 3 leer, ergeben sie 3: Die Freigabe vergibt
die Plätze 0, 1 und 3, und den vierten Freund weist schon Steam ab; er sieht „Die Hütte ist voll
(3 Spieler).“

### 3. Angezeigter Name

Die Formel `display_name` ist definiert als:

`display_name = len(s) >= 1 ? trunc(s, name_max) : "Spieler " + (slot + 1)`, mit `s = clean(raw_name)` und
`trunc(s, n) = len(s) <= n ? s : left(s, n - 1) + "…"`, zusätzlich höchstens `name_bytes_max` Byte in UTF-8.

- `clean` entfernt Steuerzeichen und unsichtbare Zeichen (Unicode Cc und Cf, zum Beispiel Zero-Width) und
  **einzeln** jedes Zeichen, das die Namensschrift nicht darstellen kann. Leerraum am Rand fällt weg,
  mehrere Leerzeichen innen werden zu einem.
- `len` zählt Textelemente (Graphem-Cluster), nicht Bytes; **kein Textelement wird zerschnitten** (auch
  „e“ + Akzent nicht). Das Verbindungszeichen U+200D (Zero Width Joiner) bleibt stehen, wenn es zwischen
  zwei darstellbaren Zeichen steht, so bleibt 👨‍👩‍👧 ein Element.
- Reihenfolge: erst `clean`, dann `trunc`; danach wird so lange das letzte Textelement vor „…“ entfernt,
  bis alles höchstens `name_bytes_max` Byte hat. „…“ steht auch dann, wenn nur die Bytegrenze kürzt.
- Rich Text ist auf dem Schild aus (`<b>` erscheint als Text).
- Der Host berechnet den Namen und verteilt das Ergebnis (Regel 8).

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| raw_name | string | 0–32 Zeichen | calculated | Steam-Name, den der Client sendet; beim IP-Weg leer |
| slot | int | 0–3 | calculated | Platz aus der Freigabe |
| name_max | int | 8–24 (Start 16) | data file | Höchstlänge auf dem Schild, etwa Figurenbreite |
| name_bytes_max | int | 61 | constant | Nutzbare Größe einer `FixedString64Bytes`; „…“ kostet 3 Byte |
| display_name | string | 1 bis `name_max` Textelemente | calculated | Text auf dem Namensschild |

**Output Range:** Nie leer, immer 1 bis `name_max` Textelemente. Ersatzname „Spieler 1“ bis „Spieler 4“.

**Example:** „␣␣Sepp der Hüttenwirt aus Tirol“ wird zu „Sepp der Hüttenwirt aus Tirol“ (29) und dann zu
„Sepp der Hütten…“ (16). Ein Name nur aus Zero-Width-Zeichen auf Platz 2 wird zu „Spieler 3“. Ein
IP-Beitritt auf Platz 1 heißt „Spieler 2“.

## Edge Cases

**Steam selbst**

- **Wenn Steam beim Spielstart nicht läuft:** Der Steam-Weg ist ausgegraut. Beim nächsten Öffnen des
  Hauptmenüs versucht das Spiel die Anmeldung erneut; Steam lässt sich also nachträglich starten, ohne das
  Spiel neu zu starten.
- **Wenn die Steam-Verbindung mitten im Spiel abreißt** (Steam stürzt ab, Internet weg): Bei den Clients
  endet die Sitzung mit „Steam-Verbindung verloren“. Der Host spielt solo weiter.
- **Wenn das Steam-Overlay ausgeschaltet ist:** „Freunde einladen“ zeigt „Das Steam-Overlay ist aus.
  Schalte es in Steam unter Einstellungen → Im Spiel ein.“

**Einladungen**

- **Wenn die Einladung zu einer Lobby ohne die Kennung `schutzhuette` gehört** (ein anderes Spacewar-Spiel):
  Sie wird ignoriert, mit „Diese Einladung gehört nicht zu Schutzhütte.“
- **Wenn die Version der Lobby nicht passt:** Der Freund verbindet sich gar nicht erst. Er prüft die
  Version anhand der Lobby-Daten vor der Verbindung und sieht sofort die Meldung zur Version mit Hinweis auf
  den Update-Knopf.
- **Wenn der Host nicht mehr da ist, die Einladung aber angenommen wird:** „Die Einladung ist nicht mehr
  gültig.“
- **Wenn die Lobby voll ist:** Steam lehnt ab, der Freund sieht „Die Hütte ist voll (N Spieler).“ (derselbe
  Text wie bei der Freigabe, N = `lobby_slots`). Nehmen zwei
  gleichzeitig den letzten Platz an, kommt einer rein, der andere bekommt „voll“; die Freigabe des Hosts
  entscheidet zuletzt.
- **Wenn man eine Einladung annimmt, während man selbst in einem Spiel ist:** Rückfrage „Aktuelles Spiel
  verlassen und beitreten?“. Ist man selbst Host, beendet „Ja“ die eigene Sitzung für alle.

**Beitreten**

- **Wenn der Beitritt abgelehnt wird oder die Zeitgrenze (20 s) abläuft:** Der Freund verlässt die Lobby
  sofort, damit er keinen Platz blockiert. Er kann die Einladung erneut annehmen.
- **Wenn ein langsamer PC beim Laden länger als 20 s braucht:** Der Beitritt bricht ab, ein neuer Versuch
  ist möglich. Die Dauer jedes Beitritts wird protokolliert, um den Wert im Test zu prüfen.
- **Wenn jemand über IP einem Host beitreten will, der über Steam hostet:** Es klappt nicht (ein Host nutzt
  nur einen Weg). Nach 60 s kommt „Der Host antwortet nicht.“ Im IP-Menü steht der Hinweis: „Hostet dein
  Freund über Steam? Dann lass dich einladen.“

**Namen**

- **Wenn sich ein Steam-Name während der Sitzung ändert:** Das Schild bleibt bis zum nächsten Beitritt beim
  alten Namen.
- **Wenn zwei Spieler denselben Namen haben:** Beide Schilder zeigen ihn so, ohne Nummer.

## Dependencies

**Hängt ab von:**

| System | Art | Schnittstelle |
| --- | --- | --- |
| Netzwerk-Sitzung | hart | Transport wählen (Steam oder IP), Freigabe des Hosts (Version über `GameVersion`, Plätze über `lobby_slots`), Spawn |
| Facepunch.Steamworks und ein Steam-Transport für NGO | hart, extern | Steam-Anmeldung, Lobby, Einladungen, Namen, Datenverkehr über Steam. **Neue Pakete**: Welche, entscheidet die ADR; installiert wird erst nach dem OK des Entwicklers (CLAUDE.md). |
| Saison-Ablauf und Spielphasen | weich | Die Beitrittssperre während der Saison kommt aus der gemeinsamen Freigabe, mit derselben Meldung (`design/gdd/saison-ablauf.md`) |

**Wird gebraucht von:**

| System | Art | Schnittstelle | Stand |
| --- | --- | --- | --- |
| Menüs | hart | Wahl zwischen Steam und IP, „Freunde einladen“, Rückfrage beim Wechsel ins andere Spiel, Meldungen (Steam läuft nicht, voll, Version, Zeitüberschreitung, ungültige Einladung) | kein Dokument |
| HUD | weich | Namensschild über jeder Figur (`display_name`) | kein Dokument |

**Beidseitig:** Der Saison-Ablauf ist freigegeben und nennt die Beitrittssperre. Menüs und HUD müssen beim
Schreiben „Steam-Lobby“ unter ihren Abhängigkeiten nennen.

## Tuning Knobs

Bei Stufe `standard` nur empfohlen; ergänzt wie beim Saison-Ablauf. Alle Werte liegen in Datenobjekten,
nie im Code.

| Stellschraube | Start | Bereich | Ablage | Wirkung |
|---|---|---|---|---|
| App-ID | 480 | 480 oder die eigene | Netzwerk-Einstellungen | Test-App oder eigenes Spiel. Gewechselt wird in Schritt 14. |
| `joinTimeoutSeconds` | 20 s | 11–60 s | Netzwerk-Einstellungen | Ab wann ein hängender Steam-Beitritt abbricht |
| `name_max` | 16 | 8–24 | Netzwerk-Einstellungen | Länge der Namensschilder |
| `max_players` | 4 | 1–4 | Freigabe (`PlayerSpawnApproval`) | Zusammen mit den Spawnpunkten ergibt das `lobby_slots`. |

Wechselwirkung: `joinTimeoutSeconds` muss über dem Puffer des Hosts (10 s) liegen, sonst kommt dessen
Ablehnungsgrund nicht mehr an.

## Visual/Audio Requirements

- Das Namensschild ist schlicht und gut lesbar: kleine Schrift über dem Kopf, immer zur Kamera gedreht, auf
  Entfernung etwas blasser.
- Beim Beitritt eines Freundes erscheint ein kurzer Hinweis „Sepp ist in der Hütte“, dazu eine Türglocke.
  Beim Gehen kommt „Sepp ist gegangen“.

`art-director` und `audio-director` nicht befragt (Kategorie Core, Lean-Modus).

## Game Feel

- „Hey, komm rein“: Von der Einladung bis zur Figur in der Hütte dauert es typisch etwa 7 s, ohne
  Zwischenschritte.
- Jede Ablehnung nennt ihren Grund, nie nur „Fehler“.
- Der IP-Weg fühlt sich an wie heute.

## UI Requirements

- **Hauptmenü:** „Spiel starten“ (Steam, vorgewählt) und darunter „Über IP spielen“ (Adresse, Hosten,
  Beitreten wie heute). Ohne Steam erscheint der Hinweis, der Steam-Knopf ist ausgegraut.
- **Im Spiel:** „Freunde einladen“ im Menü.
- **Meldungen:** Rückfrage „Aktuelles Spiel verlassen und beitreten?“ und alle Ablehnungstexte aus den
  Grenzfällen.
- **Namensschilder** über den Figuren.

> **UX-Hinweis:** Hauptmenü (Steam oder IP), Einladen, Meldungen und Namensschilder brauchen vor den Epics
> ein UX-Dokument mit `/ux-design`.

## Cross-References

| Verweis | Quelle | Genutzt in |
|---|---|---|
| Versionsprüfung, Meldungstexte | `GameVersion`, `PlayerSpawnApproval` | Regeln 3 und 5, Kriterien 7 und 23 |
| `SlotCount` und `FindFreeSlot` (werden ersetzt bzw. angepasst) | `PlayerSpawnApproval` | Formel 2, Kriterium 33 |
| IP-Weg, Port 7777, Meldung „Verbindung getrennt.“ | `ConnectionHud` | Regel 9 |
| Host-Puffer 10 s, Transport 60 × 1 s | `Prototype.unity` (NetworkManager, Unity Transport) | Formel 1 |
| Beitritt nur zwischen den Saisons | `design/gdd/saison-ablauf.md` | Regel 6 |
| Test-App 480, Wechsel in Schritt 14, Pakete nur nach OK | `production/milestones/vertical-slice.md`, CLAUDE.md | Regel 2, Dependencies |

## Acceptance Criteria

Vorschlag von `qa-lead` (2026-11-08), an die Entscheidungen des Entwicklers angepasst und freigegeben.

**Damit die Tests ohne Steam laufen:** Alle Formeln und Entscheidungen sind reine Funktionen (zum Beispiel
eine statische Klasse `SteamLobbyMath` mit `LobbySlots`, `DisplayName`, `CheckLobby`, `JoinFailed`).
Steam steckt hinter einer Schnittstelle (zum Beispiel `ISteamService`: Anmeldung, Lobby anlegen,
betreten, verlassen, Lobby-Daten, Overlay öffnen und Overlay-Status, eigener Name, Ereignisse
„Einladung angenommen“ und „Steam-Verbindung verloren“); in Tests ersetzt sie ein `FakeSteamService`, der
Aufrufe zählt und Antworten vorgibt. Der Beitritt ist eine C#-Klasse mit `Tick(double realtime)`, ohne
echte Uhr. Ob die Namensschrift ein Zeichen darstellen kann, bekommt `DisplayName` als Funktion
übergeben. Tests unter `Assets/Tests/EditMode/Net/`, Zeiten `double`, Toleranz ±0,01.

**Messweg (gehört in den Bau):** Log-Zeilen
`[Steam] t=<Echtzeit> <init-ok|init-failed|host-refused|lobby-created|lobby-left|invite-overlay|invite-received|join-start|join-ok|join-reject|join-timeout|member-joined|member-left|connection-lost> app= lobby=<letzte 6 Ziffern> members=<n>/<lobby_slots> dur=<s> reason=`
mit den Gründen `no-steam`, `overlay-off`, `foreign`, `version`, `full`, `season-running`, `invalid`,
`timeout`, `host-ended`, `steam-lost`, `config`. Jeder Beitrittsversuch endet mit genau einer Zeile
`join-ok`, `join-reject` oder `join-timeout`, jeweils mit `dur=`. **Keine Steam-Namen im Log**, weil
Logs geteilt werden.

Testarten: **[EditMode]**, **[PlayMode]**, **[Manuell]** (solo mit dem eigenen Steam-Konto),
**[Manuell, optional zwei Fenster]** (nur der IP-Weg über 127.0.0.1), **[Freundestest]** (zwei PCs, zwei
Steam-Konten, nie zwei Fenster).

### A. Formeln

1. **[EditMode]** `join_timeout` als Wert: **GIVEN** `joinTimeoutSeconds` 20 / 70 / 5 im Datenobjekt
   **THEN** gilt 20 / 60 / 11 (begrenzt). Die Begründungsformel ergibt (10, 7, 10, 0,3) → 20,000 und dient
   nur als Rechenbeispiel.
2. **[EditMode + Codeprüfung]** **GIVEN** `join_timeout` 20, `t_join_start` 100,0 **WHEN** `JoinFailed`
   **THEN** `t_now` 119,99 ohne Figur → nein; 120,00 ohne Figur → ja; 125,0 mit Figur → nein.
   *Codeprüfung:* Die Funktion nimmt Echtzeit, `game_time` kommt nicht vor.
3. **[EditMode]** **GIVEN** die Steam-Anmeldung kommt erst bei t = 30,0, die Lobby-Anfrage bei 30,5
   **WHEN** `Tick(50,49)` und `Tick(50,51)` **THEN** erst „Tritt bei“, dann „Abgelehnt“; im Log
   `join-start t=30.500`.
4. **[EditMode]** `LobbySlots` (G = gesetzt, – = leer): [G G G G], 4 → 4; [G G – G], 4 → 3; 8 × G, 4 → 4;
   [G G – G G G G G], 4 → 3; [G G], 4 → 2; [G G G G], 1 → 1; [], 4 → 0; [– – – –], 4 → 0.
5. **[EditMode]** `DisplayName` mit `name_max` 16 und Schrift „alles außer 山“: „␣␣Sepp der Hüttenwirt aus
   Tirol“, Platz 0 → „Sepp der Hütten…“; 16 × „A“ → unverändert; 17 × „A“ → 15 × „A“ + „…“; nur U+200B,
   Platz 2 → „Spieler 3“; leer, Platz 1 → „Spieler 2“; „A“ + U+0007 + „B“ + U+200B + „C“ → „ABC“;
   „Sepp␣␣␣der␣␣␣␣Wirt“ → „Sepp der Wirt“; „Sepp山Wirt“ → „SeppWirt“; „<b>Sepp</b>“ → unverändert
   (11 Textelemente); 14 × „a“ + „e“ mit U+0301 + „bc“ → 14 × „a“ + „e“ mit U+0301 + „…“; „👨‍👩‍👧“
   (Schrift kann es) → ein Element, nicht zerlegt; derselbe Name auf Platz 0 und 1 → zweimal derselbe Text;
   mit `name_max` 8: „Sepp der Wirt“ → „Sepp de…“.
6. **[EditMode]** **GIVEN** 16 Textelemente aus je „e“ + 4 kombinierenden Akzenten (je 9 Byte) **WHEN**
   `DisplayName` **THEN** höchstens 61 Byte, endet auf „…“, kein Element zerschnitten, passt in
   `FixedString64Bytes` (genau 6 Elemente + „…“ = 57 Byte).
7. **[EditMode]** `CheckLobby` bei eigener Version 0.2.0: `game=schutzhuette, version=0.2.0` → in Ordnung;
   keine Daten, `game=spacewar` oder `game=Schutzhuette` → fremd („Diese Einladung gehört nicht zu
   Schutzhütte.“); `version=0.2.1` → Text gleich `GameVersion.MismatchReason("0.2.1", "0.2.0")`;
   `version=0.1.9` → „… Der Host muss aktualisieren.“; „fremd“ wird vor der Version geprüft.

### B. Steam-Anbindung und Hosten (Regeln 1, 2, 3)

8. **[EditMode + Codeprüfung]** **GIVEN** App-ID-Einstellung 480 bzw. 999999 **WHEN** das Spiel startet
   **THEN** wird die Anmeldung genau 1× mit 480 bzw. 999999 aufgerufen. *Codeprüfung:* Die Zahl 480 steht
   in keinem Skript unter `Scripts/`.
9. **[Manuell]** **GIVEN** Steam läuft **WHEN** das Spiel startet **THEN** steht `init-ok app=480` im Log,
   „Spiel starten“ (Steam) ist vorgewählt, die eigene Freundesliste zeigt „Spacewar“.
10. **[PlayMode + Manuell]** **GIVEN** Steam läuft nicht **WHEN** das Hauptmenü öffnet **THEN** ist der
    Steam-Knopf nicht klickbar, darunter steht genau „Steam läuft nicht, über IP spielen“, „Über IP
    hosten“ ist klickbar, im Log `init-failed reason=no-steam`. *Manuell weiter:* Steam starten, über IP
    hosten, trennen → der Steam-Knopf ist klickbar, neue Zeile `init-ok`, ohne Neustart des Spiels.
11. **[EditMode]** **GIVEN** `lobby_slots` 4 **WHEN** „Spiel starten“ (Steam) **THEN** genau 1 Lobby:
    privat, 4 Plätze, `game=schutzhuette`, `version=<GameVersion.Current>`, Besitzer der Host; der
    IP-Transport startet nicht. **WHEN** „Über IP hosten“ **THEN** keine Lobby.
12. **[Manuell]** **GIVEN** Steam läuft **WHEN** „Spiel starten“ (Steam) **THEN** `lobby-created
    members=1/4`; die zurückgelesenen Lobby-Daten zeigen `schutzhuette` und die eigene Version;
    `netstat -an | findstr :7777` zeigt keinen UDP-Eintrag; nach „Trennen“ `lobby-left`.
13. **[Manuell, optional zwei Fenster]** **GIVEN** Fenster A hostet über Steam **WHEN** Fenster B über IP
    127.0.0.1:7777 beitreten will **THEN** entsteht keine Verbindung, nach 60 s (±2 s) zeigt B „Der Host
    antwortet nicht.“, und im IP-Menü steht „Hostet dein Freund über Steam? Dann lass dich einladen.“
14. **[EditMode]** **GIVEN** `lobby_slots` 0 **WHEN** der Host über Steam oder IP startet **THEN** startet
    nichts, keine Lobby, sichtbar „Konfigurationsfehler: Keine Spawnpunkte gesetzt.“, im Log
    `host-refused reason=config`.

### C. Einladen (Regeln 4, 7)

15. **[Manuell]** **GIVEN** der Host hostet über Steam **WHEN** „Freunde einladen“ oder Shift+Tab **THEN**
    öffnet sich das Overlay (beim Knopf direkt der Einladungsdialog); `invite-overlay` hat dieselbe
    Kurz-ID wie `lobby-created`.
16. **[PlayMode + Manuell]** **GIVEN** das Overlay ist aus **WHEN** „Freunde einladen“ **THEN** erscheint
    genau „Das Steam-Overlay ist aus. Schalte es in Steam unter Einstellungen → Im Spiel ein.“, das
    Overlay wird 0× aufgerufen, im Log `invite-overlay reason=overlay-off`.
17. **[Freundestest + Codeprüfung]** **GIVEN** der Host hostet über Steam und hat nicht eingeladen
    **WHEN** der Freund den Host in seiner Freundesliste ansieht **THEN** steht dort nur „Spacewar“, ohne
    Phase und Spielerzahl, ohne „Spiel beitreten“; nach der Einladung kommt sie im Steam-Chat an.
    *Codeprüfung:* Rich Presence wird nirgends aufgerufen.
18. **[Freundestest]** **GIVEN** der Freund ist beigetreten **WHEN** er „Freunde einladen“ drückt **THEN**
    öffnet sich sein Overlay mit derselben Lobby-Kurz-ID. *Ein Beitritt eines Dritten über seine Einladung
    braucht drei Konten und bleibt ungeprüft.*

### D. Beitreten (Regeln 5, 6; `join_timeout`)

19. **[Freundestest]** **GIVEN** Vorbereitung, der Freund hat Schutzhütte schon gestartet **WHEN** er die
    Einladung im Overlay annimmt (zweiter Versuch: im Steam-Chat) **THEN** folgen bei ihm
    `invite-received`, `join-start` und `join-ok dur=<s>` mit dur < 20; der Host loggt `[Spawn] …
    angenommen` und `member-joined members=2/4`. Jede `dur` kommt in den Freundestest-Bericht.
20. **[EditMode]** **GIVEN** ein Beitritt läuft, der Host lehnt bei +12,0 s mit Grund X ab **WHEN** `Tick`
    bis +30 s **THEN** Zustand „Abgelehnt“ mit genau X, die Lobby wird im selben Tick 1× verlassen, der
    Transport 1× beendet, genau 1 `join-reject`, kein `join-timeout`; dieselbe Einladung erneut annehmen →
    „Tritt bei“.
21. **[EditMode]** **GIVEN** ein Beitritt läuft, die Figur erscheint nie **WHEN** `Tick(+19,99)` und
    `Tick(+20,01)` **THEN** erst „Tritt bei“, dann „Abgelehnt“ mit „Zeitüberschreitung: Der Host antwortet
    nicht.“, Lobby 1× verlassen, 1 Zeile `join-timeout dur=20.010`. Erscheint die Figur bei +19,99 →
    `join-ok`.
22. **[Freundestest]** **GIVEN** Testprofil, Nachmittag **WHEN** der Freund die Einladung annimmt **THEN**
    sieht er genau „Gerade läuft eine Saison. Bitte nach der Bewertung erneut beitreten.“, sein Log zeigt
    `join-reject reason=season-running`, der Host loggt binnen 2 s `member-left members=1/4`, ein
    `invite-overlay` des Hosts klappt auch während der Saison; in der Bewertung dieselbe Einladung erneut
    angenommen → `join-ok`.
23. **[EditMode]** **GIVEN** Lobby-Daten mit `version=0.2.1` (eigene 0.2.0) bzw. ohne `game` **WHEN** die
    Einladung angenommen wird **THEN** startet der Transport 0×, eine betretene Lobby wird 1× verlassen,
    Text wie in Kriterium 7, im Log `join-reject reason=version` bzw. `reason=foreign`.
24. **[Freundestest]** **GIVEN** der Host läuft mit `_maxPlayers` = 1 **WHEN** der Freund annimmt **THEN**
    sieht er „Die Hütte ist voll (1 Spieler).“, sein Log zeigt `join-reject reason=full`, kein
    Transportversuch, keine `[Spawn]`-Zeile beim Host.
25. **[EditMode]** **GIVEN** 1 freier Platz **WHEN** die Freigabe zwei Anfragen direkt nacheinander bekommt
    **THEN** wird die erste angenommen, die zweite mit „Die Hütte ist voll (N Spieler).“ abgelehnt.
    **GIVEN** der Fake antwortet beim Lobby-Beitritt „voll“ bzw. „existiert nicht“ **THEN** dieselbe
    Voll-Meldung bzw. „Die Einladung ist nicht mehr gültig.“, Transport 0×.
26. **[Freundestest]** **GIVEN** der Host hat eingeladen und dann die Sitzung beendet **WHEN** der Freund
    annimmt **THEN** „Die Einladung ist nicht mehr gültig.“, Log `join-reject reason=invalid`.
27. **[EditMode + Freundestest]** **GIVEN** man ist selbst Host mit 1 Client bzw. selbst Client **WHEN**
    eine Einladung angenommen wird **THEN** erscheint „Aktuelles Spiel verlassen und beitreten?“; „Nein“ →
    nichts ändert sich; „Ja“ → erst Shutdown 1× (als Host endet die Sitzung des Clients), dann der
    Lobby-Beitritt. *Freundestest:* Der Freund hostet solo, der Entwickler lädt ihn ein, der Freund wählt
    „Ja“ und steht danach in der Hütte des Entwicklers.
28. **[EditMode, erst ab eigener App-ID]** **GIVEN** die Kommandozeile enthält `+connect_lobby <id>`, die
    Anmeldung kommt bei t = 8,0 **WHEN** das Spiel startet **THEN** tritt es nach der Anmeldung genau
    dieser Lobby bei (`join-start t=8.000`). Dazu ein Freundestest mit Kaltstart in Schritt 14.

### E. Namen (Regel 8)

29. **[EditMode + Codeprüfung]** **GIVEN** ein Client schickt nach dem Beitritt per RPC „␣␣Sepp␣␣der␣Wirt“
    bzw. 200 Byte Text **THEN** verteilt der Host „Sepp der Wirt“ bzw. weist mehr als 128 Byte ab und nimmt
    den Ersatznamen. *Codeprüfung:* Der Name steht nicht in den Verbindungsdaten; nur der Host schreibt
    ihn (NetworkVariable mit Schreibrecht Server).
30. **[EditMode]** **GIVEN** ein Client mit Steam-Namen aus 32 × „山“ (96 Byte) **WHEN** er beitritt **THEN**
    wird er angenommen (die Versionsprüfung sieht den Namen nie), und sein Schild zeigt 15 × „山“ + „…“,
    sofern die Schrift „山“ kann (sonst „Spieler n“).
31. **[PlayMode]** **GIVEN** das Namensschild-Prefab **WHEN** es „<b>Sepp</b>“ bekommt **THEN** ist Rich
    Text aus, das Schild zeigt 11 sichtbare Zeichen.
32. **[Freundestest]** **GIVEN** beide sind in der Hütte **WHEN** sie sich ansehen **THEN** steht über der
    Figur des anderen dessen Steam-Name, gekürzt wie in Kriterium 5. *Optional:* Ändert der Freund seinen
    Namen, zeigt der Host bis zu seinem Neubeitritt den alten.

### F. IP-Weg und Plätze im Bau (Regel 9, `lobby_slots`)

33. **[EditMode + Codeprüfung]** `SlotCount` gibt es in `PlayerSpawnApproval` nicht mehr; Freigabe und
    Lobby rufen dieselbe Funktion `LobbySlots` auf. **GIVEN** 4 Spawnpunkte, Index 2 leer, `max_players` 4
    **WHEN** 4 Anfragen nacheinander (Host mitgezählt) **THEN** vergibt die Freigabe 0, 1 und 3; die vierte
    wird mit „Die Hütte ist voll (3 Spieler).“ abgelehnt. `FindFreeSlot` läuft über `min(n_spawn,
    max_players)`. **GIVEN** 8 gesetzte Punkte, `max_players` 4 **THEN** wird die fünfte abgelehnt.
34. **[Manuell, optional zwei Fenster]** **GIVEN** Fenster A hostet über IP **WHEN** Fenster B über
    127.0.0.1:7777 beitritt **THEN** erscheint B wie heute, ohne `[Steam] join-…` und ohne Lobby, mit
    „Spieler 2“ über der Figur. **GIVEN** kein Host läuft **WHEN** B über IP beitreten will **THEN** kommt
    nach 60 s (±2 s) „Der Host antwortet nicht.“ (nicht nach 20 s, nicht „Verbindung getrennt.“).

### G. Ende und Abbruch (Regel 10, Edge Cases zu Steam)

35. **[Freundestest]** **WHEN** der Host trennt **THEN** ist der Freund binnen 2 s im Hauptmenü mit „Der
    Host hat das Spiel beendet“, sein Log zeigt `lobby-left reason=host-ended`; nimmt er die alte
    Einladung danach an, gilt Kriterium 26.
36. **[Freundestest]** **WHEN** der Freund trennt **THEN** loggt der Host binnen 2 s `member-left
    members=1/4`; nimmt der Freund dieselbe Einladung erneut an, steht er wieder auf Spawnpunkt 2.
37. **[Freundestest]** **GIVEN** Vorbereitung **WHEN** der Freund Steam beendet **THEN** sieht er
    „Steam-Verbindung verloren“ und landet im Hauptmenü mit ausgegrautem Steam-Knopf; der Host spielt
    weiter, loggt `member-left`, keine Exception.
38. **[Manuell]** **GIVEN** der Host hostet solo über Steam **WHEN** Steam beendet wird **THEN** läuft das
    Spiel weiter (Laufen und Tragen funktionieren), im Log `connection-lost reason=steam-lost`, keine
    Exception.

**Annahmen des `qa-lead`, beim Bau zu bestätigen:** Log-Format und Kurz-ID, Lobby-Schlüssel `game` und
`version`, Netz-Toleranzen (2 s; 60 s ±2 s), Klassennamen `SteamLobbyMath`, `ISteamService`,
`FakeSteamService`, `Tick(double realtime)`, Host im Editor mit Steam für Kriterium 24.

## Open Questions

| # | Frage | Wer klärt | Bis wann |
|---|---|---|---|
| 1 | Welche Pakete: Facepunch.Steamworks plus welcher NGO-Transport für Unity 6.6 und NGO 2.13? | `/architecture-decision`, danach OK des Entwicklers | vor dem Bau |
| 2 | Beitrittsdauer im Freundestest messen, besonders hinter strengem NAT über das Steam-Relay | Freundestest | erster Steam-Freundestest |
| 3 | Automatischer Spielstart über eine Einladung (`+connect_lobby`) | Schritt 14 | mit der eigenen App-ID |
| 4 | Welche Schrift fürs Namensschild, und kann sie Umlaute, kyrillische und chinesische Zeichen sowie Emoji? | Grafik und HUD | erste echte Grafik |
| 5 | Wer abgelehnt wird oder die Zeit überschreitet, muss die Lobby sofort verlassen; das muss der Freundestest bestätigen. | Freundestest | erster Steam-Freundestest |
