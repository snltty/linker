// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"
#include <Windows.h>
#include <mmdeviceapi.h>
#include <endpointvolume.h>

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


extern "C" __declspec(dllexport) IMMDeviceEnumerator * InitEnumerator()
{
	HRESULT hr;
	hr = CoInitialize(nullptr);
	if (FAILED(hr))
	{
		return NULL;
	}

	IMMDeviceEnumerator* pEnumerator = NULL;
	hr = CoCreateInstance(
		__uuidof(MMDeviceEnumerator),
		NULL,
		CLSCTX_ALL,
		__uuidof(IMMDeviceEnumerator),
		(LPVOID*)&pEnumerator
	);
	if (FAILED(hr))
	{
		return NULL;
	}
	return pEnumerator;
}
extern "C" __declspec(dllexport) IMMDevice * InitDevice(IMMDeviceEnumerator * pEnumerator)
{
	if (pEnumerator == NULL)
	{
		return NULL;
	}

	IMMDevice* pDevice = NULL;
	// 获取默认音频渲染设备
	HRESULT hr = pEnumerator->GetDefaultAudioEndpoint(eRender, eConsole, &pDevice);
	if (FAILED(hr)) {
		// 错误处理
		CoUninitialize();
		return NULL;
	}
	return pDevice;
}
extern "C" __declspec(dllexport) IAudioEndpointVolume * InitVolume(IMMDevice * pDevice)
{
	if (pDevice == NULL)
	{
		return NULL;
	}

	IAudioEndpointVolume* pEndpointVolume = NULL;
	HRESULT hr;
	hr = pDevice->Activate(
		__uuidof(IAudioEndpointVolume),
		CLSCTX_ALL,
		NULL,
		(LPVOID*)&pEndpointVolume
	);
	if (FAILED(hr))
	{
		CoUninitialize();
		return NULL;
	}
	return pEndpointVolume;
}
extern "C" __declspec(dllexport) IAudioMeterInformation * InitMeterInfo(IMMDevice * pDevice) {

	if (pDevice == NULL)
	{
		return NULL;
	}

	IAudioMeterInformation* pMeterInfo = NULL;
	HRESULT hr = pDevice->Activate(
		__uuidof(IAudioMeterInformation),
		CLSCTX_ALL,
		nullptr,
		reinterpret_cast<void**>(&pMeterInfo));

	if (FAILED(hr))
	{
		CoUninitialize();
		return NULL;
	}

	return pMeterInfo;
}

extern "C" __declspec(dllexport) bool FreeVolume(IMMDeviceEnumerator * pEnumerator, IMMDevice * pDevice, IAudioEndpointVolume * pEndpointVolume, IAudioMeterInformation * pMeterInfo)
{
	if (pEnumerator != NULL)
	{
		pEnumerator->Release();
	}

	if (pDevice != NULL)
	{
		pDevice->Release();
	}
	if (pEndpointVolume != NULL)
	{
		pEndpointVolume->Release();
	}
	if (pMeterInfo != NULL)
	{
		pMeterInfo->Release();
	}

	CoUninitialize();
	return true;
}

extern "C" __declspec(dllexport) float GetSystemVolume(IAudioEndpointVolume * pEndpointVolume)
{
	if (pEndpointVolume == NULL)
	{
		return 1;
	}

	float volume = 0.0f;
	HRESULT hr = pEndpointVolume->GetMasterVolumeLevelScalar(&volume);
	if (FAILED(hr))
	{
		return 2;
	}
	return 3;
}

extern "C" __declspec(dllexport) float GetSystemMasterPeak(IAudioMeterInformation * pMeterInfo)
{
	if (pMeterInfo == NULL)
	{
		return 0;
	}

	float peakValue = 0.0f;
	HRESULT hr = pMeterInfo->GetPeakValue(&peakValue);
	if (FAILED(hr))
	{
		return 0;
	}
	return peakValue;
}

extern "C" __declspec(dllexport) int SetSystemVolume(IAudioEndpointVolume * pEndpointVolume, float volume)
{
	if (pEndpointVolume == NULL)
	{
		return false;
	}

	HRESULT hr = pEndpointVolume->SetMasterVolumeLevelScalar(volume, NULL);
	if (FAILED(hr))
	{
		return false;
	}

	return true;
}


extern "C" __declspec(dllexport) bool GetSystemMute(IAudioEndpointVolume * pEndpointVolume)
{
	if (pEndpointVolume == NULL)
	{
		return 0;
	}

	BOOL mute;
	HRESULT hr = pEndpointVolume->GetMute(&mute);
	if (FAILED(hr))
	{
		return mute;
	}
	return mute;
}

extern "C" __declspec(dllexport) bool SetSystemMute(IAudioEndpointVolume * pEndpointVolume, BOOL mute)
{
	if (pEndpointVolume == NULL)
	{
		return 0;
	}

	HRESULT hr = pEndpointVolume->SetMute(mute, NULL);
	if (FAILED(hr))
	{
		return false;
	}
	return true;
}

