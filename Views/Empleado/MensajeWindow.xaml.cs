using System;
using System.Windows;

namespace BiomentricoHolding.Views
{
    public partial class MensajeWindow : Window
    {
        public bool Resultado { get; private set; } = false;

        public MensajeWindow(string mensaje, bool mostrarCancelar = false)
        {
            InitializeComponent();
            txtMensaje.Text = mensaje;
            btnCancelar.Visibility = mostrarCancelar ? Visibility.Visible : Visibility.Collapsed;
        }

        // ✅ Nuevo constructor con texto personalizado para botones
        public MensajeWindow(string mensaje, bool mostrarCancelar, string textoAceptar, string textoCancelar)
        : this(mensaje, mostrarCancelar)
        {
            btnOK.Content = textoAceptar;
            btnCancelar.Content = textoCancelar;
            btnCancelar.Visibility = mostrarCancelar ? Visibility.Visible : Visibility.Collapsed;
        }


        // Constructor con autocierre
        public MensajeWindow(string mensaje, int segundosAutocierre)
            : this(mensaje, false)
        {
            btnOK.Visibility = Visibility.Collapsed;
            btnCancelar.Visibility = Visibility.Collapsed;

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(segundosAutocierre)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                this.Close();
            };
            timer.Start();
        }

        // Constructor para mensaje de carga tipo modal (sin botones)
        public MensajeWindow(string mensaje, bool mostrarCancelar, bool esCarga)
            : this(mensaje, mostrarCancelar)
        {
            if (esCarga)
            {
                btnOK.Visibility = Visibility.Collapsed;
                btnCancelar.Visibility = Visibility.Collapsed;
                this.Cursor = System.Windows.Input.Cursors.Wait;
            }
        }

        // Constructor con tipo de mensaje (alerta, info, etc.)
        public MensajeWindow(string mensaje, int segundosAutocierre, string tipo)
            : this(mensaje, false)
        {
            btnOK.Visibility = Visibility.Collapsed;
            btnCancelar.Visibility = Visibility.Collapsed;

            // Cambiar ícono y color
            switch (tipo.ToLower())
            {
                case "advertencia":
                    icono.Text = "⚠";
                    icono.Foreground = System.Windows.Media.Brushes.DarkOrange;
                    break;
                case "error":
                    icono.Text = "❌";
                    icono.Foreground = System.Windows.Media.Brushes.Red;
                    break;
                default: // info
                    icono.Text = "🔔";
                    icono.Foreground = System.Windows.Media.Brushes.SteelBlue;
                    break;
            }

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(segundosAutocierre)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                this.Close();
            };
            timer.Start();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            Resultado = true;
            this.DialogResult = true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Resultado = false;
            this.DialogResult = false;
        }
        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Asegura que no se tome como confirmación
            this.Close();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!this.DialogResult.HasValue)
            {
                this.DialogResult = false;
            }
        }
    }
}
