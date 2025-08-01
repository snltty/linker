<template>
    <div>
        <slot></slot>
        <PlanEdit v-if="plan.showEdit" v-model="plan.showEdit" ></PlanEdit>
    </div>
</template>

<script>
import { getPlans, removePlan } from '@/apis/plan';
import { onMounted, onUnmounted,  provide,  ref } from 'vue';
import PlanEdit from './PlanEdit.vue';
export default {
    components: { PlanEdit },
    props:['machineid','category','handles'],
    setup (props) {
        
        const plan = ref({
            machineid:props.machineid,
            timer:0,
            list:{},
            current:{},
            showEdit:false,
            category:props.category||'',
            handles:props.handles||[],
            handleJson:(props.handles||[]).reduce((json,item,index)=>{ json[item.value] = item.label; return json;  },{}),
            triggers:[],
            methods:[
                {label:'手动',value:0},
                {label:'启动后',value:1},
                {label:'到点',value:2},
                {label:'定时',value:4},
                {label:'Cron',value:8},
                {label:'触发',value:16},
            ]
        });
        provide('plan',plan);
        const _getPlans = () => {
            clearTimeout(plan.value.timer);
            getPlans(plan.value.machineid,props.category).then((res) => {
                plan.value.list =  res.reduce((json,item,index)=>{
                    json[`${item.Key}-${item.Handle}`] = item;
                    return json;
                },{});
                plan.value.timer = setTimeout(_getPlans,1000);
            }).catch(()=>{
                plan.value.timer = setTimeout(_getPlans,1000);
            });
        }
        const remove = (key,handle)=>{
            const item = plan.value.list[`${key}-${handle}`];
            if(item){
                removePlan(plan.value.machineid,item.Id).then(()=>{
                    _getPlans();
                });
            }
        }

        onMounted(()=>{
            _getPlans();
        });
        onUnmounted(()=>{
            clearTimeout(plan.value.timer);
        })

        return {plan,remove}
    }
}
</script>

<style lang="stylus" scoped>
</style>