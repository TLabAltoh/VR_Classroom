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
const { parse } = require("path");
const server = http.createServer((req, res) => {
	res.writeHead(200);
	res.write("VR_Classroom");
	res.end();
});

console.log("\ncreate server " + bar);

server.listen(port);

let ws = new websocket.Server({ server: server });

console.log("\nstart server on port: " + port + " " + bar);

////////////////////////////////////////////////////////////////////////////////////////

// #endregion Create HTTP Server

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

const OVR_GUEST_ANCHOR = "OVR_GUEST_ANCHOR";
const COMMA = ".";
const R_HAND = "R_HAND";
const L_HAND = "L_HAND";
const HEAD = "HEAD";

class unity_room {

	seatLength = 5;
	seatFilled = 0;
	seats = [];
	socketTable = [];
	grabbTable = [];
	waitApprovals = [];

	syncObjects = {};	// list of updated transforms
	syncAnims = {};		// List of updated animations
	syncDivides = {};	// Devide State
	rbObjects = {};		// List of gravity enabled objects
	rbTable = {};		// A dictionary of players responsible for computing rigidbody gravity per object

	allocateFinished = true;

	constructor() {

		for (var i = 0; i < this.seatLength; i++) {
			this.seats.push(false);
			this.socketTable.push(null);
			this.grabbTable.push([]);
		}
	};

	onMessage(userInfo, parse, message) {

		// Ensure that frequently used message types are placed on top.

		if (parse.action === SYNCTRANSFORM) {

			// summary:
			// cache object transform

			this.syncObjects[parse.transform.id] = parse;

			return;
		} else if (parse.action == GRABBLOCK) {

			// summary:
			// update lock/unlock state grabb table

			this.updateGrabbTable(parse, message);

			// broadcast
			ws.clients.forEach(client => {
				if (client != userInfo.socket) client.send(message);
			});

			return;
		} else if (parse.action == REGISTRBOBJ) {

			// summary:
			// regist rigidbody obj

			// log regist target to console
			console.log("regist rb object: " + parse.transform.id);

			// add use gravity enabled object
			this.rbObjects[parse.transform.id] = parse;

			this.allocateRigidbody(false);

			return;
		} else if (parse.action == FORCERELEASE) {

			// summary:
			// force release object from player

			// output console force release target
			console.log("force release: " + this.grabbTable[userInfo.seatIndex].transform);

			// delete from grabb table
			this.deleteObjFromGrabbTable(parse);

			// broadcast
			ws.clients.forEach(client => {
				if (client != userInfo.socket) client.send(message);
			});

			return;
		} else if (parse.action == SYNCANIM) {

			// summary:
			// sync animation

			console.log("sync animation");

			this.syncAnims[parse.animator.id] = parse;

			ws.clients.forEach(client => {
				if (client != userInfo.socket) client.send(message);
			});

			return;
		} else if (parse.action == CLEARTRANSFORM) {

			// summary:
			// clear transform cache and devide state hash

			console.log("delete transform cache");

			delete this.syncObjects[parse.transform.id];
			delete this.syncDivides[parse.transform.id];

			return;
		} else if (parse.action == CLEARANIM) {

			// summary:
			// clear anim state cache

			console.log("delete anim cache");

			delete this.syncAnims[parse.transform.id];

			return;
		} else if (parse.action === REFRESH) {

			// summary:
			// reflesh all sync component

			console.log("reflesh all sync component");

			if (parse.active === true) this.sendCurrentWoldData(userInfo.socket);

			this.sendCurrentGrabbState(userInfo.socket);

			this.allocateRigidbody(false);

			return;
		} else if (parse.action == UNIREFRESHTRANSFORM) {

			// summary:
			// reflesh object

			console.log("reflesh object: " + parse.transform.id);

			this.sendSpecificTransform(parse.transform.id, userInfo.socket);

			this.allocateRigidbody(false);

			return;
		} else if (parse.action == UNIREFRESHANIM) {

			// summary:
			// reflesh anim

			console.log("reflesh anim: " + parse.animator.id);

			this.sendSpecificAnim(parse.animator.id, userInfo.socket);

			return;
		} else if (parse.action == DIVIDEGRABBER) {

			// summary:
			// divide / combin grabber

			// output divide target
			console.log("divide obj: " + parse.transform.id);

			// update table with divide state
			this.syncDivides[parse.transform.id] = parse;

			// broadcast
			ws.clients.forEach(client => {
				if (client != userInfo.socket) client.send(message);
			});

			return;
		} else if (parse.action == CUSTOMACTION) {
			// summary:
			// custom action

			// output received custom message
			console.log("custom message: " + message);

			// relay packet
			if (parse.dstIndex !== -1) {
				// -1 is unicast
				var target = this.socketTable[parse.dstIndex];
				if (target != null) {
					target.send(message);
                }
			} else {
				// boradcast
				ws.clients.forEach(client => {
					if (client != userInfo.socket) client.send(message);
				});
			}
			return;
		} else if (parse.action === REGIST) {
			// summary:
			// regist client to seat table

			console.log("\nreceived a request to join " + bar);

			this.waitApprovals.push(() => {
				console.log("\napproval for client start" + bar);

				// check befor acept
				if (parse.role === GUEST) {
					// Guest Index : 1 ~ seatLength - 1
					for (var i = 1; i < this.seatLength; i++) {
						if (this.seats[i] === false) {
							userInfo.seatIndex = i;
							this.seats[i] = true;
							this.socketTable[i] = userInfo.socket;
							break;
						}
					}
				} else if (parse.role === HOST) {
					// Host Index: 0
					if (this.seats[0] === false) {
						userInfo.seatIndex = 0;
						this.seats[0] = true;
						this.socketTable[0] = userInfo.socket;
					}
				}

				// send socket to result
				if (userInfo.seatIndex === -1) {
					console.log("declined to participate");
					var obj = {
						role: SERVER,
						action: REGECT,
						srcIndex: -1,
						dstIndex: userInfo.seatIndex
					};
					var json = JSON.stringify(obj);
					userInfo.socket.send(json);
				} else {
					this.onJoined(userInfo);
				}
			});

			setTimeout(this.approvalToParticipate.bind(this), 2 * 1000);
			return;
		} else if (parse.action == EXIT) {

			// summary:
			// exit from room

			console.log("\nclient exit " + bar);

			if (seatIndex !== -1) {
				// Notify players to leave
				var obj = {
					role: SERVER,
					action: GUESTDISCONNECT,
					srcIndex: seatIndex,
					dstIndex: -1
				};
				var json = JSON.stringify(obj);

				ws.clients.forEach(client => {
					if (client !== userInfo.socket) client.send(json);
				});

				// Updating Tables
				this.seats[userInfo.seatIndex] = false;
				this.socketTable[userInfo.seatIndex] = null;
				this.grabbTable[userInfo.seatIndex] = [];
				this.seatFilled -= 1;

				console.log(seats);

				this.allocateRigidbody(true);

				userInfo.seatIndex = -1;
			}
			return;
		}
    }

