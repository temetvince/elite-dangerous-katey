# Katey-Handbuch

Auch in [English](MANUAL.md), [Español](MANUAL.es.md), [Français](MANUAL.fr.md), [Português
(Brasil)](MANUAL.pt-BR.md) und [Русский](MANUAL.ru.md).

Katey ist ein Desktop-Begleiter für Elite Dangerous. Es liest die Journal-Dateien, die das Spiel auf deinem
eigenen Rechner schreibt, und bleibt beim Spielen auf dem Laufenden. Es plant Modifikationen gegen die
Materialien, die du tatsächlich hast. Es prüft deine gespeicherten Schiffs-Builds gegen die Schiffe und
Module, die du besitzt. Es folgt deiner Route und dem System, in dem du bist, und liest deinen Verlauf als
Hauptbuch. Nichts verlässt deinen Rechner, solange du es nicht in den Optionen erlaubst, wo jede ausgehende
Verbindung zu Beginn aus ist.

## Inhalt

- [Schnellstart](#schnellstart)
- [Das Fenster](#das-fenster): die [obere Leiste](#die-obere-leiste), die [untere Leiste](#die-untere-leiste),
  die [Suche](#suche), [die Plan-Schublade](#die-plan-schublade) und [die Live-Leiste](#die-live-leiste)
- [Die Seiten](#die-seiten): [Startseite](#startseite), [Ingenieure](#ingenieure), [ein
  Ingenieur](#ein-ingenieur), [Module](#module), [ein Modultyp](#ein-modultyp), [ein Bauplan](#ein-bauplan),
  [Materialien](#materialien), [ein Material](#ein-material), [Freischalten](#freischalten),
  [Flotte](#flotte), [ein Schiff](#ein-schiff), [ein Build](#ein-build), [System](#system), [Route](#route),
  [Statistik](#statistik) und [Optionen](#optionen)
- [Arbeiten mit EDSY](#arbeiten-mit-edsy)
- [Vorgefertigte Schiffe](#vorgefertigte-schiffe)
- [Was das Journal Katey nicht sagen kann](#was-das-journal-katey-nicht-sagen-kann)
- [Deine Daten](#deine-daten)

## Schnellstart

1. **Katey starten.** Die Veröffentlichung ist eine einzelne ausführbare Datei; nichts muss installiert
   werden. Es läuft immer nur ein Katey: Ein zweiter Start zeigt einen Hinweis und lässt das erste in Ruhe, da
   zwei sich gegenseitig die gespeicherten Daten überschreiben würden. Unter Windows wird der Journal-Ordner
   des Spiels von selbst gefunden. Sagt die Seite Optionen, dass kein Ordner gefunden wurde, zeige sie auf den
   Ordner mit den Dateien namens `Journal.*.log`, meist unter `Saved Games\Frontier Developments\Elite
   Dangerous`.
2. **Aufholen lassen.** Der erste Lauf spielt deinen gesamten Journalverlauf ab, älteste Datei zuerst, und
   zeigt seinen Fortschritt oben im Fenster und in der Leiste: wie viele Dateien gelesen sind, mit einem
   Balken, der sich Datei für Datei füllt. Nichts Live erscheint, bevor er fertig ist, du siehst also nie eine
   Zahl von unterwegs. Ein mehrjähriger Verlauf braucht ein paar Sekunden.
3. **Ingenieure anhaken.** Öffne **Ingenieure** in der oberen Leiste und hake die Ingenieure an, die du
   freigeschaltet hast und besuchen willst. Schalte oben auf dieser Seite **Nur Angebote ausgewählter
   Ingenieure anzeigen** ein, und jede Liste und jede Auswahl in Katey grenzt sich auf das ein, was diese
   Ingenieure rollen können, wobei jede Stufenauswahl bei der Stufe beginnt, die sie erreichen.
4. **Modifikationen planen.** Öffne **Module**, wähle einen Modultyp, wähle am gewünschten Bauplan eine Stufe
   und einen Experimentaleffekt und drücke **Hinzufügen**. Oder öffne die **Plan**-Schublade über die untere
   Leiste und füge einen EDSY-Build in ihr Importfeld ein; jedes modifizierte Modul des Builds wird ein
   Eintrag.
5. **Den Plan lesen.** Die Schublade führt jeden Eintrag mit denen auf, die ihn rollen können, jedes Material,
   das der Plan braucht, gegen das, was du hast, und ganz unten die Tauschgeschäfte bei den Materialhändlern,
   die decken, was dir fehlt. **Einkaufsliste kopieren** legt die Einkaufsliste in die Zwischenablage.
6. **Fliegen.** Die Live-Leiste rechts folgt dem Spiel: deine Position, die Route, der Treibstoff, die Körper,
   die sich in diesem System zu scannen lohnen, wobei die mit einem Material, das dir fehlt, markiert sind.
   **Abdocken** verschiebt sie in ein eigenes Fenster, das über dem Spiel bleibt.
7. **Einen Build gegen deine Flotte prüfen.** Öffne **Flotte**, füge einen EDSY-Build in das Feld für
   gespeicherte Builds ein und öffne den Build. Er wird Slot für Slot mit dem Schiff verglichen, das am besten
   zu ihm passt, und mit allem, was du besitzt, und ein Knopf plant die Modifikationslücke.

Alles Weitere in diesem Handbuch sagt, was jeder Teil des Fensters tut und wie du das Meiste daraus holst.

## Das Fenster

Katey ist ein Fenster mit einer Seite in der Mitte, einer Live-Leiste am rechten Rand und zwei weiteren
Leisten oben und unten. Es zeichnet seine eigene Titelleiste in den Farben der App und trägt Kateys Gesicht
als Symbol. Die Seiten sind nie schmaler als 960 Pixel, die Mindestbreite des Fensters ist also das plus die
Live-Leiste, solange diese angedockt ist; docke sie ab, und das Fenster passt auf einen 1080 Pixel breiten
Bildschirm. Es öffnet sich zentriert auf dem Hauptbildschirm, egal auf welchem Bildschirm es zuletzt war.

### Die obere Leiste

Die obere Leiste ist die Titelleiste des Fensters: Ziehe sie, um das Fenster zu verschieben, doppelklicke sie
zum Maximieren und nutze die drei Knöpfe an ihrem rechten Ende zum Minimieren, Maximieren und Schließen. Von
links nach rechts enthält sie:

- **KATEY**, die Wortmarke, die die [Startseite](#startseite) öffnet.
- **Zurück** und **Vorwärts**, die wie in einem Browser funktionieren und dich dorthin zurückbringen, wo du
  gescrollt hattest.
- Die Seitenlinks: **Ingenieure**, **Module**, **Materialien**, **Freischalten**, **Flotte**, **Route**
  und **System**. Der, in dessen Bereich du bist, leuchtet, sodass die Seite eines Ingenieurs
  Ingenieure erleuchtet und ein Bauplan Module. Drücke einen Link erneut, während seine eigene Seite
  offen ist, und du kehrst zu der Seite zurück, auf der du vorher warst.

### Die untere Leiste

- **Sprache**, der Globus ganz links, zeigt die Sprache, in der Katey läuft, und öffnet die Liste der
  Sprachen, in denen es ausgeliefert wird: Englisch, Deutsch, Spanisch, Französisch, Portugiesisch (Brasilien)
  und Russisch, jede in ihrem eigenen Namen, mit „Systemsprache“ zuerst. Katey folgt deiner Windows-Sprache,
  bis du eine wählst; die Wahl wird gemerkt, und das ganze Fenster wechselt auf einmal. Die eigenen Namen des
  Spiels wechseln mit: Materialien, Baupläne, Experimentaleffekte, Modultypen, Technologiehändler-Gegenstände,
  Planetenklassen, Atmosphären und Vulkanismus sowie die Gesamtwerte. Spezies-, Schiffs- und Modulnamen, die
  aus deinem Journal kommen, folgen der Sprache des Spielclients. Dieses Handbuch hat eine Fassung in jeder
  Sprache, oben verlinkt.
- **Optionen** daneben öffnet die Seite [Optionen](#optionen). Drücke es erneut, während die Optionen offen
  sind, und du kehrst zu der Seite zurück, auf der du warst.
- **Statistik** daneben öffnet die Seite [Statistik](#statistik) und bringt dich, erneut gedrückt, während
  sie offen ist, zu der Seite zurück, auf der du warst. Sie steht hier und nicht in der oberen Leiste,
  damit die Seitenlinks in jeder Sprache in eine Zeile passen.
- Das **Suchfeld** quer über die Mitte; siehe [Suche](#suche).
- **Plan** rechts öffnet und schließt die [Plan-Schublade](#die-plan-schublade). Die Zahl darauf ist, wie
  viele Einträge der Plan enthält.

### Suche

Tippe in das Suchfeld, oder drücke **Ctrl K**, um hineinzuspringen, um einen Ingenieur, einen Modultyp, einen
Bauplan, ein Material, ein von dir besuchtes System, eines deiner Schiffe oder einen gespeicherten Build zu
finden. Die Ergebnisse steigen beim Tippen über dem Feld auf. **Enter** öffnet das erste Ergebnis, die
Pfeiltasten bewegen sich durch sie, und **Escape** leert das Feld. Jedes Ergebnis öffnet seine eigene Seite.

### Seiten, Links und gemerkte Einstellungen

Jede Seite beschreibt eine Sache oder listet Dinge einer Art. Jede Erwähnung von etwas anderem auf einer Seite
ist ein Link, der dessen Seite öffnet: Ein Systemname öffnet das System, der Name eines Ingenieurs öffnet den
Ingenieur, ein Material-Chip öffnet das Material. Die kleinen Beschriftungen über dem Titel einer Seite sind
Links zurück hinauf zu dem Index, zu dem sie gehört.

Drei Mechanismen verhalten sich auf jeder Seite gleich, und jeder merkt sich seinen Zustand zwischen den
Läufen:

- **Sortieren.** Klicke eine Spaltenüberschrift, um die Tabelle danach zu sortieren; klicke sie erneut, um die
  Richtung umzukehren.
- **Abschnitte.** Eine lange Seite zeigt einen Abschnitt zugleich, gewählt über die Reiter unter ihrem Titel.
- **Gruppen.** Eine Seite, die Dinge nach Gruppen zusammenfasst, öffnet und schließt jede Gruppe mit einem
  Klick, mit **Alle ausklappen** und **Alle einklappen** daneben.

Filter, Schalter und der Zustand jeder Aufklappung werden ebenfalls gemerkt, sodass eine Seite so zurückkommt,
wie du sie verlassen hast.

### Die Plan-Schublade

Der Plan ist die Liste der Modifikationen, die du vorhast. Die Schublade öffnet sich über jeder Seite über den
Knopf **Plan** und enthält:

- **Die Einträge.** Jeder hat:
  - einen Schalter, um ihn aus den Summen herauszulassen, ohne ihn zu löschen;
  - eine Stufenauswahl und, bei einem Typ, der Experimentaleffekte annimmt, eine Experimentaleffekt-Auswahl
    und den Schalter **Nur Experimentaleffekt**; ein Typ ohne zeigt keines von beiden;
  - die je Stufe erwarteten Rolls;
  - die Ingenieure, die ihn rollen können, jeder ein Link, eingefärbt nach deinen Haken wie überall in Katey.
    Ein Eintrag, den niemand von den Angehakten rollen kann, trägt eine Warnung.

  Der Schalter **Nur Experimentaleffekt** macht aus einem Eintrag einen, der nur die Materialien des
  Experimentaleffekts kostet, für ein Modul, das seinen Bauplan bereits hat. Eine
  Technologiehändler-Freischaltung, die du auf der Seite [Freischalten](#freischalten) angehakt hast, ist
  ebenfalls ein Eintrag, mit ihren festen Materialien und den Waren, die sie außerdem von einem Markt braucht.

  Der Plan folgt dem, was du beim Ingenieur tust:

  - jeder Roll, den das Spiel an einem Modul mit Bauplan und Typ eines Eintrags meldet, nimmt dieser Stufe
    einen Roll ab oder räumt sie, wenn die Stufe vollendet ist;
  - der Experimentaleffekt verschwindet vom Eintrag, sobald er auf dem Modul ist, bei welcher Stufe auch
    immer;
  - sobald das Modul auf der Zielstufe steht und nichts mehr gewünscht ist, verlässt der Eintrag den Plan.

  Nur Rolls nach dem Hinzufügen des Eintrags zählen, sodass früher gemachte Modifikationen nie einen Eintrag
  entfernen.
- **Die Summen.** Jedes Material, das der Plan braucht, mit dem, was du hast, und dem, was dir fehlt,
  gruppiert nach roh, hergestellt und kodiert. **Einkaufsliste kopieren** legt die Liste als reinen Text in
  die Zwischenablage.
- **Die Tauschgeschäfte.** Was du bei den Materialhändlern abgeben musst, um alles zu decken, was dir fehlt,
  Tausch für Tausch, gruppiert nach Händler. Siehe [Materialien](#materialien) dazu, wie Tauschgeschäfte
  gewählt werden.
- **Der EDSY-Import.** Füge einen aus EDSY exportierten Build ein, drücke **Export einlesen**, hake jedes
  Modul ab, das du nicht willst, und drücke **Zum Plan hinzufügen**. Siehe [Arbeiten mit
  EDSY](#arbeiten-mit-edsy).

**Plan leeren** leert den Plan nach einem zweiten Druck, sodass ein Ausrutscher ihn nicht verlieren kann.

### Die Live-Leiste

Die Leiste folgt dem Spiel am rechten Rand des Fensters. Sie zeigt:

- **Wo du bist** und was das Schiff tut: angedockt, im Supercruise, gelandet und so weiter.
- **Die Route**: den Halt, an dem du bist, und die nächsten paar, der Rest zusammengefasst. Der nächste
  tankbare Stern und jeder Ingenieurshalt werden hervorgehoben. Die zurückgelegte Entfernung steht gegen die
  ganze und als Prozentsatz, mit dem Tankbalken, wie die [Routenseite](#route) ihn zeichnet. Das
  Treibstoffbild sagt, wie viele Sprünge du machen kannst, bevor du tanken musst, und bis zu welchem Stern
  spätestens. Wird eine Spansh-Route gehalten, erscheint der nächste Wegpunkt mit einem Knopf **Kopieren** für
  die Galaxiekarte.
- **Dieses System**: der Scan-Fortschritt und die geschätzten Erlöse, mit jedem gescannten Körper, der
  Oberflächenmaterialien oder biologische Signale trägt. **Erkundungsdaten unverkauft** ist, was die noch an
  Bord befindlichen Daten dieses Systems zahlen würden. Das sind die Scans, die du nicht verkauft hast, mit
  dem Erstentdeckungsbonus, wo ein Körper von dir zu entdecken war, und die Spezies, die du vollständig
  beprobt und nicht verkauft hast, mit dem Ersterfassungsbonus, wo das Spiel sagte, niemand habe sie erfasst.
  Die Systemseite ergänzt, was dir bereits gezahlt wurde. Die Überschrift und der Ring werden grün, sobald
  jeder Körper gescannt ist.
  - Bevor ein Körper kartiert ist, ist sein Leben eine Zeile: wie viele Gattungen dort leben könnten und was
    sie wahrscheinlich wert sind, mit und ohne Ersterfassungsbonus. Einmal sondiert, führt der Körper seine
    Gattungen auf, die reichsten zuerst. Jede Zeile ist die Gattung, ihre wahrscheinlichste Spezies mit den
    Chancen oder ihrer Probenstufe und was sie zahlt, mit dem Bonus, wo er gilt. Die Zeile ist violett nach
    der ersten Probe, blau nach der zweiten und grün, sobald alle drei genommen sind.
  - Die Schwerkraft erscheint ab 1,0 g als Marke, ab 1,5 g in Rot.
  - Eine Wahl aus drei sagt, wofür die Liste ist, jede nach Entfernung von der Ankunft, die nächsten zuerst.
    **Landbar** listet jeden landbaren Körper mit seinem Leben und seinen Materialien. **Biologie** listet die
    Körper mit Leben und zeigt nur ihr Leben, sodass ein landbarer Körper mit Materialien allein nicht
    erscheint. **Mineralien** listet die Körper mit Oberflächenmaterialien und zeigt nur ihre Materialien; ein
    Körper mit Leben und Materialien wird aufgeführt, seine Biologie-Zahl weiterhin im Titel, seine Gattungen
    nicht. Landbar ist die Vorgabe. Die Wahl wird gemerkt.
  - In der Ansicht Biologie sagt eine zweite Wahl aus zwei, welche Körper mit Leben ausgelassen werden. Die
    Wahl wird gemerkt. Bedingungen, zu denen Katey keine Aufzeichnung hat, werden nie ausgeblendet.
  - **Alles Leben** listet jeden.
  - **Nur-Bakterien überspringen** lässt Körper aus, deren Leben allein Bakterien sind: nach den Gattungen des
    Scanners, sobald kartiert, davor bei einem einzelnen Signal, das zu mindestens vier Fünfteln
    wahrscheinlich Bakterien ist.

Die Leiste wählt die Route oder die Körper von selbst. Sie zeigt die Körper vom Moment deiner Ankunft an, mit
einer Zeile darüber zur Route: der nächste Sprung, wie viele Sprünge übrig sind und wo die Route endet. Sie
wechselt zur Route, wenn der Antrieb zum Sprung lädt, und behält sie bis zu dem Moment, in dem du am nächsten
Stern ankommst. Im Supercruise kann ein Laden nur ein Sprung sein, also wechselt die Leiste sofort; im
Normalraum kann dasselbe Laden für den Supercruise sein, also wartet die Leiste auf den Countdown des Sprungs.
Das Spiel löscht eine Route mitten in ihrem letzten Sprung, und die Leiste zeigt diesen Sprung weiter, bis er
gemacht ist. Was du anvisiert hast, bewegt die Leiste nicht: Das Spiel visiert bei jeder Ankunft entlang einer
Route von selbst den nächsten Sprung an, ein anvisiertes System sagt also nichts darüber, ob du hier
erkundest. Der nützliche Abschnitt ist ohne Klick oben.

Visiere ein System an, das nicht auf deiner geplanten Route liegt, und das Spiel plant seine Route neu durch
dieses System, schreibt seine Routendatei aber nicht sofort neu. Die Leiste und die Seite Route sagen
„Umgeleitet über“ dieses System, mit den übrigen Sprüngen aus der eigenen Zählung des Spiels statt aus der
veralteten Datei, bis die Datei aufgeholt hat.

**Abdocken** verschiebt die Leiste in ein eigenes Fenster, das über anderen Fenstern bleibt, für einen zweiten
Monitor oder eine Stream-Aufnahme. Ein einzeiliger Streifen bleibt im Hauptfenster, mit dem Scan-Fortschritt
und diesen Bedienelementen:

- **Leiste zurückholen** dockt sie wieder an. Das Schließen des Leistenfensters tut dasselbe.
- **Position zurücksetzen** setzt das Leistenfenster auf den Hauptbildschirm in seiner Standardgröße zurück.
  Andernfalls merkt es sich, wo du es gelassen hast und wie groß du es gemacht hast.
- **Overlay** macht das Fenster weitgehend transparent mit heller Schrift, sodass es über einem Spiel liegen
  kann, das im Fenster oder rahmenlos läuft. Ein Spiel im exklusiven Vollbild verdeckt jedes andere Fenster,
  dieses eingeschlossen.
- **Durchklicken** lässt Klicks durch die Leiste an das Spiel gehen. Solange es an ist, werden die Knöpfe,
  Links und Schalter der Leiste ausgeblendet, da nichts darauf gedrückt werden kann, und der Fensterrand
  verschwindet ebenfalls, da nichts daran gegriffen werden kann, sodass nur die eigene Tafel der Leiste über
  dem Spiel zu sehen ist. Schalte Durchklicken über den Streifen aus, um sie zurückzubekommen. Es funktioniert
  nur unter Windows.

## Die Seiten

Ein Abschnitt je Seite.

### Startseite

Die Startseite: eine Kachel für jeden Bereich mit einer Zeile dazu, was er enthält, und eine Tafel, die sagt,
was der Plan enthält und ob das Journal beobachtet wird. Fünf Zahlen laufen oben entlang:

- **Materialien fehlen**: wie viele Materialien dem Plan noch fehlen.
- **Verdient, 7 Tage**: in der letzten Spielwoche verdiente Credits.
- **Sprünge übrig**: übrige Sprünge auf der Route und ob der Treibstoff dafür reicht.
- **Körper gescannt**: der Scan-Fortschritt dieses Systems.
- **Daten an Bord**: was die noch nicht verkauften Scans und Proben wert sind, gezählt seit deinem letzten
  Verkauf und durch einen Tod zurückgesetzt. Körper, die dir eine Navigationsbake übergeben hat, zählen nicht:
  Das Spiel zahlt nichts für Daten, die es verschenkt hat, und die Systemseite bewertet sie ebenfalls mit
  nichts.
  - Wenn du eine Probe vollendest, sagt das Journal, ob jemand diese Spezies auf diesem Körper erfasst hatte.
    Hatte es niemand, zählt Katey den Ersterfassungsbonus, das Fünffache des Grundwerts, als wahrscheinlich.
    Ein anderer Commander kann sie immer noch zuerst verkaufen, die Zahl ist also eine Wahrscheinlichkeit,
    kein Versprechen.
  - Vor jeder Probe springt die Betreten-Markierung des Scans ein. Leben wird zu Fuß erfasst, ein Körper, den
    niemand betreten hat, kann also von niemandem erfasst worden sein. Katey zählt den Bonus dort als
    wahrscheinlich und nicht, wo jemand anderes gelandet ist.
  - Deine eigene Landung nimmt ihn nicht weg. Auf einem Körper, den du als Erster betreten hast, zählt eine
    Spezies, die du nicht beprobt hast, weiterhin fünffach, bis du die Exobiologiedaten dieses Körpers
    verkaufst. Nach diesem Verkauf nimmt Katey für das, was du dort unbeprobt gelassen hast, nichts mehr an,
    da ein anderer Commander seitdem gelandet sein kann. Eine Spezies, die du beprobt hast, hat das Wort des
    Spiels, mit der Probe gegeben, und behält oder verliert den Bonus danach.

Farbe bedeutet überall etwas: Materialstufen, die drei Inventare, die vier Plätze, an die ein Modul passt,
Sternklassen, wie die Galaxiekarte sie malt, und die Urteile bereit, teilweise und fehlend.
Vorhanden-gegen-Bedarf-Balken sitzen unter jedem Material, das der Plan will, und der Treibstoffbalken auf der
Routenseite und in der Leiste zeigt den Tank mit dem, was der nächste Sprung verbrennt, in Blau über dem
Orange dessen, was bleibt, wie die eigene Anzeige des Spiels es zeichnet.

### Ingenieure

Jeder Ingenieur, der dir nächste zuerst, mit seiner Entfernung und ob er auf deiner Route liegt. Hake die
Ingenieure an, die du freigeschaltet hast und nutzen willst; die Haken steuern die Warnungen des Plans und
alles, was der Schalter oben auf der Seite eingrenzt.

**Nur Angebote ausgewählter Ingenieure anzeigen** ist der wichtigste Schalter in Katey. An, bieten der
Modulindex und jede Auswahl von Bauplan, Stufe und Experimentaleffekt nur, was die angehakten Ingenieure
rollen können, und jede Stufenauswahl beginnt bei der Stufe, die sie erreichen. Aus, wird alles angeboten, und
Stufenauswahlen beginnen bei der höchsten Stufe. So oder so warnt der Plan weiterhin vor einem Eintrag, den
niemand von den Angehakten rollen kann. Der Schalter wird gemerkt.

Wo immer Katey einen Ingenieur nennt, ist der Name ein Link zu seiner Seite. Mit dem Schalter aus steht jeder
Name in schlichtem Text. Mit ihm an steht ein angehakter Ingenieur im Blau des HUD und ein nicht angehakter
gedimmt, sodass sich jede Liste von Ingenieuren auf einen Blick lesen lässt.

Ein Suchfeld filtert die Liste nach Namen, und die Spalten sortieren.

### Ein Ingenieur

Ein Ingenieur: wo er ist und wie weit das von dir entfernt ist, und ob er ein Halt auf deiner Route ist. Jeder
Bauplan, den er anbietet, ist mit der Stufe, bei der er endet, und einem Knopf **Hinzufügen** aufgeführt, der
ihn plant. Die Seite sagt auch, welche deiner Planeinträge er rollen kann und wie viele deiner Module in
seinem System gelagert sind.

### Module

Jeder Modultyp, der modifiziert werden kann, in einer Gruppe je Platz am Schiff: kerninterne Module, optionale
interne Module, Waffenaufhängungen und Hilfsaufhängungen. Klicke eine Gruppe, um sie zu öffnen, oder nutze
**Alle ausklappen** und **Alle einklappen**. Mit **Nur Angebote ausgewählter Ingenieure anzeigen** an führt
eine Gruppe nur die Typen auf, an denen deine angehakten Ingenieure einen Bauplan rollen können. Die offenen
Gruppen werden gemerkt.

### Ein Modultyp

Eine Art Modul, etwa der Frameshift-Antrieb oder ein Strahlenlaser. Seine Abschnitte:

- **Baupläne.** Jeder Bauplan mit der Stufe, die deine gewählten Ingenieure erreichen, jedem, der ihn
  anbietet, und dessen Entfernung, einer Stufenauswahl und einem Knopf **Hinzufügen**. Die Auswahlen folgen
  dem Ingenieursschalter.
- **Experimentaleffekte.** Die Experimentaleffekte, die der Typ annimmt, und was jeder kostet. Stufe 0 in
  einer Bauplanzeile plant den Experimentaleffekt allein, für ein Modul, das seinen Bauplan bereits hat.
- **Varianten.** Jede Größe und Klasse, die das Spiel verkauft, mit den von dir besessenen markiert.
- **Du besitzt.** Die Module dieses Typs, die du besitzt, verbaut oder gelagert, wo sie sind und wie sie
  modifiziert sind.
- **In deinen Builds.** Was deine gespeicherten Builds von diesem Typ verlangen.

### Ein Bauplan

Ein Bauplan über jeden Modultyp, für den er gilt: was ein Roll auf jeder Stufe kostet, wer ihn für welche
Typen anbietet, und eine Zeile, die ihn zum Plan hinzufügt. Wähle den Modultyp, die Stufe und einen
Experimentaleffekt und drücke **Hinzufügen**, oder drücke **Hinzufügen** in der Zeile einer Stufe. Können
deine gewählten Ingenieure den Bauplan nicht erreichen, während der Ingenieursschalter an ist, sagt die Zeile
das, statt eine Stufe anzubieten, die du nicht rollen kannst.

### Materialien

Jedes Material mit dem, was du hast, was der Plan braucht und was dir fehlt. Ein Schalter je Inventar zeigt
oder verbirgt rohe, hergestellte und kodierte Materialien, ein Namensfilter grenzt die Liste ein, und **Nur
Fehlendes** behält nur die, die dir fehlen. All das wird gemerkt.

Jede Zeile nimmt außerdem eine **Reserve** in der Spalte Behalten: die Zahl, die du mindestens behalten
willst, und einen Schalter **Handel**, der sagt, ob das Material überhaupt bei einem Händler abgegeben werden
darf. Die Reserven werden gespeichert.

Unter der Tabelle sagt **Bei den Materialhändlern**, was abzugeben ist, um alles zu decken, was dir fehlt, und
jede Reserve wieder aufzufüllen, Tausch für Tausch mit dem Kurs, gruppiert nach Händler. Der Planer gibt nur
aus, was du über der Reserve hältst, und nie ein Material, dessen Schalter Handel aus ist. Er bevorzugt die
Tauschgeschäfte, die am wenigsten Wert verlieren und aus deinen vollsten Stapeln kommen, und sagt, warum, wenn
etwas nicht gedeckt werden kann.

Die Kurse sind die des Spiels: sechs für eins eine Stufe aufwärts, eins für drei abwärts, und noch einmal
sechsmal so viel über Familien hinweg. Unter jedem Tausch nennt eine zweite Zeile die Familien der beiden
Materialien, wie die Händler sie gruppieren, etwa „Familie Eisen → Familie Kohlenstoff“ oder „beide in der
Familie Wake-Scans“, gefolgt davon, wie viel die ganzen Stapel übrig lassen. Jedes in einem Tausch genannte
Material ist ein Link, der auf seiner Zeile hier landet. Dieselbe Liste sitzt unten in der Plan-Schublade.

### Ein Material

Ein Material: was du hast gegen das, was der Plan braucht, welche Planeinträge es ausgeben, was sonst im Spiel
es nutzt, Bauplanstufen wie Experimentaleffekte, und welche gescannten Körper in deinem aktuellen System es
tragen.

### Freischalten

Alles, was der Guardian- und der menschliche Technologiehändler verkaufen, ein Händler zugleich, in
einklappbaren Gruppen: die Guardian-Module, -Waffen, modifizierten Waffen und Jäger sowie die Module, Waffen
und vormodifizierten Module des menschlichen Händlers. Klicke eine Gruppe, um sie zu öffnen oder zu schließen,
oder nutze **Alle ausklappen** und **Alle einklappen**; die offenen Gruppen werden gemerkt. Jede Zeile zeigt,
was die Freischaltung an Materialien kostet, nach Stufe und Inventar, und an Waren, die du auf einem Markt
kaufst.

Hake eine Freischaltung an, und sie kommt in den Plan. Ihre Materialien zählen in den Summen des Plans und in
den Tauschgeschäften bei den Materialhändlern, und ihre Waren erscheinen in der Schublade unter „Auf einem
Markt kaufen“. Das Abzeichen jeder Zeile sagt, wo sie steht:

- **Bezahlbar**, in Orange: Was der Plan in deinem Inventar übrig lässt, deckt sie.
- **Fehlt**, in Rot: Es deckt sie nicht, und die Zeile sagt, um wie viel. Die Zeile ist gedimmt, lässt sich
  aber trotzdem anhaken; sie wird dann ein fehlender Eintrag wie jeder andere, und die Schublade sagt, was zu
  sammeln ist.
- **Geplant**: Sie ist im Plan. Hake sie ab, um sie herauszunehmen.
- **Freigeschaltet**, in Grün: Das Journal verzeichnet, dass du sie gekauft hast, oder du hast ein Modul
  gekauft oder besitzt eines, das sie freischaltet, was den Kauf beweist, selbst wenn er vor den Journalen
  lag, die das Spiel aufbewahrt hat. Sie lässt sich nicht anhaken.

Der Plan kommt zuerst. Das Anhaken einer Freischaltung beansprucht ihre Materialien, sodass eine andere, die
dieselben brauchte, auf Fehlt kippen kann; das Abhaken bringt die andere zurück. Unter der Tabelle liegen die
Materialien und Waren für jede geplante Freischaltung gegen das, was du hast.

Einige vormodifizierte Freischaltungen, etwa der modifizierte FSA V1, werden im Journal nicht genannt, Katey
kann also nicht erkennen, ob du sie gekauft hast; eine solche Zeile sagt das.

### Flotte

Deine Schiffe, deine gespeicherten Builds und die Module, die du besitzt und kein Build nutzt. Ihre
Abschnitte:

- **Schiffe.** Jedes Schiff, das du besitzt, wo es ist und ob es aus einem Paket eingelöst wurde; siehe
  [Vorgefertigte Schiffe](#vorgefertigte-schiffe). Kosmetische Gegenstände wie Lackierungen und Schiffs-Kits
  werden überall in Katey ausgelassen.
- **Gespeicherte Builds.** Deine gespeicherten Builds, jeder mit einem Urteil gegen alles, was du besitzt,
  gemäß der Teilungsregel. Das ist Besitz, nicht die Ausrüstung eines Schiffs; öffne den Build, um ihn Slot
  für Slot mit einem Schiff zu vergleichen.
  - **Alles vorhanden**: Jedes Modul, das der Build will, ist irgendwo in deiner Flotte oder im Lager.
  - **Teilweise**: Einige sind vorhanden, aber nicht ganz durchmodifiziert.
  - **Fehlend**, mit der Anzahl.

  Wenn mehr als ein eigenes Modul passen würde, wird das zuerst genannt, das bereits in einem Schiff vom
  Schiffstyp des Builds verbaut ist, dann eines im Lager, dann eines in einem anderen Schiff. Unter denen am
  selben Ort kommt das dir nächste zuerst, nach den Koordinaten, die das Journal für sein System hat, mit
  deinem eigenen System zuerst und einem nie besuchten Ort zuletzt.

  Ein Einfügefeld nimmt einen neuen Build aus EDSY an, und ein Build kann hier entfernt werden. Ein
  eingefügter Build mit demselben Namen wie ein gespeicherter Build für denselben Schiffstyp ersetzt diesen
  Build, statt neben ihm zu stehen: Der Knopf fragt zuerst, und ein zweiter Druck ersetzt ihn an Ort und
  Stelle. Derselbe Name auf einem anderen Schiffstyp ist ein neuer Build.
- **Eigene Module, die kein Build nutzt.** Die eigenen Module, die kein Build beansprucht, unter einer
  **Teilungsregel**, die sagt, ob Module zwischen Schiffen wandern dürfen, nur zwischen Schiffen desselben
  Schiffstyps oder gar nicht, und mit Filtern nach Standort, Platz am Schiff und Modifikationszustand.

Die Flottenliste und die Liste der gelagerten Module werden vom Spiel neu geschrieben, wenn du irgendwo mit
Werft oder Ausrüstung andockst, sie sind also so frisch wie dein letzter solcher Besuch.

### Ein Schiff

Ein Schiff, das du besitzt: seine verbauten Module, jedes mit einer Auswahl, die Modifikationen für genau
dieses Modul plant, und **Für EDSY kopieren**, das den eigenen Datensatz des Spiels zu dem Schiff in die
Zwischenablage legt, zum Einfügen in den Import von EDSY.

Der Knopf der Auswahl sagt, wo die Arbeit steht:

- **Zum Plan hinzufügen**, wenn die Wahl eine Arbeit ist.
- **Im Plan**, solange der Plan Bauplan, Stufe und Experimentaleffekt, die für diesen Slot gewählt sind,
  bereits enthält.
- **Bereits modifiziert**, solange das Modul sie bereits hat. Wähle eine höhere Stufe, einen anderen Bauplan
  oder einen anderen Experimentaleffekt, und es ist wieder eine Arbeit.

Ein Modul, das mit einem Paketschiff kam, ist als **Vorgefertigtes Modul** markiert, weil seine Modifikation
bei diesem Schiff bleibt. Bei einem vorgefertigten Schiff sagt die Seite, ob es noch wie eingelöst ist oder
wie viele Slots sich geändert haben; diese Zeile öffnet sich zu einer Tabelle dessen, was jeder geänderte Slot
ursprünglich hatte und jetzt enthält. Siehe [Vorgefertigte Schiffe](#vorgefertigte-schiffe).

### Ein Build

Ein gespeicherter Build, verglichen mit dem, was du hast. Seine Abschnitte:

- **Mit einem Schiff vergleichen.** Der Build Slot für Slot gegen eines deiner Schiffe, beginnend mit dem, das
  bereits am besten zu ihm passt; wähle ein anderes aus der Liste. Jeder Slot sagt, ob er gekauft werden muss,
  ob er Modifikation braucht und ob deine gewählten Ingenieure sie können, und ob ein Modul, das du bereits
  anderswo besitzt, den Slot stattdessen füllen würde, innerhalb eines Orts und Standorts deiner Wahl.

  **Die Modifikationsänderungen für dieses Schiff planen** hängt jeden Modifikationsunterschied an den Plan
  an; ein Slot, dem nur sein Experimentaleffekt fehlt, wird ein Eintrag allein für den Effekt. Der Knopf zählt
  nur, was der Plan noch nicht enthält, und tritt zurück, und sagt das, sobald jede Änderung geplant ist.

  Jeder Eintrag ist mit diesem Schiff und Slot gestempelt, wie die Schiffsseite ihre eigenen stempelt, sodass
  die beiden Seiten darin übereinstimmen, was für dieses Schiff bereits geplant ist. Dieselbe Änderung, für
  ein anderes Schiff geplant, zählt hier weiterhin als ungeplant. Die beiden Knöpfe auf dieser Seite erkennen
  die Einträge des jeweils anderen: Die Modifikation eines Slots, als Lücke in deinem Besitz geplant, ist
  dieselbe Arbeit wie die Änderung für dieses Schiff, und die Schiffsseite zählt den Eintrag eines Builds für
  ihren Schiffstyp genauso.
- **Gegen alles, was du besitzt.** Jedes Modul, das der Build will, gegen jedes Modul, das du besitzt, gemäß
  der Teilungsregel der Flotte. Ein Modul mit dem richtigen Bauplan auf einer niedrigeren Stufe oder ohne den
  Experimentaleffekt ist **teilweise** statt fehlend. Ebenso ein Modul auf der richtigen Stufe, das noch nicht
  zu seiner vollen Wirkung gerollt ist; sein Modifikationstext sagt, wie weit es ist, etwa „Verstärkt G3,
  25%“, und das Planen der Lücke verlangt nur die auf dieser Stufe noch ausstehenden Rolls. Ein Modul, das
  modifiziert wurde, bevor das Spiel diese Zahl festhielt, gilt als vollständig.

  Der Status ist danach eingefärbt, wo das Modul ist:

  - grün: in einem Schiff dieses Schiffstyps verbaut;
  - blau: im Lager;
  - gelb: in einem Schiff eines anderen Schiffstyps verbaut;
  - orange: vorhanden, aber mit nötiger Änderung, oder unsicher;
  - rot: fehlend.

  Ein Modul, das mit einem Paketschiff kam, kann es nicht verlassen, zählt also nur für Builds vom Schiffstyp
  dieses Schiffs und ist dann als **vorgefertigt** markiert. **Die Lücken in deinem Besitz planen** tut für
  diese Liste, was der Knopf darüber für ein Schiff tut, und tritt genauso zurück. Die beiden Zahlen
  unterscheiden sich mit Absicht: Ein Slot kann eine Änderung für dieses Schiff sein, während ein Modul, das
  du anderswo besitzt, ihn bereits deckt.

Ein Modul wird abgeglichen, wo immer es in einem Slot derselben Art sitzt, sodass ein Modul, das in eine
andere optionale Bucht oder Waffenaufhängung verlegt wurde, weiterhin zählt. **Für EDSY kopieren** legt den
Build in der Form in die Zwischenablage, die EDSY importiert: der Export, den du eingefügt hast, genau wie du
ihn eingefügt hast, sodass auf dem Rückweg nichts verloren geht, was EDSY geschrieben hat.

### System

Für das System, in dem du bist: jeder Körper, den du gescannt hast, bei diesem Besuch oder einem früheren, den
dein Journalverlauf enthält, mit einem Abzeichen **gescannt**, das bei einem früheren Besuch den Tag nennt.
Ist die Systemabfrage in den Optionen erlaubt, kommen die Körper, die Spansh kennt und du nicht gescannt hast,
mit einem Abzeichen **von Spansh** in die Liste. Sie kommen mit ihren Klassen, Bedingungen, Signalen und
bekannten Gattungen. Ihr Leben wird bewertet wie dein eigenes; ihre Kartografie ist dir nichts wert, bis du
sie selbst scannst. Sie sagen nichts über Entdeckung, Kartierung oder Landung, es wird für sie also kein
Erstbonus irgendeiner Art angenommen. Für ein früher besuchtes System sagt die Seite, wo es ist und wann du
dort warst.

Über den Körpern fassen drei Zeilen das System zusammen.

- **Erkundungsdaten unverkauft** ist, was die noch an Bord befindlichen Daten dieses Systems zahlen würden,
  Boni gezählt, wo sie gelten, dann wie viel davon für das Erstsein ist. Sobald Daten dieses Systems verkauft
  wurden, endet die Zeile mit dem, was dir dafür gezahlt wurde. Ein Verkauf eines Systems wird genommen, wie
  das Spiel ihn gemeldet hat; ein Verkauf mehrerer wird nach Kateys Schätzung jedes einzelnen unter ihnen
  aufgeteilt.
- Die zweite Zeile schlüsselt die Zahl auf. Kartografie erscheint wie gescannt ohne Bonus, dann mit dem
  Erstentdeckungsbonus, wo ein Körper von dir zu entdecken war. Exobiologie erscheint nach den Gattungen des
  Scanners, „etwa“, bis jede Gattung eine beprobte Spezies hat, dann mit dem Ersterfassungsbonus, wo niemand
  den Fuß gesetzt hatte.
- Die dritte sagt, wie viele Körper niemand vor dir entdeckt hatte, auf wie vielen du gelandet bist und wie
  viele landbare Körper noch niemand betreten hat, dich eingeschlossen.

Die Körper sind Stern für Stern aufgeführt: Auf jeden Stern, benannt nach seiner Klasse und ob du an ihm
tanken kannst, folgen die Körper, die ihn umkreisen, die nächsten zuerst. Das Sortieren nach einer Spalte
bringt stattdessen die ganze Liste in diese Reihenfolge. Jede Spalte merkt sich ihre eigene Richtung, sodass
die Rückkehr zu einer sie so vorfindet, wie du sie verlassen hast; das gilt für jede Tabelle in Katey.

Jeder Körper zeigt seine Signale und Oberflächenmaterialien, dann seine Gattungen, die wertvollsten zuerst,
mit ihren Spezies-Vorhersagen und geschätzten Erlösen. Wo niemand den Körper betreten hat, trägt jede
Schätzung die Zahl mit dem Ersterfassungsbonus in Klammern, als „2,1 Mio. (10,5 Mio.) Cr“. Eine Gattung ist
violett nach ihrer ersten Probe, blau nach der zweiten und grün, sobald alle drei genommen sind. Die Stufe
wird gemerkt, wenn du in das System zurückkommst und nachdem du verkaufst, wie das Spiel sie behält. Eine
andere Spezies vor der dritten Probe zu beginnen, verliert die angefangene, wie beim Probennehmer. Unter den
Gattungen steht die eigene Exobiologie-Zahl des Körpers, „etwa“, bis jede Gattung auf ihm eine beprobte
Spezies hat. Sobald eine Spezies dort alle drei Proben hat, sagt eine grüne Zeile unter dieser Zahl, was du
auf dem Körper gesammelt hast und was daraus mit dem Ersterfassungsbonus wird.

Die Karte und die Abzeichen jedes Körpers heben hervor, was zählt, bevor du dich auf ihn festlegst.

- Der Rahmen der Karte sagt, was der Körper enthält: grün für Leben, blau für Oberflächenmaterialien; ein
  Körper mit beidem bekommt Grün. Die Karte eines Sterns ist orange gerahmt und trägt ein Abzeichen **Stern**,
  mit **tankbar** in Grün oder **nicht tankbar** in Gelb daneben.
- Drei Statusplätze, immer in derselben Reihenfolge, jeder aus den Markierungen, die das Spiel bei jedem Scan
  schreibt: violett für Entdeckung, türkis für Kartierung, grün für Betreten. Eine Pille hat drei
  Erscheinungen, sodass ein frischer Körper aus gefüllten Pillen besteht, ein von dir bearbeiteter aus
  farbigen Wörtern und ein von anderen bearbeiteter grau ist, und du erkennst, was was ist, ohne die Wörter zu
  lesen.
  - Gefüllt mit einer Linie darunter ist der Bonus noch offen: **nicht kartiert**, wo der Kartierungsbonus
    liegt, oder **nicht betreten**.
  - Umrandet mit Wörtern in der Farbe des Platzes ist der Bonus deiner: **von dir entdeckt**, **von dir
    kartiert** oder **von dir betreten**.
  - Umrandet mit grauen Wörtern hat ihn jemand anderes genommen: **entdeckt**, **kartiert**, **betreten**.
- Die Entdeckung ist nie offen, da du den Körper gescannt hast. Das Spiel schreibt eine Erstentdeckung gut,
  wenn die Daten verkauft werden, **von dir entdeckt** bleibt also bis dahin und danach.
- Ein Erstes bleibt deins, wenn du zurückkommst. Sobald du die Daten eines Systems verkauft hast, melden die
  Markierungen des Spiels deinen eigenen Datensatz, Katey behält also einen Körper, den du zuerst entdeckt
  oder kartiert hast, als deinen, wie auch immer ein späterer Scan lautet. Ein Körper, den du zuerst betreten
  hast, ist von der Landung an deiner. Ein Körper, den du gescannt, aber nie verkauft hast, nimmt die späteren
  Markierungen, wie sie kommen, da sie dann bedeuten, dass jemand anderes zuerst verkauft hat.
- Ein Stern hat keinen Kartierungs- oder Betreten-Platz, und ein Körper, auf dem niemand landen kann, hat
  keinen Betreten-Platz. Wo jemand anderes zuerst war, du aber ebenfalls kartiert hast oder gelandet bist,
  sagt eine leise Zeile unter der Karte **Von dir kartiert.** oder **Hier gelandet.**
- In einem bevölkerten System lässt sich Betreten nicht beanspruchen, und die Markierung des Spiels sagt
  nichts, Katey behandelt dort also jeden Körper als betreten. Ein Körper, der von Spansh kam, sagt
  **Entdeckung unbekannt**, **Kartierung unbekannt** und **Betreten unbekannt**.
- Grünes Abzeichen **landbar**: Ein Schiff kann dort landen.
- Schwerkraft, Temperatur, Atmosphäre und die Oberflächenhinweise stehen in der Titelzeile neben der
  Entfernung, die nach Bedarf umbricht, sodass eine Karte kurz bleibt. Die Schwerkraft wird ab 1,0 g gelb mit
  einem Abzeichen **hohe Schwerkraft** und ab 1,5 g rot mit **extreme Schwerkraft**: Bei diesem Zug kann ein
  schweres Schiff seinen Sinkflug womöglich nicht mehr stoppen, also nähere dich langsam und fange früh ab.

**Namen kopieren** legt den Namen des Systems für die Galaxiekarte in die Zwischenablage. Jeder Systemname
überall in Katey ist ein Link zu dieser Seite.

Die Spezies-Vorhersagen stammen daher, wo jede Spezies zuvor gefunden wurde. Bevor ein Körper kartiert ist,
wird ein Gattungssignal gegen seine Atmosphäre, seinen Typ, Vulkanismus, Schwerkraft, Temperatur und Druck
abgeglichen; einmal kartiert, wird die eigene Gattungsliste des Scanners verwendet.

Bis dahin ist die Exobiologie-Zahl des Körpers eine Vorhersage, als „wahrscheinlich“ markiert:

- jede Gattung, die unter den Bedingungen des Körpers leben kann, wird danach gewichtet, wie oft sie unter
  solchen Bedingungen gefunden wurde;
- dieser gewichtete Erlös wird mit der Zahl der biologischen Signale multipliziert;
- eine Zeile sagt, wie viele Gattungen dort leben könnten, und nennt die drei wahrscheinlichsten, jede mit
  ihren Chancen, ihrer wahrscheinlichsten Spezies und ihrem erwarteten Erlös vor jedem Bonus. Die Signale
  können von jeder Gattung stammen, die die Bedingungen zulassen, nicht nur von diesen dreien.

Die Signalzahl erscheint als Abzeichen. Eine Spezies, die niemand auf einem Körper erfasst hat, zahlt das
Fünffache ihres Grundwerts. Das Journal kann nicht sagen, ob es jemand hat, also nimmt Katey, wenn niemand den
Körper kartiert hatte, den Bonus als wahrscheinlich und zeigt die Zahl mal fünf, mit dem Grundwert daneben.
Die Leiste ordnet die Körper nach dieser Zahl, bekannt oder vorhergesagt, Bonus eingeschlossen, dann nach der
Signalzahl. Die Spalte **Landbar** der Systemseite sortiert genauso, mit den landbaren Körpern zuerst.

### Route

Zwei Abschnitte:

- **Die Spielroute**, die das Spiel geplant hat, Stern für Stern, mit deiner Position zuerst in jeder Zeile.
  Jeder Halt sagt, ob er **neu für dich**, **besucht** oder durch einen Honk oder eine Navigationsbake
  **vermessen** ist, die Entfernung vom Start und den bei Ankunft erwarteten Treibstoff.
  - Das System, das du anvisiert hast, ist immer der nächste Sprung. Ohne Route anvisiert, ist es eine Route
    aus einem Sprung. Weiter entlang der Route anvisiert, fallen die Halte dazwischen weg. Abseits der Route
    anvisiert, zeigt die Seite den einen Sprung dorthin und eine Zeile, die sagt, wo die geplante Route
    weitergeht und wie viele Sprünge bleiben. Die Entfernung des Ziels kommt aus deinem Verlauf oder der
    Spansh-Route und lautet 0, wenn keiner von beiden das System kennt.
  - Die Treibstoffprojektion rechnet den Tankstand des Spiels entlang der kommenden Etappen mit dem eigenen
    Verbrauch des Schiffs weiter, nach der eigenen Gleichung des Spiels aus den Massen, der Fracht und den
    Antriebswerten, die das Journal meldet. Sie ist vom ersten Moment an da und sagt, wie viele Sprünge
    bleiben, bevor du tanken musst, bis zu welchem tankbaren Stern spätestens und was jeder Halt kostet. Jeder
    Sprung wird mit dem für ihn übrigen Treibstoff berechnet, sodass das Leichterwerden des Schiffs beim
    Verbrennen mitverfolgt wird.
  - Die Fracht sind die jetzt an Bord befindlichen Tonnen, und die Projektion wird bei jeder
    Journalaktualisierung neu gerechnet, sodass ein Verkauf oder eine Abholung unterwegs sie in dem Moment
    ändert, in dem das Spiel ihn meldet. Ein Schiff, für dessen Ausrüstung das Journal die Massen nicht
    geliefert hat, fällt auf den aus seinen letzten Sprüngen gelernten Verbrauch zurück. Ein Abflug von einem
    Neutronenstern oder einem Weißen Zwerg zählt als geboosteter Sprung und ein Guardian-Booster als
    zusätzliche Reichweite.
  - Der Tankbalken über den Halten ist gezeichnet wie die eigene Anzeige des Spiels: Orange ist, was nach dem
    nächsten Sprung im Tank bleibt, Blau ist, was dieser Sprung verbrennt, und die blaue Zahl neben dem Stand
    ist derselbe Verbrauch in Tonnen.
  - Sobald du gesprungen bist, nimmt das Spiel den Treibstoff sofort, und der Tank zeigt es, sodass während
    des Sprungs der Halt, an dem du ankommst, den Tankstand zeigt und der blaue Balken zum Sprung danach
    wandert.
- **Die Spansh-Route.** Mit dem Planer in den [Optionen](#optionen) eingeschaltet, fragt dies den
  Galaxie-Routenplaner von Spansh nach einer Route von deiner Position zu einem beliebigen System, für ein
  Schiff, das du besitzt, oder einen gespeicherten Build. Die Planung nutzt die eigenen Massen, Tanks und
  Antriebswerte des Schiffs: die des Journals für ein Schiff, die des EDSY-Exports für einen Build.
  Neutronenboosts, FSA-Injektionen, das Tanken an Sekundärsternen und das Auftanken an jedem tankbaren Stern
  sind Optionen.
  - Die Auswahl beginnt bei dem Schiff, das du fliegst. Ein Klick darauf öffnet eine Tafel mit einem leeren
    Suchfeld und der Liste, in dieser Reihenfolge: das zuerst gewählte Schiff oder der Build, das Schiff, das
    du fliegst, als Zweites, dann die gespeicherten Builds für den gewählten Schiffstyp, deine Schiffe und die
    übrigen Builds. Jedes zeigt seine Reichweite mit vollem Tank, das Schiff, das du fliegst, in der
    Akzentfarbe. Tippe, um die Liste nach Name oder Schiffstyp einzugrenzen. Klicke einen Eintrag oder drücke
    Enter für den ersten, um ihn zu wählen. Nichts anderes ändert die Wahl.
  - Dem Planer wird gesagt, der Antrieb sei aufgeladen, wenn du seit deinem letzten Sprung einen
    Jet-Cone-Boost genommen hast oder wenn du an einem Neutronenstern sitzt und Neutronenboosts eingeschaltet
    sind, sodass der erste Sprung als geboosteter geplant wird.
  - Kateys Antriebswerte sind dieselben Tabellen, mit denen Spanshs eigene Seite arbeitet, SCO- und
    Mk-II-Antriebe eingeschlossen, sodass eine über Katey angefragte Planung einer entspricht, die durch
    Einfügen des Builds auf der Seite entsteht.
  - Das Ziel, die Routenplanung und die Optionen werden gespeichert, während du sie änderst. Das Schiff oder
    der Build und die Fracht bleiben, wie du sie gesetzt hast, solange die Route gehalten wird, über Seiten
    und Neustarts hinweg, bis du **Route verwerfen** drückst. Dann öffnet sich die Seite wieder auf dem
    Schiff, das du fliegst, mit der Fracht, die es trägt, ausgefüllt, die du überschreiben kannst. Das
    Wechseln zwischen Schiffen oder Builds lässt die Fracht in Ruhe.
  - Die Route führt jeden Wegpunkt mit seinem Sprung auf, der verbleibenden Entfernung, dem Treibstoff, den
    der Planer bei Ankunft erwartet, und ob er ein Neutronenstern oder ein Tankstopp ist. Sie folgt deinen
    Sprüngen, um zu sagen, welcher Wegpunkt der nächste ist, kopiert diesen Namen für die Galaxiekarte und
    wird zwischen den Läufen behalten.
  - Verlässt du die Route für einen Umweg, etwa zum Tanken, wird der letzte Wegpunkt, den du seit der Planung
    erreicht hast, als **zuletzt bekannt** markiert, während der danach der nächste Wegpunkt bleibt, sodass
    **Kopieren** dir weiterhin den Neutronenstern gibt, auf den du zielen sollst. Eine Route, die von einem
    Ort geplant wurde, an dem du nicht bist, hat keinen zuletzt bekannten Wegpunkt, bis du einen ihrer Halte
    erreichst, selbst wenn du einige davon zuvor durchquert hast.

### Statistik

Dein Journalverlauf, als Hauptbuch gelesen. Seine Abschnitte:

- **Einkommen.** Ein Ringdiagramm dessen, was du nach Aktivität verdient hast, von Kopfgeldern bis
  Exobiologie, mit einem Schalter je Aktivität, der das Diagramm ohne sie neu zeichnet, und einer
  Zeitraumauswahl von der gesamten Zeit bis zur letzten Woche.
- **Exobiologie.** Was an Bord und unverkauft ist: Proben, wie viele wahrscheinlich Ersterfassungen sind, und
  ihr Wert mit und ohne Bonus. Dann, was organische Daten mit und ohne Ersterfassungsbonus eingebracht haben,
  und jede verkaufte Spezies.
- **Erkundung.** Was an Bord und unverkauft ist: Systeme, gescannte und kartierte Körper und ihr geschätzter
  Kartografiewert. Dann die Verkäufe des Zeitraums neben den Gesamtwerten des Spiels.
- **Kampf, Handel und Bergbau, Missionen und Sonstiges, Powerplay.** Die eigenen Zahlen jeder Aktivität für
  den Zeitraum, neben den Gesamtwerten des Spiels dafür.
- **Schwärme und Crews.** Mit anderen geflogene Zeit und was das Spiel als geteilt markiert hat, nach Art.
  Jeder Commander, mit dem du geflogen bist, ist mit der gemeinsamen Zeit und dem aufgeführt, was geteilt
  wurde, während er anwesend war. Jede angenommene Schwarm-Mission ist mit denen aufgeführt, die damals im
  Schwarm waren, und ihrem Stand. Im Schwarm zu fliegen, während du allein erkundest, teilt nichts und zählt
  hier also nichts. Das Journal sagt nicht, ob eine Schwarm-Mission vom Brett oder von einem Schwarmmitglied
  kam, daher stehen beide hier.
- **Gesamtwerte.** Jede laufende Summe, die das Spiel meldet, Spielzeit eingeschlossen. Ein gemerkter Schalter
  führt auch die Zahlen bei null auf; er ist standardmäßig aus. Die Zeitraumauswahl verbirgt sich hier, da
  dies die eigenen Summen des Spiels sind.

### Optionen

- **Aktualisierungen.** Die Version, die du verwendest, und, mit dem GitHub-Schalter an, ob es eine neuere
  Veröffentlichung gibt. Katey fragt einmal beim Start und wann immer du **Jetzt prüfen** drückst, im
  Hintergrund, sodass der Start nicht länger dauert. Gibt es eine neuere Veröffentlichung, öffnet **Seite der
  Veröffentlichung öffnen** sie in deinem Browser; nichts wird für dich heruntergeladen oder installiert.
- **Textgröße.** Wie groß die Fenster alles zeichnen, als Prozentsatz der Standardgröße, für Leser, die
  größere Schrift brauchen. Text und Abstände wachsen zusammen, sodass die Seiten ihre Form behalten; löse die
  Leiste ab oder verbreitere das Fenster, wenn eine Seite nicht mehr passt. Die Wahl wird gemerkt. Ist der
  Bildschirm zu klein für die vergrößerten Seiten, bleibt das Fenster am Bildschirmrand stehen und die Seiten
  rollen seitwärts.
- **Journal-Ordner.** Der Ordner, den Katey beobachtet. **Erkennen** sucht an den üblichen Orten,
  **Durchsuchen** wählt einen, oder tippe einen Pfad; die Beobachtung folgt dem Ordner sofort und spielt seine
  Geschichte ab. Die Statuszeile sagt, was die Beobachtung tut.
- **Journal-Sicherung.** Ein Ordner, in den der ganze Journal-Ordner kopiert wird, nur was neu oder geändert
  ist, wenn Katey schließt und wann immer du **Jetzt sichern** drückst. Richte ihn auf einen Ordner, den
  OneDrive oder ein anderes Cloud-Laufwerk synchronisiert, und deine Journale verlassen damit den Rechner.
- **Gespeicherte Daten.** Der Ordner, in dem Katey seine eigenen Daten hält; siehe [Deine
  Daten](#deine-daten). Wähle einen anderen, und die Daten ziehen sofort dorthin um. Enthält der Ordner
  bereits eine Katey-Datendatei, wird sie geladen, andernfalls werden die aktuellen Daten dort geschrieben,
  und die Daten am alten Ort bleiben, wie sie waren. Zwei Rechner, die auf einen synchronisierten Ordner
  zeigen, teilen einen Plan, einen Satz Ingenieure und einen Satz Builds.
- **Ausgehende Verbindungen.** Die eine Stelle, die entscheidet, was Katey über das Netzwerk senden darf, ein
  Schalter je Art von Anfrage, jeder aus, bis du ihn anhakst. Es gibt drei Arten. Zwei gehen an
  [Spansh](https://spansh.co.uk/): die Frage nach einer Route, wenn du **Planen** drückst, und die Frage nach
  den Körpern des Systems, in dem du gerade angekommen bist. Die dritte geht an [GitHub](https://github.com/temetvince/elite-dangerous-katey/releases)
  und fragt nach der neuesten Veröffentlichung von Katey. Die Schalter werden gemerkt.

## Arbeiten mit EDSY

[EDSY](https://edsy.org/) ist der Schiffsbauer, den die meisten in der Community nutzen, und Katey spricht
seine Import- und Exportformen in beide Richtungen.

- **Ein Build in den Plan.** Öffne in EDSY den Build und wähle **Export**, dann den Loadout-Text im
  Journalstil. Füge ihn in das Importfeld der Plan-Schublade ein und drücke **Export einlesen**. Jedes
  modifizierte Modul wird ein Eintrag mit seinem Bauplan, seiner Stufe und seinem Experimentaleffekt; hake die
  ab, die du nicht willst, und drücke **Zum Plan hinzufügen**.
- **Ein Build als gespeicherter Build.** Füge denselben Text in das Feld für gespeicherte Builds auf der Seite
  [Flotte](#flotte) ein. Der Build wird dann mit deinen Schiffen und deinen eigenen Modulen verglichen. Das
  Einfügen eines neueren Exports eines Builds, den du bereits gespeichert hast, gleicher Name und gleicher
  Schiffstyp, ersetzt den gespeicherten Build, nachdem du bestätigt hast.
- **Ein Schiff oder ein Build zurück zu EDSY.** **Für EDSY kopieren** auf der Seite eines
  [Schiffs](#ein-schiff) oder eines [Builds](#ein-build) legt einen Loadout in die Zwischenablage. Wähle in
  EDSY **Import** und füge ein.

## Vorgefertigte Schiffe

Ein aus einem Paket im Shop des Spiels eingelöstes Schiff kommt mit Modulen, die Katey als vorgefertigt
markiert, solange sie in den Slots bleiben, in denen sie kamen. Kaufen, Abholen, Tauschen, Einlagern oder
Verkaufen in einen Slot beendet die Markierung, was immer das Spiel als Wert des Neuen zeigt, ebenso ein
gekauftes Modul derselben Art, das den Slot übernimmt. Die Paketmodule des Spiels können nicht gelagert, auf
ein anderes Schiff verlegt oder verkauft werden. Ein solches Modul zu modifizieren ist Modifikation, die bei
diesem Schiff bleibt. Katey markiert diese:

- Die Seite [Flotte](#flotte) markiert ein eingelöstes Schiff als **Vorgefertigt**.
- Die Seite eines [Schiffs](#ein-schiff) markiert jedes solche Modul neben seiner Modifikationsauswahl als
  **Vorgefertigtes Modul**, und ein [Build](#ein-build)-Vergleich warnt, wo der zu modifizierende Slot eines
  enthält.
- Eine Schiffsseite sagt auch, ob ein vorgefertigtes Schiff **Noch wie eingelöst** ist, sodass Verkaufen und
  erneutes Einlösen nichts verliert, oder wie viele Slots sich seitdem geändert haben. Die Zeile öffnet sich
  zu einer Tabelle dessen, was jeder geänderte Slot ursprünglich hatte und jetzt enthält. Ein vorgefertigtes
  Modul, gegen ein gekauftes derselben Art getauscht, zählt als Änderung, da das gekaufte das Schiff verlassen
  kann.

Ein vorgefertigtes Schiff, das du nie eingesetzt hast, existiert für das Spiel nicht, es kann also nicht in
der Flotte erscheinen.

## Was das Journal Katey nicht sagen kann

Katey weiß nur, was das Spiel schreibt. Daraus folgen einige Grenzen:

- Ob jemand anderes ein System entdeckt hat, ist erst bekannt, wenn du einen Körper darin scannst. Vor der
  Ankunft kann Katey nur sagen, ob *du* dort warst.
- Das Journal hält nicht fest, wer einen Körper zuerst entdeckt oder kartiert hat oder wer zuerst darauf
  gelandet ist, nur ob du es warst.
- Die Flottenliste und die Liste der gelagerten Module werden neu geschrieben, wenn du irgendwo mit Werft oder
  Ausrüstung andockst, sie sind also so frisch wie dein letzter solcher Besuch. Bauplan und Stufe eines
  gelagerten Moduls werden festgehalten, aber nicht sein Experimentaleffekt, den der Build-Vergleich als
  unsicher meldet.
- Das Journal sagt nicht, ob eine Schwarm-Mission vom Missionsbrett oder von einem Schwarmmitglied kam.
- Körper werden für das System gezeigt, in dem du bist; ein früheres System zeigt, wo es ist und wann du dort
  warst.
- Ein Körper, den du bei einem früheren Besuch gescannt hast, ist bereits in deinen Daten, das Spiel schreibt
  also nichts, wenn der Honk ihn wieder findet. Katey merkt sich jeden Körper, den du bei irgendeinem Besuch
  innerhalb deines Journalverlaufs gescannt hast, und führt ihn bei deiner Rückkehr wieder auf, markiert mit
  dem Tag, an dem er gescannt wurde. Ein Körper, der vor deiner ältesten Journal-Datei gescannt wurde, lässt
  sich so nicht wiederherstellen. Meldet der Honk das ganze System als bereits bekannt, zählt Katey jeden
  Körper als gescannt, kann aber nur die aufführen, die es hat, oder die, die Spansh kennt, wenn dieser
  Schalter an ist.
- Die als **von Spansh** markierten Körper sind, was Spansh kennt, was älter sein kann als das System: eine
  Vermessung von vor Jahren oder nur ein Teil eines Systems. Katey fragt einmal nach einem System, wenn du
  darin ankommst, und behält die Antwort für den Lauf; ist Spansh nicht erreichbar, wartet es fünf Minuten,
  bevor es erneut nach diesem System fragt.

## Deine Daten

Katey hält eine Datei: den Plan, die Ingenieursauswahl, die gespeicherten Builds, die Materialreserven, den
Journal-Ordner, den Sicherungsordner und -schalter, die Schalter der ausgehenden Verbindungen und die letzte
Spansh-Route, die Platzierung und die Einstellungen der Leiste und jede gemerkte Ansichtswahl. Standardmäßig
liegt sie unter deinem lokalen Anwendungsdatenordner, in `Katey\state.json`. Das Feld **Gespeicherte Daten**
in den [Optionen](#optionen) verschiebt sie an einen beliebigen Ort, einen synchronisierten Ordner
eingeschlossen. Wenn ein Update eine Einstellung ausmustert oder umbenennt, verwirft Katey den alten Wert und
beginnt bei der Vorgabe, sodass die Datei nie Einstellungen trägt, die nichts liest.

Katey liest die Journal-Dateien lokal und sendet nirgendwohin etwas, solange du es nicht unter **Ausgehende
Verbindungen** in den [Optionen](#optionen) erlaubst, wo zu Beginn alles aus ist. Es gibt drei Arten von
Anfragen. Zwei gehen an [Spansh](https://spansh.co.uk/): wenn du **Planen** drückst, zwei Systemnamen und die
Sprungwerte des Schiffs; und bei der Ankunft in einem System dessen Kennung, die die Körper zurückbringt, die
Spansh dort kennt. Die dritte geht an GitHub, wenn Katey startet und wenn du **Jetzt prüfen** drückst: eine
Anfrage nach der neuesten Veröffentlichung des Projekts, die nichts von dir mitführt. Jede Anfrage nennt
Katey, seine Version und die Adresse des Projekts, wie Spansh es von Werkzeugen verlangt, die seinen Dienst
nutzen. Die Journal-Sicherung kopiert Dateien nur in den Ordner, den du wählst.

Katey führt außerdem ein Protokoll, `katey.log`, neben der Standard-`state.json` unter deinem lokalen
Anwendungsdatenordner. Es hält fest, was Katey getan hat und was schiefging: Starten und Beenden, den
beobachteten Journal-Ordner, gestellte Anfragen und aufgetretene Fehler und den Grund, falls es sich je von
selbst schließt. Schließt sich Katey unerwartet, sagt diese Datei warum, und sie ist das, was zu einem
Fehlerbericht gehört. Sie wird ab einem Megabyte rotiert, bleibt also klein.

Das Protokoll enthält Sternsystem- und Schiffsnamen aus deinem Journal und sonst nichts über dich: keinen
Commander-Namen, keine Credits, keine Positionen und keine Ordnernamen. Kateys eigene Meldungen tragen nie
einen Pfad. Alles andere, was das Protokoll erreicht, etwa ein Fehler von Windows, behält nur einen
Dateinamen, mit deinem Konto- und Rechnernamen ersetzt, sodass die Datei so geteilt werden kann, wie sie ist.

Für einen Fehler, der mehr Details braucht, starte Katey als `katey.exe --verbose` oder `katey.exe --log-level
debug`. Das Protokoll hält dann auch die geöffneten Seiten, die Entscheidungen der Leiste und die gestellten
Abfragen fest; `--log-level warning` behält nur Probleme. Beim nächsten Start ohne die Option ist es wieder
normal. `katey.exe --pseudo-loc` zeigt jedes Wort länger, mit Akzenten und in Klammern, womit ein Übersetzer
oder ein Fehlerbericht Text findet, der abgeschnitten ist oder nicht durch die Tabellen gegangen ist.

Geht die Journal-Datei des Tages verloren, während das Spiel läuft, schreibt das Spiel sie von Anfang an neu,
und alles, was sie enthielt, ist für das Spiel und für Katey verloren. Die Sicherung behält ihre frühere Kopie
einer solchen Datei neben der neuen, benannt mit dem Zusatz `rewritten`. Bis zu deinem nächsten Sprung nimmt
Katey deine Position aus der Routendatei des Spiels, deren erster Halt das System ist, in dem du bist, sodass
die Leiste und die Routenseite richtig bleiben; die Körper des Systems sind leer, bis du erneut scannst.
