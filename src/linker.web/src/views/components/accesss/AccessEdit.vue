<template>
     <el-dialog append-to=".app-wrap" v-model="state.show" :close-on-click-modal="false" center :title="$t('access.edit',[state.machineName])" width="580" top="1vh">
        <div>
            <Access :accesss="state.accesss" ref="accessDom"></Access>
        </div>
        <template #footer>
            <el-button plain @click="state.show = false" :loading="state.loading">{{$t('common.cancel')}}</el-button>
            <el-button type="success" plain @click="handleSave" :loading="state.loading">{{$t('common.confirm')}}</el-button>
        </template>
    </el-dialog>
</template>
<script>
import { setAccess } from '@/apis/access';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch } from 'vue';
import Access from './Access.vue'
import { useI18n } from 'vue-i18n';

export default {
    props: ['data','modelValue'],
    emits: ['change','update:modelValue'],
    components:{Access},
    setup(props, { emit }) {

        const {t} = useI18n();
        const state = reactive({
            show: true,
            loading: false,
            machineName:props.data.MachineName,
            accesss:props.data.hook_accesss
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const accessDom = ref(null);
        const handleSave = () => {
            state.loading = true;
            const access = accessDom.value.getValue();
            setAccess({
                ToMachineId:props.data.MachineId,
                Access:access[0],
                FullAccess:access[1]
            }).then(() => {
                state.loading = false;
                state.show = false;
                ElMessage.success(t('common.opered'));
                emit('change')
            }).catch((err) => {
                console.log(err);
                state.loading = false;
                ElMessage.error(t('common.operFail'));
            });
        }

        return {
            state, accessDom,  handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>