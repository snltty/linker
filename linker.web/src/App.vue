<template>
    <div>
        <template v-if="configed">
            <router-view />
        </template>
        <Api></Api>
    </div>
</template>
<script>
import { computed } from 'vue';
import { provideGlobalData } from './provide';
import Api from './views/Api.vue';
export default {
    components:{Api},
    setup(props) {
        const globalData = provideGlobalData();
        const configed = computed(()=>globalData.value.config.configed);

        return { configed};
    }
}
</script>
<style lang="stylus" scoped>

</style>
