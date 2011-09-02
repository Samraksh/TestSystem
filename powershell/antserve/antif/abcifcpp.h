// --------------------------------------------------------------------
// abcifcpp.h
//
// This version generated on 17 April 2008.
//
// Copyright (c) 2008 te
//
// --------------------------------------------------------------------
// C++ interface to the (identical interface) antif.h/bbxif.h routines
// Both antifcpp.h and bbxifcpp.h include this file.
//
// The antif.dll and bbxif.dll DLLs have the same interface. Note that this
// may change as products develop. Also, be aware that though the interface
// is common, the contents of the DLLs differ!
// --------------------------------------------------------------------

#ifndef abcifcppH
#define abcifcppH

#include "abcif.h"

//---------------------------------------------------------------------

class TModIfError {
  public:
    RL_RESULT err;
    TModIfError(RL_RESULT e) { err = e; }
    TModIfError(void)        { err = RL_ERROR; }    // default constructor
  };

class TModIfCpp {
  private:
    RL_HANDLE FHandle;

    void CheckResult (RL_RESULT res) {
      if (res != RL_OK)
        throw TModIfError(res);
      }

  public:
    TModIfCpp(void) {
      RL_RESULT r = RL_Initialize(&FHandle);
      CheckResult(r);
      }

    ~TModIfCpp(void) {
      try {
        RL_RESULT r = RL_Finalize(FHandle);
        CheckResult(r);
        }
      catch (...) {
        throw;
        }
      }

    void Info(int AModType, TModParams* pInfo, int InfoBytes) {
      RL_RESULT r = RL_Info(AModType, pInfo, InfoBytes);
      CheckResult(r);
      }

    void ClockInfo(int ClkIx, int *ClockPeriod, int *ClockPeriodUnit,
                                      char *pTagBuff, int TagBuffSize) {
      RL_RESULT r = RL_ClockInfo(ClkIx, ClockPeriod, ClockPeriodUnit,
                                            pTagBuff, TagBuffSize);
      CheckResult(r);
      }

    void ConnectToServer(char* AHost, int APort) {
      RL_RESULT r = RL_ConnectToServer(FHandle, AHost, APort);
      CheckResult(r);
      }

    void DisconnectFromServer(void) {
      RL_RESULT r = RL_DisconnectFromServer(FHandle);
      CheckResult(r);
      }

    void GetLastErrorMessage(char* s, int len) {
      RL_RESULT r = RL_GetLastErrorMessage(FHandle, s, len);
      CheckResult(r);
      }

    void QueryServerID(char* s, int len) {
      RL_RESULT r = RL_QueryServerID(FHandle, s, len);
      CheckResult(r);
      }

    int ModCount(void) {
      int n = 0;
      RL_RESULT r = RL_ModCount(FHandle, &n);
      CheckResult(r);
      return n;
      }

    void ModInfo(int index, char* name, char* flags) {
      RL_RESULT r = RL_ModInfo(FHandle, index, name, flags);
      CheckResult(r);
      }

    bool ModIsOpen(void) {
      int v;
      RL_RESULT r = RL_ModIsOpen(FHandle, &v);
      CheckResult(r);
      return (v==1);
      }

    void OpenMod(char *name) {
      RL_RESULT r = RL_OpenMod(FHandle, name);
      CheckResult(r);
      }

    void CloseMod(void) {
      RL_RESULT r = RL_CloseMod(FHandle);
      CheckResult(r);
      }

    void SetHitPattern(int ix, bool AndCombine, char* Pat) {
      RL_RESULT r = RL_SetHitPattern(FHandle, ix, AndCombine, Pat);
      CheckResult(r);
      }

    void GetHitPattern(int ix, bool* pAndCombine, char* pBuff, int BuffLen) {
      RL_RESULT r = RL_GetHitPattern(FHandle, ix, pAndCombine, pBuff, BuffLen);
      CheckResult(r);
      }

