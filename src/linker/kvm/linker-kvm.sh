#!/bin/bash

rm -rf /linker/kvm/supervisord.conf 2>/dev/null
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
    
rm -rf /usr/share/kvmd/extras/linker 2>/dev/null
mkdir -p /usr/share/kvmd/extras/linker
cat >> /usr/share/kvmd/extras/linker/manifest.yaml << EOF
name: linker
description: linker network
icon: share/svg/logo-linker.png
path: linker
daemon: kvmd-linker
place: 21
EOF

sed -i 's/8080/1806/g' /etc/kvmd_backup/override.yaml
sed -i 's/4430/1807/g' /etc/kvmd_backup/override.yaml


rm -rf /usr/share/kvmd/web/linker 2>/dev/null
mkdir -p /usr/share/kvmd/web/linker
cp -rf /linker/kvm/index.html /usr/share/kvmd/web/linker/index.html
cp -rf /linker/kvm/logo-linker.png /usr/share/kvmd/web/share/svg/logo-linker.png

supervisord -c /linker/kvm/supervisord.conf &

/kvmd/init.sh