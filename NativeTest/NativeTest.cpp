// NativeTest.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <thread>
#include <chrono>
#include "candle.h"

template<typename T>
T& valueAndMove(uint8_t*& data)
{
	auto& reference = *(T*)(data);
	data += sizeof(T);
	return reference;
}

void sendMovementFrame(candle_handle device, uint8_t channel, int32_t position)
{
	candle_frame_t frame;
	{
		frame.can_id = 22 << 19 | CANDLE_ID_EXTENDED;
		frame.can_dlc = 7;
		frame.flags = 0;
		frame.data[0] = 1;
		frame.data[1] = 1;
		frame.data[2] = 0;
		frame.data[3] = 1;
		frame.data[4] = 0;
		frame.data[5] = 0;
		frame.data[6] = 0;
		frame.data[7] = 0;
	}

	// Send a TargetPosition frame (for Muscle Memory example)
	auto data = frame.data;
	valueAndMove<uint8_t>(data) = 1;
	valueAndMove<uint16_t>(data) = 12;
	valueAndMove<uint32_t>(data) = position;

	if (!candle_frame_send(device, channel, &frame)) {
		std::cerr << "Failed to send CAN frame" << std::endl;
	}
}


void sendFrames(candle_handle device, uint8_t channel)
{
	candle_frame_t frame;
	{
		frame.can_id = 22 << 19 | CANDLE_ID_EXTENDED;
		frame.can_dlc = 7;
		frame.flags = 0;
		frame.data[0] = 1;
		frame.data[1] = 1;
		frame.data[2] = 0;
		frame.data[3] = 1;
		frame.data[4] = 0;
		frame.data[5] = 0;
		frame.data[6] = 0;
		frame.data[7] = 0;
	}


	std::cout << "Sending init : " << std::endl;
	if (!candle_frame_send(device, channel, &frame)) {
		std::cerr << "Failed to send CAN frame" << std::endl;
	}



	std::cout << "Sending moves : " << std::endl;
	for (int i = 0; i < 100; i++) {
		sendMovementFrame(device, channel, i);
		std::cout << ".";
		std::this_thread::sleep_for(std::chrono::milliseconds(100));
	}
	for (int i = 100; i >= 0; i--) {
		sendMovementFrame(device, channel, i);
		std::cout << ".";
		std::this_thread::sleep_for(std::chrono::milliseconds(100));
	}
	
	std::cout << "Receiving all : " << std::endl;
	while(candle_frame_read(device, &frame, 100)) {
		auto id = frame.can_id;

		if (id & CANDLE_ID_EXTENDED) {
			std::cout << "E, ";
		}
		if (id & CANDLE_ID_RTR) {
			std::cout << "R, ";
		}
		if (id & CANDLE_ID_ERR) {
			std::cout << "ERR, ";
		}

		id &= (1 << 29) - 1;
		char buffer[100];

		sprintf_s(buffer, "ID : %d, DLC : %d, Data : %.2X,%.2X,%.2X,%.2X,%.2X,%.2X,%.2X,%.2X, Time : %d"
			, id
			, frame.can_dlc
			, frame.data[0]
			, frame.data[1]
			, frame.data[2]
			, frame.data[3]
			, frame.data[4]
			, frame.data[5]
			, frame.data[6]
			, frame.data[7]
			, frame.timestamp_us / 1000
		);
		std::cout << buffer << std::endl;
	}
}

void runChannel(candle_handle device, uint8_t channel)
{
	candle_capability_t capabilities;
	if (!candle_channel_get_capabilities(device, channel, &capabilities)) {
		std::cerr << "Failed to get capabilities" << std::endl;
	}
	else {
		std::cout << "Capabilities: " << std::endl;
		std::cout << "\t feature: " << capabilities.feature << std::endl;
		std::cout << "\t fclk_can: " << capabilities.fclk_can << std::endl;
		std::cout << "\t tseg1_min: " << capabilities.tseg1_min << std::endl;
		std::cout << "\t tseg1_max: " << capabilities.tseg1_max << std::endl;
		std::cout << "\t tseg2_min: " << capabilities.tseg2_min << std::endl;
		std::cout << "\t tseg2_max: " << capabilities.tseg2_max << std::endl;
		std::cout << "\t sjw_max: " << capabilities.sjw_max << std::endl;
		std::cout << "\t brp_min: " << capabilities.brp_min << std::endl;
		std::cout << "\t brp_max: " << capabilities.brp_max << std::endl;
		std::cout << "\t brp_inc: " << capabilities.brp_inc << std::endl;
	}

	candle_bittiming_t bitTiming;
	{
		bitTiming.brp = 875;
		bitTiming.phase_seg1 = 12;
		bitTiming.phase_seg2 = 2;
		bitTiming.sjw = 1;
		bitTiming.prop_seg = 1;
	}
	if (!candle_channel_set_timing(device, channel, &bitTiming)) {
		std::cerr << "Failed to set bit timing" << std::endl;
	}

	if (!candle_channel_set_bitrate(device, channel, 500000)) {
		std::cerr << "Failed to set bit rate" << std::endl;
	}

	if (!candle_channel_start(device, channel, 0)) {
		std::cerr << "Failed to set start channel" << std::endl;
	}

	sendFrames(device, channel);

	if (!candle_channel_stop(device, channel)) {
		std::cerr << "Failed to set top channel" << std::endl;
	}
}

void runDevice(candle_handle device)
{
	std::cout << "Opening device" << std::endl;
	if (!candle_dev_open(device)) {
		std::cerr << "Failed to open device" << std::endl;
	}

	uint32_t timestamp;
	if (!candle_dev_get_timestamp_us(device, &timestamp)) {
		std::cerr << "Failed to get timestamp" << std::endl;
	}
	else {
		std::cout << "Timestamp : " << timestamp << std::endl;
	}

	uint8_t numChannels;
	if (!candle_channel_count(device, &numChannels)) {
		std::cerr << "Failed to get number of channels" << std::endl;
	}
	else {
		std::cout << "Channel count : " << (int) numChannels << std::endl;
	}

	for (uint8_t channel = 0; channel < numChannels; channel++) {
		runChannel(device, channel);
	}

	if (!candle_dev_close(device)) {
		std::cerr << "Failed to close device " << std::endl;
	}
}


int main()
{
	// Print CAN devices
	{
		candle_list_handle deviceList;
		if (!candle_list_scan(&deviceList)) {
			std::cerr << "Failed to get CAN devices" << std::endl;
		}

		uint8_t count;
		if (!candle_list_length(deviceList, &count)) {
			std::cerr << "Failed to get list length" << std::endl;
		}
		std::cout << "Found " << (int) count << " devices" << std::endl;

		for (uint8_t i = 0; i < count; i++) {
			candle_handle device;
			if (!candle_dev_get(deviceList, i, &device)) {
				std::cerr << "Failed to get device " << (int) i << std::endl;
				continue;
			}

			candle_devstate_t deviceState;
			if (!candle_dev_get_state(device, &deviceState)) {
				std::cerr << "Failed to get device state" << std::endl;
			}

			std::cout << "Device state : "
				<< (deviceState == CANDLE_DEVSTATE_AVAIL
					? "Available"
					: "In use")
				<< std::endl;

			auto path = candle_dev_get_path(device);
			std::cout << "Device path : " << path << std::endl;

			runDevice(device);

			candle_dev_free(device);
		}

		candle_list_free(deviceList);
	}
}