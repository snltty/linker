<template>
    <div class="run-btn green">
        <img src="@/assets/cell.svg" alt="" />
    </div>
</template>

<script>
export default {
    setup () {
        

        return {}
    }
}
</script>

<style lang="stylus" scoped>
@keyframes loading {
    from{transform:rotate(0deg)}
    to{transform:rotate(360deg)}
}
.run-btn{
    padding:3rem
    border-radius: 50%;
    text-decoration: none !important;
    white-space: nowrap;
    display: inline-block;
    vertical-align: baseline;
    position: relative;
    cursor: pointer;
    background-repeat: no-repeat;
    background-position: bottom left;
    background-image: url('@/assets/button_bg.png');
    background-position: bottom left, top right, 0 0, 0 0;
    background-clip: border-box;
    box-shadow: 0 0 1px #fff inset;
    -webkit-transition: background-position 1s;
    -moz-transition: background-position 1s;
    transition: background-position 1s, box-shadow 0.3s;

    &.gray {
        box-shadow: 0 0 0 4px #d1d8e261;
        color: #525252 !important;
        border: 1px solid  #b7b7b7 !important;
        background-image: url('@/assets/button_bg.png'), url('@/assets/button_bg.png'), -webkit-gradient(radial, 50% 100%, 0, 50% 100%, 100, from(#ffffff), to(rgba(197, 199, 202, 0))), -webkit-gradient(linear, 0% 0%, 0% 100%, from(#616163), to(#919397));

        .el-icon {
            color: #abb1b7;
        }
    }

    &.gray:hover {
        box-shadow: 0 0 0 6px #d1d8e261;
        background-image: url('@/assets/button_bg.png'), url('@/assets/button_bg.png'),-webkit-gradient(radial, 50% 100%, 0, 50% 100%, 100, from(#ffffff), to(rgba(197, 199, 202, 0))), -webkit-gradient(linear, 0% 0%, 0% 100%, from(#616163), to(#919397));
    }

    &.green {
        box-shadow: 0 0 0 4px #aee5b696;
        color: #525252 !important;
        border: 1px solid #78c18a !important;
        background-color: #79be1e;
        background-image: url('@/assets/button_bg.png'), url('@/assets/button_bg.png'),-webkit-gradient(radial, 50% 100%, 0, 50% 100%, 100, from(#00eb28), to(rgba(162, 211, 30, 0))), -webkit-gradient(linear, 0% 0%, 0% 100%, from(#0a5f21), to(#17b34e));

        .el-icon {
            color: #43873f;
        }
    }

    &.green:hover {
        box-shadow: 0 0 0 6px #aee5b696;
        background-color: #89d228;
        background-image: url('@/assets/button_bg.png'), url('@/assets/button_bg.png'),-webkit-gradient(radial, 50% 100%, 0, 50% 100%, 100, from(#00eb28), to(rgba(162, 211, 30, 0))), -webkit-gradient(linear, 0% 0%, 0% 100%, from(#0a5f21), to(#17b34e))
    }

    &:hover {
        background-position: top left;
        background-position: top left, bottom right, 0 0, 0 0;
    }

    &:active {
        bottom: -1px;
    }

    img{
        height:5rem;
    }
    &.loading,&.green{
        img{
            animation:loading 1s linear infinite;
        }
    }
}

</style>