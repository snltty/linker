target=$(cd $(dirname $0); pwd)
image="snltty/linker"


fs=('linker')
ps=('musl' 'debian')
rs=('x64' 'arm64')

cd linker.web 
npm install &&
npm run build &&
cd ../

for f in ${fs[@]} 
do
	for p in ${ps[@]} 
	do
		for r in ${rs[@]} 
		do
            rr=linux-${p}-${r}
            if [ $p = "debian" ]
            then
                rr=linux-${r}
            fi
			dotnet publish ./${f} -c release -f net8.0 -o ./public/publish/docker/linux-${p}-${r}/${f}  -r ${rr}  --self-contained true -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true  -p:TrimMode=partial
			cp -rf linker/Dockerfile-${p} public/publish/docker/linux-${p}-${r}/${f}/Dockerfile-${p}
			cp -rf public/extends/any/web public/publish/docker/linux-${p}-${r}/${f}/web
            mkdir -p public/publish/docker/linux-${p}-${r}/${f}/plugins/tuntap
            mkdir -p public/publish/docker/linux-${p}-${r}/${f}/configs
            mkdir -p public/publish/docker/linux-${p}-${r}/${f}/logs
			cp -rf linker/plugins/tuntap/tun2socks-linux-${r} public/publish/docker/linux-${p}-${r}/${f}/plugins/tuntap/tun2socks
            if [ $p = "musl" ]
            then
                cp -rf linker/libmsquic-${r}.so public/publish/docker/linux-${p}-${r}/${f}/libmsquic.so
            fi
		done

		cd public/publish/docker/linux-${p}-x64/${f}
		docker buildx build -f ${target}/public/publish/docker/linux-${p}-x64/${f}/Dockerfile-${p} --platform="linux/x86_64"  --force-rm -t "${image}-${p}-x64" . --push
		cd ../../../../../

		cd public/publish/docker/linux-${p}-arm64/${f}
		docker buildx build -f ${target}/public/publish/docker/linux-${p}-arm64/${f}/Dockerfile-${p} --platform="linux/arm64"  --force-rm -t "${image}-${p}-arm64" . --push
		cd ../../../../../
	done
done