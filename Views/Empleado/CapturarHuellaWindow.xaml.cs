using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BiomentricoHolding.Services;
using BiomentricoHolding.Views;
using DPFP;
using System.Drawing;

namespace BiomentricoHolding.Views.Empleado
{
    public partial class CapturarHuellaWindow : Window
    {
        private readonly CapturaHuellaService capturaService;

        public Template ResultadoTemplate { get; private set; }

        // Imagen convertida para mostrar en RegistrarEmpleado
        public ImageSource UltimaHuellaCapturada { get; private set; }

        public CapturarHuellaWindow()
        {
            InitializeComponent();

            capturaService = new CapturaHuellaService
            {
                Modo = ModoCaptura.Registro // 👈 muy importante
            };

            capturaService.Mensaje += MostrarMensajeTexto;
            capturaService.TemplateGenerado += HuellaCapturada;
            capturaService.MuestraProcesadaImagen += DibujarHuella; // 👈 actualizamos este
            capturaService.IntentoFallido += MostrarFalloYReintentar;

            capturaService.IniciarCaptura();
        }

        private void MostrarMensajeTexto(string mensaje)
        {
            Dispatcher.Invoke(() =>
            {
                txtEstado.Text = mensaje;

                if (mensaje.Contains("Error: las muestras no coincidieron"))
                {
                    var alert = new MensajeWindow(mensaje);
                    alert.ShowDialog();
                }
            });
        }

        private void MostrarAlerta(string mensaje)
        {
            Dispatcher.Invoke(() =>
            {
                var msg = new MensajeWindow(mensaje);
                msg.ShowDialog();
            });
        }

        private void HuellaCapturada(Template template)
        {
            ResultadoTemplate = template;

            Dispatcher.Invoke(() =>
            {
                MostrarAlerta("✅ Huella capturada correctamente.");
                this.DialogResult = true;
                this.Close();
            });
        }

        private void DibujarHuella(Bitmap bmp)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    IntPtr hBitmap = bmp.GetHbitmap();

                    var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(100, 100)
                    );

                    UltimaHuellaCapturada = bitmapSource;

                    var image = new System.Windows.Controls.Image
                    {
                        Source = bitmapSource,
                        Width = 100,
                        Height = 100,
                        Margin = new Thickness(5)
                    };

                    panelHuellas.Children.Add(image);

                    DeleteObject(hBitmap);
                }
                catch (Exception ex)
                {
                    MostrarAlerta("❌ Error al mostrar imagen: " + ex.Message);
                }
            });
        }

        private void MostrarFalloYReintentar()
        {
            Dispatcher.Invoke(() =>
            {
                var mensaje = new MensajeWindow("🛑 Las huellas no coincidieron.\n\nPor favor, intenta nuevamente.");
                mensaje.ShowDialog();

                panelHuellas.Children.Clear();
                txtEstado.Text = "Coloca tu dedo nuevamente en el lector.";
            });
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
