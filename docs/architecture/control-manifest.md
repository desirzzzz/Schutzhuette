# Control Manifest

> **Engine**: Unity 6.6 (6000.6.6f1), URP, PhysX, Netcode for GameObjects 2.13.3
> **Last Updated**: 2026-11-10
> **Manifest Version**: 2026-11-10
> **ADRs Covered**: ADR-0001, ADR-0002, ADR-0003
> **Status**: Active — regenerate with `/create-control-manifest update` when ADRs change

`Manifest Version` ist das Datum, an dem dieses Manifest erzeugt wurde. Stories übernehmen es beim
Schreiben, und `/story-readiness` vergleicht die Version einer Story mit dieser, um Stories zu finden, die
gegen alte Regeln geschrieben wurden. Es ist immer gleich `Last Updated`.

Dieses Manifest ist der Spickzettel für den Bau: was zu tun ist und was nie. Die Regeln stammen aus den
angenommenen ADRs, den technischen Vorgaben und der Engine-Referenz. Das Warum steht im jeweiligen ADR.

**Noch nicht abgedeckt**, weil die Entscheidungen ausstehen (`docs/architecture/architecture.md`):
ADR-0004 Interaktion übers Netz (vor Plan-Schritt 4), ADR-0005 Gäste-Wegfindung (vor Schritt 5), ADR-0006
Steam-Transport (vor Schritt 6), ADR-0007 UI-Technik (vor Schritt 15), ADR-0008 Sprachchat (vor Schritt 16),
ADR-0009 Ragdoll (vor Schritt 17). Nach jedem angenommenen ADR wird das Manifest neu erzeugt.

Technische Prüfung: TD-MANIFEST skipped — Lean mode.

---

## Foundation Layer Rules

*Gilt für: Netz-Sitzung und Autorität (`Net/`), Physik der Gegenstände, Gegenstandsarten und Katalog
(`Items/`), Beitritt, Start des Hosts*

### Required Patterns

**Autorität und Wünsche**
- **Spielzustand ändert nur der Host.** Clients schicken Wünsche per `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]` von ihrer eigenen Spielfigur. RPC-Namen enden auf `Rpc`. — source: ADR-0001
- **Der Host prüft jeden Wunsch** (Abstand, Zustand, wer hält was), bevor er ihn ausführt. — source: ADR-0001
- **Einzige Ausnahme ist der eigene Spielerkörper:** Er ist owner-autoritativ. — source: ADR-0001
- **Objekt-Identität über das Netz** läuft über `NetworkObjectReference` oder `NetworkObjectId`. Eine `NetworkObjectReference` nur aus einem gespawnten Objekt bauen (`IsSpawned` prüfen); wer eine empfängt, verträgt ein fehlschlagendes `TryGet`. — source: ADR-0001
- **Verbindungs-Meldungen:** Neuer Code nutzt `NetworkManager.OnConnectionEvent` und abonniert es vor `StartHost()`/`StartClient()`. Host-Logik wertet nur `ClientConnected` und `ClientDisconnected` aus. Vor dem Lesen von `PeerClientIds` `IsCreated` prüfen; wer die Liste braucht, kopiert sie. — source: ADR-0001
- **Beitritt:** Die reine Funktion `JoinRules.Decide` prüft in der Reihenfolge Version → Saison läuft → voll. `PlayerSpawnApproval` ruft sie im `ConnectionApprovalCallback` auf und gibt den Grund über `response.Reason` zurück. Beide liegen in `Net/` (`Schutzhuette.Net`). „Saison läuft“ ist ein `bool` an `PlayerSpawnApproval`, den Core von oben setzt. — source: ADR-0003

**Physik der Gegenstände**
- **Gegenstände** haben einen server-autoritativen `NetworkTransform` mit `NetworkRigidbody` (`AutoUpdateKinematicState` an, `UseRigidBodyForMotion` aus, `SwitchTransformSpaceWhenParented` an). Bei Clients sind sie immer kinematisch. — source: ADR-0001
- **Drei Zustände beim Host:** *Liegt* (Rigidbody aktiv, schläft ein, „Discrete“), *Getragen* (Kind der Spielfigur, kinematisch, Collider aus; Wände prüft der Träger mit `CarryCollision`), *Fällt* (Host rechnet die Physik voll). „Fällt“ kennt nur der Host, es geht nicht ins Netz. — source: ADR-0001
- **Kollisionsprüfung an einer einzigen Stelle je Gegenstand beim Host abgleichen:** `ContinuousDynamic` nur, wenn der Gegenstand fällt und nicht kinematisch ist, sonst `Discrete`. Eigener Code, der einen Körper kinematisch macht oder entfernt, stellt vorher auf `Discrete`. Die Vorhersage-Kopie beim Client bekommt beim Erzeugen `ContinuousDynamic`. — source: ADR-0001
- **Aufprall messen:** `ImpactReporter` (je Gegenstand, nur Host) misst `impact_speed` als größten senkrechten Anteil der Relativgeschwindigkeit über alle Kontaktpunkte, prüft die Bruchregel und meldet einmal an `ImpactEvents`. Andere Systeme hören dort zu. — source: ADR-0001
- **Trifft ein nicht kinematischer Körper einen liegenden Gegenstand,** schaltet dessen `ImpactReporter` ihn im selben Callback zuerst auf „Fällt“ und prüft erst danach die Bruchregel. — source: ADR-0001
- **Kontaktpunkte ohne Allokation lesen** (`contactCount` und `GetContact(i)`). Was ein Zuhörer braucht, steht im `ImpactReport`. — source: ADR-0001
- **Zerbrechen:** genau einmal melden und entfernen (Sperre gegen weitere Kontakte im selben Physikschritt), in der Reihenfolge Melden (`Broke = true`) → Entfernen über `ItemSpawning.Remove`. — source: ADR-0001, ADR-0002
- **`ImpactEvents.Raise` sichert jeden Zuhörer einzeln ab** (try/catch mit `Debug.LogException`), damit ein Fehler weder andere Zuhörer noch das Entfernen verhindert. — source: ADR-0001
- **`Rigidbody.linearVelocity` nur beim Loslassen** (Startgeschwindigkeit) **und beim Aufheben** (auf null) schreiben. Laufende Kräfte gehen über `AddForce`. — source: ADR-0001
- **Ausrüstung „am Körper“** wechselt nie in „Fällt“, auch nicht beim Sturz des Trägers (gilt, bis ADR-0004 den Zustand festlegt). — source: ADR-0001

