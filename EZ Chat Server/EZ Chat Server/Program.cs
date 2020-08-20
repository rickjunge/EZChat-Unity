using System;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EZChatServer
{
    class TcpChatServer
    {
        public static TcpChatServer chat;
        private TcpListener listener;

        private List<TcpClient> clients = new List<TcpClient>();
        private Dictionary<TcpClient, string> names = new Dictionary<TcpClient, string>();

        private Queue<string> messageQueue = new Queue<string>();

        public readonly string ChatName;
        public readonly int Port;
        public readonly bool SendConnectMessage;
        public bool Running { get; private set; }
        public readonly int BufferSize = 2 * 1024;  // 2KB

        // Make a new TCP chat server, with our provided name
        public TcpChatServer(string chatName, int port, bool sendConnectMessage)
        {
            // Set the basic data
            ChatName = chatName;
            Port = port;
            SendConnectMessage = sendConnectMessage;
            Running = false;

            // Initialize new listener. Change the first variable to your public IP Adress, when testing is done.
            listener = new TcpListener(IPAddress.Any, Port);
        }

        // If the server is running, this will shut down the server
        public void Shutdown()
        {
            Running = false;
            Console.WriteLine("Shutting down server");
        }

        public void Run()
        {
            Console.WriteLine("Starting the \"{0}\" Server on Port {1}.", ChatName, Port);
            Console.WriteLine("Press Ctrl-C to shut down the server.");

            listener.Start();
            Running = true;
            
            // Main Server Loop
            while (Running)
            {
                // check for new clients
                if (listener.Pending())
                    _handleNewConnection();

                // handle clients
                _checkForDisconnects();
                _checkForNewMessages();
                _sendMessages();

                // increasing this value results in longer responding time, but lower CPU usage
                Thread.Sleep(10);
            }

            foreach (TcpClient m in clients)
                _cleanupClient(m);
            listener.Stop();

            // Some info
            Console.WriteLine("Server is shut down.");
        }

        private void _handleNewConnection()
        {
            // There is (at least) one, see what they want
            bool good = false;
            TcpClient newClient = listener.AcceptTcpClient();      // Blocks
            NetworkStream netStream = newClient.GetStream();

            // Modify the default buffer sizes
            newClient.SendBufferSize = BufferSize;
            newClient.ReceiveBufferSize = BufferSize;

            // Print some info
            EndPoint endPoint = newClient.Client.RemoteEndPoint;
            Console.WriteLine("Handling a new client from {0}...", endPoint);

            // Let them identify themselves
            byte[] msgBuffer = new byte[BufferSize];
            int bytesRead = netStream.Read(msgBuffer, 0, msgBuffer.Length);     // Blocks
            //Console.WriteLine("Got {0} bytes.", bytesRead);
            if (bytesRead > 0)
            {
                string msg = Encoding.UTF8.GetString(msgBuffer, 0, bytesRead);
                if (msg.StartsWith("name:"))
                {
                    // get client name
                    string name = msg.Substring(msg.IndexOf(':') + 1);

                    if ((name != string.Empty) && (!names.ContainsValue(name)))
                    {
                        //name is good, add to list
                        good = true;
                        names.Add(newClient, name);
                        clients.Add(newClient);

                        Console.WriteLine("{0} joined with the name {1}.", endPoint, name);

                        // tell chat about new client
                        if (SendConnectMessage)
                            messageQueue.Enqueue(String.Format("{0} has joined the chat.", name));
                    }
                }
                else
                {
                    // weird message, scriptkid?
                    Console.WriteLine("Wasn't able to identify {0} as a Viewer or Messenger.", endPoint);
                    _cleanupClient(newClient);
                }
            }

            if (!good)
                newClient.Close();
        }

        private void _checkForDisconnects()
        {
            foreach (TcpClient c in clients.ToArray())
            {
                if (_isDisconnected(c))
                {
                    string name = names[c];

                    Console.WriteLine("Client {0} has left.", name);
                    messageQueue.Enqueue(String.Format("{0} has left the chat", name));

                    // cleanup
                    clients.Remove(c);
                    names.Remove(c);
                    _cleanupClient(c);
                }
            }
        }

        private void _checkForNewMessages()
        {
            foreach (TcpClient m in clients)
            {
                int messageLength = m.Available;
                if (messageLength > 0)
                {
                    // messageLength is not 0, so we got a message
                    byte[] msgBuffer = new byte[messageLength];
                    m.GetStream().Read(msgBuffer, 0, msgBuffer.Length);

                    // attach name and enqueue
                    string msg = String.Format("{0}: {1}", names[m], Encoding.UTF8.GetString(msgBuffer));
                    messageQueue.Enqueue(msg);
                }
            }
        }

        private void _sendMessages()
        {
            foreach (string msg in messageQueue)
            {
                // Encode the message
                byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);

                // Send the message to each client
                foreach (TcpClient c in clients)
                {
                    if (c.Connected)
                        c.GetStream().Write(msgBuffer, 0, msgBuffer.Length);
                }
            }
            // clear out the queue
            messageQueue.Clear();
        }

        private static bool _isDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
#pragma warning disable CS0168 // Variable ist deklariert, wird jedoch niemals verwendet
            catch (Exception e)
#pragma warning restore CS0168 // Variable ist deklariert, wird jedoch niemals verwendet
            {
                // We got a socket or disposedObject error, assume it's disconnected
                // whatever it is, cant reach client
                return true;
            }
        }

        private static void _cleanupClient(TcpClient client)
        {
            // close client and network stream
            if (!client.Connected)
                return;
            client.GetStream().Close();
            client.Close();
        }

        protected static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            chat.Shutdown();
            args.Cancel = true;
        }

        public static void Main(string[] args)
        {
            //load config
            ConfigHandler.LoadConfig(   out string name, 
                                        out int port, 
                                        out bool sendConnectMessage);

            //create new chat server
            chat = new TcpChatServer(name, port, sendConnectMessage);

            // close server shortcut
            Console.CancelKeyPress += InterruptHandler;

            // run the server
            chat.Run();
        }
    }
}