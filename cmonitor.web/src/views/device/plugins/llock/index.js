export default {
    field() {
        return {
            LLock: {
                Value: false
            },
        }
    },
    update(item, report) {
        item.LLock.Value = report.LLock.Value;
    }
}