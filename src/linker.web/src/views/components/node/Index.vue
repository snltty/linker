<template>
    <el-dialog v-model="state.show" :title="$t('server.denyTitle',[data.Name])" top="1vh" width="400">
        <div>
            <el-tabs type="border-card">
                <el-tab-pane :label="$t('server.denyMasters')"><Masters :type="type" :data="data"></Masters></el-tab-pane>
                <el-tab-pane :label="$t('server.denyList')"><Denys :type="type" :data="data"></Denys></el-tab-pane>
            </el-tabs>
        </div>
    </el-dialog>
</template>

<script>
import { reactive, watch } from 'vue';
import Masters from './Masters.vue';
import Denys from './Denys.vue';
export default {
    props: ['type','data','modelValue'],
    emits: ['update:modelValue'],
    components: {Masters,Denys},
    setup (props,{emit}) {
        const state = reactive({
            show: true
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        return {state}
    }
}
</script>

<style lang="stylus" scoped>

</style>