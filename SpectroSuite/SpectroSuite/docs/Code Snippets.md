Zuerst die Recherche-Ergebnisse zur Kamera: Die **IDS uEye UI-2212SE-M** ist eine industrielle Bildverarbeitungskamera. Es handelt sich um eine **monochrome (Schwarz-Weiß) Kamera** mit einem CCD-Sensor in WVGA-Auflösung (768 x 576 Pixel). Sie wird typischerweise über eine **USB 2.0-Schnittstelle** angebunden. Die "M" in der Bezeichnung steht für "Monochrome". Da es eine monochrome Kamera ist, sind Einstellungen wie Weißabgleich irrelevant, während Parameter wie Pixelformat (z.B. 8-Bit oder 12-Bit Graustufen), Gain und Belichtungszeit von zentraler Bedeutung sind.

Basierend auf dieser Information und den bereitgestellten Dokumenten, hier sind die C\#-Code-Snippets zur Entwicklung einer vollumfänglichen Steuerung.

-----

### **Grundlagen und Setup**

Zuerst müssen die notwendigen Namespaces eingebunden und das Haupt-Kameraobjekt deklariert werden. Dieses Objekt wird alle Interaktionen mit der Kamera steuern.

```csharp
// Notwendige Namespaces für die IDS uEye API.
using uEye; [cite_start]// Hauptnamespace mit der zentralen Camera-Klasse [cite: 18]
using uEye.Defines; [cite_start]// Enthält wichtige Enums wie Status, ColorMode, etc. [cite: 18]
using System.Windows.Media.Imaging; [cite_start]// Für die Darstellung in WPF (BitmapSource) [cite: 3]
using System.Windows; [cite_start]// Für den Dispatcher in WPF [cite: 5]

public class CameraController : IDisposable
{
    // Das zentrale Kamera-Objekt, das eine Instanz der physischen Kamera repräsentiert.
    [cite_start]private uEye.Camera m_Camera; [cite: 3, 30]

    [cite_start]// Ein Lock-Objekt zur Gewährleistung der Thread-Sicherheit bei Zugriffen auf die Kamera. [cite: 6]
    private readonly object _lockObject = new object();

    // ... weitere Methoden folgen ...
}
```

-----

### **Schritt 1: Verfügbare Kameras finden und auswählen**

Bevor eine Verbindung hergestellt werden kann, muss die Anwendung wissen, welche Kameras angeschlossen sind. Die API bietet eine Funktion, um eine Liste aller erkannten Geräte abzurufen.

```csharp
/// <summary>
/// Sucht nach allen verfügbaren uEye-Kameras und gibt die Device-ID der ersten
/// unbenutzten Kamera zurück. Gibt -1 zurück, wenn keine Kamera gefunden wurde.
/// </summary>
public int FindCamera()
{
    // Ruft eine Liste aller an das System angeschlossenen Kameras ab.
    uEye.Types.CameraInformation[] cameraList;
    [cite_start]uEye.Info.Camera.GetCameraList(out cameraList); [cite: 69]

    if (cameraList == null || cameraList.Length == 0)
    {
        MessageBox.Show("Keine Kameras gefunden!");
        [cite_start]return -1; [cite: 136]
    }

    // Wir wählen die erste Kamera aus, die nicht bereits in Benutzung ist.
    foreach (var cam in cameraList)
    {
        // Prüft das "InUse"-Flag, um sicherzustellen, dass die Kamera nicht von
        // einer anderen Anwendung blockiert wird.
        if (!cam.InUse)
        {
            // Die DeviceID wird für die Initialisierung benötigt.
            [cite_start]return cam.DeviceID; [cite: 137]
        }
    }

    MessageBox.Show("Alle Kameras sind bereits in Verwendung!");
    [cite_start]return -1; [cite: 138]
}
```

-----

### **Schritt 2: Kamera initialisieren und verbinden**

Nachdem eine Kamera-ID gefunden wurde, kann die Verbindung hergestellt werden. Die `Init()`-Methode ist hierfür zentral.

**\!\! KONFLIKT UND LÖSUNGSSTRATEGIE \!\!**

Die Quelldokumente zeigen unterschiedliche Wege, die `Init()`-Methode aufzurufen:

1.  [cite\_start]**`m_Camera.Init(0)`**: Initialisiert die erste verfügbare Kamera[cite: 4]. Dies ist einfach, aber nicht robust, wenn mehrere Kameras angeschlossen sind.
2.  [cite\_start]**`m_Camera.Init(deviceID, handle)`**: Initialisiert eine spezifische Kamera und übergibt ein Fenster-Handle für die direkte Bildanzeige[cite: 53]. Dies ist typisch für Windows Forms, aber **ungeeignet für WPF**, wo die Bilddarstellung anders funktioniert.
3.  [cite\_start]**`m_Camera.Init(deviceID, IntPtr.Zero)`**: Initialisiert eine spezifische Kamera ohne ein Fenster-Handle[cite: 118]. **Dies ist die empfohlene Methode für WPF**, da sie die Logik von der Darstellung entkoppelt.

