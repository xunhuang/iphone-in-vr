using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NativeWebSocket;
using System.Text.Json;

public class ExampleObject
{
    public string type;
    public string name;
}

public class Connection : MonoBehaviour
{
  WebSocket websocket;

  // Start is called before the first frame update
  async void Start()
  {
    websocket = new WebSocket("ws://localhost:8080");
    // websocket = new WebSocket("wss://simple-webrtc-signal-3khoexoznq-uc.a.run.app");

    websocket.OnOpen += () =>
    {
      Debug.Log("WebSocket Connection open!");

 ExampleObject obj = new ExampleObject { type = "watcher", name = "example" };
        string json = JsonUtility.ToJson(obj);
        Debug.Log(json);
      websocket.SendText( json);
    };

    websocket.OnError += (e) =>
    {
      Debug.Log("WebSocket Error! " + e);
    };

    websocket.OnClose += (e) =>
    {
      Debug.Log("WebSocket Connection closed!");
      Debug.Log(e);
    };

    websocket.OnMessage += (bytes) =>
    {
      // Reading a plain text message
      var message = System.Text.Encoding.UTF8.GetString(bytes);
      Debug.Log("Received OnMessage! (" + bytes.Length + " bytes) " + message);
    };

    await websocket.Connect();
  }

  void Update()
  {
    #if !UNITY_WEBGL || UNITY_EDITOR
      websocket.DispatchMessageQueue();
    #endif
  }

  async void SendWebSocketMessage()
  {
    if (websocket.State == WebSocketState.Open)
    {
      // Sending bytes
      await websocket.Send(new byte[] { 10, 20, 30 });

      // Sending plain text
      await websocket.SendText("plain text message");
    }
  }

  private async void OnApplicationQuit()
  {
    await websocket.Close();
  }
}