**Gegenstandsarten und Katalog**
- **Ein Datenobjekt je Eintrag:** `ItemKind` je Art, `WareKind` je Ware, `NeedKind` je Bedürfnis; ein `ItemCatalog` listet alle und trägt die Katalog-Einstellungen. Ablage: `Assets/_Project/Data/Items/`, `Data/Wares/`, `Data/Needs/`, `Data/Guests/`. — source: ADR-0002
- **Jeder Spielwert einer Art steht im `ItemKind`.** Komponenten und Prefabs führen keine eigenen Kopien (keine Brenndauer im `FuelItem`, keine Masse nur im Rigidbody). — source: ADR-0002
- **Optionale Eigenschaften sind feste `[Serializable]`-Blöcke** mit Schalter `Enabled` im `ItemKind` (Brennbar, Hackbar, Vorratspaket, Gericht, Zerbrechlich, Werkzeug, Ausrüstung). Eine neue Eigenschaft ist ein neuer Block. `ToolAction` und `EquipmentSlot` sind Enums in Foundation. — source: ADR-0002
- **Das Prefab verrät die Art:** Jede Art hat ein eigenes Prefab, jedes Gegenstands-Prefab trägt die Komponente `Item` mit Verweis auf seine `ItemKind` und steht in der Netzwerk-Prefab-Liste. In `Awake` übernimmt der Rigidbody auf jedem Rechner die Masse aus dem Katalog. — source: ADR-0002
- **Kennungen** folgen dem Muster `^[a-z0-9_]{1,29}$`. Waren beginnen mit `ware_`, Bedürfnisse mit `beduerfnis_`, Arten mit keinem der beiden. Übers Netz gehen sie als `FixedString32Bytes`. — source: ADR-0002
- **Unbekannte Kennung beim Client:** Fehler im Log, der Gegenstand wird nicht angezeigt. — source: ADR-0002
- **Werte je Gegenstand** sind NetworkVariables, die nur der Host schreibt. Der Startwert kommt vor `Spawn()` in ein normales Feld (zum Beispiel `SetInitialWare`), und der Host überträgt ihn in `OnNetworkSpawn` in die NetworkVariable. Clients verarbeiten spätere Änderungen über `OnValueChanged`. — source: ADR-0002
- **Erscheinen nur über `ItemSpawning.Spawn`** (Host): prüft die Art, `Instantiate`, Startwerte, `Spawn()`. Eine ungültige Art erscheint nicht. Wer ein Vorratspaket ohne feste Ware erzeugt, nennt die Ware. — source: ADR-0002
- **Verschwinden nur über `ItemSpawning.Remove`** (Host): erst `Discrete`, dann `NetworkObject.Despawn(true)`. — source: ADR-0002
- **Zugriff zur Laufzeit über `CatalogService.Current`** (Komponente am `NetworkManager`-Objekt). Er baut die Nachschlagetabellen (Kennung → Eintrag) bei jedem Start neu. Prüfregeln, Editor-Code und Tests bekommen den Katalog als Parameter. — source: ADR-0002
- **Prüfung als reine Funktion:** `CatalogValidator.Validate(ItemCatalog, NetworkPrefabsList)` liefert einen `CatalogReport`. Sie läuft im Editor (Menüpunkt, EditMode-Tests), im Build-Skript (`-executeMethod`, Exit-Code 1 bei Fehlern) und vor `StartHost()` im Startmenü. — source: ADR-0002
- **Prefabs, die ein Editor-Skript anlegt,** lassen danach ihren `GlobalObjectIdHash` neu erzeugen (wie `PrototypeSceneBuilder.RefreshPrefabHashFromCommandLine`). — source: ADR-0002
- **Katalog-Assets entstehen per Editor-Skript oder im Inspector.** — source: ADR-0002
- **Zeitregeln für Gerichte** stehen rein statisch in `DishRules`; Clients berechnen `is_warm` und `warm_fraction` selbst aus `CookedTime` (`NetworkVariable<double>`, Spielzeit). — source: ADR-0002

### Forbidden Approaches

