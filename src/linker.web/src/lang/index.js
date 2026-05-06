import { createI18n } from 'vue-i18n'
const langs = [
    {
        label: '简体中文',
        value: 'zh-CN',
        ctx: require.context('../', true, /zh-cn\.json$/)
    },
    {
        label: '繁体中文',
        value: 'zh-TW',
        ctx: require.context('../', true, /zh-tw\.json$/)
    },
    {
        label: 'English',
        value: 'en-US',
        ctx: require.context('../', true, /en-us\.json$/)
    }
]

const messages = langs.reduce((json, cur) => {
    if(!json[cur.value]){
        json[cur.value] = {};
    }
    cur.ctx.keys().forEach(_key => {
        const data = cur.ctx(_key);
        Object.assign(json[cur.value], data);
    });
    return json;
},{});


export const LOCALE_OPTIONS = langs.reduce((json, cur) => { json[cur.value]=cur.label; return json  },{});
const i18n = createI18n({
    locale: localStorage.getItem('locale-lang') || navigator.language || navigator.browserLanguage,
    fallbackLocale: langs[0].value,
    legacy: false,
    allowComposition: true,
    messages: messages
})
export default i18n