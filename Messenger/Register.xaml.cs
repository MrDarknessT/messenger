using System.Net.Sockets;
using System.Text;

namespace Messenger
{
    public partial class Register : ContentPage
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private const string ServerIp = "127.0.0.1";
        private const int ServerPort = 5000;

        public Register()
        {
            InitializeComponent();
        }

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void OnRegisterEntryCompleted(object sender, EventArgs e)
        {
            SendMessage();
        }

        private async void SendMessage()
        {
            string username = UsernameEntry.Text;
            string password = PasswordEntry.Text;
            string password2 = PasswordEntry2.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("���� �����", "������� ��� ������������ � ������.", "OK");
                return;
            }
            else if (password != password2)
            {
                await DisplayAlert("���� �����", "������ �� ���������.", "OK");
                return;
            }

            try
            {
                _client = new TcpClient(ServerIp, ServerPort);
                _stream = _client.GetStream();

                // �������� ������ ��� �����������
                string registrationData = $"/register{username},{password}";
                byte[] data = Encoding.UTF8.GetBytes(registrationData);
                await _stream.WriteAsync(data, 0, data.Length);

                // ������ ������������� ��������� �� �������
                await ReceiveMessages();
            }
            catch (Exception ex)
            {
                Cleanup();
                await DisplayAlert("������ ����������� (����� � ��������)", ex.Message, "OK");
            }
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // �������� �� ���������� �����������
                    if (message.StartsWith("/invalidReg"))
                    {
                        await DisplayAlert("������ �����������", "�������� ������ ��� �����������.", "OK");
                    }
                    else
                    {
                        Cleanup();
                        // ����������� �� �������� �����������
                        await Navigation.PopAsync();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("������ �����������", ex.Message, "OK");
                Cleanup();
            }
        }

        private async void Cleanup()
        {
            try
            {
                if (_client != null && _client.Connected)
                {
                    // �������� ������ ��� ������
                    string registrationData = "/exit";
                    byte[] data = Encoding.UTF8.GetBytes(registrationData);
                    await _stream.WriteAsync(data, 0, data.Length);
                }

                _stream?.Close();
                _client?.Close();
                _client = null;
            }
            catch(Exception ex)
            {
                await DisplayAlert("������ ���������� ������", ex.Message, "OK");
            }
        }
    }
}