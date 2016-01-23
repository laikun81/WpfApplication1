// ArkDll.cpp : DLL ���� ���α׷��� ���� ������ �Լ��� �����մϴ�.
//

#include "stdafx.h"
#include "ArkDll.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// ������ ���� ���α׷� ��ü�Դϴ�.

CWinApp theApp;
int _tmain(int argc, TCHAR* argv[], TCHAR* envp[])
{
	


	int nRetCode = 0;

	HMODULE hModule = ::GetModuleHandle(NULL);
	
	if (hModule != NULL)
	{
		// MFC�� �ʱ�ȭ�մϴ�. �ʱ�ȭ���� ���� ��� ������ �μ��մϴ�.
		if (!AfxWinInit(hModule, NULL, ::GetCommandLine(), 0))
		{
			// TODO: ���� �ڵ带 �ʿ信 ���� �����մϴ�.
			_tprintf(_T("�ɰ��� ����: MFC�� �ʱ�ȭ���� ���߽��ϴ�.\n"));
			nRetCode = 1;
		}
		else
		{
			// TODO: ���� ���α׷��� ������ ���⿡�� �ڵ��մϴ�.
		}
	}
	else
	{
		// TODO: ���� �ڵ带 �ʿ信 ���� �����մϴ�.
		_tprintf(_T("�ɰ��� ����: GetModuleHandle ����\n"));
		nRetCode = 1;
	}

	return nRetCode;
}

CArkLib _ark;
ArkEvent _evt;
ArkInStream _in;
ArkOutStream _out;
IArkCompressor *_comp;

template  <typename T>
int SetCB(T *cb, T _cb){
	if (!_cb){
		return 0;
	}
	*cb = _cb;
	return 1;
}

int SetCB_DebugCallBack(DebugCallBack _cb){ return _debugCallBack ? SetCB(&_debugCallBack, _cb) : 0; }

int SetCB_evt_OnOpening(ArkEvent::cb_OnOpening _cb){ return SetCB(&_evt._OnOpening, _cb); }
int SetCB_evt_OnStartFile(ArkEvent::cb_OnStartFile _cb){ return SetCB(&_evt._OnStartFile, _cb); }
int SetCB_evt_OnProgressFile(ArkEvent::cb_OnProgressFile _cb){ return SetCB(&_evt._OnProgressFile, _cb); }
int SetCB_evt_OnCompleteFile(ArkEvent::cb_OnCompleteFile _cb){ return SetCB(&_evt._OnCompleteFile, _cb); }
int SetCB_evt_OnError(ArkEvent::cb_OnError _cb){ return SetCB(&_evt._OnError, _cb); }
int SetCB_evt_OnMultiVolumeFileChanged(ArkEvent::cb_OnMultiVolumeFileChanged _cb){ return SetCB(&_evt._OnMultiVolumeFileChanged, _cb); }
int SetCB_evt_OnAskOverwrite(ArkEvent::cb_OnAskOverwrite _cb){ return SetCB(&_evt._OnAskOverwrite, _cb); }
int SetCB_evt_OnAskPassword(ArkEvent::cb_OnAskPassword _cb){ return SetCB(&_evt._OnAskPassword, _cb); }

int SetCB_out_Open(ArkOutStream::cb_Open _cb){ return SetCB(&_out._Open, _cb); }
int SetCB_out_SetSize(ArkOutStream::cb_SetSize _cb){ return SetCB(&_out._SetSize, _cb); }
int SetCB_out_Write(ArkOutStream::cb_Write _cb){ return SetCB(&_out._Write, _cb); }
int SetCB_out_Close(ArkOutStream::cb_Close _cb){ return SetCB(&_out._Close, _cb); }
int SetCB_out_CreateFolder(ArkOutStream::cb_CreateFolder _cb){ return SetCB(&_out._CreateFolder, _cb); }

int SetCB_in_Read(ArkInStream::cb_Read _cb){ return SetCB(&_in._Read, _cb); }
int SetCB_in_SetPos(ArkInStream::cb_SetPos _cb){ return SetCB(&_in._SetPos, _cb); }
int SetCB_in_GetPos(ArkInStream::cb_GetPos _cb){ return SetCB(&_in._GetPos, _cb); }
int SetCB_in_GetSize(ArkInStream::cb_GetSize _cb){ return SetCB(&_in._GetSize, _cb); }
int SetCB_in_Close(ArkInStream::cb_Close _cb){ return SetCB(&_in._Close, _cb); }

