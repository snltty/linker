target=$(cd $(dirname $0)/..; pwd)


cd src/linker.web 
npm install &&
npm run build &&
cd ../../

rs=('linux-loongarch64')
for r in ${rs[@]} 
do

    dotnet publish src/linker -c release -f net8.0 -o public/publish/${r} -r linux-${r} -p:PublishSingleFile=true  --self-contained true  -p:TrimMode=partial -p:TieredPGO=true  -p:DebugType=full -p:EventSourceSupport=false -p:DebugSymbols=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true -p:MetricsSupport=false -p:StackTraceSupport=false -p:XmlResolverIsNetworkingEnabledByDefault=false
    cp -rf public/extends/any/web public/publish/${r}/web
    mkdir -p public/publish/${r}/configs
    mkdir -p public/publish/${r}/logs

    tar -czvf linker-${r}.tar.gz public/publish/${r}

    cd ../../../../
done
