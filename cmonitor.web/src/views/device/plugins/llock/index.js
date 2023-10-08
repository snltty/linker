export default {
    field() {
        return {
            LLock: {
                Value: false
            },
        }
    },
    update(item, report) {
        if (!report.LLock) return;
        item.LLock.Value = report.LLock.Value;
    }
}