ArkEvent::ArkEvent(){}
void ArkEvent::OnOpening(const SArkFileItem* pFileItem, float progress, BOOL32& bStop){ _evt._OnOpening(pFileItem, progress, bStop); }
void ArkEvent::OnStartFile(const SArkFileItem* pFileItem, BOOL32& bStopCurrent, BOOL32& bStopAll, int index){ _evt._OnStartFile(pFileItem, bStopCurrent, bStopAll, index); }
void ArkEvent::OnProgressFile(const SArkProgressInfo* pProgressInfo, BOOL& bStopCurrent, BOOL& bStopAll){ _evt._OnProgressFile(pProgressInfo, bStopCurrent, bStopAll); }
void ArkEvent::OnCompleteFile(const SArkProgressInfo* pProgressInfo, ARKERR nErr){ _evt._OnCompleteFile(pProgressInfo, nErr); }
void ArkEvent::OnError(ARKERR nErr, const SArkFileItem* pFileItem, BOOL bIsWarning, BOOL& bStopAll){ _evt._OnError(nErr, pFileItem, bIsWarning, bStopAll); }
void ArkEvent::OnMultiVolumeFileChanged(LPCWSTR szPathFileName){ _evt._OnMultiVolumeFileChanged(szPathFileName); }
void ArkEvent::OnAskOverwrite(const SArkFileItem* pFileItem, LPCWSTR szLocalPathName, ARK_OVERWRITE_MODE& overwrite, WCHAR pathName2Rename[ARK_MAX_PATH]){ _evt._OnAskOverwrite(pFileItem, szLocalPathName, overwrite, pathName2Rename); }
void ArkEvent::OnAskPassword(const SArkFileItem* pFileItem, ARK_PASSWORD_ASKTYPE askType, ARK_PASSWORD_RET& ret, WCHAR passwordW[ARK_MAX_PASS]){ _evt._OnAskPassword(pFileItem, askType, ret, passwordW); }

ArkInStream::ArkInStream(){}
//�Է� ��Ʈ������ ����Ÿ�� ������ ȣ��˴ϴ�. 
BOOL32 ArkInStream::Read(void* lpBuffer, UINT32 nNumberOfBytesToRead, UINT32* lpNumberOfBytesRead){ return _in._Read(lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead); }
//�Է� ��Ʈ������ ����Ÿ�� �д� ��ġ�� �ٲٰ��� �� �� ȣ��˴ϴ�. 
INT64 ArkInStream::SetPos(INT64 pos){ return _in._SetPos(pos); }
//�Է� ��Ʈ������ ���� ��ġ�� �˰��� �� �� ȣ��˴ϴ�. 
INT64 ArkInStream::GetPos(){ return _in._GetPos(); }
//�Է� ��Ʈ���� ũ�⸦ �˰��� �� �� ȣ��˴ϴ�. 
INT64 ArkInStream::GetSize(){ return _in._GetSize(); }
//�Է� ��Ʈ���� �ݰ��� �� �� ȣ��˴ϴ�. 
BOOL32 ArkInStream::Close(){ return _in._Close(); }
ArkOutStream::ArkOutStream(){}
//���� ������ �����ϱ� ���ؼ� ��� ��Ʈ���� ���� ȣ��˴ϴ�. 
BOOL32 ArkOutStream::Open(LPCWSTR szPathName){ return _out._Open(szPathName); }
//��� ��Ʈ���� ����Ÿ�� �������� �� ũ�⸦ �̸� �˷��ֱ� ���ؼ� ȣ��Ǹ�, �޸� ���ۿ� ������ Ǯ ��쿡 �����մϴ�. 
BOOL32 ArkOutStream::SetSize(INT64 nSize){ return _out._SetSize(nSize); }
//��� ��Ʈ���� ����Ÿ�� ������ �� �� ȣ��˴ϴ�. 
BOOL32 ArkOutStream::Write(const void* lpBuffer, UINT32 nNumberOfBytesToWrite){ return _out._Write(lpBuffer, nNumberOfBytesToWrite); }
//���� ������ ����Ǿ ������ �ڵ��� �ݰ��� �� �� ȣ��˴ϴ�. 
BOOL32 ArkOutStream::Close(){ return _out._Close(); }
//���Ͼ������� ������ ������ �����Ҷ� ȣ��˴ϴ�. 
BOOL32 ArkOutStream::CreateFolder(LPCWSTR szPathName){ return _out._CreateFolder(szPathName); }

