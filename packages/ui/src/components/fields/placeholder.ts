import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';

const inSentence = (label: string) =>
  label.replace(/^\p{Lu}(?=\p{Ll})/u, (first) => first.toLowerCase());

export function usePlaceholder() {
  const { t } = useTranslation();
  return useCallback(
    (kind: 'enter' | 'select', label: string) => t(`field.${kind}`, { label: inSentence(label) }),
    [t],
  );
}
