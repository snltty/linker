export default {
    field() {
        return {
            Viewer: {
                share: false,
                mode: 'server'
            }
        };
    },
    state: {
        viewer: {
            showShare: false,
            device: '',
            devices: [],
            shareUpdateFlag: 0
        }
    },

    init() {
    },
    uninit() {
    },

    update(item, report) {
        if (!report.Viewer) return;
        item.Viewer.share = report.Viewer.Value;
        item.Viewer.mode = ['client', 'server'][report.Viewer.Mode];
    }
}