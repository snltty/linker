#!/bin/bash

if [ ! -f /etc/kvmd/.init_flag ]; then
    cat >> /etc/kvmd/supervisord.conf << EOF

[program:linker]
command=./linker
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

/kvmd/init.sh