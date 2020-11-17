# Candle.NET
.NET wrapper for the Candle API for controlling CAN bus gateways/analysers using the candlelight firmware (e.g. CANable, CANtact, etc)

## Notes

* We wrap a modified version of the CandleAPIDriver found in the [Cangaroo repo](https://github.com/HubertD/cangaroo/tree/master/src/driver/CandleApiDriver/api). 
* This is only tested on Windows (10 64bit). And since the underlying driver uses WinUSB, it is unlikely to work on other platforms

# Documentation

Example apps can be found in the `TestApp` folder.

```c#

using Candle;

void run() {
  var devices = Device.ListDevices();
  
  foreach(var device in devices) {
    device.Open();
    foraech(var channel in device.Channels) {
      channel.start();
      channel.Start(500000);
      
      // Send frames
      var frame = new Frame();
      frame.ID = 1;
      frame.Extended = true;
      frame.data = new byte[7] { 1, 1, 0, 1, 0, 0 0 };
      channel.Send(frame);
      
      // Receive frames
      var receivedFrames = channel.Receive();
      foreach(var frame in receivedFrames) {
      	Console.WriteLine(frame);
      }

      channel.stop();
    }
  }

```