- **Nie schreiben Clients NetworkVariables oder bewegen Gegenstände selbst** — zwei Entscheider erzeugen widersprüchlichen Zustand — source: ADR-0001
- **Nie lösen Vorhersage-Kopien Bruchregel, Meldedienst oder andere Spiel-Logik aus** — Vorhersage ist nur optisch; die Kopie ist nicht vernetzt und zerbricht nie selbst — source: ADR-0001
- **Nie `OnCollisionEnter` für Spiel-Logik außerhalb von `ImpactReporter`** — ein Messpunkt, damit Zerbrechen, Geräusche und Gäste mit demselben `impact_speed` rechnen — source: ADR-0001
- **Nie `collision.contacts` lesen und nie den `Collision`-Parameter über den Callback hinaus speichern** — Allokation je Kontakt; Unity verwendet das Objekt wieder — source: ADR-0001
- **Nie `EntityId` oder `InstanceID` über das Netz** — für Identität zwischen Rechnern gelten die Kennungen der Netz-Schicht — source: ADR-0001, current-best-practices.md
- **Nie `OnClientConnectedCallback`/`OnClientDisconnectCallback` in neuem Code, nie `PeerConnected`/`PeerDisconnected` für Host-Logik, nie `PeerClientIds` speichern** — NGO gibt das Array direkt nach dem Aufruf frei — source: ADR-0001
- **Nie ein eigenes `private void OnDestroy()` in einem NetworkBehaviour** — es verdeckt `NetworkBehaviour.OnDestroy`, NGO gibt dann die NetworkVariables nicht frei; nur als `override` mit `base.OnDestroy()` — source: ADR-0001
- **Nie den Spielerkörper server-autoritativ mit Rückabgleich bauen** — viel Aufwand für einen Schutz, den ein Koop-Spiel nicht braucht — source: ADR-0001
- **Nie Distributed Authority** — mehrere Entscheider für Zerbrechen, Treffer und Tragen zu zweit; Connection Approval wird damit nicht unterstützt — source: ADR-0001
- **Nie einen eigenen Vorausblick per SphereCast statt der Kollisionsprüfung der Physik** — mehr Code und kein Aufprall-Ereignis — source: ADR-0001
- **Nie ein Event je Gegenstand, und Gäste suchen Aufpralle nie selbst per OverlapSphere** — sie müssten jeden Gegenstand kennen oder verpassen kurze Aufpralle — source: ADR-0001
- **Nie einen Zustand „zerbrochen“ als NetworkVariable** — mehr Zustand; verworfen zugunsten des Dauer-Objekts `ImpactEffects` — source: ADR-0001
- **Nie Datenobjekte zur Laufzeit schreiben** (`ItemKind`, `WareKind`, `NeedKind`, `ItemCatalog`, `GuestType`) — im Editor ändert das das Asset dauerhaft, und ohne Domain Reload bleibt der Wert bis zum nächsten Play. Zustand eines einzelnen Gegenstands gehört an den Gegenstand — source: ADR-0002
- **Nie eine Art an Kennung, Anzeigename oder Prefab-Name erkennen** — nur an ihrer Eigenschaft (`Kind.Fuel.Enabled`), damit neue Arten ohne Code-Änderung funktionieren — source: ADR-0002
- **Nie ScriptableObjects, Asset-Pfade oder Listen-Positionen über das Netz** — Listen-Positionen verschieben sich beim Umsortieren still auf eine andere Art — source: ADR-0002
- **Nie eine Kennung als Nummer (Listenplatz) oder Prüfsumme** — still die falsche Art bei abweichendem Katalog beziehungsweise in Logs unlesbar — source: ADR-0002
- **Nie eine Kennung nach dem ersten Release ändern** — source: ADR-0002
- **Nie zwei Arten mit demselben Prefab, nie die Art-Kennung als NetworkVariable je Gegenstand** — kostet Netzdaten und neue Fehlerfälle — source: ADR-0002
- **Nie still eine andere Art anzeigen, wenn eine Kennung unbekannt ist** — source: ADR-0002
- **Nie eine NetworkVariable vor `Spawn()` direkt schreiben, und nie den Startwert erst nach `Spawn()`** — das eine loggt eine Warnung, das andere kommt einen Tick zu spät — source: ADR-0002
- **Nie `Despawn` oder `Destroy` für Gegenstände direkt aufrufen** — nur `ItemSpawning.Remove`; ein kinematischer Körper mit durchgehender Kollisionsprüfung löst sonst eine Warnung aus — source: ADR-0002
- **Nie Nachschlagetabellen als serialisiertes `Dictionary` im Asset, nie Laufzeit-Tabellen im `ItemCatalog`** — ohne Domain Reload könnten sie zwischen zwei Play-Durchläufen alt bleiben — source: ADR-0002
- **Nie `CatalogService.Current` in Prüfregeln, Editor-Code oder Tests** — source: ADR-0002
- **Nie die Gäste-Prüfung aus Foundation-Code aufrufen** (Katalog-Dienst, Netz-Code) — Aufrufe gehen nur nach unten; sie wird von oben aufgerufen (Startmenü, Build-Skript, Build-Hook, Tests) — source: ADR-0002
- **Nie ein großes ScriptableObject mit einer Liste aller Einträge** — ein Prefab kann nicht sauber auf ein Listenelement verweisen — source: ADR-0002
- **Nie Arten aus einer externen Datei (JSON) zur Laufzeit laden** — keine Verweise auf Prefabs, kein Inspector, eigener Lade- und Prüfcode — source: ADR-0002
- **Nie Eigenschaften als `[SerializeReference]`-Liste oder als Komponenten am Prefab** — Inspector-Anzeige für 6.6 nicht belegt beziehungsweise Werte stünden wieder im Prefab — source: ADR-0002
- **Nie einen Katalog-Verweis in jeder Komponente, nie `Resources.Load`** — viele vergessliche Verweise beziehungsweise laut Engine-Referenz veraltet — source: ADR-0002
- **Nie eine `.asset`-Datei des Katalogs von Hand bearbeiten** — source: ADR-0002
- **Nie kennt Foundation-Code (`Net/`, `Items/`) `SeasonClock`, `SeasonPhase` oder anderes aus Core** — Schichtenregel; Core setzt Werte von oben (`PlayerSpawnApproval.SeasonRunning`) — source: ADR-0003

### Performance Guardrails

