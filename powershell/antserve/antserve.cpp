// antserve.cpp -------------------------------------------------------------
//
// Copyright (c) 2006  te
//
// Version:
//
//      01 Feb 2006    sb    Created.
//
//---------------------------------------------------------------------------

#include <vcl.h>
#pragma hdrstop
USERES("antserve.res");
USERES("trayicon.res");
USEFORM("rlsmain.cpp", ServerApp);
USELIB("antif.lib");
//---------------------------------------------------------------------------
#include "brand.h"

const char *SoftwareVersion    = "Software Version: " __DATE__;
const char *PrimaryCopyright   = PRIMARY_COPYRIGHT;

#define APP_TAG                RL_APP
const char *app_tag            = APP_TAG;
const char *CapitalisedTag     = "Ant";
const char *SecondaryCopyright = "Copyright (c) RockyLogic Ltd. 2004 to 2008";

const char *HelpFile           = ".\\" APP_TAG ".hlp";
const char *AppTitle           = "Ant Server";
//---------------------------------------------------------------------------
WINAPI WinMain(HINSTANCE, HINSTANCE, LPSTR, int) {

    // we only want one copy of the server, so attempt to create a mutex. If
    // the mutex already exists, GetLastError will return ERROR_ALREADY_EXISTS

    HANDLE hInstanceMutex = ::CreateMutex(NULL, TRUE, "RLServer" );

    if (GetLastError() == ERROR_ALREADY_EXISTS) {
      if (hInstanceMutex)
        CloseHandle(hInstanceMutex);
      return 0;
      }

    try {
      Application->Initialize();
      Application->Title = AppTitle;
      Application->CreateForm(__classid(TServerApp), &ServerApp);
      ShowWindow(Application->Handle, SW_HIDE);
      Application->ShowMainForm = false;
      Application->Minimize();

      Application->Run();
      }
    catch (Exception &exception) {
      Application->ShowException(&exception);
      }

    ReleaseMutex(hInstanceMutex);
    CloseHandle(hInstanceMutex);
    return 0;
  }
//---------------------------------------------------------------------------
