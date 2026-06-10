<template>
     <el-dialog append-to=".app-wrap" v-model="state.show" :close-on-click-modal="false" :title="$t('tuntap.lease')" top="1vh" width="510">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="80">
                <el-form-item :label="$t('tuntap.name')" prop="Name">
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
                            <el-select v-model="state.ruleForm.Mtu" class="w-100" :disabled="state.ruleForm.IP == '0.0.0.0'">
                                <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.mtus"></el-option>
                            </el-select>
                        </el-col>
                        <el-col :span="14"></el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('tuntap.mss')" prop="MssFix">
                    <el-row class="w-100">
                        <el-col :span="10">
                            <el-select v-model="state.ruleForm.MssFix" class="w-100" :disabled="state.ruleForm.IP == '0.0.0.0'">
                                <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.msss"></el-option>
                            </el-select>
                        </el-col>
                        <el-col :span="14"></el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('tuntap.network')" prop="IP">
                    <el-row class="w-100">
                        <el-col :span="13">
                            <el-input v-trim v-model="state.ruleForm.IP" :disabled="state.disabled" @change="handlePrefixLengthChange" />
                        </el-col>
                        <el-col :span="1" class="t-c">/</el-col>
                        <el-col :span="3">
                            <el-input v-trim @change="handlePrefixLengthChange" :disabled="state.disabled" v-model="state.ruleForm.PrefixLength" />
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
                            <el-descriptions-item :label="$t('tuntap.network')">{{ state.values.Network }}</el-descriptions-item>
                            <el-descriptions-item :label="$t('tuntap.gateway')">{{ state.values.Gateway }}</el-descriptions-item>
                            <el-descriptions-item :label="$t('common.start')">{{ state.values.Start }}</el-descriptions-item>
                            <el-descriptions-item :label="$t('common.end')">{{ state.values.End }}</el-descriptions-item>
                            <el-descriptions-item :label="$t('tuntap.broadcast')">{{ state.values.Broadcast }}</el-descriptions-item>
                            <el-descriptions-item :label="$t('tuntap.ipCount')">{{ state.values.Count }}</el-descriptions-item>
                        </el-descriptions>
                    </div>
                </el-form-item>
                <el-form-item :label="$t('tuntap.segment')" prop="VlsmStatus">
                    <el-select v-model="state.ruleForm.VlsmStatus" class="w-14" :disabled="state.ruleForm.IP == '0.0.0.0'">
                        <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.vlsms"></el-option>
                    </el-select>
                </el-form-item>
                <el-form-item :label="$t('tuntap.subnet')" prop="Subs">
                    <div class="subs">
                        <template v-for="(item,index) in state.ruleForm.Subs">
                            <el-row class="w-100 sub-item">
                                <el-col :span="4" class="pdr-10">
                                    <el-input v-trim v-model="item.Name" :disabled="state.ruleForm.IP == '0.0.0.0'"/>
                                </el-col>
                                <el-col :span="7">
                                    <el-input v-trim v-model="item.IP" disabled/>
                                </el-col>
                                <el-col :span="1" class="t-c">/</el-col>
                                <el-col :span="3">
                                    <el-input v-trim v-model="item.PrefixLength" disabled/>
                                </el-col>
                                <el-col :span="9" class="t-r">
                                    <template v-if="state.ruleForm.IP != '0.0.0.0'">
                                        <el-button type="danger" @click="handleDelSub(index)"><el-icon><Delete></Delete></el-icon></el-button>
                                        <el-button type="info" @click="handleEditSub(index)"><el-icon><Edit></Edit></el-icon></el-button>
                                        <el-button type="primary" @click="handleAddSub(index)"><el-icon><Plus></Plus></el-icon></el-button>
                                    </template>
                                </el-col>
                            </el-row>
                        </template>
                    </div>
                </el-form-item>
                
                <el-form-item label="" prop="alert"></el-form-item>
                <AccessShow value="Lease">
                    <el-form-item label="" prop="Btns">
                        <div>
                            <el-button @click="state.show = false">{{$t('common.cancel')}}</el-button>
                            <el-button type="primary" @click="handleSave">{{$t('common.confirm')}}</el-button>
                        </div>
                    </el-form-item>
                </AccessShow>
            </el-form>
        </div>
    </el-dialog>
    <el-dialog append-to=".app-wrap" v-model="state.showEdit" :title="$t('tuntap.subnet')" top="1vh" width="440">
        <div>
            <div class="head t-c mgb-1">
                <el-select  v-model="state.prefixLength" class="w-20 mgl-1" @change="handleSubChange" :disabled="state.disabled">
                    <el-option v-for="value in state.prefixLengths" :value="value.value" :label="value.label"></el-option>
                </el-select>
            </div>
            <el-table :data="state.subs.list" size="small" border height="400">
                <el-table-column property="CIDR" label="CIDR">
                    <template #default="scope">
                        <el-tag>{{ scope.row.Start }}/{{ state.prefixLength }}</el-tag>
                    </template>
                </el-table-column>
                <el-table-column property="Start" :label="$t('common.start')"></el-table-column>
                <el-table-column property="End" :label="$t('common.end')"></el-table-column>
                <el-table-column property="Oper" :label="$t('common.oper')" width="60">
                    <template #default="scope">
                        <el-button size="small" v-if="scope.row.Disabled == false" @click="handleUseSub(scope.row)">{{$t('common.use')}}</el-button>
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
import { Delete, Plus,Refresh,Edit, MessageBox } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n';
export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    components: {Delete,Plus,Refresh,Edit},
    setup(props, { emit }) {

        const {t} = useI18n();
        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            disabled:computed(()=>state.ruleForm.Subs.filter(c=>c.IP != '0.0.0.0').length > 0),
            ruleForm: {
                Name:'',
                IP:'0.0.0.0',
                PrefixLength:24,
                Subs:[],
                Mtu:1420,
                MssFix:2,
                VlsmStatus:2,
            },
            rules: {
                Name: {
                    type: 'string',
                    pattern: /^$|^[A-Za-z][A-Za-z0-9]{0,31}$/,
                    message:t('tuntap.validate'),
                    transform(value) {
                        return value.trim();
                    },
                }
            },
            values:{},
            vlsms:[
                {value:1,label:`${t('tuntap.master')} <-/->${t('tuntap.subnet')}`},
                {value:2,label:`${t('tuntap.master')}  -->${t('tuntap.subnet')}`},
                {value:4,label:`${t('tuntap.master')} <--> ${t('tuntap.subnet')}`},
            ],
            mtus:[
                {value:1480,label:'1480'},
                {value:1460,label:'1460'},
                {value:1440,label:'1440'},
                {value:1420,label:'1420'},
                {value:1400,label:'1400'},
                {value:1380,label:'1380'},
                {value:1360,label:'1360'},
                {value:1340,label:'1340'},
                {value:1320,label:'1320'},
                {value:1300,label:'1300'},
                {value:1280,label:'1280'},
                {value:1260,label:'1260'},
                {value:1240,label:'1240'},
                {value:1220,label:'1220'},
                {value:1200,label:'1200'}
            ],
            msss:[
                {value:0,label:''},
                {value:1,label:''},
                {value:2,label:''},
                {value:3,label:''},
                {value:4,label:''},
                {value:5,label:''},
                {value:6,label:t('tuntap.unset')},
                {value:7,label:t('tuntap.clamp')},
                {value:1400,label:'1400'},
                {value:1380,label:'1380'},
                {value:1360,label:'1360'},
                {value:1340,label:'1340'},
                {value:1320,label:'1320'},
                {value:1300,label:'1300'},
                {value:1280,label:'1280'},
                {value:1260,label:'1260'},
                {value:1240,label:'1240'},
                {value:1220,label:'1220'},
                {value:1200,label:'1200'}
            ],

            showEdit: false,
            editIndex : -1,
            prefixLengths: Array.from({ length: 17 }, (_, i) => { 
                return {
                    value:32-i,
                    label:`/${32-i}、${(1<<(32-(32-i)))} IP`
                } 
            }),
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
                    res.Subs = [{Name:`${t('tuntap.subnet')}1`,IP:'0.0.0.0',PrefixLength:29}];
                }else{
                    const sub = res.Subs.filter(c=>c.IP != '0.0.0.0')[0];
                    if(sub){
                        state.prefixLength = sub.PrefixLength;
                    }
                }
                state.ruleForm.Subs = res.Subs;
                state.ruleForm.Mtu = res.Mtu;
                state.ruleForm.MssFix = res.MssFix;
                state.ruleForm.VlsmStatus = res.VlsmStatus;
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
            const json = JSON.parse(JSON.stringify(state.ruleForm));
            json.Subs = json.Subs.filter(c=>c.IP != '0.0.0.0');
            addNetwork(state.ruleForm).then(()=>{
                ElMessage.success(t('common.opered'));
                state.show = false;
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
            })
        }
        const handleClear = ()=>{
            ElMessageBox.confirm(t('common.clearSure',['']), t('common.tips'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning'
            }).then(() => {
                state.ruleForm.IP = '0.0.0.0';
                state.ruleForm.PrefixLength = 24;
                state.ruleForm.Subs = [{Name:`${t('tuntap.subnet')}1`,IP:'0.0.0.0',PrefixLength:29}];
                _calcNetwork();
            }).catch(() => {
            });
        }

        const handleAddSub = (index)=>{
            state.ruleForm.Subs.splice(index+1,0,{Name:t('tuntap.subnet')+(state.ruleForm.Subs.length+1),IP:'0.0.0.0',PrefixLength:29});
        }
        const handleDelSub = (index)=>{
            if(state.ruleForm.Subs.length <= 1){
                state.ruleForm.Subs = [{Name:`${t('tuntap.subnet')}1`,IP:'0.0.0.0',PrefixLength:29}];
                return;
            }
            ElMessageBox.confirm(t('common.operSure',['']), t('common.tips'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
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