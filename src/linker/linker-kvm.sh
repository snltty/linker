#!/bin/bash

if [ ! -f /etc/kvmd/.init_flag ]; then
	cat >> /etc/systemd/system/linker.service  << EOF
[Unit]
Description=linker

[Service]
WorkingDirectory=/linker
ExecStartPre=/bin/chmod +x /linker/linker
ExecStart=/linker/linker
ExecStop=/bin/kill $MAINPID
ExecReload=/bin/kill -HUP $MAINPID
Restart=always

[Install]
WantedBy=multi-user.target
EOF

    systemctl daemon-reload >/dev/null 2>&1
    systemctl enable linker >/dev/null 2>&1
    systemctl restart linker >/dev/null 2>&1
fi


/kvmd/init.sh