@echo off

rd /s /q public\\extends

cd cmonitor.web
call npm install
call npm run build 
cd ../ 

cd cmonitor.web.client
call npm install
call npm run build 
cd ../ 


for %%r in (win-x64,win-arm64) do (
	dotnet publish ./cmonitor.sas.service -c release -f net8.0 -o public/extends/%%r/  -r %%r -p:PublishTrimmed=true  -p:TrimMode=partial  --self-contained true -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true
	dotnet publish ./cmonitor.snatch.win -c release -f net8.0-windows -r %%r -o public/extends/%%r/plugins/snatch  -p:PublishSingleFile=true --self-contained false
	dotnet publish ./cmonitor.llock.win -c release -f net8.0-windows -r %%r -o public/extends/%%r/plugins/llock  -p:PublishSingleFile=true --self-contained false
	dotnet publish ./cmonitor.message.win -c release -f net8.0-windows -r %%r -o public/extends/%%r/plugins/message  -p:PublishSingleFile=true --self-contained false
	dotnet publish ./cmonitor.notify.win -c release -f net8.0-windows -r %%r -o public/extends/%%r/plugins/notify  -p:PublishSingleFile=true --self-contained false
	dotnet publish ./cmonitor.wallpaper.win -c release -f net8.0-windows -r %%r -o public/extends/%%r/plugins/wallpaper  -p:PublishSingleFile=true --self-contained false
	dotnet restore ./cmonitor.viewer.server.win
	MSBuild.exe ./cmonitor.viewer.server.win /t:Publish /p:Configuration=Release /p:TargetFramework=net8.0-windows /p:PublishSingleFile=true /p:RuntimeIdentifier=%%r /p:PublishDir=../public/extends/%%r/plugins/viewer

	echo F|xcopy "cmonitor.viewer.client.win\\dist\\*" "public\\extends\\%%r\\plugins\\viewer\\*"  /s /f /h /y
	echo F|xcopy "cmonitor.install.win\\dist\\*" "public\\extends\\%%r\\*"  /s /f /h /y
)

for %%r in (win-x64,win-arm64,linux-x64,linux-arm64,osx-x64,osx-arm64) do (
	echo F|xcopy "cmonitor\\plugins\\tuntap\\tun2socks-%%r" "public\\extends\\%%r\\plugins\\tuntap\\tun2socks"  /s /f /h /y
)