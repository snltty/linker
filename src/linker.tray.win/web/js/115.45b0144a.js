"use strict";(self["webpackChunklinker_web"]=self["webpackChunklinker_web"]||[]).push([[115],{8115:function(e,l,o){o.r(l),o.d(l,{default:function(){return h}});var t=o(6768),a=o(4232);const n={class:"group-wrap"};function r(e,l,o,r,d,i){const s=(0,t.g2)("el-input"),u=(0,t.g2)("el-table-column"),c=(0,t.g2)("Delete"),p=(0,t.g2)("el-icon"),m=(0,t.g2)("el-button"),g=(0,t.g2)("el-popconfirm"),w=(0,t.g2)("Plus"),h=(0,t.g2)("Select"),k=(0,t.g2)("el-table");return(0,t.uX)(),(0,t.CE)("div",n,[(0,t.bF)(k,{stripe:"",data:r.state.list,border:"",size:"small",width:"100%",height:`${r.state.height}px`,onCellDblclick:r.handleCellClick},{default:(0,t.k6)((()=>[(0,t.bF)(u,{prop:"Name",label:e.$t("server.groupName"),width:"100"},{default:(0,t.k6)((e=>[e.row.NameEditing?((0,t.uX)(),(0,t.Wv)(s,{key:0,autofocus:"",size:"small",modelValue:e.row.Name,"onUpdate:modelValue":l=>e.row.Name=l,onBlur:l=>r.handleEditBlur(e.row,"Name")},null,8,["modelValue","onUpdate:modelValue","onBlur"])):((0,t.uX)(),(0,t.CE)(t.FK,{key:1},[(0,t.eW)((0,a.v_)(e.row.Name),1)],64))])),_:1},8,["label"]),(0,t.bF)(u,{prop:"Id",label:"Id"},{default:(0,t.k6)((e=>[e.row.IdEditing?((0,t.uX)(),(0,t.Wv)(s,{key:0,autofocus:"",size:"small",modelValue:e.row.Id,"onUpdate:modelValue":l=>e.row.Id=l,onBlur:l=>r.handleEditBlur(e.row,"Id")},null,8,["modelValue","onUpdate:modelValue","onBlur"])):((0,t.uX)(),(0,t.CE)(t.FK,{key:1},[(0,t.eW)((0,a.v_)(e.row.Id),1)],64))])),_:1}),(0,t.bF)(u,{prop:"Password",label:e.$t("server.groupPassword")},{default:(0,t.k6)((e=>[e.row.PasswordEditing?((0,t.uX)(),(0,t.Wv)(s,{key:0,type:"password","show-password":"",size:"small",modelValue:e.row.Password,"onUpdate:modelValue":l=>e.row.Password=l,onBlur:l=>r.handleEditBlur(e.row,"Password")},null,8,["modelValue","onUpdate:modelValue","onBlur"])):((0,t.uX)(),(0,t.CE)(t.FK,{key:1},[(0,t.eW)((0,a.v_)(e.row.Password.replace(/.{1}/g,"*")),1)],64))])),_:1},8,["label"]),(0,t.bF)(u,{prop:"Oper",label:e.$t("server.groupOper"),width:"160"},{default:(0,t.k6)((l=>[(0,t.Lk)("div",null,[(0,t.bF)(g,{title:e.$t("server.groupDelConfirm"),onConfirm:e=>r.handleDel(l.$index)},{reference:(0,t.k6)((()=>[(0,t.bF)(m,{type:"danger",size:"small"},{default:(0,t.k6)((()=>[(0,t.bF)(p,null,{default:(0,t.k6)((()=>[(0,t.bF)(c)])),_:1})])),_:1})])),_:2},1032,["title","onConfirm"]),(0,t.bF)(m,{size:"small",onClick:e=>r.handleAdd(l.$index)},{default:(0,t.k6)((()=>[(0,t.bF)(p,null,{default:(0,t.k6)((()=>[(0,t.bF)(w)])),_:1})])),_:2},1032,["onClick"]),(0,t.bF)(m,{type:"primary",size:"small",onClick:e=>r.handleUse(l.$index)},{default:(0,t.k6)((()=>[(0,t.bF)(p,null,{default:(0,t.k6)((()=>[(0,t.bF)(h)])),_:1})])),_:2},1032,["onClick"])])])),_:1},8,["label"])])),_:1},8,["data","height","onCellDblclick"])])}var d=o(9299),i=o(3830),s=o(1219),u=o(144),c=o(7477),p=o(5931),m={components:{Delete:c.epd,Plus:c.FWt,Select:c.l6P},setup(e){const{t:l}=(0,p.s9)(),o=(0,i.B)(),a=(0,u.Kh)({list:o.value.config.Client.Groups||[],height:(0,t.EW)((()=>o.value.height-70))});(0,t.wB)((()=>o.value.config.Client.Groups),(()=>{0==a.list.filter((e=>e["__editing"])).length&&(a.list=o.value.config.Client.Groups)}));const n=(e,l)=>{r(e,l.property)},r=(e,l)=>{a.list.forEach((e=>{e["NameEditing"]=!1,e["IdEditing"]=!1,e["PasswordEditing"]=!1})),e[`${l}Editing`]=!0,e["__editing"]=!0},c=(e,l)=>{e[`${l}Editing`]=!1,e["__editing"]=!1,h()},m=e=>{a.list.splice(e,1),h()},g=e=>{a.list.filter((e=>""==e.Id||""==e.Name)).length>0||(a.list.splice(e+1,0,{Name:"",Id:"",Password:""}),h())},w=e=>{const t=a.list.slice(),n=t[e];t[e]=t[0],t[0]=n,(0,d.rd)({name:o.value.config.Client.Name,groups:t}).then((()=>{s.nk.success(l("common.oper")),setTimeout((()=>{window.location.reload()}),1e3)})).catch((e=>{console.log(e),s.nk.error(l("common.operFail"))}))},h=()=>{(0,d.zp)(a.list).then((()=>{s.nk.success(l("common.oper"))})).catch((e=>{console.log(e),s.nk.error(l("common.operFail"))}))};return{state:a,handleCellClick:n,handleEditBlur:c,handleDel:m,handleAdd:g,handleUse:w}}},g=o(1241);const w=(0,g.A)(m,[["render",r],["__scopeId","data-v-06dc106b"]]);var h=w}}]);