- **Physik der Gegenstände:** durchgehende Kollisionsprüfung nur für fallende Gegenstände; bei Clients rechnet keine Physik für Gegenstände; `OnCollisionEnter` beim Host ohne Allokation — source: ADR-0001
- **Netzverkehr Physik:** keine neue NetworkVariable; je Bruch eine RPC plus Despawn; Aufprall-Geräusche mit Sperrzeit je Gegenstand; Ruhendes sendet nichts — source: ADR-0001
- **Katalog:** Nachschlagetabellen einmal beim Start, Nachschlagen über ein `Dictionary`, keine Kosten pro Frame — source: ADR-0002
- **Netzverkehr Katalog:** keine Daten für die Art eines Gegenstands; Ware-Kennung (etwa 10–15 Byte) und Kochzeitpunkt (8 Byte) nur einmal in der Spawn-Nachricht — source: ADR-0002
- Die ADRs nennen keine Millisekunden-Budgets je System. Gemessen wird in Plan-Schritt 5 (`architecture.md`, QQ-01 und QQ-02).

### Engine API Constraints

Diese Punkte liegen nach dem Wissensstand des Modells oder sind nicht belegt. Die Story, die sie zuerst
benutzt, prüft sie im Editor und nimmt sie als Abnahmekriterium auf.

- **Umschalten der Kollisionsprüfung** (ADR-0001, Verification 1): keine Konsolen-Warnung bei Host und Client, auch nicht beim Bruch; eine Tasse mit 15 m/s fliegt nicht durch eine 0,2 m dicke Innenwand. Dieselben Fälle mit dauerhaft `ContinuousSpeculative` wiederholen. Zeigt `ContinuousDynamic` Warnungen oder Durchflüge und der Dauerwert nicht, gilt der Dauerwert, und die Abgleichstelle entfällt.
- **Wurf trifft Gast** (ADR-0001, Verification 2): Ein Wurf mit 8–15 m/s trifft den Collider eines Gastes (Plan-Schritt 5).
- **`OnConnectionEvent`** (ADR-0001, Verification 3): meldet beim Host `ClientConnected` für den eigenen Client (Log-Prüfung).
- **Startwert in der Spawn-Nachricht** (ADR-0002, Verification 1): Der Client sieht die Ware-Kennung schon in seinem `OnNetworkSpawn`, ohne die Warnung „NetworkVariable is written to, but doesn't know its NetworkBehaviour yet“ beim Host.
- **Build-Abbruch** (ADR-0002, Verification 2): Ein absichtlich kaputter Katalog lässt das Build-Skript mit Exit-Code 1 enden, ohne dass eine `.exe` entsteht. `BuildPipeline.BuildPlayer` und `IPreprocessBuildWithReport` sind für 6.6 nicht belegt; der Build-Hook ist nur Zusatzschutz.
- **Katalog bleibt unberührt** (ADR-0002, Verification 3): Nach einer Spielrunde zeigt `git status` keine Änderung an den Katalog-Dateien.
- **Länge der Kennung** (ADR-0002, Verification 4): 29 Zeichen passen in `FixedString32Bytes`, 30 meldet die Prüfung als Fehler.
- **PhysX in Unity 6.4–6.6:** Änderungen sind nicht belegt (`breaking-changes.md` nennt keine). Rigidbody-Werte im Editor prüfen, nicht aus der Referenz übernehmen.
- **Reihenfolge von `OnCollisionEnter`** bei zwei Körpern ist nicht festgelegt; kein Code darf sich darauf verlassen.

---

## Core Layer Rules

*Gilt für: Saison-Ablauf, Spielzeit und Phasen (`Core/`), Meldungen an die Spiel-Logik, Solo-Pause,
Startaufstellung*

### Required Patterns

