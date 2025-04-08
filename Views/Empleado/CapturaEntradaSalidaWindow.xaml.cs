using BiomentricoHolding.Services;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using BiomentricoHolding.Data.DataBaseRegistro_Test;

namespace BiomentricoHolding.Views.Empleado
{
    public partial class CapturaEntradaSalidaWindow : Window
    {
        private DispatcherTimer _timer;
        private CapturaHuellaService _capturaService = new CapturaHuellaService();

        public CapturaEntradaSalidaWindow()
        {
            InitializeComponent();
            IniciarReloj();

            _capturaService.Mensaje += MostrarMensaje;
            _capturaService.MuestraProcesada += MostrarImagenHuella;
            _capturaService.TemplateGenerado += ProcesarHuellaCapturada;

            _capturaService.IniciarCaptura();
        }

        private void IniciarReloj()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += ActualizarReloj;
            _timer.Start();
        }

        private void ActualizarReloj(object sender, EventArgs e)
        {
            var cultura = new CultureInfo("es-CO");
            txtReloj.Text = DateTime.Now.ToString("HH:mm:ss", cultura);
            txtFecha.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy", cultura).ToUpper();
        }

        private void BtnReiniciar_Click(object sender, RoutedEventArgs e)
        {
            txtEstadoHuella.Text = "Por favor coloque su dedo en el lector";
            lblNombreEmpleado.Text = "Nombre: ---";
            lblDocumento.Text = "Documento: ---";
            lblTipoMarcacion.Text = "Marcación: ---";
            lblEstadoMarcacion.Text = "Estado: ---";
            imgHuella.Source = null;
            _capturaService.IniciarCaptura();
        }

        private void MostrarImagenHuella(Bitmap imagen)
        {
            Dispatcher.Invoke(() =>
            {
                imgHuella.Source = ConvertirBitmapToImageSource(imagen);
            });
        }

        private ImageSource ConvertirBitmapToImageSource(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        private void ProcesarHuellaCapturada(DPFP.Template templateCapturado)
        {
            Dispatcher.Invoke(() =>
            {
                var usuarios = ObtenerUsuariosDesdeBD(); // Simulado o real con EF
                var verificador = new DPFP.Verification.Verification();
                var resultado = new DPFP.Verification.Verification.Result();

                foreach (var usuario in usuarios)
                {
                    if (usuario.Huella == null) continue;

                    var templateBD = new DPFP.Template(new MemoryStream(usuario.Huella));

                    verificador.Verify(templateCapturado, templateBD, ref resultado);
                    if (resultado.Verified)
                    {
                        MostrarDatosEmpleado(usuario);
                        DeterminarTipoMarcacion(usuario);
                        return;
                    }
                }

                MostrarMensaje("❌ Huella no coincide con ningún usuario registrado.");
            });
        }

        private void MostrarDatosEmpleado(Usuario empleado)
        {
            lblNombreEmpleado.Text = $"Nombre: {empleado.Nombres} {empleado.Apellidos}";
            lblDocumento.Text = $"Documento: {empleado.Documento}";
            lblTipoMarcacion.Text = "Procesando...";
            lblEstadoMarcacion.Text = "---";
        }

        private void MostrarMensaje(string mensaje)
        {
            Dispatcher.Invoke(() => txtEstadoHuella.Text = mensaje);
        }

        // Simulación para que compile. Deberías implementar la consulta real.
        private List<Usuario> ObtenerUsuariosDesdeBD()
        {
            // Aquí debería ir tu lógica real de Entity Framework para traer los usuarios con huella
            return new List<Usuario>(); // Simulado
        }

        private void DeterminarTipoMarcacion(Usuario usuario)
        {
            // Aquí irá la lógica para consultar horario y registrar marcación
        }
    }
}
