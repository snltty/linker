target=$(cd $(dirname $0)/..; pwd)

cd ../

rs=('x64' 'arm64' 'arm')
index=0

cd src/linker.web 
npm install &&
npm run build &&
cd ../../

for r in ${rs[@]} 
do
    dotnet publish src/linker -c release -f net8.0 -o public/publish/${r} -r linux-musl-${r}  -p:PublishSingleFile=true  --self-contained true  -p:TrimMode=partial -p:TieredPGO=true  -p:DebugType=none -p:EventSourceSupport=false -p:DebugSymbols=false -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true -p:MetricsSupport=false -p:StackTraceSupport=false -p:XmlResolverIsNetworkingEnabledByDefault=false
    cp -rf public/extends/any/web public/publish/${r}/web
    mkdir -p public/publish/${r}/configs
    mkdir -p public/publish/${r}/logs
    cp -rf src/linker/libmsquic-musl-${r}.so public/publish/${r}/libmsquic.so

    mkdir -p public/publish-ipk/${r}
    cp -rf install-package/ipk/package/* public/publish-ipk/${r}/
    cp -rf install-package/ipk/libs/${r}/* public/publish-ipk/${r}/data/   
    mkdir -p public/publish-ipk/${r}/data/usr/bin/linker
    cp -rf public/publish/${r}/* public/publish-ipk/${r}/data/usr/bin/linker/

    sed -i "s|{version}|{{version}}|g" public/publish-ipk/${r}/control/control
    sed -i 's/\r$//' public/publish-ipk/${r}/data/etc/init.d/linker
    sed -i 's/\r$//' public/publish-ipk/${r}/control/control
    sed -i 's/\r$//' public/publish-ipk/${r}/control/postinst
    sed -i 's/\r$//' public/publish-ipk/${r}/control/prerm

    chmod +x public/publish-ipk/${r}/data/etc/init.d/linker
    chmod +x public/publish-ipk/${r}/control/control
    chmod +x public/publish-ipk/${r}/control/postinst
    chmod +x public/publish-ipk/${r}/control/prerm

    cd public/publish-ipk/${r}
    tar -czf data.tar.gz -C data/ .
    tar -czf control.tar.gz -C control/ .
    echo "2.0" > debian-binary
    tar -czf linker-${r}.ipk debian-binary data.tar.gz control.tar.gz
    cd ../../../

    ((index++))
done