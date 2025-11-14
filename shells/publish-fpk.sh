target=$(cd $(dirname $0)/..; pwd)

cd ../

mkdir -p public/publish-fpk/docker
cp -rf install-package/docker/* public/publish-fpk/docker/

sed -i "s|{version}|1.9.6|g" public/publish-fpk/docker/manifest
sed -i 's/\r$//' public/publish-fpk/docker/manifest
sed -i 's/\r$//' public/publish-fpk/docker/cmd/main

cd public/publish-fpk/docker

chmod +x src/linker/fnpack
sudo mv src/linker/fnpack /usr/local/bin/fnpack

fnpack build

cd ../../../

: <<'EOF'
rs=('x64')
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


    ((index++))
done
EOF
