//---------------------------------------------------------------------
// abcif.h
//
// Copyright (c) 2001 to 2008  te
//---------------------------------------------------------------------
//
// Version:
//
//      01 Feb 2006    sb   Created.
//      01 May 2006    sb   Frequency Counter functions added
//      28 Jul 2006    sb   stdbool.h included to define bool for standard
//                            C 'extern' protected
//      01 Oct 2006    sb   Several changes:
//      01 Mar 2008    te   Several function name changes so that the
//                            software can be licensed to Bugblat.
//                            'Ant' references changed to 'Mod"
//
//---------------------------------------------------------------------
// Declaration of the identical antif.h and bbxif.h interfaces.
// Both antif.h and bbxif.h include this file.
//
// The antif.dll and bbxif.dll DLLs have the same interface. Note that this
// may change as products develop. Also, be aware that though the interface
// is common, the contents of the DLLs differ!
// --------------------------------------------------------------------

#ifndef abcifH
#define abcifH

#ifndef __cplusplus
//#include <stdbool.h>
#endif

//---------------------------------------------------------------------
#define LOCALHOST                     "127.0.0.1"
#define DEFAULT_PORT_RL               8279          /* "RO"=82,79 */
#define DEFAULT_PORT_BB               8280

//---------------------------------------------------------------------
typedef struct {
  int     Channels;               // Ant8:8, Ant16:16, Ant18e:18, BBX34:32
  int     Depth;                  // 3K for Ant8, 2K for Ant16
  int     PatternRecognizers;     // 2
  int     XFunctions;             // 2
  int     StateMachineFunctions;  // 8
  int     MaxCounter;             // 1023
  int     BytesWide;              //
  int     ThresoldGroups;         //
  int     FCchannels;             // >0 if frequency counter supported
  int     ModCaps;                // misc capability flags in all modes
  int     LAcaps;                 // misc capability flags in LA mode
  int     FCcaps;                 // misc capability flags in FC mode
  int     SlowestClockIx;
  int     FastestClockIx;
  int     Dummy[8];
  } TModParams;

typedef int             RL_RESULT;
typedef void*           RL_HANDLE;
typedef unsigned char   BYTE;
typedef unsigned short  WORD;
typedef unsigned long   DWORD;
typedef __int64         QWORD;

//---------------------------------------------------------------------
// frequency counter parameters

#define FC_OP_FREQ                     0
#define FC_OP_PERIOD                   1
#define FC_OP_EVENTS                   2

#define FC_GATE_100MS                  0
#define FC_GATE_1S                     1
#define FC_GATE_10S                    2
#define FC_GATE_INF                    7

#define FC_EDGE_RISING_RISING          3
#define FC_EDGE_RISING_FALLING         1
#define FC_EDGE_FALLING_RISING         2
#define FC_EDGE_FALLING_FALLING        0
//---------------------------------------------------------------------
// return codes

#define RL_OK                          0
#define RL_ERROR                      -1
#define RL_BAD_HANDLE                 -2
#define RL_COULD_NOT_CONNECT          -3
#define RL_BAD_PARAMETER              -4
#define RL_BUFFER_TOO_SMALL           -5
#define RL_UNEXPECTED_SERVER_MESSAGE  -6
#define RL_ERROR_MESSAGE_FROM_SERVER  -7
#define RL_NOT_OPEN                   -8

// routines from the FTDI driver interface

#define OP_FtListDevices               1
#define OP_FtOpenEx                    2
#define OP_FtClose                     3
#define OP_FtPurge                     4
#define OP_FtSetTimeouts               5
#define OP_FtRead                      6
#define OP_FtWrite                     7
#define OP_FtSetUSBParameters          8
#define OP_FtSetLatencyTimer           9
#define OP_FtGetLatencyTimer          10

//---------------------------------------------------------------------
#define ANT_OP_SUCCESS                             0

// device access protocol errors
#define ANT_OP_FAILED_MODULE_OPEN                  1
#define ANT_OP_FAILED_NO_MODULE_OPEN               2
#define ANT_OPEN_FAILED_UNKNOWN_MODULE_NAME        3
#define ANT_OPEN_FAILED_MODULE_NO_LONGER_PRESENT   4
#define ANT_OPEN_FAILED_IN_USE                     5
#define ANT_LOADFPGA_FAILED_DEVICE_CODE            6
#define ANT_LOADFPGA_FAILED_BAD_INDEX              7
#define ANT_LOADFPGA_LOAD_FAILED                   8

// error in received parameter(s)
#define ANT_OPEN_FAILED_MODULE_NAME_LENGTH        40

// miscellaneous errors
#define ANT_INTERNAL_ERROR_BUFFER_LENGTH          60

//---------------------------------------------------------------------
#define USB_NAME_LENGTH         8             /* e.g. RL1234AB */
#define FLAGS_LENGTH            3

// flag characters position in the string
#define POS_FLAG_ANT_TYPE       0             /* r, o, c, ...           */
#define POS_FLAG_OPEN           1             /* b(usy) or f(ree)       */
#define POS_FLAG_ANT_PRESENT    2             /* p(resent) or m(issing) */