- **Der Ablauf steht in der reinen C#-Klasse `SeasonDirector`** (kein MonoBehaviour, nur Host). Sie bekommt die Serverzeit als Parameter und meldet über `ISeasonOutput`. Die Formeln stehen rein statisch in `SeasonMath`. — source: ADR-0003
- **`SeasonClock` hält genau zwei Netzwerkwerte:** `NetworkVariable<SeasonSnapshot>` (Phase, Phasenstart, Pausensumme, Saisonzähler in einem struct) und `NetworkList<ulong>` (wer bestätigt hat). Nur der Host schreibt; den Startwert schreibt er in `OnNetworkSpawn`. Beide sind Feld-Initialisierer. — source: ADR-0003
- **`SeasonSnapshot`** liest und schreibt in `NetworkSerialize` alle vier Felder in derselben Reihenfolge, und `Equals` vergleicht alle vier. — source: ADR-0003
- **Spielregeln mit Zeit rechnen mit `game_time`** (`SeasonClock.Current.GameTime` oder als Parameter). Gespeicherte Zeitpunkte sind `double` in `game_time`. Grundlage ist `NetworkManager.ServerTime.Time`. — source: ADR-0003
- **Beim Client läuft die Spielzeit mit `max` gegen das letzte Bild,** ohne Ausnahme. Den gemerkten Wert setzt er nur in `OnNetworkSpawn` zurück. Einen Rücksprung über 0,5 s loggt er nur (`[Season] time-jump`). `PhaseElapsed` liefert nie weniger als 0. — source: ADR-0003
- **Solo-Pause über `Time.timeScale`:** Nur `SeasonClock` schreibt den Wert (0 in der Pause, sonst 1) und setzt ihn in `OnNetworkSpawn`, `OnNetworkDespawn` und bei `SubsystemRegistration` auf 1. Pause gilt nur, wenn das Menü des Hosts offen ist, der Host allein ist und die Phase Nachmittag, Abend oder Nacht ist. — source: ADR-0003
- **Code, der durch `Time.deltaTime` teilt,** fängt den Wert 0 ab. — source: ADR-0003
- **Tests, die eine Pause auslösen,** setzen `Time.timeScale` im TearDown auf 1. — source: ADR-0003
- **Spiel-Logik hört an `SeasonEvents`** (`SeasonStarted`, `PhaseBegan`, `NightOver`, `SeasonAborted`) und tut dort nur beim Host etwas. Jeder Zuhörer wird einzeln in try/catch aufgerufen. — source: ADR-0003
- **Reihenfolge der Meldungen:** Start: Gegenstände entfernen → `SeasonStarted` → Startaufstellung → `PhaseBegan(Afternoon)`. Abbruch in der Nacht: `NightOver(Abort)` → `SeasonAborted` → `PhaseBegan(Preparation)`. Mehrere Phasenwechsel in einem Schritt melden jede Phase einzeln in der richtigen Reihenfolge. — source: ADR-0003
- **Jedes System setzt sich bei `SeasonStarted` selbst zurück.** — source: ADR-0003
- **Code, der `SeasonClock.Current` liest, verträgt `null`** (Objekt noch nicht gespawnt) und führt dann keine Zeitregel aus. — source: ADR-0003
- **„Bereit“ und „Weiter“** schicken Clients per Owner-RPC von ihrer Spielfigur (`PlayerSeasonRequests`); der Host gibt sie mit der Absender-Kennung an `SeasonClock` weiter. — source: ADR-0003
- **Wer verbunden ist:** `SeasonClock` übergibt beim Spawn `NetworkManager.ConnectedClientsIds` an `SeasonDirector.ClientJoined` und danach jedes `ClientConnected` und `ClientDisconnected` aus `OnConnectionEvent`. — source: ADR-0003
- **„Saison starten“ wird abgelehnt,** solange ein angenommener Spieler noch nicht erschienen ist (`PendingJoins`, Log `start-blocked reason=join-pending`). — source: ADR-0003
- **Startaufstellung als Datenobjekt `StartLayout`** („Platz-Kennung → Art, optional Ware“) mit Markern `ItemSlot` in der Szene. Beim Start entfernt der Host jeden Gegenstand mit `ItemSpawning.Remove` und erzeugt die neuen mit `ItemSpawning.Spawn`. Ein unbekannter Eintrag lässt den Platz leer und loggt einen Fehler. — source: ADR-0003
- **Spieler an den Startpunkt:** Der Host schickt `[Rpc(SendTo.Owner)]`; der Besitzer schaltet den `CharacterController` aus, setzt Position und Drehung, ruft `NetworkTransform.Teleport(pos, rot, transform.localScale)`, setzt die Fallgeschwindigkeit auf 0 und schaltet den `CharacterController` wieder an. — source: ADR-0003
- **Phasenlängen und Druckwerte stehen im `SeasonProfile`.** — source: ADR-0003
- **Beim Wechsel nach Zeit** rechnet der Director `neuer Start = alter Start + Länge`. — source: ADR-0003
- **Wer einen Gegenstand hält oder auf ihn verweist,** geht damit um, dass der Host ihn entfernt (Startaufstellung, Zerbrechen). — source: ADR-0003

### Forbidden Approaches

- **Nie rechnen Spielregeln mit `Time.time`, `Time.deltaTime`, `Time.unscaledTime` oder `NetworkManager.ServerTime` direkt** — nur `game_time` steht in der Solo-Pause und ist auf allen Rechnern gleich. Darstellung darf `Time.deltaTime` nutzen — source: ADR-0003
- **Nie Fortschritt der Phase, Druck oder verstrichene Zeit als NetworkVariable oder RPC übertragen** — Verkehr in jedem Tick und Ruckeln beim Client; jeder Rechner rechnet sie mit `SeasonMath` — source: ADR-0003
- **Nie nach dem Setzen einer NetworkVariable zusätzlich von Hand auslösen, was an `OnValueChanged` hängt** (zum Beispiel `SeasonClock.PhaseChanged`) — der Setter ruft `OnValueChanged` auch beim Host sofort auf; ein Handaufruf meldet doppelt — source: ADR-0003
- **Nie `Time.timeScale` außerhalb von `SeasonClock` schreiben** — source: ADR-0003
- **Nie einzelne NetworkVariables für Phase und Phasenstart** — der Client sähe kurz die neue Phase mit dem alten Start — source: ADR-0003
- **Nie `NetworkManager.LocalTime` für die gemeinsame Uhr** — eine Vorausschätzung des Clients, liegt vor dem Host — source: ADR-0003
- **Nie eine eigene Uhr je Rechner mit Abgleich per RPC** — baut nach, was NGO schon tut — source: ADR-0003
- **Nie Ticks statt Sekunden** — hängt an der Tick-Rate; die Formeln rechnen in Sekunden — source: ADR-0003
- **Nie eine Pause über einen eigenen Wert** (`Physics.simulationMode = Script`, jedes System prüft selbst) — jedes vergessene System läuft in der Pause weiter — source: ADR-0003
- **Nie `SeasonEvents` auf allen Rechnern melden, nie Instanz-Events am Uhr-Objekt für Spiel-Logik** — vergisst ein Zuhörer die Host-Prüfung, setzt ein Client Spielzustand zurück — source: ADR-0003
- **Nie den Abbruch-Grund an die Phasen-Meldung hängen** — dafür gibt es `SeasonAborted` — source: ADR-0003
- **Nie kennt der Saison-Code Klassen aus `Hut/`, `Guests/`, `Weather/` oder anderen Feature-Ordnern** — source: ADR-0003
- **Nie 600, 720, 480 oder 1080 als feste Zahlen in den Saison-Skripten** — source: ADR-0003

