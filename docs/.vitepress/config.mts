import { defineConfig } from 'vitepress'
import { withMermaid } from 'vitepress-plugin-mermaid'

const isPagesBuild = process.env.GITHUB_ACTIONS === 'true' || process.env.GITHUB_PAGES === 'true'
const repoName = process.env.GITHUB_REPOSITORY?.split('/')[1] || 'Dino'
const docsBase = isPagesBuild ? `/${repoName}/` : '/'

export default withMermaid(
  defineConfig({
    title: 'DINOForge',
    description: 'General-purpose mod platform for Diplomacy is Not an Option',
    lang: 'en-US',
    base: docsBase,
    lastUpdated: true,
    cleanUrls: true,
    appearance: 'dark',
    ignoreDeadLinks: true,

    head: [
      ['link', { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' }],
    ],

    themeConfig: {
      logo: '/favicon.svg',
      siteTitle: 'DINOForge',

      nav: [
        { text: 'Guide', link: '/guide/getting-started' },
        { text: 'Packs', link: '/packs' },
        { text: 'Concepts', link: '/concepts/architecture' },
        { text: 'Warfare', link: '/warfare/overview' },
        { text: 'Reference', link: '/reference/schemas' },
        { text: 'API', link: '/api/' },
        { text: 'Roadmap', link: '/roadmap/' },
        { text: 'Community', link: '/community' },
      ],

      sidebar: [
        {
          text: 'Introduction',
          items: [
            { text: 'What is DINOForge?', link: '/' },
            { text: 'Getting Started', link: '/guide/getting-started' },
            { text: 'Quick Start', link: '/guide/quick-start' },
            { text: 'Pack Registry', link: '/packs' },
          ],
        },
        {
          text: 'Concepts',
          items: [
            { text: 'Architecture', link: '/concepts/architecture' },
            { text: 'ECS Bridge', link: '/concepts/ecs-bridge' },
            { text: 'Registry System', link: '/concepts/registry-system' },
          ],
        },
        {
          text: 'Guide',
          items: [
            { text: 'Creating Packs', link: '/guide/creating-packs' },
            { text: 'Asset Intake (Pre-Impl)', link: '/asset-intake/assetctl-prd' },
          ],
        },
        {
          text: 'Warfare Domain',
          items: [
            { text: 'Overview', link: '/warfare/overview' },
            { text: 'Factions', link: '/warfare/factions' },
            { text: 'Unit Roles', link: '/warfare/unit-roles' },
          ],
        },
        {
          text: 'Reference',
          items: [
            { text: 'Schema Reference', link: '/reference/schemas' },
            { text: 'CLI Reference', link: '/reference/cli' },
            { text: 'Asset Intake Spec', link: '/reference/asset-intake/blender-normalization-worker' },
            { text: 'Unity Import Contract', link: '/reference/asset-intake/unity-import-contract' },
            { text: 'Faction Taxonomy', link: '/reference/asset-intake/faction-taxonomy' },
          ],
        },
        {
          text: 'Project',
          items: [
            { text: 'Roadmap', link: '/roadmap/' },
            { text: 'ADR Index', link: '/adr/' },
          ],
        },
      ],

      editLink: {
        pattern: 'https://github.com/KooshaPari/Dino/edit/main/docs/:path',
        text: 'Edit this page on GitHub',
      },

      search: {
        provider: 'local',
      },

      socialLinks: [
        { icon: 'github', link: 'https://github.com/KooshaPari/Dino' },
      ],

      footer: {
        message: 'Released under the MIT License.',
        copyright: '© 2025 Phenotype',
      },
    },

    mermaid: {},
  })
)
