// 다음 ifdef 블록은 DLL에서 내보내기하는 작업을 쉽게 해 주는 매크로를 만드는 
// 표준 방식입니다. 이 DLL에 들어 있는 파일은 모두 명령줄에 정의된 _EXPORTS 기호로
// 컴파일되며, 다른 프로젝트에서는 이 기호를 정의할 수 없습니다.
// 이렇게 하면 소스 파일에 이 파일이 들어 있는 다른 모든 프로젝트에서는 
// ARKDLL_API 함수를 DLL에서 가져오는 것으로 보고, 이 DLL은
// 이 DLL은 해당 매크로로 정의된 기호가 내보내지는 것으로 봅니다.
#include"ArkLib.h"

#ifdef ARKDLL_EXPORTS
#define ARKDLL_API __declspec(dllexport)
#else
#define ARKDLL_API __declspec(dllimport)
#endif

// 이 클래스는 ArkDll.dll에서 내보낸 것입니다.
class ARKDLL_API CArkDll {
public:
	CArkDll(void);
	// TODO: 여기에 메서드를 추가합니다.
};

extern ARKDLL_API int nArkDll;

ARKDLL_API int fnArkDll(void);

typedef void(__stdcall *DebugCallBack)(WCHAR *txt);
DebugCallBack _debugCallBack;
extern "C" ARKDLL_API int SetCB_DebugCallBack(DebugCallBack _cb);
extern "C" ARKDLL_API int DelCB_DebugCallBack();

class ArkEvent : public IArkEvent
{
public:
	ArkEvent();
	ARKMETHOD(void) OnOpening(const SArkFileItem* pFileItem, float progress, BOOL32& bStop);
	ARKMETHOD(void) OnStartFile(const SArkFileItem* pFileItem, BOOL32& bStopCurrent, BOOL32& bStopAll, int index);
	ARKMETHOD(void) OnProgressFile(const SArkProgressInfo* pProgressInfo, BOOL32 &bStopCurrent, BOOL32 &bStopAll);
	ARKMETHOD(void) OnCompleteFile(const SArkProgressInfo* pProgressInfo, ARKERR nErr);
	ARKMETHOD(void) OnError(ARKERR nErr, const SArkFileItem* pFileItem, BOOL32 bIsWarning, BOOL32& bStopAll);
	ARKMETHOD(void) OnMultiVolumeFileChanged(LPCWSTR szPathFileName);
	ARKMETHOD(void) OnAskOverwrite(const SArkFileItem* pFileItem, LPCWSTR szLocalPathName, ARK_OVERWRITE_MODE& overwrite, WCHAR pathName2Rename[ARK_MAX_PATH]);
	ARKMETHOD(void) OnAskPassword(const SArkFileItem* pFileItem, ARK_PASSWORD_ASKTYPE askType, ARK_PASSWORD_RET& ret, WCHAR passwordW[ARK_MAX_PASS]);

	typedef void(__stdcall *cb_OnOpening)(const SArkFileItem* pFileItem, float progress, BOOL32 &bStop);
	typedef void(__stdcall *cb_OnStartFile)(const SArkFileItem* pFileItem, BOOL32& bStopCurrent, BOOL32& bStopAll, int index);
	typedef void(__stdcall *cb_OnProgressFile)(const SArkProgressInfo* pProgressInfo, BOOL32& bStopCurrent, BOOL32& bStopAll);
	typedef void(__stdcall *cb_OnCompleteFile)(const SArkProgressInfo* pProgressInfo, ARKERR nErr);
	typedef void(__stdcall *cb_OnError)(ARKERR nErr, const SArkFileItem* pFileItem, BOOL32 bIsWarning, BOOL32& bStopAll);
	typedef void(__stdcall *cb_OnMultiVolumeFileChanged)(LPCWSTR szPathFileName);
	typedef void(__stdcall *cb_OnAskOverwrite)(const SArkFileItem* pFileItem, LPCWSTR szLocalPathName, ARK_OVERWRITE_MODE& overwrite, WCHAR pathName2Rename[ARK_MAX_PATH]);
	typedef void(__stdcall *cb_OnAskPassword)(const SArkFileItem* pFileItem, ARK_PASSWORD_ASKTYPE askType, ARK_PASSWORD_RET& ret, WCHAR passwordW[ARK_MAX_PASS]);

