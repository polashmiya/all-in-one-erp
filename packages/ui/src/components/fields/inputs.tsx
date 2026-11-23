import { InputNumber, Switch, type InputNumberProps } from 'antd';
import { useTranslation } from 'react-i18next';
import { CURRENCY_SYMBOL } from '../../utils/format';

type NumberProps = Omit<InputNumberProps<number>, 'prefix' | 'suffix'>;

const grouped = (value: number | string | undefined) => {
  if (value === undefined || value === '') return '';
  const [int = '', frac] = String(value).split('.');
  const lastThree = int.slice(-3);
  const rest = int.slice(0, -3).replace(/\B(?=(\d{2})+(?!\d))/g, ',');
  const body = rest ? `${rest},${lastThree}` : lastThree;
  return frac !== undefined ? `${body}.${frac}` : body;
};

const ungroup = (value: string | undefined) => (value ?? '').replace(/,/g, '');

export function MoneyInput({
  currency = 'BDT',
  compact,
  ...props
}: NumberProps & { currency?: string; compact?: boolean }) {
  return (
    <InputNumber<number>
      min={0}
      precision={2}
      controls={false}
      formatter={grouped}
      parser={(v) => Number(ungroup(v))}
      prefix={<span className="erp-affix is-strong">{CURRENCY_SYMBOL}</span>}
      suffix={compact ? undefined : <span className="erp-affix">{currency}</span>}
      className="erp-number is-money"
      {...props}
    />
  );
}

export function UnitInput({ unit, ...props }: NumberProps & { unit: string }) {
  return (
    <InputNumber<number>
      controls={false}
      suffix={<span className="erp-affix">{unit}</span>}
      className="erp-number"
      {...props}
    />
  );
}

export function PercentInput(props: NumberProps) {
  return <UnitInput unit="%" min={0} max={100} precision={2} {...props} />;
}

export function NumberStepper({
  value,
  onChange,
  min = 0,
  max,
  step = 1,
  disabled,
  ...props
}: NumberProps) {
  const { t } = useTranslation();
  const current = typeof value === 'number' ? value : 0;
  const clamp = (n: number) =>
    Math.min(max ?? Number.POSITIVE_INFINITY, Math.max(min ?? Number.NEGATIVE_INFINITY, n));
  const stepBy = (delta: number) => onChange?.(clamp(current + delta * Number(step)));

  return (
    <div className={`erp-stepper${disabled ? ' is-disabled' : ''}`}>
      <button
        type="button"
        aria-label={t('field.decrease')}
        disabled={disabled || current <= Number(min)}
        onClick={() => stepBy(-1)}
      >
        −
      </button>
      <InputNumber<number>
        variant="borderless"
        controls={false}
        precision={0}
        formatter={grouped}
        parser={(v) => Number(ungroup(v))}
        value={value}
        onChange={onChange}
        min={min}
        max={max}
        disabled={disabled}
        {...props}
      />
      <button
        type="button"
        aria-label={t('field.increase')}
        disabled={disabled || (max !== undefined && current >= Number(max))}
        onClick={() => stepBy(1)}
      >
        +
      </button>
    </div>
  );
}

export function YesNoSwitch({
  checked,
  onChange,
  disabled,
  id,
}: {
  checked?: boolean;
  onChange?: (checked: boolean) => void;
  disabled?: boolean;
  id?: string;
}) {
  const { t } = useTranslation();

  return (
    <span className="erp-yesno">
      <Switch id={id} checked={!!checked} onChange={onChange} disabled={disabled} />
      <span>{checked ? t('field.yes') : t('field.no')}</span>
    </span>
  );
}
