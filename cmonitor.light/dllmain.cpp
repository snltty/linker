// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"
#include <Windows.h>
#include <wbemidl.h>

#pragma comment(lib, "wbemuuid.lib")

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

typedef void(__stdcall* BrightnessChangeCallback)(int brightness);

class WmiMonitorBrightnessEventHandler : public IWbemObjectSink
{
public:

	BrightnessChangeCallback g_pfnCallback = nullptr;

	ULONG STDMETHODCALLTYPE AddRef() { return 1; }
	ULONG STDMETHODCALLTYPE Release() { return 1; }
	HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppv) { return S_OK; }

	HRESULT STDMETHODCALLTYPE Indicate(LONG lObjectCount, IWbemClassObject** apObjArray)
	{
		for (int i = 0; i < lObjectCount; i++)
		{
			VARIANT varBrightness;
			HRESULT hr = apObjArray[i]->Get(L"Brightness", 0, &varBrightness, nullptr, nullptr);
			if (SUCCEEDED(hr) && varBrightness.vt == VT_UI1)
			{
				if (g_pfnCallback != NULL) {
					g_pfnCallback(static_cast<int>(varBrightness.bVal));
				}
			}
			VariantClear(&varBrightness);
		}

		return WBEM_S_NO_ERROR;
	}

	HRESULT STDMETHODCALLTYPE SetStatus(LONG lFlags, HRESULT hResult, BSTR strParam, IWbemClassObject* pObjParam)
	{
		return WBEM_S_NO_ERROR;
	}
};


extern "C" __declspec(dllexport) IWbemLocator * InitIWbemLocator()
{
	HRESULT hr;
	hr = CoInitialize(nullptr);
	if (FAILED(hr))
	{
		return NULL;
	}

	// 启动WMI服务
	hr = CoInitializeSecurity(
		NULL,
		-1,
		NULL,
		NULL,
		RPC_C_AUTHN_LEVEL_DEFAULT,
		RPC_C_IMP_LEVEL_IMPERSONATE,
		NULL,
		EOAC_NONE,
		NULL
	);
	if (FAILED(hr))
	{
		CoUninitialize();
		return NULL;
	}

	IWbemLocator* pWbemLocator = NULL;
	hr = CoCreateInstance(
		CLSID_WbemLocator,
		0,
		CLSCTX_INPROC_SERVER,
		IID_IWbemLocator,
		reinterpret_cast<LPVOID*>(&pWbemLocator)
	);
	if (FAILED(hr))
	{
		CoUninitialize();
		return NULL;
	}

	return pWbemLocator;
}
extern "C" __declspec(dllexport) IWbemServices * InitIWbemServices(IWbemLocator * pWbemLocator)
{
	HRESULT hr;
	// 连接WMI服务
	IWbemServices* pWbemServices = NULL;
	BSTR str = SysAllocString(L"ROOT\\WMI");
	hr = pWbemLocator->ConnectServer(
		str,
		NULL,
		NULL,
		0,
		NULL,
		0,
		0,
		&pWbemServices
	);
	if (FAILED(hr))
	{
		SysFreeString(str);
		pWbemLocator->Release();
		CoUninitialize();
		return NULL;
	}
	SysFreeString(str);
	return pWbemServices;
}

