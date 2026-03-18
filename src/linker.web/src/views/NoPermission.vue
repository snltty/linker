<template>
    <div class="no-permission-wrap">
        <p>
            <img src="@/assets/no-permission.svg" alt="">
        </p>
        <h3>{{$t('permission.closed')}}</h3>
        <p class="flex mgt-1 fs-14">
            <router-link :to="{name:'Network'}">{{$t('permission.simple')}}</router-link>
            <span class="split"></span>
            <router-link :to="{name:'Full'}">{{$t('permission.full')}}</router-link>
            <span class="split"></span>
            <a href="javascript:;" @click="handleResetConnect">{{$t('permission.clear')}}</a>
        </p>
    </div>
</template>

<script>
export default {
    setup () {
        
        const handleResetConnect = () => {
            localStorage.setItem('api-cache', '');
            window.location.reload();
        }
        return {handleResetConnect}
    }
}
</script>

<style lang="stylus" scoped>
.split{
    padding:0 1rem;
}
.no-permission-wrap{
    padding:2rem;
    border:1px solid #ddd;
    border-radius:4px;
    position:absolute;
    background-color:#fff;
    left:50%;
    top:50%;
    transform:translateX(-50%) translateY(-50%);
    text-align:center;

    font-size:1.6rem;

}
img{height:7rem;}
</style>