import { ArrowDownOutlined, ArrowUpOutlined } from '@ant-design/icons';
import { FloatButton, Tabs } from 'antd';
import { useEffect, useRef, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

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
  const { t } = useTranslation();
  const [active, setActive] = useState(tabs[0]?.key);
  const [atBottom, setAtBottom] = useState(false);
  const navRef = useRef<HTMLDivElement>(null);
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
      setAtBottom(bottom);
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

  const scrollToEdge = () => {
    const scroller = scrollParentOf(navRef.current);
    scroller.scrollTo({ top: atBottom ? 0 : scroller.scrollHeight, behavior: 'smooth' });
  };

  const edgeLabel = atBottom ? t('common.goTop') : t('common.goBottom');

  return (
    <>
      <div ref={navRef} className="erp-form-nav">
        <Tabs
          className="erp-card-tabs"
          activeKey={active}
          onChange={goTo}
          items={tabs.map((tb) => ({ key: tb.key, label: tb.label }))}
        />
      </div>
      <FloatButton
        className="erp-scroll-edge"
        tooltip={edgeLabel}
        aria-label={edgeLabel}
        icon={atBottom ? <ArrowUpOutlined /> : <ArrowDownOutlined />}
        onClick={scrollToEdge}
      />
      {tabs.map((tb) => (
        <div
          key={tb.key}
          ref={(element) => {
            sections.current[tb.key] = element;
          }}
          className="erp-form-section"
        >
          {tb.content}
        </div>
      ))}
    </>
  );
}
