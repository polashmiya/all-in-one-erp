import {
  AppstoreOutlined,
  BarcodeOutlined,
  CalendarOutlined,
  CopyOutlined,
  EditOutlined,
  TagOutlined,
} from '@ant-design/icons';
import {
  getGetItemQueryKey,
  useDeleteItem,
  useGetItem,
  useSetItemsStatus,
  type ItemDetail,
} from '@erp/api-client';
import {
  DescriptionList,
  DetailSection,
  DetailsPageLayout,
  ForbiddenResult,
  formatDateTime,
  formatMoney,
  formatNumber,
  formatPercent,
  StatusTag,
} from '@erp/ui';
import { App } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { problemMessage } from '@/common/utils/api-errors';
import { useRefreshItemManagement, useUnitKey } from '../../common/master-data';
import { ITEM_PROFILE_FEATURE, itemPaths } from '../constants';

export function ItemViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(ITEM_PROFILE_FEATURE);
  const query = useGetItem(id, {
    query: { queryKey: useUnitKey(getGetItemQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  if (!query.data) {
    return (
      <DetailsPageLayout
        loading={query.isPending}
        missing={!query.isPending}
        missingTitle={t('item.notFound')}
        missingActions={[{ key: 'back', label: t('item.backToList'), to: itemPaths.list }]}
      />
    );
  }

  return <ItemView item={query.data} />;
}

function ItemView({ item }: { item: ItemDetail }) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(ITEM_PROFILE_FEATURE);
  const setStatus = useSetItemsStatus();
  const remove = useDeleteItem();
  const refresh = useRefreshItemManagement();
  const flag = (on: boolean) => (
    <StatusTag tone={on ? 'success' : 'neutral'} dot>
      {on ? t('common.yes') : t('common.no')}
    </StatusTag>
  );
  const percent = (value?: number | null) => (value == null ? null : formatPercent(value));

  const failed = (error: unknown) => void message.error(problemMessage(error, t('master.failed')));

  const toggleStatus = async () => {
    try {
      await setStatus.mutateAsync({ data: { ids: [item.id], isActive: !item.isActive } });
      await refresh();
      void message.success(
        t(item.isActive ? 'master.deactivated' : 'master.activated', { count: 1 }),
      );
    } catch (error) {
      failed(error);
    }
  };

  const deleteItem = async () => {
    try {
      await remove.mutateAsync({ id: item.id });
      void navigate(itemPaths.list, { replace: true });
      void refresh();
      void message.success(t('master.deleted', { count: 1 }));
    } catch (error) {
      failed(error);
    }
  };

  const general = (
    <>
      <DetailSection title={t('item.sec.basic')}>
        <DescriptionList
          columns={4}
          items={[
            { key: 'name', label: t('item.f.name'), value: item.name },
            { key: 'type', label: t('item.f.itemType'), value: item.itemTypeName },
            { key: 'hs', label: t('item.f.hsCode'), value: item.hsCode, code: true },
            { key: 'code', label: t('item.f.code'), value: item.code, code: true },
            {
              key: 'description',
              label: t('item.f.description'),
              value: item.description,
              wide: true,
            },
          ]}
        />
      </DetailSection>
      <DetailSection title={t('item.sec.classification')}>
        <DescriptionList
          columns={4}
          items={[
            { key: 'department', label: t('item.f.department'), value: item.departmentName },
            { key: 'category', label: t('item.f.category'), value: item.categoryName },
            { key: 'sub', label: t('item.f.subCategory'), value: item.subCategoryName },
            { key: 'minor', label: t('item.f.minorCategory'), value: item.minorCategoryName },
          ]}
        />
      </DetailSection>
      <DetailSection title={t('item.sec.barcode')}>
        <DescriptionList
          columns={4}
          items={[
            { key: 'auto', label: t('item.f.autoBarcode'), value: flag(item.isAutoBarcode) },
            { key: 'barcode', label: t('item.f.barcode'), value: item.barCode, code: true },
            { key: 'child', label: t('item.f.childBarcode'), value: flag(item.isChildBarcode) },
            {
              key: 'master',
              label: t('item.f.masterItem'),
              value: item.masterItemId ? (
                <Link to={itemPaths.view(item.masterItemId)}>
                  {item.masterItemName} ({item.masterItemCode})
                </Link>
              ) : null,
            },
          ]}
        />
      </DetailSection>
    </>
  );

  const pricing = (
    <>
      <DetailSection title={t('item.sec.units')}>
        <DescriptionList
          columns={4}
          items={[
            { key: 'uom', label: t('item.f.uom'), value: item.uomName },
            { key: 'decimal', label: t('item.f.noDecimal'), value: flag(item.isNoDecimalQty) },
            { key: 'alt', label: t('item.f.altUom'), value: item.altUomName },
            {
              key: 'conversion',
              label: t('item.f.conversion'),
              value: item.altUomName
                ? t('item.v.conversion', {
                    alt: item.altUomName,
                    qty: formatNumber(item.altUomConversionQty, 2),
                    uom: item.uomName,
                  })
                : null,
            },
          ]}
        />
      </DetailSection>
      <DetailSection title={t('item.sec.prices')}>
        <DescriptionList
          columns={4}
          items={[
            { key: 'last', label: t('item.f.lastCost'), value: formatMoney(item.lastCostPrice) },
            {
              key: 'average',
              label: t('item.f.averageCost'),
              value: formatMoney(item.averageCostPrice),
            },
            { key: 'sales', label: t('item.f.salesPrice'), value: formatMoney(item.salesPrice) },
            { key: 'supplier', label: t('item.f.supplier'), value: item.supplierName },
          ]}
        />
      </DetailSection>
    </>
  );

  const tax = (
    <DetailSection title={t('item.tab.tax')}>
      <DescriptionList
        columns={4}
        items={[
          {
            key: 'pType',
            label: t('item.v.purchaseSupplyType'),
            value: item.purchaseSupplyTypeName,
          },
          {
            key: 'pVat',
            label: t('item.v.purchaseVat'),
            value: percent(item.purchaseVatPercentage),
          },
          { key: 'sType', label: t('item.v.salesSupplyType'), value: item.salesSupplyTypeName },
          { key: 'sVat', label: t('item.v.salesVat'), value: percent(item.salesVatPercentage) },
          { key: 'sd', label: t('item.f.sd'), value: percent(item.sdPercentage) },
          { key: 'inclusive', label: t('item.f.vatInclusive'), value: flag(item.isVatInclusive) },
          { key: 'rebate', label: t('item.f.vatRebatable'), value: flag(item.isVatRebatable) },
        ]}
      />
    </DetailSection>
  );

  const status = (
    <>
      <DetailSection title={t('item.side.usage')}>
        <DescriptionList
          columns={4}
          items={[
            { key: 'active', label: t('item.f.active'), value: flag(item.isActive) },
            { key: 'saleable', label: t('item.f.saleable'), value: flag(item.isSaleable) },
            { key: 'purchase', label: t('item.f.purchasable'), value: flag(item.isPurchase) },
            {
              key: 'negative',
              label: t('item.f.negativeSales'),
              value: flag(item.isNegativeSales),
            },
            {
              key: 'multiple',
              label: t('item.f.multiplePrices'),
              value: flag(item.isMultipleSalesPrice),
            },
          ]}
        />
      </DetailSection>
      <DetailSection title={t('item.side.recordInfo')}>
        <DescriptionList
          columns={4}
          items={[
            { key: 'unit', label: t('item.side.businessUnit'), value: unit?.name },
            { key: 'createdBy', label: t('item.side.createdBy'), value: item.createdByName },
            {
              key: 'created',
              label: t('item.side.created'),
              value: formatDateTime(item.createdAt),
            },
            { key: 'modifiedBy', label: t('item.side.modifiedBy'), value: item.modifiedByName },
            {
              key: 'updated',
              label: t('item.side.updated'),
              value: item.modifiedAt ? formatDateTime(item.modifiedAt) : null,
            },
          ]}
        />
      </DetailSection>
    </>
  );

  return (
    <DetailsPageLayout
      crumbs={[item.code]}
      header={{
        backTo: itemPaths.list,
        icon: item.imageSource ? <img src={item.imageSource} alt="" /> : <AppstoreOutlined />,
        title: item.name,
        tags: (
          <>
            <StatusTag tone={item.isActive ? 'success' : 'neutral'} dot>
              {item.isActive ? t('master.active') : t('master.inactive')}
            </StatusTag>
            <StatusTag tone="violet">{item.itemTypeName}</StatusTag>
            {item.isChildBarcode && (
              <StatusTag tone="warning">{t('item.f.childBarcode')}</StatusTag>
            )}
          </>
        ),
        meta: [
          { key: 'code', icon: <BarcodeOutlined />, label: `${item.code} · ${item.barCode}` },
          {
            key: 'category',
            icon: <TagOutlined />,
            label: `${item.categoryName} › ${item.subCategoryName} › ${item.minorCategoryName}`,
          },
          {
            key: 'updated',
            icon: <CalendarOutlined />,
            label: t('item.v.updatedBy', {
              date: formatDateTime(item.modifiedAt ?? item.createdAt),
              name: (item.modifiedAt ? item.modifiedByName : item.createdByName) ?? '—',
            }),
          },
        ],
        actions: [
          {
            key: 'duplicate',
            label: t('item.duplicate'),
            icon: <CopyOutlined />,
            hidden: !access.create,
            to: itemPaths.duplicate(item.id),
          },
          {
            key: 'more',
            label: t('common.moreActions'),
            hidden: !access.edit && !access.delete,
            menu: [
              {
                key: 'toggle',
                label: item.isActive ? t('master.deactivate') : t('master.activate'),
                hidden: !access.edit,
                onClick: toggleStatus,
              },
              {
                key: 'delete',
                label: t('master.delete'),
                danger: true,
                hidden: !access.delete,
                confirm: {
                  title: t('master.deleteOne', { name: item.name }),
                  description: t('master.deleteHint'),
                  okText: t('master.delete'),
                  danger: true,
                },
                onClick: deleteItem,
              },
            ],
          },
          {
            key: 'edit',
            label: t('master.edit'),
            icon: <EditOutlined />,
            variant: 'primary',
            hidden: !access.edit,
            to: itemPaths.edit(item.id),
          },
        ],
      }}
      tabs={[
        { key: 'general', label: t('item.tab.general'), content: general },
        { key: 'pricing', label: t('item.tab.pricing'), content: pricing },
        { key: 'tax', label: t('item.tab.tax'), content: tax },
        { key: 'status', label: t('item.tab.status'), content: status },
      ]}
    />
  );
}
