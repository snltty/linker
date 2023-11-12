export default {
    field() {
        return {
            LLock: {
                LockScreen: false
            },
        }
    },
    update(item, report) {
        if (!report.LLock) return;
        item.LLock.LockScreen = report.LLock.LockScreen;
    }
}