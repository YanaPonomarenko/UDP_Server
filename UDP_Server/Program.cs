using System.Net;
using System.Net.Sockets;
using System.Text;

class ClientState
{
    public IPEndPoint EndPoint { get; set; }
    public string Name { get; set; }
    public int SecretNumber { get; set; }
    public int Attempts { get; set; }
}

class Program
{
    static UdpClient server = new UdpClient(5000);
    static Dictionary<string, ClientState> clients = new Dictionary<string, ClientState>();

    static void Main()
    {
        Console.WriteLine("Сервер запущено на порту 5000...");

        while (true)
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = server.Receive(ref remoteEP);
            string message = Encoding.UTF8.GetString(data).Trim();
            string key = remoteEP.ToString();

            Console.WriteLine($"[{key}] → {message}");

            HandleMessage(key, remoteEP, message);
        }
    }

    static void HandleMessage(string key, IPEndPoint ep, string message)
    {
        if (message.StartsWith("JOIN:"))
        {
            string name = message.Substring(5);

            if (clients.ContainsKey(key))
            {
                Send(ep, "INFO: Ви вже зареєстровані");
                return;
            }

            int secret = new Random().Next(1, 101);
            clients[key] = new ClientState
            {
                EndPoint = ep,
                SecretNumber = secret,
                Attempts = 0,
                Name = name
            };

            Console.WriteLine($"Зареєстровано: {name}, загадане число: {secret}");
            Send(ep, $"WELCOME:{name}|Я загадав число від 1 до 100. Вгадуй");
            return;
        }

        if (!clients.ContainsKey(key))
        {
            Send(ep, "ERROR:Спочатку відправ JOIN:<ім'я>");
            return;
        }

        var client = clients[key];

        if (message.StartsWith("GUESS:"))
        {
            if (!int.TryParse(message.Substring(6), out int guess))
            {
                Send(ep, "ERROR:Введи число!");
                return;
            }

            client.Attempts++;

            if (client.Attempts >= 10)
            {
                Send(ep, $"GAMEOVER:Не вгадав {client.SecretNumber} за 10 спроб! Нова гра");
                client.SecretNumber = new Random().Next(1, 101);
                client.Attempts = 0;
                Console.WriteLine($"  >> {client.Name} програв! Нове число: {client.SecretNumber}");
                return;
            }


            if (guess < client.SecretNumber)
                Send(ep, $"HINT:БІЛЬШЕ|Спроба {client.Attempts}");
            else if (guess > client.SecretNumber)
                Send(ep, $"HINT:МЕНШЕ|Спроба {client.Attempts}");
            else
            {
                Send(ep, $"WIN:Вгадав за {client.Attempts} спроб!");
                client.SecretNumber = new Random().Next(1, 101);
                client.Attempts = 0;
                Console.WriteLine($"  >> {client.Name} вгадав! Нове число: {client.SecretNumber}");
            }
            return;
        }

        if (message == "EXIT" || message == "QUIT")
        {
            Console.WriteLine($"  >> {client.Name} відключився.");
            clients.Remove(key);
            return;
        }
 
        if (message == "LIST")
        {
            string list = string.Join(", ", clients.Values.Select(c => c.Name));
            Send(ep, $"LIST:{list}");
            return;
        }

        Send(ep, "ERROR:Невідома команда");
    }

    static void Send(IPEndPoint ep, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        server.Send(data, data.Length, ep);
    }
}