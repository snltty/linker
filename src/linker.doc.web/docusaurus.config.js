// @ts-check
// `@type` JSDoc annotations allow editor autocompletion and type checking
// (when paired with `@ts-check`).
// There are various equivalent ways to declare your Docusaurus config.
// See: https://docusaurus.io/docs/api/docusaurus-config

import { themes as prismThemes } from 'prism-react-renderer';

/** @type {import('@docusaurus/types').Config} */
const config = {
    title: 'linker-doc',
    tagline: 'linker、海内皆隔壁，天涯若比邻。',
    favicon: 'img/favicon.ico',

    // Set the production url of your site here
    url: 'https://linker-doc.snltty.com',
    // Set the /<baseUrl>/ pathname under which your site is served
    // For GitHub pages deployment, it is often '/<projectName>/'
    baseUrl: '/',

    // GitHub pages deployment config.
    // If you aren't using GitHub pages, you don't need these.
    organizationName: 'snltty', // Usually your GitHub org/user name.
    projectName: 'linker.doc.web', // Usually your repo name.

    onBrokenLinks: 'throw',
    onBrokenMarkdownLinks: 'warn',

    scripts: [
        { src: 'https://hm.baidu.com/hm.js?e990192bd30d5e0eea50b34444f911f8', async: true }
    ],

    // Even if you don't use internationalization, you can use this field to set
    // useful metadata like html lang. For example, if your site is Chinese, you
    // may want to replace "en" with "zh-Hans".
    i18n: {
        defaultLocale: 'zh-cn',
        locales: ['zh-cn'],
    },

    presets: [
        [
            'classic',
            /** @type {import('@docusaurus/preset-classic').Options} */
            ({
                docs: {
                    sidebarPath: './sidebars.js',
                },
                blog: {
                    showReadingTime: true,
                    editUrl:'https://github.com/facebook/docusaurus/tree/main/packages/create-docusaurus/templates/shared/',
                },
                theme: {
                    customCss: './src/css/custom.css',
                },
            }),
        ],
    ],

    themeConfig:
        /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
        ({
            navbar: {
                title: 'linker-doc',
                logo: {
                    alt: 'linker logo',
                    src: 'img/logo.png',
                },
                items: [
                    { href: 'https://blog.snltty.com', label: '博客', position: 'left' },
                    { href: 'https://tun324.snltty.com', label: 'TUN转代理', position: 'left' }
                ],
            },
            footer: {
                style: 'light',
                links: [
                    { href: 'https://mi-d.cn', label: '米多贝克', position: 'left' },
                    { href: 'https://www.wpe64.com', label: 'WPE64代理和抓包', position: 'left' },
                ],
                copyright: `Copyright © ${new Date().getFullYear()} linker, Inc. Built with Docusaurus.`,
            },
            prism: {
                theme: prismThemes.github,
                darkTheme: prismThemes.dracula,
            },
            announcementBar: {
                id: 'support_us',
                content: '<span style="font-size:14px;color:#f7033a;">使用官方信标服务器时，在<a target="_blank" href="https://afdian.com/a/snltty">【🔋爱发电】</a>充电后的订单号可在[服务器][中继]部分导入，以解锁相应中继带宽，自建不需要</span>',
                backgroundColor: '#f5f5f5',
                textColor: '#ff0000',
                isCloseable: false,
            },
        }),
};

export default config;
