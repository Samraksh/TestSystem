// rlsmain.cpp --------------------------------------------------------------
//
// Copyright (c) 2006  te
//
// Version:
//
//      01 Feb 2006    sb    Created.
//      30 Jun 2006    sb    Updated for Ant18e
//      12 Mar 2008    sb    Updated for Bugblat (incomplete)
//
//---------------------------------------------------------------------------
#include <vcl.h>
#pragma hdrstop

#include <map>
#include <string>
#include <list>
#ifdef _WIN32
  using namespace std;
#endif

#include "brand.h"
#include "antif.h"
#include "rlsmain.h"

//---------------------------------------------------------------------------
#pragma package(smart_init)
#pragma resource "*.dfm"

TServerApp *ServerApp;

//---------------------------------------------------------------------------
// tray icon stuff
#include <shellapi.h>
    // The ID of the tray icon is arbitrary
static const int IDC_TRAY1 = 1005;
    // Hint text for the tray icon
static const char *HINT_MESSAGE = "Test Server";
    // WM_TRAYNOTIFY (in rlsmain.h) is a user defined message that the tray
    // will send to our window handle for tray mouse events

//---------------------------------------------------------------------------
__fastcall TServerApp::TServerApp(TComponent* Owner) : TForm(Owner) {
  Caption = "Version: " __DATE__ " ";
  Copyright->Caption = SecondaryCopyright;

  // Load the icon from the EXE's resources
  TrayIcon = new Graphics::TIcon;
  TrayIcon->Handle = LoadImage(HInstance,
                              "LITTLEICON",
                              IMAGE_ICON,
                              0,0,          // desired width and height
                              0);           // flags

  // Add the icon to the taskbar
  AddIcon();

  Application->OnMinimize = AppOnMinimize;
  Application->OnRestore  = AppOnRestore;

  bool IsRL = strcmp(app_tag, RL_APP)==0;
  int Port  = IsRL ? DEFAULT_PORT_RL : DEFAULT_PORT_BB;

  RL_RESULT r = RL_Initialize(&h);
  r = RL_ConnectToServer(h, NULL, Port);
  if (r != RL_OK) {
    AnsiString Msg = "Failed to connect to local server on Port "
                        + AnsiString(Port);
    Application->MessageBox(Msg.c_str(), "Server Fatal Error", MB_OK);
    exit(EXIT_FAILURE);
    }

  ModList->Cells[0][0]  = "Module ID";
  ModList->Cells[1][0]  = "Module Type";
  ModList->Cells[2][0]  = "Status";
  ModList->RowCount     = 1;

  int space = ModList->Width - 2* ModList->GridLineWidth;
  ModList->ColWidths[0] = space/3;
  ModList->ColWidths[1] = space/3;
  ModList->ColWidths[2] = space - 2* ModList->ColWidths[0];

  Left = (Screen->Width  - Width )/2;
  Top  = (Screen->Height - Height)/2;

  PortLabel->Caption = "Port: " + AnsiString(Port);

  RestoreApp = false;
  MinimizeApp = false;

  Ticker           = new TTimer(this);
  Ticker->Enabled  = false;
  Ticker->Interval = 1000;
  Ticker->OnTimer  = TickerTick;
  }

//---------------------------------------------------------------------------
__fastcall TServerApp::~TServerApp() {
  RemoveIcon();
  delete TrayIcon;
  RL_Finalize(h);
  }

void __fastcall TServerApp::WMTrayNotify(TMessage &Msg) {
  // The LPARAM of the message identifies the type of mouse message.
  // When they right click, show the popup menu. When they double
  // click with the left mouse, show the form.
  switch(Msg.LParam) {
    case WM_RBUTTONUP:
            POINT WinPoint;           // find the mouse cursor
            GetCursorPos(&WinPoint);  // using api function, store
            SetForegroundWindow(Handle);
            PopupMenu1->Popup(WinPoint.x,WinPoint.y);
            PostMessage(Handle, WM_NULL, 0,0);
            break;
    case WM_LBUTTONDBLCLK:
            Application->Restore();
            break;
    }
  }

