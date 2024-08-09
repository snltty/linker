@echo off

rd /s /q public\\extends
rd /s /q public\\publish
rd /s /q public\\publish-zip
mkdir public\\publish-zip


cd linker.web
call npm install
call npm run build 
cd ../

for %%r in (win-x86,win-x64,win-arm64) do (
	echo F|xcopy "linker.tray.win\\dist\\*" "public\\extends\\%%r\\linker-%%r\\*"  /s /f /h /y
	echo F|xcopy "linker\\msquic.dll" "public\\extends\\%%r\\linker-%%r\\msquic.dll"  /s /f /h /y
	echo F|xcopy "linker\\msquic-%%r.dll" "public\\extends\\%%r\\linker-%%r\\msquic.dll"  /s /f /h /y
	echo F|xcopy "linker\\msquic-openssl3-%%r.dll" "public\\extends\\%%r\\linker-%%r\\msquic-openssl.dll"  /s /f /h /y
	echo F|xcopy "linker\\wintun-%%r.dll" "public\\extends\\%%r\\linker-%%r\\wintun.dll"  /s /f /h /y
)

for %%r in (win-x86,win-x64,win-arm64,linux-x64,linux-arm,linux-arm64,linux-musl-x64,linux-musl-arm,linux-musl-arm64,osx-x64,osx-arm64) do (
	
	dotnet publish ./linker -c release -f net8.0 -o ./public/publish/%%r/linker-%%r  -r %%r  -p:PublishSingleFile=true -p:PublishTrimmed=true  --self-contained true  -p:TrimMode=partial -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true
	echo F|xcopy "public\\extends\\%%r\\linker-%%r\\*" "public\\publish\\%%r\\linker-%%r\\*"  /s /f /h /y

	echo F|xcopy "public\\extends\\any\\*" "public\\publish\\%%r\\linker-%%r\\*"  /s /f /h /y

	7z a -tzip ./public/publish-zip/linker-%%r.zip ./public/publish/%%r/*
)