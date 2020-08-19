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

        // types of clients connected
        private List<TcpClient> viewers = new List<TcpClient>();
        private List<TcpClient> messengers = new List<TcpClient>();

        // Names that are taken by other messengers
        private Dictionary<TcpClient, string> names = new Dictionary<TcpClient, string>();

        // Messages that need to be sent
        private Queue<string> messageQueue = new Queue<string>();

        // Extra fun data
        public readonly string ChatName;
        public readonly int Port;
        public bool Running { get; private set; }

        // Buffer
        public readonly int BufferSize = 2 * 1024;  // 2KB

        // Make a new TCP chat server, with our provided name
        public TcpChatServer(string chatName, int port)
        {
            // Set the basic data
            ChatName = chatName;
            Port = port;
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
            Console.WriteLine("Starting the \"{0}\" EZ Chat Server on Port {1}.", ChatName, Port);
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

            // Stop the server and clean up any connected clients
            foreach (TcpClient v in viewers)
                _cleanupClient(v);
            foreach (TcpClient m in messengers)
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
                    // Okay, so they might be a messenger
                    string name = msg.Substring(msg.IndexOf(':') + 1);

                    if ((name != string.Empty) && (!names.ContainsValue(name)))
                    {
                        // They're new here, add them in
                        good = true;
                        names.Add(newClient, name);
                        viewers.Add(newClient);
                        messengers.Add(newClient);

                        Console.WriteLine("{0} is a Messenger with the name {1}.", endPoint, name);

                        // Tell the viewers we have a new messenger
                        messageQueue.Enqueue(String.Format("{0} has joined the chat.", name));
                    }
                }
                else
                {
                    // Wasn't either a viewer or messenger, clean up anyways.
                    Console.WriteLine("Wasn't able to identify {0} as a Viewer or Messenger.", endPoint);
                    _cleanupClient(newClient);
                }
            }

            // Do we really want them?
            if (!good)
                newClient.Close();
        }

        // Sees if any of the clients have left the chat server
        private void _checkForDisconnects()
        {
            // Check the viewers first
            foreach (TcpClient v in viewers.ToArray())
            {
                if (_isDisconnected(v))
                {
                    //Console.WriteLine("Viewer {0} has left.", v.Client.RemoteEndPoint);

                    // cleanup on our end
                    viewers.Remove(v);     // Remove from list
                    _cleanupClient(v);
                }
            }

            // Check the messengers second
            foreach (TcpClient m in messengers.ToArray())
            {
                if (_isDisconnected(m))
                {
                    // Get info about the messenger
                    string name = names[m];

                    // Tell the viewers someone has left
                    Console.WriteLine("Messeger {0} has left.", name);
                    messageQueue.Enqueue(String.Format("{0} has left the chat", name));

                    // clean up on our end 
                    messengers.Remove(m);  // Remove from list
                    names.Remove(m);       // Remove taken name
                    _cleanupClient(m);
                }
            }
        }

        // See if any of our messengers have sent us a new message, put it in the queue
        private void _checkForNewMessages()
        {
            foreach (TcpClient m in messengers)
            {
                int messageLength = m.Available;
                if (messageLength > 0)
                {
                    // there is one!  get it
                    byte[] msgBuffer = new byte[messageLength];
                    m.GetStream().Read(msgBuffer, 0, msgBuffer.Length);     // Blocks

                    // Attach a name to it and shove it into the queue
                    string msg = String.Format("{0}: {1}", names[m], Encoding.UTF8.GetString(msgBuffer));
                    messageQueue.Enqueue(msg);
                }
            }
        }

        // Clears out the message queue (and sends it to all of the viewers
        private void _sendMessages()
        {
            foreach (string msg in messageQueue)
            {
                // Encode the message
                byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);

                // Send the message to each viewer
                foreach (TcpClient v in viewers)
                {
                    if (v.Connected)
                        v.GetStream().Write(msgBuffer, 0, msgBuffer.Length);    // Blocks
                }
            }

            // clear out the queue
            messageQueue.Clear();
        }

        // Checks if a socket has disconnected
        // Adapted from -- http://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
        private static bool _isDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
            catch (Exception e)
            {
                // We got a socket error, assume it's disconnected
                return true;
            }
        }

        // cleans up resources for a TcpClient
        private static void _cleanupClient(TcpClient client)
        {
            if (!client.Connected)
                return;
            client.GetStream().Close();     // Close network stream
            client.Close();                 // Close client
        }

        protected static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            chat.Shutdown();
            args.Cancel = true;
        }

        public static void Main(string[] args)
        {
            // Create the server
            string name = "EZChat";//args[0].Trim();
            int port = 6000;
            chat = new TcpChatServer(name, port);

            // Close Server if user presses Ctrl+C
            Console.CancelKeyPress += InterruptHandler;

            // run the chat server
            chat.Run();
        }
    }
}