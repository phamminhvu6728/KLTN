
const { defineConfig } = require('@playwright/test');

module.exports = defineConfig({
  testDir: './tests',

  timeout: 300000,

  expect: {
    timeout: 10000
  },

  retries: 0,
  workers: 1,

  use: {
    baseURL: 'http://localhost:5080',
    browserName: 'chromium',

    headless: false,

    launchOptions: {
      slowMo: 1000
    },

    video: 'off',

    screenshot: 'only-on-failure',

    trace: 'retain-on-failure'
  },

  reporter: [
    ['list'],
    ['html', { open: 'never' }]
  ]
});
