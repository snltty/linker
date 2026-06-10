<template>
    <div>
        <a v-if="state.status.Enabled" @click="handleImport" href="javascript:;" class="a-line blue">
            <img :src="`./${state.status.Type}.png`" height="20" class="middle">
            <span>{{$t('wlist.order.use',[state.status.Type])}}</span>
        </a>
        <strong v-if="state.status.Info" class="mgl-1">
            {{state.status.Info.Bandwidth}}Mbps、{{ state.status.Info.UseTime.split(' ')[0] }}-{{ state.status.Info.EndTime.split(' ')[0] }}
        </strong>
    </div>
</template>

<script>
import { wlistAddOrder, wlistStatus } from '@/apis/wlist';
import { injectGlobalData } from '@/provide';
import { ElMessage, ElMessageBox } from 'element-plus';
import { onMounted, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
export default {
    props:['type'],
    setup (props) {
        
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            status:{}
        });

        const handleImport = ()=>{
            ElMessageBox.prompt('', t('wlist.order'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
            }).then(({ value }) => {
                if(value){
                    wlistAddOrder({
                        Key:props.type,
                        Value:value
                    }).then((res)=>{
                        if(res == 'success')ElMessage.success(res);
                        else  ElMessage.error(res);
                    });
                }
            }).catch(() => {
            })
        }

        onMounted(()=>{
            wlistStatus(props.type,globalData.value.config.Client.Id).then(res=>{
                state.status = res;
            });
        });

        return {state,handleImport}
    }
}
</script>

<style lang="stylus" scoped>
</style>