//---------------------------------------------------------------------------
void __fastcall TServerApp::CloseBtnClick(TObject */*Sender*/) {
  Application->Terminate();
  }

void __fastcall TServerApp::RestoreBtnClick(TObject */*Sender*/) {
  Application->Restore();
  }

void __fastcall TServerApp::CancelButtonClick(TObject */*Sender*/) {
  Application->Minimize();
  }
//---------------------------------------------------------------------------
void __fastcall TServerApp::AddIcon() {
  // Use the Shell_NotifyIcon API function to add the icon to the tray.
  NOTIFYICONDATA IconData;
  IconData.cbSize = sizeof(NOTIFYICONDATA);
  IconData.uID    = IDC_TRAY1;
  IconData.hWnd   = Handle;
  IconData.uFlags = NIF_MESSAGE|NIF_ICON|NIF_TIP;
  IconData.uCallbackMessage = WM_TRAYNOTIFY;
  lstrcpy(IconData.szTip, HINT_MESSAGE);
  IconData.hIcon  = TrayIcon->Handle;

  Shell_NotifyIcon(NIM_ADD,&IconData);
  }

void __fastcall TServerApp::RemoveIcon() {
  NOTIFYICONDATA IconData;
  IconData.cbSize = sizeof(NOTIFYICONDATA);
  IconData.uID    = IDC_TRAY1;
  IconData.hWnd   = Handle;
  IconData.hIcon  = TrayIcon->Handle;

  Shell_NotifyIcon(NIM_DELETE,&IconData);
  }

//---------------------------------------------------------------------------
void __fastcall TServerApp::AppOnMinimize(TObject */*Sender*/) {
  Ticker->Enabled = false;
  ShowWindow(Application->Handle, SW_HIDE);
  Visible = false;
  }

void __fastcall TServerApp::AppOnRestore(TObject */*Sender*/) {
  ShowWindow(Application->Handle, SW_SHOW);
  Visible = true;
  SetForegroundWindow(Handle);
  UpdateDisplay();                  // force immediate update of modules list
  Ticker->Enabled = true;
  }

//---------------------------------------------------------------------------
void __fastcall TServerApp::ModListSelectCell(TObject */*Sender*/, int /*ACol*/,
      int /*ARow*/, bool &CanSelect) {
  CanSelect = false;
  }

//---------------------------------------------------------------------------
#define CODE_PB         1       /* present and busy */
#define CODE_PF         2       /* present and free */
#define CODE_MB         3       /* missing and busy */
#define CODE_MF         4       /* missing and free */

