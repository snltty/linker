<template>
    <el-row>
        <el-col :span="8">
            <el-checkbox v-model="state.checkAll" @change="handleCheckAllChange" label="全选" :indeterminate="state.isIndeterminate" />
        </el-col>
        <el-col :span="8" v-if="globalData.config.Client.FullAccess">
            <el-checkbox v-model="state.full" ><span class="red">满权限(顶级管理权)</span></el-checkbox>
        </el-col>
        <el-col :span="6">
            <el-input size="small" v-model="state.search"></el-input>
        </el-col>
    </el-row>
    <div class="access-wrap scrollbar" :style="{height:`${state.height}rem`}">
        <el-checkbox-group v-model="state.checkList" @change="handleCheckedChange">
            <el-row>
                <template v-for="(item,index) in access" :key="index">
                    <el-col :xs="12" :sm="8">
                        <el-checkbox :value="item.Value" :label="item.Text" />
                    </el-col>
                </template>
            </el-row>
        </el-checkbox-group>
    </div>
</template>
<script>
import {  computed, onMounted, reactive } from 'vue'; 
import { injectGlobalData } from '@/provide';
export default {
    props:['accesss','height'],
    setup(props) {

        const globalData = injectGlobalData();
        const exclude = ['ExternalShow','Cdkey']
        const access = computed(()=>{
            const json = globalData.value.config.Client.Accesss;
            return Object.keys(json).reduce((arr,key,index)=>{
                if(globalData.value.hasAccess(key) && !exclude.includes(key)){
                    const value = json[key];
                    value.Key = key;
                    arr.push(value);
                }
                return arr;
            },[]).filter(c=>c.Text.includes(state.search));
        });

        const state = reactive({
            height:props.height || 50,
            checkList: [
                globalData.value.config.Client.Accesss.Api.Value,
                globalData.value.config.Client.Accesss.Web.Value,
                globalData.value.config.Client.Accesss.NetManager.Value,
                globalData.value.config.Client.Accesss.FullManager.Value,
                globalData.value.config.Client.Accesss.Transport.Value,
                globalData.value.config.Client.Accesss.RenameSelf.Value,
                globalData.value.config.Client.Accesss.UpdateSelf.Value,
                globalData.value.config.Client.Accesss.TuntapStatusSelf.Value,
                globalData.value.config.Client.Accesss.TuntapChangeSelf.Value,
                globalData.value.config.Client.Accesss.ForwardShowSelf.Value,
                globalData.value.config.Client.Accesss.ForwardSelf.Value,
                globalData.value.config.Client.Accesss.TunnelChangeSelf.Value,
                globalData.value.config.Client.Accesss.TunnelRemove.Value,
                globalData.value.config.Client.Accesss.Action.Value,
                globalData.value.config.Client.Accesss.Socks5StatusSelf.Value,
                globalData.value.config.Client.Accesss.Socks5ChangeSelf.Value,
                globalData.value.config.Client.Accesss.FirewallSelf.Value,
                globalData.value.config.Client.Accesss.WakeupSelf.Value,
            ],
            checkAll:false,
            full:false,
            isIndeterminate:false,

            search:''
        });

        const getValue = ()=>{
            const arr = state.checkList.reduce((arr,item)=>{
                arr[item] = '1';
                return arr;
            },[]);
            for(let i = 0; i < arr.length;i++){
                arr[i] = arr[i] || '0';
            }
            return [arr.join(''),state.full];
        }
        const handleCheckedChange = (value)=>{
            const checkedCount = value.length;
            state.checkAll = checkedCount === access.value.length;
            state.isIndeterminate = checkedCount > 0 && checkedCount < access.value.length;
        }
        const handleCheckAllChange = (value)=>{
            state.checkAll = value;
            state.checkList = value ? access.value.map(item=>item.Value) : [];
            state.isIndeterminate = false;
        }

        onMounted(()=>{
            if(props.accesss){
                state.checkList = access.value.reduce((arr,item)=>{
                    if(props.accesss[item.Value] == '1'){
                        arr.push(item.Value);
                    }
                    return arr;
                },[]);
            }
            handleCheckedChange(state.checkList);
        })

        return {globalData,state,access,getValue,handleCheckAllChange,handleCheckedChange};
    }
}
</script>
<style lang="stylus" scoped>
 .el-col {text-align:left;}
</style>