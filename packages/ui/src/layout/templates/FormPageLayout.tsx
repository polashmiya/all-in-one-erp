import { App, Form, type FormInstance } from 'antd';
import { useRef, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import type { ActionItem, ActionVariant } from '../../components/actions/types';
import { useHotkey } from '../../hooks/useHotkey';
import { useGoBack } from '../../hooks/useGoBack';
import { useUnsavedGuard } from '../../hooks/useUnsavedGuard';
import { PageContainer, type PageStatus } from '../page/PageContainer';
import { SplitLayout } from '../page/CardGrid';

export interface FormSection {
  key: string;
  fields?: string[];
  content: ReactNode;
}

export interface FormSubmitAction {
  key: string;
  label: string;
  icon?: ReactNode;
  variant?: ActionVariant;
  validate?: 'all' | string[];
  hidden?: boolean;
}

export interface FormPageLayoutProps<T> {
  form: FormInstance<T>;
  initialValues?: Partial<T>;
  title: ReactNode;
  subTitle?: ReactNode;
  status?: PageStatus;
  backTo?: string;
  crumbs?: string[];
  sections?: FormSection[];
  aside?: FormSection[];
  children?: ReactNode;
  submitActions: FormSubmitAction[];
  cancelTo?: string;
  saving?: boolean;
  guard?: boolean;
  onSubmit: (values: T, actionKey: string) => Promise<unknown>;
}

function Cards({ sections }: { sections: FormSection[] }) {
  return (
    <div className="erp-form-stack">
      {sections.map((section) => (
        <section key={section.key} className="erp-card erp-form-block">
          {section.content}
        </section>
      ))}
    </div>
  );
}

export function FormPageLayout<T extends object>({
  form,
  initialValues,
  title,
  subTitle,
  status,
  backTo,
  crumbs,
  sections,
  aside,
  children,
  submitActions,
  cancelTo,
  saving,
  guard = true,
  onSubmit,
}: FormPageLayoutProps<T>) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const [dirty, setDirty] = useState(false);
  const [errorCount, setErrorCount] = useState(0);
  const dirtyRef = useRef(false);

  const goBack = useGoBack(cancelTo ?? '/');

  useUnsavedGuard(guard ? () => dirtyRef.current : false, guard && dirty);

  const markDirty = (value: boolean) => {
    dirtyRef.current = value;
    setDirty(value);
  };

  const countErrors = () =>
    setErrorCount(form.getFieldsError().filter((field) => field.errors.length).length);

  const revealErrors = () => {
    countErrors();
    const first = form.getFieldsError().find((field) => field.errors.length);
    if (first) form.scrollToField(first.name, { block: 'center', behavior: 'smooth', focus: true });
  };

  const submit = async (action: FormSubmitAction) => {
    try {
      if (action.validate === undefined || action.validate === 'all') await form.validateFields();
      else await form.validateFields(action.validate);
    } catch {
      revealErrors();
      void message.error(t('common.formInvalid'));
      return;
    }
    const values = form.getFieldsValue(true) as T;
    dirtyRef.current = false;
    try {
      await onSubmit(values, action.key);
      setErrorCount(0);
      setDirty(false);
    } catch {
      dirtyRef.current = dirty;
      revealErrors();
    }
  };

  const saveAction =
    submitActions.find((a) => a.variant === 'primary' && !a.hidden) ??
    submitActions.find((a) => !a.hidden);

  useHotkey('s', () => saveAction && void submit(saveAction), { mod: true, enabled: !saving });

  const actions: ActionItem[] = [
    ...(cancelTo
      ? [{ key: 'cancel', label: t('common.cancel'), variant: 'text' as const, onClick: goBack }]
      : []),
    ...submitActions.map((action) => ({
      key: action.key,
      label: action.label,
      icon: action.icon,
      variant: action.variant ?? 'default',
      loading: saving,
      hidden: action.hidden,
      onClick: () => submit(action),
    })),
  ];

  const statusText = [
    dirty ? t('common.unsaved') : null,
    errorCount ? t('common.needsAttention', { count: errorCount }) : null,
  ]
    .filter(Boolean)
    .join(' · ');

  return (
    <PageContainer
      sticky
      title={title}
      subTitle={subTitle}
      status={status}
      backTo={backTo}
      crumbs={crumbs}
      extra={
        statusText && (
          <span className="erp-status-dot erp-status-dot--warning erp-text-sm">{statusText}</span>
        )
      }
      actions={actions}
    >
      <Form<T>
        form={form}
        layout="vertical"
        className="erp-form"
        initialValues={initialValues}
        onValuesChange={() => markDirty(true)}
        onFieldsChange={() => errorCount > 0 && countErrors()}
      >
        <SplitLayout
          aside={aside?.length ? <Cards sections={aside} /> : undefined}
          main={
            sections ? (
              <Cards sections={sections} />
            ) : (
              <section className="erp-card erp-form-block">{children}</section>
            )
          }
        />
      </Form>
    </PageContainer>
  );
}
