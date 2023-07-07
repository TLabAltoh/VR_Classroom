const ws = require('ws').Server;
const port = 3001;
const server = new ws({ port: port });

var roomDic = {};
const OFFER = 0;
const ANSWER = 1;
const ICE = 2;
const JOIN = 3;

console.log('signaling server start. port=' + port);

server.on('connection', function (socket) {
    console.log('[server.onconnection] client connected');

    socket.on('message', function (message) {
        var obj = JSON.parse(message);
        console.log("[socket.onmessage] " + "action: " + obj.action + " from: " + obj.src + " dst: " + obj.dst);

        if (obj.action === JOIN) {
            // If room dictionary is not created
            if ((obj.room in roomDic) === false) {
                console.log("[socket.onmessage] create new room: " + obj.room);
                roomDic[obj.room] = {};
            }

            // Add
            (roomDic[obj.room])[obj.src] = socket;

            // Broadcast
            var roomValues = Object.values(roomDic[obj.room]);
            roomValues.forEach((value) => {
                if (value !== socket) {
                    console.log("[socket.onmessage] send join message");
                    value.send(message);
                }
            });
        } else if (obj.action === ICE || obj.action === OFFER || obj.action === ANSWER) {
            // Unicast
            (roomDic[obj.room])[obj.dst].send(message);
        }
    });

    socket.on("close", function close() {
        // Delete
        var roomKeys = Object.keys(roomDic);
        roomKeys.forEach((key) => {
            var socketKeys = Object.keys(roomDic[key]);
            socketKeys.forEach((socketKey) => {
                if ((roomDic[key])[socketKey] === socket) {
                    console.log("[socket.onclose] delete socket from:" + key);
                    delete (roomDic[key])[socketKey];
                }
            });
        });
    });
});