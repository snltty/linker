<template>
    <div class="foot-wrap">
        <ul class="flex">
            <template v-for="(item,index) in footMenuModules" :key="index">
                <li>
                    <component :is="item"></component>
                </li>
            </template>
        </ul>
    </div>
</template>

<script>
import { injectGlobalData } from '@/views/provide';
import { computed } from 'vue';

export default {
    components: {},
    setup() {

        const globalData = injectGlobalData();

        const footMenuFiles = require.context('../plugins/', true, /FootMenu\.vue/);
        const _footMenuModules = footMenuFiles.keys().map(c => footMenuFiles(c).default).sort((a, b) => a.sort - b.sort);
        const plugins = computed(()=>globalData.value.config.Common.Plugins||[]);
        const footMenuModules = computed(()=>_footMenuModules.filter(c=>plugins.value.length == 0 || plugins.value.indexOf(c.pluginName)>=0));
        return { footMenuModules }
    }
}
</script>

<style lang="stylus">
.el-dialog.is-align-center.options {
    margin-top: 1vh;
    max-width: 40rem;
}

.el-dialog.is-align-center.options-center {
    max-width: 40rem;
}
</style>

<style lang="stylus" scoped>
.foot-wrap {
    border-top: 1px solid rgba(18, 46, 79, 0.8);
    position: relative;
    z-index: 999;
}

ul li {
    width: 25%;
    text-align: center;

    a {
        padding: 1.2rem 0;
        font-size: 1.6rem;
        display: block;
        color: #f5f5f5;
        line-height: 1;

        &:hover {
            background-color: rgba(0, 0, 0, 0.05);
        }
    }
}
</style>