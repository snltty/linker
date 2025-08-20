<template>
    <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="计划任务" top="2vh" width="450">
       <div>
           <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item label="执行操作" prop="Handle">
                    <el-select v-model="state.ruleForm.Handle"  disabled>
                        <el-option v-for="(item,index) in plan.handles" :value="item.value" :label="item.label"></el-option>
                    </el-select>
                </el-form-item>  
                <el-form-item label="执行方式" prop="Method">
                    <el-select v-model="state.ruleForm.Method"  @change="handleChange">
                        <el-option v-for="(item,index) in plan.methods" :value="item.value" :label="item.label"></el-option>
                    </el-select>
                    <strong class="mgl-2" v-if="state.ruleForm.Method >= 2">
                        {{ state.ruleForm.Rule }}
                    </strong>
                </el-form-item>
                <el-form-item label="在" prop="Rule" v-if="state.ruleForm.Method == 2">
                    <div class="w-100">
                        <el-select v-model="state.ruleAt.type"  @change="handleChange">
                            <el-option :value="2" label="每月"></el-option>
                            <el-option :value="3" label="每日"></el-option>
                            <el-option :value="4" label="每时"></el-option>
                            <el-option :value="5" label="每分"></el-option>
                        </el-select>  <span>的</span>
                    </div>
                    <div class="w-100 mgt-1">
                        <el-input v-trim @change="handleChange" v-if="state.ruleAt.type < 2" v-model="state.ruleAt.month" ><template #append>月</template></el-input>
                        <el-input v-trim @change="handleChange" v-if="state.ruleAt.type < 3" :class="{'mgl-1':state.ruleAt.type < 2}" v-model="state.ruleAt.day" ><template #append>日</template></el-input>
                        <el-input v-trim @change="handleChange" v-if="state.ruleAt.type < 4" :class="{'mgl-1':state.ruleAt.type < 3}" v-model="state.ruleAt.hour" ><template #append>时</template></el-input>
                        <el-input v-trim @change="handleChange" v-if="state.ruleAt.type < 5" :class="{'mgl-1':state.ruleAt.type < 4}" v-model="state.ruleAt.min" ><template #append>分</template></el-input>
                        <el-input v-trim @change="handleChange" :class="{'mgl-1':state.ruleAt.type < 5}" v-model="state.ruleAt.sec" ><template #append>秒</template></el-input>
                    </div>
                </el-form-item>
                <el-form-item label="每" prop="Rule" v-if="state.ruleForm.Method == 4">
                    <div class="w-100">
                        <el-input v-trim @change="handleChange" v-model="state.ruleTimer.year" ><template #append>年</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleTimer.month" ><template #append>月</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleTimer.day" ><template #append>日</template></el-input>
                    </div>
                    <div class="w-100 mgt-1">
                        <el-input v-trim @change="handleChange" v-model="state.ruleTimer.hour" ><template #append>时</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleTimer.min" ><template #append>分</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleTimer.sec" ><template #append>秒</template></el-input>
                    </div>
                </el-form-item>
                <el-form-item label="Cron" prop="Rule" v-if="state.ruleForm.Method == 8">
                    <div class="w-100">
                        <el-input v-trim @change="handleChange" v-model="state.ruleCron.sec" ><template #append>秒</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleCron.min" ><template #append>分</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleCron.hour" ><template #append>时</template></el-input>
                    </div>
                    <div class="w-100 mgt-1">
                        <el-input v-trim @change="handleChange" v-model="state.ruleCron.day" ><template #append>日</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleCron.month" ><template #append>月</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleCron.week" ><template #append>周</template></el-input>
                    </div>
                </el-form-item>
                <el-form-item label="在" prop="Rule" v-if="state.ruleForm.Method == 16">
                    <div class="w-100">
                        <el-select v-model="state.ruleForm.TriggerHandle"  @change="handleChange">
                            <el-option v-for="(item,index) in plan.triggers" :value="item.value" :label="item.label"></el-option>
                        </el-select> <span>的</span>
                    </div>
                    <div class="mgt-1 w-100">
                        <el-input v-trim @change="handleChange" v-model="state.ruleTrigger.year" ><template #append>年</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleTrigger.month" ><template #append>月</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleTrigger.day" ><template #append>日</template></el-input>
                    </div>
                    <div class="mgt-1 w-100">
                        <el-input v-trim @change="handleChange" v-model="state.ruleTrigger.hour" ><template #append>时</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleTrigger.min" ><template #append>分</template></el-input>
                        <el-input v-trim @change="handleChange" class="mgl-1" v-model="state.ruleTrigger.sec" ><template #append>秒</template></el-input>
                        <span>后</span>
                    </div>
                </el-form-item> 
                <el-form-item label="内容" prop="Value">
                    <el-input v-trim type="textarea" resize="none" rows="5" v-model="state.ruleForm.Value"></el-input>
                </el-form-item>
                <el-form-item label="禁用" prop="Disabled">
                    <el-switch v-model="state.ruleForm.Disabled" />
                </el-form-item>

                <el-form-item label="" prop="Btns">
                   <div class="t-c w-100">
                       <el-button @click="state.show = false">取消</el-button>
                       <el-button type="primary" @click="handleSave" :loading="state.loading">确认</el-button>
                   </div>
                </el-form-item>
           </el-form>
       </div>
   </el-dialog>
