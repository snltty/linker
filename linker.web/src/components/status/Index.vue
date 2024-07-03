<template>
    <div class="status-wrap flex">
        <div class="copy"><a href="https://github.com/snltty/linker" target="_blank">snltty©linker v1.1.1.2</a></div>
        <div class="flex-1"></div>
        <div class="api"><Api></Api></div>
        <div class="server"><Server></Server></div>
    </div>
</template>
<script>
import Api from './Api.vue'
import Server from './Server.vue'
export default {
    components:{Api,Server},
    setup(props) {
        
    }
}
</script>
<style lang="stylus" scoped>
.status-wrap{
    border-top:1px solid #ddd;
    background-color:#f5f5f5;
    height:3rem;
    line-height:3rem;
    font-size:1.2rem;
    color:#555;
    

    .copy{
        padding-left:.5rem;
        a{color:#555;}
    }
}
</style>