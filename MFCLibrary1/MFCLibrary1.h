// MFCLibrary1.h : MFCLibrary1 DLL�� �⺻ ��� �����Դϴ�.
//

#pragma once

#ifndef __AFXWIN_H__
	#error "PCH�� ���� �� ������ �����ϱ� ���� 'stdafx.h'�� �����մϴ�."
#endif

#include "resource.h"		// �� ��ȣ�Դϴ�.


// CMFCLibrary1App
// �� Ŭ������ ������ ������ MFCLibrary1.cpp�� �����Ͻʽÿ�.
//

class CMFCLibrary1App : public CWinApp
{
public:
	CMFCLibrary1App();

// �������Դϴ�.
public:
	virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()
};
