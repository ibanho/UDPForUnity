using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UDPManager : MonoBehaviour
{
	UDPRT recvUDP;
	UDPRT sendBroadcast;
	
    // Start is called before the first frame update
    void Start()
    {
    	recvUDP = UDPRT.CreateInstance(5555);
    	sendBroadcast = UDPRT.CreateInstance(5555, "255.255.255.255", "RoomInfo");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
