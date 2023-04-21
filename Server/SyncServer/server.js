
const websocket = require("ws");

const port = 5000;
const bar = "----------------";

//
// Start HTTPS Server
//

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

//
// Start HTTP Server
//

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

//
// Player Data
//

const seatLength = 3;
var seatFilled = 0;
var seats = [];
var socketTable = [];
for(var i = 0; i < seatLength; i++){
	seats.push(false);
	socketTable.push(null);
}

//
// Sync Objects
//

var syncObjects = { };
var rbTable = {};

function allocateRigidbody(){
	var syncObjValues = Object.values(syncObjects);

	var check = false;
	for(var j = 0; j < seatLength; j++){
		if(seats[j] === true){
			check = true;
		}
	}

	if(check === false){
		return;
	}

	var i = 0;
	syncObjValues.forEach(function (value) {
		if (value.transform.rigidbody === true && value.transform.gravity === true) {
			var live = true;
			while(live === true){
				if (seats[i] === true) {
					rbTable[value.transform.id] = i;
					i = (i + 1) % seatLength;
					live = false;
					break;
				} else {
					// no one in the seat
					i = (i + 1) % seatLength;
					continue;
				}
			}
		}
	});

	for (var j = 0; j < seatLength; j++) {
		if (seats[j] === false) {
			// no one in the seat
			continue;
		}

		syncObjValues.forEach(function (value) {
			// Set useGravity to Off for rigidbodies that you are not in charge of
			var obj = {
				role: "server",
				action: "allocate gravity",
				active: (rbTable[value.transform.id] === j),
				transform: {
					id: value.transform.id
				}
			};
			var json = JSON.stringify(obj);
			socketTable[j].send(json);
		});
	}
}

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
				syncObjects[parse.transform.id] = parse;
			}
			else if (parse.action === "regist") {
				console.log("\nclient registed " + bar);
				for (var i = 0; i < seatLength; i++) {
					if (seats[i] === false) {
						seatIndex = i;
						seats[i] = true;
						socketTable[i] = socket;
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
					seatFilled += 1;

					console.log("student acepted ");
					console.log(seats);

					var obj = {
						role: "server",
						action: "acept",
						seatIndex: seatIndex
					};
					var json = JSON.stringify(obj);
					socket.send(json);

					console.log("load current world data");

					var syncObjValues = Object.values(syncObjects);

					// https://pisuke-code.com/javascript-dictionary-foreach/
					syncObjValues.forEach(function (value) {
						var json = JSON.stringify(value);
						socket.send(json);
					});

					console.log("allocate rigidbody");

					allocateRigidbody();
				}
			}else if(parse.action == "set gravity"){
				var targetIndex = rbTable[parse.transform.id];
				console.log("rb gravity switched: " + socketTable[targetIndex]);
				if(targetIndex !== seatIndex && targetIndex !== undefined){
					socketTable[targetIndex].send(message);
				}

				return;
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

		if (seatIndex !== -1) {
			seatFilled -= 1;
			seats[seatIndex] = false;
			socketTable[seatIndex] = null;

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

		allocateRigidbody();
	});
});
