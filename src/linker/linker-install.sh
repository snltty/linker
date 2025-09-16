#!/bin/bash

# Linker 安装管理脚本
set -e

# 配置变量
BASE_URL="https://static.snltty.com/downloads/linker"
VERSION_URL="${BASE_URL}/version.txt"
TEMP_DIR="/tmp/linker-install"
INSTALL_DIR="/usr/local/bin/linker"
SERVICE_NAME="linker"
DOCKER_IMAGE="snltty/linker-musl"
DOCKER_CONTAINER="linker"

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 输出彩色信息
info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

debug() {
    echo -e "${BLUE}[DEBUG]${NC} $1"
}

# 检查命令是否存在
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# 检测系统架构
detect_arch() {
    local arch
    arch=$(uname -m)
    case "$arch" in
        x86_64|amd64)
            echo "x64"
            ;;
        aarch64|arm64)
            echo "arm64"
            ;;
        armv7l|armv8l|arm)
            echo "arm"
            ;;
        *)
            error "不支持的架构: $arch"
            exit 1
            ;;
    esac
}

# 检测libc类型
detect_libc() {
    if ldd --version 2>&1 | grep -q musl; then
        echo "musl"
    elif [ -f /etc/alpine-release ]; then
        echo "musl"
    else
        echo "gnu"
    fi
}

# 获取最新版本号
get_latest_version() {
    if command_exists curl; then
        curl -sSL --connect-timeout 10 "$VERSION_URL" | head -n 1 | tr -d '\r'
    elif command_exists wget; then
        wget -qO- --timeout=10 "$VERSION_URL" | head -n 1 | tr -d '\r'
    else
        error "需要 curl 或 wget 来获取版本信息"
        exit 1
    fi
}

# 安装Docker
install_docker() {
    info "开始安装Docker..."
    
    if command_exists docker; then
        info "Docker 已经安装"
        return 0
    fi
    
    # 使用官方Docker安装脚本
    if command_exists curl; then
        curl -fsSL https://get.docker.com -o get-docker.sh
    elif command_exists wget; then
        wget -q https://get.docker.com -O get-docker.sh
    else
        error "需要 curl 或 wget 来下载Docker安装脚本"
        exit 1
    fi
    
    # 安装Docker
    chmod +x get-docker.sh
    sh get-docker.sh
    
    # 启动Docker服务
    if command_exists systemctl; then
        systemctl start docker
        systemctl enable docker
    elif command_exists service; then
        service docker start
    fi
    
    # 验证Docker安装
    if docker --version; then
        info "Docker 安装成功"
        rm -f get-docker.sh
        return 0
    else
        error "Docker 安装失败"
        rm -f get-docker.sh
        return 1
    fi
}

# 检查Docker容器是否存在
docker_container_exists() {
    docker ps -a --filter "name=^/${DOCKER_CONTAINER}$" --format '{{.Names}}' | grep -q "^${DOCKER_CONTAINER}$"
}

# 设置Docker容器
setup_docker() {
    info "配置Docker容器..."
    
    # 安装Docker（如果不存在）
    if ! command_exists docker; then
        warn "Docker 未安装，开始自动安装..."
        if ! install_docker; then
            error "Docker 安装失败，无法继续Docker安装"
            exit 1
        fi
    fi
    
    # 检查容器是否已存在
    if docker_container_exists; then
        error "Docker容器 ${DOCKER_CONTAINER} 已存在，请先卸载或删除现有容器"
        exit 1
    fi
    
    info "拉取Docker镜像: ${DOCKER_IMAGE}"
    if ! docker pull "${DOCKER_IMAGE}"; then
        error "拉取Docker镜像失败"
        exit 1
    fi
    
    # 创建并运行容器
    info "创建并启动Docker容器: ${DOCKER_CONTAINER}"
    
    local docker_run_cmd="docker run -it -d \
        --name ${DOCKER_CONTAINER} \
        -v $INSTALL_DIR/configs:/app/configs \
        -v $INSTALL_DIR/logs:/app/logs \
        ---device /dev/net/tun \
        --restart=always \
        --privileged=true \
        --network host \
        ${DOCKER_IMAGE}"

    debug "执行命令: ${docker_run_cmd}"
    
    if eval "${docker_run_cmd}"; then
        info "Docker容器创建成功"
        
        # 等待容器启动
        sleep 3
        
        # 检查容器状态
        if docker ps --filter "name=^/${DOCKER_CONTAINER}$" --format '{{.Status}}' | grep -q "Up"; then
            info "Docker容器运行正常"
        else
            warn "容器已创建但可能未正常运行，请检查日志: docker logs ${DOCKER_CONTAINER}"
        fi
    else
        error "Docker容器创建失败"
        exit 1
    fi
}

