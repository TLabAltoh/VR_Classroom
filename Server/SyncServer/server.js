const websocket = require("ws");

const port = 5000;
const bar = "----------------";

// #region Create HTTPS Server

////////////////////////////////////////////////////////////////////////////////////////

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

////////////////////////////////////////////////////////////////////////////////////////

// #endregion Create HTTPS Server

// #region Create HTTP Server

////////////////////////////////////////////////////////////////////////////////////////

const http = require("http");
const server = http.createServer((req, res) => {
	res.writeHead(200);
	res.write("VR_Kensyu");
	res.end();
});

console.log("\ncreate server " + bar);

server.listen(port);

let ws = new websocket.Server({ server: server });

console.log("\nstart server on port: " + port + " " + bar);

////////////////////////////////////////////////////////////////////////////////////////

// #endregion Create HTTP Server

// #region Player Data

////////////////////////////////////////////////////////////////////////////////////////

const seatLength = 5;
var seatFilled = 0;
var seats = [];
var socketTable = [];
var grabbTable = [];
var waitApprovals = [];

for (var i = 0; i < seatLength; i++) {
	seats.push(false);
	socketTable.push(null);
	grabbTable.push([]);
}

//#endregion Player Data

// #region Sync Objects

////////////////////////////////////////////////////////////////////////////////////////

// list of updated transforms
var syncObjects = {};

// List of updated animations
var syncAnims = {};

// Devide State
var syncDivides = {};

// List of gravity enabled objects
var rbObjects = {};

// A dictionary of players responsible for computing rigidbody gravity per object
var rbTable = {};

////////////////////////////////////////////////////////////////////////////////////////

// #endregion Sync Objects

// #region Const values

////////////////////////////////////////////////////////////////////////////////////////

const SERVER = 0;
const HOST = 1;
const GUEST = 2;

const REGIST = 0;
const REGECT = 1;
const ACEPT = 2;
const EXIT = 3;
const GUESTDISCONNECT = 4;
const GUESTPARTICIPATION = 5;
const ALLOCATEGRAVITY = 6;
const REGISTRBOBJ = 7;
const GRABBLOCK = 8;
const FORCERELEASE = 9;
const DIVIDEGRABBER = 10;
const SYNCTRANSFORM = 11;
const SYNCANIM = 12;
const CLEARTRANSFORM = 13;
const CLEARANIM = 14;
const REFRESH = 15;
const UNIREFRESHTRANSFORM = 16;
const UNIREFRESHANIM = 17;
const CUSTOMACTION = 18;

////////////////////////////////////////////////////////////////////////////////////////

// #endregion Const values

////////////////////////////////////////////////////////////////////////////////////////

var allocateFinished = true;

function allocateRigidbodyTask() {

	console.log("re allocate rigidbody");

	//
	// player existence check
	//

	var check = false;
	for (var j = 0; j < seatLength; j++)
		if (seats[j] === true) check = true;

	if (check === false) return;

	//
	// Recalculate rigidbody allocation table
	//

	var rbObjValues = Object.values(rbObjects);
	var seatIndex = 0;

	rbObjValues.forEach(function (value) {
		while (true) {
			// Check if someone is in the seat
			if (seats[seatIndex] === true) {
				// When an object is held by someone else, the player holding the object continues to be in charge of the physics processing.
				for (var i = 0; i < seatLength; i++) {
					for (var j = 0; j < grabbTable[i].length; j++) {
						if (grabbTable[i][j].transform.id === value.transform.id) {
							seatIndex = (seatIndex + 1) % seatLength;
							break;
						}
                    }
                }

				// If no one has grabbed this object, let this player compute the physics of this object.
				rbTable[value.transform.id] = seatIndex;
				seatIndex = (seatIndex + 1) % seatLength;
				break;
			} else {
				seatIndex = (seatIndex + 1) % seatLength;
				continue;
			}
		}
	});

	//
	// Re-allocate with updated tables
	//

	for (seatIndex = 0; seatIndex < seatLength; seatIndex++) {
		// Check if someone is in the seat
		if (seats[seatIndex] === false) continue;

		rbObjValues.forEach(function (value) {
			// https://pisuke-code.com/javascript-foreach-continue/
			// in foreach
			// continue ---> return;
			if (rbTable[value.transform.id] === undefined) return;

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
			console.log("seat " + seatIndex + " . " + value.transform.id + "\t" + ((rbTable[value.transform.id] === seatIndex) ? "allocated" : "not allocated"));
		});
	}
}

