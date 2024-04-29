export default {
    pluginName: 'cmonitor.plugin.keyboard.',
    field() {
        return {
            Keyboard: {
            }
        }
    },
    state: {
        keyboard: {
            showKeyBoard: false,
            devices: []
        }
    },
    init() {
    },
    update(item, report) {
    }
}