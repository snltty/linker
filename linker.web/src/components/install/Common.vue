<template>
    <div class="t-c">
        <el-checkbox v-model="state.form.client" label="作为客户端" />
        <el-checkbox v-model="state.form.server" label="作为服务端" />
    </div>
</template>

<script>
import {inject, reactive} from 'vue'
import {ElMessage} from 'element-plus'
export default {
    name: 'Common',
    setup () {
        
        const step = inject('step');
        const state =  reactive({
            form: {
                client: (step.value.json.Common && step.value.json.Common.client) || false,
                server: (step.value.json.Common && step.value.json.Common.server) || false,
            }
        });
        const handleValidate = (prevJson) => {
            return new Promise((resolve, reject) => {
                if(!state.form.client && !state.form.server){
                    ElMessage.error('请选择客户端或服务端');
                    reject();
                }else{
                    resolve({
                        Common:{
                            client: state.form.client,
                            server: state.form.server,
                            modes:[
                            state.form.client ? 'client' : '',
                            state.form.server ? 'server' : ''
                            ].filter(c=>!!c)
                        }
                    });
                }
            });
        }


        return {
            state,handleValidate
        }
    }
}
</script>

<style lang="stylus" scoped>

</style>