using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Media.Animation;


namespace BiomentricoHolding.Views.Empleado
{
    public partial class MiniLoginWindow : Window
    {
        public bool AccesoPermitido { get; private set; } = false;

        public MiniLoginWindow()
        {
            InitializeComponent();
                     
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string clave = txtPassword.Password;

            if (ValidarUsuarioDesdeBD(usuario, clave))
            {
                AccesoPermitido = true;
                this.DialogResult = true;
                this.Close(); // <- Esto cierra la ventana correctamente
            }
            else
            {
                MessageBox.Show("Usuario o contraseña incorrectos o inactivo");
            }
        }

        private bool ValidarUsuarioDesdeBD(string usuario, string clave)
        {
            try
            {
                using (var context = AppSettings.GetContextUno()) // Usa tu método de AppSettings
                {
                    return context.Usuarios.Any(u =>
                        u.NombreUsuario == usuario &&
                        u.Contrasena == clave &&
                        u.Estado == true
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al validar usuario: " + ex.Message);
                return false;
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close(); // <- También aquí, cerrar correctamente
        }
    }
}
