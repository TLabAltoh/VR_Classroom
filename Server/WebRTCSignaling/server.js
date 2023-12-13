const ws = require('ws').Server;
const port = 3001;
const server = new ws({ port: port });

// room dictionary
var roomDic = {};

// action number
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
                roomDic[obj.room] = { };
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

            // check room dictionary is exist
            if (roomDic === null || roomDic === undefined) return;

            // check room is exist
            var room = roomDic[obj.room];
            if (room === null || room === undefined) return;

            // if user is not exist in the room
            var socketValues = Object.values(room);
            if (socketValues.includes(socket) === false) return;

            // if room is exist
            var socketKeys = Object.keys(room);
            socketKeys.forEach((socketKey) => {
                if (room[socketKey] === socket) {
                    // if socket is mine, Delete from room
                    console.log("[socket.onclose] " + socketKey + " delete socket from: " + obj.room);
                    delete room[socketKey];
                } else {
                    // if socket is not mine, send exit message
                    console.log("[socket.onmessage] send exit message for " + socketKey);
                    room[socketKey].send(message);
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

        if (roomDic === null || roomDic === undefined) return;

        var roomKeys = Object.keys(roomDic);
        roomKeys.forEach((roomKey) => {
            // check room is exist
            var room = roomDic[roomKey];
            if (room === null || room === undefined) return;

            // if user is not exist in the room
            var socketValues = Object.values(room);
            if (socketValues.includes(socket) === false) return;

            /*
             *             obj.src = userID;
            obj.room = roomID;
            obj.dst = dst;
            obj.action = (int)action;
            obj.desc = desc;
            obj.ice = ice;
             * */

            // Notify players to exit
            var obj = {
                "src": "",
                "room": roomKey,
                "dst": "",
                "action": EXIT,
                "desc": "",
                "ice": ""
            };

            // if room is exist
            var socketKeys = Object.keys(room);

            // get exited user key
            socketKeys.forEach((socketKey) => {
                if (room[socketKey] === socket) {
                    // if socket is mine, Delete from room
                    console.log("[socket.onclose] " + socketKey + " delete socket from: " + obj.room);
                    obj.src = socketKey;
                    delete room[socketKey];
                }
            });

            var json = JSON.stringify(obj);

            // nortify other user
            socketKeys.forEach((socketKey) => {
                if (room[socketKey] !== socket && room[socketKey] !== undefined && room[socketKey] !== null) {
                    // if socket is not mine, send exit message
                    console.log("[socket.onmessage] send exit message for " + socketKey);
                    room[socketKey].send(json);
                }
            });
        })

        return;
    });
});
