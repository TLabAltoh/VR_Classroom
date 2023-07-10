const ws = require('ws').Server;
const port = 3001;
const server = new ws({ port: port });

var roomDic = {};
const OFFER = 0;
const ANSWER = 1;
const ICE = 2;
const JOIN = 3;
const EXIT = 4;

console.log('signaling server start. port=' + port);

server.on('connection', function (socket) {

    console.log('[server.onconnection] client connected');

    socket.on('message', function (message) {
        var obj = JSON.parse(message);
        console.log("[socket.onmessage] " + "action: " + obj.action + " from: " + obj.src + " dst: " + obj.dst);

        if (obj.action === JOIN) {

            //
            // User Join
            //

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

            return;
        } else if (obj.action === EXIT) {

            //
            // User Exit
            //

            if (roomDic === null || roomDic === undefined) return;

            if (roomDic[obj.room] === null || roomDic[obj.room] === undefined) return;

            var socketKeys = Object.keys(roomDic[obj.room]);
            socketKeys.forEach((socketKey) => {

                if ((roomDic[obj.room])[socketKey] === socket) {

                    console.log("[socket.onclose] " + socketKey + " delete socket from: " + obj.room);

                    // Delete from room
                    delete (roomDic[obj.room])[socketKey];

                    // Broadcast exit message
                    var otherSocketKeys = Object.keys(roomDic[obj.room]);
                    otherSocketKeys.forEach((otherSocketKey) => {
                        console.log("[socket.onmessage] send exit message for " + otherSocketKey);
                        (roomDic[obj.room])[otherSocketKey].send(message);
                    });
                }
            });

            return;
        } else if (obj.action === ICE || obj.action === OFFER || obj.action === ANSWER) {

            //
            // Defualt
            //

            // Unicast message
            (roomDic[obj.room])[obj.dst].send(message);

            return;
        }
    });

    socket.on("close", function close() {

        //
        // Close
        //

        console.log("[socket.onclose] close");

        return;
    });
});