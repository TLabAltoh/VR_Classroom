
const websocket = require("ws");

const port = 5000;
const bar = "----------------";

/*
const https = require("https");
const fs = require("fs");
const ssl_server_key = "./SelfSigned/live_server.key.pem";
const ssl_server_crt = "./SelfSigned/live_server.cert.pem";
const options = {
	key: fs.readFileSync(ssl_server_key),
	cert: fs.readFileSync(ssl_server_crt)
};

const server = https.createServer(options, (req, res) => {
	res.writeHead(200);
	res.end("VR_Kensyu");
});
*/

const http = require("http");
const server = http.createServer((req, res) => {
	res.writeHead(200);
	res.write("VR_Kensyu");
	res.end();
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

var syncObjects = { };

ws.on("connection", function (socket) {
	console.log("\nclient connected " + bar);

	var seatIndex = -1;

	socket.on("message", function (data, isBinary) {
		const message = isBinary ? data : data.toString();

		console.log("\nrecv message: " + message);

		const parse = JSON.parse(message);

		if(parse.role === "student"){
			if (parse.action === "sync transform") {
				console.log("sync transform");
				syncObjects[parse.transform.id] = message;
			}
			else if (parse.action === "regist") {
				console.log("\nclient registed " + bar);

				for (var i = 0; i < seatLength; i++) {
					if (seats[i] === false) {
						seatIndex = i;
						seats[i] = true;
						console.log(seats);
						break;
					}
				}

				if (seatIndex === -1) {
					console.log("student rejected");
					var obj = {
						role: "server",
						action: "reject"
					};
					var json = JSON.stringify(obj);
					socket.send(json);
				} else {
					console.log("student acepted " + seats);
					var obj = {
						role: "server",
						action: "acept",
						seatIndex: seatIndex
					};
					var json = JSON.stringify(obj);
					socket.send(json);

					console.log("load current world data");

					// https://pisuke-code.com/javascript-dictionary-foreach/
					Object.values(syncObjects).forEach(function (value) {
						socket.send(value);
					});
				}
			}
		}

		ws.clients.forEach(client => {
			if (client != socket) {
				client.send(message);
			}
        });
	});

	socket.on('close', function close() {
		console.log("\nclient closed " + bar);

		if(seatIndex !== -1){
			seats[seatIndex] = false;
			console.log(seats);

			ws.clients.forEach(client => {
				var obj = {
					"role": "server",
					"action": "disconnect",
					"seatIndex": seatIndex
				};
				var json = JSON.stringify(obj);

				if (client != socket) {
					client.send(json);
				}
			});
		}
	});
});
