// (c)2011 MuchDifferent. All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;
using uLink;

/// <summary>
/// This script is a simple example of a chat system for an authoritative uLink server.
/// </summary>
/// <remarks>
/// When the server is authoritative, the clients can't force the server to broadcast
/// messages. Therfore the client sends a private RPC to the server. The server receives 
/// this RPC and can check for bad language (and do other checks) before broadcasting 
/// this message to all connected clients.
/// </remarks>
[AddComponentMenu("uLink Utilities/Chat GUI")]
[RequireComponent(typeof(uLinkNetworkView))]
public class uLinkChatGUI : uLink.MonoBehaviour
{
	public enum Position
	{
		BottomLeft,
		BottomRight,
		TopLeft,
		TopRight,
	}

	public Position position = Position.BottomLeft;

	public KeyCode typeByKey = KeyCode.Return;
	public bool isTyping = false;
	
	public string whenNotTyping = "Press Enter to chat...";
	
	public int maxEntries = 50;
	
	public int windowWidth = 200;
	public int windowheight = 200;

    public bool useLoginData = false;

    public UnityEngine.MonoBehaviour[] enableWhenTyping;
    public UnityEngine.MonoBehaviour[] disableWhenTyping;

	public int guiDepth = 0;
	
	private string inputPrefix;
	
	private string inputField = "";
	private List<string> entries = new List<string>();
	private Vector2 scrollPosition;
	private bool oldLockCursor = false;

	void Start()
	{
		enabled = false;
		
		if (uLink.Network.status == uLink.NetworkStatus.Connected)
		{
			uLink_OnConnectedToServer();
		}
	}
	
	void uLink_OnServerInitialized()
	{
		uLink_OnConnectedToServer();
	}

	void uLink_OnConnectedToServer()
	{
		enabled = true;

        inputPrefix = (useLoginData && uLink.Network.loginData.Length != 0 ? uLink.Network.loginData[0] : uLink.Network.player) + ": ";
		
		// TODO: notify everybody when we join and leave the chat
	}
	
	void uLink_OnDisconnectedFromServer()
	{
		enabled = false;
	}

	void Update()
	{
		if (Input.GetKeyDown(typeByKey))
		{
			SwitchMode();
		}
	}
	
	void OnGUI()
	{
		if (!Input.GetKeyDown(typeByKey) && Event.current.type == EventType.KeyDown && Event.current.keyCode == typeByKey)
		{
			Event.current.Use();

			SwitchMode();
		}
		
		GUI.depth = guiDepth;

		int x = 10;
		int y = 10;

		if (position == Position.TopRight || position == Position.BottomRight) x = Screen.width - windowWidth - x;
		if (position == Position.BottomLeft || position == Position.BottomRight) y = Screen.height - windowheight - y;
		
		GUILayout.BeginArea(new Rect(x, y, windowWidth, windowheight));
		ChatGUI();
		GUILayout.EndArea();
	}
	
	void ChatGUI()
	{
		scrollPosition = GUILayout.BeginScrollView(scrollPosition, "Box");
	
		foreach (string entry in entries)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(entry);
			GUILayout.EndHorizontal();
			
		}
		
		GUILayout.EndScrollView();
		
		if (isTyping)
		{
			GUI.SetNextControlName("ChatInputField");
			inputField = GUILayout.TextField(inputField);
			GUI.FocusControl("ChatInputField");
		}
		else
		{
			GUI.enabled = false;
			GUILayout.TextField(whenNotTyping);
			GUI.enabled = true;
		}
	}
	
	[RPC]
	void Chat(string entry)
	{
		if (entries.Count == maxEntries)
		{
			entries.RemoveAt(0);
		}
		
		entries.Add(entry);
		
		scrollPosition.y = float.MaxValue;
		
		if (uLink.Network.isServer)
		{
			networkView.RPC("Chat", uLink.RPCMode.Others, entry);
		}
	}

    void SwitchMode()
    {
		if (!isTyping)
		{
			oldLockCursor = Screen.lockCursor;
		}
		else if (inputField.Length > 0)
		{
			networkView.RPC("Chat", uLink.NetworkPlayer.server, inputPrefix + inputField);
			inputField = "";
			Screen.lockCursor = oldLockCursor;
		}

		SetTyping(!isTyping);
    }

    void SetTyping(bool value)
    {
        isTyping = value;

        foreach (UnityEngine.MonoBehaviour component in enableWhenTyping)
        {
			component.enabled = value;
        }

        foreach (UnityEngine.MonoBehaviour component in disableWhenTyping)
        {
            component.enabled = !value;
        }
    }
}