	onClose(userInfo) {
		if (userInfo.seatIndex !== -1) {
			// Notify players to leave
			var obj = {
				role: SERVER,
				action: GUESTDISCONNECT,
				srcIndex: userInfo.seatIndex,
				sdtIndex: -1
			};
			var json = JSON.stringify(obj);

			ws.clients.forEach(client => {
				if (client !== userInfo.socket) client.send(json);
			});

			// Updating Tables
			this.seats[userInfo.seatIndex] = false;
			this.socketTable[userInfo.seatIndex] = null;
			this.grabbTable[userInfo.seatIndex] = [];
			this.seatFilled -= 1;

			console.log(this.seats);

			this.allocateRigidbody(true);

			userInfo.seatIndex = -1;
		}
		return;
	}

	approvalToParticipate() {

		// summary:
		// Digesting the accumulated acceptance process

		console.log("try approval to participate ....");

		// 1. waiting for allocateRigidbody to execute
		// 2. waitApprovals is empty
		if (this.allocateFinished === false || this.waitApprovals.length == 0) {
			setTimeout(this.approvalToParticipate.bind(this), 2 * 1000);
			return;
		}

		// deque from wait approvals
		var approval = this.waitApprovals.shift();

		// invoke
		approval();
	}

	allocateRigidbodyTask() {

		// summary:
		// 

		console.log("re allocate rigidbody");

		// player existence check
		var check = false;
		for (var j = 0; j < this.seatLength; j++) {
			if (this.seats[j] === true) {
				check = true;
			}
        }
		if (check === false) {
			return;
        }

		// Recalculate rigidbody allocation table
		var rbObjValues = Object.values(this.rbObjects);
		var seatIndex = 0;
		rbObjValues.forEach(function (value) {
			while (true) {
				// Check if someone is in the seat
				if (this.seats[seatIndex] === true) {
					// When an object is held by someone else, the player holding the object continues to be in charge of the physics processing.
					for (var i = 0; i < this.seatLength; i++)
					for (var j = 0; j < this.grabbTable[i].length; j++) {
						if (this.grabbTable[i][j].transform.id === value.transform.id) {
							// this.rbTable[value.transform.id] = seatIndex;	// <--- It should be assigned by grabb lock and does not need to be executed
							seatIndex = (seatIndex + 1) % this.seatLength;
							return;
						}
					}

					// If no one has grabbed this object, let this player compute the physics of this object.
					this.rbTable[value.transform.id] = seatIndex;
					seatIndex = (seatIndex + 1) % this.seatLength;
					return;
				} else {
					seatIndex = (seatIndex + 1) % this.seatLength;
					continue;
				}
			}
		}.bind(this));

		// Re-allocate with updated tables
		for (seatIndex = 0; seatIndex < this.seatLength; seatIndex++) {
			// Check if someone is in the seat
			if (this.seats[seatIndex] === false) {
				continue;
            }

			rbObjValues.forEach(function (value) {
				// https://pisuke-code.com/javascript-foreach-continue/
				// in foreach
				// continue ---> return;
				if (this.rbTable[value.transform.id] === undefined) {
					return;
                }

				// Set useGravity to Off for rigidbodies that you are not in charge of
				var obj = {
					role: SERVER,
					srcIndex: -1,
					dstIndex: seatIndex,
					action: ALLOCATEGRAVITY,
					active: (this.rbTable[value.transform.id] === seatIndex),
					transform: { id: value.transform.id }
				};
				var json = JSON.stringify(obj);
				this.socketTable[seatIndex].send(json);

				console.log(
					"seat " + seatIndex + ": " + value.transform.id +
					", \tgrabb: " + this.grabbTable[value.transform.id] +
					", \t" + ((this.rbTable[value.transform.id] === seatIndex) ? "allocated" : "not allocated"));
			}.bind(this));
		}
	}

