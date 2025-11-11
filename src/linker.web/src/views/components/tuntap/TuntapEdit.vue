<template>
    <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap"
        :title="`设置[${state.machineName}]组网`" top="1vh" width="780">
        <div>
            <el-tabs type="border-card">
                <el-tab-pane label="网卡">
                    <TuntapIP ref="ipDom"></TuntapIP>
                </el-tab-pane>
                <el-tab-pane label="点/网对网">
                    <TuntapLan ref="lanDom"></TuntapLan>
                </el-tab-pane>
                <el-tab-pane label="端口转发">
                    <TuntapForward ref="forwardDom"></TuntapForward>
                </el-tab-pane>
                <el-tab-pane label="路由">
                    <TuntapRoutes></TuntapRoutes>
                </el-tab-pane>
            </el-tabs>
            <div class="foot t-c">
                <el-button @click="state.show = false" :loading="state.loading">取消</el-button>
                <el-button type="primary" @click="handleSave" :loading="state.loading">确定保存</el-button>
            </div>
        </div>
    </el-dialog>
</template>
<script>
import { updateTuntap } from '@/apis/tuntap';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch} from 'vue';
import { useTuntap } from './tuntap';
import TuntapForward from './TuntapForward.vue'
import TuntapLan from './TuntapLan.vue'
import TuntapIP from './TuntapIP.vue'
import TuntapRoutes from './TuntapRoutes.vue';
export default {
    props: ['modelValue'],
    emits: ['change', 'update:modelValue'],
    components: { TuntapForward ,TuntapLan,TuntapIP,TuntapRoutes},
    setup(props, { emit }) {

        const tuntap = useTuntap();
        const state = reactive({
            show: true,
            machineName: tuntap.value.current.device.MachineName,
            loading:false
        });
        
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const ipDom = ref(null);
        const lanDom = ref(null);
        const forwardDom = ref(null);
        const handleSave = () => {
            state.loading = true;
            const json = ipDom.value.getData();
            json.Lans = lanDom.value ?  lanDom.value.getData() : tuntap.value.current.Lans;
            json.Forwards = forwardDom.value ?  forwardDom.value.getData() : tuntap.value.current.Forwards;
            updateTuntap(json).then(() => {
                state.show = false;
                state.loading = false;
                ElMessage.success('已操作！');
                emit('change');
            }).catch((err) => {
                state.loading = false;
                console.log(err);
                ElMessage.error('操作失败！');
                
            });
        }

        return {
            state, handleSave,
            ipDom,lanDom,forwardDom
        }
    }
}
</script>
<style lang="stylus" scoped>
.foot{
    padding-top:2rem;
}
</style>