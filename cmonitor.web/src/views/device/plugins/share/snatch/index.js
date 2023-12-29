export default {
    field() {
        return {
        };
    },
    state: {
        shareSnatch: {
            showTemplate: false,
            showUse: true,
            answers: []
        }
    },
    init() {
    },
    update(item, report) {
        if (!report.Share) return;
    }
}