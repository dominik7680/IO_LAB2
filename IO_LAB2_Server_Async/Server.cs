using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IO_LAB2_Server_Async
{
    class Server
    {
        private static List<ConnectedClient> _clients = new List<ConnectedClient>();
        private static ManualResetEvent _connected = new ManualResetEvent(false);
        private static Socket _server = null;
        public static int Port { get { return 40000; } }
        public static IPAddress LocalIPAddress { get { return IPAddress.Loopback; } }
        public static IPEndPoint EndPoint { get { return new IPEndPoint(LocalIPAddress, Port); } }

        public static void StartListening()
        {
            try
            {
                Console.WriteLine("Starting server");
                _server = CreateListener();
                Console.WriteLine($"Server Started, Waiting for a connection ...");

                while (true)
                {
                    _connected.Reset();
                    _server.BeginAccept(new AsyncCallback(AcceptCallback), _server);
                    _connected.WaitOne();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static Socket CreateListener()
        {
            Socket socket = null;
            try
            {
                socket = CreateSocket();
                socket.Bind(EndPoint);
                socket.Listen(10);
            }
            catch (Exception)
            {
                throw;
            }

            return socket;
        }

        public static Socket CreateSocket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            PrintConnectionState("Connection received");

            _connected.Set();

            Socket socket = _server.EndAccept(ar);

            var client = new ConnectedClient();
            client.Socket = socket;

            _clients.Add(client);

            try
            {
                SendReply(client, "Podaj swoj nickname:");
                client.Socket.BeginReceive(client.Buffer, 0, client.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
            }
            catch (SocketException)
            {
                CloseClient(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            string err;
            ConnectedClient client;
            int bytesRead;

            if (!CheckState(ar, out err, out client))
            {
                Console.WriteLine(err);
                return;
            }

            try
            {
                bytesRead = client.Socket.EndReceive(ar);
            }
            catch (SocketException)
            {
                CloseClient(client);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            if (bytesRead > 0)
            {
                client.BuildIncomingMessage(bytesRead);
                if (client.MessageReceived())
                {
                    string incomingMessage = client.IncomingMessage.ToString().Trim();

                    if (String.IsNullOrEmpty(client.Nickname))
                    {
                        client.Nickname = incomingMessage;
                        SendToAllRegisteredClients($"Witamy na czacie {client.Nickname}");
                    }
                    else
                    {
                        SendToAllRegisteredClientsExcept(client, $">> {client.Nickname}: {incomingMessage}");
                    }
                    client.ClearIncomingMessage();

                }
            }

            try
            {
                client.Socket.BeginReceive(client.Buffer, 0, client.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
            }
            catch (SocketException)
            {
                CloseClient(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void SendReply(ConnectedClient client, string message)
        {
            if (client == null)
            {
                Console.WriteLine("Unable to send reply: client null");
                return;
            }

            Console.Write("Sending Reply: ");

            client.CreateOutgoingMessage(message);
            var byteReply = client.OutgoingMessageToBytes();

            try
            {
                client.Socket.BeginSend(byteReply, 0, byteReply.Length, SocketFlags.None, new AsyncCallback(SendReplyCallback), client);
            }
            catch (SocketException)
            {
                CloseClient(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void SendToAllRegisteredClients(string message)
        {
            foreach (var client in _clients.FindAll(x => !String.IsNullOrEmpty(x.Nickname)))
            {
                client.CreateOutgoingMessage(message);
                var byteReply = client.OutgoingMessageToBytes();
                client.Socket.BeginSend(byteReply, 0, byteReply.Length, SocketFlags.None, new AsyncCallback(SendReplyCallback), client);
            }
        }

        public static void SendToAllRegisteredClientsExcept(ConnectedClient clientToExclude, string message)
        {
            foreach (var client in _clients.FindAll(x => !String.IsNullOrEmpty(x.Nickname)))
            {
                if(client == clientToExclude)
                    continue;
                client.CreateOutgoingMessage(message);
                var byteReply = client.OutgoingMessageToBytes();
                client.Socket.BeginSend(byteReply, 0, byteReply.Length, SocketFlags.None, new AsyncCallback(SendReplyCallback), client);
            }
        }

        private static void SendReplyCallback(IAsyncResult ar)
        {
            Console.WriteLine("Reply Sent");
        }

        private static bool CheckState(IAsyncResult ar, out string err, out ConnectedClient client)
        {
            client = null;
            err = "";

            if (ar == null)
            {
                err = "Async result null";
                return false;
            }

            client = (ConnectedClient)ar.AsyncState;
            if (client == null)
            {
                err = "Client null";
                return false;
            }

            return true;
        }

        private static void CloseClient(ConnectedClient client)
        {
            PrintConnectionState("Client disconnected");
            client.Close();
            if (_clients.Contains(client))
            {
                _clients.Remove(client);
            }
        }

        private static void CloseAllSockets()
        {
            // Close all clients
            foreach (ConnectedClient connection in _clients)
            {
                connection.Close();
            }
            // Close server
            _server.Close();
        }

        public static void PrintConnectionState(string msg)
        {
            string divider = new String('*', 60);
            Console.WriteLine();
            Console.WriteLine(divider);
            Console.WriteLine(msg);
            Console.WriteLine(divider);
        }
    }
}
