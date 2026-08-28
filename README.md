# Industrial Machine Vision Inspection System

A C# WPF industrial machine vision inspection application integrated with a Python/OpenCV vision server through TCP/JSON communication.

This project demonstrates an end-to-end industrial inspection workflow including configurable vision processing, pixel-to-machine coordinate conversion, PASS/FAIL evaluation, camera fault simulation, PLC handshake simulation, recipe management, alarms, production statistics, and annotated inspection image display.

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
