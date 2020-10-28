using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LanGameManager : MonoBehaviour
{
    public KWUdpServer _server;
    public KWGameRoom _room;
    public KWUdpClient _client;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MakeGame()
    {
        Debug.Log("MakeGame");
        if (_server != null)
        {
            _server.StartServer(1, "네모 1234");
        }
    }

    public void StartGame()
    {
        if (_server != null && _server.IsStartServer == true)
        {
            _server.StartGame();
        }
    }

    public void CloseGame()
    {
        if (_server != null && _server.IsStartServer == true)
        {
            _server.CloseServer();
        }        
    }

    public void RefreshRoom()
    {
        Debug.Log("RefreshRoom");
        if (_room != null)
        {
            _room.RoomListChecker();
        }
    }

    public void JoinGame(RoomInfo room)
    {
        Debug.Log("JoinGame");
        _client.JoinRoom(room);
    }
}
