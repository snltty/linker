@echo off

rd /s /q public\\publish
rd /s /q public\\publish-zip
mkdir public\\publish-zip
	

for %%r in (win-x64,win-arm64,linux-x64,linux-arm64,osx-x64,osx-arm64) do (
	dotnet publish ./cmonitor -c release -f net8.0 -o ./public/publish/%%r/single  -r %%r -p:PublishTrimmed=true  -p:TrimMode=partial  --self-contained true -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true
	dotnet publish ./cmonitor -c release -f net8.0 -r %%r -o ./public/publish/%%r/any/  -p:PublishSingleFile=true --self-contained false

	echo F|xcopy "public\\extends\\%%r\\*" "public\\publish\\%%r\\single\\*"  /s /f /h /y
	echo F|xcopy "public\\extends\\%%r\\*" "public\\publish\\%%r\\any\\*"  /s /f /h /y	

	echo F|xcopy "public\\extends\\any\\*" "public\\publish\\%%r\\single\\*"  /s /f /h /y
	echo F|xcopy "public\\extends\\any\\*" "public\\publish\\%%r\\any\\*"  /s /f /h /y	

	7z a -tzip ./public/publish-zip/cmonitor-%%r.zip ./public/publish/%%r/*
)