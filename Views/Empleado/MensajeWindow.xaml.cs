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
    }
}
