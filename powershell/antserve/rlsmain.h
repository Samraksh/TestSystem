// rlsmain.h ----------------------------------------------------------------
//
// Copyright (c) 2006  te
//
// Version:
//
//      01 Feb 2006    sb    Created.
//
//---------------------------------------------------------------------------
#ifndef rlsmainH
#define rlsmainH
//---------------------------------------------------------------------------
#include <Classes.hpp>
#include <Controls.hpp>
#include <StdCtrls.hpp>
#include <Forms.hpp>
#include <Menus.hpp>
#include <ExtCtrls.hpp>
#include <Grids.hpp>

#define WM_TRAYNOTIFY  (WM_USER + 1001)

class TServerApp : public TForm {
  __published:                        // IDE-managed Components
    TPopupMenu *PopupMenu1;
    TMenuItem *CloseMnu;
    TButton *CloseBtn;
    TMenuItem *RestoreBtn;
    TMenuItem *Sep1;
    TStringGrid *ModList;
    TLabel *Copyright;
    TButton *CancelButton;
    TLabel *PortLabel;

    void __fastcall CloseBtnClick(TObject *Sender);
    void __fastcall CancelButtonClick(TObject *Sender);
    void __fastcall RestoreBtnClick(TObject *Sender);

    void __fastcall TickerTick(TObject *Sender);

    void __fastcall ModListDrawCell(TObject *Sender, int ACol, int ARow,
          TRect &Rect, TGridDrawState State);
    void __fastcall ModListSelectCell(TObject *Sender, int ACol, int ARow,
          bool &CanSelect);

  private:                            // User declarations
    Graphics::TIcon *TrayIcon;
    void __fastcall WMTrayNotify(TMessage &Msg);
    void __fastcall RemoveIcon();
    void __fastcall AddIcon();
    void __fastcall AppOnMinimize(TObject *Sender);
    void __fastcall AppOnRestore(TObject *Sender);

    bool __fastcall SetCell(AnsiString& old, AnsiString val);
    void __fastcall UpdateDisplay(void);

    TTimer*       Ticker;
    bool          RestoreApp,
                  MinimizeApp;
    RL_HANDLE     h;

  public:     // User declarations
    __fastcall TServerApp(TComponent* Owner);
    __fastcall ~TServerApp();

  BEGIN_MESSAGE_MAP
    MESSAGE_HANDLER(WM_TRAYNOTIFY, TMessage, WMTrayNotify)
  END_MESSAGE_MAP(TForm)
  };
//---------------------------------------------------------------------------
#endif
