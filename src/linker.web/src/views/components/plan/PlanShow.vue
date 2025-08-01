<template>
    <a href="javascript:;" class="a-line" @click="handleEdit">
        <span v-if="item">{{ rule }}</span>
        <span v-else>未设置</span>
    </a>
</template>

<script>
import { computed, inject } from 'vue';

export default {
    props: ['keyid','handle'],
    setup (props) {
        
        const regex = /(\d+|\*)-(\d+|\*)-(\d+|\*)\s+(\d+|\*):(\d+|\*):(\d+|\*)/;
        const regexNumber = /(\d+)-(\d+)-(\d+)\s+(\d+):(\d+):(\d+)/;
        const ruleTrans = {
            0:()=>`手动`,
            1:()=>`网络启动后`,
            2:(item,rule)=>{
                if(regex.test(rule) == false){
                    return rule;
                }
                const [,year,month,day,hour,minute,second] = rule.match(regex);
                if(minute == '*') return `每分钟的${second}秒`;
                if(hour == '*') return `每小时的${minute}分${second}秒`;
                if(day == '*') return `每天的${hour}时${minute}分${second}秒`;
                if(month == '*') return `每月的${day}日${hour}时${minute}分${second}秒`;
                if(year == '*') return `每年的${month}月${day}日${hour}时${minute}分${second}秒`;
            },
            4:(item,rule)=>{
                if(regexNumber.test(rule) == false){
                    return rule;
                }
                const [,year,month,day,hour,minute,second] = rule.match(regexNumber);
                const arr = [];
                if(year != '0') arr.push(`${year}年`);
                if(month != '0') arr.push(`${month}月`);
                if(day != '0') arr.push(`${day}日`);
                if(hour != '0') arr.push(`${hour}时`);
                if(minute != '0') arr.push(`${minute}分`);
                if(second != '0') arr.push(`${second}秒`);
                return `每${arr.join('')}`
            },
            8:(item,rule)=>{
                return `Cron : ${rule}`;
            },
            16:(item,rule)=>{
                if(regexNumber.test(rule) == false){
                    return rule;
                }
                const [,year,month,day,hour,minute,second] = rule.match(regexNumber);
                const arr = [];
                if(year != '0') arr.push(`${year}年`);
                if(month != '0') arr.push(`${month}月`);
                if(day != '0') arr.push(`${day}日`);
                if(hour != '0') arr.push(`${hour}时`);
                if(minute != '0') arr.push(`${minute}分`);
                if(second != '0') arr.push(`${second}秒`);
                return `在【${plan.value.handleJson[item.TriggerHandle]}】的${arr.join('')}后`
            },
        }

        const plan = inject('plan');
        const item = computed(()=>plan.value.list[`${props.keyid}-${props.handle}`]);
        const rule = computed(()=>{
            if(!item.value) return '';
            const method = item.value.Method;
            if(ruleTrans[method]){
                return ruleTrans[method](item.value,item.value.Rule);
            }
            return item.value.Rule;
        });
        const handleEdit = () => {
            plan.value.current = item.value || {
                Id:0,
                Category:plan.value.category,
                Key:`${props.keyid}`,
                Handle:props.handle,
                Value:'',
                Disabled:false,
                TriggerHandle:'',
                Method:2,
                Rule:''
            };
            plan.value.triggers = JSON.parse(JSON.stringify(plan.value.handles.filter(c=>c.value != props.handle)));
            plan.value.showEdit = true;
        }

        return {item,rule,handleEdit}
    }
}
</script>

<style lang="stylus" scoped>

</style>