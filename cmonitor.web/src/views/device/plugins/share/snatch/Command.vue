<template>
    <div class="item">
        <div class="subitem">
            <span class="label">投票答题</span>
            <el-button @click="handleSnatchUse">开始</el-button>
            <el-button @click="handleSnatchQuestion">模板</el-button>
        </div>
    </div>
    <SnatchTemplate v-if="pluginState.shareSnatch.showTemplate" v-model="pluginState.shareSnatch.showTemplate"></SnatchTemplate>
    <SnatchUse v-if="pluginState.shareSnatch.showUse" v-model="pluginState.shareSnatch.showUse"></SnatchUse>
</template>

<script>
import { injectPluginState } from '@/views/device/provide';
import SnatchTemplate from './template/Template.vue'
import SnatchUse from './use/Use.vue'
export default {
    components: { SnatchTemplate, SnatchUse },
    setup() {

        const pluginState = injectPluginState();

        const handleSnatchQuestion = () => {
            pluginState.value.shareSnatch.showTemplate = true;
        }
        const handleSnatchUse = () => {
            pluginState.value.shareSnatch.showUse = true;
        }

        return { pluginState, handleSnatchUse, handleSnatchQuestion }
    }
}
</script>

<style lang="stylus" scoped></style>