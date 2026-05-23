'use strict';

const INIT = '/etc/init.d/linker';

function router_host() {
	let host = http.getenv('HTTP_HOST') || http.getenv('SERVER_NAME') || '192.168.1.1';
	let ipv6 = match(host, /^(\[[^\]]+\])/);

	if (ipv6)
		return ipv6[1];

	return replace(host, /:\d+$/, '');
}

function manage_url() {
	return 'http://' + router_host() + ':1804/';
}

function service(action) {
	return system(INIT + ' ' + action + ' >/dev/null 2>&1');
}

function write_result(title, status, rc) {
	http.status(status, title);
	http.prepare_content('text/html; charset=UTF-8');
	http.write('<!doctype html><html><head><meta charset="utf-8"><title>Linker</title></head><body>');
	http.write('<h1>' + title + '</h1>');

	if (rc != null)
		http.write('<p>exit code: ' + rc + '</p>');

	http.write('<p><a href="./stop">停止</a> | <a href="./start">启动</a> | <a href="./manage">管理页面</a></p>');
	http.write('</body></html>');
}

return {
	action_start: function(env) {
		let rc = service('start');

		if (rc == 0)
			write_result('linker 已启动', 200, rc);
		else
			write_result('linker 启动失败', 500, rc);
	},

	action_stop: function(env) {
		let rc = service('stop');

		if (rc == 0)
			write_result('linker 已停止', 200, rc);
		else
			write_result('linker 停止失败', 500, rc);
	},

	action_manage: function(env) {
		http.redirect(manage_url());
	}
};
