import {
  ApartmentOutlined,
  AppstoreOutlined,
  BankOutlined,
  InboxOutlined,
  SafetyCertificateOutlined,
  ShopOutlined,
} from '@ant-design/icons';
import { ApiError, useSetupAccount, type AccountSetupResult } from '@erp/api-client';
import { PageContainer, useUnsavedGuard, WizardLayout } from '@erp/ui';
import { App, Form } from 'antd';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { ACCOUNT_PATHS } from '../../constants';
import { SetupSuccess } from '../components/SetupSuccess';
import { SetupSummary } from '../components/SetupSummary';
import {
  initialValues,
  STEP_KEYS,
  stepOfField,
  suggestCode,
  toFieldPath,
  toSetupRequest,
  type FieldPath,
  type SetupFormValues,
} from '../model/setup-model';
import { AccountStep } from '../components/steps/AccountStep';
import { BusinessUnitsStep } from '../components/steps/BusinessUnitsStep';
import { MenuAccessStep } from '../components/steps/MenuAccessStep';
import { OfficesStep } from '../components/steps/OfficesStep';
import { ReviewStep } from '../components/steps/ReviewStep';
import { WarehousesStep } from '../components/steps/WarehousesStep';

const LAST_STEP = STEP_KEYS.length - 1;

interface ServerFieldError {
  name: FieldPath;
  errors: string[];
}

