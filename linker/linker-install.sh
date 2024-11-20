#!/bin/bash

LINKER_DOWNLOAD_URL="https://static.qbcode.cn/downloads/linker"
LINKER_DOWNLOAD_VERSION=""
LINKER_FILE_NAME="linker-linux-"

LINKER_INSTALL_PATH=$1
if [ -z "$LINKER_INSTALL_PATH" ]; then
    LINKER_INSTALL_PATH="/usr/local/bin"
fi
echo -e "安装位置:${LINKER_INSTALL_PATH}"

red='\033[0;31m'
green='\033[0;32m'
yellow='\033[0;33m'
plain='\033[0m'
export PATH=$PATH:/usr/local/bin


install_base() {
    (command -v git >/dev/null 2>&1 && command -v curl >/dev/null 2>&1 && command -v wget >/dev/null 2>&1 && command -v unzip >/dev/null 2>&1 && command -v getenforce >/dev/null 2>&1) ||
        (install_soft curl wget git unzip)
}
install_soft() {
    (command -v yum >/dev/null 2>&1 && yum makecache >/dev/null 2>&1 && yum install $* iproute2  dmidecode net-tools curl traceroute iptables ca-certificates -y >/dev/null 2>&1)  ||
        (command -v apt >/dev/null 2>&1 && apt update >/dev/null 2>&1 && apt install $* iproute2  dmidecode net-tools curl traceroute iptables ca-certificates -y >/dev/null 2>&1) ||
        (command -v pacman >/dev/null 2>&1 && pacman -Syu $* base-devel --noconfirm && install_arch) ||
        (command -v apt-get >/dev/null 2>&1 && apt-get update >/dev/null 2>&1 && apt-get install $* iproute2  dmidecode net-tools curl traceroute iptables ca-certificates -y >/dev/null 2>&1) ||
        (command -v apk >/dev/null 2>&1 && apk update >/dev/null 2>&1 && apk add --no-cache net-tools iproute2 numactl-dev iputils iptables dmidecode -f >/dev/null 2>&1)
}
install_systemd() {
    
    os_arch=""
    os_alpine="0"
    [ -e /etc/os-release ] && cat /etc/os-release | grep -i "PRETTY_NAME" | grep -qi "alpine" && os_alpine='1'
    [ "$os_alpine" != 1 ] && ! command -v systemctl >/dev/null 2>&1 && echo "不支持此系统：未找到 systemctl 命令" && exit 1
    # check root
    [[ $EUID -ne 0 ]] && echo -e "${yellow}===================================================\n${red}错误: 必须使用root用户运行此脚本！${plain}" && exit 1

    ## os_arch
    if [[ $(uname -m | grep 'x86_64') != "" ]]; then
        os_arch="x64"
    elif [[ $(uname -m | grep 'aarch64\|armv8b\|armv8l') != "" ]]; then
        os_arch="arm64"
    elif [[ $(uname -m | grep 'arm') != "" ]]; then
        os_arch="arm"
    fi
    if [ -z "$os_arch" ]; then
        echo -e "${yellow}===================================================\n${red} 仅支持arm arm64 amd64 ${plain}" && exit 1
    fi

    LINKER_FILE_NAME="$LINKER_FILE_NAME$os_arch"
    if [ "$os_alpine" == 1 ]; then
        LINKER_FILE_NAME="$LINKER_FILE_NAME-musl"
    fi

    if [ -e "/etc/systemd/system/linker.service" ]; then
        echo -e "${yellow}===================================================\n${plain}docker已存在linker服务，请自选择：${yellow}\n1. 继续安装\n2. 卸载\n3. 退出${plain}"
        while true; do
            read -e -r -p "请输入选择 [1-2]：" option
            case "${option}" in
                1)
                    break
                    ;;
                2)
                    echo -e "${yellow}===================================================${plain}\n正在移除服务！"
                    systemctl disable linker >/dev/null 2>&1
                    systemctl stop linker >/dev/null 2>&1
                    rm -rf /etc/systemd/system/linker.service
                    echo -e "${green}已移除服务${plain}"
                    exit 1
                    ;;
                3)
                    exit 1
                    ;;
                *)
                    echo "${red}请输入正确的数字 [1-3]${plain}"
                    ;;
            esac
        done
        systemctl disable linker >/dev/null 2>&1
        systemctl stop linker >/dev/null 2>&1
        rm -rf /etc/systemd/system/linker.service >/dev/null 2>&1
    fi

    echo -e "${yellow}===================================================${plain}\n正在安装依赖..."
    install_soft
    echo -e "${green}已安装依赖${plain}"

    echo -e "${yellow}===================================================${plain}\n正在获取版本..."
    LINKER_DOWNLOAD_VERSION=$(curl -m 10 -s $LINKER_DOWNLOAD_URL/version.txt | head -n 1 | tr -d '[:space:]')
    if [ "${LINKER_DOWNLOAD_VERSION:0:1}" != "v" ]; then
    echo -e "${red}获取版本号失败${plain}" && exit 1
    fi
    echo -e "${green}版本号:$LINKER_DOWNLOAD_VERSION${plain}"


    echo -e "${yellow}===================================================${plain}\n正在下载程序..."
    wget -t 2 -T 60 -O ${LINKER_INSTALL_PATH}/${LINKER_FILE_NAME}-${LINKER_DOWNLOAD_VERSION}.zip ${LINKER_DOWNLOAD_URL}/${LINKER_DOWNLOAD_VERSION}/${LINKER_FILE_NAME}.zip >/dev/null 2>&1
    if [[ $? != 0 ]]; then
        echo -e "${red}下载程序失败，请检查本机能否连接 ${LINKER_DOWNLOAD_URL}/${LINKER_DOWNLOAD_VERSION}/${LINKER_FILE_NAME}.zip${plain}" && exit 1
    fi
    echo -e "${green}下载程序完成${plain}"

    echo -e "${yellow}===================================================${plain}\n正在解压..."
    unzip -qo -O UTF-8 "${LINKER_INSTALL_PATH}/${LINKER_FILE_NAME}-${LINKER_DOWNLOAD_VERSION}.zip" -d $LINKER_INSTALL_PATH
    chmod 777 -R ${LINKER_INSTALL_PATH}/$LINKER_FILE_NAME
    echo -e "${green}解压完成${plain}"
    rm -rf ${LINKER_INSTALL_PATH}/${LINKER_FILE_NAME}-${LINKER_DOWNLOAD_VERSION}.zip


    echo -e "${yellow}===================================================${plain}\n正在下载服务文件..."
    wget -t 2 -T 60 -O ${LINKER_INSTALL_PATH}/${LINKER_FILE_NAME}/linker.service ${LINKER_DOWNLOAD_URL}/linker.service >/dev/null 2>&1
    if [[ $? != 0 ]]; then
        echo -e "${red}下载服务文件失败，请检查本机能否连接 ${LINKER_DOWNLOAD_URL}/linker.service${plain}" && exit 1
    fi
    echo -e "${green}下载服务文件完成${plain}"

    cp -f ${LINKER_INSTALL_PATH}/${LINKER_FILE_NAME}/linker.service /etc/systemd/system/linker.service
    sed -i "s|{dir}|$LINKER_INSTALL_PATH/$LINKER_FILE_NAME|g" /etc/systemd/system/linker.service
    systemctl daemon-reload >/dev/null 2>&1
    systemctl enable linker >/dev/null 2>&1
    systemctl restart linker >/dev/null 2>&1

    echo -e "${yellow}===================================================\n1、已安装linker"
    echo -e "2、如果你希望运行为客户端，请使用浏览器打开0.0.0.0:1804，进行初始化，或者systemctl stop linker，然后从别处导出配置将configs文件夹覆盖替换"
    echo -e "3、如果你希望运行为服务端，可以systemctl stop linker，修改configs/server.json配置，然后systemctl start linker再次运行${plain}"
}
install_docker() {
    LINKER_FILE_NAME="linker"
    os_alpine="0"
    [ -e /etc/os-release ] && cat /etc/os-release | grep -i "PRETTY_NAME" | grep -qi "alpine" && os_alpine='1'
    command -v docker >/dev/null 2>&1
    if [[ $? != 0 ]]; then
        echo -e "${yellow}===================================================${plain}\n正在安装 Docker"
        if [ "$os_alpine" != 1 ]; then
            bash <(curl -sL https://get.docker.com -o get-docker.sh) >/dev/null 2>&1
            if [[ $? != 0 ]]; then
                echo -e "${red}下载脚本失败，请检查本机能否连接 https://get.docker.com${plain}"
                return 0
            fi
            systemctl enable docker.service >/dev/null 2>&1
            systemctl start docker.service >/dev/null 2>&1
        else
            apk add docker docker-compose >/dev/null 2>&1
            rc-update add docker
            rc-service docker start
        fi
        echo -e "${green}Docker 安装成功${plain}"
    fi

    LINKER_IMAGES=$(docker ps | grep -w "linker")
    if [ -n "$LINKER_IMAGES" ]; then
        echo -e "${yellow}===================================================${plain}\ndocker已存在linker容器，请自选择：${yellow}\n1. 继续安装\n2. 卸载\n3. 退出${plain}"
        while true; do
            read -e -r -p "请输入选择 [1-3]：" option
            case "${option}" in
                1)
                    break
                    ;;
                2)
                    echo -e "${yellow}===================================================${plain}\n正在移除容器！"
                    docker stop linker >/dev/null 2>&1
                    docker rm linker >/dev/null 2>&1
                    echo -e "${green}已移除容器${plain}"
                    exit 1
                    ;;
                3)
                    exit 1
                    ;;
                *)
                    echo "${red}请输入正确的数字 [1-2]${plain}"
                    ;;
            esac
        done
        docker stop linker >/dev/null 2>&1
        docker rm linker >/dev/null 2>&1
    fi

    echo -e "${yellow}===================================================${plain}\n开始拉取镜像并运行容器，如果镜像拉取失败，你可能需要更换镜像源"

    docker run -it -d --name linker \
    -v $LINKER_INSTALL_PATH/linker/configs:/app/configs \
    -v $LINKER_INSTALL_PATH/linker/logs:/app/logs \
    --device /dev/net/tun \
    --restart=always \
    --privileged=true \
    --network host \
    snltty/linker-musl

    NEZHA_IMAGES=$(docker images --format "{{.Repository}}:{{.Tag}}" | grep -w "snltty/linker")
    if [ -n "$NEZHA_IMAGES" ]; then
        echo -e "${green}docker容器启动成功${plain}" && exit 1
    else
        echo -e "${red}docker容器启动失败${plain}" && exit 1
    fi
}

select_version() {
    if [[ -z $LINKER_IS_DOCKER ]]; then
        echo -e "${yellow}===================================================\n${plain}请自行选择您的安装方式：${yellow}\n1. Docker\n2. 独立安装${plain}"
        while true; do
            read -e -r -p "请输入选择 [1-2]：" option
            case "${option}" in
                1)
                    LINKER_IS_DOCKER=1
                    break
                    ;;
                2)
                    LINKER_IS_DOCKER=0
                    break
                    ;;
                *)
                    echo "${red}请输入正确的数字 [1-2]${plain}"
                    ;;
            esac
        done
    fi
}

install_base
select_version


if [[ $LINKER_IS_DOCKER == 1 ]]; then
    install_docker
elif [[ $LINKER_IS_DOCKER == 0 ]]; then
    install_systemd
fi

