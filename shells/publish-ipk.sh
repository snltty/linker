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
    
    mkdir -p public/publish-ipk/${r}
    cp -rf install-package/ipk/package/* public/publish-ipk/${r}/
    cp -rf install-package/ipk/libs/${r}/* public/publish-ipk/${r}/data/   
    mkdir -p public/publish-ipk/${r}/data/usr/bin/linker
    cp -rf public/publish/${r}/* public/publish-ipk/${r}/data/usr/bin/linker/

    sed -i "s|{version}|2.0.17|g" public/publish-ipk/${r}/control/control
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

    apk_info_file=public/publish-apk/${r}/control/package.info
    if [ ! -f "${apk_info_file}" ] && [ -f public/publish-apk/${r}/control/.PKGINFO ]; then
        awk -F ' = ' '
            /^#/ || NF < 2 { next }
            $1 == "pkgname" { print "name:" $2; next }
            $1 == "pkgver" { print "version:" $2; next }
            $1 == "pkgdesc" { print "description:" $2; next }
            $1 == "arch" { print "arch:" $2; next }
            $1 == "license" { print "license:" $2; next }
            $1 == "origin" { print "origin:" $2; next }
            $1 == "url" { print "url:" $2; next }
            $1 == "maintainer" { print "maintainer:" $2; next }
            $1 == "depend" { deps = deps ? deps " " $2 : $2; next }
            END { if (deps) print "depends:" deps }
        ' public/publish-apk/${r}/control/.PKGINFO > "${apk_info_file}"
    fi

    sed -i "s|{version}|2.0.17|g" "${apk_info_file}"
    sed -i "s|{apk_arch}|noarch|g" "${apk_info_file}"
    sed -i 's/\r$//' public/publish-apk/${r}/data/etc/init.d/linker
    sed -i 's/\r$//' "${apk_info_file}"
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
        line=${line%%#*}
        line=$(printf '%s' "${line}" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')
        [ -z "${line}" ] && continue

        if printf '%s' "${line}" | grep -q '^[^:=][^:=]*[[:space:]]*=[[:space:]]*'; then
            key=$(printf '%s' "${line}" | sed 's/[[:space:]]*=.*//')
            value=$(printf '%s' "${line}" | sed 's/^[^=]*=[[:space:]]*//')
            case "${key}" in
                pkgname) key=name ;;
                pkgver) key=version ;;
                pkgdesc) key=description ;;
                depend) key=depends ;;
            esac
            line="${key}:${value}"
        fi

        apk_info_args+=(--info "${line}")
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
