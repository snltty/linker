#!/bin/bash

if [ ! -f /linker/kvm/supervisord.conf ]; then
    cat >> /linker/kvm/supervisord.conf << EOF

[supervisord]
logfile = /linker/kvm/supervisord.log
logfile_maxbytes = 50MB           
pidfile = /linker/kvm/supervisord.pid 
daemon = true

[unix_http_server]
file = /linker/kvm/supervisor.sock

[supervisorctl]
serverurl = unix:///linker/kvm/supervisor.sock 

[program:linker]
command=/linker/linker
directory=/linker/
autostart=true
autorestart=true
priority=12
stopasgroup=true
stdout_logfile=/linker/kvm/stdout
stdout_logfile_maxbytes = 0
redirect_stderr=true
EOF

fi


if [ ! -f /usr/share/kvmd/extras/linker/manifest.yaml ]; then
	mkdir -p /usr/share/kvmd/extras/linker
    cat >> /usr/share/kvmd/extras/linker/manifest.yaml << EOF
name: linker
description: linker network
icon: share/svg/logo-linker.png
path: linker
daemon: kvmd-linker
place: 21

EOF

fi


python3 - <<END
import json

with open("/usr/share/kvmd/web/share/i18n/i18n_zh.json", "r", encoding='utf-8') as f:
    data = json.load(f)

data["copyright"] = "版权所有 &copy; 2018-2024 Maxim Devaev | 由 SilentWind 二次开发 | snltty 三次包装集成Linker组网"
data["kvm_text2"] = "//<a href=\"https://linker-doc.snltty.com/docs/14%E3%80%81%E4%B8%BA%E7%88%B1%E5%8F%91%E7%94%B5\">这些人</a>向 Linker 项目赞助并支持其工作，非常感谢他们的帮助。<br>//如果您也想支持 Linker ，可以在 <a target=\"_blank\" href=\"https://afdian.com/a/snltty\"> 爱发电 </a>上捐款</a>。<br><br>//<a href=\"https://one-kvm.mofeng.run/thanks/#_2\">这些人</a>向 One-KVM 项目赞助并支持其工作，非常感谢他们的帮助。<br>//如果您也想支持 One-KVM ，可以在 <a target=\"_blank\" href=\"https://afdian.com/a/silentwind\"> 爱发电 </a>上捐款</a>。<br><br>//<a href=\"https://github.com/pikvm/pikvm?tab=readme-ov-file#special-thanks\">这些人</a>向 PiKVM 项目赞助并支持其工作，非常感谢他们的帮助。<br>//如果您也想支持 PiKVM ，可以在 <a target=\"_blank\" href=\"https://www.patreon.com/pikvm\"> Patreon</a> 或 <a target=\"_blank\" href=\"https://paypal.me/pikvm\"> PayPal 上捐款</a>。";

with open("/usr/share/kvmd/web/share/i18n/i18n_zh.json", "w", encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=2)

END


supervisord -c /linker/kvm/supervisord.conf &

/kvmd/init.sh