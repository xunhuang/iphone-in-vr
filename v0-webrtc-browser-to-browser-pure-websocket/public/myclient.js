
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
// SIGNALLING_SERVER_URL = "wss://video-demo-3khoexoznq-uc.a.run.app/";
const socket = new WebSocket(SIGNALLING_SERVER_URL);
socket.addEventListener("open", () => {
    console.log("opened connection to signalling server");
    // send("ESTABLISHED");
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
                .setRemoteDescription({ sdp: json.sessionDescription.sdp, type: json.type })
                .then(() => peerConnection.createAnswer())
                .then(sdp => peerConnection.setLocalDescription(sdp))
                .then(() => {
                    socket.send(
                        JSON.stringify({
                            type: "answer",
                            sessionDescription: peerConnection.localDescription,
                        }),
                    );
                });

            peerConnection.ontrack = event => {
                video.srcObject = event.streams[0];
            };

            peerConnection.onicecandidate = event => {
                if (event.candidate) {
                    console.log("---------");
                    console.log(event.candidate)
                    socket.send(
                        JSON.stringify({
                            type: "candidate",
                            candidate: {
                                sdp: event.candidate.candidate,
                                sdpMLineIndex: event.candidate.sdpMLineIndex,
                                sdpMid: event.candidate.sdpMid,
                            },
                        }),
                    );
                }
            };

            // peerConnection
            //     .createOffer()
            //     .then(sdp => peerConnection.setLocalDescription(sdp))
            //     .then(() => {
            //         socket.send(
            //             JSON.stringify({
            //                 type: "offer",
            //                 payload: peerConnection.localDescription
            //             }),
            //         );
            //     });
        } else if (json.type === "candidate") {
            console.log("apply ice candidate", json.candidate);
            let candidate = json.candidate;
            if (!candidate.candidate) {
                candidate.candidate = candidate.sdp;
            }
            console.log("candidate fixed", candidate);
            peerConnection
                .addIceCandidate(new RTCIceCandidate(candidate))
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