ARKERR Init(){
	ARKERR result = _ark.Create(ARK_DLL_RELEASE_FILE_NAME);
	if (result == ARKERR_NOERR)
		_ark.SetEvent(&_evt);
	return result;
}
BOOL32 IsCreated(){ return _ark.IsCreated(); }
void Destroy(){ return _ark.Destroy(); }

void Release(void){ return _ark.Release(); }
BOOL32 Open(LPCWSTR filePath, LPCWSTR password){ return _ark.Open(filePath, password ? password : NULL); }
BOOL32 OpenStream(ARKBYTE* srcStream, int srcLen, LPCWSTR password){ return _ark.Open(srcStream, srcLen, password ? password : NULL); }
BOOL32 IsBrokenArchive(){ return _ark.IsBrokenArchive(); }
BOOL32 IsSolidArchive(){ return _ark.IsSolidArchive(); }
void Close(void){ return _ark.Close(); }
void SetPassword(LPCWSTR password){ return _ark.SetPassword(password); }
int GetFileItemCount(){ return _ark.GetFileItemCount(); }
const SArkFileItem *GetFileItem(INT index){ return _ark.GetFileItem(index); }
ARK_FF GetFileFormat(){ return _ark.GetFileFormat(); }
BOOL32 IsEncryptedArchive(){ return _ark.IsEncryptedArchive(); }
BOOL32 IsOpened(){ return _ark.IsOpened(); }
BOOL32 ExtractAllTo(LPCWSTR folderPath){ return _ark.ExtractAllTo(folderPath); }
BOOL32 ExtractAllToStream(){ return _ark.ExtractAllTo(&_out); }
BOOL32 ExtractOneTo(int index, LPCWSTR folderPath){ return _ark.ExtractOneTo(index, folderPath); }
BOOL32 ExtractOneToStream(int index){ return _ark.ExtractOneTo(index, &_out); }
BOOL32 ExtractOneToBytes(int index, BYTE* outBuf, int outBufLen){ return _ark.ExtractOneTo(index, outBuf, outBufLen); }
const ARKERR GetLastErrorArk(){ return _ark.GetLastError(); }
LPCWSTR FileFormat2Str(ARK_FF ff){ return _ark.FileFormat2Str(ff); }
void SetGlobalOpt(const SArkGlobalOpt& opt){ return _ark.SetGlobalOpt(opt); }
INT64 GetArchiveFileSize(){ return _ark.GetArchiveFileSize(); }
LPCWCH GetFilePathName(){ return _ark.GetFilePathName(); }
UINT32 GetLastSystemError(){ return _ark.GetLastSystemError(); }
BOOL32 TestArchive(){ return _ark.TestArchive(); }

void CompressorInit(void){
	_comp = _ark.CreateCompressor();
	_comp->Init();
	_comp->SetEvent(&_evt);
	return;
}
void CompressorRelease(){ return _comp->Release(); }
BOOL32 SetOption(SArkCompressorOpt& opt, const BYTE* password, int pwLen){ return _comp->SetOption(opt, password, pwLen); }
BOOL32 SetArchiveFile(IArk* pArchive){ return _comp->SetArchiveFile(pArchive); }
BOOL32 AddFileItem(LPCWSTR szSrcPathName, LPCWSTR szTargetPathName, BOOL32 overwrite, LPCWSTR szFileComment = NULL){ return _comp->AddFileItem(szSrcPathName, szTargetPathName, overwrite, szFileComment); }
BOOL32 RenameItem(int index, LPCWSTR szPathName){ return _comp->RenameItem(index, szPathName); }
BOOL32 DeleteItem(int index){ return _comp->DeleteItem(index); }
int FindFileItemIndex2Add(LPCWSTR szTargetPathName){ return _comp->FindFileItemIndex2Add(szTargetPathName); }
INT64 GetTotalFileSize2Archive(){ return _comp->GetTotalFileSize2Archive(); }
BOOL32 CreateArchive(LPCWSTR szArchivePathName, LPCWSTR szArchiveComment = NULL){ return _comp->CreateArchive(szArchivePathName, szArchiveComment); }
const ARKERR GetLastErrorCompressor(){ return _comp->GetLastError(); }

//    HDC screenDC = ::GetDC(0);
//    ::Rectangle(screenDC, 200, 200, 300, 300);
//::ReleaseDC(0, screenDC);