<!-- end list -->

```csharp
/// <summary>
/// Initialisiert die Kamera mit der angegebenen Device-ID.
/// </summary>
/// <param name="deviceId">Die ID der zu initialisierenden Kamera.</param>
/// <returns>True bei Erfolg, andernfalls False.</returns>
public bool InitializeCamera(int deviceId)
{
    m_Camera = new uEye.Camera();

    // Wir verwenden die empfohlene Methode für WPF: Initialisierung mit der Device-ID
    // und einem Null-Pointer für das Fenster-Handle, da wir das Bild manuell rendern.
    // Das Flag "UseDeviceID" stellt sicher, dass der int-Wert als Device-ID interpretiert wird.
    [cite_start]Status status = m_Camera.Init(deviceId | (int)DeviceEnumeration.UseDeviceID, IntPtr.Zero); [cite: 118]

    if (status != Status.SUCCESS)
    {
        MessageBox.Show("Kamera-Initialisierung fehlgeschlagen: " + status);
        return false;
    }

    return true;
}
```

-----

### **Schritt 3: Kamera-Parameter für die UI-2212SE-M konfigurieren**

Nach der Initialisierung müssen die Parameter gesetzt werden. Für die monochrome UI-2212SE-M sind dies vor allem das Pixelformat, die Belichtung und der Gain.

```csharp
/// <summary>
/// Konfiguriert die grundlegenden Parameter für die UI-2212SE-M.
/// </summary>
public void ConfigureCamera()
{
    // --- 1. Pixelformat setzen ---
    // Da die UI-2212SE-M eine monochrome Kamera ist, ist Mono8 (8-Bit Graustufen)
    [cite_start]// eine Standardwahl für gute Performance und ausreichende Bildqualität. [cite: 3]
    [cite_start]// Für einen höheren Dynamikumfang kann Mono12 oder SensorRaw12 verwendet werden. [cite: 21]
    m_Camera.PixelFormat.Set(ColorMode.Mono8);

    // --- 2. Bildspeicher allokieren ---
    // Für eine flüssige Live-Ansicht werden mehrere Puffer benötigt. [cite_start]3 ist ein gängiger Wert. [cite: 3, 31]
    // Wir allokieren die Puffer und fügen sie einer Aufnahmesequenz hinzu.
    m_Camera.Memory.Allocate(3);
    int[] sequence;
    m_Camera.Memory.GetList(out sequence);
    [cite_start]m_Camera.Memory.Sequence.Add(sequence); [cite: 89, 90]

    // --- 3. Belichtungszeit, Framerate und PixelClock ---
    // Diese Werte hängen voneinander ab. Eine höhere PixelClock erlaubt eine höhere Framerate.
    // PixelClock (in MHz)
    Range<int> pixelClockRange;
    m_Camera.Timing.PixelClock.GetRange(out pixelClockRange);
    m_Camera.Timing.PixelClock.Set(pixelClockRange.Maximum); [cite_start]// Maximale Auslesegeschwindigkeit [cite: 75]

    // Framerate (in fps - Bilder pro Sekunde)
    // Wir setzen die Framerate auf einen gewünschten Wert, z.B. 30 fps.
    [cite_start]m_Camera.Timing.Framerate.Set(30.0); [cite: 77, 140]

    // Belichtungszeit (in ms)
    // Eine feste Belichtungszeit von 10ms als Beispiel.
    [cite_start]m_Camera.Timing.Exposure.Set(10.0); [cite: 20, 73, 139]

    // --- 4. Gain (Verstärkung) ---
    // Setzt die Hardware-Verstärkung. Werte von 0-100.
    // Da es eine Monochrom-Kamera ist, ist nur der Master-Gain relevant.
    [cite_start]m_Camera.Gain.Hardware.Scaled.SetMaster(50); [cite: 78, 141]

    // Alternativ kann die Auto-Gain-Funktion genutzt werden.
    [cite_start]// m_Camera.AutoFeatures.Software.Gain.SetEnable(true); [cite: 3]
}
```

-----

### **Schritt 4: Bildaufnahme und Verarbeitung für WPF**

Die Bildaufnahme startet asynchron. Ein Event wird ausgelöst, sobald ein neues Bild im Puffer verfügbar ist.

**\!\! KONFLIKT UND LÖSUNGSSTRATEGIE \!\!**

Die Verarbeitung des Bildes unterscheidet sich je nach UI-Framework:

