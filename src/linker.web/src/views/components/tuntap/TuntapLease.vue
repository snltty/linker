<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="配置本组的网络" top="1vh" width="500">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="70">
                <el-form-item label="网卡名" prop="Name">
                    <el-row class="w-100">
                        <el-col :span="10">
                            <el-input v-trim v-model="state.ruleForm.Name" class="w-100"/>
                        </el-col>
                        <el-col :span="14"></el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="MTU" prop="MTU">
                    <el-row class="w-100">
                        <el-col :span="10">
                            <el-input-number v-trim v-model="state.ruleForm.Mtu" :min="0" :max="1500" class="w-100" />
                        </el-col>
                        <el-col :span="14"></el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="MSS钳制" prop="MssFix">
                    <el-row class="w-100">
                        <el-col :span="10">
                            <el-select v-model="state.ruleForm.MssFix" class="w-100">
                                <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.msss"></el-option>
                            </el-select>
                        </el-col>
                        <el-col :span="14"></el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="网络号" prop="IP">
                    <el-row class="w-100">
                        <el-col :span="13">
                            <el-input v-trim v-model="state.ruleForm.IP" @change="handlePrefixLengthChange" />
                        </el-col>
                        <el-col :span="1" class="t-c">/</el-col>
                        <el-col :span="3">
                            <el-input v-trim @change="handlePrefixLengthChange" v-model="state.ruleForm.PrefixLength" />
                        </el-col>
                        <el-col :span="1" class="t-c"></el-col>
                        <el-col :span="6">
                            <el-button @click="handleClear"><el-icon><Refresh /></el-icon></el-button>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" prop="IP1">
                    <div class="w-100">
                        <el-descriptions :column="2" size="small" border title="">
                            <el-descriptions-item label="网络号">{{ state.values.Network }}</el-descriptions-item>
                            <el-descriptions-item label="网关">{{ state.values.Gateway }}</el-descriptions-item>
                            <el-descriptions-item label="开始IP">{{ state.values.Start }}</el-descriptions-item>
                            <el-descriptions-item label="结束IP">{{ state.values.End }}</el-descriptions-item>
                            <el-descriptions-item label="广播号">{{ state.values.Broadcast }}</el-descriptions-item>
                            <el-descriptions-item label="IP数量">{{ state.values.Count }}</el-descriptions-item>
                        </el-descriptions>
                    </div>
                </el-form-item>
                <el-form-item label="子网" prop="Subs">
                    <div class="subs">
                        <template v-for="(item,index) in state.ruleForm.Subs">
                            <el-row class="w-100 sub-item">
                                <el-col :span="4" class="pdr-10">
                                    <el-input v-trim v-model="item.Name"/>
                                </el-col>
                                <el-col :span="7">
                                    <el-input v-trim v-model="item.IP" disabled/>
                                </el-col>
                                <el-col :span="1" class="t-c">/</el-col>
                                <el-col :span="3">
                                    <el-input v-trim v-model="item.PrefixLength" disabled/>
                                </el-col>
                                <el-col :span="9" class="t-r">
                                    <el-button type="danger" @click="handleDelSub(index)"><el-icon><Delete></Delete></el-icon></el-button>
                                    <el-button type="info" @click="handleEditSub(index)"><el-icon><Edit></Edit></el-icon></el-button>
                                    <el-button type="primary" @click="handleAddSub(index)"><el-icon><Plus></Plus></el-icon></el-button>
                                </el-col>
                            </el-row>
                        </template>
                    </div>
                </el-form-item>
                <el-form-item label="" prop="alert"></el-form-item>
                <AccessShow value="Lease">
                    <el-form-item label="" prop="Btns">
                        <div>
                            <el-button @click="state.show = false">取消</el-button>
                            <el-button type="primary" @click="handleSave">确认</el-button>
                        </div>
                    </el-form-item>
                </AccessShow>
            </el-form>
        </div>
    </el-dialog>
    <el-dialog v-model="state.showEdit" append-to=".app-wrap" title="选择子网" top="1vh" width="440">
        <div>
            <div class="head t-c mgb-1">
                <el-select  v-model="state.prefixLength" class="w-20 mgl-1" @change="handleSubChange">
                    <el-option v-for="value in state.prefixLengths" :value="value.value" :label="`/${value.value}、每段 : ${value.length}个IP`"></el-option>
                </el-select>
            </div>
            <el-table :data="state.subs.list" size="small" border height="400">
                <el-table-column property="CIDR" label="CIDR">
                    <template #default="scope">
                        <el-tag>{{ scope.row.Start }}/{{ state.prefixLength }}</el-tag>
                    </template>
                </el-table-column>
                <el-table-column property="Start" label="开始"></el-table-column>
                <el-table-column property="End" label="结束"></el-table-column>
                <el-table-column property="Oper" label="操作" width="60">
                    <template #default="scope">
                        <el-button size="small" v-if="scope.row.Disabled == false" @click="handleUseSub(scope.row)">选用</el-button>
                    </template>
                </el-table-column>
            </el-table>
            <div class="t-c mgt-1">
                <div class="inline">
                    <el-pagination small background layout="total,prev,pager, next" :total="state.subs.count"
                    :page-size="state.subs.size" :current-page="state.subs.page" @current-change="handleSubPageChange"/>
                </div>
            </div>
        </div>
    </el-dialog>
