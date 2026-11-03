using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        private System.Net.Sockets.TcpClient client;
        private NetworkStream stream;
        private string myName;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            myName = TxtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(myName))
            {
                MessageBox.Show("Enter your name!");
                return;
            }

            try
            {
                client = new System.Net.Sockets.TcpClient("127.0.0.1", 8888);
                stream = client.GetStream();

                byte[] nameData = Encoding.UTF8.GetBytes(myName);
                await stream.WriteAsync(nameData, 0, nameData.Length);

                TxtName.IsEnabled = false;
                TxtMessage.IsEnabled = true;
                BtnSend.IsEnabled = true;
                LstChat.Items.Add("Connected to server.");

                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message);
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            string message = TxtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                TxtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Send error: " + ex.Message);
            }
        }

        private async Task ReceiveLoop()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Dispatcher.Invoke(() => LstChat.Items.Add(message));
                }
            }
            catch { }
            finally
            {
                Dispatcher.Invoke(() => LstChat.Items.Add("Disconnected from server."));
            }
        }
    }
}