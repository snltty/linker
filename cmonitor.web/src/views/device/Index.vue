<template>
    <div class="device-list-wrap absolute flex flex-column" id="device-list-wrap">
        <div class="items flex-1 relative scrollbar-1">
            <Items></Items>
        </div>
        <div class="foot">
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
import { providePluginState } from './provide'
export default {
    components: { Items, FootMenu, FootOptions },
    setup() {

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
            indexModules
        }
    }
}
</script>

<style lang="stylus" scoped>
.device-list-wrap {
    .head {
        padding: 1rem 1rem 1rem 1rem;
        border-bottom: 1px solid #ddd;
        background-color: #f0f0f0;
        z-index: 999;
        position: relative;
        box-shadow: 1px 1px 4px rgba(0, 0, 0, 0.075);
    }

    .items {
        padding: 0.6rem;
        transform-style: preserve-3d;
        perspective: 600px;
        padding-bottom: 11rem;
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
            border: 1px solid rgba(18, 63, 76, 0.8);
            border-radius: 4px;
            background-color: rgba(19, 67, 89, 0.5);
            // background-color: rgba(219, 234, 255, 0.5);
            // border: 1px solid rgba(18, 63, 76, 0.8);
            backdrop-filter: blur(2px);
        }
    }
}
</style>