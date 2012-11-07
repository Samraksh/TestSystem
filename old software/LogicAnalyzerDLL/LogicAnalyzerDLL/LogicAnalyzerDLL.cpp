// This is the main DLL file.
/*	Name	:	Logic Analyzer.cpp
 *
 *  Author  :	nived.sivadas@samraksh.com
 *
 *	Description : Almost copied verbatim from all the example source of the logic analyze but is written to provide
 *				  an interface to the logic for the test rig. The .net version of their source dll does not seem to work
 */


#include "stdafx.h"


#include "SaleaeDeviceApi.h"


#include <memory>
#include <iostream>
#include <string>


void __stdcall OnConnect(U64 device_id, GenericInterface* device_interface, void* user_data);
void __stdcall OnDisconnect( U64 device_id, void* user_data );
void __stdcall OnReadData( U64 device_id, U8* data, U32 data_length, void* user_data );
void __stdcall OnWriteData( U64 device_id, U8* data, U32 data_length, void* user_data );
void __stdcall OnError( U64 device_id, void* user_data );

#define USE_LOGIC_16 1

#if( USE_LOGIC_16 )
	Logic16Interface* gDeviceInterface = NULL;
#else
	LogicInterface* gDeviceInterface = NULL;
#endif

U64 gLogicId = 0;
U32 gSampleRateHz = 4000000;

void Initialize()
{
	DevicesManagerInterface::RegisterOnConnect( &OnConnect );
	DevicesManagerInterface::RegisterOnDisconnect( &OnDisconnect );
	DevicesManagerInterface::BeginConnect();
}

bool stop()
{
	if(gDeviceInterface->IsStreaming() == false)
		return false;
	else
		gDeviceInterface->Stop();

	return true;
}

bool startRead()
{
	gDeviceInterface->ReadStart();
	return true;
}

bool startWrite()
{
#if(USE_LOGIC_16)
	return false;
#else
	gDeviceInterface->WriteStart();
	return true;
#endif
}

int readByte()
{
#if(USE_LOGIC_16)
	return 0;
#else
	return (int) gDeviceInterface->GetInput()
#endif
}

void __stdcall OnConnect( U64 device_id, GenericInterface* device_interface, void* user_data )
{
#if( USE_LOGIC_16 )

	if( dynamic_cast<Logic16Interface*>( device_interface ) != NULL )
	{
		std::cout << "A Logic16 device was connected (id=0x" << std::hex << device_id << std::dec << ")." << std::endl;

		gDeviceInterface = (Logic16Interface*)device_interface;
		gLogicId = device_id;

		gDeviceInterface->RegisterOnReadData( &OnReadData );
		gDeviceInterface->RegisterOnWriteData( &OnWriteData );
		gDeviceInterface->RegisterOnError( &OnError );

		U32 channels[16];
		for( U32 i=0; i<16; i++ )
			channels[i] = i;

		gDeviceInterface->SetActiveChannels( channels, 16 );
		gDeviceInterface->SetSampleRateHz( gSampleRateHz );
	}

#else

	if( dynamic_cast<LogicInterface*>( device_interface ) != NULL )
	{
		std::cout << "A Logic device was connected (id=0x" << std::hex << device_id << std::dec << ")." << std::endl;

		gDeviceInterface = (LogicInterface*)device_interface;
		gLogicId = device_id;

		gDeviceInterface->RegisterOnReadData( &OnReadData );
		gDeviceInterface->RegisterOnWriteData( &OnWriteData );
		gDeviceInterface->RegisterOnError( &OnError );

		gDeviceInterface->SetSampleRateHz( gSampleRateHz );
	}

#endif
}

void __stdcall OnDisconnect( U64 device_id, void* user_data )
{
	if( device_id == gLogicId )
	{
		std::cout << "A device was disconnected (id=0x" << std::hex << device_id << std::dec << ")." << std::endl;
		gDeviceInterface = NULL;
	}
}

void __stdcall OnReadData( U64 device_id, U8* data, U32 data_length, void* user_data )
{
#if( USE_LOGIC_16 )
	std::cout << "Read " << data_length/2 << " words, starting with 0x" << std::hex << *(U16*)data << std::dec << std::endl;
#else
	std::cout << "Read " << data_length << " bytes, starting with 0x" << std::hex << (int)*data << std::dec << std::endl;
#endif

	//you own this data.  You don't have to delete it immediately, you could keep it and process it later, for example, or pass it to another thread for processing.
	DevicesManagerInterface::DeleteU8ArrayPtr( data );
}

void __stdcall OnWriteData( U64 device_id, U8* data, U32 data_length, void* user_data )
{
#if( USE_LOGIC_16 )

#else
	static U8 dat = 0;

	//it's our job to feed data to Logic whenever this function gets called.  Here we're just counting.
	//Note that you probably won't be able to get Logic to write data at faster than 4MHz (on Windows) do to some driver limitations.

	//here we're just filling the data with a 0-255 pattern.
	for( U32 i=0; i<data_length; i++ )
	{
		*data = dat;
		dat++;
		data++;
	}

	std::cout << "Wrote " << data_length << " bytes of data." << std::endl;
#endif
}

void __stdcall OnError( U64 device_id, void* user_data )
{
	std::cout << "A device reported an Error.  This probably means that it could not keep up at the given data rate, or was disconnected. You can re-start the capture automatically, if your application can tolerate gaps in the data." << std::endl;
	//note that you should not attempt to restart data collection from this function -- you'll need to do it from your main thread (or at least not the one that just called this function).
}
