export default {
    pluginName: 'command',
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