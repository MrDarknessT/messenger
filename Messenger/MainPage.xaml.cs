using System.Net.Sockets;
using System.Text;

namespace Messenger
{
    public partial class MainPage : ContentPage
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public MainPage(TcpClient client, NetworkStream stream)
        {
            InitializeComponent();
            _client = client;
            _stream = stream;

            // Удаление кнопки навигации "назад"
            NavigationPage.SetHasBackButton(this, false);

            // Начало прослушивания сообщений от сервера
            ReceiveMessages();
        }

        private async void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    AddMessage(message);
                }
            }
            catch (Exception ex)
            {
                Cleanup();
                // Перемещение на страницу авторизации
                await Navigation.PopAsync();
                await DisplayAlert("Ошибка прослушивания сервера", ex.Message, "OK");
            }
        }

        private void OnSendClicked(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void OnMessageEntryCompleted(object sender, EventArgs e)
        {
            SendMessage();
        }

        private async void SendMessage()
        {
            if (_client == null || !_client.Connected) return;

            string message = MessageEntry.Text;
            if (!string.IsNullOrWhiteSpace(message))
            {
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes("/message" + message);
                    await _stream.WriteAsync(data, 0, data.Length);
                    MessageEntry.Text = string.Empty; // Очистка поля ввода
                }
                catch 
                {
                    await DisplayAlert("Ошибка", "Ошибка отправки сообщения.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Предупреждение", "Сообщение, которые вы хотите отправить, не должно быть пустым.", "OK");
            }
        }

        private async void OnDisconnectClicked(object sender, EventArgs e)
        {
            if (_client != null)
            {
                try
                {
                    Cleanup();
                    // Перемещение на страницу авторизации
                    await Navigation.PopAsync(); 
                }
                catch 
                {
                    await DisplayAlert("Ошибка", "Отключение прошло неправильно.", "OK");
                }
            }
        }

        private void AddMessage(string message)
        {
            var label = new Label { Text = message };
            MessagesStack.Children.Add(label);

            MessagesScrollView.ScrollToAsync(0, MessagesStack.Height, true); // Автоматичсекая прокрутка поля сообщений
        }

        private async void Cleanup()
        {
            try
            {
                if (_client != null && _client.Connected)
                {
                    // Отправка данных для выхода
                    string registrationData = "/exit";
                    byte[] data = Encoding.UTF8.GetBytes(registrationData);
                    await _stream.WriteAsync(data, 0, data.Length);
                }

                _stream?.Close();
                _client?.Close();
                _client = null;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка завершения сенаса", ex.Message, "OK");
            }
        }
    }
}