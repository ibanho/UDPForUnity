using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomUI : MonoBehaviour
{
    public RoomInfo _info;

    public delegate void OnButtonClickedEvent(RoomInfo info);
    public event OnButtonClickedEvent onGameRoomClicked;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnButtonClicked()
    {
        if (onGameRoomClicked != null)
        {
            onGameRoomClicked(_info);
        }
    }
}
