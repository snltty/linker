#!/bin/bash

# Linker macOS 发布脚本
# 用于构建 macOS 控制台客户端 (osx-x64 和 osx-arm64)
# 注意: 此脚本应在 macOS 上运行以获得最佳兼容性

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT"

# 清理和创建目录
rm -rf public/extends/osx-x64
rm -rf public/extends/osx-arm64
rm -rf public/publish/osx-x64
rm -rf public/publish/osx-arm64
mkdir -p public/publish-zip

# 构建 Web 前端
echo "Building web frontend..."
cd src/linker.web
npm install
npm run build
cd "$PROJECT_ROOT"

# 复制版本文件
cp shells/version.txt public/version.txt

# macOS 发布配置
PUBLISH_OPTS="-c release -f net8.0 -p:PublishSingleFile=true --self-contained true -p:TrimMode=partial -p:TieredPGO=true -p:DebugType=full -p:EventSourceSupport=false -p:DebugSymbols=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true -p:MetadataUpdaterSupport=false -p:UseSystemResourceKeys=true -p:MetricsSupport=false -p:StackTraceSupport=false -p:XmlResolverIsNetworkingEnabledByDefault=false"

# 发布 osx-arm64 (Apple Silicon)
echo "Publishing osx-arm64 (Apple Silicon)..."
dotnet publish src/linker $PUBLISH_OPTS -r osx-arm64 -o public/publish/osx-arm64/linker-osx-arm64

# 复制 Apple Silicon 原生库
cp src/linker/libutunshim-arm64.dylib public/publish/osx-arm64/linker-osx-arm64/libutunshim.dylib 2>/dev/null || true

# 复制通用扩展文件
cp -r public/extends/any/* public/publish/osx-arm64/linker-osx-arm64/ 2>/dev/null || true

# 打包
cd public/publish/osx-arm64
zip -r ../../publish-zip/linker-osx-arm64.zip .
cd "$PROJECT_ROOT"

# 发布 osx-x64 (Intel Mac)
echo "Publishing osx-x64 (Intel Mac)..."
dotnet publish src/linker $PUBLISH_OPTS -r osx-x64 -o public/publish/osx-x64/linker-osx-x64

# 复制 Intel 原生库
cp src/linker/libutunshim.dylib public/publish/osx-x64/linker-osx-x64/libutunshim.dylib 2>/dev/null || true

# 复制通用扩展文件
cp -r public/extends/any/* public/publish/osx-x64/linker-osx-x64/ 2>/dev/null || true

# 打包
cd public/publish/osx-x64
zip -r ../../publish-zip/linker-osx-x64.zip .
cd "$PROJECT_ROOT"

echo ""
echo "=========================================="
echo "macOS builds completed successfully!"
echo "=========================================="
echo "Output files:"
echo "  - public/publish-zip/linker-osx-arm64.zip (Apple Silicon)"
echo "  - public/publish-zip/linker-osx-x64.zip (Intel Mac)"
echo ""

