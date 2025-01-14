<template>
    <div class="no-permission-wrap">
        <p>
            <img src="@/assets/no-permission.svg" alt="">
        </p>
        <h3>此地禁止通行</h3>
        <p class="flex" style="margin-top:1rem;font-size:1.4rem;">
            <router-link :to="{name:'Network'}">简单管理</router-link>
            <span style="padding:0 1rem"></span>
            <router-link :to="{name:'Full'}">专业管理</router-link>
            <span style="padding:0 1rem"></span>
            <a href="javascript:;" @click="handleResetConnect">清除连接</a>
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