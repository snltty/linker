export default {
    field() {
        return {
            Usb: {
                Value: false
            },
        }
    },
    update(item, report) {
        if (!report.Usb) return;
        item.Usb.Value = report.Usb.Value;
    }
}