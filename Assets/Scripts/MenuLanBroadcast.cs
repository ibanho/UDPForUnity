using System;
using Bolt.Matchmaking;
using Bolt.Photon;
using UdpKit;
using UdpKit.Platform;
using UnityEngine;

public class MenuLanBroadcast : Bolt.GlobalEventListener
{
	private Rect _labelRoom = new Rect(0, 0, 140, 75);
	private GUIStyle _labelRoomStyle;

	private void Awake()
	{
		Application.targetFrameRate = 60;
		BoltLauncher.SetUdpPlatform(new PhotonPlatform());

		_labelRoomStyle = new GUIStyle()
		{
			fontSize = 20,
			fontStyle = FontStyle.Bold,
			normal =
			{
				textColor = Color.white
                }
        };
	}

	public override void BoltStartBegin()
	{
		BoltNetwork.RegisterTokenClass<PhotonRoomProperties>();
	}

	public override void BoltStartDone()
	{
		if (BoltNetwork.IsServer)
		{
			string matchName = Guid.NewGuid().ToString();

			var props = new PhotonRoomProperties();

			props.IsOpen = true;
			props.IsVisible = false; // Make the session invisible

			props["type"] = "game01";
			props["map"] = "Tutorial1";

			BoltMatchmaking.CreateSession(
				sessionID: matchName,
				sceneToLoad: "Tutorial1",
				token: props
			);
		}

		// Broadcast and Listen for LAN Sessions
		BoltNetwork.EnableLanBroadcast();
	}

	public override void SessionListUpdated(Map<Guid, UdpSession> sessionList)
	{
		BoltLog.Info("Session list updated: {0} total sessions", sessionList.Count);
	}

	// GUI

	public void OnGUI()
	{
		GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));

		if (BoltNetwork.IsRunning == false)
		{
			if (ExpandButton("Start Server"))
			{
				BoltLauncher.StartServer();
			}

			if (ExpandButton("Start Client"))
			{
				BoltLauncher.StartClient();
			}
		}
		else if (BoltNetwork.IsClient)
		{
			SelectRoom();
		}

		GUILayout.EndArea();
	}

	private void SelectRoom()
	{
		GUI.Label(_labelRoom, "Looking for rooms:", _labelRoomStyle);

		if (BoltNetwork.SessionList.Count > 0)
		{
			GUILayout.BeginVertical();
			GUILayout.Space(30);

			foreach (var session in BoltNetwork.SessionList)
			{
				UdpSession udpSession = session.Value;

				var label = string.Format("Join: {0} | {1}", udpSession.HostName, udpSession.Source);

				if (ExpandButton(label))
				{
					BoltMatchmaking.JoinSession(udpSession);
				}
			}

			GUILayout.EndVertical();
		}
	}

	private bool ExpandButton(string text)
	{
		return GUILayout.Button(text, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
	}
}