### Performance Guardrails

- **Saison-Uhr:** ein `Step` pro Bild beim Host, ein paar Rechnungen pro Bild auf jedem Rechner — source: ADR-0003
- **Netzverkehr Saison:** `SeasonSnapshot` (etwa 21 Byte) etwa sechsmal pro Saison; die Bestätigungsliste nur in Vorbereitung und Bewertung; kein Verkehr pro Bild — source: ADR-0003

### Engine API Constraints

- **`Time.timeScale = 0`** (ADR-0003, Verification 1 und 2): Was es anhält, ist für 6.6 nicht belegt. Prüfen: Menü 20 s offen, während eine Kiste fliegt → sie hängt in der Luft und fliegt danach weiter, `game_time` steht, keine NaN-Werte. Play bei offenem Menü beenden und neu starten → `Time.timeScale` ist im ersten Bild 1.
- **`ServerTime` läuft mit unskalierter Zeit** (NGO-2.13.3-Quelltext): `Time.timeScale = 0` hält sie nicht an. Beim Client kann sie rückwärts springen, wenn die Abweichung größer als 0,2 s ist.
- **Spielzeit beim Client** (ADR-0003, Verification 3 und 4): mit Network Simulator (100 ms) wird `game_time` in keinem Bild kleiner; keine Log-Zeile mit neuer Phase und altem Phasenstart; `PhaseChanged` feuert beim Host genau einmal pro Wechsel.
- **`OnValueChanged`** feuert beim ersten Abgleich nach dem Spawn nicht; `SeasonClock` übernimmt den Startwert in `OnNetworkSpawn` einmal selbst. Schreibt der Host mehrere Werte in einem Bild, kommt beim Client nur der letzte an.
- **Später Beitritt** (ADR-0003, Verification 6): Ein Client, der in der Bewertung beitritt, sieht Phase und Saison-Nr. richtig, auch wenn andere Objekte vor `SeasonClock` erscheinen.
- **`NetworkTransform.Teleport`** (ADR-0003, Verification 7): wirft auf der nicht autoritativen Seite eine Exception. Prüfen: Der Spieler des Freundes springt beim Host an den Startpunkt, ohne durch die Hütte zu gleiten.
- **Die Interpolation von `NetworkTransform` rechnet mit skalierter Zeit:** `timeScale = 0` würde bei Clients Bewegungen einfrieren, deshalb Pause nur ohne Clients.

---

## Feature Layer Rules

*Gilt für: Ofen, Lager, Gäste, Wetter, Kasse, Nachteinsatz (`Hut/`, `Guests/`, `Weather/`)*

### Required Patterns

- **Feature-Systeme hören an `ImpactEvents` und `SeasonEvents`** und melden sich nach der Regel unter „Cross-Cutting Constraints“ an und ab. Doppelte Wirkung verhindert jeder Zuhörer mit eigener Sperrzeit (zum Beispiel `noise_lockout` je Gast). — source: ADR-0001, ADR-0003
- **Jedes Feature-System setzt sich bei `SeasonStarted` selbst zurück** (Ofen, Lager, Gäste, Kasse, Einsatz). — source: ADR-0003
- **Gästetypen (`GuestType`) und Gäste-Einstellungen** folgen dem Katalog-Muster im Gäste-Code (`Guests/`) und verweisen nach unten auf `NeedKind`. Die Gäste-Daten prüft eine eigene reine Funktion im Gäste-Code (`guest_data_valid`), samt Querverweis Gericht → Bedürfnis. — source: ADR-0002
- **Die Hüttenkasse** merkt sich ihren Stand bei `SeasonStarted` und setzt ihn bei `SeasonAborted` zurück. Der Saisonzähler steigt erst bei „Nächste Saison“. — source: ADR-0003
- **Der Nachteinsatz** ruft nur `SeasonClock.MissionFinished()` auf und kennt sonst nichts von der Uhr. — source: ADR-0003

### Forbidden Approaches

- **Nie ein eigenes `OnCollisionEnter` für Spiel-Logik in Feature-Code** — nur `ImpactReporter` misst — source: ADR-0001
- **Nie Brennzeit, Abkühlen, Fortschritt oder Geduld mit `Time.deltaTime` rechnen** — nur mit `game_time` — source: ADR-0003
- **Nie eigene Kopien von Spielwerten einer Art in Feature-Komponenten** (zum Beispiel Brenndauer im `FuelItem`) — source: ADR-0002

---

## Presentation Layer Rules

*Gilt für: Klang, Effekte, HUD, Menüs, Licht (`Audio/`, `UI/`)*

### Required Patterns

- **Scherben-Effekt, Scherben-Klang und alle Aufprall-Geräusche** sendet das dauerhafte Objekt `ImpactEffects` (vom Host beim Start erzeugt). Es hört an `ImpactEvents` und sendet `PlayBreakRpc` und `PlayImpactRpc` an alle. Bei zwei zusammenstoßenden Gegenständen zählt der schwerere. `PlayImpactRpc` nur, wenn der Gegenstand noch gespawnt ist. — source: ADR-0001
- **Ein Client überspringt die Geräusch-RPC,** wenn der Gegenstand bei ihm gerade ausgeblendet ist, weil seine Kopie fliegt. — source: ADR-0001
- **Anzeigen (HUD, Licht, Musik) lesen `SeasonClock.Current` und das Event `SeasonClock.PhaseChanged`.** Sie setzen keine Phase voraus, weil beim Client nur der letzte Wert ankommt. — source: ADR-0003
- **Das Menü schaltet beim Öffnen die Gameplay-Action-Maps aus** (Bewegung, Blick, Interaktion) und beim Schließen wieder an, mit und ohne Pause. Das Menü nutzt eine eigene Action-Map, die an bleibt. — source: ADR-0003
- **Das Startmenü ruft vor `StartHost()` die Katalog- und die Gäste-Prüfung auf.** Bei Fehlern startet der Host nicht, und die Fehler stehen auf dem Bildschirm. — source: ADR-0002

