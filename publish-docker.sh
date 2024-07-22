target=$(cd $(dirname $0); pwd)
image="snltty/linker"


fs=('linker')
ps=('musl')
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
			dotnet publish ./${f} -c release -f net8.0 -o ./public/publish/docker/linux-${p}-${r}/${f}  -r ${p}-${r}  --self-contained true -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true  -p:TrimMode=partial
			rm -rf public/publish/docker/linux-${p}-${r}/${f}/${f}
			cp -rf linker/Dockerfile-${p} public/publish/docker/linux-${p}-${r}/${f}/Dockerfile-${p}
			cp -rf public/extends/any/* public/publish/docker/linux-${p}-${r}/${f}/*
			cp -rf linker/plugins/tuntap/tun2socks-${p}-${r} public/publish/docker/linux-${p}-${r}/${f}/tun2socks
		done

		cd public/publish/docker/linux-${p}-x64/${f}
		docker buildx build -f ${target}/public/publish/docker/linux-${p}-x64/${f}/Dockerfile-${p} --platform="linux/x86_64"  --force-rm -t "${image}-${p}-x64" . --push
		cd ../../../../../

		cd public/publish/docker/linux-${p}-arm64/${f}
		docker buildx build -f ${target}/public/publish/docker/linux-${p}-arm64/${f}/Dockerfile-${p} --platform="linux/arm64"  --force-rm -t "${image}-${p}-arm64" . --push
		cd ../../../../../
	done
done