    void SetXFunction(int ix, char* Func) {
      RL_RESULT r = RL_SetXFunction(FHandle, ix, Func);
      CheckResult(r);
      }

    void GetXFunction(int ix, char *pBuff, int BuffLen) {
      RL_RESULT r = RL_GetXFunction(FHandle, ix, pBuff, BuffLen);
      CheckResult(r);
      }

    void SetStateMachineFunction(int ix, char* Func) {
      RL_RESULT r = RL_SetStateMachineFunction(FHandle, ix, Func);
      CheckResult(r);
      }

    void GetStateMachineFunction(int ix, char *pBuff, int BuffLen) {
      RL_RESULT r = RL_GetStateMachineFunction(FHandle, ix, pBuff, BuffLen);
      CheckResult(r);
      }

    void SetClockIx(int ClkIx) {
      RL_RESULT r = RL_SetClockIx(FHandle, ClkIx);
      CheckResult(r);
      }

    int GetClockIx(void) {
      int ix;
      RL_RESULT r = RL_GetClockIx(FHandle, &ix);
      CheckResult(r);
      return ix;
      }

    void SetTriggerPos(int Percent) {
      RL_RESULT r = RL_SetTriggerPos(FHandle, Percent);
      CheckResult(r);
      }

    int GetTriggerPos(void) {
      int v;
      RL_RESULT r = RL_GetTriggerPos(FHandle, &v);
      CheckResult(r);
      return v;
      }

    void SetTimerCounter(int Val, bool TCIsTimer) {
      RL_RESULT r = RL_SetTimerCounter(FHandle, Val, TCIsTimer);
      CheckResult(r);
      }

    void GetTimerCounter(int* pVal, bool* pTCIsTimer) {
      RL_RESULT r = RL_GetTimerCounter(FHandle, pVal, pTCIsTimer);
      CheckResult(r);
      }

    void SetThreshold(int AByteIx, int AThreshValX10) {
      RL_RESULT r = RL_SetThreshold(FHandle, AByteIx, AThreshValX10);
      CheckResult(r);
      }

    int GetThreshold(int AByteIx) {
      int ThreshValX10;
      RL_RESULT r = RL_GetThreshold(FHandle, AByteIx, &ThreshValX10);
      CheckResult(r);
      return ThreshValX10;
      }

    void Run(bool Arun) {
      RL_RESULT r = RL_Run(FHandle, Arun);
      CheckResult(r);
      }

    int GetRunStatus(void) {
      int Status;
      RL_RESULT r = RL_GetRunStatus(FHandle, &Status);
      CheckResult(r);
      return Status;
      }

    void GetPinStatus(char* pStatus, int AStatusLen) {
      RL_RESULT r = RL_GetPinStatus(FHandle, pStatus, AStatusLen);
      CheckResult(r);
      }

    __int64 GetVal(int AParam) {
      __int64 Val;
      RL_RESULT r = RL_GetVal(FHandle, AParam, &Val);
      CheckResult(r);
      return Val;
      }

    void SetVal(int AParam, __int64 AVal) {
      RL_RESULT r = RL_SetVal(FHandle, AParam, AVal);
      CheckResult(r);
      }

    void Readback(BYTE *pBuff, int ABuffSize) {
      RL_RESULT r = RL_Readback(FHandle, pBuff, ABuffSize, 0);
      CheckResult(r);
      }

    void FCRun(int AOp, int AEdges, int AGateLength, char *APat, bool ARun) {
      RL_RESULT r = RL_FCRun(FHandle, AOp, AEdges, AGateLength, APat, ARun);
      CheckResult(r);
      }

    void FCGetStatus(int *pStat, int ALen) {
      RL_RESULT r = RL_FCGetStatus(FHandle, pStat, ALen);
      CheckResult(r);
      }

    void FCGetCounts(__int64 *pVal, int AChans) {
      RL_RESULT r = RL_FCGetCounts(FHandle, pVal, AChans);
      CheckResult(r);
      }
  };

#endif
