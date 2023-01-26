using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Text.Json;
using NativeWebSocket;

namespace Unity.WebRTC.Samples
{
[System.Serializable]
    public class OfferFromSignal
    {
        public string type ;
        public SessionDescription sessionDescription ;
    }
[System.Serializable]
    public class SessionDescription
    {
        public string type ;
        public string sdp ;
    }


    [System.Serializable]
    public class Candidate
    {
        public string sdp;
        // public string candidate ;
        public string sdpMid ;
        public int sdpMLineIndex ;
    }

    [System.Serializable]
    public class CandidateFromSignal
    {
        public string type ;
        public Candidate candidate ;
    }

        internal static class WebRTCSettings
    {
        public const int DefaultStreamWidth = 1280;
        public const int DefaultStreamHeight = 720;

        private static Vector2Int s_StreamSize = new Vector2Int(DefaultStreamWidth, DefaultStreamHeight);
        private static RTCRtpCodecCapability s_useVideoCodec = null;

        public static Vector2Int StreamSize
        {
            get { return s_StreamSize; }
            set { s_StreamSize = value; }
        }

        public static RTCRtpCodecCapability UseVideoCodec
        {
            get { return s_useVideoCodec; }
            set { s_useVideoCodec = value; }
        }
    }

    class VideoReceiveSample : MonoBehaviour
    {

#pragma warning disable 0649
        [SerializeField] private Button callButton;
        [SerializeField] private Button hangUpButton;
        [SerializeField] private Button addTracksButton;
        [SerializeField] private Button removeTracksButton;
        [SerializeField] private Toggle useWebCamToggle;
        [SerializeField] private Dropdown webCamListDropdown;
        [SerializeField] private Toggle useMicToggle;
        [SerializeField] private Dropdown micListDropdown;
        [SerializeField] private Camera cam;
        [SerializeField] private AudioClip clip;
        [SerializeField] private RawImage sourceImage;
        [SerializeField] private AudioSource sourceAudio;
        [SerializeField] private RawImage receiveImage;
        [SerializeField] private AudioSource receiveAudio;
        [SerializeField] private Transform rotateObject;
#pragma warning restore 0649

        private RTCPeerConnection _pc1, _pc2;
        private VideoStreamTrack videoStreamTrack;
        private AudioStreamTrack audioStreamTrack;
        private MediaStream receiveAudioStream, receiveVideoStream;
        private DelegateOnIceConnectionChange pc1OnIceConnectionChange;
        private DelegateOnIceConnectionChange pc2OnIceConnectionChange;
        private DelegateOnIceCandidate pc1OnIceCandidate;
        private DelegateOnIceCandidate pc2OnIceCandidate;
        private DelegateOnTrack pc2Ontrack;
        private WebCamTexture webCamTexture;


        private void Awake()
        {
        }

        private void OnDestroy()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                webCamTexture = null;
            }
        }

private WebSocket websocket;
private async void SetupWebSocket() {
    // websocket = new WebSocket("ws://localhost:8080");
    websocket = new WebSocket("wss://simple-webrtc-signal-3khoexoznq-uc.a.run.app");

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
      if (message.StartsWith("{\"type\":\"offer\"")) {
        Debug.Log("offer received");
            StartCoroutine( handleOffer(message));
      } else if (message.StartsWith("{\"type\":\"candidate\"")) {
        Debug.Log("Received OnMessage! (" + bytes.Length + " bytes) " + message);
        CandidateFromSignal candidate = JsonUtility.FromJson<CandidateFromSignal>(message);
        if ( candidate == null ) {
            Debug.Log("candidate is null");
        } else {
            // if ( candidate.candidate.candidate == null ) {
            if ( candidate.candidate.sdp == null ) {
                Debug.Log("candidate is null");
                return ;
            } else {
                Debug.Log("candidate is good ..............................");
            }

            Debug.Log(candidate.candidate.sdp);

            var iceCandidateInit = new RTCIceCandidateInit();
            iceCandidateInit.candidate = candidate.candidate.sdp;
            iceCandidateInit.sdpMid = candidate.candidate.sdpMid;
            iceCandidateInit.sdpMLineIndex = candidate.candidate.sdpMLineIndex;

             _pc2.AddIceCandidate(new RTCIceCandidate(iceCandidateInit));
        }
      } else if (message.StartsWith("{\"type\":\"broadcaster\"")) {
        Debug.Log("candidate received");
        websocket.SendText("{\"type\":\"watcher\"}");
      } else {
      Debug.Log("Received OnMessage! (" + bytes.Length + " bytes) " + message);
      }
    };

    await websocket.Connect();
}

