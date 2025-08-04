# IDS uEyeDotNet.dll – Integration und Anwendung in C\# (.NET, WPF)

-----

## 1\. Einleitung

Dieses Dokument dient als umfassende Anleitung für Entwickler, die eine IDS-Kamera vom Typ UI-2212SE-M in eine C\# WPF-Anwendung (.NET) mit Visual Studio 2022 integrieren möchten. Es fasst die wesentlichen Schritte von der **Initialisierung der Kamera** über die **Konfiguration von Parametern** bis zur **Bildaufnahme und -verarbeitung** zusammen. Die Informationen basieren auf technischen Dokumentationen und Praxisanalysen, um einen schnellen und erfolgreichen Einstieg zu ermöglichen. Das Ziel ist es, Entwicklern mit C\#-Grundkenntnissen, aber ohne spezifische Erfahrung mit der IDS uEye API, eine schrittweise und verständliche Anleitung an die Hand zu geben.

-----

## 2\. Überblick über uEyeDotNet.dll und typische Anwendungen

Die `uEyeDotNet.dll` ist die zentrale .NET-Bibliothek von IDS, die eine objektorientierte Schnittstelle zur Steuerung aller uEye-Kameras bietet. Sie basiert auf der **.NET Standard 2.0 Library** und ist somit mit modernen .NET-Versionen kompatibel.

### Wichtige Assemblies und Abhängigkeiten:

  * **`uEyeDotNet.dll`**: Die Haupt-SDK für die gesamte Kamera-Funktionalität.
  * **`uEyeDotNetFramework.dll`**: Bietet Framework-spezifische Erweiterungen.
  * **`ueye_api.dll` / `ueye_api_64.dll`**: Die nativen Treiber, auf denen das .NET SDK aufbaut.

### Wichtige Namespaces:

  * `using uEye;` // Hauptnamespace für Klassen wie `uEye.Camera`
  * `using uEye.Defines;` // Enthält alle wichtigen Enumerationen (Status, ColorMode etc.) und Konstanten
  * `using uEye.Types;` // Beinhaltet Datentypen und Strukturen (z.B. Range, CameraInformation)

### Typische Projektstruktur in einer Anwendung:

  * **`MainForm`/`MainWindow`**: Das Hauptfenster, das die UI-Elemente für die Kamerasteuerung und die Live-Anzeige enthält.
  * **Kamera-Logik-Klasse**: Eine separate Klasse, die die direkte Interaktion mit der `uEye.Camera`-Klasse kapselt (Initialisierung, Events, Cleanup).
  * **Hilfsklassen**: Oft werden für Speicherverwaltung (`MemoryHelper`) oder spezifische Steuerungen (Timing, Gain) eigene Klassen verwendet.

-----

## 3\. Kamera-Verbindung: Auflisten, Auswahl, Initialisierung

Der erste Schritt ist immer, eine Verbindung zur Kamera herzustellen. Dies umfasst das Finden verfügbarer Kameras, die Auswahl einer bestimmten Kamera und deren Initialisierung.

### Verfügbare Kameras anzeigen:

Um eine Liste aller an das System angeschlossenen Kameras zu erhalten, wird die Methode `uEye.Info.Camera.GetCameraList()` verwendet. Diese liefert ein Array von `uEye.Types.CameraInformation`-Strukturen, die Details wie Modell, Seriennummer und Device-ID enthalten.

```csharp
// Alle verfügbaren Kameras auflisten
uEye.Types.CameraInformation[] cameraList;
uEye.Info.Camera.GetCameraList(out cameraList);

if (cameraList.Length == 0)
{
    MessageBox.Show("Keine Kameras gefunden!");
    return;
}

foreach (uEye.Types.CameraInformation info in cameraList)
{
    Console.WriteLine($"Kamera-ID: {info.CameraID}");
    Console.WriteLine($"Device-ID: {info.DeviceID}");
    Console.WriteLine($"Modell: {info.Model}");
    Console.WriteLine($"Seriennummer: {info.SerialNumber}");
    Console.WriteLine($"In Verwendung: {info.InUse}");
}
```