	allocateRigidbody(isImidiate) {

		// allocateFinished -> false:
		// wait for allocateRigidbodyTask() invoked

		if (isImidiate === true) {
			this.allocateFinished = false;
			this.allocateRigidbodyTask();
			this.allocateFinished = true;
			return;
		}

		if (this.allocateFinished == false) {
			return;
        }

		this.allocateFinished = false;

		setTimeout(function () {
			this.allocateRigidbodyTask();
			this.allocateFinished = true;
		}.bind(this), 5 * 1000);
	}

	deleteObjFromGrabbTable(target) {

		// summary:
		// delete target obj from grabb table

		for (var i = 0; i < this.seatLength; i++) {
			this.grabbTable[i] = this.grabbTable[i].filter(function (value) {
				return value.transform.id !== target.transform.id
			});
        }
	}

	updateGrabbTable(parse, message) {

		/*
			-1 : No one is grabbing
			-2 : No one grabbed, but Rigidbody does not calculate
		*/

		// summary:
		// Ensure that the object you are grasping does not cover
		// If someone has already grabbed the object, overwrite it

		// target.seatIndex	: player index that is grabbing the object
		// seatIndex		: index of the socket actually communicating

		this.deleteObjFromGrabbTable(parse);

		var grabIndex = parse.grabIndex;
		var transform = parse.transform;

		// output console grabb lock state and push to grabb table
		if (parse.grabIndex !== -1) {
			console.log("grabb lock: " + transform.id + ", grabindex: " + grabIndex);

			// If the grabbed object has gravity enabled, the player who grabbed it is authorized to calculate gravity.
			if (transform.id in this.rbObjects) {

				this.rbTable[transform.id] = grabIndex;

				for (var i = 0; i < this.seatLength; i++) {

					if (this.seats[i] === false) {
						continue;
					}

					var obj = {
						role: SERVER,
						srcIndex: -1,
						dstIndex: -1,
						action: ALLOCATEGRAVITY,
						active: (this.rbTable[transform.id] === i),
						transform: { id: transform.id }
					};
					var json = JSON.stringify(obj);
					this.socketTable[i].send(json);
					console.log("seat " + i + " . " + this.rbTable[transform.id] + "\t" + transform.id);
				}
			}

			// push target obj to grabb table
			this.grabbTable[grabIndex].push({ transform: transform, message: message });
		} else {
			console.log("grabb unlock: " + transform);
        }

	}

