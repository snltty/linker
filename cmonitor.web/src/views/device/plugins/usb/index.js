export default {
    field() {
        return {
            Usb: {
                Value: false
            },
        }
    },
    update(item, report) {
        item.Usb.Value = report.Usb.Value;
    }
}