### Kamera auswählen:

Für die Initialisierung wird die `DeviceID` der gewünschten Kamera benötigt. In einer Anwendung mit mehreren Kameras sollte dem Benutzer ein Auswahldialog angeboten werden. Für einfache Anwendungen oder Tests kann die erste verfügbare Kamera verwendet werden.

```csharp
// Einfache Auswahl der ersten verfügbaren Kamera, die nicht in Benutzung ist.
int selectedDeviceID = -1;
foreach (var camera in cameraList)
{
    if (!camera.InUse)
    {
        selectedDeviceID = camera.DeviceID;
        break;
    }
}

if (selectedDeviceID == -1)
{
    MessageBox.Show("Alle Kameras sind bereits in Verwendung!");
    return;
}
```

### Kamera-Initialisierung und Verbindung:

Die Initialisierung erfolgt über die `Init()`-Methode des `uEye.Camera`-Objekts. Der Rückgabewert sollte immer auf `uEye.Defines.Status.SUCCESS` geprüft werden.

-----

**Konflikt:** Die `Init()`-Methode kann auf verschiedene Weisen aufgerufen werden. Die technische Dokumentation zeigt eine Initialisierung mit einem Handle für die direkte Anzeige in einem Control (typisch für Windows Forms), während andere Beispiele eine Initialisierung ohne Handle zeigen (besser geeignet für WPF, wo das Bild manuell gerendert wird).

-----

**Beispiel für WPF (bevorzugt):**

```csharp
// Kamera-Objekt erstellen
uEye.Camera m_Camera = new uEye.Camera();

// Kamera mit Device-ID initialisieren. Für WPF wird kein Handle benötigt (IntPtr.Zero).
// Das Flag UseDeviceID stellt sicher, dass die ID als Geräte-ID interpretiert wird.
uEye.Defines.Status statusRet = m_Camera.Init(
    selectedDeviceID | (Int32)uEye.Defines.DeviceEnumeration.UseDeviceID,
    IntPtr.Zero);

if (statusRet != uEye.Defines.Status.SUCCESS)
{
    MessageBox.Show("Kamera-Initialisierung fehlgeschlagen");
    return;
}
```

**Alternative (erste gefundene Kamera, vereinfacht):**

```csharp
// Öffnet die erste im System gefundene Kamera (Device-ID 0 ist nicht garantiert die erste)
// Besser ist die explizite Auswahl über die Device-ID Liste.
uEye.Defines.Status statusRet = m_Camera.Init(0);
```

### Kamera schließen:

Eine saubere Freigabe der Kamera-Ressourcen ist essenziell.

```csharp
private void CloseCamera()
{
    if (m_Camera != null && m_Camera.IsOpened)
    {
        m_Camera.Acquisition.Stop(); // Aufnahme stoppen
        m_Camera.Exit(); // Kamera schließen und Ressourcen freigeben
    }
}
```

-----

## 4\. Wichtige Kameraoptionen: Belichtung, Pixelclock, Gain etc.

### Belichtungszeit (Exposure):

Die Belichtungszeit wird in Millisekunden (ms) eingestellt. Der gültige Bereich kann vorher abgefragt werden.

```csharp
// Belichtungszeit auf 10.0 ms setzen
double exposureTime = 10.0;
uEye.Defines.Status status = m_Camera.Timing.Exposure.Set(exposureTime);

// Aktuelle Belichtungszeit abrufen
double currentExposure;
m_Camera.Timing.Exposure.Get(out currentExposure);
```

### Pixeluhr (Pixel Clock):

Die Pixeluhr (in MHz) beeinflusst die Auslesegeschwindigkeit des Sensors und damit direkt die maximal mögliche Framerate.

```csharp
// Pixeluhr auf 25 MHz setzen
int pixelClock = 25;
m_Camera.Timing.PixelClock.Set(pixelClock);
```