	cb_OnOpening _OnOpening;
	cb_OnStartFile _OnStartFile;
	cb_OnProgressFile _OnProgressFile;
	cb_OnCompleteFile _OnCompleteFile;
	cb_OnError _OnError;
	cb_OnMultiVolumeFileChanged _OnMultiVolumeFileChanged;
	cb_OnAskOverwrite _OnAskOverwrite;
	cb_OnAskPassword _OnAskPassword;
};
extern "C" ARKDLL_API int SetCB_evt_OnOpening(ArkEvent::cb_OnOpening _cb);
extern "C" ARKDLL_API int SetCB_evt_OnStartFile(ArkEvent::cb_OnStartFile _cb);
extern "C" ARKDLL_API int SetCB_evt_OnProgressFile(ArkEvent::cb_OnProgressFile _cb);
extern "C" ARKDLL_API int SetCB_evt_OnCompleteFile(ArkEvent::cb_OnCompleteFile _cb);
extern "C" ARKDLL_API int SetCB_evt_OnError(ArkEvent::cb_OnError _cb);
extern "C" ARKDLL_API int SetCB_evt_OnMultiVolumeFileChanged(ArkEvent::cb_OnMultiVolumeFileChanged _cb);
extern "C" ARKDLL_API int SetCB_evt_OnAskOverwrite(ArkEvent::cb_OnAskOverwrite _cb);
extern "C" ARKDLL_API int SetCB_evt_OnAskPassword(ArkEvent::cb_OnAskPassword _cb);

class ArkInStream : public IArkSimpleInStream
{
public:
	ArkInStream();
	ARKMETHOD(BOOL32) Read(void* lpBuffer, UINT32 nNumberOfBytesToRead, UINT32* lpNumberOfBytesRead);
	ARKMETHOD(INT64) SetPos(INT64 pos);
	ARKMETHOD(INT64) GetPos();
	ARKMETHOD(INT64) GetSize();
	ARKMETHOD(BOOL32) Close();

	typedef BOOL32(__stdcall *cb_Read)(void* lpBuffer, UINT32 nNumberOfBytesToRead, UINT32* lpNumberOfBytesRead);
	typedef INT64(__stdcall *cb_SetPos)(INT64 pos);
	typedef INT64(__stdcall *cb_GetPos)(void);
	typedef INT64(__stdcall *cb_GetSize)(void);
	typedef BOOL32(__stdcall *cb_Close)(void);

	cb_Read _Read;
	cb_SetPos _SetPos;
	cb_GetPos _GetPos;
	cb_GetSize _GetSize;
	cb_Close _Close;
};
extern "C" ARKDLL_API int SetCB_in_Read(ArkInStream::cb_Read _cb);
extern "C" ARKDLL_API int SetCB_in_SetPos(ArkInStream::cb_SetPos _cb);
extern "C" ARKDLL_API int SetCB_in_GetPos(ArkInStream::cb_GetPos _cb);
extern "C" ARKDLL_API int SetCB_in_GetSize(ArkInStream::cb_GetSize _cb);
extern "C" ARKDLL_API int SetCB_in_Close(ArkInStream::cb_Close _cb);

class ArkOutStream : public IArkSimpleOutStream
{
public:
	ArkOutStream();
	//압축 해제를 시작하기 위해서 출력 스트림을 열때 호출됩니다. 
	ARKMETHOD(BOOL32) Open(LPCWSTR szPathName);
	//출력 스트림에 데이타를 쓰기전에 그 크기를 미리 알려주기 위해서 호출되며, 메모리 버퍼에 압축을 풀 경우에 유용합니다. 
	ARKMETHOD(BOOL32) SetSize(INT64 nSize);
	//출력 스트림에 데이타를 쓰고자 할 때 호출됩니다. 
	ARKMETHOD(BOOL32) Write(const void* lpBuffer, UINT32 nNumberOfBytesToWrite);
	//압축 해제가 종료되어서 파일을 핸들을 닫고자 할 때 호출됩니다. 
	ARKMETHOD(BOOL32) Close();
	//파일아이템중 폴더의 압축을 해제할때 호출됩니다. 
	ARKMETHOD(BOOL32) CreateFolder(LPCWSTR szPathName);

	typedef BOOL32(__stdcall *cb_Open)(LPCWSTR szPathName);
	typedef BOOL32(__stdcall *cb_SetSize)(INT64 nSize);
	typedef BOOL32(__stdcall *cb_Write)(const void* lpBuffer, UINT32 nNumberOfBytesToWrite);
	typedef BOOL32(__stdcall *cb_Close)();
	typedef BOOL32(__stdcall *cb_CreateFolder)(LPCWSTR szPathName);