// module types
enum TModuleType { MT_NOT_A_NAME    = 'n',  // initial state of vars
                   MT_UNKNOWN       = 'u',  // after name changes
                   MT_FAILED        = 'f',  // error thrown during I/O

                   MT_ANT8          = 'r',
                   MT_ANT16         = 'o',
                   MT_ANT18E        = 'c',

                   MT_BBX34         = 'b',

                   MT_UNRECOGNISED  = 'q' };// could not identify

//---------------------------------------------------------------------
// pattern recogniser values

#define PATTERN_LO                    '0'
#define PATTERN_HI                    '1'
#define PATTERN_RISING_EDGE           'R'
#define PATTERN_FALLING_EDGE          'F'
#define PATTERN_EITHER_EDGE           'E'
#define PATTERN_DONT_CARE             '-'

// --------------------------------------------------------------------
// sampling time units
#define TIME_UNIT_NS      0           /* 1 ns                           */
#define TIME_UNIT_MUS     1           /* 1 microsec                     */
#define TIME_UNIT_MS      2           /* 1 ms                           */
#define TIME_UNIT_S       3           /* 1 second                       */
#define TIME_UNIT_SYNC    4           /* synchronous acquisition timing */

// --------------------------------------------------------------------
// sample speeds. both the 1-2-4 time sequence and the 5-2-1 frequency sequence
// new entries are added at the end.

#define K_Sync                     0       /* Synchronous, rising edge */
#define K_500MHz                   1       /* 500MHz -  2ns */
#define K_250MHz                   2       /* 250MHz -  4ns */
#define K_200MHz                   3       /* 200MHz -  5ns */
#define K_100MHz                   4       /* 100MHz - 10ns */
#define K_50MHz                    5       /* 50MHz  - 20ns */
#define K_25MHz                    6       /* 25MHz  - 40ns */
#define K_20MHz                    7       /* 20MHz  - 50ns */
#define K_10MHz                    8       /* 10MHz  -100ns */
#define K_5MHz                     9       /* 5MHz   -200ns */
#define K_2_5MHz                  10       /* 2.5MHz -400ns */
#define K_2MHz                    11       /* 2MHz   -500ns */
#define K_1MHz                    12       /* 1MHz   -  1us */
#define K_500KHz                  13       /* 500KHz -  2us */
#define K_250KHz                  14       /* 250KHz -  4us */
#define K_200KHz                  15       /* 200KHz -  5us */
#define K_100KHz                  16       /* 100KHz - 10us */
#define K_50KHz                   17       /* 50KHz  - 20us */
#define K_25KHz                   18       /* 25KHz  - 40us */
#define K_20KHz                   19       /* 20KHz  - 50us */
#define K_10KHz                   20       /* 10KHz  -100us */
#define K_5KHz                    21       /* 5KHz   -200us */
#define K_2_5KHz                  22       /* 2.5KHz -400us */
#define K_2KHz                    23       /* 2KHz   -500us */
#define K_1KHz                    24       /* 1KHz   -  1ms */
#define K_500Hz                   25       /* 500Hz  -  2ms */
#define K_250Hz                   26       /* 250Hz  -  4ms */
#define K_200Hz                   27       /* 200Hz  -  5ms */
#define K_100Hz                   28       /* 100Hz  - 10ms */
#define K_Sync_R              K_Sync       /* Synchronous, rising edge  */
#define K_Sync_F                  29       /* Synchronous, falling edge */
#define K_Sync_E                  30       /* Synchronous, either edge  */
#define K_1GHz                    31       /* 1GHz   -  1ns */
#define LA_KMAX  31                         /* highest defined index */

// --------------------------------------------------------------------
// state machine equation indices

#define EQN_TC_ENABLE              0
#define EQN_TC_LOAD                1
#define EQN_HIT2_HIT1              2
#define EQN_HIT2_TRIG              3
#define EQN_HIT1_HIT2              4
#define EQN_HIT1_TRIG              5
#define EQN_SEARCH_HIT1            6
#define EQN_SEARCH_TRIG            7

// --------------------------------------------------------------------
// 8-bit Status code returned by RL_GetRunStatus
// bit 7     : 'triggered'
// bit 6     : all RAM has been written at least once
// bits 5..0 : the state of the internal state machine

#define ANTLA_STATUS_TRIGGERED               (1 << 7)
#define ANTLA_STATUS_RAMWRITTEN              (1 << 6)

#define ANTLA_ACQUISITION_STATE_idle         1
#define ANTLA_ACQUISITION_STATE_prefill      2
#define ANTLA_ACQUISITION_STATE_search       3
#define ANTLA_ACQUISITION_STATE_hit1         4
#define ANTLA_ACQUISITION_STATE_hit2         5
#define ANTLA_ACQUISITION_STATE_triggered    6
#define ANTLA_ACQUISITION_STATE_done         7

