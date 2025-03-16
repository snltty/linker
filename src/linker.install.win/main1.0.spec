
added_files = [
    ('img/logo.png','img/')
]

# 配置打包参数
a = Analysis(
    ['main1.0.py'],
    pathex=['D:/py/linker/.venv/Lib/site-packages'],
    binaries=[],
    datas=added_files ,
    hiddenimports=['requests', 'pywin32', 'winshell', 'tkinterweb'],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)


pyz = PYZ(a.pure)


exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='linker简易安装程序',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon='D:\py\linker\src\linker.install.win\img\linker.ico'
)

