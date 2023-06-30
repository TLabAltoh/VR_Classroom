const url       = 'ws://localhost:3001/';
const websocket = require('ws');
const ws        = new websocket(url);

let peerConnection = null;

ws.onopen = function (evt) {
    console.log('[ws.open()]');
    connect();
};

ws.onerror = function (err) {
    console.error('[ws.onerror()] ERR:', err);
};

ws.onmessage = function (evt) {
    console.log('[ws.onmessage()] data:', evt.data);

    let message = JSON.parse(evt.data);

    if (message.type === 'offer') {
        // got offer
        console.log('[onmessage()] received offer ...');
        let offer = new RTCSessionDescription(message);
        setOffer(offer);
    }

    else if (message.type === 'answer') {
        // got answer
        console.log('[onmessage()] received answer ...');
        let answer = new RTCSessionDescription(message);
        setAnswer(answer);
    }
};

function sendSdp(sessionDescription) {
    // シグナリングサーバーに送る
    let message = JSON.stringify(sessionDescription);
    console.log('sending SDP=' + message);
    ws.send(message);
}

function prepareNewConnection() {
    let pc_config   = { "iceServers": [] };
    let peer        = new RTCPeerConnection(pc_config);

    // on get remote stream
    if ('ontrack' in peer) {
        peer.ontrack = function (event) {
            console.log('[peer.ontrack()] peer.ontrack()');
            let stream = event.streams[0];
            // process stream
        };
    }
    else {
        peer.onaddstream = function (event) {
            console.log('[peer.onaddstream()] peer.onaddstream()');
            let stream = event.stream;
            // process stream
        };
    }

    // on get local ICE candidate
    peer.onicecandidate = function (evt) {
        if (evt.candidate) {
            console.log("[peer.onicecandidate()] " + evt.candidate);
        } else {
            console.log('[peer.onicecandidate()] empty ice event');
            sendSdp(peer.localDescription);
        }
    };

    // when need to exchange SDP
    peer.onnegotiationneeded = function (evt) {
        console.log('[peer.onnegotiationneeded()]');
    };

    // other events
    peer.onicecandidateerror = function (evt) {
        console.error('[peer.onicecandidateerror()] ICE candidate ERROR:', evt);
    };

    peer.onsignalingstatechange = function () {
        console.log('== signaling status=' + peer.signalingState);
    };

    peer.oniceconnectionstatechange = function () {
        console.log('== ice connection status=' + peer.iceConnectionState);
        if (peer.iceConnectionState === 'disconnected') {
            console.log('-- disconnected --');
            hangUp();
        }
    };

    peer.onicegatheringstatechange = function () {
        console.log('==***== ice gathering state=' + peer.iceGatheringState);
    };

    peer.onconnectionstatechange = function () {
        console.log('==***== connection state=' + peer.connectionState);
    };

    peer.onremovestream = function (event) {
        console.log('[peer.onremovestream()]');
    };

    // add local stream
    if (localStream) {
        console.log('[prepareNewConnection()] adding local stream...');
        peer.addStream(localStream);
    } else
        console.warn('[prepareNewConnection()] no local stream, but continue.');

    return peer;
}

//#region Offer
function makeOffer() {
    peerConnection = prepareNewConnection();

    // use Trickle ICE

    peerConnection.createOffer()
        .then(function (sessionDescription) {
            console.log('[createOffer()] succsess in promise');
            return peerConnection.setLocalDescription(sessionDescription);
        }).then(function () {
            console.log('[setLocalDescription()] succsess in promise');
            sendSdp(peerConnection.localDescription);
        }).catch(function (err) {
            console.error(err);
        });
}

function setOffer(sessionDescription) {
    if (peerConnection) console.error('[setOffse()] peerConnection alreay exist!');

    peerConnection = prepareNewConnection();
    peerConnection.setRemoteDescription(sessionDescription)
        .then(function () {
            console.log('[setRemoteDescription(offer)] succsess in promise');
            makeAnswer();
        }).catch(function (err) {
            console.error('[setRemoteDescription(offer)] ERROR: ', err);
        });
}
//#endregion Offer

//#region Answer
function makeAnswer() {
    console.log('[makeAnswer()] creating remote session description...');
    if (!peerConnection) {
        console.error('[makeAnswer()] peerConnection NOT exist!');
        return;
    }

    // use Trickle ICE

    peerConnection.createAnswer()
        .then(function (sessionDescription) {
            console.log('[createAnswer()] create ICE');
            return peerConnection.setLocalDescription(sessionDescription);
        }).then(function () {
            console.log('[setLocalDescription()] succsess in promise');
            sendSdp(peerConnection.localDescription);
        }).catch(function (err) {
            console.error(err);
        });
}

function setAnswer(sessionDescription) {
    if (!peerConnection) {
        console.error('[makeAnswer()] peerConnection NOT exist!');
        return;
    }

    peerConnection.setRemoteDescription(sessionDescription)
        .then(function () {
            console.log('[setRemoteDescription(answer)] succsess in promise');
        }).catch(function (err) {
            console.error('[setRemoteDescription(answer)] ERROR: ', err);
        });
}
//#endregion Answer


function connect() {
    // start PeerConnection

    if (!peerConnection) {
        console.log('[connect()] make Offer');
        makeOffer();
    }
    else
        console.warn('[connect()] peer already exist.');
}

function hangUp() {
    // close PeerConnection

    if (peerConnection) {
        console.log('[hangUp()] Hang up.');
        peerConnection.close();
        peerConnection = null;
        pauseVideo(remoteVideo);
    }
    else
        console.warn('[hangUp()] peer NOT exist.');
}