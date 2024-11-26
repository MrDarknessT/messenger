using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;

namespace TcpChatServer
{
    class Program
    {
        private static List<TcpClient> clients = new List<TcpClient>();
        private static MySqlConnection connection;

        static void Main(string[] args)
        {
            StartServer();
        }

        private static void StartServer()
        {
            // Подключение к базе данных
            string connectionString = "server=localhost;database=data;user=root;password=1111;";
            connection = new MySqlConnection(connectionString);
            connection.Open();

            TcpListener server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Console.WriteLine("Сервер запущен...");

            while (true)
            {
                var client = server.AcceptTcpClient();
                clients.Add(client);
                Console.WriteLine("Клиент подключен.");
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        private static void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj; // Приведение типа
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            string username = null;

            try
            {
                // Авторизация пользователя
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string loginData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string[] credentials = loginData.Split(',');

                if (credentials.Length == 2 && AuthenticateUser(credentials[0], credentials[1]))
                {
                    username = credentials[0];
                    Console.WriteLine($"{username} авторизован.");
                    BroadcastMessage($"{username} вошел в чат.");

                    while (true)
                    {
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break; // Клиент отключился

                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"{username}: {message}");
                        BroadcastMessage($"{username}: {message}");

                        // Проверка на отключение
                        if (message.Equals("/exit", StringComparison.OrdinalIgnoreCase)) // пример команды /exit
                        {
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Не удалось авторизовать пользователя.");
                }
            }
            finally
            {
                if (username != null)
                {
                    BroadcastMessage($"{username} вышел из чата.");
                    Console.WriteLine($"{username} отключен.");
                }

                clients.Remove(client);
                client.Close();
            }
        }


        private static bool AuthenticateUser(string username, string password)
        {
            string query = "SELECT COUNT(*) FROM users WHERE username=@username AND password=@password";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void BroadcastMessage(string message)
        {
            byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
            foreach (var client in clients)
            {
                NetworkStream stream = client.GetStream();
                stream.Write(messageBuffer, 0, messageBuffer.Length);
            }
        }
    }
}
