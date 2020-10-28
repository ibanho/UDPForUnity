using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// protocol
// C: client. H: host server
// C ->  H  (Broadcast REQ_ROOMINFO)   C <- H  (RES_ROOMINFO)
// C <- H (MSG_CLOSEROOM)


[Serializable]
public class RoomInfo
{
	public int GameType;
	public string IP;
	public int PORT;
	// 0: 생성 1: 진행
	public int Status;
	public string Ttitle;
}

public class KWGameRoom : MonoBehaviour
{	
	List<RoomInfo> m_Rooms = new List<RoomInfo>();

	[HideInInspector]
	public int ServerListenPort = 3333;

	[HideInInspector]
	public int ClientListenPort = 3334;

	[HideInInspector]
	public int RoomListenPort = 3335;

	UdpClient Server;
	IPEndPoint ClientEp;

	private object obj = new object();

	private object _asyncLock = new object();

	Queue<string> m_ReceiveQueue = new Queue<string>();

	public GameObject _listPanel;

	List<GameObject> _listItems = new List<GameObject>();

	public GameObject _ItemPrefab;

	public LanGameManager _manager;

	// Start is called before the first frame update
	void Start()
    {
		ClientEp = new IPEndPoint(IPAddress.Any, RoomListenPort);
		Server = new UdpClient(RoomListenPort);
		Server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		Server.Client.EnableBroadcast = true;
		StartUdpReceive();
	}

    // Update is called once per frame
    void Update()
    {
		if (m_ReceiveQueue.Count > 0)
		{
			string msg;
			lock (_asyncLock)
			{
				msg = m_ReceiveQueue.Dequeue();
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
		if (info != null && info.Count == 3)
        {
			switch (info[1])
            {
				case "RES_ROOMINFO":
					SetRoomInfo(info[0], info[2]);
					break;

				case "MSG_CLOSEROOM":
					CloseRoom(info[2]);
					break;
            }
			
        }
	}

	void SetRoomInfo(string ip, string packetdata)
    {
		RoomInfo roomInfo = PacketDataToRoomInfo(packetdata);
		roomInfo.IP = ip;
		if (roomInfo != null)
		{
			if (roomInfo.Status == 0)
			{   // 대기 중인 방			
				if (m_Rooms.Find(r => r.Ttitle.Equals(roomInfo.Ttitle)) == null)
				{   // 중복된 방이 없음					
					m_Rooms.Add(roomInfo);
					ChangeRooms();
				}
			}
			else if (roomInfo.Status == 1)
            {   // 진행 중인 방
				int index = m_Rooms.FindIndex(r => r.Ttitle.Equals(roomInfo.Ttitle));
				if (index > -1)
				{
					m_Rooms.RemoveAt(index);
					ChangeRooms();
				}
			}
		}
	}

	void CloseRoom(string packetdata)
    {
		RoomInfo roomInfo = PacketDataToRoomInfo(packetdata);
		if (roomInfo != null)
		{
			int index = m_Rooms.FindIndex(r => r.Ttitle.Equals(roomInfo.Ttitle));
			if (index > -1)
			{
				m_Rooms.RemoveAt(index);
				ChangeRooms();
			}
		}
	}

	RoomInfo PacketDataToRoomInfo(string packetdata)
    {
		RoomInfo roomInfo = null; 
		List<string> room = packetdata.Split('|').ToList<string>();
		if (room != null && room.Count == 5)
		{
			roomInfo = new RoomInfo();
			roomInfo.GameType = Int32.Parse(room[0]);
			roomInfo.IP = room[1];
			roomInfo.PORT = Int32.Parse(room[2]);
			roomInfo.Status = Int32.Parse(room[3]);
			roomInfo.Ttitle = room[4];
		}
		
		return roomInfo;
	}

	/// <summary>
	/// 방 리스트가 변경됨
	/// </summary>
	void ChangeRooms()
    {
		if (_listPanel == null)
        {
			return;
        }

		foreach (GameObject item in _listItems)
        {
			DestroyImmediate(item);
        }
		_listItems.Clear();

		foreach (RoomInfo room in m_Rooms)
        {
			GameObject obj = GameObject.Instantiate(_ItemPrefab, _listPanel.transform);
			RoomUI roomUI = obj.GetComponent<RoomUI>();
			roomUI._info = room;
			roomUI.onGameRoomClicked += _manager.JoinGame;
			obj.GetComponentInChildren<Text>().text = room.Ttitle;
			_listItems.Add(obj);
		}
	}

	void StartUdpReceive()
	{
		Server.BeginReceive(new AsyncCallback(ReceiveData), obj);
	}

	void ReceiveData(IAsyncResult result)
	{
		Debug.Log("ReceiveData");
		if (result.IsCompleted)
		{
			byte[] DATA = Server.EndReceive(result, ref ClientEp);			
			string ReceivedMsg = ClientEp.Address.ToString() + ":" + Encoding.UTF8.GetString(DATA);
			Debug.Log(ReceivedMsg);
			lock (_asyncLock)
			{
				m_ReceiveQueue.Enqueue(ReceivedMsg);
			}
		}
		StartUdpReceive();
	}

	/// <summary>
	/// 서버 포트로 브로드캐스팅 
	/// </summary>
	public void RoomListChecker()
	{
		Debug.Log("RoomListChecker");
		UdpClient MulticastClient = new UdpClient();
		try
		{			
			MulticastClient.Client.SendTimeout = 200;
			MulticastClient.EnableBroadcast = true;

			byte[] _byte = Encoding.ASCII.GetBytes("REQ_ROOMINFO");
			MulticastClient.Send(_byte, _byte.Length, new IPEndPoint(IPAddress.Broadcast, ServerListenPort));

			if (MulticastClient != null) 
			{
				MulticastClient.Close();
			}
		}
		catch (Exception e)
		{
			if(MulticastClient != null) 
			{
				MulticastClient.Close();
			}
		}
	}
}
