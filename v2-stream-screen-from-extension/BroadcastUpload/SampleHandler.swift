//
//  SampleHandler.swift
//  BroadcastUpload
//
//  Created by Roman on 28.02.2021.
//

import ReplayKit
import UserNotifications
import WebRTC

class SampleHandler: RPBroadcastSampleHandler,WebSocketDelegate, WebRTCClientDelegate {


    var webRTCClient: WebRTCClient!
    var socket: WebSocket!

    override init() {
        super.init()

        print("hello.........")
        webRTCClient = WebRTCClient()
        webRTCClient.delegate = self
        webRTCClient.setup(videoTrack: true, audioTrack: true, dataChannel: true, customFrameCapturer: true)

        socket = WebSocket(url: URL(string: "https://simple-webrtc-signal-3khoexoznq-uc.a.run.app/")!)

        socket.delegate = self
        self.socket.connect()
    }

    override func broadcastStarted(withSetupInfo setupInfo: [String : NSObject]?) {
        
        
        debugPrint("=== start")
        print("hello.........")

    }

    override func processSampleBuffer(_ sampleBuffer: CMSampleBuffer, with sampleBufferType: RPSampleBufferType) {
        if (webRTCClient.isConnected && sampleBufferType == RPSampleBufferType.video) {
            self.webRTCClient.captureCurrentFrame(sampleBuffer: sampleBuffer)
        } else {
//            print("not yet connected, dropping frame")
        }
        return;
    }

    override func broadcastPaused() {
        debugPrint("=== paused")
    
    }

    override func broadcastResumed() {
        debugPrint("=== resumed")
    }

    override func broadcastFinished() {
 
        debugPrint("FINISHED")
    }

    private func scheduleNotification() {
        print("scheduleNotification")
    }
   
}

// MARK: - WebSocket Delegate
extension SampleHandler {
    
    func websocketDidConnect(socket: WebSocketClient) {
        print("-- websocket did connect --")

        if !webRTCClient.isConnected {
            webRTCClient.connect(onSuccess: { (offerSDP: RTCSessionDescription) -> Void in
                self.sendSDP(sessionDescription: offerSDP)
            })
        }
    }
    
    func websocketDidDisconnect(socket: WebSocketClient, error: Error?) {
        print("-- websocket did disconnect --")

    }
    
    func websocketDidReceiveMessage(socket: WebSocketClient, text: String) {
        do{
            print ("-------------------------------")
            print(text)

             let signalingMessage = try JSONDecoder().decode(SignalingMessage.self, from: text.data(using: .utf8)!)
            print ("aaaaaaaaaaaaaaaaaaaaaaaaaaaa")
            if signalingMessage.type == "offer" {
                print ("bbbbbbbbbbbbbbbbbbbbbbbbb")

                webRTCClient.receiveOffer(offerSDP: RTCSessionDescription(type: .offer, sdp: (signalingMessage.sessionDescription?.sdp)!), onCreateAnswer: {(answerSDP: RTCSessionDescription) -> Void in
                    self.sendSDP(sessionDescription: answerSDP)
                })
            }else if signalingMessage.type == "answer" {
                print ("cccccccccccccccccccccccccccc")

                webRTCClient.receiveAnswer(answerSDP: RTCSessionDescription(type: .answer, sdp: (signalingMessage.sessionDescription?.sdp)!))
            }else if signalingMessage.type == "candidate" {
                print ("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee")

                let candidate = signalingMessage.candidate!
                webRTCClient.receiveCandidate(candidate: RTCIceCandidate(sdp: candidate.sdp, sdpMLineIndex: candidate.sdpMLineIndex, sdpMid: candidate.sdpMid))
            }
        }catch{
            print(error)
        }
        
    }
    
    func websocketDidReceiveData(socket: WebSocketClient, data: Data) { }
    
    // MARK: - WebRTC Signaling
    private func sendSDP(sessionDescription: RTCSessionDescription){
        var type = ""
        if sessionDescription.type == .offer {
            type = "offer"
        }else if sessionDescription.type == .answer {
            type = "answer"
        }
        
        let sdp = SDP.init(sdp: sessionDescription.sdp)
        let signalingMessage = SignalingMessage.init(type: type, sessionDescription: sdp, candidate: nil)
        do {
            let data = try JSONEncoder().encode(signalingMessage)
            let message = String(data: data, encoding: String.Encoding.utf8)!
            
            if self.socket.isConnected {
                self.socket.write(string: message)
            }
        }catch{
            print(error)
        }
    }
    
    private func sendCandidate(iceCandidate: RTCIceCandidate){
        let candidate = Candidate.init(sdp: iceCandidate.sdp, sdpMLineIndex: iceCandidate.sdpMLineIndex, sdpMid: iceCandidate.sdpMid!)
        let signalingMessage = SignalingMessage.init(type: "candidate", sessionDescription: nil, candidate: candidate)
        do {
            let data = try JSONEncoder().encode(signalingMessage)
            let message = String(data: data, encoding: String.Encoding.utf8)!
            
            if self.socket.isConnected {
                self.socket.write(string: message)
            }
        }catch{
            print(error)
        }
    }
}


// MARK: - WebRTCClient Delegate
extension SampleHandler {
    func didGenerateCandidate(iceCandidate: RTCIceCandidate) {
        self.sendCandidate(iceCandidate: iceCandidate)
    }
    
    func didIceConnectionStateChanged(iceConnectionState: RTCIceConnectionState) {
        print("ice connection state changed")

    }
    func didConnectWebRTC() {
        // MARK: Disconnect websocket
        print("web RTC connected")

        self.socket.disconnect()
    }
    
    func didDisconnectWebRTC() {
        print("web RTC disconnected")

    }
    
    func didOpenDataChannel() {
        print("did open data channel")
    }
    
    func didReceiveData(data: Data) {

    }
    
    func didReceiveMessage(message: String) {
    }
}
