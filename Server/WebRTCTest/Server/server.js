const ws        = require('ws').Server;
const port      = 3001;
const server    = new ws({ port: port });

console.log('signaling server start. port=' + port);

server.on('connection', function (ws) {
    console.log('[server.onconnection] client connected');

    ws.on('message', function (message) {
        console.log("[ws.onmessage] " + message)
        ws.clients.forEach(function each(client) {
            if (ws === client) client.send(message);
        });
    });
});