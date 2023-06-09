
const websocket = require("ws");

const port = 5000;
const bar = "----------------";

// #region Create HTTPS Server

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

// #endregion Create HTTPS Server

// #region Create HTTP Server

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

// #endregion Create HTTP Server

// #region Player Data

const seatLength = 4;
var seatFilled = 0;
var seats = [];
var socketTable = [];

// for debug
var grabbTable = [];
for(var i = 0; i < seatLength; i++){
	seats.push(false);
	socketTable.push(null);
	grabbTable.push([]);
}

//#endregion Player Data

// #region Sync Objects

// list of updated transforms
var syncObjects = {};

// List of updated animations
var syncAnims = {};

// A dictionary of players responsible for computing rigidbody gravity per object
var rbTable = {};

// #endregion Sync Objects

// #region Const values

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
//	divideGrabber,
//	syncTransform,
//	syncAnim,
//	reflesh,
//  customAction
//}

const SERVER				= 0;
const HOST					= 1;
const GUEST					= 2;

const REGIST				= 0;
const REGECT				= 1;
const ACEPT					= 2;
const GUESTDISCONNECT		= 3;
const GUESTPARTICIPATION	= 4;
const ALLOCATEGRAVITY		= 5;
const SETGRAVITY			= 6;
const GRABBLOCK				= 7;
const FORCERELEASE			= 8;
const DIVIDEGRABBER			= 9;
const SYNCTRANSFORM			= 10;
const SYNCANIM				= 11;
const REFRESH				= 12;
const CUSTOMACTION			= 13;

// #endregion Const values

// #region Reassign rigidbody with current member ()

function allocateRigidbody(){

	console.log("re allocate rigidbody");

	// player existence check

	var check = false;
	for (var j = 0; j < seatLength; j++)
		if (seats[j] === true)
			check = true;

	if (check === false)
		return;

	// Recalculate rigidbody allocation table

	var syncObjValues = Object.values(syncObjects);

	var seatIndex = 0;

	syncObjValues.forEach(function (value) {
		if (value.transform.rigidbody === true && value.transform.gravity === true) {
			while (true) {
				// Check if someone is in the seat
				if (seats[seatIndex] === true) {
					rbTable[value.transform.id] = seatIndex;
					seatIndex = (seatIndex + 1) % seatLength;
					break;
				} else {
					seatIndex = (seatIndex + 1) % seatLength;
					continue;
				}
			}
		}
	});

	// Re-allocate with updated tables

	for (seatIndex = 0; seatIndex < seatLength; seatIndex++) {
		// Check if someone is in the seat
		if (seats[seatIndex] === false)
			continue;

		syncObjValues.forEach(function (value) {
			// Set useGravity to Off for rigidbodies that you are not in charge of
			var obj = {
				role: SERVER,
				action: ALLOCATEGRAVITY,
				active: (rbTable[value.transform.id] === seatIndex),
				transform: {
					id: value.transform.id
				}
			};
			var json = JSON.stringify(obj);
			socketTable[seatIndex].send(json);
		});
	}
}
// #endregion Reassign rigidbody with current member ()

