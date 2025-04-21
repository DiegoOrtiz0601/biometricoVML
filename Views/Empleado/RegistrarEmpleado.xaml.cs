using BiomentricoHolding.Data.DataBaseRegistro_Test;
using BiomentricoHolding.Services;
using BiomentricoHolding.Utils;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using EmpleadoModel = BiomentricoHolding.Data.DataBaseRegistro_Test.Empleado;

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
                new MensajeWindow("⚠️ Todos los campos son obligatorios.").ShowDialog();
                return;
            }

            if (_huellaCapturada == null)
            {
                new MensajeWindow("🛑 Debes capturar la huella antes de continuar.").ShowDialog();
                return;
            }

            try
            {
                int cedula = int.Parse(txtCedula.Text.Trim());
                byte[] huellaBytes = HuellaHelper.ConvertirTemplateABytes(_huellaCapturada);

                using (var context = AppSettings.GetContextUno())
                {
                    EmpleadoModel empleado;
                    if (esModificacion && idEmpleadoActual > 0)
                    {
                        empleado = context.Empleados.FirstOrDefault(e => e.IdEmpleado == idEmpleadoActual);
                        if (empleado == null)
                        {
                            new MensajeWindow("❌ No se encontró el empleado para modificar.").ShowDialog();
                            return;
                        }

                        empleado.Nombres = txtNombres.Text.Trim();
                        empleado.Apellidos = txtApellidos.Text.Trim();
                        empleado.Documento = cedula;
                        empleado.IdEmpresa = (int)cbEmpresa.SelectedValue;
                        empleado.IdSede = (int)cbSede.SelectedValue;
                        empleado.IdArea = (int)cbArea.SelectedValue;
                        empleado.IdTipoEmpleado = (int)cbTipoEmpleado.SelectedValue;
                        empleado.Huella = huellaBytes;
                        empleado.FechaIngreso = DateTime.Now;
                    }
                    else
                    {
                        empleado = new EmpleadoModel
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

                        context.Empleados.Add(empleado);
                        context.SaveChanges(); // Necesario para obtener el ID
                        idEmpleadoActual = empleado.IdEmpleado;
                    }


                    context.SaveChanges();

                    int idEmpleado = empleado.IdEmpleado;
                    int idTipoEmpleado = (int)cbTipoEmpleado.SelectedValue;
                    string nombreTipoEmpleado = context.TiposEmpleados
                        .Where(t => t.Id == idTipoEmpleado)
                        .Select(t => t.Nombre)
                        .FirstOrDefault() ?? "";

                    bool esRotativo = nombreTipoEmpleado.Trim().ToLower() == "enrolado rotativo";

                    var asignacionExistente = context.AsignacionHorarios
                        .FirstOrDefault(a => a.IdEmpleado == idEmpleado && a.Estado == true);

                    bool usarHorarioEspecifico = false;

                    if (asignacionExistente != null)
                    {
                        string mensaje = esRotativo
                            ? "Ya existe un horario activo. ¿Deseas reemplazarlo por un horario específico o mantener el actual?"
                            : "Ya existe un horario activo. ¿Deseas reemplazarlo por un horario genérico o mantener el actual?";

                        var decision = new MensajeWindow(mensaje, true, "Reemplazar", "Mantener");
                        bool? resultado = decision.ShowDialog();

                        if (resultado == true && decision.Resultado)
                        {
                            asignacionExistente.Estado = false;
                            usarHorarioEspecifico = esRotativo;
                        }
                        else
                        {
                            Logger.Agregar("⏸ Se decidió mantener el horario actual. El empleado fue actualizado sin modificar la asignación.");
                            FinalizarGuardado(esModificacion); // Guarda los cambios del empleado
                            return;
                        }
                    }

                
                    else
                    {
                        string mensaje = esRotativo
                            ? "¿Deseas crear un horario específico para este colaborador?"
                            : "¿Deseas crear un horario genérico para este colaborador?";

                        var decision = new MensajeWindow(mensaje, true);
                        var resultado = decision.ShowDialog();

                        if (decision.Resultado)
                        {
                            usarHorarioEspecifico = esRotativo;
                        }
                        else
                        {
                            Logger.Agregar("🚫 El usuario canceló la creación de asignación de horario.");
                            return;
                        }
                    }

                    if (usarHorarioEspecifico)
                    {
                        var ventana = new AsignarHorarioWindow();
                        bool? respuesta = ventana.ShowDialog();

                        if (respuesta == true)
                        {
                            var asignacion = new AsignacionHorario
                            {
                                IdEmpleado = idEmpleado,
                                FechaInicio = DateOnly.FromDateTime(DateTime.Today),
                                FechaFin = DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, 12, 31)),
                                FechaCreacion = DateTime.Now,
                                CreadoPor = 3, // Reemplazar con ID real
                                Estado = true,
                                TipoHorario = 2
                            };

                            context.AsignacionHorarios.Add(asignacion);
                            context.SaveChanges();

                            foreach (var dia in ventana.HorariosAsignados)
                            {
                                var detalle = new DetalleHorario
                                {
                                    IdAsignacion = asignacion.Id,
                                    DiaSemana = dia.Key,
                                    HoraInicio = TimeOnly.FromTimeSpan(dia.Value.Inicio),
                                    HoraFin = TimeOnly.FromTimeSpan(dia.Value.Fin)
                                };
                                context.DetalleHorarios.Add(detalle);
                            }

                            context.SaveChanges();
                            Logger.Agregar("📅 Horario específico asignado.");
                        }
                        else
                        {
                            Logger.Agregar("⛔ Usuario canceló la asignación de horario específico.");
                            return;
                        }
                    }
                    else
                    {
                        var asignacion = new AsignacionHorario
                        {
                            IdEmpleado = idEmpleado,
                            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
                            FechaFin = DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, 12, 31)),
                            FechaCreacion = DateTime.Now,
                            CreadoPor = 3,
                            Estado = true,
                            TipoHorario = 1
                        };

                        context.AsignacionHorarios.Add(asignacion);
                        context.SaveChanges();

                        for (int dia = 1; dia <= 7; dia++)
                        {
                            context.DetalleHorarios.Add(new DetalleHorario
                            {
                                IdAsignacion = asignacion.Id,
                                DiaSemana = dia,
                                HoraInicio = TimeOnly.Parse("07:00:00"),
                                HoraFin = (dia == 5 || dia == 6) ? TimeOnly.Parse("16:30:00") : TimeOnly.Parse("17:30:00")
                            });
                        }

                        context.SaveChanges();
                        Logger.Agregar("🕒 Horario genérico asignado.");
                    }

                    string mensajeFinal = esModificacion ? "Empleado actualizado correctamente." : "Empleado registrado correctamente.";
                    new MensajeWindow($"✅ {mensajeFinal}").ShowDialog();

                    if (!esModificacion)
                    {
                        var confirmacion = new MensajeWindow("¿Deseas agregar otro empleado?", true);
                        if (confirmacion.ShowDialog() == true)
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

                    esModificacion = false;
                    idEmpleadoActual = 0;
                    LimpiarFormulario();
                }
            }
            catch (Exception ex)
            {
                Logger.Agregar($"❌ Error inesperado: {ex.Message}");
                new MensajeWindow($"❌ Ocurrió un error:\n{ex.Message}").ShowDialog();
            }
        }

        private void FinalizarGuardado(bool fueModificacion)
        {
            string mensajeFinal = fueModificacion ? "Empleado actualizado correctamente." : "Empleado registrado correctamente.";
            new MensajeWindow($"✅ {mensajeFinal}").ShowDialog();

            if (!fueModificacion)
            {
                var confirmacion = new MensajeWindow("¿Deseas agregar otro empleado?", true);
                if (confirmacion.ShowDialog() == true)
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

            esModificacion = false;
            idEmpleadoActual = 0;
            LimpiarFormulario();
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
