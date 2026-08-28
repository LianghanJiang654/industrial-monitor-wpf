# Industrial Machine Vision Inspection System

A C# WPF industrial machine vision inspection application integrated with a Python/OpenCV vision server through TCP/JSON communication.

This project demonstrates an end-to-end inspection workflow including configurable vision processing, pixel-to-machine coordinate conversion, PASS/FAIL evaluation, camera fault simulation, PLC handshake simulation, recipe management, alarms, production statistics, and annotated inspection image display.

## System Architecture

```text
C# WPF HMI
    |
    | Camera Trigger
    v
Camera Service
    |
    | Inspection Request
    v
TCP / JSON
    |
    v
Python Vision Server
    |
    v
OpenCV Processing
    |
    |-- Thresholding
    |-- Blob / Contour Detection
    |-- Position Measurement
    |-- Angle Measurement
    |-- Area Measurement
    |-- Pixel -> Machine Mapping
    |-- PASS / FAIL Evaluation
    |
    v
JSON Result + Annotated JPEG (Base64)
    |
    v
C# WPF HMI
    |
    v
PLC Handshake + Production Statistics
```

## Key Features

### WPF Industrial HMI

- C# / WPF desktop application
- MVVM-based application architecture
- Asynchronous inspection workflow
- Camera, vision and PLC status monitoring
- Production cycle, PASS, FAIL and yield statistics
- Inspection image and detected mark visualization

### Machine Vision Inspection

The Python/OpenCV vision pipeline supports:

- Fixed thresholding
- Otsu thresholding
- Adaptive thresholding
- Contour / blob detection
- Minimum and maximum area filtering
- Object center detection
- Rotation angle measurement
- Position tolerance inspection
- Angle tolerance inspection
- Area tolerance inspection
- PASS / FAIL evaluation
- Annotated inspection image generation

## Pixel-to-Machine Coordinate Mapping

The vision pipeline includes a simulated 9-point affine calibration workflow for converting image pixel coordinates into machine coordinates.

Detected objects contain both:

```text
Pixel coordinates:   X / Y
Machine coordinates: MachineX / MachineY
```

The current calibration points are simulated for software integration testing and are not presented as calibration data collected from a physical production machine.

## TCP / JSON Vision Integration

The WPF application communicates with the Python vision server through TCP sockets.

Inspection requests contain configurable parameters including:

- Image name
- Threshold mode
- Threshold value
- Minimum blob area
- Maximum blob area
- Position tolerance
- Angle tolerance
- Area tolerance

The vision server returns:

- Inspection status
- PASS / FAIL result
- Detected mark information
- Pixel X / Y coordinates
- Machine X / Y coordinates
- Rotation angle
- Blob area
- Inspection message
- Base64 encoded annotated JPEG image

The WPF TCP client reads the complete response stream before JSON deserialization, allowing larger annotated image payloads to be transferred reliably.

## PLC Handshake Simulation

The application implements an industrial-style inspection handshake:

```text
READY -> START -> TRIGGER -> BUSY -> DONE -> PASS / FAIL
```

The PASS / FAIL state is driven by the result returned from the vision inspection pipeline rather than hard-coded test values.

The current PLC implementation is simulated and does not claim communication with physical PLC hardware.

## Camera Simulation

Camera functionality is abstracted through `ICameraService`.

The simulated camera service supports:

- Connect / disconnect
- Live mode
- Software trigger
- Exposure adjustment
- Gain adjustment
- Offline fault simulation
- Timeout fault simulation
- No-image fault simulation

This allows inspection sequences and camera error-handling paths to be tested without physical industrial camera hardware.

## Recipe Management

Inspection recipes support configurable process and vision parameters with JSON persistence.

Different products can therefore use different inspection parameters without changing application source code.

## Alarm and Error Handling

The application includes:

- Vision alarm codes
- Camera fault handling
- Inspection timeout handling
- NG reason reporting
- System alarm persistence and acknowledgement
- User-visible alarm status

## Technology Stack

**Application**

- C#
- .NET
- WPF
- MVVM
- Async / Await
- TCP/IP Socket
- JSON
- SQLite

**Machine Vision**

- Python
- OpenCV
- NumPy
- TCP Socket
- JSON
- Base64 image transport

## Inspection Workflow

```text
Camera / PLC Trigger
        |
        v
Image Acquisition
        |
        v
TCP Inspection Request
        |
        v
Python / OpenCV Processing
        |
        v
Blob Detection
        |
        v
Position / Angle / Area Inspection
        |
        v
Pixel -> Machine Coordinate Mapping
        |
        v
PASS / FAIL
        |
        v
Annotated Image Returned to WPF
        |
        v
PLC DONE + PASS / FAIL
        |
        v
Production Statistics Updated
```

## Hardware Simulation Disclosure

This repository is an industrial automation and machine vision portfolio project.

### Currently simulated hardware

- Industrial camera hardware
- Physical PLC hardware
- Production machine I/O
- Real machine calibration point acquisition

### Implemented software

- WPF industrial HMI
- MVVM application logic
- TCP/JSON communication
- Python/OpenCV vision processing
- Blob inspection
- Position / angle / area tolerance evaluation
- PASS / FAIL decision logic
- Pixel-to-machine affine coordinate mapping
- Annotated image generation and transport
- Recipe persistence
- Alarm handling
- Camera fault simulation
- PLC handshake state logic

The camera, PLC and vision layers use service abstractions so simulated implementations can later be replaced by industrial camera SDKs, PLC communication drivers, or other machine vision platforms.

## Configuration

Vision server connection settings are located in:

```text
AppConfig.cs
```

Example:

```csharp
public const string VisionServerIp = "192.168.2.130";
public const int VisionServerPort = 5001;
```

Change the IP address to match the computer running the Python vision server.

## Project Scope

This project focuses on software architecture and integration workflows commonly used in industrial machine vision applications:

- Industrial HMI development
- Machine vision algorithm integration
- Cross-language TCP communication
- Inspection parameter management
- PLC-style handshake logic
- Camera fault handling
- Pixel-to-machine coordinate conversion
- Production result monitoring

Future development can replace the simulated camera and PLC services with physical industrial hardware while preserving the higher-level application architecture.