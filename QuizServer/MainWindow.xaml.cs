using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ServidorUDP.ViewModels;

namespace QuizServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServidorViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = (ServidorViewModel)DataContext;
            viewModel.SetTimerControl(TimerControl);
        }
    }
}