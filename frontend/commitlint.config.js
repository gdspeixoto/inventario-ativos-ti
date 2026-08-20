/**
 * Conventional Commits — https://www.conventionalcommits.org
 *
 * Format: <type>(<scope>): <subject>
 * Example: feat(auth): add Azure AD B2C provider
 */
module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'type-enum': [
      2,
      'always',
      [
        'feat',
        'fix',
        'docs',
        'style',
        'refactor',
        'perf',
        'test',
        'build',
        'ci',
        'chore',
        'revert',
      ],
    ],
    'scope-enum': [
      1,
      'always',
      [
        'auth',
        'core',
        'shared',
        'layout',
        'theme',
        'i18n',
        'pages',
        'config',
        'docs',
        'deps',
        'ci',
      ],
    ],
    'subject-case': [2, 'never', ['start-case', 'pascal-case', 'upper-case']],
    'header-max-length': [2, 'always', 100],
  },
};
