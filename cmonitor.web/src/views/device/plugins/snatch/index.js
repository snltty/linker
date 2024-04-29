export default {
    pluginName: 'cmonitor.plugin.snatch.',
    field() {
        return {
        };
    },
    state: {
        shareSnatch: {
            showTemplate: false,
            showUse: false,
            answers: []
        }
    },
    init() {
    },
    update(item, report) {
        if (!report.Share) return;
    }
}