### Forbidden Approaches

- **Nie sendet ein Gegenstand, der gleich entfernt wird, seine Effekte selbst** — nach dem Despawn kann er keine RPC mehr senden, und `NetworkObjectReference` wirft — source: ADR-0001
- **Nie ein eigener `OnCollisionEnter` in `ImpactSound`** — entfällt zugunsten von `ImpactEffects` — source: ADR-0001
- **Nie hören Anzeigen an `SeasonEvents`** — der Meldedienst läuft nur beim Host — source: ADR-0003

---

## Global Rules (All Layers)

### Naming Conventions

| Element | Convention | Example | Source |
|---------|-----------|---------|--------|
| Classes | PascalCase | `PlayerController` | `naming.classes` in project.yaml |
| Variables | camelCase für lokale Variablen und Parameter; private Felder `_camelCase`; öffentliche Felder und Properties PascalCase | `_moveSpeed`, `MoveSpeed` | `naming.variables` in project.yaml, technical-preferences.md |
| Signals/Events | PascalCase ohne „On“; Methoden, die auf ein Event reagieren, heißen `On<Event>` | `PhaseChanged`, Reaktion `OnPhaseChanged` | `naming.signals` in project.yaml (am 2026-11-10 an ADRs und Code angepasst) |
| Files | PascalCase, gleich dem Klassennamen | `PlayerController.cs` | `naming.files` in project.yaml |
| Constants | PascalCase | `ShrinkFactor` | `naming.constants` in project.yaml (am 2026-11-10 an den vorhandenen Code angepasst) |
| Scenes/Prefabs | PascalCase | `Prototype.unity` | `naming.scenes` in project.yaml |
| RPC-Methoden | enden auf `Rpc` | `PickUpRpc` | ADR-0001 |
| Kennungen im Katalog | `^[a-z0-9_]{1,29}$`, Präfixe `ware_` und `beduerfnis_` | `holzscheit`, `ware_tee` | ADR-0002 |
| Namespaces | wie in den ADRs: `Schutzhuette.Items`, `Schutzhuette.Core`, `Schutzhuette.Net`; nicht `Schutzhuette.Physics` (verdeckt `UnityEngine.Physics`) | `Schutzhuette.Items` | ADR-0001, ADR-0002, ADR-0003 |

### Performance Budgets

| Target | Value | Source |
|--------|-------|--------|
| Framerate | 60 fps | technical-preferences.md (`performance.*` fehlt in project.yaml) |
| Frame budget | 16,6 ms | technical-preferences.md |
| Draw calls | noch nicht festgelegt, Messung in Plan-Schritt 5 (QQ-01) | technical-preferences.md |
| Memory ceiling | noch nicht festgelegt, Messung in Plan-Schritt 5 (QQ-01) | technical-preferences.md |

### Approved Libraries / Addons

- `com.unity.netcode.gameobjects` 2.13.3 — Netz, Host-Client. Auf 2.13.x bleiben, solange Unity 6.6 läuft (3.x verlangt Unity 6000.7) — source: technical-preferences.md
- `com.unity.transport` 6.6.0 — kommt mit NGO, auf Unity 6.6 eingebaut — source: technical-preferences.md
- `com.unity.multiplayer.playmode` 3.0.0 — Tests mit bis zu vier Spielern im Editor — source: technical-preferences.md
- `com.unity.multiplayer.tools` 2.2.12 — Network Simulator für die Tests mit etwa 100 ms — source: technical-preferences.md
- `com.unity.inputsystem` 1.20.0 — neues Input System — source: technical-preferences.md
- `com.unity.test-framework` 1.8.0 — EditMode- und PlayMode-Tests — source: `Packages/manifest.json` (technical-preferences.md nennt den Stand vor `/test-setup`)
- `com.coplaydev.unity-mcp`, `com.ramenvr.aura-unity`, `com.ramenvr.autopilot` — Editor-Werkzeuge des Entwicklers, kein Spielcode — source: technical-preferences.md
- **Noch nicht freigegeben, nur nach Rückfrage:** AI Navigation (ADR-0005), Facepunch.Steamworks (ADR-0006), Vivox oder Dissonance (ADR-0008), Addressables — source: CLAUDE.md („Neue Pakete nur nach Rückfrage“), `docs/architecture/architecture.md`

### Forbidden APIs (Unity 6000.6.6f1)

Nur Einträge, die dieses Projekt betreffen. Quelle: `docs/engine-reference/unity/deprecated-apis.md`.