### Gain (Verstärkung):

Gain steuert die Helligkeit des Bildes digital. Ein hoher Gain kann zu Bildrauschen führen. Die UI-2212SE-M ist eine Monochrom-Kamera, daher ist nur der Master-Gain relevant.

```csharp
// Master Gain auf 50% des Maximalwerts setzen (Bereich 0-100)
int masterGain = 50;
m_Camera.Gain.Hardware.Scaled.SetMaster(masterGain);

// Auto-Gain aktivieren
m_Camera.AutoFeatures.Software.Gain.SetEnable(true);
```

### Framerate:

Die Framerate (Bilder pro Sekunde, fps) gibt die Aufnahmegeschwindigkeit an.

```csharp
// Framerate auf 30 fps setzen
double framerate = 30.0;
m_Camera.Timing.Framerate.Set(framerate);
```

### Farbtiefe und Pixelformat:

Für die monochrome Kamera UI-2212SE-M ist `Mono8` (8-Bit Graustufen) das gängigste Format. Für einen höheren Dynamikumfang können auch 12-Bit- oder Rohdatenformate (`SensorRaw`) verwendet werden.

```csharp
// Pixelformat auf 8-Bit Graustufen setzen
m_Camera.PixelFormat.Set(uEye.Defines.ColorMode.Mono8);

// Für höhere Bittiefe (falls unterstützt)
// m_Camera.PixelFormat.Set(uEye.Defines.ColorMode.Mono12);
// m_Camera.PixelFormat.Set(uEye.Defines.ColorMode.SensorRaw12);
```

-----

## 5\. Bildaufnahme und -verarbeitung: RAW / Bitmap / Farbtiefe

Die Bildaufnahme erfolgt typischerweise asynchron über Events. Die Kamera zeichnet kontinuierlich Bilder in einen Puffer (Sequence) auf, und ein Event benachrichtigt die Anwendung, wenn ein neues Bild zur Verarbeitung bereitsteht.

### Pufferverwaltung (Buffer Management):

Für eine flüssige Live-Ansicht ohne Bildverluste sind mehrere Bildpuffer erforderlich (typischerweise 3).

1.  **Allokieren:** Speicher für mehrere Bilder reservieren.
2.  **Sequenz erstellen:** Die allokierten Puffer zu einer Aufnahmesequenz hinzufügen.
3.  **Freigeben:** Nach Gebrauch den Speicher wieder freigeben.

<!-- end list -->

```csharp
// 1. Puffer allokieren (3 Puffer für Sequenz-Aufnahme)
uEye.Defines.Status statusRet = MemoryHelper.AllocImageMems(m_Camera, 3);
if (statusRet != uEye.Defines.Status.SUCCESS) return;

// 2. Puffer-Sequenz initialisieren
statusRet = MemoryHelper.InitSequence(m_Camera);
if (statusRet != uEye.Defines.Status.SUCCESS) return;

// Die Hilfsklassen AllocImageMems und InitSequence kapseln die Standardaufrufe
// Camera.Memory.Allocate() und Camera.Memory.Sequence.Add().
```

### Bildverarbeitung mit Events:

Der `EventFrame` wird von einem separaten Thread der API ausgelöst. Daher müssen alle UI-Aktualisierungen an den UI-Thread delegiert werden (in WPF über `Dispatcher.Invoke()`).

