#!/usr/bin/env bash

target=$(cd $(dirname $0)/..; pwd)

rs=('x64' 'arm64' 'arm')
index=0
APK_CMD=${APK_CMD:-apk}

cd src/linker.web 
npm install &&
npm run build &&
cd ../../

for r in ${rs[@]} 
do
    dotnet publish src/linker -c release -f net8.0 -o public/publish/${r} -r linux-musl-${r} -p:PublishSingleFile=true  --self-contained true  -p:TrimMode=partial -p:TieredPGO=true  -p:DebugType=full -p:EventSourceSupport=false -p:DebugSymbols=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true -p:MetricsSupport=false -p:StackTraceSupport=false -p:XmlResolverIsNetworkingEnabledByDefault=false
    cp -rf public/extends/any/web public/publish/${r}/web
    mkdir -p public/publish/${r}/configs
    mkdir -p public/publish/${r}/logs
    cp -rf src/linker/libmsquic-musl-${r}.so public/publish/${r}/libmsquic.so

    mkdir -p public/publish-ipk/${r}
    cp -rf install-package/ipk/package/* public/publish-ipk/${r}/
    cp -rf install-package/ipk/libs/${r}/* public/publish-ipk/${r}/data/   
    mkdir -p public/publish-ipk/${r}/data/usr/bin/linker
    cp -rf public/publish/${r}/* public/publish-ipk/${r}/data/usr/bin/linker/

    sed -i "s|{version}|1.9.99|g" public/publish-ipk/${r}/control/control
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
    tar -czf linker-openwrt-${r}.ipk debian-binary data.tar.gz control.tar.gz
    cd ../../../

    mkdir -p public/publish-apk/${r}
    cp -rf install-package/apk/package/* public/publish-apk/${r}/
    cp -rf install-package/ipk/libs/${r}/* public/publish-apk/${r}/data/
    mkdir -p public/publish-apk/${r}/data/usr/bin/linker
    cp -rf public/publish/${r}/* public/publish-apk/${r}/data/usr/bin/linker/

    sed -i "s|{version}|1.9.99|g" public/publish-apk/${r}/control/package.info
    sed -i "s|{apk_arch}|noarch|g" public/publish-apk/${r}/control/package.info
    sed -i 's/\r$//' public/publish-apk/${r}/data/etc/init.d/linker
    sed -i 's/\r$//' public/publish-apk/${r}/control/package.info
    sed -i 's/\r$//' public/publish-apk/${r}/control/.post-install
    sed -i 's/\r$//' public/publish-apk/${r}/control/.pre-deinstall

    chmod +x public/publish-apk/${r}/data/etc/init.d/linker
    chmod +x public/publish-apk/${r}/control/.post-install
    chmod +x public/publish-apk/${r}/control/.pre-deinstall

    cd public/publish-apk/${r}
    mkdir -p data/lib/apk/packages
    (cd data && find . \( -type f -o -type l \) | sed 's|^\./|/|' | sort > ../linker.list)
    mv linker.list data/lib/apk/packages/linker.list

    apk_info_args=()
    while IFS= read -r line; do
        [ -n "${line}" ] && apk_info_args+=(--info "${line}")
    done < control/package.info

    "${APK_CMD}" mkpkg "${apk_info_args[@]}" \
        --script "post-install:control/.post-install" \
        --script "post-upgrade:control/.post-install" \
        --script "pre-deinstall:control/.pre-deinstall" \
        --files data \
        --output linker-openwrt-${r}.apk
    cd ../../../

    ((index++))
done