private IEnumerator handleOffer(string message) {

        Debug.Log("handle offer ... ");

        OfferFromSignal offer = JsonUtility.FromJson<OfferFromSignal>(message);

        var configuration = GetSelectedSdpSemantics();
        _pc2 = new RTCPeerConnection(ref configuration);
            _pc2.OnIceCandidate = pc2OnIceCandidate;
            _pc2.OnIceConnectionChange = pc2OnIceConnectionChange;
            _pc2.OnTrack = pc2Ontrack;

        var remoteDescription = new RTCSessionDescription();
        remoteDescription.type = RTCSdpType.Offer;
        remoteDescription.sdp = offer.sessionDescription.sdp;
        
            Debug.Log("trying to set remote description");
        var op0 = _pc2.SetRemoteDescription(ref remoteDescription);
        yield return op0;

            Debug.Log("set remote description done ");

        var op = _pc2.CreateAnswer();
            Debug.Log("answer created 1");
        yield return op;
        if (!op.IsError) {

            Debug.Log("answer created 2");
            yield return OnCreateAnswerSuccess(_pc2, op.Desc);
        }
}

        private void Start()
        {
            Debug.Log("hello start ");
            SetupWebSocket();


            pc1OnIceConnectionChange = state => { OnIceConnectionChange(_pc1, state); };
            pc2OnIceConnectionChange = state => { OnIceConnectionChange(_pc2, state); };
            pc1OnIceCandidate = candidate => { OnIceCandidate(_pc1, candidate); };
            pc2OnIceCandidate = candidate => { OnIceCandidate(_pc2, candidate); };
            pc2Ontrack = e =>
            {
                if (e.Track is VideoStreamTrack video)
                {
                    video.OnVideoReceived += tex =>
                    {
                        receiveImage.texture = tex;
                    };
                }

                if (e.Track is AudioStreamTrack audioTrack)
                {
                    receiveAudio.SetTrack(audioTrack);
                    receiveAudio.loop = true;
                    receiveAudio.Play();
                }
            };
            StartCoroutine(WebRTC.Update());
        }

        private void Update()
        {
    #if !UNITY_WEBGL || UNITY_EDITOR
      websocket.DispatchMessageQueue();
    #endif
            if (rotateObject != null)
            {
                rotateObject.Rotate(1, 2, 3);
            }
        }

        private static RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration config = default;
            config.iceServers = new[] {new RTCIceServer {urls = new[] {"stun:stun.l.google.com:19302"}}};

            return config;
        }

        private void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state)
        {
            switch (state)
            {
                case RTCIceConnectionState.New:
                    Debug.Log($"{GetName(pc)} IceConnectionState: New");
                    break;
                case RTCIceConnectionState.Checking:
                    Debug.Log($"{GetName(pc)} IceConnectionState: Checking");
                    break;
                case RTCIceConnectionState.Closed:
                    Debug.Log($"{GetName(pc)} IceConnectionState: Closed");
                    break;
                case RTCIceConnectionState.Completed:
                    Debug.Log($"{GetName(pc)} IceConnectionState: Completed");
                    break;
                case RTCIceConnectionState.Connected:
                    Debug.Log($"{GetName(pc)} IceConnectionState: Connected");
                    break;
                case RTCIceConnectionState.Disconnected:
                    Debug.Log($"{GetName(pc)} IceConnectionState: Disconnected");
                    break;
                case RTCIceConnectionState.Failed:
                    Debug.Log($"{GetName(pc)} IceConnectionState: Failed");
                    break;
                case RTCIceConnectionState.Max:
                    Debug.Log($"{GetName(pc)} IceConnectionState: Max");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void OnIceCandidate(RTCPeerConnection pc, RTCIceCandidate candidate)
        {
            Debug.Log($"{GetName(pc)} ICE candidate:\n {candidate.Candidate}");
        }

        private string GetName(RTCPeerConnection pc)
        {
            return (pc == _pc1) ? "pc1" : "pc2";
        }

        private void OnSetLocalSuccess(RTCPeerConnection pc)
        {
            Debug.Log($"{GetName(pc)} SetLocalDescription complete");
        }

        static void OnSetSessionDescriptionError(ref RTCError error)
        {
            Debug.LogError($"Error Detail Type: {error.message}");
        }

        private void OnSetRemoteSuccess(RTCPeerConnection pc)
        {
            Debug.Log($"{GetName(pc)} SetRemoteDescription complete");
        }

        IEnumerator OnCreateAnswerSuccess(RTCPeerConnection pc, RTCSessionDescription desc)
        {
            Debug.Log($"Answer from {GetName(pc)}:\n{desc.sdp}");
            Debug.Log($"{GetName(pc)} setLocalDescription start");
            var op = pc.SetLocalDescription(ref desc);
            yield return op;

            if (!op.IsError)
            {
                OnSetLocalSuccess(pc);

                   OfferFromSignal answer = new OfferFromSignal();
                    answer.type = "answer";
            answer.sessionDescription = new SessionDescription();
            answer.sessionDescription.type = "answer";
            answer.sessionDescription.sdp = desc.sdp;
            string json = JsonUtility.ToJson(answer);
            Debug.Log(json);
            websocket.SendText(json);
            }
            else
            {
                var error = op.Error;
                OnSetSessionDescriptionError(ref error);
            }
        }

        private static void OnCreateSessionDescriptionError(RTCError error)
        {
            Debug.LogError($"Error Detail Type: {error.message}");
        }
    }
}
