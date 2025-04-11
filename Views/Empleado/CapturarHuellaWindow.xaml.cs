using BiomentricoHolding.Services;
using DPFP;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BiomentricoHolding.Views.Empleado
{
    public partial class CapturarHuellaWindow : Window
    {
        private readonly CapturaHuellaService capturaService;

        public Template ResultadoTemplate { get; private set; }

        public ImageSource UltimaHuellaCapturada { get; private set; }

        public CapturarHuellaWindow()
        {
            InitializeComponent();

            capturaService = new CapturaHuellaService();

            // 🛑 Siempre detener captura previa
            capturaService.DetenerCaptura();

            // ✅ Reestablecer modo correcto
            capturaService.Modo = ModoCaptura.Registro;

            capturaService.Mensaje += MostrarMensajeTexto;
            capturaService.TemplateGenerado += HuellaCapturada;
            capturaService.MuestraProcesadaImagen += DibujarHuella;
            capturaService.IntentoFallido += MostrarFalloYReintentar;

            // ✅ Esperar un momento antes de reiniciar
            Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(500); // medio segundo
                capturaService.IniciarCaptura();
            });
        }

        private void MostrarMensajeTexto(string mensaje)
        {
            Dispatcher.Invoke(() =>
            {
                txtEstado.Text = mensaje;

                if (mensaje.Contains("Error: las muestras no coincidieron"))
                {
                    new MensajeWindow(mensaje).ShowDialog();
                }
            });
        }

        private void MostrarAlerta(string mensaje)
        {
            Dispatcher.Invoke(() =>
            {
                new MensajeWindow(mensaje).ShowDialog();
            });
        }

        private void HuellaCapturada(Template template)
        {
            ResultadoTemplate = template;

            Dispatcher.Invoke(() =>
            {
                MostrarAlerta("✅ Huella capturada correctamente.");
                this.DialogResult = true;
                this.Close(); // Cerramos la ventana
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
                new MensajeWindow("🛑 Las huellas no coincidieron.\n\nPor favor, intenta nuevamente.").ShowDialog();
                panelHuellas.Children.Clear();
                txtEstado.Text = "Coloca tu dedo nuevamente en el lector.";
            });
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            capturaService.DetenerCaptura(); // ✅ Para que el lector se libere correctamente
            this.Close();
        }

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        protected override void OnClosed(EventArgs e)
        {
            capturaService.DetenerCaptura(); // ✅ Seguridad adicional al cerrar
            base.OnClosed(e);
        }
    }
}