```csharp
// 1. Frame-Event registrieren
m_Camera.EventFrame += onFrameEvent;

// 2. Live-Video starten
m_Camera.Acquisition.Capture();

// 3. Event-Handler-Implementierung
private void onFrameEvent(object sender, EventArgs e)
{
    uEye.Camera camera = sender as uEye.Camera;
    Int32 memoryID;

    // Letzten Puffer abrufen
    if (camera.Memory.GetLast(out memoryID) == uEye.Defines.Status.SUCCESS && memoryID > 0)
    {
        // Puffer sperren für die Verarbeitung
        if (camera.Memory.Lock(memoryID) == uEye.Defines.Status.SUCCESS)
        {
            // --- Bilddatenverarbeitung für WPF ---
            IntPtr buffer;
            camera.Memory.GetBuffer(memoryID, out buffer);

            uEye.Types.ImageInfo imageInfo;
            camera.Information.GetImageInfo(memoryID, out imageInfo);
            int width = imageInfo.Width;
            int height = imageInfo.Height;
            int bitsPerPixel = imageInfo.BitsPerPixel; // Für Mono8 ist dies 8

            // Stride berechnen (Bytes pro Bildzeile)
            int stride = (width * bitsPerPixel + 7) / 8;

            // UI-Update im UI-Thread durchführen
            Application.Current.Dispatcher.Invoke(() =>
            {
                // BitmapSource für WPF erstellen
                var bitmapSource = BitmapSource.Create(width, height, 96, 96,
                                        PixelFormats.Gray8, // Für Mono8
                                        null, buffer, stride * height, stride);

                // An Image-Control binden
                DisplayImage.Source = bitmapSource;
            });
            // --- Ende der WPF-spezifischen Verarbeitung ---

            // Puffer wieder freigeben
            camera.Memory.Unlock(memoryID);
        }
    }
}
```

### Verarbeitung von RAW / 12-Bit-Daten:

Wenn die Kamera auf ein 12-Bit-Format (`Mono12`, `SensorRaw12`) eingestellt ist, ändert sich die Verarbeitung. Die `bitsPerPixel` sind nun 12. WPF hat kein natives 12-Bit-Graustufenformat. Die Daten müssen entweder manuell auf 8-Bit oder 16-Bit (`Gray16`) konvertiert werden, bevor sie in einer `BitmapSource` angezeigt werden können. Die Konvertierung von 12 auf 16 Bit ist in der Regel einfacher (durch Bit-Shifting).

-----

## 6\. Integration in eigene Projekte: Objektstruktur, Best Practices

### Objektstruktur:

  * **`uEye.Camera`**: Die zentrale Klasse, die ein Kameraobjekt repräsentiert. Sie bietet Zugriff auf alle Untermodule wie `Timing`, `Memory`, `Acquisition` etc.
  * **`uEye.Types.CameraInformation`**: Enthält alle Metadaten einer gefundenen Kamera.
  * **`uEye.Defines.Status`**: Eine essenzielle Enumeration, die als Rückgabewert für fast alle API-Aufrufe dient und den Erfolg oder spezifische Fehler anzeigt.

### Best Practices:

  * **Fehlerbehandlung**: Prüfen Sie den `uEye.Defines.Status`-Rückgabewert jedes API-Aufrufs.
  * **Ressourcenverwaltung**: Implementieren Sie das `IDisposable`-Pattern in Ihrer Kamera-Klasse, um sicherzustellen, dass `m_Camera.Exit()` und die Abmeldung von Events (`m_Camera.EventFrame -= onFrameEvent;`) zuverlässig aufgerufen werden.
  * **Threading**: UI-Updates müssen immer im UI-Thread stattfinden (`Dispatcher.Invoke`). Führen Sie langlaufende Operationen wie die Kamera-Initialisierung asynchron aus, um die UI nicht zu blockieren.
  * **Architektur (MVVM)**: Kapseln Sie die gesamte Kamera-Logik in einer separaten Service-Klasse (`CameraService`). Das ViewModel kommuniziert dann mit diesem Service und stellt die Daten (z.B. das `BitmapSource`-Objekt) per Data Binding der View (dem WPF-Fenster) zur Verfügung. Dies fördert die Testbarkeit und Wartbarkeit.
  * **Performance**: Nutzen Sie immer mehrere Bildpuffer (mindestens 3) für eine kontinuierliche Aufnahme. Vermeiden Sie aufwendige Konvertierungen im `onFrameEvent`-Handler, wenn hohe Frameraten benötigt werden.

-----

## 7\. Codebeispiele: Zentrale Snippets zum Wiederverwenden