function allocateRigidbody(isImidiate) {

	// allocateFinished -> false:
	// wait for allocateRigidbodyTask() invoked

	if (isImidiate === true) {
		allocateFinished = false;
		allocateRigidbodyTask();
		allocateFinished = true;
		return;
    }

	if (allocateFinished == false) return;

	allocateFinished = false;

	setTimeout(() => {
		allocateRigidbodyTask();
		allocateFinished = true;
	}, 5 * 1000);
}

////////////////////////////////////////////////////////////////////////////////////////

function deleteObjFromGrabbTable(target) {
	// delete target obj from grabb table
	for (var i = 0; i < seatLength; i++)
		grabbTable[i] = grabbTable[i].filter(function (value) { return value.transform.id !== target.transform.id });
}

////////////////////////////////////////////////////////////////////////////////////////

function updateGrabbTable(seatIndex, target, message) {

	/*
		-1 : No one is grabbing
		-2 : No one grabbed, but Rigidbody does not calculate
	*/

	// Ensure that the object you are grasping does not cover
	// If someone has already grabbed the object, overwrite it

	// target.seatIndex	: player index that is grabbing the object
	// seatIndex		: index of the socket actually communicating

	deleteObjFromGrabbTable(target);

	// output console grabb lock state and push to grabb table
	if (target.seatIndex !== -1) {
		console.log("grabb lock: " + target.transform);

		//
		// If the grabbed object has gravity enabled, the player who grabbed it is authorized to calculate gravity.
		//

		if (target.transform.id in rbTable) {

			rbTable[target.transform.id] = seatIndex;

			for (i = 0; i < seatLength; i++) {

				if (seats[i] === false) continue;

				var obj = {
					role: SERVER,
					action: ALLOCATEGRAVITY,
					active: (rbTable[target.transform.id] === i),
					transform: {
						id: target.transform.id
					}
				};
				var json = JSON.stringify(obj);
				socketTable[i].send(json);
				console.log("seat " + i + " . " + rbTable[target.transform.id] + "\t" + target.transform.id);
			}
        }

		//
		// push target obj to grabb table
		//

		grabbTable[seatIndex].push({ transform: target.transform, message: message });
	} else
		console.log("grabb unlock: " + target.transform);
}

////////////////////////////////////////////////////////////////////////////////////////

function sendCurrentWoldData(socket) {

	// Load world transforms prior to rigidbody assignment
	var syncObjValues = Object.values(syncObjects);
	var syncAnimValues = Object.values(syncAnims);
	var syncDivideValues = Object.values(syncDivides);

	// send cached object transform
	// https://pisuke-code.com/javascript-dictionary-foreach/
	syncObjValues.forEach(function (value) {
		json = JSON.stringify(value);
		socket.send(json);
	});

	// send cached animatoin state
	syncAnimValues.forEach(function (value) {
		json = JSON.stringify(value);
		socket.send(json);
	});

	// send cached divide state
	syncDivideValues.forEach(function (value) {
		json = JSON.stringify(value);
		socket.send(json);
	});
}

function sendSpecificTransform(target, socket) {

	// Transform
	if (target in syncObjects) {
		target = syncObjects[target];
		var json = JSON.stringify(target);
		socket.send(json);
	}

	// Divide State
	if (target in syncDivides) {
		target = syncDivides[target];
		var json = JSON.stringify(target);
		socket.send(json);
    }
}