extern "C" __declspec(dllexport) HRESULT GetBrightness(IWbemServices* pServices, BYTE* pBrightness)
{
	HRESULT hResult = S_OK;

	// 构建查询语句
	BSTR query = SysAllocString(L"SELECT * FROM WmiMonitorBrightness");
	BSTR wql = SysAllocString(L"WQL");

	// 执行查询并获取结果集
	IEnumWbemClassObject* pEnumerator = nullptr;
	hResult = pServices->ExecQuery(wql, query, WBEM_FLAG_FORWARD_ONLY | WBEM_FLAG_RETURN_IMMEDIATELY, nullptr, &pEnumerator);
	if (SUCCEEDED(hResult))
	{
		IWbemClassObject* pObject = nullptr;
		ULONG uReturned = 0;

		// 遍历结果集
		while ((hResult = pEnumerator->Next(WBEM_INFINITE, 1, &pObject, &uReturned)) == S_OK)
		{
			VARIANT varBrightness;

			// 获取亮度值属性
			hResult = pObject->Get(L"CurrentBrightness", 0, &varBrightness, nullptr, nullptr);
			if (SUCCEEDED(hResult) && varBrightness.vt == VT_UI1)
			{
				*pBrightness = varBrightness.bVal;
				break;
			}

			pObject->Release();
		}

		if (hResult == WBEM_S_FALSE)
		{
			hResult = E_FAIL;
		}

		pEnumerator->Release();
	}

	SysFreeString(wql);
	SysFreeString(query);

	return hResult;
}
extern "C" __declspec(dllexport) HRESULT SetBrightness(IWbemServices * pServices, BYTE brightness)
{
	HRESULT hResult = S_OK;

	// 构建查询语句
	BSTR query = SysAllocString(L"SELECT * FROM WmiMonitorBrightnessMethods");
	BSTR wql = SysAllocString(L"WQL");
	BSTR str1 = SysAllocString(L"WmiSetBrightness");

	// 执行查询并获取结果集
	IEnumWbemClassObject* pEnumerator = nullptr;
	hResult = pServices->ExecQuery(wql, query, WBEM_FLAG_FORWARD_ONLY | WBEM_FLAG_RETURN_IMMEDIATELY, nullptr, &pEnumerator);
	if (SUCCEEDED(hResult))
	{
		IWbemClassObject* pObject = nullptr;
		ULONG uReturned = 0;

		// 遍历结果集
		while ((hResult = pEnumerator->Next(WBEM_INFINITE, 1, &pObject, &uReturned)) == S_OK)
		{
			// 获取 WmiMonitorBrightnessMethods 类的实例
			VARIANT varInstance;
			hResult = pObject->Get(L"__PATH", 0, &varInstance, nullptr, nullptr);
			if (SUCCEEDED(hResult) && varInstance.vt == VT_BSTR)
			{
				IWbemClassObject* pBrightnessMethodsObject = nullptr;
				hResult = pServices->GetObject(varInstance.bstrVal, 0, nullptr, &pBrightnessMethodsObject, nullptr);
				if (SUCCEEDED(hResult))
				{
					// 获取设置亮度的方法
					VARIANT varMethod;
					hResult = pBrightnessMethodsObject->Get(L"WmiSetBrightness", 0, &varMethod, nullptr, nullptr);
					if (SUCCEEDED(hResult) && varMethod.vt == VT_UNKNOWN)
					{
						// 执行设置亮度的方法
						IWbemClassObject* pSetBrightnessMethodObject = nullptr;
						hResult = varMethod.punkVal->QueryInterface(IID_IWbemClassObject, (LPVOID*)&pSetBrightnessMethodObject);
						if (SUCCEEDED(hResult))
						{
							// 设置方法调用参数
							VARIANT varBrightness;
							varBrightness.vt = VT_UI1;
							varBrightness.bVal = brightness;

							hResult = pSetBrightnessMethodObject->Put(L"Brightness", 0, &varBrightness, 0);
							if (SUCCEEDED(hResult))
							{
								// 调用方法并获取返回值
								IWbemClassObject* pOutParams = nullptr;
								hResult = pServices->ExecMethod(varInstance.bstrVal, str1,
									0, nullptr, pSetBrightnessMethodObject, &pOutParams, nullptr);
								if (SUCCEEDED(hResult))
								{
									// 处理返回值
									VARIANT varReturnValue;
									hResult = pOutParams->Get(L"ReturnValue", 0, &varReturnValue, nullptr, nullptr);
									if (SUCCEEDED(hResult) && varReturnValue.vt == VT_UINT &&
										varReturnValue.uintVal == 0)
									{
									}
									else
									{
										hResult = E_FAIL;
									}

									if (pOutParams)
									{
										pOutParams->Release();
									}
								}
							}

							pSetBrightnessMethodObject->Release();
						}
					}

					pBrightnessMethodsObject->Release();
				}
			}

			pObject->Release();
		}

		if (hResult == WBEM_S_FALSE)
		{
			hResult = E_FAIL;
		}

		pEnumerator->Release();
	}

	SysFreeString(wql);
	SysFreeString(query);
	SysFreeString(str1);

	return hResult;
}


extern "C" __declspec(dllexport) bool FreeLight(IWbemLocator * pWbemLocator, IWbemServices * pWbemServices, IWbemObjectSink * pEventSink)
{
	if (pWbemLocator != NULL)
	{
		pWbemLocator->Release();
	}

	if (pWbemServices != NULL)
	{
		pWbemServices->Release();
	}
	if (pEventSink != NULL)
	{
		pEventSink->Release();
	}
	CoUninitialize();
	return true;
}

extern "C" __declspec(dllexport) IWbemObjectSink * StartListener(IWbemServices * pWbemServices, BrightnessChangeCallback g_pfnCallback)
{
	HRESULT hr;
	// 设置安全性
	hr = CoSetProxyBlanket(
		pWbemServices,
		RPC_C_AUTHN_WINNT,
		RPC_C_AUTHZ_NONE,
		NULL,
		RPC_C_AUTHN_LEVEL_CALL,
		RPC_C_IMP_LEVEL_IMPERSONATE,
		NULL,
		EOAC_NONE
	);
	if (FAILED(hr))
	{
		pWbemServices->Release();
		CoUninitialize();
		return NULL;
	}

	// 创建事件接收器
	WmiMonitorBrightnessEventHandler* pEventSinkHandle = new WmiMonitorBrightnessEventHandler;
	pEventSinkHandle->g_pfnCallback = g_pfnCallback;
	IWbemObjectSink* pEventSink = pEventSinkHandle;
	

	// 注册事件接收器
	hr = pWbemServices->ExecNotificationQueryAsync(
		SysAllocString(L"WQL"),
		SysAllocString(L"SELECT * FROM WmiMonitorBrightnessEvent"),
		WBEM_FLAG_SEND_STATUS,
		NULL,
		pEventSink
	);
	if (FAILED(hr))
	{
		pEventSink->Release();
		pWbemServices->Release();
		CoUninitialize();
		return NULL;
	}
	return pEventSink;
}