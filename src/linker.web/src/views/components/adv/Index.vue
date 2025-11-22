<template>
    <template v-if="state.loading">
        <div class="adv-wrap">
            <el-skeleton animated >
                <template #template>
                    <el-skeleton-item/>
                </template>
            </el-skeleton>
        </div>
    </template>
    <template v-else>
        <div class="adv-wrap" v-if="state.html">
            <div class="inner skeleton-animation" v-html="state.html"></div>
        </div>
    </template>
</template>

<script>
import { nextTick, onMounted, reactive } from 'vue';

export default {
    setup () {

        const state = reactive({
            html:'',
            loading:true   
        });

        onMounted(()=>{
            fetch(`https://linker.snltty.com/adv.html?t=${Date.now()}`).then(res=>res.text()).then(res=>{
                state.html = res;
                state.loading = false;
                nextTick(()=>{
                    window.dispatchEvent(new Event('resize'));
                });
            }).catch((err)=>{
                console.log(err);
                state.loading = false;
            });
        });

        return {
            state
        }
    }
}
</script>

<style lang="stylus" scoped>
.adv-wrap{
    padding:1rem 1rem 0 1rem;

    .inner{
        border:1px solid #ddd;
        padding:.6rem;
        border-radius.4rem;
        box-shadow: 0 0 6px 2px rgba(0, 0, 0, 0.05);
    }
}
</style>