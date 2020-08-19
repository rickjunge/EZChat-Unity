using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EZChatClient : MonoBehaviour
{
    public string clientName;
    public int port = 6000;
    public string host = "localhost";
    public int minSecondsBetweenMessages = 2;
    private bool canSendMessage = true;
    private InputField EZChatInputField;
    // Start is called before the first frame update
    void Start()
    {
        EZChatInputField = GameObject.Find("EZChatInputField").GetComponent<InputField>();
        // Create a non-connected TcpClient
        client = new TcpClient
        {
            SendBufferSize = BufferSize,
            ReceiveBufferSize = BufferSize
        };          // Other constructors will start a connection

        // connect
        Connect();
    }

    public void Update()
    {
        ListenForMessages();
    }

    private TcpClient client;
    public bool Running { get; private set; }

    // Buffer & messaging
    public readonly int BufferSize = 2 * 1024;  // 2KB
    private NetworkStream msgStream = null;

    public void Connect()
    {
        // Try to connect
        client.Connect(host, port);       // Will resolve DNS for us; blocks
        EndPoint endPoint = client.Client.RemoteEndPoint;

        // Make sure we're connected
        if (client.Connected)
        {
            // Got in!
            Debug.Log("Connected to the server at " + endPoint);

            // Tell them that we're a messenger
            msgStream = client.GetStream();
            byte[] msgBuffer = Encoding.UTF8.GetBytes(String.Format("name:{0}", clientName));
            msgStream.Write(msgBuffer, 0, msgBuffer.Length);   // Blocks
        }
        else
        {
            _cleanupNetworkResources();
            Debug.LogError("Wasn't able to connect to the server " + endPoint);
        }
    }
    
    public void BtnSendMessage()
    {
        StartCoroutine(SendMsg(EZChatInputField.text));
        EZChatInputField.text = "";
    }

    public IEnumerator SendMsg(string msg)
    {
        if (!canSendMessage)
            yield break;

        canSendMessage = false;

        if (msg != string.Empty)
        {
            // Send the message
            byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);
            msgStream.Write(msgBuffer, 0, msgBuffer.Length);   // Blocks
        }
        yield return new WaitForSeconds(minSecondsBetweenMessages);
        canSendMessage = true;
    }

    public void ListenForMessages()
    {
            // Do we have a new message?
            int messageLength = client.Available;
            if (messageLength > 0)
            {
                // Read the whole message
                byte[] msgBuffer = new byte[messageLength];
                msgStream.Read(msgBuffer, 0, messageLength);   // Blocks


                // Decode it and print it
                string msg = Encoding.UTF8.GetString(msgBuffer);
                Debug.Log(msg);
            }
    }


    // Cleanup network ressources
    private void _cleanupNetworkResources()
    {
        msgStream?.Close();
        msgStream = null;
        client.Close();
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
        catch (SocketException e)
        {
            Debug.Log(e);
            return true;
        }
    }
}