export function AccountSetupPage() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const [form] = Form.useForm<SetupFormValues>();
  const values = Form.useWatch([], { form, preserve: true }) as SetupFormValues | undefined;
  const setup = useSetupAccount();

  const [step, setStep] = useState(0);
  const [reached, setReached] = useState(0);
  const [activeUnit, setActiveUnit] = useState(0);
  const [dirty, setDirty] = useState(false);
  const [result, setResult] = useState<AccountSetupResult>();
  const codeEdited = useRef(false);
  const pendingErrors = useRef<ServerFieldError[]>([]);

  const showErrors = useCallback(
    (fields: ServerFieldError[]) => form.setFields(fields as Parameters<typeof form.setFields>[0]),
    [form],
  );

  useUnsavedGuard(dirty && !result);

  useEffect(() => {
    const forStep = pendingErrors.current.filter((e) => stepOfField(e.name) === step);
    if (forStep.length) {
      showErrors(forStep);
      pendingErrors.current = pendingErrors.current.filter((e) => !forStep.includes(e));
    }

    if (step === LAST_STEP) {
      const all = form.getFieldsValue(true) as SetupFormValues;
      if (!all.admin?.email && all.account?.email)
        form.setFieldValue(['admin', 'email'], all.account.email.trim().toLowerCase());
      if (!all.admin?.fullName && all.account?.name) {
        form.setFieldValue(
          ['admin', 'fullName'],
          t('setup.defaultAdminName', { account: all.account.name.trim() }),
        );
      }
    }
  }, [step, form, t, showErrors]);

  const validateCurrentStep = async () => {
    try {
      await form.validateFields();
      return true;
    } catch (error) {
      const first = (error as { errorFields?: { name: FieldPath }[] }).errorFields?.[0]?.name;
      if (
        first?.[0] === 'businessUnits' &&
        typeof first[1] === 'number' &&
        (step === 2 || step === 3)
      )
        setActiveUnit(first[1]);
      void message.error(t('setup.fixErrors'));
      return false;
    }
  };

  const goTo = async (target: number) => {
    if (target === step) return;
    if (target > step && !(await validateCurrentStep())) return;
    setStep(target);
    setReached((r) => Math.max(r, target));
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const submit = async () => {
    if (!(await validateCurrentStep())) return;

    try {
      const created = await setup.mutateAsync({
        data: toSetupRequest(form.getFieldsValue(true) as SetupFormValues),
      });
      setDirty(false);
      setResult(created);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    } catch (error) {
      if (
        error instanceof ApiError &&
        error.status === 400 &&
        Object.keys(error.fieldErrors).length
      ) {
        const fields = Object.entries(error.fieldErrors).map(([key, errors]) => ({
          name: toFieldPath(key),
          errors,
        }));
        const firstStep = Math.min(...fields.map((f) => stepOfField(f.name)));
        const first = fields.find((f) => stepOfField(f.name) === firstStep)!;
        if (first.name[0] === 'businessUnits' && typeof first.name[1] === 'number')
          setActiveUnit(first.name[1]);

        if (firstStep === step) showErrors(fields.filter((f) => stepOfField(f.name) === step));
        pendingErrors.current = fields.filter((f) => stepOfField(f.name) !== step);
        setStep(firstStep);
        void message.error(t('setup.fixErrors'));
      } else if (error instanceof ApiError && error.status === 409) {
        void message.error(error.problem?.detail ?? t('setup.saveFailed'));
      } else {
        void message.error(t('setup.saveFailed'));
      }
    }
  };

  const onValuesChange = (changed: SetupFormValues) => {
    setDirty(true);
    const account = changed.account;
    if (!account) return;
    if ('code' in account) codeEdited.current = !!account.code;
    if ('name' in account && !codeEdited.current)
      form.setFieldValue(['account', 'code'], suggestCode(account.name));
  };

  const startAnother = () => {
    form.resetFields();
    codeEdited.current = false;
    pendingErrors.current = [];
    setResult(undefined);
    setDirty(false);
    setStep(0);
    setReached(0);
    setActiveUnit(0);
  };

  const crumbs = [t('setup.title')];

  if (result) {
    return (
      <PageContainer title={t('setup.title')} crumbs={crumbs}>
        <SetupSuccess
          result={result}
          onDone={() => navigate(ACCOUNT_PATHS.list)}
          onAnother={startAnother}
        />
      </PageContainer>
    );
  }

  const stepItems = [
    {
      icon: <BankOutlined />,
      title: t('setup.step.account'),
      description: t('setup.step.accountDesc'),
    },
    {
      icon: <ApartmentOutlined />,
      title: t('setup.step.units'),
      description: t('setup.step.unitsDesc'),
    },
    {
      icon: <ShopOutlined />,
      title: t('setup.step.offices'),
      description: t('setup.step.optional'),
    },
    {
      icon: <InboxOutlined />,
      title: t('setup.step.warehouses'),
      description: t('setup.step.optional'),
    },
    {
      icon: <AppstoreOutlined />,
      title: t('setup.step.menus'),
      description: t('setup.step.menusDesc'),
    },
    {
      icon: <SafetyCertificateOutlined />,
      title: t('setup.step.review'),
      description: t('setup.step.reviewDesc'),
    },
  ].map((item, index) => ({ ...item, key: STEP_KEYS[index] ?? String(index) }));

  return (
    <WizardLayout
      title={t('setup.title')}
      subTitle={t('setup.subtitle')}
      crumbs={crumbs}
      cancelTo={ACCOUNT_PATHS.list}
      steps={stepItems}
      current={step}
      reached={reached}
      onStepChange={(target) => void goTo(target)}
      finishLabel={t('setup.create')}
      finishing={setup.isPending}
      onFinish={() => void submit()}
      aside={<SetupSummary values={values} />}
    >
      <Form
        form={form}
        layout="vertical"
        className="erp-form"
        initialValues={initialValues}
        onValuesChange={onValuesChange}
        onFinish={() => void (step === LAST_STEP ? submit() : goTo(step + 1))}
        scrollToFirstError={{ block: 'center', behavior: 'smooth' }}
      >
        {step === 0 && <AccountStep />}
        {step === 1 && <BusinessUnitsStep />}
        {step === 2 && <OfficesStep activeUnit={activeUnit} onUnitChange={setActiveUnit} />}
        {step === 3 && <WarehousesStep activeUnit={activeUnit} onUnitChange={setActiveUnit} />}
        {step === 4 && <MenuAccessStep />}
        {step === 5 && <ReviewStep onEdit={(target) => void goTo(target)} />}
      </Form>
    </WizardLayout>
  );
}
