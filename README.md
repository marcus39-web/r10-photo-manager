# R10 Photo Manager

R10 Photo Manager ist ein Desktop-Foto-Manager auf Basis von **C#**, **WPF** und **.NET 8**.  
Die Anwendung scannt Bildarchive, erzeugt Thumbnails, erstellt einen JSON-Index, bietet eine Suchoberfläche mit Filtern und kann den Index optional in eine lokale Datenbank importieren.

## Inhalt

- [Überblick](#überblick)
- [Hauptfunktionen](#hauptfunktionen)
- [Windows-Integration](#windows-integration)
- [Projektstruktur](#projektstruktur)
- [Voraussetzungen](#voraussetzungen)
- [Starten und Build](#starten-und-build)
- [Bedienung der Oberfläche](#bedienung-der-oberfläche)
- [Suche und Indexierung](#suche-und-indexierung)
- [Wichtige Dateien und Verzeichnisse](#wichtige-dateien-und-verzeichnisse)
- [Fehlerbehebung](#fehlerbehebung)
- [Weiterentwicklung](#weiterentwicklung)
- [Beitrag und Lizenz](#beitrag-und-lizenz)

## Überblick

Die App ist für die Verwaltung eines lokalen Fotoarchivs ausgelegt.  
Im Mittelpunkt stehen:

- das **Scannen eines Bildarchivs**
- das **Erzeugen eines durchsuchbaren Indexes**
- die **Anzeige von Vorschaubildern**
- die **Suche nach Dateinamen, Tags und Filtern**
- die **Windows-Integration** für App-Suche und Dateiübergabe

## Hauptfunktionen

- Scannen eines Verzeichnisses und Erfassen unterstützter Bilddateien
- Erzeugen und Speichern von JPEG-Thumbnails
- Volltext-/Filter-Suche über den geladenen Index
- Anzeige von Kennzahlen auf der Indexseite
- Persistenz über `Data/index.json`
- Optionaler Import des JSON-Indexes in eine lokale SQL-Datenbank via EF Core
- Unterstützung für JPG, PNG und mehrere RAW-Formate

## Windows-Integration

Die Windows-Integration ist ein zentraler Bestandteil des Projekts.

### 1. Windows-App-Suche / Startmenü

Die Anwendung registriert sich beim Start selbst für die Windows-Shell:

- Startmenü-Verknüpfung für **R10 Photo Manager**
- zusätzlicher Such-Alias **R10CSharp**
- Eintrag für die Windows-Deinstallationsliste
- App-Path-Registrierung für saubere Auflösung der EXE

Relevante Datei:

- `Services/WindowsAppRegistration.cs`

### 2. Explorer-Integration / „Öffnen mit“

Unterstützte Bilddateien können über **„Öffnen mit“** direkt an die App übergeben werden.

Unterstützte Erweiterungen:

- `.jpg`, `.jpeg`, `.png`
- `.cr2`, `.cr3`, `.nef`, `.arw`, `.rw2`, `.dng`

Relevante Datei:

- `Services/ExplorerOpenWithRegistration.cs`

### 3. Übergabe an die Suche

Wenn die Anwendung von Windows oder dem Explorer mit einer Datei gestartet wird:

- übernimmt `App.xaml.cs` die Startargumente,
- `MainWindow.xaml.cs` öffnet direkt die `SearchPage`,
- und der Dateiname wird als initialer Suchbegriff verwendet.

Dadurch ist die Verbindung zwischen **Windows-Dateiübergabe** und **interner Foto-Suche** klar abgebildet.

## Projektstruktur

```text
R10CSharp/
├─ App.xaml / App.xaml.cs
├─ MainWindow.xaml / MainWindow.xaml.cs
├─ Pages/
│  ├─ StartPage.xaml / .cs
│  ├─ SearchPage.xaml / .cs
│  ├─ ScanPage.xaml / .cs
│  ├─ IndexPage.xaml / .cs
│  └─ SettingsPage / SettingPage.xaml.cs
├─ Services/
│  ├─ IndexBuilder.cs
│  ├─ SearchEngine.cs
│  ├─ FileScanner.cs
│  ├─ SettingsService.cs
│  ├─ IndexImportService.cs
│  ├─ WindowsAppRegistration.cs
│  └─ ExplorerOpenWithRegistration.cs
├─ Models/
│  └─ PhotoIndexEntry.cs
├─ Data/
│  ├─ R10PhotoContext.cs
│  ├─ config.json
│  ├─ index.json
│  └─ Thumbnails/
└─ Migrations/
```

## Voraussetzungen

- Windows
- .NET 8 SDK
- Visual Studio 2022 oder Visual Studio 2026
- optional: SQL Server LocalDB für den Datenbankimport

## Starten und Build

1. Lösung `R10CSharp.slnx` in Visual Studio öffnen.
2. Projekt erstellen über **Build -> Build Solution**.
3. App starten über **F5** oder **Strg+F5**.

## Bedienung der Oberfläche

### MainWindow / Navigation

- **Start**  
  Öffnet die Startseite mit Schnellaktionen.

- **Suche**  
  Öffnet die Suchseite.

- **Scan**  
  Öffnet die Seite zum Indexaufbau.

- **Index**  
  Öffnet die Kennzahlen- und Übersichtsseite.

- **Einstellungen**  
  Öffnet die Konfiguration für Pfade, Parallelität und weitere Optionen.

### StartPage

Die Startseite bietet Kachel-Buttons als Schnellzugriff auf:

- Suche
- Scan
- Index
- Einstellungen

### SearchPage

- globale Suchabfrage
- Filter für Kategorie, RAW/JPG, Serie und Favoriten
- Ordner-/Bibliotheksfilter
- Ergebnisliste mit Vorschaubild und Dateiinformationen

### ScanPage

- Start des Archiv-Scans
- Fortschrittsanzeige
- ETA-Anzeige
- Abbrechen eines laufenden Scanvorgangs

### IndexPage

Zeigt zusammengefasste Kennzahlen aus dem Index:

- Gesamtanzahl Dateien
- RAW-Dateien
- JPG-Dateien
- Kategorien
- Serien

### SettingsPage

Konfigurierbar sind unter anderem:

- `ArchivePath`
- `IndexPath`
- `ThumbnailParallelism`
- `ConnectionString`

## Suche und Indexierung

### Indexaufbau

Der Indexaufbau wird primär durch `Services/IndexBuilder.cs` gesteuert.

Dabei passieren im Wesentlichen folgende Schritte:

1. Dateisuche über `FileScanner`
2. Erstellen von `PhotoIndexEntry`-Objekten
3. Generieren von Thumbnails
4. Speichern des Ergebnisses in `Data/index.json`

### Suchlogik

Die eigentliche In-Memory-Suche erfolgt in `Services/SearchEngine.cs`.

Aktuell berücksichtigt die Suche insbesondere:

- Dateiname
- Tags
- Kategorie
- RAW/JPG
- Serie
- Datumsbereich
- Bibliothek / Unterordner

### Besondere Bedeutung der Suchanbindung

Die Suche ist nicht nur eine UI-Funktion, sondern auch die Zieloberfläche der Windows-Integration:

- Windows/Explorer übergibt eine Datei an die App
- die App extrahiert daraus den Dateinamen
- die `SearchPage` wird mit diesem Begriff geöffnet
- der Benutzer kann ähnliche oder passende Bilder direkt finden

Damit ist die **Verbindung zwischen Windows-App-Suche, Explorer-Integration und interner Bildsuche** ein Kernmerkmal des Projekts.

## Wichtige Dateien und Verzeichnisse

- `Data/index.json`  
  JSON-Index der Anwendung

- `Data/config.json`  
  persistente Anwendungseinstellungen

- `Data/Thumbnails/`  
  generierte Vorschaubilder

- `Models/PhotoIndexEntry.cs`  
  Datenmodell eines indexierten Bildes

- `Data/R10PhotoContext.cs`  
  EF-Core-DbContext

- `Services/WindowsAppRegistration.cs`  
  Registrierung für Startmenü, Windows-App-Suche und App-Pfade

- `Services/ExplorerOpenWithRegistration.cs`  
  Explorer- und Dateityp-Integration

## Fehlerbehebung

- **App schließt beim Klick auf Index**  
  `Data/index.json` prüfen. Bei leerer oder ungültiger Datei wird inzwischen ein leerer Index verwendet. Zusätzlich kann `bin\Debug\net8.0-windows\Data\app_error_log.txt` geprüft werden.

- **DB-Migration oder Import schlägt fehl**  
  Prüfen, ob SQL Server LocalDB vorhanden ist oder den `ConnectionString` in `Data/config.json` anpassen.

- **Thumbnails fehlen**  
  Rechte für `Data/Thumbnails` sowie Erreichbarkeit der Quelldateien prüfen.

- **Windows-Suche findet die App nicht sofort**  
  Nach der ersten Registrierung kann es nötig sein, die App einmal neu zu starten oder Windows kurz Zeit für die Shell-Aktualisierung zu geben.

## Weiterentwicklung

Geplante oder sinnvolle nächste Ausbaustufen:

- Detailansichten für einzelne Einträge
- Bearbeiten von Metadaten und Tags
- erweiterte Filterlogik
- stärkere Datenbanknutzung als primäre Datenquelle
- UI-Feinschliff und Layout-Optimierung
- weitere Shell- und Suchintegration unter Windows

## Beitrag und Lizenz

Beiträge sind willkommen.  
Bitte Issues oder Pull Requests im Repository verwenden.

**Lizenz:** MIT

**Repository:**  
https://github.com/marcus39-web/r10-photo-manager.git