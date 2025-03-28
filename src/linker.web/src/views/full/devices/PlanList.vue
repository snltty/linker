<template>
    <div>
        <slot></slot>
        <PlanEdit v-if="plan.showEdit" v-model="plan.showEdit" ></PlanEdit>
    </div>
</template>

<script>
import { getPlans } from '@/apis/plan';
import { onMounted, onUnmounted,  provide,  ref } from 'vue';
import PlanEdit from './PlanEdit.vue';
export default {
    components: { PlanEdit },
    props:['machineid','category','handles'],
    setup (props) {
        
        const plan = ref({
            timer:0,
            list:{},
            current:{},
            showEdit:false,
            category:props.category||'',
            handles:props.handles||[],
            triggers:[],
            methods:[
                {label:'手动',value:0},
                {label:'启动后',value:1},
                {label:'到点',value:100},
                {label:'定时',value:101},
                {label:'Cron',value:102},
                {label:'触发',value:103},
            ]
        });
        provide('plan',plan);
        const _getPlans = () => {
            clearTimeout(plan.value.timer);
            getPlans(props.machineid,props.category).then((res) => {
                console.log(res);

                plan.value.timer = setTimeout(_getPlans,1000);
            }).catch(()=>{
                plan.value.timer = setTimeout(_getPlans,1000);
            });
        }
        onMounted(()=>{
            _getPlans();
            console.log(props);
        });
        onUnmounted(()=>{
            clearTimeout(plan.value.timer);
        })

        return {plan}
    }
}
</script>

<style lang="stylus" scoped>
</style>