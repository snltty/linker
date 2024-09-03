<template>
    <div class="running-version-wrap flex">
        <span><a href="javascript:;" @click="handleEdit">配置版本 : {{version || 1}}</a></span>
        <div><span>，配置自动同步，除非</span><el-checkbox v-model="disableSyncValue" @change="handleSync">关闭自动同步</el-checkbox></div>
        <span class="flex-1"></span>
        <slot></slot>
    </div>
</template>
<script>
import {updateDisableSync, updateVersion} from '@/apis/running'
import { injectGlobalData } from '@/provide';
import { ElMessageBox } from 'element-plus';
import { computed, ref, watch } from 'vue'
export default {
    props:['ckey'],
    setup(props) {
        const globalData = injectGlobalData();
        const version = computed(()=>globalData.value.config.Running.Versions[props.ckey]);
        
        const disableSync = computed(()=>globalData.value.config.Running.DisableSyncs[props.ckey] || false);
        watch(()=>disableSync.value,()=>{
            disableSyncValue.value = disableSync.value;
        });
        const disableSyncValue = ref(disableSync.value);


        const handleEdit = () => {
            ElMessageBox.prompt('输入你要修改到的版本', '修改版本', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                inputValue:version.value,
                inputPattern:/\d+/,
                inputErrorMessage: 'Invalid Number',
            }).then(({ value }) => {
                value = +value;
                if(isNaN(value)) return ;
                updateVersion({key:props.ckey,version:value});
            }).catch(()=>{

            });
        }
        const handleSync = ()=>{
            updateDisableSync({key:props.ckey,sync:disableSyncValue.value})
        }

        return {
            version,disableSyncValue,handleEdit,handleSync
        }
    }
}
</script>
<style lang="stylus" scoped>
    .running-version-wrap{
        span{
            vertical-align top
        }
        padding:0 0 1rem 0;
        line-height:3.2rem
    }
</style>