# 下载并安装Linker
download_install() {
    local version=$1
    local arch=$2
    local libc=$3
    
    # 确定文件名
    local filename="${SERVICE_NAME}-linux-${arch}.zip"
    local pathname="${SERVICE_NAME}-linux-${arch}"

    os_alpine="0"
    [ -e /etc/os-release ] && cat /etc/os-release | grep -i "PRETTY_NAME" | grep -qi "alpine" && os_alpine='1'
    if [ "$os_alpine" == 1 ]; then
        filename="${SERVICE_NAME}-linux-musl-${arch}.zip"
        pathname="${SERVICE_NAME}-linux-musl-${arch}"
    fi

    local download_url="${BASE_URL}/${version}/${filename}"
    
    info "下载链接: $download_url"
    
    # 创建临时目录
    rm -rf "$TEMP_DIR"
    mkdir -p "$TEMP_DIR"
    
    # 下载文件
    if command_exists curl; then
        if ! curl -sSL --connect-timeout 30 -o "${TEMP_DIR}/${filename}" "$download_url"; then
            error "下载失败: $download_url"
            exit 1
        fi
    elif command_exists wget; then
        if ! wget -q --timeout=30 -O "${TEMP_DIR}/${filename}" "$download_url"; then
            error "下载失败: $download_url"
            exit 1
        fi
    else
        error "需要 curl 或 wget 来下载文件"
        exit 1
    fi
    
    # 检查下载是否成功
    if [ ! -f "${TEMP_DIR}/${filename}" ]; then
        error "下载失败，文件不存在"
        exit 1
    fi
    
    # 解压文件
    if command_exists unzip; then
        if ! unzip -qo "${TEMP_DIR}/${filename}" -d "$TEMP_DIR"; then
            error "解压失败"
            exit 1
        fi
    else
        error "需要 unzip 来解压文件"
        exit 1
    fi
    
    # 安装到系统目录
    mkdir -p "$INSTALL_DIR"
    cp -r "${TEMP_DIR}/${pathname}/." "$INSTALL_DIR/"
    
    rm -rf "$TEMP_DIR"

    info "Linker 已安装到 ${INSTALL_DIR}/"
}

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
# 安装系统依赖
install_dependencies() {
    info "安装系统依赖..."
    
    install_base
    install_soft
    if command_exists apt-get; then
        apt-get update >/dev/null 2>&1
        apt-get install -y curl wget unzip iproute2 net-tools iptables bash traceroute >/dev/null 2>&1
    elif command_exists yum; then
        yum install -y curl wget unzip iproute net-tools iptables bash traceroute >/dev/null 2>&1
    elif command_exists dnf; then
        dnf install -y curl wget unzip iproute net-tools iptables bash traceroute >/dev/null 2>&1
    elif command_exists apk; then
        apk add curl wget unzip iproute2 net-tools iptables bash traceroute >/dev/null 2>&1
    else
        warn "无法确定包管理器，跳过依赖安装"
        warn "请确保已安装: curl/wget, unzip, ip, ifconfig, iptables, bash, traceroute"
    fi
}

# 配置systemctl服务
setup_systemctl() {
    info "配置systemctl服务..."
    
    local service_file="/etc/systemd/system/${SERVICE_NAME}.service"
    
    cat > "$service_file" << EOF
[Unit]
Description=${SERVICE_NAME}
After=network.target

[Service]
WorkingDirectory=${INSTALL_DIR}
ExecStartPre=/bin/chmod +x ${INSTALL_DIR}/${SERVICE_NAME}
ExecStart=${INSTALL_DIR}/${SERVICE_NAME}
ExecStop=/bin/kill $MAINPID
ExecReload=/bin/kill -HUP $MAINPID
Restart=always

[Install]
WantedBy=multi-user.target
EOF

    systemctl daemon-reload
    systemctl enable "$SERVICE_NAME"
    systemctl start "$SERVICE_NAME"
    
    info "systemctl服务已配置并启动"
}

# 配置supervisor
setup_supervisor() {
    info "配置supervisor..."
    
    if ! command_exists supervisorctl; then
        error "Supervisor未安装，请先安装supervisor"
        exit 1
    fi
    
    local supervisor_conf="/etc/supervisor/conf.d/${SERVICE_NAME}.conf"
    
    cat > "$supervisor_conf" << EOF
[program:${SERVICE_NAME}]
command=${INSTALL_DIR}/${SERVICE_NAME}
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/${SERVICE_NAME}.log
EOF

    supervisorctl update
    supervisorctl start "$SERVICE_NAME"
    
    info "supervisor配置已完成"
}

# 安装Linker
install_linker() {
    local install_type=$1
    
    info "开始安装${SERVICE_NAME}..."
    
    # 安装依赖（如果不是Docker方式）
    if [ "$install_type" != "3" ]; then
        install_dependencies
    fi
    
    # 根据安装类型进行配置
    case "$install_type" in
        1|2|4)
            # 获取版本和系统信息
            info "获取最新版本..."
            local version
            version=$(get_latest_version)
            info "最新版本: $version"
            
            local arch
            arch=$(detect_arch)
            info "系统架构: $arch"
            
            local libc
            libc=$(detect_libc)
            info "Libc类型: $libc"
            
            # 下载并安装
            download_install "$version" "$arch" "$libc"
            
            # 配置服务
            case "$install_type" in
                1)
                    setup_systemctl
                    ;;
                2)
                    setup_supervisor
                    ;;
                4)
                    info "直接安装完成，请手动运行: ${INSTALL_DIR}/${SERVICE_NAME}"
                    ;;
            esac
            ;;
        3)
            setup_docker
            ;;
        *)
            error "无效的安装类型"
            exit 1
            ;;
    esac
    
    info "安装完成!"
}

