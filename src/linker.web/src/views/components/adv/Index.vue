<template>
    <div class="adv-wrap" v-if="state.html">
        <div class="inner" v-html="state.html"></div>
    </div>
</template>

<script>
import { injectGlobalData } from '@/provide';
import { nextTick, onMounted, reactive } from 'vue';

export default {
    setup () {

        const state = reactive({
            html:''      
        });

        onMounted(()=>{
            fetch('https://linker.snltty.com/adv.html').then(res=>res.text()).then(res=>{
                state.html = res;
                nextTick(()=>{
                    window.dispatchEvent(new Event('resize'));
                });
            }).catch((err)=>{
                console.log(err);
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

    .inner{border:1px solid #eee;padding:.6rem;border-radius.4rem;}
}
</style>