using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using uEye;
using SpectroSuite.src.Services;

namespace SpectroSuite.src.Views
{
    /// <summary>
    /// Interaktionslogik für CameraView.xaml
    /// </summary>
    public partial class CameraView : Page
    {
        private readonly CameraService _cameraService;
        private WriteableBitmap _bitmap;

        public CameraView()
        {
            InitializeComponent();

            _cameraService = new CameraService();

            // Kamera initialisieren und Livebild starten
            if (_cameraService.Initialize())
            {
                _cameraService.StartLiveVideo(OnFrameCaptured);
            }
            else
            {
                MessageBox.Show("Kamera konnte nicht initialisiert werden.");
            }
        }

        private void OnFrameCaptured(object sender, EventArgs e)
        {
            if (sender is Camera camera)
            {
                // Letztes Bild abrufen
                int memoryId;
                if (camera.Memory.GetLast(out memoryId) != uEye.Defines.Status.Success)
                    return;

                // Bild sperren
                if (camera.Memory.Lock(memoryId) == uEye.Defines.Status.Success)
                {
                    IntPtr buffer;
                    camera.Memory.ToIntPtr(memoryId, out buffer);

                    uEye.Types.ImageInfo info;
                    camera.Information.GetImageInfo(memoryId, out info);

                    int width = info.ImageSize.Width;
                    int height = info.ImageSize.Height;
                    int bitsPerPixel = 8;

                    int stride = (width * bitsPerPixel + 7) / 8;

                    // UI-Update über Dispatcher (da Event nicht im UI-Thread läuft)
                    Dispatcher.BeginInvoke(() =>
                    {
                        var bitmapSource = BitmapSource.Create(width, height, 96, 96,
                            PixelFormats.Gray8, null,
                            buffer, stride * height, stride);

                        _bitmap = new WriteableBitmap(bitmapSource);

                        CameraImage.Source = _bitmap;
                    });

                    camera.Memory.Unlock(memoryId);
                }
            }
        }

        
    }
}
