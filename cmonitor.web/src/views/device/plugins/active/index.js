export default {
    field() {
        return {
            ActiveWindow: {
                Title: '',
                FileName: '',
                Desc: '',
                Pid: 0,
                Count: 0
            }
        }
    },
    state: {
        activeWindow: [
            {
                showTimes: false,
                items: [],
                showWindows: false,
                showChoose: false,
                devices: []
            }
        ]
    },
    update(item, report) {
        item.ActiveWindow.Title = report.ActiveWindow.Title;
        item.ActiveWindow.FileName = report.ActiveWindow.FileName;
        item.ActiveWindow.Desc = report.ActiveWindow.Desc;
        item.ActiveWindow.Pid = report.ActiveWindow.Pid;
        item.ActiveWindow.Count = report.ActiveWindow.Count;
    }
}