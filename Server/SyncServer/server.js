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

const BROAD_CAST = -1;
const FREE = -1;
const FIXED  = -2;

const REGIST = 0;
const REGECT = 1;
const ACEPT = 2;
const EXIT = 3;
const GUEST_DISCONNECT = 4;
const GUEST_PARTICIPATION = 5;
const ALLOCATE_GRAVITY = 6;
const REGIST_RB_OBJ = 7;
const GRABB_LOCK = 8;
const FORCE_RELEASE = 9;
const DIVIDE_GRABBER = 10;
const SYNC_TRANSFORM = 11;
const SYNC_ANIM = 12;
const CLEAR_TRANSFORM = 13;
const CLEAR_ANIM = 14;
const REFRESH = 15;
const UNI_REFRESH_TRANSFORM = 16;
const UNI_REFRESH_ANIM = 17;
const CUSTOM_ACTION = 18;

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
	grabbTable = [];		// Table of objects directly grabbed by the player
	simpleLockTable = [];	// Objects that are not in anyone's grasp but are protected by exclusion controls
	waitApprovals = [];

	syncObjects = {};	// List of updated transforms
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

		/**
		 * Ensure that frequently used message types are placed on top.
		 */

		if (parse.action === SYNC_TRANSFORM) {

			/**
			 * summary:
			 * cache object transform
			 */

			this.syncObjects[parse.transform.id] = parse;

			return;
		} else if (parse.action == GRABB_LOCK) {

			/**
			 * summary:
			 * update lock/unlock state grabb table
			 */

			this.updateGrabbTable(parse, message);

			ws.clients.forEach(client => {	// broadcast
				if (client != userInfo.socket) client.send(message);
			});

			return;
		} else if (parse.action == REGIST_RB_OBJ) {

			console.log("[log] registration of rigidbody id ", parse.transform.id, " with the server has been requested.");

			this.rbObjects[parse.transform.id] = parse;	// add use gravity enabled object

			this.allocateRigidbody(false);

			return;
		} else if (parse.action == FORCE_RELEASE) {

			console.log("[log] a forced release of object id ", parse.transform.id, " has been requested.");

			this.deleteObjFromGrabbTable(parse);	// delete from grabb table

			ws.clients.forEach(client => {	// broadcast
				if (client != userInfo.socket) {
					client.send(message);
				}
			});

			return;
		} else if (parse.action == SYNC_ANIM) {

			console.log("[log] animation id ", parse.animator.id, " has been synchronized from the client to the server.");

			this.syncAnims[parse.animator.id] = parse;

			ws.clients.forEach(client => {
				if (client != userInfo.socket) {
					client.send(message);
				}
			});

			return;
		} else if (parse.action == CLEAR_TRANSFORM) {

			console.log("[log] deletion of the cache for transform id ", parse.transform.id, " has been requested.");

			delete this.syncObjects[parse.transform.id];
			delete this.syncDivides[parse.transform.id];

			return;
		} else if (parse.action == CLEAR_ANIM) {

			console.log("[log] deletion of the cache for animation id ", parse.animator.id, " has been requested.");

			delete this.syncAnims[parse.animator.id];

			return;
		} else if (parse.action === REFRESH) {

			console.log("[log] a request has been made to synchronize the world information cached by the server.");

			if (parse.active === true) {
				this.sendCurrentWoldData(userInfo.socket);
			}

			this.sendCurrentGrabbState(userInfo.socket);

			this.allocateRigidbody(false);

			return;
		} else if (parse.action == UNI_REFRESH_TRANSFORM) {

			console.log("[log] synchronization of transform id ", parse.transform.id, " is requested.");

			this.sendSpecificTransform(parse.transform.id, userInfo.socket);

			this.allocateRigidbody(false);

			return;
		} else if (parse.action == UNI_REFRESH_ANIM) {

			console.log("[log] synchronization of animation id ", parse.animator.id, " is requested.");

			this.sendSpecificAnim(parse.animator.id, userInfo.socket);

			return;
		} else if (parse.action == DIVIDE_GRABBER) {

			console.log("[log] the object has been divided or combined: ", parse.transform.id);	// output divide target

			this.syncDivides[parse.transform.id] = parse;	// update table with divide state

			ws.clients.forEach(client => {	// broadcast
				if (client != userInfo.socket) {
					client.send(message);
				}
			});

			return;
		} else if (parse.action == CUSTOM_ACTION) {
			/**
			 * summary:
			 * custom action
			 */

			/**
			 * output received custom message
			 */
			console.log("[log] custom message: ", message);

			/**
			 * relay packet
			 */
			if (parse.dstIndex !== BROAD_CAST) {	// -1 is unicast
				var target = this.socketTable[parse.dstIndex];
				if (target != null) {
					target.send(message);
                }
			} else {	// boradcast
				ws.clients.forEach(client => {
					if (client != userInfo.socket) {
						client.send(message);
					}
				});
			}
			return;
		} else if (parse.action === REGIST) {

			/**
			 * summary:
			 * regist client to seat table
			 */

			console.log("[log] received a request to join.");

			this.waitApprovals.push(() => {
				console.log("[action] approval for client start.");

				/**
				 * check befor acept
				 */
				if (parse.role === GUEST) {	// Guest Index : 1 ~ seatLength - 1
					for (var i = 1; i < this.seatLength; i++) {
						if (this.seats[i] === false) {
							userInfo.seatIndex = i;
							this.seats[i] = true;
							this.socketTable[i] = userInfo.socket;
							break;
						}
					}
				} else if (parse.role === HOST) {	// Host Index: 0
					if (this.seats[0] === false) {
						userInfo.seatIndex = 0;
						this.seats[0] = true;
						this.socketTable[0] = userInfo.socket;
					}
				}

				if (userInfo.seatIndex === -1) {	// send socket to result
					console.log("[error] declined to participate.");
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

			/**
			 * upon receiving an exit notification from a client, 
			 * the system notifies other clients and deletes information about the client on the server.
			 */

			console.log("[log] client exit.");

			if (seatIndex !== -1) {	
				var obj = {	// notify players to leave
					role: SERVER,
					action: GUEST_DISCONNECT,
					srcIndex: seatIndex,
					dstIndex: -1
				};
				var json = JSON.stringify(obj);

				ws.clients.forEach(client => {
					if (client !== userInfo.socket){
						client.send(json);
					}
				});

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
			var obj = {	// Notify players to leave
				role: SERVER,
				action: GUEST_DISCONNECT,
				srcIndex: userInfo.seatIndex,
				dstIndex: -1
			};
			var json = JSON.stringify(obj);

			ws.clients.forEach(client => {
				if (client !== userInfo.socket) {
					client.send(json);
				}
			});

			this.seats[userInfo.seatIndex] = false;	// Updating Tables
			this.socketTable[userInfo.seatIndex] = null;
			this.grabbTable[userInfo.seatIndex] = [];
			this.seatFilled -= 1;

			console.log("[log] current sheet table: " + this.seats);

			this.allocateRigidbody(true);

			userInfo.seatIndex = -1;
		}
		return;
	}

	/**
	 * Digesting the accumulated acceptance process
	 * @returns 
	 */
	approvalToParticipate() {

		console.log("[action] try approval to participate ....");

		/**
		 * When the following conditions are satisfied, the process is interrupted and waits until the next timeout
		 * 1. waiting for allocateRigidbody to execute
		 * 2. waitApprovals is empty
		 */
		if (this.allocateFinished === false || this.waitApprovals.length == 0) {
			console.log("[log] the program was not in a runnable state. we will try again in the next loop.");
			setTimeout(this.approvalToParticipate.bind(this), 2 * 1000);
			return;
		}

		var approval = this.waitApprovals.shift();	// deque from wait approvals

		approval();	// invoke
	}

	allocateRigidbodyTask() {

		console.log("[action] allocate rigidbody.");

		var check = false;
		for (var j = 0; j < this.seatLength; j++) {	// player existence check
			if (this.seats[j] === true) {
				check = true;
			}
        }
		if (check === false) {
			return;
        }

		var rbObjValues = Object.values(this.rbObjects);
		var seatIndex = 0;
		rbObjValues.forEach(function (value) {	// recalculate rigidbody allocation table
			while (true) {
				if (this.seats[seatIndex] === true) {	// check if someone is in the seat
					for (var i = 0; i < this.seatLength; i++) {	// when an object is held by someone else, the player holding the object continues to be in charge of the physics processing.
						for (var j = 0; j < this.grabbTable[i].length; j++) {
							if (this.grabbTable[i][j].transform.id === value.transform.id) {
								// this.rbTable[value.transform.id] = seatIndex;	// <--- it should be assigned by grabb lock and does not need to be executed
								seatIndex = (seatIndex + 1) % this.seatLength;
								return;
							}
						}
					}

					this.rbTable[value.transform.id] = seatIndex;	// if no one has grabbed this object, let this player compute the physics of this object.
					seatIndex = (seatIndex + 1) % this.seatLength;
					return;
				} else {
					seatIndex = (seatIndex + 1) % this.seatLength;
					continue;
				}
			}
		}.bind(this));

		for (seatIndex = 0; seatIndex < this.seatLength; seatIndex++) {	// re-allocate with updated tables
			if (this.seats[seatIndex] === false) {	// check if someone is in the seat
				continue;
            }

			rbObjValues.forEach(function (value) {
				if (this.rbTable[value.transform.id] === undefined) {
					return;
                }

				var obj = {	// set useGravity to Off for rigidbodies that you are not in charge of
					role: SERVER,
					srcIndex: -1,
					dstIndex: seatIndex,
					action: ALLOCATE_GRAVITY,
					active: (this.rbTable[value.transform.id] === seatIndex),
					transform: { id: value.transform.id }
				};
				var json = JSON.stringify(obj);
				this.socketTable[seatIndex].send(json);

				console.log(
					"[log] seat ", seatIndex, ": ", value.transform.id, ", \t",
					((this.rbTable[value.transform.id] === seatIndex) ? "allocated" : "not allocated"));
			}.bind(this));
		}

		console.log("[finish] rigidbody allocated.");
	}

	allocateRigidbody(isImidiate) {

		if (isImidiate === true) {
			this.allocateFinished = false;
			this.allocateRigidbodyTask();
			this.allocateFinished = true;
			return;
		}

		if (this.allocateFinished == false) {	// wait for allocateRigidbodyTask() invoked
			return;
        }

		this.allocateFinished = false;

		setTimeout(function () {
			this.allocateRigidbodyTask();
			this.allocateFinished = true;
		}.bind(this), 5 * 1000);
	}

	deleteObjFromGrabbTable(target) {
		for (var i = 0; i < this.seatLength; i++) {
			this.grabbTable[i] = this.grabbTable[i].filter(function (value) {
				return value.transform.id !== target.transform.id
			});
        }

		this.simpleLockTable = this.simpleLockTable.filter(function (value) {
			return value.transform.id !== target.transform.id
		});
	}

	updateGrabbTable(parse, message) {

		/**
		 * Ensure that the object you are grasping does not cover 
		 * If someone has already grabbed the object, overwrite it
		 * 
		 * FREE : No one is grabbing
		 * FIXED : No one grabbed, but Rigidbody does not calculate
		 */

		this.deleteObjFromGrabbTable(parse);

		var grabIndex = parse.grabIndex;
		var transform = parse.transform;

		if (parse.grabIndex !== FREE) {	// output console grabb lock state and push to grabb table
			console.log("[action] grabb lock: " + transform.id + ", grabindex: " + grabIndex);

			if (transform.id in this.rbObjects) {	// If the grabbed object has gravity enabled, the player who grabbed it is authorized to calculate gravity.

				this.rbTable[transform.id] = grabIndex;

				for (var i = 0; i < this.seatLength; i++) {

					if (this.seats[i] === false) {
						continue;
					}

					var obj = {
						role: SERVER,
						srcIndex: -1,
						dstIndex: -1,
						action: ALLOCATE_GRAVITY,
						active: (this.rbTable[transform.id] === i),
						transform: { id: transform.id }
					};
					var json = JSON.stringify(obj);
					this.socketTable[i].send(json);
					console.log("seat " + i + " . " + this.rbTable[transform.id] + "\t" + transform.id);
				}
			}

			if (parse.grabIndex === FIXED){
				this.simpleLockTable.push({ transform: transform, message: message });	
			}else{
				this.grabbTable[grabIndex].push({ transform: transform, message: message });	// push target obj to grabb table
			}

		} else {
			console.log("[action] grabb unlock: ", transform.id);
        }

		console.log("[finish] grabb table updated successfully");
	}

	sendCurrentWoldData(socket) {

		console.log("[action] sending current world data to socket.");

		var syncObjValues = Object.values(this.syncObjects);
		var syncAnimValues = Object.values(this.syncAnims);
		var syncDivideValues = Object.values(this.syncDivides);

		syncObjValues.forEach(function (value) {
			var json = JSON.stringify(value);
			socket.send(json);
		}.bind(this));

		syncAnimValues.forEach(function (value) {	// send cached animatoin state
			var json = JSON.stringify(value);
			socket.send(json);
		}.bind(this));

		syncDivideValues.forEach(function (value) {	// send cached divide state
			var json = JSON.stringify(value);
			socket.send(json);
		}.bind(this));

		console.log("[finish] send current grabb table.");
	}

	sendSpecificTransform(id, socket) {

		console.log("[action] synchronizes object id ", id, " to socket.");

		if (id in this.syncObjects) {	// send transform state
			var parse = this.syncObjects[id];
			var json = JSON.stringify(parse);
			socket.send(json);
		}

		if (id in this.syncDivides) {	// send divide state
			var parse = this.syncDivides[id];
			var json = JSON.stringify(parse);
			socket.send(json);
		}

		console.log("[finish] synchronization is complete.");
	}

	sendSpecificAnim(id, socket) {

		console.log("[action] synchronizes animation id ", id, " to socket.");

		if (id in this.syncAnims) {	// send animator state
			var parse = this.syncAnims[id];
			var json = JSON.stringify(parse);
			socket.send(json);
		}

		console.log("[finish] synchronization is complete.");
	}

	sendCurrentGrabbState(socket) {

		console.log("[action] synchronizes grabb table to socket.");

		this.grabbTable.forEach(function (array) {
			array.forEach(function (value) {
				socket.send(value.message);
			}.bind(this))
		}.bind(this));

		this.simpleLockTable.forEach(function (value) {
			socket.send(value.message);
		}.bind(this));

		console.log("[finish] synchronization is complete.");
	}

	onJoined(userInfo) {

		console.log("[action] player has joined. starts the acceptance process.");
		console.log("[log] current sheet table: " + this.seats);

		this.seatFilled += 1;	// count up filled seat count

		var obj = {	// send acept message
			role: SERVER,
			action: ACEPT,
			srcIndex: -1,
			dstIndex: userInfo.seatIndex
		};
		var json = JSON.stringify(obj);
		userInfo.socket.send(json);

		var commonString = OVR_GUEST_ANCHOR + COMMA + userInfo.seatIndex.toString() + COMMA;	// remove transforms for player body tracker that already exist
		delete this.syncObjects[commonString + R_HAND];
		delete this.syncObjects[commonString + L_HAND];
		delete this.syncObjects[commonString + HEAD];

		this.sendCurrentWoldData(userInfo.socket);

		/**
		 * if you assign a rigidbody to a new participant, synchronization will start
		 * before determining who is holding the object, so notify who is holding which object first.
		 */

		this.sendCurrentGrabbState(userInfo.socket);

		console.log("[log] send current grabb state");

		console.log("[action] notify existing participants of a player's participation.");

		obj = {
			role: SERVER,
			action: GUEST_PARTICIPATION,
			srcIndex: userInfo.seatIndex,
			dstIndex: -1
		};
		json = JSON.stringify(obj);

		for (var index = 0; index < this.seatLength; index++) {
			if (index === userInfo.seatIndex) {
				continue;
			}
			var dstSocket = this.socketTable[index];
			if (dstSocket !== null) {
				dstSocket.send(json);
			}
		}

		console.log("[log] existing participants have been notified.");

		console.log("[action] share information about existing participants with new participants.");

		for (var index = 0; index < this.seatLength; index++) {
			if (index === userInfo.seatIndex) {
				continue;
			}

			var exists = this.socketTable[index];
			if (exists !== null) {
				obj = {
					role: SERVER,
					action: GUEST_PARTICIPATION,
					srcIndex: index,
					dstIndex: userInfo.seatIndex
				};
				json = JSON.stringify(obj);
				userInfo.socket.send(json);
			}
		}

		console.log("[log] information about existing participants is shared with new participants.");

		console.log("[finish] new participant acceptance has been successfully processed.");
	}
}

var rooms = {};
var roomIDs = {};

ws.on("connection", function (socket) {
	console.log("[log] client connected " + bar);

	var userInfo = { seatIndex: -1, socket: socket };

	socket.on("message", function (data, isBinary) {

		const message = isBinary ? data : data.toString();
		const parse = JSON.parse(message);
		var room = rooms[parse.roomID];

		if (room === undefined || room === null) {
			console.log("[action] create room: ", parse.roomID);
			room = new unity_room();
			rooms[parse.roomID] = room;
			console.log("[finish] room created: ", parse.roomID);
		}

		if (!(socket in roomIDs)) {	// connects the hash of the socket to the room id.
			roomIDs[socket] = parse.roomID;
		}

		room.onMessage(userInfo, parse, message);
	});

	socket.on('close', function close() {
		console.log("[log] client closed " + bar);

		var roomID = roomIDs[socket];
		var room = rooms[roomID];

		if (room !== undefined && room !== null) {
			room.onClose(userInfo);
			delete roomIDs[socket];
		}
	});
});
