using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class KWUdpClient : MonoBehaviour
{
    [HideInInspector]
    public int ClientListenPort = 3334; 

    UdpClient _listen;
    UdpClient _send;
    object _object;

    bool IsConnectedServer = false;

    string _ServerIP;
    int _ServerPort;

    IPEndPoint ServerEp;

    Queue<string> m_ReceiveData = new Queue<string>();

    private object _asyncLockReceived = new object();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (m_ReceiveData.Count > 0)
        {
            string msg;
            lock (_asyncLockReceived)
            {
                msg = m_ReceiveData.Dequeue();
            }

            PatchMessage(msg);
        }
    }

    void PatchMessage(string msg)
    {
        List<string> info = msg.Split(':').ToList<string>();
        if (info != null && info.Count > 1)
        {
            switch (info[1])
            {
                case "RES_JOINROOM":
                    break;
            }
        }
    }

    private void ConnectServer(string ip, int port)
    {
        if (_send == null)
        {
            _ServerIP = ip;
            _ServerPort = port;
            _send = new UdpClient(ip, port);
        }

        if (_listen == null)
        {
            _listen = new UdpClient(ClientListenPort);
            _listen.BeginReceive(new AsyncCallback(ReceiveCallback), _object);
        }        
    }

    void ReceiveCallback(IAsyncResult ar)
    {
        if (ar.IsCompleted)
        {
            byte[] ReceivedData = _listen.EndReceive(ar, ref ServerEp);
            string ServerIP = ServerEp.Address.ToString();
            string ReceivedMsg = ServerIP + ":" + Encoding.UTF8.GetString(ReceivedData);
            Debug.Log(ReceivedMsg);
            lock (_asyncLockReceived)
            {
                m_ReceiveData.Enqueue(ReceivedMsg);
            }
        }

        _listen.BeginReceive(new AsyncCallback(ReceiveCallback), _object);
    }

    void Send(string msg)
    {
        if (_send == null)
        {
            _send = new UdpClient(_ServerIP, _ServerPort);
        }

        byte[] data = Encoding.UTF8.GetBytes(msg);
        _send.BeginSend(data, data.Length, new AsyncCallback(SendIt), null);
    }

    void SendIt(IAsyncResult result)
    {
        if (_send != null)
        {
            _send.EndSend(result);
        }
    }

    public void JoinRoom(RoomInfo room)
    {
        ConnectServer(room.IP, room.PORT);
        Send("REQ_JOINROOM");
    }

    void CloseAll()
    {
        if (_send != null)
        {
            _send.Close();
            _send = null;
        } 

        if (_listen != null)
        {
            _listen.Close();
            _listen = null;
        }
    }
}
