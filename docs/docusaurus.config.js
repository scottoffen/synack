import { themes as prismThemes } from 'prism-react-renderer';

export default {
  title: 'Synack',
  tagline: 'Tagline TBD',
  url: 'https://scottoffen.github.io',
  baseUrl: '/synack/',
  onBrokenLinks: 'warn',
  onBrokenMarkdownLinks: 'warn',
  favicon: 'img/favicon.ico',
  trailingSlash: false,

  organizationName: 'scottoffen', // GitHub username
  projectName: 'synack',          // Repo name

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  // This enables compatibility with Docusaurus v4 (future-proof)
  future: {
    v4: true,
  },

  presets: [
    [
      'classic',
      {
        docs: {
          routeBasePath: '/', // Serve docs at the root (no /docs prefix)
          sidebarPath: './sidebars.js',
          editUrl: 'https://github.com/scottoffen/synack/edit/main/docs/',
        },
        blog: false, // Disable blog
        theme: {
          customCss: './src/css/custom.css',
        },
      },
    ],
  ],

  themeConfig: {
    navbar: {
      title: 'Synack',
      logo: {
        alt: 'Synack Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/scottoffen/synack',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentation',
          items: [
            {
              label: 'Getting Started',
              to: '/',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'Discussions',
              href: 'https://github.com/scottoffen/synack/discussions',
            },
            {
              label: 'Stack Overflow',
              href: 'https://stackoverflow.com/questions/tagged/synack',
            },
          ],
        },
        {
          title: 'Project',
          items: [
            {
              label: 'Contributing Guide',
              href: 'https://github.com/scottoffen/synack/blob/main/CONTRIBUTING.md',
            },
            {
              label: 'Code of Conduct',
              href: 'https://github.com/scottoffen/synack/blob/main/CODE_OF_CONDUCT.md',
            },
            {
              label: 'GitHub',
              href: 'https://github.com/scottoffen/synack',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Synack`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
    titleDelimiter: '|',
    titleTemplate: 'PipeForge | %s'
  },
};
