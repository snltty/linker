<template>
    <div>
        <el-table :data="state.list" stripe border size="small" width="100%" height="60vh">
            <el-table-column prop="id" label="id" width="200"></el-table-column>
            <el-table-column prop="sendtBytes" :label="$t('status.flowUpload')" sortable>
                <template #default="scope">
                    <span>{{ scope.row.sendtBytesText }}</span>
                </template>
            </el-table-column>
            <el-table-column prop="sendtSpeed" :label="$t('status.flowUpload')" sortable>
                <template #default="scope">
                    <span>{{ scope.row.sendtSpeedText }}/s</span>
                </template>
            </el-table-column>
            <el-table-column prop="receiveBytes"  :label="$t('status.flowDownload')" sortable>
                <template #default="scope">
                    <span>{{ scope.row.receiveBytesText }}</span>
                </template>
            </el-table-column>
            <el-table-column prop="receiveSpeed" :label="$t('status.flowDownload')" sortable>
                <template #default="scope">
                    <span>{{ scope.row.receiveSpeedText }}/s</span>
                </template>
            </el-table-column>
        </el-table>
    </div>
</template>

<script>
import { getMessengerFlows } from '@/apis/flow';
import { onMounted, onUnmounted, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
export default {
    props: ['modelValue','config','machineId'],
    emits: ['update:modelValue'],
    setup (props,{emit}) {
        
        const {t} = useI18n();
        const state = reactive({
            timer:0,
            list:[],
            old:null
        });
        const _getMessengerFlows = ()=>{
            clearTimeout(state.timer);
            getMessengerFlows(props.machineId).then(res => {
                const old = state.old || res;
                const list = [];
                for(let j in res){
                    const item = res[j];
                    const itemOld = old[j];
                    const text = `[${j}]${t(`status.messenger${j}`) || `unknown`}`;
                    list.push({
                        id:text,

                        sendtBytes:item.SendtBytes,
                        sendtBytesText:parseSpeed(item.SendtBytes),

                        sendtSpeed:item.SendtBytes-itemOld.SendtBytes,
                        sendtSpeedText:parseSpeed(item.SendtBytes-itemOld.SendtBytes),

                        receiveBytes:item.ReceiveBytes,
                        receiveBytesText:parseSpeed(item.ReceiveBytes),

                        receiveSpeed:item.ReceiveBytes-itemOld.ReceiveBytes,
                        receiveSpeedText:parseSpeed(item.ReceiveBytes-itemOld.ReceiveBytes),
                    });
                }
                state.list = list.filter(c=>!!c.id);

                state.old = res;
                state.timer = setTimeout(_getMessengerFlows,1000);
            }).catch((e)=>{
                state.timer = setTimeout(_getMessengerFlows,1000);
            });
        }
        const parseSpeed = (num) => {
            let index = 0;
            while (num >= 1024) {
                num /= 1024;
                index++;
            }
            return `${num.toFixed(2)}${['B', 'KB', 'MB', 'GB', 'TB'][index]}`;
        }

        onMounted(()=>{
            _getMessengerFlows();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });
        

        return {
            config:props.config,state
        }
    }
}
</script>

<style lang="stylus" scoped>
a{
    font-weight:bold;position:absolute;right:1rem;bottom:90%;
    border:1px solid #ddd;
    background-color:#fff;
    z-index :9
    p{
        line-height:normal;
        white-space: nowrap;
    }
}
</style>