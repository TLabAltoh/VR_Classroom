# VR_Kensyu
Multiplayer using UnityVR and WebSockets

# Screenshot
Image is an example of a project using this asset  
![TLabGrabbable Capture Trim](https://user-images.githubusercontent.com/121733943/235363804-01b50f49-674e-40d4-a11e-39ed3ced5600.gif)

# Getting Started
## Prerequisites
- Unity 2021.3.23f1  
- Oculus Integration (Install from asset store)  
- ProBuilder (Install from asset store)  
- node (v16.15.0)
## How to run locally  
![VRGrabber Capture 1](https://user-images.githubusercontent.com/121733943/235403254-baff2580-169c-4595-aeab-efb95d4054e1.png)
1. Specify the address of the PC on which to start the server (port 5000)
2. For multiplayer execution, execute the following commands from the Server/SyncServer/server.js
```
npm start
```
3. Launch the game from UnityEditor or the built file
## How to play
- IndexTrigger: Select UI
- handTrigger: Manipulating objects in the scene (grip, expand)

## Build Method
Change the UnityEditor platform to Windows or Android and build a scene named "Host"

## TLabVRGrabber
[TLabVRGrabber can be used on its own at the following link](https://github.com/TLabAltoh/TLabVRGrabber)
