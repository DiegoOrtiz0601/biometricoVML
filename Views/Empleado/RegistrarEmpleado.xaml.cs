using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BiomentricoHolding.Services;
using BiomentricoHolding.Data.DataBaseRegistro_Test;
using BiomentricoHolding.Views.Empleado;
using BiomentricoHolding.Utils;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;

namespace BiomentricoHolding.Views.Empleado
{
    public partial class RegistrarEmpleado : UserControl
    {
        private DPFP.Template _huellaCapturada = null;

        public RegistrarEmpleado()
        {
            InitializeComponent();
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
                // Buscar y cargar datos del empleado en el formulario
                if (int.TryParse(cedulaTexto, out int cedula))
                {
                    using (var context = AppSettings.GetContextUno())
                    {
                        var empleado = context.Empleados.FirstOrDefault(e => e.Documento == cedula);
                        if (empleado != null)
                        {
                            txtNombres.Text = empleado.Nombres;
                            txtApellidos.Text = empleado.Apellidos;
                            txtCedula.Text = empleado.Documento.ToString();
                            cbEmpresa.SelectedValue = empleado.IdEmpresa;
                            cbTipoEmpleado.SelectedValue = empleado.IdTipoEmpleado;

                            // Cargar sedes y áreas correspondientes
                            cbEmpresa_SelectionChanged(null, null); // Refresca sedes
                            cbSede.SelectedValue = empleado.IdSede;

                            cbSede_SelectionChanged(null, null); // Refresca áreas
                            cbArea.SelectedValue = empleado.IdArea;

                            // Cargar huella (desde base de datos a _huellaCapturada)
                            if (empleado.Huella != null)
                            {
                                _huellaCapturada = new DPFP.Template(new System.IO.MemoryStream(empleado.Huella));
                                MessageBox.Show("Huella cargada del sistema ✅", "Información");
                            }

                            MessageBox.Show("Empleado cargado correctamente para modificación.");
                        }
                    }
                }
            }
        }

        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
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
                new MensajeWindow("🛑 Debes capturar la huella antes de registrar al empleado.").ShowDialog();
                return;
            }

            try
            {
                int cedula = int.Parse(txtCedula.Text.Trim());
                byte[] huellaBytes = HuellaHelper.ConvertirTemplateABytes(_huellaCapturada);

                using (var context = AppSettings.GetContextUno())
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
                }

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
            catch (Exception ex)
            {
                new MensajeWindow($"❌ Ocurrió un error inesperado:\n\n{ex.Message}").ShowDialog();
            }
        }

        private void BtnCapturarHuella_Click(object sender, RoutedEventArgs e)
        {
            var ventanaCaptura = new CapturarHuellaWindow();
            bool? resultado = ventanaCaptura.ShowDialog();

            if (resultado == true)
            {
                var template = ventanaCaptura.ResultadoTemplate;
                var imagenHuella = ventanaCaptura.UltimaHuellaCapturada; // ✅ La imagen como ImageSource

                if (template != null && imagenHuella != null)
                {
                    _huellaCapturada = template;

                    imgHuella.Source = imagenHuella;
                    imgHuella.Visibility = Visibility.Visible;
                    imgHuellaBorder.Visibility = Visibility.Visible; // ✅ Muestra el borde si lo agregaste

                    var msg = new MensajeWindow("✅ Huella capturada correctamente.");
                    msg.ShowDialog();
                }
            }
            else
            {
                var msg = new MensajeWindow("❌ La captura fue cancelada.");
                msg.ShowDialog();
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
        }
        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.FindName("MainContent") is ContentControl contenedor)
            {
                contenedor.Content = null;
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
