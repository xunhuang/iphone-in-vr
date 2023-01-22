
let peerConnection;
const config = {
    iceServers: [
        {
            "urls": "stun:stun.l.google.com:19302",
        },
    ]
};

const video = document.querySelector("video");

SIGNALLING_SERVER_URL = "wss://simple-webrtc-signal-3khoexoznq-uc.a.run.app/";
const socket = new WebSocket(SIGNALLING_SERVER_URL);
socket.addEventListener("open", () => {
    console.log("opened connection to signalling server");
});

socket.addEventListener("message", function (event) {
    (async () => {
        const text = await event.data;
        const json = JSON.parse(text);

        console.log("received message from signalling server", json);

        if (json.type === "offer") {
            console.log("apply remote session description");
            peerConnection = new RTCPeerConnection(config);
            peerConnection
                .setRemoteDescription({ sdp: json.payload.sdp, type: json.type })
                .then(() => peerConnection.createAnswer())
                .then(sdp => peerConnection.setLocalDescription(sdp))
                .then(() => {
                    socket.send(
                        JSON.stringify({
                            type: "answer",
                            payload: peerConnection.localDescription,
                        }),
                    );
                });

            peerConnection.ontrack = event => {
                video.srcObject = event.streams[0];
            };

            peerConnection.onicecandidate = event => {
                if (event.candidate) {
                    socket.send(
                        JSON.stringify({
                            type: "candidate",
                            payload: event.candidate,
                        }),
                    );
                }
            };
        } else if (json.type === "candidate") {
            // ice candidate
            console.log("apply ice candidate", json.payload.candidate);
            peerConnection
                .addIceCandidate(new RTCIceCandidate(json.payload))
                .catch(e => console.error(e));
        } else if (json.type === "broadcaster") {
            socket.send(
                JSON.stringify({
                    type: "watcher",
                    payload: {},
                }),
            );
        } else {
            console.log("uknown message type", json.type)
        }
    })();
});

