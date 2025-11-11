@echo off

SET sdkpath=%1
if "%sdkpath%"=="" (SET sdkpath="D:\\Android\\android-sdk")

cd ../

rd /s /q public\\extends
rd /s /q public\\publish
rd /s /q public\\publish-zip
mkdir public\\publish-zip


cd src/linker.web
call npm install
call npm run build 
cd ../../


echo F|xcopy "shells\\version.txt" "public\\version.txt" /f /h /y

echo F|xcopy "public\\extends\\any\\web\\*" "src\\linker.app\\public\\web\\*"  /s /f /h /y
dotnet publish ./src/linker.app -c:Release -f:net8.0-android /p:AndroidUseApkSigner=true /p:AndroidPackageFormat=apk /p:AndroidKeyStore=true /p:AndroidSigningKeyStore=linker.jks /p:AndroidSigningStorePass=linker /p:AndroidSigningKeyAlias=linker /p:AndroidSigningKeyPass=linker /p:AndroidSdkDirectory=%sdkpath%
echo F|xcopy "src\\linker.app\\bin\\Release\\net8.0-android\\publish\\com.snltty.linker.app-Signed.apk" "public\\publish-zip\\linker.apk"  /s /f /h /y

for %%r in (win-x86,win-x64,win-arm64) do (
	echo F|xcopy "src\\linker.tray.win\\dist\\*" "public\\extends\\%%r\\linker-%%r\\*"  /s /f /h /y
	echo F|xcopy "src\\linker.route.win\\dist\\*" "public\\extends\\%%r\\linker-%%r\\*"  /s /f /h /y
	echo F|xcopy "src\\linker\\msquic.dll" "public\\extends\\%%r\\linker-%%r\\msquic.dll"  /s /f /h /y
	echo F|xcopy "src\\linker\\msquic-%%r.dll" "public\\extends\\%%r\\linker-%%r\\msquic.dll"  /s /f /h /y
	echo F|xcopy "src\\linker\\msquic-openssl3-%%r.dll" "public\\extends\\%%r\\linker-%%r\\msquic-openssl.dll"  /s /f /h /y
	echo F|xcopy "src\\linker\\wintun-%%r.dll" "public\\extends\\%%r\\linker-%%r\\wintun.dll"  /s /f /h /y
)

for %%r in (win-x86,win-x64,win-arm64,linux-x64,linux-arm,linux-arm64,linux-musl-x64,linux-musl-arm,linux-musl-arm64) do (
	
	dotnet publish src/linker -c release -f net8.0 -o public/publish/%%r/linker-%%r  -r %%r  -p:PublishSingleFile=true  --self-contained true  -p:TrimMode=partial -p:TieredPGO=true  -p:DebugType=full -p:EventSourceSupport=false -p:DebugSymbols=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true -p:MetricsSupport=false -p:StackTraceSupport=false -p:XmlResolverIsNetworkingEnabledByDefault=false
    echo F|xcopy "public\\extends\\%%r\\linker-%%r\\*" "public\\publish\\%%r\\linker-%%r\\*"  /s /f /h /y

	echo F|xcopy "public\\extends\\any\\*" "public\\publish\\%%r\\linker-%%r\\*"  /s /f /h /y

	7z a -tzip public/publish-zip/linker-%%r.zip public/publish/%%r/*
)