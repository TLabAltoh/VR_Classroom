
const https = require("https");
const websocket = require("ws");

const fs = require("fs");
const path = require('path');

const port = 443;
const bar = "----------------";

const ssl_server_key = "./SelfSigned/live_server.key.pem";
const ssl_server_crt = "./SelfSigned/live_server.cert.pem";
const options = {
	key: fs.readFileSync(ssl_server_key),
	cert: fs.readFileSync(ssl_server_crt)
};

/*
const server = https.createServer(options, (req, res) => {
	res.writeHead(200);
	res.end("VR_Kensyu");
});
*/

const server = https.createServer((req, res) => {
	res.writeHead(200);
	res.end("VR_Kensyu");
});

console.log("\ncreate server " + bar);

server.listen(port);

let ws = new websocket.Server({server: server});

console.log("\nserver start " + bar);

const seatLength = 3;
var seats = [];
for(var i = 0; i < seatLength; i++){
	seats.push(false);
}

ws.on("connection", function (socket) {
	console.log("\nclient connected " + bar);

	var seatIndex = -1;

	socket.on("message", function (data, isBinary) {
		const message = isBinary ? data : data.toString();
		const parse = JSON.parse(message);

		if(parse.action === "regist" && parse.role === "student"){
			console.log("\nclient registed " + bar);

			for(var i = 0; i < seatLength; i++){
				if(seats[i] === false){
					seatIndex = i;
					seats[i] = true;
					console.log(seats);
					break;
				}
			}

			if(seatIndex === -1){
				console.log("student rejected");
				var obj = {
					role: "server",
					action: "reject"
				};
				var json = JSON.stringify(obj);
				socket.send(json);
			}else{
				console.log("student acepted " + seats);
				var obj = {
					role: "server",
					action: "acept",
					seatIndex: seatIndex
				};
				var json = JSON.stringify(obj);
				socket.send(json);
			}
		}

		console.log(message);
		ws.clients.forEach(client => {
            client.send(message);
        });
	});

	socket.on('close', function close() {
		console.log("\nclient closed " + bar);

		if(seatIndex !== -1){
			seats[seatIndex] = false;
			console.log(seats);
		}
	});
});