function sendSpecificAnim(target, socket) {
	// Animator
	if (target in syncAnims) {
		target = syncAnims[target];
		var json = JSON.stringify(target);
		socket.send(json);
	}
}

function sendCurrentGrabbState(socket) {

	// get grabb object list from each paticipante
	grabbTable.forEach(function (array) {

		// for each item in grabb list
		array.forEach(function (value) {

			// send grabb message to target
			socket.send(value.message);

			// log console grabb message send
			if (value != [] && value != undefined && value != null)
				console.log("send grabbed item: " + value.message);
		})
	});
}

function onJoined(seatIndex, socket) {

	// output console to current seat table
	console.log("approved to participate");
	console.log(seats);

	// count up filled seat count
	seatFilled += 1;

	// send acept message
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

	sendCurrentWoldData(socket);

	console.log("send current grabb table");

	// If you assign a rigidbody to a new participant, synchronization will start
	// before determining who is holding the object, so notify who is holding which object first.

	sendCurrentGrabbState(socket);

	console.log("notify existing participants");

	obj = {
		role: SERVER,
		action: GUESTPARTICIPATION,
		seatIndex: seatIndex
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

////////////////////////////////////////////////////////////////////////////////////////

// start interval task
setTimeout(approvalToParticipate, 2 * 1000);

function approvalToParticipate() {

	console.log("approval to participate loop ....");

	// 1. waiting for allocateRigidbody to execute
	// 2. waitApprovals is empty
	if (allocateFinished === false || waitApprovals.length == 0) {
		setTimeout(approvalToParticipate, 2 * 1000);
		return;
    }

	// deque from wait approvals
	var approval = waitApprovals.shift();

	// invoke
	approval();

	// next loop after 2 secound
	setTimeout(approvalToParticipate, 2 * 1000);
}

////////////////////////////////////////////////////////////////////////////////////////

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

			// #region
			syncObjects[parse.transform.id] = parse;

			return;
			// #endregion
		} else if (parse.action == GRABBLOCK) {

			//
			// update lock/unlock state grabb table
			//

			// #region
			updateGrabbTable(seatIndex, parse, message);

			// broadcast
			ws.clients.forEach(client => {
				if (client != socket) client.send(message);
			});

			return;
			// #endregion
		} else if (parse.action == REGISTRBOBJ) {

			//
			// regist rigidbody obj
			//

			// #region

			// log regist target to console
			console.log("regist rb object: " + parse.transform.id);

			// add use gravity enabled object
			rbObjects[parse.transform.id] = parse;

			allocateRigidbody(false);

			return;
			// #endregion
		} else if (parse.action == FORCERELEASE) {

			//
			// force release object from player
			//

			// #region

			// output console force release target
			console.log("force release: " + grabbTable[seatIndex].transform);

			// delete from grabb table
			deleteObjFromGrabbTable(parse);

			// broadcast
			ws.clients.forEach(client => {
				if (client != socket) client.send(message);
			});

			return;
			// #endregion
		} else if (parse.action == SYNCANIM) {

			//
			// sync animation
			//

			// #region
			console.log("sync animation");

			syncAnims[parse.animator.id] = parse;

			ws.clients.forEach(client => {
				if (client != socket) client.send(message);
			});

			return;
			// #endregion
		} else if (parse.action == CLEARTRANSFORM) {

			//
			// clear transform cache and devide state hash
			//

			// #region
			console.log("delete transform cache");

			delete syncObjects[parse.transform.id];
			delete syncDivides[parse.transform.id];

			return;
			// #endregion
		} else if (parse.action == CLEARANIM) {

			//
			// clear anim state cache
			//

			// #region
			console.log("delete anim cache");

			delete syncAnims[parse.transform.id];

			return;
			// #endregion
		} else if (parse.action === REFRESH) {

			//
			// reflesh all sync component
			//

			// #region
			console.log("reflesh all sync component");

			if (parse.active === true) sendCurrentWoldData(socket);

			sendCurrentGrabbState(socket);

			allocateRigidbody(false);

			return;
			// #endregion
		} else if (parse.action == UNIREFRESHTRANSFORM) {

			//
			// reflesh object
			//

			// #region
			console.log("reflesh object: " + parse.transform.id);

			sendSpecificTransform(parse.transform.id, socket);

			allocateRigidbody(false);

			return;
			// #endregion
		} else if (parse.action == UNIREFRESHANIM) {

			//
			// reflesh anim
			//

			// #region
			console.log("reflesh anim: " + parse.animator.id);

			sendSpecificAnim(parse.animator.id, socket);

			return;
			// #endregion
		} else if (parse.action == DIVIDEGRABBER) {

			//
			// divide / combin grabber
			//

			// #region

			// output divide target
			console.log("divide obj: " + parse.transform.id);

			// update table with divide state
			syncDivides[parse.transform.id] = parse;

			// broadcast
			ws.clients.forEach(client => {
				if (client != socket) client.send(message);
			});

			return;
			// #endregion
		} else if (parse.action == CUSTOMACTION) {

			//
			// custom action
			//

			// #region

			// output received custom message
			console.log("custom message: " + message);

			// relay packet
			if (parse.seatIndex !== -1) {
				// -1 is unicast
				var target = socketTable[parse.seatIndex];
				if (target != null) target.send(message);
			} else {
				// boradcast
				ws.clients.forEach(client => {
					if (client != socket) client.send(message);
				});
			}

			return;
			// #endregion
		} else if (parse.action === REGIST) {

			//
			// regist client to seat table
			//

			// #region
			console.log("\nreceived a request to join " + bar);

			waitApprovals.push(() => {
				console.log("\napproval for client start" + bar);

				// check befor acept
				if (parse.role === GUEST) {
					// Guest table : 1 ~ seatLength - 1
					for (var i = 1; i < seatLength; i++) {
						if (seats[i] === false) {
							seatIndex = i;
							seats[i] = true;
							socketTable[i] = socket;
							break;
						}
					}

				} else if (parse.role === HOST) {
					if (seats[0] === false) {
						seatIndex = 0;
						seats[0] = true;
						socketTable[0] = socket;
					}
				}

				// send socket to result
				if (seatIndex === -1) {
					console.log("declined to participate");
					var obj = {
						role: SERVER,
						action: REGECT
					};
					var json = JSON.stringify(obj);
					socket.send(json);
				} else
					onJoined(seatIndex, socket);
			});
			return;
			// #endregion
		} else if (parse.action == EXIT) {

			//
			// exit from room
			//

			// #region
			console.log("\nclient exit " + bar);

			if (seatIndex !== -1) {
				// Notify players to leave
				var obj = {
					"role": SERVER,
					"action": GUESTDISCONNECT,
					"seatIndex": seatIndex
				};
				var json = JSON.stringify(obj);

				ws.clients.forEach(client => {
					if (client !== socket) client.send(json);
				});

				// Updating Tables
				seats[seatIndex] = false;
				socketTable[seatIndex] = null;
				grabbTable[seatIndex] = [];
				seatFilled -= 1;

				console.log(seats);

				allocateRigidbody(true);

				seatIndex = -1;
			}

			return;
			// #endregion
		}
	});

	// #endregion socket on message

	// #region socket on close

	socket.on('close', function close() {
		console.log("\nclient closed " + bar);

		//
		// exit from room
		//

		// #region
		if (seatIndex !== -1) {
			// Notify players to leave
			var obj = {
				"role": SERVER,
				"action": GUESTDISCONNECT,
				"seatIndex": seatIndex
			};
			var json = JSON.stringify(obj);

			ws.clients.forEach(client => {
				if (client !== socket) client.send(json);
			});

			// Updating Tables
			seats[seatIndex] = false;
			socketTable[seatIndex] = null;
			grabbTable[seatIndex] = [];
			seatFilled -= 1;

			console.log(seats);

			allocateRigidbody(true);

			seatIndex = -1;
		}

		return;
		// #endregion
	});

	// #endregion socket on close
});