</template>
<script>
import { addPlan } from '@/apis/plan';
import { inject, onMounted, reactive, ref, watch } from 'vue';

export default {
   props: ['data','modelValue'],
   emits: ['change','update:modelValue'],
   setup(props, { emit }) {

        const regex = /(\d+|\*)-(\d+|\*)-(\d+|\*)\s+(\d+|\*):(\d+|\*):(\d+|\*)/;
        const regexNumber = /(\d+)-(\d+)-(\d+)\s+(\d+):(\d+):(\d+)/;
        const regexCorn = /(.+)\s+(.+)\s+(.+)\s+(.+)\s+(.+)\s+(.+)/;
        const ruleFormRef = ref(null);
        const plan = inject('plan');
        if(!plan.value.current.TriggerHandle && plan.value.triggers.length > 0){
            plan.value.current.TriggerHandle = plan.value.triggers[0].value;
        }
        
        const state = reactive({
           show: true,
           loading: false,
            ruleCron:{
                week:'*',
                month:'*',
                day:'*',
                hour:'*',
                min:'*',
                sec:'30',
            },
            ruleAt:{
                type:3,
                month:'*',
                day:'*',
                hour:0,
                min:0,
                sec:0,
            },
            ruleTimer:{
                year:0,
                month:0,
                day:0,
                hour:0,
                min:0,
                sec:30,
            },
            ruleTrigger:{
                year:0,
                month:0,
                day:0,
                hour:0,
                min:0,
                sec:30,
           },
           ruleForm: {
                Id: plan.value.current.Id,
                Category: plan.value.current.Category,
                Key: plan.value.current.Key,
                Value: plan.value.current.Value,
                Rule: plan.value.current.Rule,
                Handle: plan.value.current.Handle,
                Method: plan.value.current.Method,
                Disabled:plan.value.current.Disabled,
                TriggerHandle:plan.value.current.TriggerHandle
           },
           rules: {}
        });
        watch(() => state.show, (val) => {
            if (!val) {
               setTimeout(() => {
                   emit('update:modelValue', val);
               }, 300);
            }
        });
     
        const decodeRuleJson = {
            2:(rule)=>{
                rule = rule || `*-*-* 0:0:0`;
                if(regex.test(rule) == false){
                    return;
                }
                const [,year,month,day,hour,minute,second] = rule.match(regex);
                if(minute == '*') state.ruleAt.type = 5;
                else if(hour == '*') state.ruleAt.type = 4;
                else if(day == '*') state.ruleAt.type = 3;
                else if(month == '*') state.ruleAt.type = 2;
                state.ruleAt.year = year;
                state.ruleAt.month = month;
                state.ruleAt.day = day;
                state.ruleAt.hour = hour;
                state.ruleAt.min = minute;
                state.ruleAt.sec = second;
            },
            4:(rule)=>{
                rule = rule || `0-0-0 0:0:30`;
                if(regexNumber.test(rule) == false){
                    return;
                }
                const [,year,month,day,hour,minute,second] = rule.match(regexNumber);
                state.ruleTimer.year = year;
                state.ruleTimer.month = month;
                state.ruleTimer.day = day;
                state.ruleTimer.hour = hour;
                state.ruleTimer.min = minute;
                state.ruleTimer.sec = second;
            },
            8:(rule)=>{
                rule = rule || `30 * * * * ?`;
                if(regexCorn.test(rule) == false){
                    return;
                }
                const [,second,minute,hour,day,month,week] = rule.match(regexCorn);
                state.ruleCron.sec = second;
                state.ruleCron.min = minute;
                state.ruleCron.hour = hour;
                state.ruleCron.day = day;
                state.ruleCron.month = month;
                state.ruleCron.week = week;
            },
            16:(rule)=>{
                rule = rule || `0-0-0 0:0:30`;
                if(regexNumber.test(rule) == false){
                    return;
                }
                const [,year,month,day,hour,minute,second] = rule.match(regexNumber);
                state.ruleTrigger.year = year;
                state.ruleTrigger.month = month;
                state.ruleTrigger.day = day;
                state.ruleTrigger.hour = hour;
                state.ruleTrigger.min = minute;
                state.ruleTrigger.sec = second;
            }
        };
        const decodeRule = ()=>{
            if(state.ruleForm.Method in decodeRuleJson){
                decodeRuleJson[state.ruleForm.Method](state.ruleForm.Rule);
            }
        }

        const buildRuleJson = {
            2:()=>{
                switch (state.ruleAt.type) {
                    case 2:
                        return `*-*-${state.ruleAt.day} ${state.ruleAt.hour}:${state.ruleAt.min}:${state.ruleAt.sec}`;
                    break;
                    case 3:
                    return `*-*-* ${state.ruleAt.hour}:${state.ruleAt.min}:${state.ruleAt.sec}`;
                    break;
                    case 4:
                    return `*-*-* *:${state.ruleAt.min}:${state.ruleAt.sec}`;
                    break;
                    case 5:
                    return `*-*-* *:*:${state.ruleAt.sec}`;
                    break;
                } 
                return '';
            },
            4:()=>`${state.ruleTimer.year}-${state.ruleTimer.month}-${state.ruleTimer.day} ${state.ruleTimer.hour}:${state.ruleTimer.min}:${state.ruleTimer.sec}`,
            8:()=>`${state.ruleCron.sec} ${state.ruleCron.min} ${state.ruleCron.hour} ${state.ruleCron.day} ${state.ruleCron.month} ${state.ruleCron.week}`,
            16:()=>`${state.ruleTrigger.year}-${state.ruleTrigger.month}-${state.ruleTrigger.day} ${state.ruleTrigger.hour}:${state.ruleTrigger.min}:${state.ruleTrigger.sec}`,
        }
        const buildRule= ()=>{
            if(state.ruleForm.Method in buildRuleJson){
                state.ruleForm.Rule = buildRuleJson[state.ruleForm.Method]();
            }
        }
        const handleChange = () => {
            buildRule();
        }

        const handleSave = () => {
            const json = JSON.parse(JSON.stringify(state.ruleForm));
            
            state.loading = true;
            addPlan(plan.value.machineid,json).then((res)=>{
                state.loading = false;
                state.show = false;
            }).catch(()=>{
                state.loading = false;
            })
        }

        onMounted(()=>{
            decodeRule();
            handleChange();
        });
        return {
          state, ruleFormRef,plan,handleChange,  handleSave
        }
   }
}
</script>
<style lang="stylus" scoped>
.el-select{width:10rem;}
.el-input{width:8rem;}
</style>