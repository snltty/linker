<template>
    <div>
        <el-transfer class="src-tranfer mgt-1"
            v-model="state.srcIdValues"
            filterable
            :filter-method="srcFilterMethod"
            :data="state.srcIds"
            :titles="[$t('firewall.unselect'), $t('firewall.selected')]"
            :props="{
                key: 'MachineId',
                label: 'MachineName',
            }"
        />
    </div>
</template>

<script>
import { getSignInNames } from '@/apis/signin';
import { onMounted, reactive } from 'vue';
export default {
    setup () {
        const state = reactive({
            srcIdValues:[],
            srcIds:[],
        });
        const _getSignInNames = ()=>{
            state.loading = true;
            getSignInNames().then((res)=>{
                state.loading = false;
                state.srcIds = res;
            }).catch((e)=>{
                state.loading = false;
            });
        }
        const srcFilterMethod = (query, item) => {
            return item.MachineName.toLowerCase().includes(query.toLowerCase())
        }

        const getIds = ()=>{
            return state.srcIdValues;
        }

        onMounted(()=>{
            _getSignInNames();
        });

        return {
            state,srcFilterMethod,getIds
        }
    }
}
</script>
<style lang="stylus">
.el-transfer.src-tranfer .el-transfer__buttons .el-button{display:block;}
.el-transfer.src-tranfer .el-transfer__buttons .el-button:nth-child(2){margin:1rem 0 0 0;}
</style>
<style lang="stylus" scoped>
</style>