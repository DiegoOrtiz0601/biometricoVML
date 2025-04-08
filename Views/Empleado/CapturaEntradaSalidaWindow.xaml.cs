using BiomentricoHolding.Services;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BiomentricoHolding.Data.DataBaseRegistro_Test;
using EmpleadoModel = BiomentricoHolding.Data.DataBaseRegistro_Test.Empleado;
using DPFP;

namespace BiomentricoHolding.Views.Empleado
{
    public partial class CapturaEntradaSalidaWindow : Window
    {
        private DispatcherTimer _timer;
        private readonly CapturaHuellaService _capturaService = new();

        public CapturaEntradaSalidaWindow()
        {
            InitializeComponent();
            IniciarReloj();
            ConfigurarEventosHuella();
        }

        private void IniciarReloj()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
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
            _capturaService.Modo = ModoCaptura.Verificacion;
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

        private void ProcesarHuellaVerificacion(DPFP.Sample sample)
        {
            MensajeWindow buscandoWindow = null;

            Dispatcher.Invoke(() =>
            {
                var extractor = new DPFP.Processing.FeatureExtraction();
                var feedback = DPFP.Capture.CaptureFeedback.None;

                FeatureSet features = new FeatureSet();
                extractor.CreateFeatureSet(sample, DPFP.Processing.DataPurpose.Verification, ref feedback, ref features);

                if (features == null || feedback != DPFP.Capture.CaptureFeedback.Good)
                {
                    MostrarMensaje("❌ No se pudo leer la huella correctamente.");
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
                            buscandoWindow?.Close();
                            MostrarDatosEmpleado(empleado);
                            DeterminarTipoMarcacion(empleado);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        buscandoWindow?.Close();
                        MostrarMensaje("❌ Error verificando huella: " + ex.Message);
                        LimpiarFormulario();
                        _capturaService.IniciarCaptura();
                        return;
                    }
                }

                buscandoWindow?.Close();
                MostrarMensaje("❌ Huella no coincide con ningún empleado registrado.");

                Dispatcher.BeginInvoke(() =>
                {
                    var alerta = new MensajeWindow("❌ Huella no coincide con ningún empleado.", 3);
                    alerta.ShowDialog();
                    LimpiarFormulario();
                    _capturaService.IniciarCaptura();
                });
            });
        }

        private void MostrarDatosEmpleado(EmpleadoModel empleado)
        {
            MostrarMensaje($"👤 Mostrando datos: {empleado.Nombres} {empleado.Apellidos}");
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

                var horaActual = DateTime.Now.TimeOfDay;
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

                var marcacion = new Marcacione
                {
                    IdEmpleado = empleado.IdEmpleado,
                    FechaHora = hoy,
                    IdEmpresa = empleado.IdEmpresa,
                    IdSede = empleado.IdSede,
                    IdTipoMarcacion = tipoMarcacion
                };

                db.Marcaciones.Add(marcacion);
                db.SaveChanges();

                lblTipoMarcacion.Text = tipoTexto;
                lblEstadoMarcacion.Text = "✔ Registrado";

                string hora = DateTime.Now.ToString("HH:mm:ss");
                string mensaje = $"✅ {tipoTexto} registrada\nHora: {hora}";

                Dispatcher.BeginInvoke(() =>
                {
                    var ventana = new MensajeWindow(mensaje, 3);
                    ventana.ShowDialog();
                    LimpiarFormulario();
                    _capturaService.IniciarCaptura();
                });
            }
            catch (Exception ex)
            {
                MostrarMensaje("❌ Error al registrar la marcación: " + ex.Message);
                lblTipoMarcacion.Text = "Error";
                lblEstadoMarcacion.Text = "⛔";
            }
        }
    }
}
