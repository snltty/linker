export default {
    pluginName: 'cmonitor.plugin.command.',
    field() {
        return {
            Command: {
            }
        }
    },
    state: {
        command: {
            showCommand: false,
            showTerm: false,
            devices: [],
        }
    },
    init() {
    },
    update(item, report) {
    }
}