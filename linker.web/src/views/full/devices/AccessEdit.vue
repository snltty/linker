<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" center append-to=".app-wrap" :title="`设置[${machineName}]的权限`" width="580" top="1vh">
        <div>
            <Access :machineid="machineid" ref="accessDom"></Access>
        </div>
        <template #footer>
            <el-button plain @click="state.show = false" :loading="state.loading">取消</el-button>
            <el-button type="success" plain @click="handleSave" :loading="state.loading">确定保存</el-button>
        </template>
    </el-dialog>
</template>
<script>
import { setAccess } from '@/apis/config';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch } from 'vue';
import Access from '@/views/full/devices/Access.vue'

export default {
    props: ['data','modelValue'],
    emits: ['change','update:modelValue'],
    components:{Access},
    setup(props, { emit }) {
        const state = reactive({
            show: true,
            loading: false,
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
            setAccess({
                ToMachineId:props.data.MachineId,
                Access:accessDom.value.getValue()
            }).then(() => {
                state.loading = false;
                state.show = false;
                ElMessage.success('已操作！');
                emit('change')
            }).catch(() => {
                state.loading = false;
                ElMessage.error('操作失败！');
            });
        }

        return {
          machineName:props.data.MachineName, 
          machineid:props.data.MachineId, state, accessDom,  handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>