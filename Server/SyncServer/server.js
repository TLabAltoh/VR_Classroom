
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

console.log("\nstart server on port: " + port + " " + bar);

// #endregion Create HTTP Server

// #region Player Data

const seatLength	= 4;
var seatFilled		= 0;
var seats			= [];
var socketTable		= [];
var grabbTable		= [];

for(var i = 0; i < seatLength; i++){
	seats.push(false);
	socketTable.push(null);
	grabbTable.push([]);
}

//#endregion Player Data

// #region Sync Objects

// list of updated transforms
var syncObjects		= {};

// List of updated animations
var syncAnims		= {};

// A dictionary of players responsible for computing rigidbody gravity per object
var rbTable			= {};

// Devide State
var syncDivides		= {};

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
const GRABBLOCK				= 6;
const FORCERELEASE			= 7;
const DIVIDEGRABBER			= 8;
const SYNCTRANSFORM			= 9;
const SYNCANIM				= 10;
const REFRESH				= 11;
const CUSTOMACTION			= 12;

// #endregion Const values

// #region Reassign rigidbody with current member ()

function allocateRigidbody(){

	console.log("re allocate rigidbody");

	// player existence check

	var check = false;
	for (var j = 0; j < seatLength; j++)
		if (seats[j] === true) check = true;

	if (check === false) return;

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
		if (seats[seatIndex] === false) continue;

		syncObjValues.forEach(function (value) {
			// https://pisuke-code.com/javascript-foreach-continue/
			// in foreach
			// continue ---> return;
			if (rbTable[value.transform.id] === undefined) return;

			// Set useGravity to Off for rigidbodies that you are not in charge of
			var obj = {
				role:		SERVER,
				action:		ALLOCATEGRAVITY,
				active:		(rbTable[value.transform.id] === seatIndex),
				transform: {
					id: value.transform.id
				}
			};
			var json = JSON.stringify(obj);
			socketTable[seatIndex].send(json);

			// 
			/*
			 * The moment I joined the room
			 * ```
				0       Cylinder.Gravity
				0       Sphere.Gravity
				0       Cube.Gravity
			 * ```
			 * since
			 * ```
				0       Cylinder.Gravity
				0       Sphere.Gravity
				0       Cube.Gravity
				undefined       OVRGuestAnchor.0.RTouch
				undefined       OVRGuestAnchor.0.LTouch
				undefined       OVRGuestAnchor.0.Head
			 * ```
			 */
			console.log(rbTable[value.transform.id] + "\t" + value.transform.id);
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
				if (client != socket) client.send(message);
			});

			return;
		} else if (parse.action == GRABBLOCK) {

			//
			// Register/unregister objects grabbed by the player in the Grabb Table
			//

			console.log("grabb lock");

			/*
				-1 : No one is grabbing
				-2 : No one grabbed, but Rigidbody does not calculate
			*/

			// Ensure that the object you are grasping does not cover
			// If someone has already grabbed the object, overwrite it

			// parse.seatIndex	: player index that is grabbing the object
			// seatIndex		: index of the socket actually communicating

			for (var i = 0; i < seatLength; i++)
				grabbTable[i] = grabbTable[i].filter(function (value) { return value.transform.id !== parse.transform.id });

			if (parse.seatIndex !== -1) grabbTable[seatIndex].push({ transform: parse.transform, message: message });

			console.log(grabbTable[seatIndex]);

			ws.clients.forEach(client => {
				if (client != socket) client.send(message);
			});

			return;
		} else if (parse.action == FORCERELEASE) {

			//
			// force release object from player
			//

			console.log("force release");

			grabbTable[seatIndex] = grabbTable[seatIndex].filter(function (value) { return value.transform.id !== parse.transform.id });

			console.log(grabbTable[seatIndex].transform);

			ws.clients.forEach(client => {
				if (client != socket) client.send(message);
			});

			return;
		} else if (parse.action == SYNCANIM) {

			//
			// sync animation
			//

			console.log("sync animation");

			syncAnims[parse.animator.id] = parse;

			ws.clients.forEach(client => {
				if (client != socket) client.send(message);
			});

			return;
		} else if (parse.action === REFRESH) {

			//
			// reflesh server
			//

			console.log("reflesh");

			if (parse.active === true) {
				// Load world transforms prior to rigidbody assignment
				var syncObjValues		= Object.values(syncObjects);
				var syncAnimValues		= Object.values(syncAnims);
				var syncDivideValues	= Object.values(syncDivides);

				// https://pisuke-code.com/javascript-dictionary-foreach/
				syncObjValues.forEach(function (value) {
					json = JSON.stringify(value);
					socket.send(json);
				});

				syncAnimValues.forEach(function (value) {
					json = JSON.stringify(value);
					socket.send(json);
				});

				syncDivideValues.forEach(function (value) {
					json = JSON.stringify(value);
					socket.send(json);
				});
            }

			// Send current grabb table

			grabbTable.forEach(function (array) {
				array.forEach(function (value) {
					socket.send(value.message);
			})});

			// Re-allocate rigidbody gravity

			allocateRigidbody();

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
						seatIndex		= i;
						seats[i]		= true;
						socketTable[i]	= socket;
						break;
					}
				}

				if (seatIndex === -1) {
					console.log("declined to participate");

					var obj = {
						role:	SERVER,
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
						role:		SERVER,
						action:		ACEPT,
						seatIndex:	seatIndex
					};
					var json = JSON.stringify(obj);
					socket.send(json);

					console.log("sending current world data");

					// Remove transforms for controllers that already exist

					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".RTouch"];
					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".LTouch"];
					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".Head"];

					// Load world transforms prior to rigidbody assignment
					var syncObjValues		= Object.values(syncObjects);
					var syncAnimValues		= Object.values(syncAnims);
					var syncDivideValues	= Object.values(syncDivides);

					// https://pisuke-code.com/javascript-dictionary-foreach/
					syncObjValues.forEach(function (value) {
						json = JSON.stringify(value);
						socket.send(json);
					});

					syncAnimValues.forEach(function (value) {
						json = JSON.stringify(value);
						socket.send(json);
					});

					syncDivideValues.forEach(function (value) {
						json = JSON.stringify(value);
						socket.send(json);
					});

					console.log("send current grabb table");

					// If you assign a rigidbody to a new participant, synchronization will start
					// before determining who is holding the object, so notify who is holding which object first.

					grabbTable.forEach(function (array) {
						array.forEach(function (value) {
							socket.send(value.message);
					})});

					console.log("re-assign the rigidbody");

					allocateRigidbody();

					console.log("notify existing participants");

					obj = {
						role:		SERVER,
						action:		GUESTPARTICIPATION,
						seatIndex:	seatIndex
					};
					json = JSON.stringify(obj);

					for (var index = 0; index < seatLength; index++) {
						if (index === seatIndex) continue;
						var target = socketTable[index];
						if (target !== null) target.send(json);
					}

					console.log("notify new participants")

					for (var index = 0; index < seatLength; index++) {
						if (index === seatIndex) continue;

						var target = socketTable[index];
						if (target !== null) {
							obj = {
								role:		SERVER,
								action:		GUESTPARTICIPATION,
								seatIndex:	index
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

					seatIndex		= 0;
					seats[0]		= true;
					socketTable[0]	= socket;

					console.log(seats);

					seatFilled += 1;

					var obj = {
						role:		SERVER,
						action:		ACEPT,
						seatIndex:	seatIndex
					};
					var json = JSON.stringify(obj);
					socket.send(json);

					console.log("sending current world data");

					// Remove transforms for controllers that already exist

					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".RTouch"];
					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".LTouch"];
					delete syncObjects["OVRGuestAnchor." + seatIndex.toString() + ".Head"];

					var syncObjValues		= Object.values(syncObjects);
					var syncAnimValues		= Object.values(syncAnims);
					var syncDivideValues	= Object.values(syncDivides);

					// https://pisuke-code.com/javascript-dictionary-foreach/
					syncObjValues.forEach(function (value) {
						json = JSON.stringify(value);
						socket.send(json);
					});

					syncAnimValues.forEach(function (value) {
						json = JSON.stringify(value);
						socket.send(json);
					});

					syncDivideValues.forEach(function (value) {
						json = JSON.stringify(value);
						socket.send(json);
					});

					console.log("send current grabb table");

					grabbTable.forEach(function (array) {
						array.forEach(function (value) {
							socket.send(value.message);
					})});

					console.log("re-assign the rigidbody");

					allocateRigidbody();

					console.log("notify existing participants");

					obj = {
						role:		SERVER,
						action:		GUESTPARTICIPATION,
						seatIndex:	seatIndex
					};
					json = JSON.stringify(obj);

					for (var index = 0; index < seatLength; index++) {
						if (index === seatIndex) continue;
						var target = socketTable[index];
						if (target !== null) target.send(json);
					}

					console.log("notify new participants")

					for (var index = 0; index < seatLength; index++) {
						if (index === seatIndex) continue;

						var target = socketTable[index];
						if (target !== null) {
							obj = {
								role:		SERVER,
								action:		GUESTPARTICIPATION,
								seatIndex:	index
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

			syncDivides[parse.transform.id] = message;

			ws.clients.forEach(client => {
				if (client != socket) client.send(message);
			});

			return;
		} else if (parse.action == CUSTOMACTION) {

			//
			// custom action
			//

			console.log("custom message");

			// (seatIndex === -1) : true�Ńu���[�h�L���X�g

			if (parse.seatIndex !== -1) {
				var target = socketTable[parse.seatIndex];
				if (target != null) target.send(message);
			} else {
				ws.clients.forEach(client => {
					if (client != socket) client.send(message);
				});
            }

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
				"role":			SERVER,
				"action":		GUESTDISCONNECT,
				"seatIndex":	seatIndex
			};
			var json = JSON.stringify(obj);

			ws.clients.forEach(client => {
				if (client !== socket) client.send(json);
			});

			// Updating Tables

			seats[seatIndex]		= false;
			socketTable[seatIndex]	= null;
			grabbTable[seatIndex]	= [];
			seatFilled -= 1;

			console.log(seats);
		}

		allocateRigidbody();
	});

	// #endregion socket on close
});
