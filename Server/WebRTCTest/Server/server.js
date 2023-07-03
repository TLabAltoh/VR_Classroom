const ws        = require('ws').Server;
const port      = 3001;
const server    = new ws({ port: port });

var joinDic     = { };
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
            // Broadcast
            var joinValues = Object.values(joinDic);
            joinValues.forEach((value) => {
                console.log("[socket.onmessage] send join message");
                value.send(message);
            });
            // Add
            joinDic[obj.src] = socket;
        } else if (obj.action === ICE || obj.action === OFFER || obj.action === ANSWER) {
            // Unicast
            joinDic[obj.dst].send(message);
        }
    });
});