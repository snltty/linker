@echo off

rd /s /q public\\publish
		
dotnet publish ./cmonitor -c release -f net7.0 -o ./public/publish/win-x64  -r win-x64  --self-contained true -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true  -p:TrimMode=partial

dotnet publish ./cmonitor -c releaselinux -f net7.0 -o ./public/publish/linux-x64  -r linux-x64 -p:PublishTrimmed=true  --self-contained true -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false -p:PublishSingleFile=true  -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true  -p:TrimMode=partial

dotnet publish ./cmonitor -c release -f net7.0 -r win-x64 -o ./public/publish/win-x64-any/  -p:PublishSingleFile=true --self-contained false
dotnet publish ./cmonitor -c releaselinux -f net7.0 -r linux-x64 -o ./public/publish/linux-x64-any/  -p:PublishSingleFile=true --self-contained false

echo F|xcopy "cmonitor\\web\\llock.win.exe" "public\\publish\\win-x64\\llock.win.exe"  /s /f /h /y
echo F|xcopy "cmonitor\\web\\message.win.exe" "public\\publish\\win-x64\\message.win.exe"  /s /f /h /y
echo F|xcopy "cmonitor\\web\\wallpaper.win.exe" "public\\publish\\win-x64\\wallpaper.win.exe"  /s /f /h /y
echo F|xcopy "cmonitor\\web\\cmonitor.win.exe" "public\\publish\\win-x64\\cmonitor.win.exe"  /s /f /h /y

for %%r in (linux-x64,linux-x64-any) do (
	for %%f in (nfapi.dll,nfdriver.sys) do (
		del  "public\\publish\\%%r\\%%f"
	)
)