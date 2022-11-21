using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SLLS_Recorder;

namespace SLLS_Screen {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        readonly ClockTimeProvider Time = new();
        Client? Client = null;


        private bool isCalledQuit = false;
        private bool isCleanuped = false;


        public MainWindow() {
            InitializeComponent();
        }

        private void Connect_Click(object sender, RoutedEventArgs e) {
            if (Client == null) {
                int port = -1;
                if (int.TryParse(PortNumber.Text, out port)) {
                    Connect.Content = "Disconnect";
                    Host.IsEnabled = false;
                    PortNumber.IsEnabled = false;
                    //Server Start
                    Client = new Client(
                        Host.Text,
                        port,
                        s => {
                            Dispatcher.Invoke(() => ListboxLog(s));
                        },
                        _ => {
                            Dispatcher.Invoke(() => {
                                Connect.Content = "Connect";
                                Host.IsEnabled = true;
                                PortNumber.IsEnabled = true;
                                Client = null;
                            });
                        },
                        Time
                    );
                } else {
                    ListboxLog("Unable Port Number");
                }
            } else {
                Client.Dispose();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (isCleanuped) return;
            e.Cancel = true;
            if (!isCalledQuit) Quit();
        }

        public void ListboxLog(string content) {
            Logger.Items.Insert(0, content);
        }

        private void Quit() {
            Task.Run(async () => {
                isCalledQuit = true;
                await (Client?.Dispose() ?? Task.CompletedTask);
                isCleanuped = true;
                Dispatcher.Invoke(() => Close());
            });
        }

    }
}
