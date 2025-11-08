<template>
    <PcShow>
        <div class="image">
            <a href="javascript:;" @click="handleBg"><el-icon><PictureRounded /></el-icon></a>
            <input type="file" id="file-input">
        </div>
    </PcShow>
</template>

<script>
import {PictureRounded} from '@element-plus/icons-vue'
import { ElMessageBox } from 'element-plus';
import {  onMounted } from 'vue';
export default {
    components:{PictureRounded},
    props:['name'],
    setup(props) {

        const key = `bg-${props.name}`;

        const handleBg = ()=>{
            if(localStorage.getItem(key)){
                ElMessageBox.confirm('清除背景？','Warning',
                    {
                        confirmButtonText: '确定',
                        cancelButtonText: '取消',
                        type: 'warning',
                    }
                ).then(() => {
                    setBg();
                }).catch(()=>{});
            }else{
                document.getElementById('file-input').click();
            }
        }
        const onFile = (event)=>{
            const file = event.target.files[0];
            if (file) {
                try{
                    const reader = new FileReader();
                    reader.onload = function(e) {
                        setBg(e.target.result);
                    };
                    reader.readAsDataURL(file);
                }catch(e){}
            }
            event.target.value = '';
        }
        const setBg = (base64)=>{
            if(base64){
                document.body.className = 'sunny';
                localStorage.setItem(key,base64);
                document.body.style=`background-image:url(${base64})`;
            }else{
                document.body.className = '';
                document.body.style = '';
                localStorage.setItem(key,'');
            }
        }
        onMounted(()=>{
            const input = document.getElementById('file-input');
            if(input){
                input.addEventListener('change', onFile);
            }
            
            setBg(localStorage.getItem(key));
        })

        return {
            handleBg
        }
    }
}
</script>

<style lang="stylus" scoped>
#file-input{opacity :0;position absolute;z-index :-1}
.el-icon{
    font-size:1.6rem;
    vertical-align:middle;
    color:#555;
}
.image{
    padding-right:1rem;
}
</style>