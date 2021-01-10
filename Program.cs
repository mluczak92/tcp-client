using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace tcp_client {
    class Program {
        static readonly string host = "127.0.0.1";
        static readonly short port = 2121;
        static TcpClient client = new TcpClient();
        static NetworkStream stream;

        static readonly byte[] receivedBuffor = new byte[64]; //not lower than 5! first 5 bytes shows whole message size
        static readonly StringBuilder messageBuilder = new StringBuilder();
        static int currentPartSize;
        static short sizeFromMessageHeader;
        static int alreadyReadBytes;

        static void Main(string[] args) {
            while (true) {
                InitConnect();
                string input = Console.ReadLine();
                try {
                    Connect();
                    SendMessage(input);
                } catch (SocketException) { //server off
                    Print("Can't reach server. Please try again in a moment.");
                    continue;
                } catch (IOException) { //server force closed while connection was active
                    stream.Close();
                    client.Close();
                    client = new TcpClient();
                    Print("Disconnected from server. Please try again in a moment.");
                    continue;
                }
            }
        }

        static void InitConnect() {
            while (true) {
                try {
                    if (Connect())
                        break;
                } catch (SocketException) { //server off
                    Print("Can't reach server.");
                    continue;
                }
            }
        }

        static bool Connect() {
            if (!client.Connected) {
                Print($"Connecting {host} using port {port}...");
                client.Connect(host, port);
                stream = client.GetStream();
                Task.Run(() => {
                    ReceiveServerMessages();
                });
                Print("Connected!");
            }
            return true;
        }

        static void ReceiveServerMessages() {
            try {
                while ((currentPartSize = stream.Read(receivedBuffor, 0, receivedBuffor.Length)) != 0) {
                    string decoded = Encoding.ASCII.GetString(receivedBuffor, 0, currentPartSize);
                    messageBuilder.Append(decoded);

                    if (sizeFromMessageHeader == 0) { //first part
                        sizeFromMessageHeader = short.Parse(decoded.Substring(0, 5));
                    }

                    alreadyReadBytes += currentPartSize;
                    if (alreadyReadBytes == sizeFromMessageHeader) { //last part
                        Console.WriteLine(messageBuilder.ToString()[6..]);
                        messageBuilder.Clear();
                        sizeFromMessageHeader = 0;
                        alreadyReadBytes = 0;
                    }
                }
            } catch (IOException) { //server force closed while connection was active
                Print("Disconnected. Please try again in a moment.");
                stream.Close();
                client.Close();
                client = new TcpClient();
                InitConnect();
            }
        }

        static void SendMessage(string input) {
            input = AddHeader(input);
            Send(input);
        }

        static string AddHeader(string input) {
            return $"{(input.Length + 6).ToString().PadLeft(5, '0')}.{input}";
        }

        static void Send(string input) {
            byte[] encoded = Encoding.ASCII.GetBytes(input);
            stream.Write(encoded, 0, encoded.Length);
        }

        static void Print(string msg) {
            Console.WriteLine($"{DateTime.Now:HH:mm fff}\t{msg}");
        }
    }
}
