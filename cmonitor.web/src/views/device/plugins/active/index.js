export default {
    field() {
        return {
            ActiveWindow: {
                Title: '',
                FileName: '',
                Desc: '',
                Pid: 0,
                DisallowCount: 0,
                DisallowRunIds: [],
                WindowCount: 0,
            }
        }
    },
    state: {
        activeWindow: [
            {
                showTimes: false,
                showWindows: false,
                showChoose: false,
                devices: [],
                showAddWindow: false,
                showActiveWindows: false,
            }
        ]
    },
    update(item, report) {
        if (!report.ActiveWindow) return;
        item.ActiveWindow.Title = report.ActiveWindow.Title;
        item.ActiveWindow.FileName = report.ActiveWindow.FileName;
        item.ActiveWindow.Desc = report.ActiveWindow.Desc;
        item.ActiveWindow.Pid = report.ActiveWindow.Pid;
        item.ActiveWindow.DisallowCount = report.ActiveWindow.DisallowCount;
        item.ActiveWindow.WindowCount = report.ActiveWindow.WindowCount || 0;
        item.ActiveWindow.DisallowRunIds = report.ActiveWindow.Ids || [];
    }
}