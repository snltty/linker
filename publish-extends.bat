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

dotnet publish ./cmonitor.sas.service -c release -f net8.0 -o public/extends/  -r win-x64 -p:PublishTrimmed=true  -p:TrimMode=partial  --self-contained true -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true
dotnet publish ./cmonitor.install.win -c release -f net8.0-windows -r win-x64 -o public/extends/  -p:PublishSingleFile=true --self-contained false
dotnet publish ./cmonitor.snatch.win -c release -f net8.0-windows -r win-x64 -o public/extends/  -p:PublishSingleFile=true --self-contained false
dotnet publish ./cmonitor.llock.win -c release -f net8.0-windows -r win-x64 -o public/extends/  -p:PublishSingleFile=true --self-contained false
dotnet publish ./cmonitor.message.win -c release -f net8.0-windows -r win-x64 -o public/extends/  -p:PublishSingleFile=true --self-contained false
dotnet publish ./cmonitor.notify.win -c release -f net8.0-windows -r win-x64 -o public/extends/  -p:PublishSingleFile=true --self-contained false
dotnet publish ./cmonitor.wallpaper.win -c release -f net8.0-windows -r win-x64 -o public/extends/  -p:PublishSingleFile=true --self-contained false
dotnet restore ./cmonitor.viewer.server.win
MSBuild.exe ./cmonitor.viewer.server.win /t:Publish /p:Configuration=Release /p:TargetFramework=net8.0-windows /p:PublishSingleFile=true /p:RuntimeIdentifier=win-x64 /p:PublishDir=../public/extends/