<template>
    <div class="w-100">
        <div>
            <span class="yellow">填写局域网IP，使用NAT转发</span>
        </div>
        <div class="wrap">
            <template v-for="(item, index) in state.lans" :key="index">
                <div class="flex" style="margin-bottom:.6rem">
                    <div >
                        <el-input v-model="item.IP" style="width:14rem" />
                        <span>/</span>
                        <el-input @change="handleMaskChange(index)" v-model="item.PrefixLength"
                            style="width:4rem" />
                    </div>
                    <div class="pdl-10">
                        <el-checkbox v-model="item.Disabled" label="禁用记录" style="vertical-align: middle;"/>
                    </div>
                    <div class="pdl-10">
                        <el-button type="danger" @click="handleDel(index)" size="small"><el-icon>
                                <Delete />
                            </el-icon></el-button>
                        <el-button type="primary" @click="handleAdd(index)" size="small"><el-icon>
                                <Plus />
                            </el-icon></el-button>
                    </div>
                </div>
            </template>
        </div>
    </div>
</template>
<script>
import { reactive } from 'vue';
import { useTuntap } from './tuntap';
import { Delete, Plus, Warning, Refresh } from '@element-plus/icons-vue'
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { Delete, Plus, Warning, Refresh },
    setup(props) {

        const tuntap = useTuntap();
        const state = reactive({
            lans: tuntap.value.current.Lans.slice(0)
        });
        if (state.lans.length == 0) {
            state.lans.push({ IP: '0.0.0.0', PrefixLength: 24 });
        }

        const handleMaskChange = (index) => {
            var value = +state.lans[index].PrefixLength;
            if (value > 32 || value < 16 || isNaN(value)) {
                value = 24;
            }
            state.lans[index].PrefixLength = value;
        }
        const handleDel = (index) => {
            state.lans.splice(index, 1);
            if (state.lans.length == 0) {
                handleAdd(0);
            }
        }

        const handleAdd = (index) => {
            state.lans.splice(index + 1, 0, { IP: '0.0.0.0', PrefixLength: 24 });
        }
        const getData = ()=>{
            return state.lans.map(c => { c.PrefixLength = +c.PrefixLength; return c; });
        }

        return {
            state,handleMaskChange,handleDel,handleAdd,getData
        }
    }
}
</script>
<style lang="stylus" scoped>
.wrap{
    padding-right:1rem;
}
</style>