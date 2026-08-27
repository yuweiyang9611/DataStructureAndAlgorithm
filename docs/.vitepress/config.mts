import { defineConfig } from 'vitepress'

const repository = 'https://github.com/yuweiyang9611/DataStructureAndAlgorithm'
const siteDescription = '用 C# 14 系统学习数据结构、算法、测试方法与综合项目。'
const socialImage = 'https://yuweiyang9611.github.io/DataStructureAndAlgorithm/og.png'

export default defineConfig({
  lang: 'zh-CN',
  title: '算法研习室',
  titleTemplate: ':title · DataStructureAndAlgorithm',
  description: siteDescription,
  base: '/DataStructureAndAlgorithm/',
  head: [
    ['meta', { name: 'theme-color', content: '#07111f' }],
    ['meta', { name: 'color-scheme', content: 'dark light' }],
    ['meta', { property: 'og:locale', content: 'zh_CN' }]
  ],
  sitemap: {
    hostname: 'https://yuweiyang9611.github.io/DataStructureAndAlgorithm/'
  },
  transformHead({ pageData }) {
    const isHome = pageData.relativePath === 'index.md'
    const pageTitle = isHome
      ? '算法研习室 · C# 数据结构与算法学习项目'
      : pageData.title || '算法研习室'
    const pageDescription = pageData.description || siteDescription
    const socialHead: [string, Record<string, string>][] = [
      ['meta', { property: 'og:type', content: 'website' }],
      ['meta', { property: 'og:title', content: pageTitle }],
      ['meta', { property: 'og:description', content: pageDescription }],
      ['meta', { name: 'twitter:card', content: isHome ? 'summary_large_image' : 'summary' }],
      ['meta', { name: 'twitter:title', content: pageTitle }],
      ['meta', { name: 'twitter:description', content: pageDescription }]
    ]

    if (isHome) {
      socialHead.push(
        ['meta', { property: 'og:image', content: socialImage }],
        ['meta', { property: 'og:image:width', content: '1200' }],
        ['meta', { property: 'og:image:height', content: '630' }],
        ['meta', { property: 'og:image:alt', content: '算法研习室：用 C# 14 构建可解释、可验证的算法能力' }],
        ['meta', { name: 'twitter:image', content: socialImage }],
        ['meta', { name: 'twitter:image:alt', content: '算法研习室：用 C# 14 构建可解释、可验证的算法能力' }]
      )
    }

    return socialHead
  },
  themeConfig: {
    siteTitle: '算法研习室',
    nav: [
      { text: '学习路线', link: '/CSharp数据结构与算法学习指导' },
      { text: '进阶专题', link: '/CSharp数据结构与算法进阶学习指导' },
      { text: 'LeetCode 100', link: '/LeetCode100题完整学习指导' },
      { text: '综合项目', link: '/综合项目实战学习指导' },
      { text: '源码导航', link: '/源码导航' }
    ],
    sidebar: [
      {
        text: '开始学习',
        items: [
          { text: '学习门户', link: '/' },
          { text: '主线学习指导', link: '/CSharp数据结构与算法学习指导' },
          { text: 'API 与比较器约定', link: '/API设计与比较器约定' }
        ]
      },
      {
        text: '专题路线',
        items: [
          { text: '进阶数据结构与算法', link: '/CSharp数据结构与算法进阶学习指导' },
          { text: 'LeetCode 经典题', link: '/LeetCode经典题目学习指导' },
          { text: 'LeetCode 100 题', link: '/LeetCode100题完整学习指导' },
          { text: 'P3 高级算法', link: '/P3数论位运算与高级算法学习指导' }
        ]
      },
      {
        text: '从算法到系统',
        items: [
          { text: '综合项目实战', link: '/综合项目实战学习指导' },
          { text: '文档与源码导航', link: '/源码导航' },
          { text: '回到 GitHub 仓库', link: repository }
        ]
      }
    ],
    search: {
      provider: 'local',
      options: {
        locales: {
          root: {
            translations: {
              button: {
                buttonText: '搜索文档',
                buttonAriaLabel: '搜索文档'
              },
              modal: {
                noResultsText: '没有找到相关内容',
                resetButtonTitle: '清除查询',
                footer: {
                  selectText: '选择',
                  navigateText: '切换',
                  closeText: '关闭'
                }
              }
            }
          }
        }
      }
    },
    socialLinks: [
      { icon: 'github', link: repository }
    ],
    editLink: {
      pattern: `${repository}/edit/main/docs/:path`,
      text: '在 GitHub 上改进本页'
    },
    outline: {
      level: [2, 3],
      label: '本页目录'
    },
    docFooter: {
      prev: '上一篇',
      next: '下一篇'
    },
    returnToTopLabel: '返回顶部',
    sidebarMenuLabel: '学习目录',
    darkModeSwitchLabel: '切换主题',
    lightModeSwitchTitle: '切换到浅色模式',
    darkModeSwitchTitle: '切换到深色模式',
    footer: {
      message: '教学实现用于理解不变量、复杂度与工程取舍。',
      copyright: 'DataStructureAndAlgorithm · C# 14 / .NET 10'
    }
  }
})
