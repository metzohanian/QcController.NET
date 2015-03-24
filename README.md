# QcController.NET
.NET library for managing some types of QuickSilver Controls motors.

This library was authorized for publication by the client Surface Machine Systems, LLC.

## Features
 - Supports immediate commands and motion-monitoring commands
 - Supports multiple motors, including near-simultaneous & coordinated motion control
 - Supports C#-domain scripting

## Known Issues
 - Uses a strange polling mechanism to iterate over message commands rather than events or coroutines (I can't remember why we did this ...

## Future Features
 - Add explicit support for on-controller scripts, script-loading, script-handling
 - Add support for checking and managing various status registers
 - Add support for interacting with control lines and IO ports
