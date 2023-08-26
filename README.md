# VR_Classroom
Online classes in VR space using Websocket and WebRTC

## Feature
- Combination of WebSocket and WebRTC according to the importance of messages in communication
- Hand tracking support  
- Support for bHaptics tactile gloves  
- WebView support (Android builds only)  
- Download object from external server (AssetBundle)  
- Control of object sharing from the teacher's side  

## Screenshot
<img src="Media/tlab-grabbable-controller.gif" width="256">  
<img src="Media/tlab-grabbable-handtracking.gif" width="256">  
<img src="Media/vkensyu.jpeg" width="256">  
<img src="Media/support-webview.jpg" width="256">

## Getting Started

### Prerequisites
- Unity 2021.3.23f1  
- Oculus Integration (Install from asset store)  
- ProBuilder (Install from asset store)  
- Android Logcat (Install from upm)  
- node (v16.15.0)  
- [bHaptics](https://assetstore.unity.com/packages/tools/integration/bhaptics-haptic-plugin-76647)
- [NativeWebsocket](https://github.com/endel/NativeWebSocket)

### Installing
Clone the repository to any directory with the following command  
```
git clone https://github.com/TLabAltoh/VR_Kensyu.git
```

### Set up
1. Execute the following commands in Server/SyncServer
```
npm start
```
2. Execute the following commands in Server/WebRTCSignaling
```
npm start
```

3. Set the SignalingServer and SyncServer addresses in Unity
![server-setup](Media/server-setup.png)  
![server-addr-manager](Media/server-address-manager.png)  
4. Launch the game from UnityEditor or the built file

### How to play
#### Enter Room
- Host
```
{IP Address (default 192.168.3.11} -p {Password (default 1234)}
```
- Guest
```
{IP Address (default 192.168.3.11)}
```
#### Controller
- IndexTrigger: Select UI
- handTrigger: Manipulating objects in the scene (grip, expand)
#### HandTracking
- Pinch of index finger and thumb: Select UI
- Hand-holding gesture: Manipulating objects in the scene (grip, expand)
