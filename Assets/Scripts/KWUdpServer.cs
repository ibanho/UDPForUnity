using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Photon.Realtime;

[Serializable]
public class ConnectedClient
{
	public string IP;
	public int Port;
	public int LastSeenTimeMS;
	public int LastSentTimeMS;

	public UdpClient Client;
	public IPEndPoint ClientEp;

	public void Send(byte[] _byte)
	{
		try
		{
			if (Client == null)
			{
				Client = new UdpClient(IP, Port);
				Client.Client.SendTimeout = 500;
			}
			Client.Send(_byte, _byte.Length);
			LastSentTimeMS = Environment.TickCount;
		}
		catch (Exception e)
		{
			//Debug.Log("clients: " + _byte.Length+ " "+ e);
			Close();
		}
	}

	public void Send(string msg)
	{
		try
		{
			if (Client == null)
			{
				Client = new UdpClient(IP, Port);
				Client.Client.SendTimeout = 500;
			}
			byte[] _byte = Encoding.UTF8.GetBytes(msg);
			Client.Send(_byte, _byte.Length);
			LastSentTimeMS = Environment.TickCount;
		}
		catch (Exception e)
		{
			//Debug.Log("clients: " + _byte.Length+ " "+ e);
			Close();
		}
	}

	public void Close()
	{
		if (Client != null) 
		{
			Client.Close(); 
		}
		
		Client = null;
	}
}
	
public class KWUdpServer : MonoBehaviour
{
	[HideInInspector]
	public int ServerListenPort = 3333;
	
	[HideInInspector]
	public int ClientListenPort = 3334;

	[HideInInspector]
	public int RoomListenPort = 3335;


	public static KWUdpServer instance;

	UdpClient Server;
	IPEndPoint ClientEp;

	public bool ShowLog = true;

	Queue<string> m_ReceiveData = new Queue<string>();

	private object _asyncLockReceived = new object();

	public RoomInfo m_RoomInfo = new RoomInfo();

	public bool IsStartServer = false;

	UdpClient _send;

	ConnectedClient _player2;

	public LanGameManager _manager;

	private void Awake()
	{
		if (instance == null) 
		{
			instance = this;
		}
	}		
	
    // Start is called before the first frame update
    void Start()
    {
		///=== 연결된 클라이언트 주기마다 ping
	}	

	public void StartServer(int GameType, string title)
    {
		Debug.Log("StartServer");
		Server = new UdpClient(ServerListenPort);
		Server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		ClientEp = new IPEndPoint(IPAddress.Any, ServerListenPort);

		Server.Client.ReceiveTimeout = 1000;
		Server.Client.SendTimeout = 200;

		Server.BeginReceive(new AsyncCallback(UdpReceiveCallback), null);

		m_RoomInfo.GameType = GameType;
		m_RoomInfo.IP = ClientEp.Address.ToString();
		m_RoomInfo.PORT = ServerListenPort;
		m_RoomInfo.Status = 0;
		m_RoomInfo.Ttitle = title;

		IsStartServer = true;

		SendRoomInfoBroadcast();
	}

	public void CloseServer()
    {
		if (Server != null)
		{
			try
			{
				Server.Close();
			}
			catch (Exception e)
			{
				Debug.Log(e.Message);
			}
			Server = null;
		}

		if (_player2 != null)
		{
			_player2.Close();
		}

		IsStartServer = false;
	}

	public void StartGame()
    {
		m_RoomInfo.Status = 1;
		SendRoomInfoBroadcast();
	}

	void UdpReceiveCallback(IAsyncResult ar)
	{
		Debug.Log("UdpReceiveCallback");
		if (ar.IsCompleted)
		{	//receive callback completed			
			byte[] ReceivedData = Server.EndReceive(ar, ref ClientEp);
			string ClientIP = ClientEp.Address.ToString();			
			string ReceivedMsg = ClientIP + ":" + Encoding.UTF8.GetString(ReceivedData);
			Debug.Log(ReceivedMsg);
			lock (_asyncLockReceived)
			{
				m_ReceiveData.Enqueue(ReceivedMsg);
			}
		}

		try
		{
			Server.BeginReceive(new AsyncCallback(UdpReceiveCallback), null);
		}
		catch (SocketException socketException)
		{
			//DebugLog("sth wrong with server receive async: " + socketException.ToString());
			if (Server != null)
			{
				Server.Close();
			}
			Server = null;
		}
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

	/// <summary>
	/// {0}:{1}:{2}
	/// client ip, packet type, packet data
	/// </summary>
	/// <param name="msg"></param>
	void PatchMessage(string msg)
	{
		List<string> info = msg.Split(':').ToList<string>();
		if (info != null && info.Count > 1)
		{
			switch (info[1])
			{
				case "REQ_ROOMINFO":
					SendRoomInfo(info[0]);
					break;

				case "REQ_JOINROOM":
					JoinRoom(info[0]);
					break;

				case "REQ_EXITROOM":
					break;

				case "MSG_HINT":
					break;

				case "MSG_CANCEL":
					break;

				case "MSG_CONFIRM":
					break;

				case "MSG_STONE":
					break;
			}
		}
	}

	void JoinRoom(string ip)
    {
		_player2 = new ConnectedClient();
		_player2.IP = ip;
		_player2.Port = ClientListenPort;

		_player2.Send("RES_JOIN_ROOM");

		StartGame();
	}

	void SendRoomInfo(string IP)
    {
		_send = new UdpClient(IP, RoomListenPort);
		StringBuilder sb = new StringBuilder();
		sb.Append("RES_ROOMINFO:");
		sb.AppendFormat("{0}|{1}|{2}|{3}|{4}", m_RoomInfo.GameType.ToString(), m_RoomInfo.IP, m_RoomInfo.PORT.ToString(),
			m_RoomInfo.Status.ToString(), m_RoomInfo.Ttitle);
		byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
		_send.BeginSend(data, data.Length, new AsyncCallback(SendIt), null);
	}

	void SendRoomInfoBroadcast()
    {		
		_send = new UdpClient();
		_send.Client.SendTimeout = 200;
		_send.EnableBroadcast = true;

		StringBuilder sb = new StringBuilder();
		sb.Append("RES_ROOMINFO:");
		sb.AppendFormat("{0}|{1}|{2}|{3}|{4}", m_RoomInfo.GameType.ToString(), m_RoomInfo.IP, m_RoomInfo.PORT.ToString(),
			m_RoomInfo.Status.ToString(), m_RoomInfo.Ttitle);
		byte[] data = Encoding.UTF8.GetBytes(sb.ToString());

		_send.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, RoomListenPort));
	}

	void SendIt(IAsyncResult result)
	{
		if (_send != null)
		{
			_send.EndSend(result);
		}
	}

	//public void MulticastChecker()
	//{
	//	UdpClient MulticastClient = new UdpClient();
	//	try
	//	{
	//		MulticastClient.Client.SendTimeout = 200;
	//		MulticastClient.EnableBroadcast = true;

	//		byte[] _byte = new byte[1];
	//		MulticastClient.Send(_byte, _byte.Length, new IPEndPoint(IPAddress.Broadcast, ClientListenPort));

	//		if (MulticastClient != null) 
	//		{
	//			MulticastClient.Close();
	//		}
	//	}
	//	catch (Exception e)
	//	{
	//		if(MulticastClient != null) 
	//		{
	//			MulticastClient.Close();
	//		}
	//	}
	//}

	public void DebugLog(string _value)
	{
		if (ShowLog)
		{
			Debug.Log(_value);
		}
	}
}
