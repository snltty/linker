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


extern "C" __declspec(dllexport) IMMDeviceEnumerator * InitSystemDeviceEnumerator()
{
	CoUninitialize();
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
		CLSCTX_INPROC_SERVER,
		__uuidof(IMMDeviceEnumerator),
		(void**)&pEnumerator
	);
	if (FAILED(hr))
	{
		CoUninitialize();
		return NULL;
	}
	return pEnumerator;
}
extern "C" __declspec(dllexport) IMMDevice * InitSystemDevice(IMMDeviceEnumerator * pEnumerator)
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
extern "C" __declspec(dllexport) IAudioEndpointVolume * InitSystemAudioEndpointVolume(IMMDevice * pDevice)
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
		(void**)&pEndpointVolume
	);
	if (FAILED(hr))
	{
		CoUninitialize();
		return NULL;
	}
	return pEndpointVolume;
}
extern "C" __declspec(dllexport) IAudioMeterInformation * InitSystemAudioMeterInformation(IMMDevice * pDevice) {

	if (pDevice == NULL)
	{
		return NULL;
	}

	IAudioMeterInformation* pMeterInfo = NULL;
	HRESULT hr = pDevice->Activate(
		__uuidof(IAudioMeterInformation),
		CLSCTX_ALL,
		nullptr,
		(void**)(&pMeterInfo));

	if (FAILED(hr))
	{
		CoUninitialize();
		return NULL;
	}

	return pMeterInfo;
}

extern "C" __declspec(dllexport) bool FreeSystemDevice(IMMDeviceEnumerator * pEnumerator, IMMDevice * pDevice, IAudioEndpointVolume * pEndpointVolume, IAudioMeterInformation * pMeterInfo)
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
		return 0;
	}

	float volume = 0.0f;
	HRESULT hr = pEndpointVolume->GetMasterVolumeLevelScalar(&volume);
	if (FAILED(hr))
	{
		return 0;
	}
	return volume;
}
extern "C" __declspec(dllexport) bool SetSystemVolume(IAudioEndpointVolume * pEndpointVolume, float volume)
{
	if (pEndpointVolume == NULL)
	{
		return FALSE;
	}

	HRESULT hr = pEndpointVolume->SetMasterVolumeLevelScalar(volume, NULL);
	if (FAILED(hr))
	{
		return FALSE;
	}

	return true;
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

extern "C" __declspec(dllexport) bool GetSystemMute(IAudioEndpointVolume * pEndpointVolume)
{
	if (pEndpointVolume == NULL)
	{
		return FALSE;
	}

	BOOL mute;
	HRESULT hr = pEndpointVolume->GetMute(&mute);
	if (FAILED(hr))
	{
		return FALSE;
	}
	return mute;
}
extern "C" __declspec(dllexport) bool SetSystemMute(IAudioEndpointVolume * pEndpointVolume, BOOL mute)
{
	if (pEndpointVolume == NULL)
	{
		return FALSE;
	}

	HRESULT hr = pEndpointVolume->SetMute(mute, NULL);
	if (FAILED(hr))
	{
		return FALSE;
	}
	return true;
}

