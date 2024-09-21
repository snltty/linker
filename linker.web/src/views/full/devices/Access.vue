<template>
    <el-row>
        <el-col :span="8">
            <el-checkbox v-model="state.checkAll" @change="handleCheckAllChange" label="全选" :indeterminate="state.isIndeterminate" />
        </el-col>
    </el-row>
    <el-checkbox-group v-model="state.checkList" @change="handleCheckedChange">
        <el-row>
            <template v-for="(item,index) in access" :key="index">
                <el-col :span="8">
                    <el-checkbox :value="item.Value" :label="item.Text" />
                </el-col>
            </template>
        </el-row>
    </el-checkbox-group>
</template>
<script>
import {  computed, onMounted, reactive } from 'vue';
import { injectGlobalData } from '@/provide';
import { useAccess } from './access';
export default {
    props:['machineid'],
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
            checkList: [
                globalData.value.config.Client.Accesss.Api.Value,
                globalData.value.config.Client.Accesss.Web.Value,
                globalData.value.config.Client.Accesss.NetManager.Value,
                globalData.value.config.Client.Accesss.FullManager.Value,
                globalData.value.config.Client.Accesss.Transport.Value,
                globalData.value.config.Client.Accesss.Action.Value,
            ],
            checkAll:false,
            isIndeterminate:false
        });

        const getValue = ()=>{
            return state.checkList.reduce((sum,item)=>{
                return (sum | item) >>> 0;
            },0);
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
                    if(((res & item.Value) >>> 0) == item.Value){
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