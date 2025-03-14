
# 配置打包参数
a = Analysis(
    ['main1.0.py'],  # 输入文件
    pathex=['D:/py/linker/.venv/Lib/site-packages'],  # 添加路径
    binaries=[],  # 可选：如果有二进制文件可以列出
    datas=[('img', 'img')],  # 添加资源文件路径
    hiddenimports=['requests', 'pywin32', 'winshell', 'tkinterweb'],  # 添加隐式导入的模块
    hookspath=[],  # 可选：hook文件路径
    hooksconfig={},  # 可选：hook配置
    runtime_hooks=[],  # 可选：运行时钩子
    excludes=[],  # 可选：排除不需要的模块
    noarchive=False,  # 不创建压缩包
    optimize=0,  # 优化级别
)

# 创建打包的pyz文件
pyz = PYZ(a.pure)

# 创建EXE文件
exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],  # 可选：附加文件
    name='linker简易安装程序',  # 设置生成的EXE文件名称
    debug=False,  # 是否显示调试信息
    bootloader_ignore_signals=False,  # 启动时忽略信号
    strip=False,  # 不移除调试符号
    upx=True,  # 使用UPX压缩
    upx_exclude=[],  # 可选：排除不压缩的文件
    runtime_tmpdir=None,  # 可选：运行时临时目录
    console=False,  # 设置为False表示不显示控制台窗口
    disable_windowed_traceback=False,  # 显示错误信息
    argv_emulation=False,  # 是否模拟命令行参数
    target_arch=None,  # 可选：指定目标架构
    codesign_identity=None,  # 可选：代码签名
    entitlements_file=None,  # 可选：授权文件
    icon='D:\py\linker\src\linker.install.win\linker.ico'  # 设置图标文件路径
)