#define PB_STR AnsiString("Busy"       )
#define PF_STR AnsiString("Free"       )
#define MB_STR AnsiString("Busy/Disc." )
#define MF_STR AnsiString("Free/Disc." )
void __fastcall TServerApp::UpdateDisplay(void) {

  int ModCount = 0;
  RL_RESULT r = RL_ModCount(h, &ModCount);
  if (r != RL_OK)
    return;

  list<string> NamesAndFlags;

  for (int i=0; i<ModCount; i++) {
    char name[USB_NAME_LENGTH+1], flags[FLAGS_LENGTH+1];
    memset(name,  0, sizeof(name) );
    memset(flags, 0, sizeof(flags));
    r = RL_ModInfo(h, i, name, flags);
    if (r != RL_OK)
      break;
    string n = name;
    string f = flags;
    NamesAndFlags.push_back(n+f);
    }
  NamesAndFlags.sort();
  // and NamesAndFlags.unique(); if there could be a duplicate
  ModCount = NamesAndFlags.size();

  bool Changed = false;

  if (ModList->RowCount != (ModCount+1)) {
    Changed = true;
    ModList->RowCount = ModCount +1;
    }

  for (int ix=0; ix<ModCount; ix++) {
    int row = ix+1;
    string nf = NamesAndFlags.front();
    NamesAndFlags.pop_front();

    string name = nf.substr(0, USB_NAME_LENGTH);
    AnsiString NameCell = AnsiString(name.c_str());
    if (NameCell != ModList->Cells[0][row]) {
      Changed = true;
      ModList->Cells[0][row] = NameCell;
      }

    TModuleType t = (TModuleType)nf[USB_NAME_LENGTH];
    char InUse    = (char)nf[USB_NAME_LENGTH+1];
    char Present  = (char)nf[USB_NAME_LENGTH+2];

    AnsiString x;
    switch (t) {
      case MT_ANT8   : x = "Ant8";   break;
      case MT_ANT16  : x = "Ant16";  break;
      case MT_ANT18E : x = "Ant18e"; break;
      case MT_BBX34  : x = "X34";    break;
      default:         x = "Unrecognised";
      }
    if (x != ModList->Cells[1][row]) {
      Changed = true;
      ModList->Cells[1][row] = x;
      }

    int code;
    if (Present=='p')
      code = (InUse=='b') ? CODE_PB : CODE_PF;
    else      // missing!
      code = (InUse=='b') ? CODE_MB : CODE_MF;
    ModList->Objects[0][row] = (TObject *)code;

    switch (code) {
      case CODE_PB : x = PB_STR; break;
      case CODE_PF : x = PF_STR; break;
      case CODE_MB : x = MB_STR; break;
      case CODE_MF : x = MF_STR; break;
      }
    if (x != ModList->Cells[2][row]) {
      Changed = true;
      ModList->Cells[2][row] = x;
      }
    }

  if (Changed)
    ModList->Invalidate();
  }

//----------------------------------------------------------------------
void __fastcall TServerApp::TickerTick(TObject */*Sender*/) {
  if (RestoreApp)
    Application->Restore();
  else if (MinimizeApp)
    Application->Minimize();
  else if (Visible)
    UpdateDisplay();

  RestoreApp = false;
  MinimizeApp = false;
  }

//---------------------------------------------------------------------
#define ABS(x)          (((x)<0)?(-(x)):(x))
void __fastcall TServerApp::ModListDrawCell(TObject */*Sender*/, int ACol,
      int ARow, TRect &Rect, TGridDrawState /*AState*/) {
  TCanvas *pC = ModList->Canvas;            // shorthand

  TPenStyle   OldPenStyle    = pC->Pen->Style;
  TBrushStyle OldBrushStyle  = pC->Brush->Style;
  TColor      OldBrushColour = pC->Brush->Color;
  TColor      OldFontColour  = pC->Font->Color;

  int YMiddle = (Rect.Top  + Rect.Bottom)/2;
  int TextYPos = YMiddle - (ABS(pC->Font->Height)/2);

  int code = (int)ModList->Objects[0][ARow];

  TColor c;
  if (ARow == 0)
    c = (TColor)0x00FF8080;
  else
    switch (code) {
      case CODE_PB : c = (TColor)0x008080FF; break;
      case CODE_PF : c = (TColor)0x0080FF80; break;
      case CODE_MB : c = clYellow;           break;
      case CODE_MF : c = clYellow;           break;
      }
  pC->Brush->Color = c;
  pC->FillRect(Rect);

  int t = pC->TextWidth(ModList->Cells[ACol][ARow]);
  int TextXPos = Rect.Left + (ModList->ColWidths[ACol] - t)/2;
  pC->TextOut(TextXPos, TextYPos, ModList->Cells[ACol][ARow] );

  pC->Pen->Style   = OldPenStyle;
  pC->Brush->Style = OldBrushStyle;
  pC->Brush->Color = OldBrushColour;
  pC->Font->Color  = OldFontColour;
  }
// EOF ----------------------------------------------------------------------

