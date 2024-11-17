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


pre_check() {

    os_arch=""
    os_alpine="0"
    [ -e /etc/os-release ] && cat /etc/os-release | grep -i "PRETTY_NAME" | grep -qi "alpine" && os_alpine='1'

    [ "$os_alpine" != 1 ] && ! command -v systemctl >/dev/null 2>&1 && echo "不支持此系统：未找到 systemctl 命令" && exit 1

    # check root
    [[ $EUID -ne 0 ]] && echo -e "${red}错误: ${plain} 必须使用root用户运行此脚本！\n" && exit 1

    ## os_arch
    if [[ $(uname -m | grep 'x86_64') != "" ]]; then
        os_arch="x64"
    elif [[ $(uname -m | grep 'aarch64\|armv8b\|armv8l') != "" ]]; then
        os_arch="arm64"
    elif [[ $(uname -m | grep 'arm') != "" ]]; then
        os_arch="arm"
    fi

    if [ -z "$os_arch" ]; then
        echo -e "${red} 仅支持arm arm64 amd64 ${plain}" && exit 1
    fi

    LINKER_FILE_NAME="$LINKER_FILE_NAME$os_arch"
    if [ "$os_alpine" == 1 ]; then
        LINKER_FILE_NAME="$LINKER_FILE_NAME-musl"
    fi

    echo -e "正在获取版本..."
    LINKER_DOWNLOAD_VERSION=$(curl -m 10 -s $LINKER_DOWNLOAD_URL/version.txt | head -n 1 | tr -d '[:space:]')
    if [ "${LINKER_DOWNLOAD_VERSION:0:1}" != "v" ]; then
    echo -e "${red}获取版本号失败${plain}" && exit 1
    fi
    echo -e "${green}版本号:$LINKER_DOWNLOAD_VERSION${plain}"


    echo -e "正在下载程序..."
    wget -t 2 -T 60 -O ${LINKER_INSTALL_PATH}/${LINKER_FILE_NAME}-${LINKER_DOWNLOAD_VERSION}.zip ${LINKER_DOWNLOAD_URL}/${LINKER_DOWNLOAD_VERSION}/${LINKER_FILE_NAME}.zip >/dev/null 2>&1
    if [[ $? != 0 ]]; then
        echo -e "${red}下载程序失败，请检查本机能否连接 ${LINKER_DOWNLOAD_URL}/${LINKER_DOWNLOAD_VERSION}/${LINKER_FILE_NAME}.zip${plain}" && exit 1
    fi
    echo -e "${green}下载完成${plain}"

    echo -e "正在解压..."
    unzip -qo -O UTF-8 "${LINKER_INSTALL_PATH}/${LINKER_FILE_NAME}-${LINKER_DOWNLOAD_VERSION}.zip" -d $LINKER_INSTALL_PATH
    chmod 777 -R ${LINKER_INSTALL_PATH}/$LINKER_FILE_NAME
    echo -e "${green}解压完成${plain}"

    rm -rf ${LINKER_INSTALL_PATH}/${LINKER_FILE_NAME}-${LINKER_DOWNLOAD_VERSION}.zip
}

pre_check
