<template>
    <div>
        <ul class="list">
            <template v-for="(item,index) in state.data" :key="index">
               <li>
                <a href="javascript:;" @click="state.show = true" :class="{green:item.Started}">
                    <template v-if="item.Started"><strong>{{item.Port}}->{{item.TargetEP}}</strong></template>
                    <template v-else>{{item.Port}}->{{item.TargetEP}}</template>
                </a>
               
               </li>
            </template>
        </ul>
        <el-dialog v-model="state.show" @open="handleOnShowList" append-to=".app-wrap" :title="`端口转发到【${state.machineName}】`" top="1vh" width="600">
            <div>
                <div class="t-c head">
                    <el-button type="success" size="small" @click="handleAdd">添加</el-button>
                    <el-button size="small" @click="handleRefresh">刷新</el-button>
                </div>
                <el-table :data="state.data" size="small" border  height="500" @cell-dblclick="handleCellClick">
                    <el-table-column property="ID" label="ID" width="60" />
                    <el-table-column property="Name" label="名称"  >
                        <template #default="scope">
                            <template v-if="scope.row.NameEditing">
                                <el-input autofocus size="small" v-model="scope.row.Name" @blur="handleEditBlur(scope.row,'Name')"></el-input>
                            </template>
                            <template v-else>
                                {{scope.row.Name}}
                            </template>
                        </template>
                    </el-table-column>
                    <el-table-column property="Port" label="本地端口" width="80" >
                        <template #default="scope">
                            <template v-if="scope.row.PortEditing">
                                <el-input type="number" autofocus size="small" v-model="scope.row.Port" @blur="handleEditBlur(scope.row,'Port')"></el-input>
                            </template>
                            <template v-else>
                                {{scope.row.Port}}
                            </template>
                        </template>
                    </el-table-column>
                    <el-table-column property="TargetEP" label="目标服务" width="140">
                        <template #default="scope">
                            <template v-if="scope.row.TargetEPEditing">
                                <el-input autofocus size="small" v-model="scope.row.TargetEP" @blur="handleEditBlur(scope.row,'TargetEP')"></el-input>
                            </template>
                            <template v-else>
                                {{scope.row.TargetEP}}
                            </template>
                        </template>
                    </el-table-column>
                    <el-table-column property="Started" label="启动状态" width="80" >
                        <template #default="scope">
                            <el-switch v-model="scope.row.Started" @change="handleStartChange(scope.row)" inline-prompt active-text="是" inactive-text="否"/>
                        </template>
                       
                    </el-table-column>
                    <el-table-column label="操作" width="66">
                        <template #default="scope">
                            <el-popconfirm confirm-button-text="确认" cancel-button-text="取消" title="删除不可逆，是否确认?" @confirm="handleDel(scope.row.ID)">
                                <template #reference>
                                    <el-button type="danger" size="small">删除</el-button>
                                </template>
                            </el-popconfirm>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </el-dialog>
    </div>
</template>
<script>
import { reactive, watch } from 'vue';
import { getForwardInfo,removeForwardInfo,addForwardInfo } from '@/apis/forward'
import { ElMessage } from 'element-plus';
export default {
    props:{
        data:{
            type:Object,
            default:[]
        },
        name:{
            type:String,
            default:''
        }
    },
    emits:['change'],
    setup(props,{emit}) {
        const state = reactive({
            show:false,
            length:props.length,
            machineName:props.name,
            data:props.data,
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('change');
                }, 300);
            }
        });

        const _getForwardInfo = ()=>{
            getForwardInfo().then((res)=>{
                state.data = res[state.machineName] || [];
            }).catch(()=>{
            });
        }
        const handleOnShowList = ()=>{
            _getForwardInfo();
        }

        const handleCellClick = (row,column)=>{
            handleEdit(row,column.property);
        }

        const handleRefresh = ()=>{
            _getForwardInfo();
            ElMessage.success('已刷新')
        }
        const handleAdd = ()=>{
            saveRow({ID:0,Name:'',Port:0,TargetEP:'127.0.0.1:80',MachineName:state.machineName});
        }   
        const handleEdit = (row,p)=>{
            state.data.forEach(c=>{
                c[`NameEditing`] = false;
                c[`PortEditing`] = false;
                c[`TargetEPEditing`] = false;
            })
            row[`${p}Editing`] =true;
        }
        const handleEditBlur = (row,p)=>{
            row[`${p}Editing`] = false;
            saveRow(row);
        }
        const handleDel = (id)=>{
            removeForwardInfo(id).then(()=>{
                _getForwardInfo();
            })
        }
        const handleStartChange = (row)=>{
            saveRow(row);
        }
        const saveRow = (row)=>{
            row.Port = parseInt(row.Port)
            addForwardInfo(row).then(()=>{
                _getForwardInfo();
            }).catch((err)=>{
                ElMessage.error(err);
            });
        }

        return {
            state,handleOnShowList,handleCellClick,handleRefresh,handleAdd,handleEdit,handleEditBlur,handleDel,handleStartChange
        }
    }
}
</script>
<style lang="stylus" scoped>
.list{
    a{
        text-decoration: underline;
    }
    a.green{color:green}
}
.head{padding-bottom:1rem}
</style>