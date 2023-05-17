
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
var grabbTable = [];
for(var i = 0; i < seatLength; i++){
	seats.push(false);
	socketTable.push(null);
	grabbTable.push([]);
}

//
// Sync Objects
//

var syncObjects = { };
var rbTable = {};

//
// Const values
//

//public enum WebRole {
//	server,
//	host,
//	guest
//}

//public enum WebAction {
//	regist,
//	regect,
//	acept,
//	guestDisconnect,
//	guestParticipation,
//	allocateGravity,
//	setGravity,
//	grabbLock,
//	forceRelease,
//	syncTransform
//}

const SERVER = 0;
const HOST = 1;
const GUEST = 2;

const REGIST = 0;
const REGECT = 1;
const ACEPT = 2;
const GUESTDISCONNECT = 3;
const GUESTPARTICIPATION = 4;
const ALLOCATEGRAVITY = 5;
const SETGRAVITY = 6;
const GRABBLOCK = 7;
const FORCERELEASE = 8;
const SYNCTRANSFORM = 9;
const SYNCANIM = 10;

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
				role: SERVER,
				action: ALLOCATEGRAVITY,
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

		// console.log("\nrecv message: " + message);

		const parse = JSON.parse(message);

		if (parse.action === SYNCTRANSFORM) {

			//
			// sync transfrom
			//

			//console.log("sync transform");
			syncObjects[parse.transform.id] = parse;

			ws.clients.forEach(client => {
				if (client != socket) {
					client.send(message);
				}
			});

			return;
		} else if (parse.action == SETGRAVITY) {

			//
			// set rigidbody gravity on / off
			//

			console.log("set gravity");

			var targetIndex = rbTable[parse.transform.id];
			if (targetIndex !== seatIndex && targetIndex !== undefined) {
				socketTable[targetIndex].send(message);
			}

			return;
		} else if (parse.action == GRABBLOCK) {

			//
			// Register/unregister objects grabbed by the player in the Grabb Table
			//

			console.log("grabb lock");

			if (parse.active === true) {
				grabbTable[seatIndex].push(parse.transform);
			} else {
				grabbTable[seatIndex] = grabbTable[seatIndex].filter(function (value) { return value.id !== parse.transform.id });
			}

			console.log(grabbTable[seatIndex]);

			ws.clients.forEach(client => {
				if (client != socket) {
					client.send(message);
				}
			});

			return;
		} else if (parse.action == FORCERELEASE) {
			console.log("force release");

			grabbTable[seatIndex] = grabbTable[seatIndex].filter(function (value) { return value.id !== parse.transform.id });

			console.log(grabbTable[seatIndex]);

			ws.clients.forEach(client => {
				if (client != socket) {
					client.send(message);
				}
			});

			return;
		} else if (parse.action == SYNCANIM) {
			ws.clients.forEach(client => {
				if (client != socket) {
					client.send(message);
				}
			});

			return;
        }

		if (parse.role === GUEST) {

			if (parse.action === REGIST) {

				//
				// regist client to seat table
				//

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
					console.log("guest rejected");
					var obj = {
						role: SERVER,
						action: REGECT
					};
					var json = JSON.stringify(obj);
					socket.send(json);

					return;
				} else {
					console.log("guest acepted ");
					console.log(seats);

					seatFilled += 1;

					var obj = {
						role: SERVER,
						action: ACEPT,
						seatIndex: seatIndex
					};
					var json = JSON.stringify(obj);
					socket.send(json);

					console.log("load current world data");

					// Remove transforms for controllers that already exist

					delete syncObjects["OVRControllerPrefab"];
					delete syncObjects["OVRControllerPrefab." + seatIndex.toString() + ".RTouch"];
					delete syncObjects["OVRControllerPrefab." + seatIndex.toString() + ".LTouch"];

					var syncObjValues = Object.values(syncObjects);

					// https://pisuke-code.com/javascript-dictionary-foreach/
					syncObjValues.forEach(function (value) {
						json = JSON.stringify(value);
						socket.send(json);
					});

					console.log("allocate rigidbody");

					allocateRigidbody();

					console.log("notify guest participation");

					for(var index = 0; index < seatLength; index++){
						var target = socketTable[index];
						if(target !== null){
							obj = {
								role: SERVER,
								action: GUESTPARTICIPATION,
								seatIndex: index
							};
							json = JSON.stringify(obj);

							ws.clients.forEach(client => {
								if (client != target) {
									client.send(json);
								}
							});
						}
					}

					return;
				}
			}
		}
	});

	socket.on('close', function close() {
		console.log("\nclient closed " + bar);

		if (seatIndex !== -1) {
			var obj = {
				"role": SERVER,
				"action": GUESTDISCONNECT,
				"seatIndex": seatIndex
			};
			var json = JSON.stringify(obj);

			ws.clients.forEach(client => {
				if (client != socket) {
					client.send(json);

					grabbTable[seatIndex].forEach(function (value) {
						var obj1 = {
							"role": SERVER,
							"action": SETGRAVITY,
							"transform": value
						};
						var json1 = JSON.stringify(obj1);

						client.send(json1);
					});
				}
			});

			seats[seatIndex] = false;
			socketTable[seatIndex] = null;
			grabbTable[seatIndex] = [];
			seatFilled -= 1;

			console.log(seats);
		}

		allocateRigidbody();
	});
});
