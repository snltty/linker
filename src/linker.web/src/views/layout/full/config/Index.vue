<template>
    <div class="servers-wrap h-100" >
        <AccessShow value="Config">
            <Config></Config>
        </AccessShow>
    </div>
</template>
<script>
import Config from './Config.vue';
export default {
    components:{Config},
    setup(props) {
        return {}
    }
}
</script>
<style lang="stylus" scoped>
.servers-wrap{
    padding:1rem
    font-size:1.3rem;
    color:#555;
    box-sizing:border-box;
    a{color:#333;}
}
</style>