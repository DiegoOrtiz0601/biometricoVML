using BiomentricoHolding.Data.DataBaseRegistro_Test;
using BiomentricoHolding.Services;
using BiomentricoHolding.Utils;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace BiomentricoHolding.Views.Empleado
{
    public partial class RegistrarEmpleado : UserControl
    {
        private DPFP.Template _huellaCapturada = null;

        private bool esModificacion = false;
        private int idEmpleadoActual = 0;
        public RegistrarEmpleado()
        {
            InitializeComponent();
            Logger.Agregar("🔄 Abriendo formulario de Registro de Empleado");
            CargarEmpresas();
            CargarTiposEmpleado();
        }

        private void CargarEmpresas()
        {
            using (var context = AppSettings.GetContextUno())
            {
                cbEmpresa.ItemsSource = context.Empresas.OrderBy(e => e.Nombre).ToList();
                cbEmpresa.DisplayMemberPath = "Nombre";
                cbEmpresa.SelectedValuePath = "IdEmpresa";
            }
        }

        private void CargarTiposEmpleado()
        {
            using (var context = AppSettings.GetContextUno())
            {
                cbTipoEmpleado.ItemsSource = context.TiposEmpleados
                    .Where(t => t.Estado)
                    .OrderBy(t => t.Nombre)
                    .ToList();

                cbTipoEmpleado.DisplayMemberPath = "Nombre";
                cbTipoEmpleado.SelectedValuePath = "Id";
            }
        }

        private void cbEmpresa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbSede.ItemsSource = null;
            cbArea.ItemsSource = null;

            if (cbEmpresa.SelectedValue is int idEmpresa)
            {
                Logger.Agregar($"🏢 Empresa seleccionada: ID {idEmpresa}");

                using (var context = AppSettings.GetContextUno())
                {
                    cbSede.ItemsSource = context.Sedes
                        .Where(s => s.IdEmpresa == idEmpresa)
                        .OrderBy(s => s.Nombre)
                        .ToList();

                    cbSede.DisplayMemberPath = "Nombre";
                    cbSede.SelectedValuePath = "IdSede";
                }
            }
        }

        private void cbSede_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbArea.ItemsSource = null;

            if (cbSede.SelectedValue is int idSede)
            {
                Logger.Agregar($"📍 Sede seleccionada: ID {idSede}");

                using (var context = AppSettings.GetContextUno())
                {
                    cbArea.ItemsSource = context.Areas
                        .Where(a => a.IdSede == idSede)
                        .OrderBy(a => a.Nombre)
                        .ToList();

                    cbArea.DisplayMemberPath = "Nombre";
                    cbArea.SelectedValuePath = "IdArea";
                }
            }
        }

        private void txtCedula_LostFocus(object sender, RoutedEventArgs e)
        {
            string cedulaTexto = txtCedula.Text.Trim();
            var icon = (TextBlock)this.FindName("iconCedulaCheck");

            if (string.IsNullOrWhiteSpace(cedulaTexto))
            {
                icon.Visibility = Visibility.Collapsed;
                return;
            }

            if (int.TryParse(cedulaTexto, out int cedula))
            {
                if (CedulaExiste(cedula))
                {
                    icon.Visibility = Visibility.Collapsed;
                    MostrarModalUsuarioYaExiste(cedulaTexto);
                }
                else
                {
                    icon.Visibility = Visibility.Visible;
                }
            }
            else
            {
                new MensajeWindow("⚠ La cédula ingresada no es válida.").ShowDialog();
                icon.Visibility = Visibility.Collapsed;
            }
        }

        private bool CedulaExiste(int cedula)
        {
            using (var context = AppSettings.GetContextUno())
            {
                return context.Empleados.Any(e => e.Documento == cedula);
            }
        }

        private void MostrarModalUsuarioYaExiste(string cedulaTexto)
        {
            var modal = new EmpleadoYaExisteDialog(cedulaTexto);
            bool? result = modal.ShowDialog();

            if (result == true && modal.Modificar)
            {
                Logger.Agregar($"✏️ Usuario con cédula {cedulaTexto} desea modificar datos.");

                if (int.TryParse(cedulaTexto, out int cedula))
                {
                    using (var context = AppSettings.GetContextUno())
                    {
                        var empleado = context.Empleados.FirstOrDefault(e => e.Documento == cedula);
                        if (empleado != null)
                        {
                            esModificacion = true;
                            idEmpleadoActual = empleado.IdEmpleado;

                            txtTituloFormulario.Text = "✏️ Actualización de Empleado";
                            btnRegistrar.Content = "💾 Actualizar";

                            txtNombres.Text = empleado.Nombres;
                            txtApellidos.Text = empleado.Apellidos;
                            txtCedula.Text = empleado.Documento.ToString();
                            cbEmpresa.SelectedValue = empleado.IdEmpresa;
                            cbTipoEmpleado.SelectedValue = empleado.IdTipoEmpleado;

                            cbEmpresa_SelectionChanged(null, null);
                            cbSede.SelectedValue = empleado.IdSede;

                            cbSede_SelectionChanged(null, null);
                            cbArea.SelectedValue = empleado.IdArea;

                            if (empleado.Huella != null)
                            {
                                _huellaCapturada = new DPFP.Template(new MemoryStream(empleado.Huella));
                                Logger.Agregar("🧬 Huella digital cargada desde base de datos");
                            }

                            Logger.Agregar($"📄 Datos cargados para modificar empleado ID: {empleado.IdEmpleado}");
                        }
                    }
                }
            }
        }

        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            Logger.Agregar("📥 Botón Registrar presionado");

            if (string.IsNullOrWhiteSpace(txtNombres.Text) ||
                string.IsNullOrWhiteSpace(txtApellidos.Text) ||
                string.IsNullOrWhiteSpace(txtCedula.Text) ||
                cbEmpresa.SelectedItem == null ||
                cbSede.SelectedItem == null ||
                cbArea.SelectedItem == null ||
                cbTipoEmpleado.SelectedItem == null)
            {
                Logger.Agregar("⚠️ Validación fallida: campos obligatorios vacíos.");
                new MensajeWindow("⚠️ Todos los campos son obligatorios.").ShowDialog();
                return;
            }

            if (_huellaCapturada == null)
            {
                Logger.Agregar("🛑 Intento de registrar sin capturar huella");
                new MensajeWindow("🛑 Debes capturar la huella antes de registrar al empleado.").ShowDialog();
                return;
            }

            try
            {
                int cedula = int.Parse(txtCedula.Text.Trim());
                byte[] huellaBytes = HuellaHelper.ConvertirTemplateABytes(_huellaCapturada);

                using (var context = AppSettings.GetContextUno())
                {
                    if (esModificacion && idEmpleadoActual > 0)
                    {
                        var empleado = context.Empleados.FirstOrDefault(e => e.IdEmpleado == idEmpleadoActual);
                        if (empleado != null)
                        {
                            empleado.Nombres = txtNombres.Text.Trim();
                            empleado.Apellidos = txtApellidos.Text.Trim();
                            empleado.Documento = cedula;
                            empleado.IdEmpresa = (int)cbEmpresa.SelectedValue;
                            empleado.IdSede = (int)cbSede.SelectedValue;
                            empleado.IdArea = (int)cbArea.SelectedValue;
                            empleado.IdTipoEmpleado = (int)cbTipoEmpleado.SelectedValue;
                            empleado.Huella = huellaBytes;
                            empleado.FechaIngreso = DateTime.Now;

                            context.SaveChanges();

                            Logger.Agregar($"📝 Empleado actualizado correctamente: {empleado.Nombres} {empleado.Apellidos} ({empleado.Documento})");
                            new MensajeWindow("✅ Empleado actualizado correctamente.").ShowDialog();

                            esModificacion = false;
                            idEmpleadoActual = 0;
                            LimpiarFormulario();
                        }
                        else
                        {
                            Logger.Agregar("❌ Error: No se encontró el empleado para modificar.");
                            new MensajeWindow("❌ No se encontró el empleado para modificar.").ShowDialog();
                        }
                    }
                    else
                    {
                        var nuevoEmpleado = new BiomentricoHolding.Data.DataBaseRegistro_Test.Empleado
                        {
                            Documento = cedula,
                            Nombres = txtNombres.Text.Trim(),
                            Apellidos = txtApellidos.Text.Trim(),
                            IdEmpresa = (int)cbEmpresa.SelectedValue,
                            IdSede = (int)cbSede.SelectedValue,
                            IdArea = (int)cbArea.SelectedValue,
                            IdTipoEmpleado = (int)cbTipoEmpleado.SelectedValue,
                            Huella = huellaBytes,
                            Estado = true,
                            FechaIngreso = DateTime.Now
                        };

                        context.Empleados.Add(nuevoEmpleado);
                        context.SaveChanges();

                        Logger.Agregar($"🆕 Empleado registrado: {nuevoEmpleado.Nombres} {nuevoEmpleado.Apellidos} ({nuevoEmpleado.Documento})");

                        int idEmpleado = nuevoEmpleado.IdEmpleado;

                        for (int dia = 2; dia <= 6; dia++)
                        {
                            context.EmpleadosHorarios.Add(new EmpleadosHorario
                            {
                                EmpleadoId = idEmpleado,
                                DiaSemana = dia,
                                Inicio = TimeOnly.Parse("07:00:00"),
                                Fin = (dia == 5 || dia == 6) ? TimeOnly.Parse("16:30:00") : TimeOnly.Parse("17:30:00"),
                                Estado = true
                            });
                        }

                        context.SaveChanges();
                        Logger.Agregar("⏱ Horarios asignados por defecto al nuevo empleado");

                        var confirmacion = new MensajeWindow("🎉 Empleado registrado correctamente.\n\n¿Deseas agregar otro empleado?", true);
                        bool? resultado = confirmacion.ShowDialog();

                        if (resultado == true)
                        {
                            LimpiarFormulario();
                        }
                        else
                        {
                            if (Application.Current.MainWindow.FindName("MainContent") is ContentControl contenedor)
                            {
                                contenedor.Content = null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Agregar("❌ Error inesperado al guardar empleado: " + ex.Message);
                new MensajeWindow($"❌ Ocurrió un error inesperado:\n\n{ex.Message}").ShowDialog();
            }
        }

        private void BtnCapturarHuella_Click(object sender, RoutedEventArgs e)
        {
            Logger.Agregar("📸 Intentando iniciar captura de huella");

            try
            {
                // ⚠️ Asegurarse de que no haya otra sesión de captura activa
                var detener = new CapturaHuellaService(); // instancia temporal
                detener.DetenerCaptura();
            }
            catch (Exception ex)
            {
                Logger.Agregar("⚠ No se pudo detener correctamente una captura previa: " + ex.Message);
            }

            var ventanaCaptura = new CapturarHuellaWindow();
            bool? resultado = ventanaCaptura.ShowDialog();

            if (resultado == true)
            {
                var template = ventanaCaptura.ResultadoTemplate;
                var imagenHuella = ventanaCaptura.UltimaHuellaCapturada;

                if (template != null && imagenHuella != null)
                {
                    _huellaCapturada = template;
                    imgHuella.Source = imagenHuella;
                    imgHuella.Visibility = Visibility.Visible;
                    imgHuellaBorder.Visibility = Visibility.Visible;

                    Logger.Agregar("✅ Huella capturada correctamente");
                    new MensajeWindow("✅ Huella capturada correctamente.").ShowDialog();
                }
            }
            else
            {
                Logger.Agregar("❌ Captura de huella cancelada por el usuario");
                new MensajeWindow("❌ La captura fue cancelada.").ShowDialog();
            }
        }


        private void LimpiarFormulario()
        {
            txtNombres.Text = "";
            txtApellidos.Text = "";
            txtCedula.Text = "";
            cbEmpresa.SelectedIndex = -1;
            cbSede.ItemsSource = null;
            cbArea.ItemsSource = null;
            cbTipoEmpleado.SelectedIndex = -1;
            _huellaCapturada = null;
            iconCedulaCheck.Visibility = Visibility.Collapsed;
            Logger.Agregar("🧹 Formulario de empleado limpiado");
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            Logger.Agregar("↩️ Usuario volvió a la pantalla principal desde RegistroEmpleado");
            Window mainWindow = Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.FindName("MainContent") is ContentControl contenedor)
            {
                contenedor.Content = null;
                if (mainWindow is MainWindow ventanaPrincipal)
                {
                    ventanaPrincipal.MostrarGifBienvenida();
                }
            }
        }

        private BitmapImage ConvertirBitmapAImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memory;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}
