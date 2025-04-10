using BiomentricoHolding.Views.Empleado;
using System.Windows;
using System.Windows.Input;
using WpfAnimatedGif;
using System.Windows.Media.Imaging;
using System.IO;
using BiomentricoHolding.Views;
using System.Windows.Controls;
using BiomentricoHolding.Views.Configuracion;

namespace BiomentricoHolding
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                this.Loaded += MainWindow_Loaded; // mover la lógica aquí
            }
            catch (Exception ex) {
                MessageBox.Show("error construtor"+ ex.ToString() );
                throw;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string gifPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.gif");

                if (File.Exists(gifPath) && imgBienvenida != null)
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(gifPath, UriKind.Absolute);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();

                    ImageBehavior.SetAnimatedSource(imgBienvenida, image);
                    ImageBehavior.SetRepeatBehavior(imgBienvenida, System.Windows.Media.Animation.RepeatBehavior.Forever);
                    ImageBehavior.SetAutoStart(imgBienvenida, true);

                    imgBienvenida.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("❌ No se encontró el GIF o imgBienvenida es null.");
                    if (imgBienvenida != null)
                        imgBienvenida.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ No se pudo cargar el GIF animado.\n\n" + ex.Message, "Error al cargar logo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRegistrarEmpleado_Click(object sender, RoutedEventArgs e)
        {
            var login = new MiniLoginWindow();
            bool? resultado = login.ShowDialog();

            if (resultado == true && login.AccesoPermitido)
            {
                MainContent.Content = new RegistrarEmpleado();
                imgBienvenida.Visibility = Visibility.Collapsed;
                MainContent.Visibility = Visibility.Visible;
            }
            else
            {
                var msg = new MensajeWindow("⚠️ Acceso denegado o cancelado.");
                msg.ShowDialog();
            }
        }

        private void BtnControlAcceso_Click(object sender, RoutedEventArgs e)
        {
            CapturaEntradaSalidaWindow ventanaCaptura = new CapturaEntradaSalidaWindow();
            ventanaCaptura.ShowDialog();
        }

        private void BtnConsultarRegistros_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Consultar registros");
        }


        private void BtnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainContent.Content = new ConfiguracionControl(); // Reutiliza tu control
                imgBienvenida.Visibility = Visibility.Collapsed;
                MainContent.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error al abrir configuración:\n" + ex.Message);
            }
        }



        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BarraTitulo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        public void MostrarGifBienvenida()
        {
            if (FindName("imgBienvenida") is Image img)
            {
                string rutaGif = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.gif");

                if (File.Exists(rutaGif))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(rutaGif, UriKind.Absolute);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();

                    ImageBehavior.SetAnimatedSource(img, image);
                    img.Visibility = Visibility.Visible;
                }
                    
                if (FindName("MainContent") is ContentControl contenedor)
                {
                    contenedor.Content = null;
                    contenedor.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
