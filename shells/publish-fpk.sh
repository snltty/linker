target=$(cd $(dirname $0)/..; pwd)


cd src/linker.web 
npm install &&
npm run build &&
cd ../../

rs=('x64')
for r in ${rs[@]} 
do

    mkdir -p public/publish-fpk/docker/${r}
    cp -rf install-package/fpk/docker/* public/publish-fpk/docker/${r}

    sed -i "s|{version}|1.9.7|g" public/publish-fpk/docker/${r}/manifest
    sed -i 's/\r$//' public/publish-fpk/docker/${r}/manifest
    sed -i 's/\r$//' public/publish-fpk/docker/${r}/cmd/main
    sed -i 's/\r$//' public/publish-fpk//${r}/cmd/uninstall_callback

    cd public/publish-fpk/docker/${r}

    tar -czf app.tgz --transform='s,app/,,g' app/docker app/ui config
    tar -czf linker.fpk --exclude='app' *
    mv linker.fpk linker-docker-x64.fpk

    cd ../../../../


    dotnet publish src/linker -c release -f net8.0 -o public/publish/${r} -r linux-${r}  -p:PublishSingleFile=true  --self-contained true  -p:TrimMode=partial -p:TieredPGO=true  -p:DebugType=none -p:EventSourceSupport=false -p:DebugSymbols=false -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true -p:MetricsSupport=false -p:StackTraceSupport=false -p:XmlResolverIsNetworkingEnabledByDefault=false
    cp -rf public/extends/any/web public/publish/${r}/web
    mkdir -p public/publish/${r}/configs
    mkdir -p public/publish/${r}/logs

    mkdir -p public/publish-fpk/bin/${r}
    
    cp -rf install-package/fpk/fbin/* public/publish-fpk/bin/${r}/
    mkdir -p public/publish-fpk/bin/${r}/app/server
    mkdir -p public/publish-fpk/bin/${r}/app/www
    cp -rf public/publish/${r}/* public/publish-fpk/bin/${r}/app/server/

    sed -i "s|{version}|1.9.7|g" public/publish-fpk/bin/${r}/manifest
    sed -i 's/\r$//' public/publish-fpk/bin/${r}/manifest
    sed -i 's/\r$//' public/publish-fpk/bin/${r}/cmd/main
    sed -i 's/\r$//' public/publish-fpk/bin/${r}/cmd/uninstall_callback

    cd public/publish-fpk/bin/${r}

    tar -czf app.tgz --transform='s,app/,,g' app/server app/ui app/www config
    tar -czf linker.fpk --exclude='app' *
    mv linker.fpk linker-bin-${r}.fpk

    cd ../../../../
done