1.  [cite\_start]**Windows Forms (`uEye_DotNet_Technische_Example.txt`)**: Verwendet `camera.Display.Render()` oder `camera.Memory.ToBitmap()`[cite: 103, 106]. Dies erzeugt ein `System.Drawing.Bitmap`.
2.  **WPF (`uEye_Kamera_Analyse_MiniTest.txt`)**: Benötigt ein `System.Windows.Media.Imaging.BitmapSource`. [cite\_start]Die Rohdaten aus dem Kamerabuffer müssen manuell konvertiert werden[cite: 5, 10]. **Dies ist der hier gezeigte, korrekte Ansatz für WPF.**

<!-- end list -->

```csharp
// Event-Handler für neue Frames deklarieren
public event Action<BitmapSource> FrameReceived;

/// <summary>
/// Startet die kontinuierliche Bildaufnahme (Live-Video).
/// </summary>
public void StartLiveVideo()
{
    // Registriere den Event-Handler, der bei jedem neuen Frame aufgerufen wird.
    [cite_start]m_Camera.EventFrame += OnFrameEvent; [cite: 64]
    // Starte die Aufnahme. Dies ist eine nicht-blockierende Operation.
    [cite_start]m_Camera.Acquisition.Capture(); [cite: 3, 42]
}

/// <summary>
/// Event-Handler, der die Bilddaten verarbeitet und an die WPF-UI weitergibt.
/// </summary>
private void OnFrameEvent(object sender, EventArgs e)
{
    int memoryId;
    // Hole die ID des zuletzt aufgenommenen Bildes.
    [cite_start]var status = m_Camera.Memory.GetLast(out memoryId); [cite: 3, 101]
    if (status == Status.SUCCESS && memoryId > 0)
    {
        // Sperre den Speicherpuffer für den exklusiven Zugriff.
        [cite_start]if (m_Camera.Memory.Lock(memoryId) == Status.SUCCESS) [cite: 3]
        {
            try
            {
                // Hole die Bildinformationen (Breite, Höhe, etc.).
                uEye.Types.ImageInfo info;
                m_Camera.Information.GetImageInfo(memoryId, out info);

                int width = info.Width;
                int height = info.Height;
                int bitsPerPixel = info.BitsPerPixel;
                // Stride = Bytes pro Bildzeile
                [cite_start]int stride = (width * bitsPerPixel + 7) / 8; [cite: 5]

                // Hole einen Pointer auf die Rohdaten im Speicher.
                IntPtr buffer;
                m_Camera.Memory.GetBuffer(memoryId, out buffer);

                // Erstelle eine WPF-kompatible BitmapSource.
                // Für Mono8 ist das Pixelformat Gray8.
                // Für Mono12 müsste hier Gray16 verwendet und die Daten ggf. konvertiert werden.
                var pixelFormat = PixelFormats.Gray8;
                var bitmapSource = BitmapSource.Create(width, height, 96, 96,
                    [cite_start]pixelFormat, null, buffer, stride * height, stride); [cite: 10]

                // Friere das Bild ein, um es Thread-sicher an die UI zu übergeben.
                bitmapSource.Freeze();

                // Feuere das Event, um die UI zu benachrichtigen.
                // Dies wird im UI-Thread über den Dispatcher aufgerufen.
                Application.Current.Dispatcher.Invoke(() => FrameReceived?.Invoke(bitmapSource));
            }
            finally
            {
                // Gib den Speicherpuffer unbedingt wieder frei, damit er neu beschrieben werden kann.
                [cite_start]m_Camera.Memory.Unlock(memoryId); [cite: 3]
            }
        }
    }
}
```

-----

### **Schritt 5: Ressourcen sauber freigeben (Dispose Pattern)**

Es ist extrem wichtig, alle von der Kamera belegten Ressourcen (Speicher, Handles) am Ende wieder freizugeben, um Abstürze oder blockierte Kameras zu vermeiden.

```csharp
/// <summary>
/// Gibt alle von der Kamera verwendeten Ressourcen frei.
/// </summary>
public void Dispose()
{
    if (m_Camera != null)
    {
        // Stoppe die Bildaufnahme, falls sie läuft.
        [cite_start]m_Camera.Acquisition.Stop(); [cite: 67]

        // Deregistriere den Event-Handler, um Speicherlecks zu vermeiden.
        [cite_start]m_Camera.EventFrame -= OnFrameEvent; [cite: 67]

        // Gib die allokierten Bildpuffer frei.
        int[] sequence;
        if (m_Camera.Memory.GetList(out sequence) == Status.SUCCESS)
        {
            foreach (var memId in sequence)
            {
                [cite_start]m_Camera.Memory.Free(memId); [cite: 49, 93]
            }
        }

        // Schließe die Kameraverbindung und gib alle Handles frei.
        [cite_start]m_Camera.Exit(); [cite: 50, 68]
        m_Camera = null;
    }
}
```