</template>
<script>
import {getNetwork,addNetwork,calcNetwork, calcSubNetwork } from '@/apis/tuntap';
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { Delete, Plus,Refresh,Edit } from '@element-plus/icons-vue'
export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    components: {Delete,Plus,Refresh,Edit},
    setup(props, { emit }) {

        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            ruleForm: {
                Name:'',
                IP:'0.0.0.0',
                PrefixLength:24,
                Subs:[],
                Mtu:1420,
                MssFix:0,
            },
            rules: {
                Name: {
                    type: 'string',
                    pattern: /^$|^[A-Za-z][A-Za-z0-9]{0,31}$/,
                    message:'请输入正确的网卡名',
                    transform(value) {
                        return value.trim();
                    },
                }
            },
            values:{},
            msss:[
                {value:-1,label:'不启用'},
                {value:0,label:'自动计算'},
                {value:1400,label:'启用1400'},
                {value:1380,label:'启用1380'},
                {value:1360,label:'启用1360'},
                {value:1340,label:'启用1340'},
                {value:1320,label:'启用1320'},
                {value:1300,label:'启用1300'},
                {value:1280,label:'启用1280'},
                {value:1260,label:'启用1260'},
                {value:1240,label:'启用1240'},
                {value:1220,label:'启用1220'},
                {value:1200,label:'启用1200'}
            ],

            showEdit: false,
            editIndex : -1,
            prefixLengths: Array.from({ length: 17 }, (_, i) => { return {value:32-i,length:1<<(32-(32-i))} }),
            prefixLength:29,
            subs:{
                list:computed(c=>{
                    return state.subs._list.slice((state.subs.page-1)*state.subs.size,state.subs.page*state.subs.size);
                }),
                _list:[],
                page:1,
                size:10,
                count:0
            }
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const _calcNetwork = ()=>{
            calcNetwork(state.ruleForm).then((res)=>{
                state.values = res;
            });
        }
        const _getNetwork = ()=>{
            getNetwork().then((res)=>{
                state.ruleForm.Name = res.Name;
                state.ruleForm.IP = res.IP;
                state.ruleForm.PrefixLength = res.PrefixLength;
                if(res.Subs.length == 0){
                    res.Subs = [{Name:'子网1',IP:'0.0.0.0',PrefixLength:24}];
                }
                state.ruleForm.Subs = res.Subs;
                state.ruleForm.Mtu = res.Mtu;
                state.ruleForm.MssFix = res.MssFix;
                _calcNetwork();
            });
        }
        const handlePrefixLengthChange = ()=>{
            var value = +state.ruleForm.PrefixLength;
            if(value>32 || value<16 || isNaN(value)){
                value = 24;
            }
            state.ruleForm.PrefixLength = value;
            _calcNetwork();
        }
        const handleSave = () => {
            addNetwork(state.ruleForm).then(()=>{
                ElMessage.success('已操作');
                state.show = false;
            }).catch((err)=>{
                console.log(err);
                ElMessage.error('操作失败');
            })
        }
        const handleClear = ()=>{
            addNetwork({Name:'',IP:'0.0.0.0',PrefixLength:24,Subs:[]}).then(()=>{
                ElMessage.success('已操作');
                _getNetwork();
            }).catch((err)=>{
                console.log(err);
                ElMessage.error('操作失败');
            });
        }

        const handleAddSub = (index)=>{
            state.ruleForm.Subs.splice(index+1,0,{Name:'子网'+(state.ruleForm.Subs.length+1),IP:'0.0.0.0',PrefixLength:24});
        }
        const handleDelSub = (index)=>{
            if(state.ruleForm.Subs.length <= 1){
                state.ruleForm.Subs = [{Name:'子网1',IP:'0.0.0.0',PrefixLength:24}];
                return;
            }
            ElMessageBox.confirm('确定要删除吗？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(() => {
                 state.ruleForm.Subs.splice(index,1);
            }).catch(() => {
            });
           
        }

        const handleSubChange = ()=>{
            calcSubNetwork({
                Subs:state.ruleForm.Subs,
                PrefixLength:state.ruleForm.PrefixLength,
                IP:state.ruleForm.IP,
                SubPrefixLength:state.prefixLength
            }).then((res)=>{
                state.subs._list = res.sort((a,b)=>b.Disabled-a.Disabled);
                state.subs.count = res.length;
                state.subs.page = 1;
            });
        }
        const handleEditSub = (index)=>{
            state.showEdit = true;
            state.editIndex = index;
            handleSubChange();
        }
        const handleSubPageChange = (page)=>{
            state.subs.page = page;
        }
        const handleUseSub = (row)=>{
            state.ruleForm.Subs[state.editIndex].IP = row.Start;
            state.ruleForm.Subs[state.editIndex].PrefixLength = state.prefixLength;
            state.showEdit = false;
            handleSubChange();
        }

        onMounted(()=>{
            _getNetwork();
        })

        return {
           state,ruleFormRef, handleSave,handlePrefixLengthChange,handleClear,
           handleDelSub,handleAddSub,handleEditSub,handleUseSub,handleSubPageChange,handleSubChange
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}
.el-button+.el-button{
    margin-left: .4rem;
}
.sub-item{
    margin-bottom:.6rem;
}
</style>