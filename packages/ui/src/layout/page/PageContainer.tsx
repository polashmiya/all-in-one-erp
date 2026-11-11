import type { ReactNode } from 'react';
import { ActionGroup } from '../../components/actions/ActionGroup';
import { BackButton } from '../../components/actions/BackButton';
import type { ActionItem } from '../../components/actions/types';
import { StatusTag, type StatusTone } from '../../components/data-display/StatusTag';
import { useLanguageStore } from '../../i18n/i18n';
import { localize } from '../../i18n/language';
import { useShell, useShellCrumbs } from '../app-shell/ShellContext';

export interface PageStatus {
  label: ReactNode;
  tone: StatusTone;
}

export interface PageHeaderProps {
  title?: ReactNode;
  subTitle?: ReactNode;
  status?: PageStatus;
  tags?: ReactNode;
  actions?: ActionItem[];
  extra?: ReactNode;
  backTo?: string;
  sticky?: boolean;
}

export interface PageContainerProps extends Omit<PageHeaderProps, 'title'> {
  title?: ReactNode | false;
  crumbs?: string[];
  fill?: boolean;
  children?: ReactNode;
}

export function PageHeader({
  title,
  subTitle,
  status,
  tags,
  actions,
  extra,
  backTo,
  sticky = false,
}: PageHeaderProps) {
  const hasActions = !!extra || !!actions?.some((a) => !a.hidden);

  return (
    <div className={`erp-page__head${sticky ? ' is-sticky' : ''}`}>
      <div className="erp-page__titles">
        <div className="erp-page__titlerow">
          {backTo && <BackButton to={backTo} />}
          <h1 className="erp-page__title">{title}</h1>
          {status && (
            <StatusTag tone={status.tone} dot>
              {status.label}
            </StatusTag>
          )}
          {tags}
        </div>
        {subTitle && <p className="erp-page__sub">{subTitle}</p>}
      </div>
      {hasActions && (
        <div className="erp-page__actions">
          {extra}
          {actions && <ActionGroup actions={actions} />}
        </div>
      )}
    </div>
  );
}

export function PageContainer({
  title,
  crumbs = [],
  fill = false,
  children,
  ...header
}: PageContainerProps) {
  const { trail } = useShell();
  const language = useLanguageStore((s) => s.language);
  const leaf = trail.at(-1);
  useShellCrumbs(crumbs);

  const heading =
    title === false ? null : (title ?? (leaf ? localize(leaf.label, language) : null));
  const hasHeader = !!heading || !!header.extra || !!header.actions?.length;

  return (
    <div className={`erp-page${fill ? ' is-fill' : ''}`}>
      {hasHeader && <PageHeader title={heading} {...header} />}
      {children}
    </div>
  );
}

export function ActionBar({ status, actions }: { status?: ReactNode; actions: ActionItem[] }) {
  return (
    <div className="erp-actionbar">
      <span className="erp-actionbar__status">{status}</span>
      <ActionGroup actions={actions} className="erp-actionbar__actions" />
    </div>
  );
}
