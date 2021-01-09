using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace tcp_client {
    class Program {
        static short port = 2121;
        static TcpClient client;
        static NetworkStream stream;

        static void Main(string[] args) {
            Print($"Hello world!");
            client = new TcpClient();
            while (true) {
                string input = ReadInput();
                try {
                    Connect();
                    PrepareAndSend(input);
                } catch (SocketException e) { //server off
                    Print(e.Message);
                    continue;
                } catch (IOException e) { //server force closed while connection was active
                    stream.Close();
                    client.Close();
                    client = new TcpClient();
                    Print(e.Message);
                    continue;
                }
            }
        }

        static string ReadInput() {
            Print($"Enter your message or \"close\" command: ");
            return Console.ReadLine();
        }

        static void Connect() {
            if (!client.Connected) {
                Print($"Connecting localhost using port {port}...");
                client.Connect(IPAddress.Loopback, port);
                stream = client.GetStream();
                Print("Connected!");
            }
        }

        static void PrepareAndSend(string input) {
            switch (input) {
                //case "close": SendCommand(input); break;
                default: SendMessage(input); break;
            }
        }

        //static void SendCommand(string input) {

        //}

        static void SendMessage(string input) {
            input = AddHeader(input);
            Send(input);
            HandleResponse();
        }

        static string AddHeader(string input) {
            return $"{(input.Length + 6).ToString().PadLeft(5, '0')}.{input}";
        }

        static void Send(string input) {
            byte[] encoded = Encoding.ASCII.GetBytes(input);
            stream.Write(encoded, 0, encoded.Length);
            Print($">>> Sent: {input}");
        }

        static void HandleResponse() {
            byte[] receivedBuffor = new byte[64]; //fixed 'safe' size for now
            stream.Read(receivedBuffor, 0, receivedBuffor.Length);
            string response = Encoding.ASCII.GetString(receivedBuffor, 0, receivedBuffor.Length);
            Print($"<<< Received: {response}");
        }

        static void Print(string msg) {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm fff")}\t{msg}");
        }
    }
}
