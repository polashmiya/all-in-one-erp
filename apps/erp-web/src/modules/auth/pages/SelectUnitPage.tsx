import { ArrowLeftOutlined, ArrowRightOutlined, ShopOutlined } from '@ant-design/icons';
import { useSelectBusinessUnit } from '@erp/api-client';
import { AuthHeading, ChoiceCards, StatusTag } from '@erp/ui';
import { App, Button } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router';
import { useShallow } from 'zustand/react/shallow';
import { redirectTarget } from '@/common/guards/redirect';
import { endSession, queryClient } from '@/common/services/api-client';
import { useAuthStore } from '@/common/store/auth-store';
import { AuthLayout } from '../components/AuthLayout';

export function SelectUnitPage() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const location = useLocation();
  const { user, businessUnits, lastBusinessUnitId, selectUnit } = useAuthStore(
    useShallow((s) => ({
      user: s.user,
      businessUnits: s.businessUnits,
      lastBusinessUnitId: s.lastBusinessUnitId,
      selectUnit: s.selectUnit,
    })),
  );
  const select = useSelectBusinessUnit();
  const [chosen, setChosen] = useState(
    () => businessUnits.find((u) => u.id === lastBusinessUnitId)?.id ?? businessUnits[0]?.id,
  );
  const chosenUnit = businessUnits.find((u) => u.id === chosen);

  const proceed = async (unitId = chosen) => {
    if (!unitId || select.isPending) return;
    try {
      const selection = await select.mutateAsync({ data: { businessUnitId: unitId } });
      queryClient.clear();
      selectUnit(selection);
      navigate(redirectTarget(location.state), { replace: true });
    } catch {
      void message.error(t('auth.switchFailed'));
    }
  };

  return (
    <AuthLayout>
      <button type="button" className="erp-linkbtn is-muted erp-auth__back" onClick={endSession}>
        <ArrowLeftOutlined /> {t('selectUnit.back')}
      </button>
      <AuthHeading
        title={t('selectUnit.title')}
        lead={t('selectUnit.lead', {
          name: user?.fullName.split(' ')[0] ?? '',
          count: businessUnits.length,
        })}
      />
      <ChoiceCards
        label={t('shell.businessUnit')}
        value={chosen}
        onChange={setChosen}
        onConfirm={(id) => void proceed(id)}
        options={businessUnits.map((unit) => ({
          value: unit.id,
          title: unit.name,
          description: `${unit.code} · ${unit.roleName}`,
          icon: <ShopOutlined />,
          badge:
            unit.id === lastBusinessUnitId ? (
              <StatusTag tone="info">{t('auth.lastUsed')}</StatusTag>
            ) : undefined,
        }))}
      />
      <Button
        type="primary"
        size="large"
        block
        className="erp-auth__submit"
        disabled={!chosenUnit}
        loading={select.isPending}
        onClick={() => void proceed()}
      >
        {t('selectUnit.continue', { name: chosenUnit?.name ?? '' })} <ArrowRightOutlined />
      </Button>
    </AuthLayout>
  );
}
