# VR_Kensyu
Remote teaching project for use in VR using Unity, WebSocket, and WebRTC  

## Feature
- Combination of WebSocket and WebRTC according to the importance of messages in communication
- Hand tracking support  
- Support for bHaptics tactile gloves  
- WebView support (Android builds only)  
- Download object from external server (AssetBundle)  
- Control of object sharing from the teacher's side  

## Screenshot
<img src="Media/TLabGrabbable.Capture.Trim.gif" width="256">  
<img src="Media/TLabGrabbable.HandTracking.Capture.gif" width="256">  
<img src="Media/V_Kensyu-Image.jpeg" width="256">  
<img src="Media/support-webview.jpg" width="256">
<img src="https://user-images.githubusercontent.com/121733943/235363804-01b50f49-674e-40d4-a11e-39ed3ced5600.gif" width="256">  
<img src="https://github.com/TLabAltoh/VR_Kensyu/assets/121733943/73a9d223-436b-489b-9d47-78a38f38c70f" width="256">  
<img src="https://github.com/TLabAltoh/VR_Kensyu/assets/121733943/edeb1f55-c6aa-436c-bd67-4ccd108f5692" width="256">  
<img src="https://github.com/TLabAltoh/VR_Kensyu/assets/121733943/82ffc226-a3ee-448c-8c18-0da42816549e" width="256">

## Getting Started

### Prerequisites
- Unity 2021.3.23f1  
- Oculus Integration (Install from asset store)  
- ProBuilder (Install from asset store)  
- Android Logcat (Install from upm)  
- node (v16.15.0)  
- [bHaptics](https://assetstore.unity.com/packages/tools/integration/bhaptics-haptic-plugin-76647)
- [NativeWebsocket](https://github.com/endel/NativeWebSocket)
- [TLabVRGrabber](https://github.com/TLabAltoh/TLabVRGrabber)
- [TLabVKeyborad](https://github.com/TLabAltoh/TLabVKeyborad)
- [TLabWebView](https://github.com/TLabAltoh/TLabWebView)
- [TLabVRPlayerController](https://github.com/TLabAltoh/TLabVRPlayerController)

### Installing
Clone the repository to any directory with the following command  
```
git clone https://github.com/TLabAltoh/VR_Kensyu.git
```
Execute the following commands in the cloned project (install necessary submodules)

```
git submodule init
git submodule update
```

### Start Server
Set up a server to synchronize worlds
1. Execute the following commands from the Server/SyncServer/ and Server/WebRTCSignaling/
```
npm start
```

![image](https://github.com/TLabAltoh/VR_Kensyu/assets/121733943/ff6c5652-d3d3-47a8-b2d3-cc3ff5ba3234)  
![server-addr-manager](https://github.com/TLabAltoh/VR_Kensyu/assets/121733943/fdcfab18-401a-44b3-8252-04a1c28c5d67)  
2. Set the SignalingServer and SyncServer addresses in Unity (ports 3001, 5000), then set the addresses for each component using the SetServerAddr button
3. Launch the game from UnityEditor or the built file

### How to play
#### Controller
- IndexTrigger: Select UI
- handTrigger: Manipulating objects in the scene (grip, expand)
#### HandTracking
- Pinch of index finger and thumb: Select UI
- Hand-holding gesture: Manipulating objects in the scene (grip, expand)
