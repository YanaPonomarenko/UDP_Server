using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UdpGameClient
{
    class Program
    {
        static UdpClient client = new UdpClient();
        static IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
        static bool running = true;

        static void Main()
        {
            Console.Write("Enter your name: ");
            string name = Console.ReadLine();

            Thread receiveThread = new Thread(Receive);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Send($"JOIN:{name}");

            while (running)
            {
                string input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                if (input == "q" || input == "quit" || input == "exit")
                {
                    Send("EXIT");
                    break;
                }
                if (input == "list")
                {
                    Send("LIST");
                    continue;
                }

                if (int.TryParse(input, out _))
                    Send($"GUESS:{input}");
                else
                    Send(input);
            }
        }

        static void Receive()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            while (running)
            {
                try
                {
                    byte[] data = client.Receive(ref ep);
                    string msg = Encoding.UTF8.GetString(data);
                    HandleResponse(msg);
                }
                catch { break; }
            }
        }

        static void HandleResponse(string msg)
        {
            var parts = msg.Split(':', 2);
            string code = parts[0];
            string body = parts.Length > 1 ? parts[1] : "";

            switch (code)
            {
                case "WELCOME":
                    var w = body.Split('|');
                    Console.WriteLine($"\n Привіт {w[0]}! {w[1]}");
                    Console.WriteLine("Введи число від 1 до 100:");
                    break;

                case "HINT":
                    var h = body.Split('|');
                    Console.WriteLine($"Має бути {h[0]}  ({h[1]})");
                    break;

                case "WIN":
                    Console.WriteLine($"\n {body}");
                    Console.WriteLine("Нова гра! Введи число від 1 до 100:");
                    break;

                case "GAMEOVER":
                    Console.WriteLine($"\n {body}");
                    Console.WriteLine("Нова гра! Введи число від 1 до 100:");
                    break;
               
                case "LIST":
                    Console.WriteLine($"Гравці онлайн: {body}");
                    break;

                case "BYE":
                    Console.WriteLine($" {body}");
                    running = false;
                    break;

                case "ERROR":
                    Console.WriteLine($" {body}");
                    break;

                default:
                    Console.WriteLine($"[server]: {msg}");
                    break;
            }
        }

        static void Send(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, serverEP);
        }
    }
}