	cb_Open _Open;
	cb_SetSize _SetSize;
	cb_Write _Write;
	cb_Close _Close;
	cb_CreateFolder _CreateFolder;
};
extern "C" ARKDLL_API int SetCB_out_Open(ArkOutStream::cb_Open _cb);
extern "C" ARKDLL_API int SetCB_out_SetSize(ArkOutStream::cb_SetSize _cb);
extern "C" ARKDLL_API int SetCB_out_Write(ArkOutStream::cb_Write _cb);
extern "C" ARKDLL_API int SetCB_out_Close(ArkOutStream::cb_Close _cb);
extern "C" ARKDLL_API int SetCB_out_CreateFolder(ArkOutStream::cb_CreateFolder _cb);

//CArkLib
extern "C" ARKDLL_API ARKERR Init(void);
extern "C" ARKDLL_API BOOL32 IsCreated();
extern "C" ARKDLL_API void Destroy();

//IArk
extern "C" ARKDLL_API void Release(void);
extern "C" ARKDLL_API BOOL32 Open(LPCWSTR filePath, LPCWSTR password);
extern "C" ARKDLL_API BOOL32 OpenStream(ARKBYTE* srcStream, int srcLen, LPCWSTR password);
extern "C" ARKDLL_API BOOL32 IsBrokenArchive();
extern "C" ARKDLL_API BOOL32 IsSolidArchive();
extern "C" ARKDLL_API void Close(void);
extern "C" ARKDLL_API void SetPassword(LPCWSTR password);
extern "C" ARKDLL_API int GetFileItemCount();
extern "C" ARKDLL_API const SArkFileItem *GetFileItem(INT index);
extern "C" ARKDLL_API ARK_FF GetFileFormat();
extern "C" ARKDLL_API BOOL32 IsEncryptedArchive();
extern "C" ARKDLL_API BOOL32 IsSolidArchive();
extern "C" ARKDLL_API BOOL32 IsOpened();
extern "C" ARKDLL_API BOOL32 ExtractAllTo(LPCWSTR folderPath);
extern "C" ARKDLL_API BOOL32 ExtractAllToStream();
extern "C" ARKDLL_API BOOL32 ExtractOneTo(int index, LPCWSTR folderPath);
extern "C" ARKDLL_API BOOL32 ExtractOneToStream(int index);
extern "C" ARKDLL_API BOOL32 ExtractOneToBytes(int index, BYTE* outBuf, int outBufLen);
extern "C" ARKDLL_API const ARKERR GetLastErrorArk();
extern "C" ARKDLL_API LPCWSTR FileFormat2Str(ARK_FF ff);
extern "C" ARKDLL_API void SetGlobalOpt(const SArkGlobalOpt& opt);
extern "C" ARKDLL_API INT64 GetArchiveFileSize();
extern "C" ARKDLL_API LPCWCH GetFilePathName();
extern "C" ARKDLL_API UINT32 GetLastSystemError();
extern "C" ARKDLL_API BOOL32 TestArchive(void);

//IArkCompressor
extern "C" ARKDLL_API void CompressorInit(void);
extern "C" ARKDLL_API void CompressorRelease(void);
extern "C" ARKDLL_API BOOL32 CompressorSetOption(SArkCompressorOpt& opt, const BYTE* password, int pwLen);
extern "C" ARKDLL_API BOOL32 SetOption(SArkCompressorOpt& opt, const BYTE* password, int pwLen);
extern "C" ARKDLL_API BOOL32 SetArchiveFile(IArk* pArchive);
extern "C" ARKDLL_API BOOL32 AddFileItem(LPCWSTR szSrcPathName, LPCWSTR szTargetPathName, BOOL32 overwrite, LPCWSTR szFileComment);
extern "C" ARKDLL_API BOOL32 RenameItem(int index, LPCWSTR szPathName);
extern "C" ARKDLL_API BOOL32 DeleteItem(int index);
extern "C" ARKDLL_API int FindFileItemIndex2Add(LPCWSTR szTargetPathName);
extern "C" ARKDLL_API INT64 GetTotalFileSize2Archive();
extern "C" ARKDLL_API BOOL32 CreateArchive(LPCWSTR szArchivePathName, LPCWSTR szArchiveComment);
extern "C" ARKDLL_API const ARKERR GetLastErrorCompressor();