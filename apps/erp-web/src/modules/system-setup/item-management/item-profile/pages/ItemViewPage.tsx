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
  type FeatureConfig,
  type ItemDetail,
} from '@erp/api-client';
import { useViewTabs } from '@erp/metadata-engine';
import {
  DescriptionList,
  DetailSection,
  DetailsPageLayout,
  ForbiddenResult,
  formatDateTime,
  formatNumber,
  StatusTag,
} from '@erp/ui';
import { App } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { selectCurrentUnit, useAuthStore } from '@/common/store/auth-store';
import { problemMessage } from '@/common/utils/api-errors';
import { useUnitKey } from '@/common/master-data';
import { useRefreshItemManagement } from '../../item-management';
import { ITEM_PROFILE_FEATURE, itemPaths } from '../constants';

export function ItemViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(ITEM_PROFILE_FEATURE);
  const config = useFeatureConfig(ITEM_PROFILE_FEATURE, access.view);
  const query = useGetItem(id, {
    query: { queryKey: useUnitKey(getGetItemQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  if (!query.data || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('item.notFound')}
        missingActions={[{ key: 'back', label: t('item.backToList'), to: itemPaths.list }]}
      />
    );
  }

  return <ItemView item={query.data} config={config.data} />;
}

function ItemView({ item, config }: { item: ItemDetail; config: FeatureConfig }) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const unit = useAuthStore(selectCurrentUnit);
  const access = useFeatureAccess(ITEM_PROFILE_FEATURE);
  const setStatus = useSetItemsStatus();
  const remove = useDeleteItem();
  const refresh = useRefreshItemManagement();
  const tabs = useViewTabs(
    config,
    item,
    {
      masterItemId: (r) =>
        r.masterItemId ? (
          <Link to={itemPaths.view(r.masterItemId)}>
            {r.masterItemName} ({r.masterItemCode})
          </Link>
        ) : null,
      altUomConversionQty: (r) =>
        r.altUomName
          ? t('item.v.conversion', {
              alt: r.altUomName,
              qty: formatNumber(r.altUomConversionQty, 2),
              uom: r.uomName,
            })
          : null,
    },
    useCustomLookups(config),
  );

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

  const recordInfo = (
    <DetailSection title={t('item.side.recordInfo')}>
      <DescriptionList
        columns={3}
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
            {item.itemTypeName && <StatusTag tone="violet">{item.itemTypeName}</StatusTag>}
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
            key: 'edit',
            label: t('master.edit'),
            icon: <EditOutlined />,
            variant: 'primary',
            hidden: !access.edit,
            to: itemPaths.edit(item.id),
          },
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
        ],
      }}
      tabs={tabs.map((tab, index) =>
        index === tabs.length - 1
          ? { ...tab, sections: [...tab.sections, { key: 'record', content: recordInfo }] }
          : tab,
      )}
    />
  );
}
