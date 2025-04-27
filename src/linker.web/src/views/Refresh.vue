<template>
    <div class="refresh-wrap" v-if="state.show" :style="{top:`${state.dy/4}px`}">
        <el-progress type="circle" :percentage="state.percentage" width="50">
            <template #default="{ percentage }">
                <span class="percentage-value">{{ percentage }}%</span>
            </template>
        </el-progress>
    </div>
</template>

<script>
import {  onMounted, reactive } from 'vue';

export default {
    setup () {
        
        const state = reactive({
            percentage:50,
            show:false,
            touchY:0,
            dy:0,
            start:0,
        });

        const touchStart = (event)=>{
            state.percentage = 0;
            const touch = event.touches[0];
            state.touchY = touch.clientY;
            state.start = Date.now();
        }
        const touchMove = (event)=>{
            const touch = event.touches[0];
            const y = touch.clientY;
            if(state.show == false && Date.now() - state.start > 300){
                state.show = true;
                state.touchY = y;
            }
            if(state.show){
                const dy = parseInt((y - state.touchY));
                state.dy = dy;
                state.percentage = dy;
                if(state.percentage > 100) state.percentage = 100;
            }
            
        }
        const touchEnd = (event)=>{
            if(state.percentage >= 100 && state.show){
                window.location.reload();
            }
            state.show = false;
            state.dy = 0;
        }

        onMounted(()=>{
            document.addEventListener('touchstart', touchStart);
            document.addEventListener('touchmove', touchMove);
            document.addEventListener('touchend',touchEnd);
            document.addEventListener('touchcancel',touchEnd);
        })

        return {state}
    }
}
</script>

<style lang="stylus" scoped>
.refresh-wrap{
    position:fixed;
    left:50%;
    top:1rem;
    transform:translateX(-50%);
    border-radius:.4rem;
    background-color:#fff;
    padding:.4rem;
    border:1px solid #ddd;
    box-shadow:0 0 1rem rgba(0,0,0,.1);
    z-index 9999999;
}
</style>