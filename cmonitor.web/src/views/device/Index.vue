<template>
    <div class="device-list-wrap absolute flex flex-column flex-nowarp" id="device-list-wrap">
        <div class="content flex-1 flex flex-column">
            <div class="head" v-if="globalData.pc">

                <Head></Head>
            </div>
            <div class="items flex-1 relative scrollbar-1">
                <Items></Items>
            </div>
            <!-- <div class="active-device flex flex-column" v-if="globalData.pc">
               

                <div class="flex-1 prev">

                    <div class="prev-inner">
                        <h3>{{globalData.currentDevice.MachineName}}</h3>
                        <div class="inner">
                            <canvas id="prev-canvas" width="1920" height="1080"></canvas>
                        </div>
                    </div>
                </div>
            </div> -->
        </div>
        <div class="foot" v-if="!globalData.pc">
            <div class="foot-options">
                <FootOptions></FootOptions>
            </div>
            <div class="foot-menu">
                <FootMenu></FootMenu>
            </div>
        </div>
        <template v-for="(item,index) in indexModules" :key="index">
            <component :is="item"></component>
        </template>
    </div>
</template>
<script>
import FootMenu from './wraps/FootMenu.vue'
import FootOptions from './wraps/FootOptions.vue'
import Items from './wraps/Items.vue'
import Head from './wraps/Head.vue'
import { providePluginState } from './provide'
import { injectGlobalData } from '../provide'
export default {
    components: { Items, FootMenu, FootOptions, Head },
    setup() {

        const globalData = injectGlobalData();

        const files = require.context('./plugins/', true, /index\.js/);
        const pluginSettings = files.keys().map(c => files(c).default);
        const pluginState = pluginSettings.reduce((data, item, index) => {
            if (item.state) {
                data = Object.assign(data, item.state);
            }
            return data;
        }, {});
        const state = providePluginState(pluginState);

        const indexFiles = require.context('./plugins/', true, /Index\.vue/);
        const indexModules = indexFiles.keys().map(c => indexFiles(c).default);

        return {
            indexModules, globalData
        }
    }
}
</script>

<style lang="stylus" scoped>
// @media (min-width: 768px) {
// .active-device {
// // width: calc(100% - 41rem) !important;
// }

// .items {
// padding: 1rem 1rem 0 1rem !important;
// height: auto;
// width: 80.6rem;
// // padding: 1rem 0 !important;
// box-sizing: border-box;
// // border-right: 1px solid #999;
// // background-color: rgba(255, 255, 255, 0.3);
// display: flex;
// display: -ms-flex;
// display: -o-flex;
// flex-wrap: wrap;
// justify-content: space-between;
// }

// .foot {
// display: none;
// }
// }
// @media (min-width: 768px) {
// .items {
// max-width: 39rem;
// }
// }
.device-list-wrap {
    .content {
        position: relative;
        overflow: hidden;

        .items {
            padding: 0.6rem;
            transform-style: preserve-3d;
            perspective: 600px;
            padding-bottom: 11.6rem;
            height: 100%;
            box-sizing: border-box;
        }
    }

    .head {
        width: 100%;
        z-index: 999;
        position: relative;
        // display: none;
    }

    .prev {
        overflow: hidden;
        padding: 0.4rem 1rem 1rem 1rem;

        .prev-inner {
            position: relative;
            box-sizing: border-box;
            font-size: 1.6rem;
            box-shadow: 0 0 4px rgba(0, 0, 0, 0.05);
            width: 100%;
            margin: 0 auto 0 auto;
            position: relative;
            transition: 0.3s;
            background-color: rgba(255, 255, 255, 1);
            border-radius: 4px;

            h3 {
                padding: 0.6rem 0 0.6rem 1rem;
                color: #666;
                font-size: 1.4rem;
            }

            .inner {
                position: relative;
                overflow: hidden;
                background-color: rgba(0, 0, 0, 0.3);
                border-radius: 0 0 4px 4px;
                // border: 1px solid rgba(255, 255, 255, 0.2);
                box-sizing: border-box;

                &:before {
                    content: '';
                    display: inline-block;
                    padding-bottom: 56.25%;
                    width: 0.1px;
                    vertical-align: middle;
                }

                canvas {
                    width: 100%;
                    height: 100%;
                    position: absolute;
                }
            }
        }
    }

    .foot {
        position: absolute;
        z-index: 999;
        left: 0.6rem;
        right: 0.6rem;
        bottom: 0.6rem;
        border-radius: 4px;

        // background-color: rgba(255, 255, 255, 0.8);
        &:after {
            content: '';
            position: absolute;
            left: 0;
            top: 0;
            right: 0;
            bottom: 0;
            z-index: -1;
            border: 1px solid rgba(18, 76, 32, 0.8);
            border-radius: 4px;
            background-color: rgba(18, 89, 38, 0.5);
            // background-color: rgba(219, 234, 255, 0.5);
            // border: 1px solid rgba(18, 63, 76, 0.8);
            backdrop-filter: blur(2px);
        }
    }
}
</style>