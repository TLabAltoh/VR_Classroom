
const websocket = require("ws");

const port = 5500;
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
	res.write("TLab_VoiceChat");
	res.end();
});



console.log("\ncreate server " + bar);

server.listen(port);

let ws = new websocket.Server({ server: server });

console.log("\nserver start " + bar);

// #endregion Create HTTP Server

ws.on("connection", function (socket) {
	console.log("\nclient connected " + bar);

	socket.on("message", function (data, isBinary) {
		const message = isBinary ? data : data.toString();

		console.log("recv voice");

		ws.clients.forEach(client => {
			//if (client != socket)
				client.send(message);
		});
	});

	socket.on("close", function close() {
		console.log("\nclient closed " + bar);
	});
});
