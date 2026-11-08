import { defineProject } from 'vitest/config';

export default defineProject({
  test: {
    name: 'metadata-engine',
    environment: 'jsdom',
    globals: true,
  },
});