# 卸载Docker容器
uninstall_docker() {
    info "卸载Docker容器..."
    
    if command_exists docker; then
        if docker_container_exists; then
            info "停止并删除Docker容器: ${DOCKER_CONTAINER}"
            docker stop "$DOCKER_CONTAINER" 2>/dev/null || true
            docker rm "$DOCKER_CONTAINER" 2>/dev/null || true
            info "Docker容器已移除"
        else
            info "Docker容器不存在，无需移除"
        fi
        
        # 可选：删除镜像
        read -rp "是否删除Docker镜像 ${DOCKER_IMAGE}？(y/N): " choice
        if [[ "$choice" =~ ^[Yy]$ ]]; then
            docker rmi "$DOCKER_IMAGE" 2>/dev/null || true
            info "Docker镜像已移除"
        fi
    else
        info "Docker 未安装，跳过Docker相关卸载"
    fi
}

# 卸载Linker
uninstall_linker() {
    info "开始卸载${SERVICE_NAME}..."
    
    # 停止服务
    if systemctl is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
        systemctl stop "$SERVICE_NAME"
        systemctl disable "$SERVICE_NAME"
        rm -f "/etc/systemd/system/${SERVICE_NAME}.service"
        systemctl daemon-reload
        info "已停止并移除systemctl服务"
    fi
    
    # 移除supervisor配置
    if [ -f "/etc/supervisor/conf.d/${SERVICE_NAME}.conf" ]; then
        supervisorctl stop "$SERVICE_NAME" 2>/dev/null || true
        rm -f "/etc/supervisor/conf.d/${SERVICE_NAME}.conf"
        supervisorctl update 2>/dev/null || true
        info "已移除supervisor配置"
    fi
    
    # 移除安装文件
    if [ -f "${INSTALL_DIR}/${SERVICE_NAME}/${SERVICE_NAME}" ]; then
        rm -f "${INSTALL_DIR}/${SERVICE_NAME}"
        info "已移除 ${INSTALL_DIR}/${SERVICE_NAME}"
    fi
    
    # 清理临时文件
    rm -rf "$TEMP_DIR"
    
    # 如果是Docker安装方式，卸载Docker容器
    read -rp "是否卸载Docker容器？(y/N): " choice
    if [[ "$choice" =~ ^[Yy]$ ]]; then
        uninstall_docker
    fi
    
    info "卸载完成!"
}

# 显示菜单
show_menu() {
    echo "========================================"
    echo "          ${SERVICE_NAME} 安装管理脚本           "
    echo "========================================"
    echo "请选择操作:"
    echo "  1) 安装 ${SERVICE_NAME}"
    echo "  2) 卸载 ${SERVICE_NAME}"
    echo "  3) 退出"
    echo "========================================"
    read -rp "请输入选择 [1-3]: " choice
    
    case $choice in
        1)
            echo "请选择安装方式:"
            echo "  1) 使用 systemctl 管理"
            echo "  2) 使用 supervisor 管理"
            echo "  3) Docker 容器安装"
            echo "  4) 直接安装（手动运行）"
            echo "========================================"
            read -rp "请输入选择 [1-4]: " install_type
            case $install_type in
                1|2|3|4)
                    install_linker "$install_type"
                    ;;
                *)
                    error "无效选择"
                    exit 1
                    ;;
            esac
            ;;
        2)
            uninstall_linker
            ;;
        3)
            info "退出脚本"
            exit 0
            ;;
        *)
            error "无效选择"
            exit 1
            ;;
    esac
}

# 检查root权限
if [ "$(id -u)" -ne 0 ]; then
    error "此脚本需要root权限运行"
    exit 1
fi

# 主程序
if [ $# -eq 0 ]; then
    show_menu
else
    case $1 in
        install)
            if [ $# -gt 1 ]; then
                case $2 in
                    systemctl) install_linker 1 ;;
                    supervisor) install_linker 2 ;;
                    docker) install_linker 3 ;;
                    direct) install_linker 4 ;;
                    *)
                        error "无效的安装类型"
                        echo "用法: $0 install [systemctl|supervisor|docker|direct]"
                        exit 1
                        ;;
                esac
            else
                echo "用法: $0 install [systemctl|supervisor|docker|direct]"
                exit 1
            fi
            ;;
        uninstall)
            uninstall_linker
            ;;
        *)
            echo "用法: $0 [install [systemctl|supervisor|docker|direct] | uninstall]"
            exit 1
            ;;
    esac
fi