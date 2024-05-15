export default {
    pluginName: 'wallpaper',
    field() {
        return {
            Wallpaper: {
                Value: false
            },
        }
    },
    update(item, report) {
        if (!report.Wallpaper) return;
        item.Wallpaper.Value = report.Wallpaper.Value;
    }
}