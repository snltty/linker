<template>
    <template v-if="state.loading">
        <div class="adv-wrap">
            <el-skeleton animated >
                <template #template>
                    <el-skeleton-item style="opacity: 0.5; height: 3rem;"/>
                </template>
            </el-skeleton>
        </div>
    </template>
    <template v-else>
        <div class="adv-wrap" v-if="state.html">
            <div class="inner" v-html="state.html"></div>
        </div>
    </template>
</template>

<script>
import { nextTick, onMounted, reactive } from 'vue';

export default {
    setup () {

        const state = reactive({
            html:'',
            loading:true,
            timer:0   
        });

        const advFn = ()=>{ 
            clearTimeout(state.timer);
            fetch(`https://linker.snltty.com/adv.html?t=${Date.now()}`).then(res=>res.text()).then(res=>{
                state.html = res;
                state.loading = false;
                nextTick(()=>{
                    window.dispatchEvent(new Event('resize'));
                });
            }).catch((err)=>{
                console.log(err);
                setTimeout(advFn,1000);
            });
        }

        onMounted(()=>{
            advFn();
        });

        return {
            state
        }
    }
}
</script>

<style lang="stylus" scoped>
.adv-wrap{
    padding:1.5rem 1.5rem 0 1.5rem;
    margin-bottom:-0.5rem

    .inner{
        border:1px solid var(--table-border-color);
        padding:.4rem;
        // background: #fff;
    }
}
</style>