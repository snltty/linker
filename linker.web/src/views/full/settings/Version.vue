<template>
    <div class="running-version-wrap flex">
        <span>配置版本 : {{version || 1}}</span>
        <el-button size="small" @click=handleEdit>手动修改版本</el-button>
        <span>高版本一端自动同步到低版本一端</span>
        <span class="flex-1"></span>
        <slot></slot>
    </div>
</template>
<script>
import {updateVersion} from '@/apis/running'
import { injectGlobalData } from '@/provide';
import { ElMessageBox } from 'element-plus';
import { computed } from 'vue'
export default {
    props:['ckey'],
    setup(props) {
        const globalData = injectGlobalData();
        const version = computed(()=>globalData.value.config.Running.Versions[props.ckey]);


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

        return {
            version,handleEdit
        }
    }
}
</script>
<style lang="stylus" scoped>
    .running-version-wrap{
        span{
            vertical-align:middle
        }
        padding:0 0 1rem 0;
        line-height:2.4rem
    }
</style>