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

        public MensajeWindow(string mensaje, int segundosAutocierre)
    : this(mensaje, false) // llama al constructor base
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
