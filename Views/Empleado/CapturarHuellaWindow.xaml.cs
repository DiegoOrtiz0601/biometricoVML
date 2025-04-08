using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BiomentricoHolding.Services;
using BiomentricoHolding.Views; // Asegúrate de que esté presente
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

            capturaService = new CapturaHuellaService();
            capturaService.Mensaje += MostrarMensajeTexto; // Solo en label
            capturaService.TemplateGenerado += HuellaCapturada;
            capturaService.MuestraProcesada += DibujarHuella;
            capturaService.IntentoFallido += MostrarFalloYReintentar;

            capturaService.IniciarCaptura();
        }

        // 👉 Mensaje solo en etiqueta
        private void MostrarMensajeTexto(string mensaje)
        {
            Dispatcher.Invoke(() =>
            {
                txtEstado.Text = mensaje;

                // Si el mensaje indica fallo de enrolamiento, se muestra también alerta visual
                if (mensaje.Contains("Error: las muestras no coinciden"))
                {
                    var alert = new MensajeWindow(mensaje);
                    alert.ShowDialog();
                }
            });
        }

        // 👉 Mensaje tipo alerta con MensajeWindow
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

        private void DibujarHuella(System.Drawing.Bitmap bmp)
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

                    // Limpieza de memoria nativa
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

                // Limpia imágenes previas
                panelHuellas.Children.Clear();

                // Reinicia visualmente el estado
                txtEstado.Text = "Coloca tu dedo nuevamente en el lector.";
            });
        }
        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Libera memoria del bitmap nativo
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
