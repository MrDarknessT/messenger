using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Messenger
{
    public partial class MainPage : ContentPage
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private const string ServerIp = "127.0.0.1"; 
        private const int ServerPort = 5000; 

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text;
            string password = PasswordEntry.Text;

            try
            {
                _client = new TcpClient(ServerIp, ServerPort);
                _stream = _client.GetStream();

                // Отправка данных для авторизации
                string loginData = $"{username},{password}";
                byte[] data = Encoding.UTF8.GetBytes(loginData);
                await _stream.WriteAsync(data, 0, data.Length);

                // Начало прослушивания сообщений от сервера
                ReceiveMessages();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            while (_client != null && _client.Connected)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    MessagesLabel.Text += message + Environment.NewLine;
                }
            }
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            if (_client == null || !_client.Connected) return;

            string message = MessageEntry.Text;
            if (!string.IsNullOrWhiteSpace(message))
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(data, 0, data.Length);
                MessageEntry.Text = string.Empty; // Очистить поле ввода
            }
        }

        private async void OnDisconnectClicked(object sender, EventArgs e)
        {
            if (_client != null)
            {
                byte[] data = Encoding.UTF8.GetBytes("/exit");
                await _stream.WriteAsync(data, 0, data.Length);
                _client.Close();
                _client = null;
                MessagesLabel.Text += "Вы отключены." + Environment.NewLine;
            }
        }
    }
}