// --------------------------------------------------------------------
// structure filled in by RL_Info()

#define MOD_CAPS_THRESH   (1<<0)  /* has variable threshold                  */

#define LA_CAPS_SYNC   (1<<0)     /* synchronous acquisition                 */
#define LA_CAPS_TRIG   (1<<1)     /* external trigger                        */
#define LA_CAPS_MON    (1<<2)     /* pin monitoring in acquisition mode      */
#define LA_CAPS_1GHZ   (1<<3)     /* 1GHz data acquisition                   */
#define LA_CAPS_SYNC_F (1<<4)     /* synchronous acquisition/falling edge    */

// --------------------------------------------------------------------
// info set by RL_SetVal() and/or returned by RL_GetVal()
#define RL_GETVAL_SAMPLECOUNT   1
#define RL_GETVAL_TRIGGERPOS    2
#define RL_GETVAL_SLOTCOUNT     3
#define RL_GETVAL_UNUSED_4      4
#define RL_GETVAL_SLOTBYTES     5

//---------------------------------------------------------------------
 #ifdef __cplusplus
 extern "C" {
 #endif

 #define RL_DLL   __declspec(dllexport) RL_RESULT

  RL_DLL RL_Info(int ModType, TModParams* pInfo, int InfoBytes);
  RL_DLL RL_ClockInfo(int ClkIx, int *ClockPeriod, int *ClockPeriodUnit,
                                      char *pTagBuff, int TagBuffSize);

  RL_DLL RL_Initialize(RL_HANDLE* h);
  RL_DLL RL_ConnectToServer(RL_HANDLE h, char* Host, int Port);
  RL_DLL RL_DisconnectFromServer(RL_HANDLE h);
  RL_DLL RL_Finalize(RL_HANDLE h);

  RL_DLL RL_GetLastErrorMessage(RL_HANDLE h, char* s, int len);

  RL_DLL RL_QueryServerID(RL_HANDLE h, char* s, int len);
  RL_DLL RL_ModCount(RL_HANDLE h, int* pCount);
  RL_DLL RL_ModInfo(RL_HANDLE h, int index, char* name, char* flags);

  RL_DLL RL_ModIsOpen(RL_HANDLE h, int *pModIsOpen);
  RL_DLL RL_OpenMod(RL_HANDLE h, char* name);
  RL_DLL RL_CloseMod(RL_HANDLE h);

  RL_DLL RL_SetHitPattern(RL_HANDLE h, int ix, bool AndCombine, char* Pat);
  RL_DLL RL_GetHitPattern(RL_HANDLE h, int ix, bool* pAndCombine,
                                                 char* pBuff, int BuffLen);

  RL_DLL RL_SetXFunction(RL_HANDLE h, int ix, char* Func);
  RL_DLL RL_GetXFunction(RL_HANDLE h, int ix, char* pBuff, int BuffLen);

  RL_DLL RL_SetStateMachineFunction(RL_HANDLE h, int ix, char* Func);
  RL_DLL RL_GetStateMachineFunction(RL_HANDLE h, int ix, char* pBuff,
                                                           int BuffLen);

  RL_DLL RL_SetClockIx(RL_HANDLE h, int ClkIx);
  RL_DLL RL_GetClockIx(RL_HANDLE h, int* pClkIx);

  RL_DLL RL_SetTriggerPos(RL_HANDLE h, int Percent);
  RL_DLL RL_GetTriggerPos(RL_HANDLE h, int* pPercent);

  RL_DLL RL_SetTimerCounter(RL_HANDLE h, int Val, bool TCIsTimer);
  RL_DLL RL_GetTimerCounter(RL_HANDLE h, int* pVal, bool* pTCIsTimer);

  RL_DLL RL_SetThreshold(RL_HANDLE h, int ByteIx, int ThreshValX10);
  RL_DLL RL_GetThreshold(RL_HANDLE h, int ByteIx, int* pThreshValX10);

  RL_DLL RL_Run(RL_HANDLE h, bool run);
  RL_DLL RL_GetRunStatus(RL_HANDLE h, int* Status);
  RL_DLL RL_GetPinStatus(RL_HANDLE h, char* pStatus, int StatusLen);
  RL_DLL RL_Readback(RL_HANDLE h, BYTE *pBuff, int BuffSize, int Dummy );

  RL_DLL RL_GetVal(RL_HANDLE h, int AParam, __int64 *pVal);
  RL_DLL RL_SetVal(RL_HANDLE h, int AParam, __int64 AVal);

  RL_DLL RL_FCRun(RL_HANDLE h, int AOp, int AEdges, int AGateLength,
                                                      char *APat, bool ARun);
  RL_DLL RL_FCGetStatus(RL_HANDLE h, int *pStat, int ALen);
  RL_DLL RL_FCGetCounts(RL_HANDLE h, QWORD *pVal, int AChans);

 #undef  RL_DLL

 #ifdef __cplusplus
 }
 #endif

#endif

// EOF ----------------------------------------------------------------
