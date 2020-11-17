# Candle.NET
.NET wrapper for the Candle API for controlling CAN bus gateways/analysers using the candlelight firmware (e.g. CANable, CANtact, etc)

## Notes

* We wrap a modified version of the CandleAPIDriver found in the [Cangaroo repo](https://github.com/HubertD/cangaroo/tree/master/src/driver/CandleApiDriver/api). 
* This is only tested on Windows (10 64bit). And since the underlying driver uses WinUSB, it is unlikely to work on other platforms