ws.on("connection", function (socket) {
	console.log("\nclient connected " + bar);

	var seatIndex = -1;

	// #region socket on message

	socket.on("message", function (data, isBinary) {
		const message = isBinary ? data : data.toString();

		//console.log("\nrecv message: " + message);

		const parse = JSON.parse(message);

		if (parse.action === SYNCTRANSFORM) {

			//
			// sync transfrom
			//

			syncObjects[parse.transform.id] = parse;

			ws.clients.forEach(client => {
				if (client != socket)
					client.send(message);
			});

			return;
		} else if (parse.action == SETGRAVITY) {

			//
			// set rigidbody gravity on / off
			//

			console.log("set gravity");

			var targetIndex = rbTable[parse.transform.id];

			// if target is not own and exist, send message
			if (targetIndex !== seatIndex && targetIndex !== undefined)
				socketTable[targetIndex].send(message);

			return;
		} else if (parse.action == GRABBLOCK) {

			//
			// Register/unregister objects grabbed by the player in the Grabb Table
			//

			console.log("grabb lock");

			if (parse.seatIndex !== -1) {
				grabbTable[seatIndex].push(parse.transform);
			} else {
				grabbTable[seatIndex] = grabbTable[seatIndex].filter(function (value) { return value.id !== parse.transform.id });
            }

			console.log(grabbTable[seatIndex]);

			ws.clients.forEach(client => {
				if (client != socket)
					client.send(message);
			});

			return;
		} else if (parse.action == FORCERELEASE) {

			//
			// force release object from player
			//

			console.log("force release");

			grabbTable[seatIndex] = grabbTable[seatIndex].filter(function (value) { return value.id !== parse.transform.id });

			console.log(grabbTable[seatIndex]);

			ws.clients.forEach(client => {
				if (client != socket)
					client.send(message);
			});

			return;
		} else if (parse.action == SYNCANIM) {

			//
			// sync animation
			//

			console.log("sync animation");

			syncAnims[parse.animator.id] = parse;

			ws.clients.forEach(client => {
				if (client != socket)
					client.send(message);
			});

			return;
		} else if (parse.action === REFRESH) {

			//
			// reflesh server
			//

			console.log("reflesh");

			// Re-allocate rigidbody gravity

			allocateRigidbody();

			// Force refles

			var obj = {
				role: SERVER,
				action: REFRESH
			};
			var json = JSON.stringify(obj);

			ws.clients.forEach(client => {
				client.send(json);
			});

			return;
		} else if (parse.action === REGIST) {

			//
			// regist client to seat table
			//

			if (parse.role === GUEST) {

				// #region Guest participation approval process

				console.log("\nreceived a request to join " + bar);

				// Guest table : 1 ~ seatLength - 1
				for (var i = 1; i < seatLength; i++) {
					if (seats[i] === false) {
						seatIndex = i;
						seats[i] = true;
						socketTable[i] = socket;
						break;
					}
				}

				if (seatIndex === -1) {
					console.log("declined to participate");

					var obj = {
						role: SERVER,
						action: REGECT
					};
					var json = JSON.stringify(obj);
					socket.send(json);

					return;
				} else {
					console.log("approved to participate");
					console.log(seats);

					seatFilled += 1;

					var obj = {
						role: SERVER,
						action: ACEPT,
						seatIndex: seatIndex
					};
					var json = JSON.stringify(obj);
					socket.send(json);

					console.log("sending current world data");

					// Remove transforms for controllers that already exist

					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".RTouch"];
					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".LTouch"];
					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".Head"];

					// Load world transforms prior to rigidbody assignment
					var syncObjValues = Object.values(syncObjects);
					var syncAnimValues = Object.values(syncAnims);

					// https://pisuke-code.com/javascript-dictionary-foreach/
					syncObjValues.forEach(function (value) {
						json = JSON.stringify(value);
						socket.send(json);
					});

					syncAnimValues.forEach(function (value) {
						json = JSON.stringify(value);
						socket.send(json);
					});

					console.log("re-assign the rigidbody");

					allocateRigidbody();

					console.log("notify existing participants");

					obj = {
						role: SERVER,
						action: GUESTPARTICIPATION,
						seatIndex: seatIndex
					};
					json = JSON.stringify(obj);

					for (var index = 0; index < seatLength; index++) {
						var target = socketTable[index];
						if (target !== null)
							target.send(json);
					}

					console.log("notify new participants")

					for (var index = 0; index < seatLength; index++) {
						var target = socketTable[index];
						if (target !== null) {
							obj = {
								role: SERVER,
								action: GUESTPARTICIPATION,
								seatIndex: index
							};
							json = JSON.stringify(obj);

							socket.send(json);
						}
					}

					return;
				}

				// #endregion Guest participation approval process

			} else if (parse.role === HOST) {

				// #region Host participation approval process

				if (seats[0] === true) {
					console.log("declined to participate");

					var obj = {
						role: SERVER,
						action: REGECT
					};
					var json = JSON.stringify(obj);
					socket.send(json);

					return;
				} else {
					console.log("approved to participate");

					seatIndex = 0;
					seats[0] = true;
					socketTable[0] = socket;

					console.log(seats);

					seatFilled += 1;

					var obj = {
						role: SERVER,
						action: ACEPT,
						seatIndex: seatIndex
					};
					var json = JSON.stringify(obj);
					socket.send(json);

					console.log("sending current world data");

					// Remove transforms for controllers that already exist

					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".RTouch"];
					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".LTouch"];
					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".Head"];

					var syncObjValues = Object.values(syncObjects);
					var syncAnimValues = Object.values(syncAnims);

					// https://pisuke-code.com/javascript-dictionary-foreach/
					syncObjValues.forEach(function (value) {
						json = JSON.stringify(value);
						socket.send(json);
					});

					syncAnimValues.forEach(function (value) {
						json = JSON.stringify(value);
						socket.send(json);
					});

					console.log("re-assign the rigidbody");

					allocateRigidbody();

					console.log("notify existing participants");

					obj = {
						role: SERVER,
						action: GUESTPARTICIPATION,
						seatIndex: seatIndex
					};
					json = JSON.stringify(obj);

					for (var index = 0; index < seatLength; index++) {
						var target = socketTable[index];
						if (target !== null)
							target.send(json);
					}

					console.log("notify new participants")

					for (var index = 0; index < seatLength; index++) {
						var target = socketTable[index];
						if (target !== null) {
							obj = {
								role: SERVER,
								action: GUESTPARTICIPATION,
								seatIndex: index
							};
							json = JSON.stringify(obj);

							socket.send(json);
						}
					}

					return;
				}

				// #endregion Host participation approval process
			}

			return;
		} else if (parse.action == DIVIDEGRABBER) {

			//
			// divide / combin grabber
			//

			console.log("divide obj");

			ws.clients.forEach(client => {
				if (client != socket)
					client.send(message);
			});

			return;
		} else if (parse.action == CUSTOMACTION) {

			//
			// custom action
			//

			console.log("custom message");

			ws.clients.forEach(client => {
				if (client != socket)
					client.send(message);
			});

			return;
        }
	});

	// #endregion socket on message

	// #region socket on close

	socket.on('close', function close() {
		console.log("\nclient closed " + bar);

		if (seatIndex !== -1) {
			// Notify players to leave

			var obj = {
				"role": SERVER,
				"action": GUESTDISCONNECT,
				"seatIndex": seatIndex
			};
			var json = JSON.stringify(obj);

			ws.clients.forEach(client => {
				client.send(json);
			});

			// Updating Tables

			seats[seatIndex] = false;
			socketTable[seatIndex] = null;
			grabbTable[seatIndex] = [];
			seatFilled -= 1;

			console.log(seats);
		}

		allocateRigidbody();
	});

	// #endregion socket on close
});
