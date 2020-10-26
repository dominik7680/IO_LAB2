using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IO_LAB2_Server_Async
{
    class ConnectedClient
    {
        public Socket Socket { get; set; }
        public int BufferSize { get; set; } = 1024;
        public byte[] Buffer { get; set; }
        public StringBuilder IncomingMessage { get; set; }
        private StringBuilder OutgoingMessage { get; set; }
        public string MessageTerminator { get; set; } = "\r\n";
        public string Nickname { get; set; }

        public ConnectedClient()
        {
            Buffer = new byte[BufferSize];
            IncomingMessage = new StringBuilder();
            OutgoingMessage = new StringBuilder();
        }

        public byte[] OutgoingMessageToBytes()
        {
            if (OutgoingMessage.ToString().IndexOf(MessageTerminator) < 0)
            {
                OutgoingMessage.Append(MessageTerminator);
            }
            return Encoding.ASCII.GetBytes(OutgoingMessage.ToString());
        }


        public void CreateOutgoingMessage(string msg)
        {
            OutgoingMessage.Clear();
            OutgoingMessage.Append(msg);
            OutgoingMessage.Append(MessageTerminator);
        }

        public void BuildIncomingMessage(int bytesRead)
        {
            IncomingMessage.Append(Encoding.ASCII.GetString(Buffer, 0, bytesRead));
        }

        public bool MessageReceived()
        {
            return IncomingMessage.ToString().IndexOf(MessageTerminator) > -1;
        }

        public void ClearIncomingMessage()
        {
            IncomingMessage.Clear();
        }

        public int IncomingMessageLength()
        {
            return IncomingMessage.Length;
        }

        public void Close()
        {
            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
            }
            catch (Exception)
            {
            }
        }
    }
}
