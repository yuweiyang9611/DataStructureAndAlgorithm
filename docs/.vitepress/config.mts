import { defineConfig } from 'vitepress'

const repository = 'https://github.com/yuweiyang9611/DataStructureAndAlgorithm'
const siteDescription = '用 C# 14 系统学习数据结构、算法、测试方法与综合项目。'
const siteUrl = 'https://yuweiyang9611.github.io/DataStructureAndAlgorithm/'
const socialImage = 'https://yuweiyang9611.github.io/DataStructureAndAlgorithm/og.png'
const completeSourcePages = new Set([
  'CSharp数据结构与算法学习指导.md',
  'CSharp数据结构与算法进阶学习指导.md',
  '综合项目实战学习指导.md'
])

function canonicalUrl(relativePath: string) {
  const route = relativePath === 'index.md'
    ? ''
    : relativePath.replace(/\.md$/, '.html')
  const encodedRoute = route.split('/').map((segment) => encodeURIComponent(segment)).join('/')
  return new URL(encodedRoute, siteUrl).href
}

export default defineConfig({
  lang: 'zh-CN',
  title: '算法研习室',
  titleTemplate: ':title · DataStructureAndAlgorithm',
  description: siteDescription,
  base: '/DataStructureAndAlgorithm/',
  lastUpdated: true,
  head: [
    ['meta', { name: 'theme-color', content: '#07111f' }],
    ['meta', { name: 'color-scheme', content: 'dark light' }],
    ['meta', { property: 'og:locale', content: 'zh_CN' }],
    ['link', { rel: 'icon', type: 'image/svg+xml', href: '/DataStructureAndAlgorithm/favicon.svg' }]
  ],
  sitemap: {
    hostname: siteUrl,
    transformItems: (items) => items.filter((item) => {
      const url = decodeURI(item.url)
      return ![...completeSourcePages].some((page) => url.endsWith(page.replace(/\.md$/, '.html')))
    })
  },
  transformHead({ pageData }) {
    const isHome = pageData.relativePath === 'index.md'
    const pageTitle = isHome
      ? '算法研习室 · C# 数据结构与算法学习项目'
      : pageData.title || '算法研习室'
    const pageDescription = pageData.description || siteDescription
    const pageCanonical = canonicalUrl(pageData.relativePath)
    const socialHead: [string, Record<string, string>][] = [
      ['link', { rel: 'canonical', href: pageCanonical }],
      ['meta', { property: 'og:type', content: 'website' }],
      ['meta', { property: 'og:url', content: pageCanonical }],
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

    if (completeSourcePages.has(pageData.relativePath)) {
      socialHead.push(['meta', { name: 'robots', content: 'noindex, follow' }])
    }

    return socialHead
  },
  themeConfig: {
    siteTitle: '算法研习室',
    nav: [
      { text: '开始', link: '/开始学习' },
      { text: '学习单元', link: '/学习单元' },
      { text: 'Trace 实验室', link: '/追踪实验室' },
      {
        text: '题库',
        items: [
          { text: '经典 19 题', link: '/LeetCode经典题目学习指导' },
          { text: '完整 100 题', link: '/LeetCode100题完整学习指导' },
          { text: 'P3 · 14 道高级专题', link: '/P3数论位运算与高级算法学习指导' }
        ]
      },
      { text: '源码导航', link: '/源码导航' }
    ],
    sidebar: [
      {
        text: '开始学习',
        items: [
          { text: '学习门户', link: '/' },
          { text: '从这里开始', link: '/开始学习' },
          { text: '22 个学习单元', link: '/学习单元' },
          { text: '算法 Trace 实验室', link: '/追踪实验室' },
          { text: 'API 与比较器约定', link: '/API设计与比较器约定' }
        ]
      },
      {
        text: '主线课程',
        link: '/CSharp数据结构与算法学习指导',
        collapsed: false,
        items: [
          { text: '项目知识地图', link: '/主线/知识地图' },
          { text: '环境与学习方法', link: '/主线/环境与学习方法' },
          { text: 'C# 最佳实践', link: '/主线/CSharp最佳实践' },
          { text: '阶段一至三', link: '/主线/阶段一至三' },
          { text: '阶段四至六', link: '/主线/阶段四至六' },
          { text: '阶段七至八与测试证据', link: '/主线/阶段七至八与测试证据' },
          { text: '练习、验收与后续', link: '/主线/练习验收与后续' }
        ]
      },
      {
        text: '进阶课程',
        link: '/CSharp数据结构与算法进阶学习指导',
        collapsed: true,
        items: [
          { text: '十八周计划与验收', link: '/进阶/十八周学习计划与验收' },
          { text: '核心数据结构', link: '/进阶/核心数据结构' },
          { text: '算法专题', link: '/进阶/算法专题' },
          { text: 'C# 与测试', link: '/进阶/CSharp与测试' },
          { text: '正确性修复与结构补强', link: '/进阶/正确性修复与结构补强' },
          { text: 'CI、追踪与测试证据', link: '/进阶/持续集成追踪与测试证据' },
          { text: '高阶算法与内存', link: '/进阶/高阶算法与内存' }
        ]
      },
      {
        text: '题库实践',
        collapsed: true,
        items: [
          { text: '经典 19 题（入门）', link: '/LeetCode经典题目学习指导' },
          { text: '完整 100 题（进阶）', link: '/LeetCode100题完整学习指导' },
          { text: 'P3 · 14 道高级专题', link: '/P3数论位运算与高级算法学习指导' }
        ]
      },
      {
        text: '综合项目',
        link: '/综合项目实战学习指导',
        collapsed: true,
        items: [
          { text: '城市即时配送', link: '/综合项目/城市即时配送' },
          { text: '迷你搜索引擎', link: '/综合项目/迷你搜索引擎' },
          { text: '项目调度', link: '/综合项目/项目调度' },
          { text: '迷你存储引擎', link: '/综合项目/迷你存储引擎' },
          { text: '工程原则与证据链', link: '/综合项目/工程原则与证据链' },
          { text: '八周路线与验收', link: '/综合项目/八周路线与验收' }
        ]
      },
      {
        text: '参考',
        collapsed: true,
        items: [
          { text: '文档与源码导航', link: '/源码导航' },
          { text: '完整主线单页', link: '/CSharp数据结构与算法学习指导' },
          { text: '完整进阶单页', link: '/CSharp数据结构与算法进阶学习指导' },
          { text: '完整项目单页', link: '/综合项目实战学习指导' },
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
    lastUpdated: {
      text: '最后更新',
      formatOptions: {
        dateStyle: 'medium',
        timeStyle: 'short'
      }
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
