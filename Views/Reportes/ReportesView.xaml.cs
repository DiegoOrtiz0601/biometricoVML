using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BiomentricoHolding.Data.DataBaseRegistro_Test;

namespace BiomentricoHolding.Views.Reportes
{
    public partial class ReportesView : UserControl
    {
        private readonly DataBaseRegistro_TestDbContext _context = new();

        public ReportesView()
        {
            InitializeComponent();
            Loaded += ReportesView_Loaded;
        }

        private void ReportesView_Loaded(object sender, RoutedEventArgs e)
        {
            if (cbTipoReporte != null)
            {
                cbTipoReporte.SelectionChanged -= cbTipoReporte_SelectionChanged;
                cbTipoReporte.SelectionChanged += cbTipoReporte_SelectionChanged;
            }

            if (txtDocumento != null)
            {
                txtDocumento.TextChanged -= TxtDocumento_TextChanged;
                txtDocumento.TextChanged += TxtDocumento_TextChanged;
            }

            CargarEmpresas();
        }

        private void TxtDocumento_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtPlaceholder.Visibility = string.IsNullOrWhiteSpace(txtDocumento.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void cbTipoReporte_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbTipoReporte?.SelectedItem is ComboBoxItem item && item.Content is string tipo)
            {
                if (panelEmpresa != null && panelEmpleado != null)
                {
                    panelEmpresa.Visibility = tipo == "Reporte por Empresa" ? Visibility.Visible : Visibility.Collapsed;
                    panelEmpleado.Visibility = tipo == "Reporte por Empleado" ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void CargarEmpresas()
        {
            cbEmpresa.Items.Clear();
            cbEmpresa.Items.Add(new ComboBoxItem { Content = "Seleccione empresa", IsEnabled = false, IsSelected = true });

            foreach (var empresa in _context.Empresas)
            {
                cbEmpresa.Items.Add(new ComboBoxItem { Content = empresa.Nombre, Tag = empresa.IdEmpresa });
            }

            cbEmpresa.SelectionChanged += (s, e) =>
            {
                if (cbEmpresa.SelectedItem is ComboBoxItem item && item.Tag is int idEmpresa)
                {
                    CargarSedes(idEmpresa);
                }
            };
        }

        private void CargarSedes(int idEmpresa)
        {
            cbSede.Items.Clear();
            cbSede.Items.Add(new ComboBoxItem { Content = "Sede (opcional)", IsEnabled = false, IsSelected = true });

            foreach (var sede in _context.Sedes.Where(s => s.IdEmpresa == idEmpresa))
            {
                cbSede.Items.Add(new ComboBoxItem { Content = sede.Nombre, Tag = sede.IdSede });
            }

            cbSede.SelectionChanged += (s, e) =>
            {
                if (cbSede.SelectedItem is ComboBoxItem item && item.Tag is int idSede)
                {
                    CargarAreas(idSede);
                }
            };
        }

        private void CargarAreas(int idSede)
        {
            cbArea.Items.Clear();
            cbArea.Items.Add(new ComboBoxItem { Content = "Área (opcional)", IsEnabled = false, IsSelected = true });

            foreach (var area in _context.Areas.Where(a => a.IdSede == idSede))
            {
                cbArea.Items.Add(new ComboBoxItem { Content = area.Nombre, Tag = area.IdArea });
            }
        }

        private void GenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            if (cbTipoReporte?.SelectedItem is ComboBoxItem item && item.Content is string tipo)
            {
                if (tipo == "Reporte por Empresa")
                    GenerarReporteEmpresa();
                else if (tipo == "Reporte por Empleado")
                    GenerarReporteEmpleado();
            }
        }

        private void GenerarReporteEmpleado()
        {
            string doc = txtDocumento.Text?.Trim();
            if (!long.TryParse(doc, out long documento))
            {
                MessageBox.Show("Ingrese un número de documento válido.");
                return;
            }

            DateTime desde = dpDesde.SelectedDate ?? DateTime.MinValue;
            DateTime hasta = dpHasta.SelectedDate ?? DateTime.MaxValue;

            var empleado = _context.Empleados.FirstOrDefault(e => e.Documento == documento);
            if (empleado == null)
            {
                MessageBox.Show("Empleado no encontrado.");
                return;
            }

            var marcacionesDia = _context.Marcaciones
                .Where(m => m.IdEmpleado == empleado.IdEmpleado && m.FechaHora >= desde && m.FechaHora <= hasta)
                .GroupBy(m => m.FechaHora.Date)
                .ToList();

            var horarios = _context.EmpleadosHorarios.Where(h => h.EmpleadoId == empleado.IdEmpleado).ToList();
            var tipos = _context.TiposMarcacions.ToList();

            var resultado = new List<dynamic>();

            foreach (var grupo in marcacionesDia)
            {
                var fecha = grupo.Key;
                var entradas = grupo.Where(m => tipos.FirstOrDefault(t => t.Id == m.IdTipoMarcacion)?.Nombre.ToLower() == "entrada").OrderBy(m => m.FechaHora);
                var salidas = grupo.Where(m => tipos.FirstOrDefault(t => t.Id == m.IdTipoMarcacion)?.Nombre.ToLower() == "salida").OrderByDescending(m => m.FechaHora);

                var entrada = entradas.FirstOrDefault();
                var salida = salidas.FirstOrDefault();

                var diaSemana = (int)fecha.DayOfWeek + 1;
                var horario = horarios.FirstOrDefault(h => h.DiaSemana == diaSemana);

                var horaEsperadaEntrada = horario?.Inicio.ToTimeSpan();
                var horaEsperadaSalida = horario?.Fin.ToTimeSpan();

                bool retardoEntrada = entrada != null && horaEsperadaEntrada.HasValue && (entrada.FechaHora.TimeOfDay - horaEsperadaEntrada.Value) > TimeSpan.FromMinutes(15);
                bool retardoSalida = salida != null && horaEsperadaSalida.HasValue && (horaEsperadaSalida.Value - salida.FechaHora.TimeOfDay) > TimeSpan.FromMinutes(15);

                resultado.Add(new
                {
                    Documento = empleado.Documento.ToString(),
                    NombreCompleto = empleado.Nombres + " " + empleado.Apellidos,
                    Empresa = _context.Empresas.FirstOrDefault(x => x.IdEmpresa == empleado.IdEmpresa)?.Nombre,
                    Sede = _context.Sedes.FirstOrDefault(x => x.IdSede == empleado.IdSede)?.Nombre,
                    Area = _context.Areas.FirstOrDefault(x => x.IdArea == empleado.IdArea)?.Nombre,
                    DiaSemana = fecha.ToString("dddd"),
                    Fecha = fecha.ToString("dd/MM/yyyy"),
                    HoraEntradaReal = entrada?.FechaHora.ToString("HH:mm") ?? "-",
                    HoraSalidaReal = salida?.FechaHora.ToString("HH:mm") ?? "-",
                    HoraEsperada = $"{horaEsperadaEntrada?.ToString(@"hh\:mm") ?? "-"} / {horaEsperadaSalida?.ToString(@"hh\:mm") ?? "-"}",
                    Retardo = retardoEntrada || retardoSalida ? "Sí" : "A tiempo",
                    EstadoIcono = retardoEntrada || retardoSalida ? "🔴" : "🟢"
                });
            }

            dgReporte.ItemsSource = resultado;
        }

        private void GenerarReporteEmpresa()
        {
            MessageBox.Show("Funcionalidad optimizada aún no implementada para empresas.");
        }

        private void ExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Exportar a Excel aún no implementado.");
        }

        private void EnviarCorreo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Enviar correo aún no implementado.");
        }
    }
}
