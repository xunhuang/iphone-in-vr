const peerConnections = {};
const config = {
    iceServers: [
        {
            "urls": "stun:stun.l.google.com:19302",
        },
    ]
};

// Get camera and microphone
const videoElement = document.querySelector("video");
const audioSelect = document.querySelector("select#audioSource");
const videoSelect = document.querySelector("select#videoSource");

SIGNALLING_SERVER_URL = "wss://simple-webrtc-signal-3khoexoznq-uc.a.run.app/";
const socket = new WebSocket(SIGNALLING_SERVER_URL);

socket.addEventListener("open", () => {
    console.log("opened connection to signalling server");

    socket.send(
        JSON.stringify({
          type: "broadcaster",
        }),
    );
});

let peerConnection;

socket.addEventListener("message", function (event) {
    (async () => {
        const text = await event.data;
        const json = JSON.parse(text);
        console.log("received message from signalling server", json);

        if (json.type === "watcher") {

            peerConnection = new RTCPeerConnection(config);
            // peerConnections[id] = peerConnection;

            let stream = videoElement.srcObject;
            stream.getTracks().forEach(track => peerConnection.addTrack(track, stream));

            peerConnection.onicecandidate = event => {
                if (event.candidate) {
                    // socket.emit("candidate", id, event.candidate);
                    socket.send(
                        JSON.stringify({
                            type: "candidate",
                          candidate: event.candidate,
                        }),
                    );
                }
            };

            peerConnection
                .createOffer()
                .then(sdp => peerConnection.setLocalDescription(sdp))
                .then(() => {
                    socket.send(
                        JSON.stringify({
                            type: "offer",
                          sessionDescription: peerConnection.localDescription
                        }),
                    );
                });
        } else if (json.type === "answer") {
          peerConnection.setRemoteDescription(json.sessionDescription);
        } else if (json.type === "candidate") {
          peerConnection.addIceCandidate(new RTCIceCandidate(json.candidate))
                .catch(e => console.error(e));
        } else {
            console.log("uknown message type", json.type)
        }
    })();
});

window.onunload = window.onbeforeunload = () => {
    socket.close();
};

audioSelect.onchange = getStream;
videoSelect.onchange = getStream;

getStream()
    .then(getDevices)
    .then(gotDevices);

function getDevices() {
    return navigator.mediaDevices.enumerateDevices();
}

function gotDevices(deviceInfos) {
    window.deviceInfos = deviceInfos;
    for (const deviceInfo of deviceInfos) {
        const option = document.createElement("option");
        option.value = deviceInfo.deviceId;
        if (deviceInfo.kind === "audioinput") {
            option.text = deviceInfo.label || `Microphone ${audioSelect.length + 1}`;
            audioSelect.appendChild(option);
        } else if (deviceInfo.kind === "videoinput") {
            option.text = deviceInfo.label || `Camera ${videoSelect.length + 1}`;
            videoSelect.appendChild(option);
        }
    }
}

function getStream() {
    if (window.stream) {
        window.stream.getTracks().forEach(track => {
            track.stop();
        });
    }
    const audioSource = audioSelect.value;
    const videoSource = videoSelect.value;
    const constraints = {
        audio: { deviceId: audioSource ? { exact: audioSource } : undefined },
        video: { deviceId: videoSource ? { exact: videoSource } : undefined }
    };
    return navigator.mediaDevices
        .getUserMedia(constraints)
        .then(gotStream)
        .catch(handleError);
}

function gotStream(stream) {
    window.stream = stream;
    audioSelect.selectedIndex = [...audioSelect.options].findIndex(
        option => option.text === stream.getAudioTracks()[0].label
    );
    videoSelect.selectedIndex = [...videoSelect.options].findIndex(
        option => option.text === stream.getVideoTracks()[0].label
    );
    videoElement.srcObject = stream;
  socket.send(
    JSON.stringify({
      type: "broadcaster",
    }),
  );
}

function handleError(error) {
    console.error("Error: ", error);
}