////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "GPIO.h"
#include <led/stm32f10x_led.h>

//---//

//
// Note: CPU_GPIO_Initialization is called elsewhere
//

BOOL GPIO::Execute2( ) {
	uint32_t ik;
	#define TESTPIN1 86
	#define TESTPIN2 87
	#define TESTPIN3 88
	#define TESTPIN4 89
	CPU_GPIO_EnableOutputPin( TESTPIN1, TRUE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_EnableOutputPin( TESTPIN2, TRUE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_EnableOutputPin( TESTPIN3, TRUE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_EnableOutputPin( TESTPIN4, TRUE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN1, FALSE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN3, FALSE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN2, FALSE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN4, FALSE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN4, TRUE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN4, FALSE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN1, TRUE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN3, TRUE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN3, FALSE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN2, TRUE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN1, FALSE );
	for(ik = 0; ik < 100000; ik++);
	CPU_GPIO_SetPinState( TESTPIN2, FALSE );
	for(ik = 0; ik < 100000; ik++);
}

BOOL GPIO::Execute( LOG_STREAM Stream )
{
    BOOL offPinState = TRUE;     // If test succeeds, states
    BOOL onPinState  = FALSE;    // are reversed

    Log& log = Log::InitializeLog( Stream, "GPIO" );
   
    if((GPIO_PIN_NONE == m_gpioPin) || CPU_GPIO_PinIsBusy(m_gpioPin))
    {
        log.CloseLog( FALSE, "pin unavailable" );
    }
    else
    {
        onPinState = TRUE;
        CPU_GPIO_EnableOutputPin( m_gpioPin, onPinState );

        onPinState = CPU_GPIO_GetPinState( m_gpioPin );

        if(!onPinState )
        {
            log.CloseLog( FALSE, "on->off fails" );
        }
        else
        {
            offPinState  = FALSE;
            
            CPU_GPIO_SetPinState( m_gpioPin, offPinState );

            offPinState = CPU_GPIO_GetPinState( m_gpioPin );

            if(offPinState)
            {
                log.CloseLog( FALSE, "off->on fails" );
            }
        }     
    }  
    return (onPinState && !offPinState); 
} //Execute


GPIO::GPIO( GPIO_PIN testPin )
{
    m_gpioPin = testPin;
}

//--//

