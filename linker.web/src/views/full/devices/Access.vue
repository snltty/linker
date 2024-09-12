<template>
     <el-checkbox-group v-model="state.checkList">
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
            ]
        });

        const getValue = ()=>{
            return state.checkList.reduce((sum,item)=>{
                return (sum | item) >>> 0;
            },0);
        }

        onMounted(()=>{
            if(allAccess.value.list[props.machineid]){
                const res = allAccess.value.list[props.machineid];
                state.checkList = access.value.reduce((arr,item)=>{
                    if(((res & item.Value) >>> 0) == item.Value){
                        arr.push(item.Value);
                    }
                    return arr;
                },[]);
            }
        })

        return {state,access,getValue};
    }
}
</script>
<style lang="stylus" scoped>
 .el-col {text-align:left;}
</style>