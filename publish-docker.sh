target=$(cd $(dirname $0); pwd)
image="snltty/cmonitor"


fs=('cmonitor')
ps=('alpine')
rs=('x64' 'arm64')


for f in ${fs[@]} 
do
	for p in ${ps[@]} 
	do
		for r in ${rs[@]} 
		do
			dotnet publish ./${f} -c release -f net7.0 -o ./public/publish/docker/linux-${p}-${r}/${f}  -r ${p}-${r}  --self-contained true -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true  -p:TrimMode=partial
			cp -rf public/publish/docker/linux-${p}-${r}/${f}/${f} public/publish/docker/linux-${p}-${r}/${f}/${f}.run
			rm -rf public/publish/docker/linux-${p}-${r}/${f}/${f}
			cp -rf cmonitor/Dockerfile-${p} public/publish/docker/linux-${p}-${r}/${f}/Dockerfile-${p}
			cp -rf cmonitor/publish/web public/publish/docker/linux-${p}-${r}/${f}
		done

		cd public/publish/docker/linux-${p}-x64/${f}
		docker buildx build -f ${target}/public/publish/docker/linux-${p}-x64/${f}/Dockerfile-${p} --platform="linux/x86_64"  --force-rm -t "${image}-${p}-x64" . --push
		cd ../../../../../

		cd public/publish/docker/linux-${p}-arm64/${f}
		docker buildx build -f ${target}/public/publish/docker/linux-${p}-arm64/${f}/Dockerfile-${p} --platform="linux/arm64"  --force-rm -t "${image}-${p}-arm64" . --push
		cd ../../../../../
	done
done