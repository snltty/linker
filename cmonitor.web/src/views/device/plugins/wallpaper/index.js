export default {
    field() {
        return {
            Wallpaper: {
                Value: false
            },
        }
    },
    update(item, report) {
        item.Wallpaper.Value = report.Wallpaper.Value;
    }
}