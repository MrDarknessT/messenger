using System.Net;
using System.Net.Sockets;
using System.Text;
using MySql.Data.MySqlClient;

namespace Ser
{
    class Program
    {
        private static List<TcpClient> clients = new List<TcpClient>();
        private static MySqlConnection connection;

        static async Task Main(string[] args)
        {
            await StartServer();
            Console.WriteLine("Сервер завершил работу.");
        }

        private static async Task StartServer()
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
                var client = await server.AcceptTcpClientAsync();
                clients.Add(client);
                Console.WriteLine("Клиент подключен.");
                _ = HandleClient(client); // Запуск новой задачи в потоке
            }
        }

        private static async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = null;
            string username = null;

            try
            {
                stream = client.GetStream();
                byte[] buffer = new byte[1024];

                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Клиент отключился

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    if (message.StartsWith("/register"))
                    {
                        string msgToProc = message.Substring(9);
                        // Регистрация пользователя
                        string[] credentials = msgToProc.Split(',');

                        if (credentials.Length == 2 && RegisterUser(credentials[0], credentials[1]))
                        {
                            username = credentials[0];
                            Console.WriteLine($"{username} зарегистрирован.");
                            await BroadcastMessage($"{username} зарегистрировался в чате.");
                        }
                        else
                        {
                            Console.WriteLine("Пользователь существует или неверно введены данные.");
                            byte[] messageBuffer = Encoding.UTF8.GetBytes("/invalidReg");
                            await stream.WriteAsync(messageBuffer, 0, messageBuffer.Length);
                        }
                    }
                    else if (message.StartsWith("/login"))
                    {
                        // Авторизация пользователя
                        string msgToProc = message.Substring(6);
                        // Регистрация пользователя
                        string[] credentials = msgToProc.Split(',');

                        if (credentials.Length == 2 && AuthenticateUser(credentials[0], credentials[1]))
                        {
                            username = credentials[0];
                            Console.WriteLine($"{username} авторизован.");
                            await BroadcastMessage($"{username} вошел в чат.");
                        }
                        else
                        {
                            Console.WriteLine("Не удалось авторизовать пользователя.");
                            byte[] messageBuffer = Encoding.UTF8.GetBytes("/invalid");
                            await stream.WriteAsync(messageBuffer, 0, messageBuffer.Length);
                        }
                    }
                    else if (message.StartsWith("/message"))
                    {
                        string msgToProc = message.Substring(8);

                        Console.WriteLine($"{username}: {msgToProc}");
                        await BroadcastMessage($"{username}: {msgToProc}");
                    }
                    else if(message.StartsWith("/exit"))
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Не распознанная команда от клиента.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Общая обработка ошибок
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
            finally
            {
                if (username != null)
                {
                    await BroadcastMessage($"{username} вышел из чата.");
                    Console.WriteLine($"{username} отключен.");
                }

                clients.Remove(client); // Удаление клиента из списка

                // Закрытие потока и клиента
                try
                {
                    stream?.Close();
                    client.Close();
                }
                catch (Exception closeEx)
                {
                    Console.WriteLine($"Ошибка при закрытии соединения: {closeEx.Message}");
                }
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

        private static bool RegisterUser(string username, string password)
        {
            // Проверка, существует ли пользователь с таким именем
            string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
            using (var checkCmd = new MySqlCommand(checkQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@username", username);
                int userExists = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (userExists > 0)
                {
                    // Пользователь уже существует
                    return false;
                }
            }

            // Если пользователь не существует, добавляем нового
            string insertQuery = "INSERT INTO users (username, password) VALUES (@username, @password)";
            using (var cmd = new MySqlCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);
                return cmd.ExecuteNonQuery() > 0; // Возвращает true, если строка была добавлена
            }
        }

        private static async Task BroadcastMessage(string message)
        {
            byte[] msgBuffer = Encoding.UTF8.GetBytes(message);
            List<Task> tasks = new List<Task>();

            foreach (var client in clients)
            {
                NetworkStream stream = client.GetStream();
                tasks.Add(stream.WriteAsync(msgBuffer, 0, msgBuffer.Length));
            }
            await Task.WhenAll(tasks);
        }
    }
}