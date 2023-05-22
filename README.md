# VR_Kensyu
Multiplayer Sample using UnityVR and WebSockets  
Hand tracking support

## Screenshot
![TLabGrabbable Capture Trim](https://user-images.githubusercontent.com/121733943/235363804-01b50f49-674e-40d4-a11e-39ed3ced5600.gif)  
![HandTracking](https://github.com/TLabAltoh/VR_Kensyu/assets/121733943/7757588c-9163-46dc-94c9-60a8e0c715e4)

## Getting Started
### Prerequisites
- Unity 2021.3.23f1  
- Oculus Integration (Install from asset store)  
- ProBuilder (Install from asset store)  
- node (v16.15.0)
- Android Logcat
- [NativeWebsocket](https://github.com/endel/NativeWebSocket)
### How to run locally  
![image](https://github.com/TLabAltoh/VR_Kensyu/assets/121733943/41132a00-540c-4833-8b60-99348667f5cc)

![VRGrabber Capture 1](https://user-images.githubusercontent.com/121733943/235403254-baff2580-169c-4595-aeab-efb95d4054e1.png)
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

### Set Up
1. Specify the address of the PC on which to start the server (port 5000)
2. For multiplayer execution, execute the following commands from the Server/SyncServer/server.js
```
npm start
```
3. Launch the game from UnityEditor or the built file
### How to play
- IndexTrigger: Select UI
- handTrigger: Manipulating objects in the scene (grip, expand)

### Build Method
Change the UnityEditor platform to Windows or Android and build a scene named "Host"

## Link
[TLabVRGrabber can be used on its own at the following link](https://github.com/TLabAltoh/TLabVRGrabber)  
[TLabVRPlayerController can be used on its own at the followint link](https://github.com/TLabAltoh/TLabVRPlayerController)

## TODO
- Supports voice chat
