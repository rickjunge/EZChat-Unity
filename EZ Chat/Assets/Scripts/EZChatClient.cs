using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EZChatClient : MonoBehaviour
{
    public string clientName;
    public int port = 6000;
    public string host = "localhost";
    public int minSecondsBetweenMessages = 2;
    private bool canSendMessage = true;
    private InputField EZChatInputField;
    private InputField EZChatOutputField;
    private GameObject EZChatClientPanel;
    [SerializeField]
    public KeyCode openChatKey;
    // Start is called before the first frame update
    void Start()
    {
        EZChatInputField = GameObject.Find("EZChatInputField").GetComponent<InputField>();
        EZChatOutputField = GameObject.Find("EZChatOutputField").GetComponent<InputField>();
        EZChatClientPanel = GameObject.Find("EZChatClientPanel");
        // Create a TcpClient
        client = new TcpClient
        {
            SendBufferSize = BufferSize,
            ReceiveBufferSize = BufferSize
        };

        // connect
        Connect();
    }

    public void Update()
    {
        ListenForMessages();
        CheckInputs();
    }

    void CheckInputs()
    {
        if (Input.GetKeyDown(openChatKey) && EventSystem.current.currentSelectedGameObject != EZChatInputField.gameObject)
        {
            EZChatClientPanel.SetActive(!EZChatClientPanel.activeSelf);
        }
    }

    private TcpClient client;
    public bool Running { get; private set; }

    // Buffer & messaging
    public readonly int BufferSize = 2 * 1024;  // 2KB
    private NetworkStream msgStream = null;

    public void Connect()
    {
        // Try to connect
        client.Connect(host, port);
        EndPoint endPoint = client.Client.RemoteEndPoint;

        // Make sure we're connected
        if (client.Connected)
        {
            // we are connected, send message to verify.
            msgStream = client.GetStream();
            byte[] msgBuffer = Encoding.UTF8.GetBytes(String.Format("name:{0}", clientName));
            msgStream.Write(msgBuffer, 0, msgBuffer.Length);   // Blocks
        }
        else
        {
            _cleanupNetworkResources();
            Debug.LogError("Could not connect to the server " + endPoint);
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
            // check for new messages
            int messageLength = client.Available;
            if (messageLength > 0)
            {
                // Read the whole message
                byte[] msgBuffer = new byte[messageLength];
                msgStream.Read(msgBuffer, 0, messageLength);   // Blocks


                // Decode it and print it
                string msg = Encoding.UTF8.GetString(msgBuffer);
                ShowMessage(msg);
            }
    }

    public void ShowMessage(string msg)
    {
        EZChatOutputField.text += Environment.NewLine + msg;
    }

    // Cleanup network resources
    private void _cleanupNetworkResources()
    {
        msgStream?.Close();
        msgStream = null;
        client.Close();
    }
}
