"use strict";(self["webpackChunklinker_web"]=self["webpackChunklinker_web"]||[]).push([[424],{6424:function(e,n,t){t.r(n),t.d(n,{default:function(){return h}});var o=t(6768);const r={class:"action-wrap"},s={class:"t-c"};function a(e,n,t,a,l,c){const i=(0,o.g2)("el-input"),u=(0,o.g2)("el-button"),d=(0,o.g2)("el-card");return(0,o.uX)(),(0,o.CE)("div",r,[(0,o.bF)(d,{shadow:"never"},{header:(0,o.k6)((()=>[(0,o.eW)("设置定义验证的静态Json参数")])),footer:(0,o.k6)((()=>[(0,o.Lk)("div",s,[(0,o.bF)(u,{type:"success",onClick:a.handleSave},{default:(0,o.k6)((()=>[(0,o.eW)("确定更改")])),_:1},8,["onClick"])])])),default:(0,o.k6)((()=>[(0,o.Lk)("div",null,[(0,o.bF)(i,{modelValue:a.state.list,"onUpdate:modelValue":n[0]||(n[0]=e=>a.state.list=e),rows:10,type:"textarea",resize:"none",onChange:a.handleSave},null,8,["modelValue","onChange"])])])),_:1})])}var l=t(4);const c=e=>(0,l.zG)("action/SetServerArgs",e);var i=t(3830),u=t(1219),d=t(144),v={setup(e){const n=(0,i.B)(),t=(0,d.Kh)({list:n.value.config.Client.Action.Args[n.value.config.Client.ServerInfo.Host]||""}),o=()=>{try{if(t.list&&"object"!=typeof JSON.parse(t.list))return void u.nk.error("Json格式错误")}catch(o){return void u.nk.error("Json格式错误")}const e={};e[n.value.config.Client.ServerInfo.Host]=t.list,c(e).then((()=>{u.nk.success("已操作")})).catch((e=>{console.log(e),u.nk.error("操作失败")}))};return{state:t,handleSave:o}}},k=t(1241);const f=(0,k.A)(v,[["render",a],["__scopeId","data-v-f78c23dc"]]);var h=f}}]);