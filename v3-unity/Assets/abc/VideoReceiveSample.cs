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
        private List<RTCRtpSender> pc1Senders;
        private VideoStreamTrack videoStreamTrack;
        private AudioStreamTrack audioStreamTrack;
        private MediaStream receiveAudioStream, receiveVideoStream;
        private DelegateOnIceConnectionChange pc1OnIceConnectionChange;
        private DelegateOnIceConnectionChange pc2OnIceConnectionChange;
        private DelegateOnIceCandidate pc1OnIceCandidate;
        private DelegateOnIceCandidate pc2OnIceCandidate;
        private DelegateOnTrack pc2Ontrack;
        private DelegateOnNegotiationNeeded pc1OnNegotiationNeeded;
        private WebCamTexture webCamTexture;


        private void Awake()
        {
            Debug.Log("hello awake ");
            callButton.onClick.AddListener(Call);
            hangUpButton.onClick.AddListener(HangUp);
            addTracksButton.onClick.AddListener(AddTracks);
            removeTracksButton.onClick.AddListener(RemoveTracks);
            useWebCamToggle.onValueChanged.AddListener(SwitchUseWebCam);
            webCamListDropdown.options = WebCamTexture.devices.Select(x => new Dropdown.OptionData(x.name)).ToList();
            useMicToggle.onValueChanged.AddListener(SwitchUseMic);
            micListDropdown.options = Microphone.devices.Select(x => new Dropdown.OptionData(x)).ToList();
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

            // OfferFromSignal answer = new OfferFromSignal();
            // answer.type = "answer";
            // answer.sessionDescription = new SessionDescription();
            // answer.sessionDescription.type = "answer";
            // answer.sessionDescription.sdp = op.Desc.sdp;
            // string json = JsonUtility.ToJson(answer);
            // Debug.Log(json);
            // websocket.SendText(json);
        }
}

        private void Start()
        {
            Debug.Log("hello start ");
            SetupWebSocket();

            pc1Senders = new List<RTCRtpSender>();
            callButton.interactable = true;
            hangUpButton.interactable = false;

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
            pc1OnNegotiationNeeded = () => { StartCoroutine(PeerNegotiationNeeded(_pc1)); };
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

        IEnumerator PeerNegotiationNeeded(RTCPeerConnection pc)
        {
            Debug.Log($"{GetName(pc)} createOffer start");
            var op = pc.CreateOffer();
            yield return op;

            if (!op.IsError)
            {
                if (pc.SignalingState != RTCSignalingState.Stable)
                {
                    Debug.LogError($"{GetName(pc)} signaling state is not stable.");
                    yield break;
                }

                yield return StartCoroutine(OnCreateOfferSuccess(pc, op.Desc));
            }
            else
            {
                OnCreateSessionDescriptionError(op.Error);
            }
        }

        private void AddTracks()
        {
            var videoSender = _pc1.AddTrack(videoStreamTrack);
            pc1Senders.Add(videoSender);
            pc1Senders.Add(_pc1.AddTrack(audioStreamTrack));

            if (WebRTCSettings.UseVideoCodec != null)
            {
                var codecs = new[] {WebRTCSettings.UseVideoCodec};
                var transceiver = _pc1.GetTransceivers().First(t => t.Sender == videoSender);
                transceiver.SetCodecPreferences(codecs);
            }

            addTracksButton.interactable = false;
            removeTracksButton.interactable = true;
        }

        private void RemoveTracks()
        {
            var transceivers = _pc1.GetTransceivers();
            foreach (var transceiver in transceivers)
            {
                if(transceiver.Sender != null)
                {
                    transceiver.Stop();
                    _pc1.RemoveTrack(transceiver.Sender);
                }
            }

            pc1Senders.Clear();
            addTracksButton.interactable = true;
            removeTracksButton.interactable = false;
        }

        private void SwitchUseWebCam(bool isOn)
        {
            webCamListDropdown.interactable = isOn;
        }

        private void SwitchUseMic(bool isOn)
        {
            micListDropdown.interactable = isOn;
        }
        
        private void setupPeerConnectionAfterOfferCall() {
            // var configuration = GetSelectedSdpSemantics();
            // _pc2 = new RTCPeerConnection(ref configuration);
            // await _pc2.setRemoteDescription(_pc1.localDescription);

        }

        private void Call()
        {
            useWebCamToggle.interactable = false;
            webCamListDropdown.interactable = false;
            useMicToggle.interactable = false;
            micListDropdown.interactable = false;
            callButton.interactable = false;
            hangUpButton.interactable = true;
            addTracksButton.interactable = true;
            removeTracksButton.interactable = false;

            Debug.Log("GetSelectedSdpSemantics");
            var configuration = GetSelectedSdpSemantics();
            _pc1 = new RTCPeerConnection(ref configuration);
            Debug.Log("Created local peer connection object pc1");
            _pc1.OnIceCandidate = pc1OnIceCandidate;
            _pc1.OnIceConnectionChange = pc1OnIceConnectionChange;
            _pc1.OnNegotiationNeeded = pc1OnNegotiationNeeded;

            _pc2 = new RTCPeerConnection(ref configuration);
            Debug.Log("Created remote peer connection object pc2");
            _pc2.OnIceCandidate = pc2OnIceCandidate;
            _pc2.OnIceConnectionChange = pc2OnIceConnectionChange;
            _pc2.OnTrack = pc2Ontrack;

            CaptureAudioStart();
            StartCoroutine(CaptureVideoStart());
        }

        private void CaptureAudioStart()
        {
            if (!useMicToggle.isOn)
            {
                sourceAudio.clip = clip;
                sourceAudio.loop = true;
                sourceAudio.Play();
                audioStreamTrack = new AudioStreamTrack(sourceAudio);
                return;
            }

            var deviceName = Microphone.devices[micListDropdown.value];
            Microphone.GetDeviceCaps(deviceName, out int minFreq, out int maxFreq);
            var micClip = Microphone.Start(deviceName, true, 1, 48000);

            // set the latency to “0” samples before the audio starts to play.
            while (!(Microphone.GetPosition(deviceName) > 0)) {}

            sourceAudio.clip = micClip;
            sourceAudio.loop = true;
            sourceAudio.Play();
            audioStreamTrack = new AudioStreamTrack(sourceAudio);
        }

        private IEnumerator CaptureVideoStart()
        {
            if (!useWebCamToggle.isOn)
            {
                videoStreamTrack = cam.CaptureStreamTrack(WebRTCSettings.StreamSize.x, WebRTCSettings.StreamSize.y);
                sourceImage.texture = cam.targetTexture;
                yield break;
            }

            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogFormat("WebCam device not found");
                yield break;
            }

            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Debug.LogFormat("authorization for using the device is denied");
                yield break;
            }

            WebCamDevice userCameraDevice = WebCamTexture.devices[webCamListDropdown.value];
            webCamTexture = new WebCamTexture(userCameraDevice.name, WebRTCSettings.StreamSize.x, WebRTCSettings.StreamSize.y, 30);
            webCamTexture.Play();
            yield return new WaitUntil(() => webCamTexture.didUpdateThisFrame);

            videoStreamTrack = new VideoStreamTrack(webCamTexture);
            sourceImage.texture = webCamTexture;
        }

        private void HangUp()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                webCamTexture = null;
            }

            receiveAudioStream?.Dispose();
            receiveAudioStream = null;
            receiveVideoStream?.Dispose();
            receiveVideoStream = null;

            videoStreamTrack?.Dispose();
            videoStreamTrack = null;
            audioStreamTrack?.Dispose();
            audioStreamTrack = null;

            Debug.Log("Close local/remote peer connection");
            _pc1?.Dispose();
            _pc2?.Dispose();
            _pc1 = null;
            _pc2 = null;
            sourceImage.texture = null;
            sourceAudio.Stop();
            sourceAudio.clip = null;
            receiveImage.texture = null;
            receiveAudio.Stop();
            receiveAudio.clip = null;
            useWebCamToggle.interactable = true;
            webCamListDropdown.interactable = useWebCamToggle.isOn;
            useMicToggle.interactable = true;
            micListDropdown.interactable = useMicToggle.isOn;
            callButton.interactable = true;
            hangUpButton.interactable = false;
            addTracksButton.interactable = false;
            removeTracksButton.interactable = false;
        }

        private void OnIceCandidate(RTCPeerConnection pc, RTCIceCandidate candidate)
        {
            // GetOtherPc(pc).AddIceCandidate(candidate);
            Debug.Log($"{GetName(pc)} ICE candidate:\n {candidate.Candidate}");
        }

        private string GetName(RTCPeerConnection pc)
        {
            return (pc == _pc1) ? "pc1" : "pc2";
        }

        private RTCPeerConnection GetOtherPc(RTCPeerConnection pc)
        {
            return (pc == _pc1) ? _pc2 : _pc1;
        }

        private IEnumerator OnCreateOfferSuccess(RTCPeerConnection pc, RTCSessionDescription desc)
        {
            Debug.Log($"Offer from {GetName(pc)}\n{desc.sdp}");
            Debug.Log($"{GetName(pc)} setLocalDescription start");
            var op = pc.SetLocalDescription(ref desc);
            yield return op;

            if (!op.IsError)
            {
                OnSetLocalSuccess(pc);
            }
            else
            {
                var error = op.Error;
                OnSetSessionDescriptionError(ref error);
            }

            var otherPc = GetOtherPc(pc);
            Debug.Log($"{GetName(otherPc)} setRemoteDescription start");
            var op2 = otherPc.SetRemoteDescription(ref desc);
            yield return op2;
            if (!op2.IsError)
            {
                OnSetRemoteSuccess(otherPc);
            }
            else
            {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }

            Debug.Log($"{GetName(otherPc)} createAnswer start");
            // Since the 'remote' side has no media stream we need
            // to pass in the right constraints in order for it to
            // accept the incoming offer of audio and video.

            var op3 = otherPc.CreateAnswer();
            yield return op3;
            if (!op3.IsError)
            {
                yield return OnCreateAnswerSuccess(otherPc, op3.Desc);
            }
            else
            {
                OnCreateSessionDescriptionError(op3.Error);
            }
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

            // var otherPc = GetOtherPc(pc);
            // Debug.Log($"{GetName(otherPc)} setRemoteDescription start");

            // var op2 = otherPc.SetRemoteDescription(ref desc);
            // yield return op2;
            // if (!op2.IsError)
            // {
            //     OnSetRemoteSuccess(otherPc);
            // }
            // else
            // {
            //     var error = op2.Error;
            //     OnSetSessionDescriptionError(ref error);
            // }
        }

        private static void OnCreateSessionDescriptionError(RTCError error)
        {
            Debug.LogError($"Error Detail Type: {error.message}");
        }
    }
}
