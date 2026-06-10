<template>
    <el-dialog append-to=".app-wrap" v-model="state.show" :close-on-click-modal="false"
        :title="$t('tuntap.title',[state.machineName])" top="1vh" width="780">
        <div>
            <el-tabs type="border-card">
                <el-tab-pane :label="$t('tuntap')">
                    <TuntapIP ref="ipDom"></TuntapIP>
                </el-tab-pane>
                <el-tab-pane :label="$t('tuntap.p2n')">
                    <TuntapLan ref="lanDom"></TuntapLan>
                </el-tab-pane>
                <el-tab-pane :label="$t('tuntap.forward')">
                    <TuntapForward ref="forwardDom"></TuntapForward>
                </el-tab-pane>
                <el-tab-pane :label="$t('tuntap.fec')">
                    <TuntapFec ref="fecDom"></TuntapFec>
                </el-tab-pane>
                <el-tab-pane :label="$t('tuntap.route')">
                    <TuntapRoutes></TuntapRoutes>
                </el-tab-pane>
            </el-tabs>
            <div class="foot t-c">
                <el-button @click="state.show = false" :loading="state.loading">{{$t('common.cancel')}}</el-button>
                <el-button type="primary" @click="handleSave" :loading="state.loading">{{$t('common.confirm')}}</el-button>
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
import TuntapFec from './TuntapFec.vue';
import { useI18n } from 'vue-i18n';
export default {
    props: ['modelValue'],
    emits: ['change', 'update:modelValue'],
    components: { TuntapForward ,TuntapLan,TuntapIP,TuntapRoutes,TuntapFec},
    setup(props, { emit }) {

        const {t} = useI18n();
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
        const fecDom = ref(null);
        const handleSave = () => {
            state.loading = true;
            const json = ipDom.value.getData();
            json.Lans = lanDom.value ?  lanDom.value.getData() : tuntap.value.current.Lans;
            json.Forwards = forwardDom.value ?  forwardDom.value.getData() : tuntap.value.current.Forwards;
            json.FecProfile = fecDom.value ?  fecDom.value.getData() : tuntap.value.current.FecProfile;
            updateTuntap(json).then(() => {
                state.show = false;
                state.loading = false;
                ElMessage.success(t('common.opered'));
                emit('change');
            }).catch((err) => {
                state.loading = false;
                console.log(err);
                ElMessage.error(t('common.operFail'));
                
            });
        }

        return {
            state, handleSave,
            ipDom,lanDom,forwardDom,fecDom
        }
    }
}
</script>
<style lang="stylus" scoped>
.foot{
    padding-top:2rem;
}
</style>