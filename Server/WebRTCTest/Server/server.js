const ws        = require('ws').Server;
const port      = 3001;
const server    = new ws({ port: port });

var roomDic     = { };
const OFFER     = 0;
const ANSWER    = 1;
const ICE       = 2;
const JOIN      = 3;

console.log('signaling server start. port=' + port);

server.on('connection', function (socket) {
    console.log('[server.onconnection] client connected');

    socket.on('message', function (message) {
        var obj = JSON.parse(message);
        console.log("[socket.onmessage] " + "action: " + obj.action + " from: " + obj.src + " dst: " + obj.dst);

        if (obj.action === JOIN) {
            // If room dictionary is not created
            if ((obj.roomName in roomDic) === false) roomDic[obj.roomName] = { };

            // Broadcast
            var roomValues = Object.values(roomDic[obj.roomName]);
            roomValues.forEach((value) => {
                console.log("[socket.onmessage] send join message");
                value.send(message);
            });
            // Add
            (roomDic[obj.roomName])[obj.src] = socket;
        } else if (obj.action === ICE || obj.action === OFFER || obj.action === ANSWER) {
            // Unicast
            (roomDic[obj.roomName])[obj.dst].send(message);
        }
    });

    socket.on("close", function close() {
        // Delete
        var roomKeys = Object.keys(roomDic);
        roomKeys.forEach((key) => {
            delete (roomDic[key])[socket];
        });
    });
});