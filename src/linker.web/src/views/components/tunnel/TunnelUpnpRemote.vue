<template>
    <div>
        <div class="head pdb-6 t-c">
            <el-input size="small" v-model="state.name" @change="handleSearch" clearable style="width:10rem;margin-right:1rem"></el-input>
            <el-button size="small" :loading="state.loading" @click="handleSearch"><el-icon><Search></Search></el-icon> </el-button>
        </div>
        <el-table stripe  :data="state.list" border size="small" width="100%" height="60vh">
            <el-table-column prop="DeviceType" label="类型" width="66" sortable>
                <template #default="scope">{{ deviceTypes[scope.row.DeviceType] }}</template>
            </el-table-column>
            <el-table-column prop="PublicPort" label="外网端口" width="90" sortable></el-table-column>
            <el-table-column prop="ClientIp" label="内网ip" width="100" sortable></el-table-column>
            <el-table-column prop="PrivatePort" label="内网端口" width="90" sortable></el-table-column>
            <el-table-column prop="ProtocolType" label="协议" width="66" sortable>
                <template #default="scope">{{ protocolTypes[scope.row.ProtocolType] }}</template>
            </el-table-column>    
            <el-table-column prop="LeaseDuration" label="存活" width="66" sortable>
                <template #default="scope">
                    <span :class="{red:scope.row.LeaseDuration ==0,green:scope.row.LeaseDuration >0}">{{ scope.row.LeaseDuration }}</span>
                </template>
            </el-table-column>
            <el-table-column prop="Description" label="描述" width="200"></el-table-column>
            <el-table-column property="Oper" label="" width="60" fixed="right">
                <template #default="scope">
                    <el-button size="small" type="danger" v-if="scope.row.Deletable" @click="handleDel(scope.row)"><el-icon><DeleteFilled></DeleteFilled></el-icon> </el-button>
                </template>
            </el-table-column>
        </el-table>
    </div>
</template>
<script>
import {  onMounted, onUnmounted, reactive } from 'vue';
import {delUpnpMappingInfo, getUpnpMappingInfo, getUpnpMappingLocalInfo } from '@/apis/tunnel';
import {DeleteFilled,Search} from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';

export default {
    props: ['modelValue','deviceTypes','protocolTypes','machineId'],
    emits: ['update:modelValue'],
    components: { DeleteFilled,Search },
    setup(props, { emit }) {

        const { t } = useI18n();
        const state = reactive({
            name: '',
            list: [],
            timer:0 ,
            loading:false
        });

        const handleDel = (row) => { 
            ElMessageBox.confirm(t('common.deleteText',[`[${row.PublicPort}:${props.protocolTypes[row.ProtocolType]}]`]), t('common.tips'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning'
            }).then(() => { 
                delUpnpMappingInfo(props.machineId,row.PublicPort,row.ProtocolType).then(res => { 
                    setTimeout(handleSearch,1000);
                    ElMessage.success(t('common.oper'));
                });
            }).catch(() => { 
            });
        }

        const getMapping = () => { 
            if(state.loading) {
                state.timer = setTimeout(getMapping,5000);
                return;
            }
            clearTimeout(state.timer);
            state.loading = true;

            Promise.all([
                getUpnpMappingInfo(props.machineId),
                getUpnpMappingLocalInfo(props.machineId)
            ]).then(res=>{
                state.loading = false;
                filter(res);
                state.timer = setTimeout(getMapping,5000);
            }).catch(()=>{
                state.loading = false;
                state.timer = setTimeout(getMapping,5000);
            });
        }
        const handleSearch = ()=>{
            if(state.loading) {
                return;
            }
            state.loading = true;

            Promise.all([
                getUpnpMappingInfo(props.machineId),
                getUpnpMappingLocalInfo(props.machineId)
            ]).then(res=>{
                state.loading = false;
                filter(res);
            }).catch(()=>{
                state.loading = false;
            });
        }
        const filter = (res) => { 
            const local = res[1].reduce((json,item,index)=>{
                json[`${item.PublicPort}->${item.ClientIp}->${item.ProtocolType}`] = item;
                return json;
            },{});

            state.list = res[0].filter(c=>{
                const _local = local[`${c.PublicPort}->${c.ClientIp}->${c.ProtocolType}`];
                if(_local){
                    c['_local'] = true;
                    c['Deletable'] = _local['Deletable'];
                }
                return c.Description.indexOf(state.name)>=0 ||
                c.ClientIp.indexOf(state.name)>=0 ||
                c.PublicPort.toString().indexOf(state.name)>=0 ||
                c.PrivatePort.toString().indexOf(state.name)>=0 ||
                props.protocolTypes[c.ProtocolType].indexOf(state.name)>=0 ||
                props.deviceTypes[c.DeviceType].indexOf(state.name)>=0;
            }).sort((a,b)=>b['_local']-a['_local']);
        }

        onMounted(()=>{
            getMapping();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {
           state, handleDel,handleSearch
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>