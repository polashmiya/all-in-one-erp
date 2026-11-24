import type { FontKey } from './appearance';

const loaders: Partial<Record<FontKey, () => Promise<unknown>>> = {
  inter: () => import('@fontsource-variable/inter/index.css'),
  roboto: () => import('@fontsource-variable/roboto/index.css'),
  openSans: () => import('@fontsource-variable/open-sans/index.css'),
  manrope: () => import('@fontsource-variable/manrope/index.css'),
  poppins: () =>
    Promise.all([
      import('@fontsource/poppins/400.css'),
      import('@fontsource/poppins/500.css'),
      import('@fontsource/poppins/600.css'),
      import('@fontsource/poppins/700.css'),
    ]),
};

export function loadFont(font: FontKey) {
  void loaders[font]?.();
}
