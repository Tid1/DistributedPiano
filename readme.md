# Distributed piano 
This project contains two Unity scene (one using the AudioHelm plugin and one using another method for sound generation) for a distributed piano which can be used on several devices which are in the same LAN.

## How to use: 
- Start a scene
- On the host device
    - Optionally: Load a custom SMF midi file into the application on your host device
    - Optionally: Configure number of octaves and start octave
    - Click "Host" button
    - Wait until the devices were located using Network discovery (works in the background after you pressed "Host")
    - Click Connect (notes and configuration will be transferred)
    - Click Start to start the note visualization