	sendCurrentWoldData(socket) {

		// Load world transforms prior to rigidbody assignment
		var syncObjValues = Object.values(this.syncObjects);
		var syncAnimValues = Object.values(this.syncAnims);
		var syncDivideValues = Object.values(this.syncDivides);

		// send cached object transform
		// https://pisuke-code.com/javascript-dictionary-foreach/
		syncObjValues.forEach(function (value) {
			var json = JSON.stringify(value);
			socket.send(json);
		}.bind(this));

		// send cached animatoin state
		syncAnimValues.forEach(function (value) {
			var json = JSON.stringify(value);
			socket.send(json);
		}.bind(this));

		// send cached divide state
		syncDivideValues.forEach(function (value) {
			var json = JSON.stringify(value);
			socket.send(json);
		}.bind(this));
	}

	sendSpecificTransform(id, socket) {

		// Transform
		if (id in this.syncObjects) {
			var parse = this.syncObjects[id];
			var json = JSON.stringify(parse);
			socket.send(json);
		}

		// Divide State
		if (id in this.syncDivides) {
			var parse = this.syncDivides[id];
			var json = JSON.stringify(parse);
			socket.send(json);
		}
	}

	sendSpecificAnim(id, socket) {

		// Animator
		if (id in this.syncAnims) {
			var parse = this.syncAnims[id];
			var json = JSON.stringify(parse);
			socket.send(json);
		}
	}

	sendCurrentGrabbState(socket) {

		// get grabb object list from each paticipante
		this.grabbTable.forEach(function (array) {

			// for each item in grabb list
			array.forEach(function (value) {

				// send grabb message to target
				socket.send(value.message);

				// log console grabb message send
				if (value != [] && value != undefined && value != null) {
					console.log("send grabbed item: " + value.message);
                }
			}.bind(this))
		}.bind(this));
	}

	onJoined(userInfo) {

		// output console to current seat table
		console.log("approved to participate");
		console.log(this.seats);

		// count up filled seat count
		this.seatFilled += 1;

		// send acept message
		var obj = {
			role: SERVER,
			action: ACEPT,
			srcIndex: -1,
			dstIndex: userInfo.seatIndex
		};
		var json = JSON.stringify(obj);
		userInfo.socket.send(json);

		console.log("sending current world data");

		// Remove transforms for player body tracker that already exist
		var commonString = OVR_GUEST_ANCHOR + COMMA + userInfo.seatIndex.toString() + COMMA;
		delete this.syncObjects[commonString + R_HAND];
		delete this.syncObjects[commonString + L_HAND];
		delete this.syncObjects[commonString + HEAD];

		this.sendCurrentWoldData(userInfo.socket);

		console.log("send current grabb table");

		// If you assign a rigidbody to a new participant, synchronization will start
		// before determining who is holding the object, so notify who is holding which object first.

		this.sendCurrentGrabbState(userInfo.socket);

		console.log("notify existing participants");

		obj = {
			role: SERVER,
			action: GUESTPARTICIPATION,
			srcIndex: userInfo.seatIndex,
			dstIndex: -1
		};
		json = JSON.stringify(obj);

		for (var index = 0; index < this.seatLength; index++) {
			if (index === userInfo.seatIndex) continue;
			var dstSocket = this.socketTable[index];
			if (dstSocket !== null) dstSocket.send(json);
		}

		console.log("notify new participants")

		for (var index = 0; index < this.seatLength; index++) {
			if (index === userInfo.seatIndex) continue;

			var exists = this.socketTable[index];
			if (exists !== null) {
				obj = {
					role: SERVER,
					action: GUESTPARTICIPATION,
					srcIndex: index,
					dstIndex: userInfo.seatIndex
				};
				json = JSON.stringify(obj);

				userInfo.socket.send(json);
			}
		}
	}
}

var rooms = {};
var roomIDs = {};

ws.on("connection", function (socket) {
	console.log("\nclient connected " + bar);

	var userInfo = { seatIndex: -1, socket: socket };

	socket.on("message", function (data, isBinary) {

		const message = isBinary ? data : data.toString();

		const parse = JSON.parse(message);

		var room = rooms[parse.roomID];

		if (room === undefined || room === null) {
			console.log("create room: " + parse.roomID);
			room = new unity_room();
			rooms[parse.roomID] = room;
		}

		if (!(socket in roomIDs)){
			console.log("enter hash table legnth: " + Object.keys(roomIDs).length);
			roomIDs[socket] = parse.roomID;
		}

		room.onMessage(userInfo, parse, message);
	});

	socket.on('close', function close() {
		console.log("\nclient closed " + bar);

		var roomID = roomIDs[socket];

		var room = rooms[roomID];

		if (room !== undefined && room !== null) {
			room.onClose(userInfo);
			delete roomIDs[socket];
		}

		console.log("exit hash table legnth: " + Object.keys(roomIDs).length);
	});
});