### Minimale Kamera-Klasse (Grundgerüst):

```csharp
public class SimpleCamera : IDisposable
{
    private uEye.Camera m_Camera;
    private const int BUFFER_COUNT = 3;
    private bool _isDisposed = false;

    public bool InitializeCamera(int deviceId)
    {
        m_Camera = new uEye.Camera();

        // Kamera initialisieren (für WPF ohne Handle)
        var status = m_Camera.Init(deviceId | (int)uEye.Defines.DeviceEnumeration.UseDeviceID, IntPtr.Zero);
        if (status != uEye.Defines.Status.SUCCESS) return false;

        // Pixelformat setzen (Beispiel Mono8)
        status = m_Camera.PixelFormat.Set(uEye.Defines.ColorMode.Mono8);
        if (status != uEye.Defines.Status.SUCCESS) return false;

        // Speicher allokieren und Sequenz initialisieren
        status = m_Camera.Memory.Allocate(BUFFER_COUNT);
        if (status != uEye.Defines.Status.SUCCESS) return false;

        int[] idList;
        m_Camera.Memory.GetList(out idList);
        status = m_Camera.Memory.Sequence.Add(idList);

        return status == uEye.Defines.Status.SUCCESS;
    }

    public void StartLiveVideo(EventHandler onFrame)
    {
        if (m_Camera.IsOpened)
        {
            m_Camera.EventFrame += onFrame;
            m_Camera.Acquisition.Capture();
        }
    }

    public void StopLiveVideo(EventHandler onFrame)
    {
        if (m_Camera.IsOpened)
        {
            m_Camera.Acquisition.Stop();
            m_Camera.EventFrame -= onFrame;
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            if (m_Camera != null && m_Camera.IsOpened)
            {
                m_Camera.Exit();
            }
            _isDisposed = true;
        }
    }
}
```

### Parameter laden und speichern:

Die API ermöglicht das Speichern und Laden von Kamera-Parametern in/aus einer Datei (.ini).

```csharp
public static void SaveParametersToFile(uEye.Camera camera, string filename)
{
    camera.Parameter.Save(filename);
}

public static void LoadParametersFromFile(uEye.Camera camera, string filename)
{
    // Aufnahme muss vor dem Laden von Parametern gestoppt werden.
    bool wasLive;
    camera.Acquisition.HasStarted(out wasLive);
    if (wasLive) camera.Acquisition.Stop();

    camera.Parameter.Load(filename);

    // Ggf. Aufnahme wieder starten.
    if (wasLive) camera.Acquisition.Capture();
}
```

-----

## 8\. Wichtige Tipps zur Fehlerbehandlung und Performance

### Häufige Fehlerquellen:

  * **Initialisierungsfehler**: Prüfen Sie, ob die Kamera korrekt angeschlossen ist, die Treiber installiert sind und keine andere Anwendung die Kamera blockiert. Der "IDS Camera Manager" ist ein nützliches Tool zur Diagnose.
  * **Speicherfehler (`Allocate Memory failed`)**: Dies passiert, wenn nicht genügend RAM verfügbar ist. Geben Sie nicht mehr benötigte Puffer immer frei. Reduzieren Sie ggf. die Auflösung oder die Anzahl der Puffer.
  * **Gesperrte Puffer (`SEQ_BUFFER_IS_LOCKED`)**: Dieser Fehler kann auftreten, wenn die Bildverarbeitung im `onFrameEvent` zu lange dauert. Der Puffer kann dann nicht rechtzeitig für das nächste Bild wiederverwendet werden. Die Verarbeitung sollte so schlank wie möglich sein. Eine kurze Wartezeit mit `Thread.Sleep(1)` kann in manchen Fällen helfen, ist aber keine saubere Lösung.
  * **Parameterfehler (`INVALID_PARAMETER`)**: Tritt auf, wenn ein Wert außerhalb des gültigen Bereichs gesetzt wird. Fragen Sie den gültigen Bereich immer mit `GetRange()` ab, bevor Sie einen Wert setzen.

