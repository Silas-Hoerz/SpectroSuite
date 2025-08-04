using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uEye;

namespace SpectroSuite.src.Services
{
    internal class CameraService : IDisposable
    {
        
        private Camera _camera;

        public bool isOpened => _camera?.IsOpened ?? false;

        public CameraService()
        {
            _camera = new Camera();
        }

        /// <summary>
        /// Initialisiert die Kamera mit einer bestimmten DeviceID.
        /// Allokiert Bildpuffer und setzt das Pixelformat.
        /// </summary>
        /// <param name="deviceId">Die ID des Geräts (Standard: 0)</param>
        /// <returns>true bei Erfolg, sonst false</returns>
        public bool Initialize(int deviceId = 0)
        {
            // Initialisiert die Kamera mit DeviceID und IntPtr.Zero (manuelles Rendern für WPF)
            var status = _camera.Init(deviceId | (int)uEye.Defines.DeviceEnumeration.UseDeviceID, IntPtr.Zero);
            if (status != uEye.Defines.Status.Success)
                return false;

            // Setzt das Pixelformat auf Mono8 (8 Bit Graustufen, passend für UI-2212SE-M)
            _camera.PixelFormat.Set(uEye.Defines.ColorMode.Mono8);

            // Anzahl der Bildpuffer, die allokiert werden sollen
            const int BUFFER_COUNT = 3;
            List<int> memoryIDs = new List<int>();

            // Bestimmt die Bildgröße (Breite, Höhe) und Bits pro Pixel
            int width, height, bitsPerPixel;
            _camera.Size.AOI.Get(out _, out _, out width, out height);
            _camera.PixelFormat.Get(out uEye.Defines.ColorMode color);
            bitsPerPixel = 8; // Mono8 = 8 Bit pro Pixel

            // Allokiert die Bildpuffer im Speicher
            for (int i = 0; i < BUFFER_COUNT; i++)
            {
                int memId;
                var aloc_status = _camera.Memory.Allocate(width, height, bitsPerPixel, true, out memId);
                if (aloc_status == uEye.Defines.Status.Success)
                {
                    memoryIDs.Add(memId);
                }
                else
                    throw new Exception($"Memory allocation for uEye failed: {aloc_status}");
            }

            // Fügt die allokierten Speicherbereiche zur Bildsequenz hinzu
            _camera.Memory.Sequence.Add(memoryIDs.ToArray());

            return true;
        }

        /// <summary>
        /// Startet das Livebild der Kamera und registriert einen Event-Handler für neue Frames.
        /// </summary>
        /// <param name="onFrame">Event-Handler, der bei jedem neuen Frame ausgelöst wird</param>
        public void StartLiveVideo(EventHandler onFrame)
        {
            if (_camera.IsOpened)
            {
                // Registriert den Event-Handler für neue Frames
                _camera.EventFrame += onFrame;
                // Startet die Bildaufnahme
                _camera.Acquisition.Capture();
            }
        }

        /// <summary>
        /// Stoppt das Livebild und entfernt den Event-Handler.
        /// </summary>
        /// <param name="onFrame">Event-Handler, der entfernt werden soll</param>
        public void StopLiveVideo(EventHandler onFrame)
        {
            if (_camera.IsOpened)
            {
                // Stoppt die Bildaufnahme
                _camera.Acquisition.Stop();
                // Entfernt den Event-Handler
                _camera.EventFrame -= onFrame;
            }
        }

        /// <summary>
        /// Gibt die Ressourcen der Kamera frei.
        /// </summary>
        public void Dispose()
        {
            if (_camera != null && _camera.IsOpened)
            {
                // Beendet die Kameraverbindung und gibt Ressourcen frei
                _camera.Exit();
            }
        }
    }
}
