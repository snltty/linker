<template>
    <el-row>
        <el-col :span="8">
            <el-checkbox v-model="state.checkAll" @change="handleCheckAllChange" label="全选" :indeterminate="state.isIndeterminate" />
        </el-col>
        <el-col :span="8">
            <el-checkbox v-model="state.full" ><span class="red">满权限(顶级管理权)</span></el-checkbox>
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
import { useAccess } from './access';
export default {
    props:['machineid','height'],
    setup(props) {

        const globalData = injectGlobalData();
        const allAccess = useAccess();
        const access = computed(()=>{
            const json = globalData.value.config.Client.Accesss;
            return Object.keys(json).reduce((arr,key,index)=>{
                if(globalData.value.hasAccess(key)){
                    const value = json[key];
                    value.Key = key;
                    arr.push(value);
                }
                return arr;
            },[]);
        });
        const state = reactive({
            height:props.height || 50,
            checkList: [
                globalData.value.config.Client.Accesss.Api.Value,
                globalData.value.config.Client.Accesss.Web.Value,
                globalData.value.config.Client.Accesss.NetManager.Value,
                globalData.value.config.Client.Accesss.FullManager.Value,
                globalData.value.config.Client.Accesss.Transport.Value,
                globalData.value.config.Client.Accesss.Action.Value,
                globalData.value.config.Client.Accesss.Group.Value,
            ],
            checkAll:false,
            full:false,
            isIndeterminate:false
        });

        const getValue = ()=>{
            if(state.full) return (+(BigInt(0xffffffffffffffff)>>BigInt(12)).toString())-1;
            return +state.checkList.reduce((sum,item)=>{
                return (sum | BigInt(item));
            },BigInt(0)).toString();
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
            if(allAccess && allAccess.value.list[props.machineid]){
                const res = allAccess.value.list[props.machineid];
                state.checkList = access.value.reduce((arr,item)=>{
                    if(+(( BigInt(res) & BigInt(item.Value)).toString()) == item.Value){
                        arr.push(item.Value);
                    }
                    return arr;
                },[]);
            }
            handleCheckedChange(state.checkList);
        })

        return {state,access,getValue,handleCheckAllChange,handleCheckedChange};
    }
}
</script>
<style lang="stylus" scoped>
 .el-col {text-align:left;}
</style>