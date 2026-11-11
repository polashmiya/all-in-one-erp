import { useEffect, useRef, useState, type ReactNode } from 'react';

export interface SectionTab {
  key: string;
  label: ReactNode;
  content: ReactNode;
}

const SECTION_OFFSET = 24;

const USER_SCROLL_EVENTS = ['wheel', 'touchstart', 'keydown', 'mousedown'] as const;

function scrollParentOf(element: HTMLElement | null): HTMLElement {
  for (let node = element?.parentElement; node; node = node.parentElement) {
    if (/(auto|scroll)/.test(getComputedStyle(node).overflowY)) return node;
  }
  return document.documentElement;
}

export function SectionTabs({ tabs }: { tabs: SectionTab[] }) {
  const [active, setActive] = useState(tabs[0]?.key);
  const navRef = useRef<HTMLElement>(null);
  const sections = useRef<Record<string, HTMLElement | null>>({});
  const clickedTab = useRef<string | null>(null);
  const tabKeys = tabs.map((tb) => tb.key).join(',');

  useEffect(() => {
    const keys = tabKeys.split(',').filter(Boolean);
    if (!keys.length) return;
    const scroller = scrollParentOf(navRef.current);

    const update = () => {
      const scrollable = scroller.scrollHeight > scroller.clientHeight + 1;
      const bottom =
        scrollable && scroller.scrollTop + scroller.clientHeight >= scroller.scrollHeight - 2;
      const line = (navRef.current?.getBoundingClientRect().bottom ?? 0) + SECTION_OFFSET;
      const passed = keys.filter(
        (key) => (sections.current[key]?.getBoundingClientRect().top ?? Infinity) <= line,
      );
      setActive(clickedTab.current ?? (bottom ? keys.at(-1) : (passed.at(-1) ?? keys[0])));
    };
    const release = () => {
      clickedTab.current = null;
    };

    update();
    scroller.addEventListener('scroll', update, { passive: true });
    for (const type of USER_SCROLL_EVENTS)
      scroller.addEventListener(type, release, { passive: true });
    return () => {
      scroller.removeEventListener('scroll', update);
      for (const type of USER_SCROLL_EVENTS) scroller.removeEventListener(type, release);
    };
  }, [tabKeys]);

  const goTo = (key: string) => {
    clickedTab.current = key;
    setActive(key);
    sections.current[key]?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  };

  return (
    <>
      <nav ref={navRef} className="erp-section-tabs">
        {tabs.map((tb) => (
          <button
            key={tb.key}
            type="button"
            className={`erp-section-tabs__item${tb.key === active ? ' is-active' : ''}`}
            aria-current={tb.key === active ? 'true' : undefined}
            onClick={() => goTo(tb.key)}
          >
            {tb.label}
          </button>
        ))}
      </nav>
      {tabs.map((tb) => (
        <div
          key={tb.key}
          ref={(element) => {
            sections.current[tb.key] = element;
          }}
          className="erp-section-tabs__panel"
        >
          {tb.content}
        </div>
      ))}
    </>
  );
}
