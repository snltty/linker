target=$(cd $(dirname $0); pwd)

cd src/linker.web 
npm install &&
npm run build &&
cd ../../

dotnet publish ./src/linker -c release -f net8.0 -o ./public/publish/loongarch64 -r loongarch64  -p:PublishSingleFile=true  --self-contained true  -p:TrimMode=partial -p:TieredPGO=true  -p:DebugType=none -p:EventSourceSupport=false -p:DebugSymbols=false -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true -p:MetricsSupport=false -p:StackTraceSupport=false -p:XmlResolverIsNetworkingEnabledByDefault=false
cp -rf public/extends/any/web public/publish/loongarch64/web
mkdir -p public/publish/loongarch64/configs
mkdir -p public/publish/loongarch64/logs

zip -r linker-loongarch64.zip public/publish/loongarch64/*