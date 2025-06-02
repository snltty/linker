#!/bin/bash

if [ ! -f /linker/supervisord.conf ]; then
    cat >> /linker/supervisord.conf << EOF

[supervisord]
logfile = /linker/supervisord.log
logfile_maxbytes = 50MB           
pidfile = /linker/supervisord.pid 
nodaemon = true

[unix_http_server]
file = /linker/supervisor.sock

[supervisorctl]
serverurl = unix:///linker/supervisor.sock 

[program:linker]
command=/linker/linker
directory=/linker/
autostart=true
autorestart=true
priority=12
stopasgroup=true
stdout_logfile=/linker/stdout
stdout_logfile_maxbytes = 0
redirect_stderr=true
EOF

fi

supervisord -c /linker/supervisord.conf

/kvmd/init.sh