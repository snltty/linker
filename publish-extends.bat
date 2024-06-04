@echo off

rd /s /q public\\extends
rd /s /q public\\publish
rd /s /q public\\publish-zip
mkdir public\\publish-zip

cd cmonitor.web
call npm install
call npm run build 
cd ../ 

for %%c in (ReleaseMonitor) do (
	echo F|xcopy "public\\extends\\any\\web\\*" "public\\extends\\%%c\\any\\web\\*"  /s /f /h /y
)

cd cmonitor.web.client
call npm install
call npm run build 
cd ../ 

for %%c in (ReleaseNetwork) do (
	echo F|xcopy "public\\extends\\any\\web-client\\*" "public\\extends\\%%c\\any\\web-client\\*"  /s /f /h /y
)

for %%r in (win-x64,win-arm64) do (
	
	dotnet publish ./cmonitor.sas.service -c release -f net8.0 -o public/extends/release/%%r/  -r %%r -p:PublishTrimmed=true  -p:TrimMode=partial  --self-contained true -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false  -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true
	echo F|xcopy "public\\extends\\release\\%%r\\cmonitor.sas.service.exe" "public\\extends\\ReleaseMonitor\\%%r\\cmonitor.service.exe"  /s /f /h /y
	
	dotnet publish ./cmonitor.network.service -c release -f net8.0 -o public/extends/release/%%r/  -r %%r -p:PublishTrimmed=true  -p:TrimMode=partial  --self-contained true -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false  -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true
	echo F|xcopy "public\\extends\\release\\%%r\\cmonitor.network.service.exe" "public\\extends\\ReleaseNetwork\\%%r\\cmonitor.service.exe"  /s /f /h /y

	dotnet publish ./cmonitor.snatch.win -c release -f net8.0-windows -r %%r -o public/extends/release/%%r/plugins/snatch  -p:PublishSingleFile=true --self-contained false
	echo F|xcopy "public\\extends\\release\\%%r\\plugins\\snatch\\*" "public\\extends\\ReleaseMonitor\\%%r\\plugins\\snatch\\*"  /s /f /h /y

	dotnet publish ./cmonitor.llock.win -c release -f net8.0-windows -r %%r -o public/extends/release/%%r/plugins/llock  -p:PublishSingleFile=true --self-contained false
	echo F|xcopy "public\\extends\\release\\%%r\\plugins\\llock\\*" "public\\extends\\ReleaseMonitor\\%%r\\plugins\\llock\\*"  /s /f /h /y

	dotnet publish ./cmonitor.message.win -c release -f net8.0-windows -r %%r -o public/extends/release/%%r/plugins/message  -p:PublishSingleFile=true --self-contained false
	echo F|xcopy "public\\extends\\release\\%%r\\plugins\\message\\*" "public\\extends\\ReleaseMonitor\\%%r\\plugins\\message\\*"  /s /f /h /y

	dotnet publish ./cmonitor.notify.win -c release -f net8.0-windows -r %%r -o public/extends/release/%%r/plugins/notify  -p:PublishSingleFile=true --self-contained false
	echo F|xcopy "public\\extends\\release\\%%r\\plugins\\notify\\*" "public\\extends\\ReleaseMonitor\\%%r\\plugins\\notify\\*"  /s /f /h /y

	dotnet publish ./cmonitor.wallpaper.win -c release -f net8.0-windows -r %%r -o public/extends/release/%%r/plugins/wallpaper  -p:PublishSingleFile=true --self-contained false
	echo F|xcopy "public\\extends\\release\\%%r\\plugins\\wallpaper\\*" "public\\extends\\ReleaseMonitor\\%%r\\plugins\\wallpaper\\*"  /s /f /h /y

	dotnet restore ./cmonitor.viewer.server.win
	MSBuild.exe ./cmonitor.viewer.server.win /t:Publish /p:Configuration=Release /p:TargetFramework=net8.0-windows /p:PublishSingleFile=true /p:RuntimeIdentifier=%%r /p:PublishDir=../public/extends/release/%%r/plugins/viewer
	echo F|xcopy "public\\extends\\release\\%%r\\plugins\\viewer\\*" "public\\extends\\ReleaseMonitor\\%%r\\plugins\\viewer\\*"  /s /f /h /y


	for %%c in (ReleaseMonitor) do (
		echo F|xcopy "cmonitor.viewer.client.win\\dist\\*" "public\\extends\\%%c\\%%r\\plugins\\viewer\\*"  /s /f /h /y
	)
                for %%c in (ReleaseNetwork,ReleaseMonitor) do (
		echo F|xcopy "cmonitor.tray.win\\dist\\*" "public\\extends\\%%c\\%%r\\*"  /s /f /h /y
	)
)

for %%r in (win-x64,win-arm64) do (
	for %%c in (ReleaseMonitor,ReleaseNetwork) do (
		echo F|xcopy "cmonitor\\msquic.dll" "public\\publish\\%%c\\%%r\\single\\msquic.dll"  /s /f /h /y
		echo F|xcopy "cmonitor\\msquic-openssl.dll" "public\\publish\\%%c\\%%r\\single\\msquic-openssl.dll"  /s /f /h /y
	)
)

for %%r in (win-x64,win-arm64,linux-x64,linux-arm64,osx-x64,osx-arm64) do (
	for %%c in (ReleaseNetwork) do (
		echo F|xcopy "cmonitor\\plugins\\tuntap\\tun2socks-%%r" "public\\extends\\%%c\\%%r\\plugins\\tuntap\\tun2socks"  /s /f /h /y
	)
)