### Performance-Monitoring:

  * **FPS (Frames Per Second)**: Überwachen Sie die tatsächliche Bildrate, um Engpässe zu identifizieren.
  * **Fehlgeschlagene Frames**: Die API bietet die Möglichkeit, die Anzahl der verlorenen oder fehlerhaft übertragenen Bilder zu überwachen.

<!-- end list -->

```csharp
// Aktuelle FPS abrufen
Double currentFps;
m_Camera.Timing.Framerate.GetCurrentFps(out currentFps);

// Capture-Status abrufen (z.B. Anzahl fehlgeschlagener Übertragungen)
uEye.Types.CaptureStatus captureStatus;
m_Camera.Information.GetCaptureStatus(out captureStatus);
Console.WriteLine($"Fehlgeschlagene Frames: {captureStatus.Total}");
```

-----

## 9\. Widersprüche und Vergleich (Anhang)

Während der Analyse der Quelldokumente wurden folgende Unterschiede und potenzielle Konflikte identifiziert:

### 1\. `Camera.Init()` Methode:

  * **Unterschied**: Die Methode `Init()` wird in den Dokumenten mit unterschiedlichen Parametern aufgerufen.
  * **`uEye_DotNet_Technische_Example.txt`**: Verwendet `m_Camera.Init(deviceID | ..., pictureBoxDisplay.Handle)`. Dieser Aufruf übergibt ein Handle eines Windows-Forms-Controls. Die API rendert das Bild dann automatisch in dieses Control. Diese Methode ist für WPF ungeeignet. Ein anderes Beispiel in derselben Datei nutzt `IntPtr.Zero`, was der korrekte Ansatz für WPF ist.
  * **`IDS_uEye_DotNet_API_Dokumentation.txt` und `uEye_Kamera_Analyse_MiniTest.txt`**: Verwenden `m_Camera.Init(0)` oder `m_Camera.Init(deviceId)`. Dies ist eine vereinfachte Form, die ebenfalls kein Handle übergibt.
  * **Empfehlung für WPF**: Die beste Methode ist die explizite Initialisierung mit der `DeviceID` und `IntPtr.Zero` für den Handle: `m_Camera.Init(deviceID | (int)uEye.Defines.DeviceEnumeration.UseDeviceID, IntPtr.Zero)`. Das Bild muss dann manuell im `onFrameEvent` zu einer `BitmapSource` verarbeitet werden.

### 2\. Anwendungsframework (Windows Forms vs. WPF):

  * **Unterschied**: Die primäre Quelldokumentation (`uEye_DotNet_Technische_Example.txt`) beschreibt eine Windows Forms Anwendung. Die Zielanwendung in diesem Dokument ist jedoch WPF.
  * **Auswirkung**: Die Bilddarstellung ist fundamental anders. In Windows Forms wird das Bild über ein Handle und die `camera.Display.Render()`-Methode angezeigt. In WPF muss die Bildverarbeitung manuell erfolgen, indem die Rohdaten aus dem Puffer in ein `BitmapSource`-Objekt konvertiert und an ein `Image`-Control gebunden werden. Die Codebeispiele in diesem Dokument wurden entsprechend für WPF angepasst.

### 3\. Speicherverwaltung (`MemoryHelper`-Klasse):

  * **Unterschied**: Das technische Beispiel (`uEye_DotNet_Technische_Example.txt`) schlägt die Verwendung einer `MemoryHelper.cs`-Klasse vor, welche die Allokierung und Sequenzinitialisierung kapselt.
  * **Empfehlung**: Dieser Ansatz ist eine gute Praxis und kann übernommen werden. Alternativ können die Methoden `m_Camera.Memory.Allocate()` und `m_Camera.Memory.Sequence.Add()` auch direkt aufgerufen werden, wie es in einigen Code-Snippets der Fall ist. Die `MemoryHelper`-Klasse erhöht lediglich die Lesbarkeit.

-----