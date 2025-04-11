using BiomentricoHolding.Data.DataBaseRegistro_Test;
using BiomentricoHolding.Services;
using BiomentricoHolding.Utils;
using DPFP;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EmpleadoModel = BiomentricoHolding.Data.DataBaseRegistro_Test.Empleado;

namespace BiomentricoHolding.Views.Empleado
{
    public partial class CapturaEntradaSalidaWindow : Window
    {
        private DispatcherTimer _timer;
        private readonly CapturaHuellaService _capturaService = new();

        public CapturaEntradaSalidaWindow()
        {
            InitializeComponent();
            Logger.Agregar("📡 Iniciando módulo de verificación de huella.");

            IniciarReloj();
            ConfigurarEventosHuella();
        }

        private void IniciarReloj()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                var cultura = new CultureInfo("es-CO");
                txtReloj.Text = DateTime.Now.ToString("HH:mm:ss", cultura);
                txtFecha.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy", cultura).ToUpper();
            };
            _timer.Start();
        }

        private void ConfigurarEventosHuella()
        {
            _capturaService.Modo = ModoCaptura.Verificacion;
            _capturaService.Mensaje += MostrarMensaje;
            _capturaService.MuestraProcesada += ProcesarHuellaVerificacion;
            _capturaService.MuestraProcesadaImagen += MostrarImagenHuella;
            _capturaService.IniciarCaptura();
        }

        private void BtnReiniciar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
            _capturaService.DetenerCaptura(); // Detenemos por seguridad antes de reiniciar
            _capturaService.IniciarCaptura();
        }

        private void LimpiarFormulario()
        {
            txtEstadoHuella.Text = "Por favor coloque su dedo en el lector";
            lblNombreEmpleado.Text = "Nombre: ---";
            lblDocumento.Text = "Documento: ---";
            lblTipoMarcacion.Text = "Marcación: ---";
            lblEstadoMarcacion.Text = "Estado: ---";
            imgHuella.Source = null;
        }

        private void MostrarMensaje(string mensaje)
        {
            Dispatcher.Invoke(() => txtEstadoHuella.Text = mensaje);
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
            using var memory = new MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            memory.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        private void ProcesarHuellaVerificacion(Sample sample)
        {
            _capturaService.DetenerCaptura(); // Siempre detener antes de procesar

            MensajeWindow buscandoWindow = null;

            Dispatcher.Invoke(() =>
            {
                Logger.Agregar("🧠 Procesando muestra de huella digital...");

                var extractor = new DPFP.Processing.FeatureExtraction();
                var feedback = DPFP.Capture.CaptureFeedback.None;
                FeatureSet features = new FeatureSet();
                extractor.CreateFeatureSet(sample, DPFP.Processing.DataPurpose.Verification, ref feedback, ref features);

                if (features == null || feedback != DPFP.Capture.CaptureFeedback.Good)
                {
                    Logger.Agregar("❌ No se pudo leer la huella correctamente.");
                    MostrarMensaje("❌ No se pudo leer la huella correctamente.");
                    _capturaService.IniciarCaptura();
                    return;
                }

                buscandoWindow = new MensajeWindow("🔍 Buscando huella...", false, true);
                buscandoWindow.Show();

                using var db = new DataBaseRegistro_TestDbContext();
                var empleados = db.Empleados.Where(e => e.Huella != null && e.Estado == true).ToList();
                var verificador = new DPFP.Verification.Verification();
                var resultado = new DPFP.Verification.Verification.Result();

                foreach (var empleado in empleados)
                {
                    try
                    {
                        var templateBD = new Template(new MemoryStream(empleado.Huella));
                        verificador.Verify(features, templateBD, ref resultado);

                        if (resultado.Verified)
                        {
                            Logger.Agregar($"✅ Huella verificada: {empleado.Nombres} {empleado.Apellidos} ({empleado.Documento})");
                            buscandoWindow?.Close();
                            MostrarDatosEmpleado(empleado);

                            // Continuar verificación en segundo plano
                            Dispatcher.InvokeAsync(() => DeterminarTipoMarcacion(empleado));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Agregar($"❌ Error verificando huella: {ex.Message}");
                        buscandoWindow?.Close();
                        MostrarMensaje("❌ Error verificando huella: " + ex.Message);
                        LimpiarFormulario();
                        _capturaService.IniciarCaptura();
                        return;
                    }
                }

                Logger.Agregar("❌ Huella no coincide con ningún empleado registrado.");
                buscandoWindow?.Close();

                Dispatcher.BeginInvoke(() =>
                {
                    MostrarMensaje("❌ Huella no coincide con ningún empleado.");
                    var alerta = new MensajeWindow("❌ Huella no coincide con ningún empleado. Por favor intente nuevamente", 3);
                    alerta.Show();
                    _capturaService.IniciarCaptura();
                });
            });
        }

        private void MostrarDatosEmpleado(EmpleadoModel empleado)
        {
            lblNombreEmpleado.Text = $"Nombre: {empleado.Nombres} {empleado.Apellidos}";
            lblDocumento.Text = $"Documento: {empleado.Documento}";
            lblTipoMarcacion.Text = "Procesando...";
            lblEstadoMarcacion.Text = "---";
        }

        private void DeterminarTipoMarcacion(EmpleadoModel empleado)
        {
            try
            {
                using var db = new DataBaseRegistro_TestDbContext();

                var hoy = DateTime.Now;
                var diaSemana = (int)hoy.DayOfWeek;
                if (diaSemana == 0) diaSemana = 7;

                var horario = db.EmpleadosHorarios.FirstOrDefault(h =>
                    h.EmpleadoId == empleado.IdEmpleado &&
                    h.DiaSemana == diaSemana &&
                    h.Estado == true);

                if (horario == null)
                {
                    MostrarMensaje("⚠ No hay horario configurado para hoy.");
                    lblTipoMarcacion.Text = "Sin horario";
                    lblEstadoMarcacion.Text = "⛔";
                    return;
                }

                var horaActual = hoy.TimeOfDay;
                var entrada = horario.Inicio.ToTimeSpan();
                var salida = horario.Fin.ToTimeSpan();

                var entradaDesde = entrada - TimeSpan.FromHours(1);
                var entradaHasta = entrada + TimeSpan.FromHours(1);
                var salidaDesde = salida - TimeSpan.FromHours(1);
                var salidaHasta = salida + TimeSpan.FromHours(1);

                int tipoMarcacion = 3;
                string tipoTexto = "Novedad";

                if (horaActual >= entradaDesde && horaActual <= entradaHasta)
                {
                    tipoMarcacion = 1;
                    tipoTexto = "Entrada";
                }
                else if (horaActual >= salidaDesde && horaActual <= salidaHasta)
                {
                    tipoMarcacion = 2;
                    tipoTexto = "Salida";
                }

                var cincoMinutosAtras = hoy.AddMinutes(-5);
                var ultima = db.Marcaciones
                    .Where(m => m.IdEmpleado == empleado.IdEmpleado && m.FechaHora >= cincoMinutosAtras)
                    .OrderByDescending(m => m.FechaHora)
                    .FirstOrDefault();

                if (ultima != null)
                {
                    var diferencia = hoy - ultima.FechaHora;
                    var mensaje = $"⚠ {empleado.Nombres} {empleado.Apellidos} ya registró una marcación de tipo {ultima.IdTipoMarcacion} hace {diferencia.Minutes} min {diferencia.Seconds} seg.\n⏳ Debe esperar 5 minutos.";
                    new MensajeWindow(mensaje, 5, tipo: "advertencia").Show();
                    _capturaService.IniciarCaptura();
                    return;
                }

                var marcacion = new Marcacione
                {
                    IdEmpleado = empleado.IdEmpleado,
                    FechaHora = hoy,
                    IdEmpresa = empleado.IdEmpresa,
                    IdSede = ConfiguracionSistema.IdSedeActual ?? empleado.IdSede,
                    IdTipoMarcacion = tipoMarcacion
                };

                db.Marcaciones.Add(marcacion);
                db.SaveChanges();

                lblTipoMarcacion.Text = tipoTexto;
                lblEstadoMarcacion.Text = "✔ Registrado";

                string hora = hoy.ToString("HH:mm:ss");
                new MensajeWindow($"✅ {tipoTexto} registrada\nHora: {hora}", 3).Show();
                _capturaService.IniciarCaptura();
            }
            catch (Exception ex)
            {
                MostrarMensaje("❌ Error al registrar la marcación: " + ex.Message);
                lblTipoMarcacion.Text = "Error";
                lblEstadoMarcacion.Text = "⛔";
            }
        }

        // 🚨 IMPORTANTE: Detener captura al cerrar la ventana
        protected override void OnClosed(EventArgs e)
        {
            _capturaService.DetenerCaptura();
            base.OnClosed(e);
        }
    }
}