- `Input.GetKey()`, `Input.GetKeyDown()`, `Input.GetMouseButton()`, `Input.GetAxis()`, `Input.mousePosition` — veraltet; Input Actions des neuen Input Systems nutzen
- `GetInstanceID()` und andere `InstanceID`-APIs — in 6.5 entfernt (Compile-Fehler); `GetEntityId()` nutzen, `EntityId` nie in ein `int` legen, nie seinen Hash als Schlüssel
- Kurz-Properties `.rigidbody`, `.collider`, `.camera`, `.light`, `.renderer`, `.audio`, `.animation`, `.particleSystem` und `GameObject.AddComponent(string)` — in 6.5 entfernt; `GetComponent<T>()` und `AddComponent<T>()` nutzen
- `Physics.RaycastAll()` — veraltet; `Physics.RaycastNonAlloc()` nutzen
- `Rigidbody.velocity` — nicht direkt schreiben; `linearVelocity` nur an den zwei Stellen aus ADR-0001, sonst `AddForce`
- `Resources.Load()` — veraltet; Verweise über Datenobjekte und den Katalog (ADR-0002)
- `Application.LoadLevel()` — veraltet; `SceneManager.LoadScene()` nutzen
- `DEVELOPMENT_BUILD` und `UNITY_64` — in 6.6 veraltet, in 6.8 entfernt; `#if UNITY_EDITOR || UNITY_ENABLE_CHECKS` oder zur Laufzeit `Debug.isDebugBuild`
- `OnDisable` von Hand an Kind-Objekte weiterreichen — seit 6.4 macht `Object.Destroy` das selbst, sonst doppelte Aufrufe
- Im Build-Skript: `BuildOptions.ForceEnableAssertions` und `CoreBuildData.developmentBuild` — veraltet; `PlayerSettings.SetManagedCodeVariant(…, ManagedCodeVariant.Checked)` beziehungsweise `CoreBuildData.useDiagnosticChecks`
- Wenn eigene Render-Pässe gebaut werden: `CommandBuffer.DrawMesh()`, `OnPreRender()`/`OnPostRender()`, `Camera.SetReplacementShader()` und der URP Compatibility Mode — entfernt oder in URP nicht unterstützt; Render-Graph-API nutzen
- Wenn Animationen dazukommen: die alte `Animation`-Komponente — `Animator` nutzen
- Dynamic Batching — in 6.6 obsolet; nicht einschalten
- `[ServerRpc]`/`[ClientRpc]` — das Beispiel in `current-best-practices.md` hat den Stand 6.3; in diesem Projekt gilt die `[Rpc(SendTo…)]`-Form aus ADR-0001

### Cross-Cutting Constraints

- **Jedes statische Feld wird in einer Methode mit `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` zurückgesetzt** (Domain Reload ist aus). — source: ADR-0001, CLAUDE.md, current-best-practices.md
- **Rechenregeln sind reine statische Funktionen** (Bruch, Pose, Ofen, `SeasonMath`, `DishRules`, `JoinRules`, `CatalogValidator`), damit EditMode-Tests sie ohne Szene prüfen. Zeit und Katalog kommen als Parameter. — source: ADR-0001, ADR-0002, ADR-0003
- **Aufrufe gehen nur nach unten:** Presentation → Feature → Core → Foundation. Eine tiefere Schicht kennt keine höhere; die höhere setzt Werte in der tieferen. — source: ADR-0002, ADR-0003
- **An- und Abmelden an einem Meldedienst** (`ImpactEvents`, `SeasonEvents`): NetworkBehaviours in `OnNetworkSpawn` (nur bei `IsServer`) und `OnNetworkDespawn`; reine MonoBehaviours in `OnEnable` und `OnDisable`. Meldedienste lösen nur beim Host aus, und nur Host-Code hört zu. — source: ADR-0001, ADR-0003
- **Zusammengehörige Netzwerte** gehen als ein `unmanaged` struct (`INetworkSerializable`, `IEquatable<T>` über alle Felder) in eine einzige NetworkVariable. — source: ADR-0003
- **Dauerhafte Netz-Objekte** (`ImpactEffects`, `SeasonClock`) erzeugt der Host beim Start. — source: ADR-0001, ADR-0003
- **Spielwerte stehen in Datenobjekten,** nicht als feste Zahlen im Code. — source: ADR-0002, ADR-0003, `.claude/docs/coding-standards.md`
- **`.unity`-, `.prefab`- und `.asset`-Dateien nie von Hand bearbeiten, `.meta`-Dateien nie anlegen oder löschen.** Änderungen laufen über Editor-Skripte oder eine Anleitung für den Editor. — source: CLAUDE.md, ADR-0002
- **Code auf Englisch, Kommentare auf Deutsch; Doc-Kommentare an allen öffentlichen APIs.** — source: CLAUDE.md, `.claude/docs/coding-standards.md`
- **Tests liegen in `Assets/Tests/EditMode/` und `Assets/Tests/PlayMode/`.** Außerhalb von `Assets/` kompiliert Unity sie nicht und meldet „0 Tests, bestanden“. — source: current-best-practices.md, `.claude/docs/directory-structure.md`
- **Batchmode nur bei geschlossenem Editor; `-runTests` nie mit `-quit` kombinieren.** — source: current-best-practices.md
- **Mesh Collider brauchen „Read/Write“ am Mesh,** sonst schlägt der Build fehl (6.6). — source: current-best-practices.md
- **Beim Einbau fremder Bibliotheken** (Steam, Sprachchat): Unitys eigene Basisklassen-Pakete (`System.Text.Json`, `System.Collections.Immutable` und weitere) überschreiben gleichnamige Assemblies eines Plug-ins (6.5). — source: current-best-practices.md

### Unresolved

- **UI-Technik:** `deprecated-apis.md` nennt UGUI (`Canvas`, `Text`, `Image`) als veraltet zugunsten von UI Toolkit, und in 6.6 sind `UxmlFactory`/`UxmlTraits` entfernt. Welche Technik das Projekt nutzt, entscheidet ADR-0007; bis dahin gibt es dazu keine Regel. Der Prototyp nutzt IMGUI.
- **„Legacy Particle System → Visual Effect Graph“** (`deprecated-apis.md`, Stand 6.3): unklar, ob damit das heutige Shuriken-System gemeint ist. Keine Regel, bis Effekte gebaut werden.
- **Addressables, Burst/Jobs, DOTS** (`current-best-practices.md`, Stand 6.3): Empfehlungen für andere Projektgrößen; hier weder installiert noch entschieden.
