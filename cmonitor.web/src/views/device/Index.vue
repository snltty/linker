<template>
    <div class="device-list-wrap absolute flex flex-column" id="device-list-wrap">
        <div class="items flex-1 relative scrollbar">
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
import { provide, ref, watch } from 'vue'
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
        padding: 2rem 1rem 1rem 1rem;
        border-bottom: 1px solid #ddd;
        background-color: #f0f0f0;
        z-index: 999;
        position: relative;
        box-shadow: 1px 1px 4px rgba(0, 0, 0, 0.075);
    }

    .foot {
        position: absolute;
        z-index: 999;
        left: 1rem;
        right: 1rem;
        bottom: 1rem;
        border-radius: 4px;

        &:after {
            content: '';
            position: absolute;
            left: 0;
            top: 0;
            right: 0;
            bottom: 0;
            z-index: -1;
            border: 1px solid rgba(255, 255, 255, 0.4);
            border-radius: 4px;
            background-color: rgba(186, 217, 255, 0.5);
            backdrop-filter: blur(2px);
        }
    }

    .items {
        padding: 1rem;
        transform-style: preserve-3d;
        perspective: 600px;
        background-color: #333;
        background-image: url('../../assets/bg.webp');
        background-size: cover;
        padding-bottom: 13rem;
    }
}
</style>