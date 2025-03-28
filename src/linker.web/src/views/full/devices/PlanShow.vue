<template>
    <a href="javascript:;" class="a-line" @click="handleEdit">
        <span v-if="item">{{ item.Rule }}</span>
        <span v-else>未设置</span>
    </a>
</template>

<script>
import { computed, inject } from 'vue';

export default {
    props: ['keyid','handle'],
    setup (props) {
        
        const plan = inject('plan');
        const item = computed(()=>plan.value.list[`${props.keyid}-${props.handle}`]);
        const handleEdit = () => {
            plan.value.current = item.value || {
                Id:0,
                Category:plan.value.category,
                Key:`${props.keyid}`,
                Handle:props.handle,
                Value:'',
                Disabled:false,
                TriggerHandle:'',
                Method:103,
                Rule:''
            };
            plan.value.triggers = JSON.parse(JSON.stringify(plan.value.handles.filter(c=>c.value != props.handle)));
            plan.value.showEdit = true;
        }

        return {item,handleEdit}
    }
}
</script>

<style lang="stylus" scoped>

</style>