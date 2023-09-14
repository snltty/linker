const { defineConfig } = require('@vue/cli-service')
module.exports = defineConfig({
    productionSourceMap: process.env.NODE_ENV === 'production' ? false : true,
    outputDir: '../cmonitor/web',
    transpileDependencies: true
})
