import {
  CalendarOutlined,
  EditOutlined,
  FileTextOutlined,
  HomeOutlined,
  UserOutlined,
} from '@ant-design/icons';
import {
  getGetPurchaseRequisitionQueryKey,
  useGetPurchaseRequisition,
  type FeatureConfig,
  type RequisitionDetail,
} from '@erp/api-client';
import { useViewTabs } from '@erp/metadata-engine';
import {
  DescriptionList,
  DetailSection,
  DetailsPageLayout,
  ForbiddenResult,
  formatDate,
  formatDateTime,
  StatusTag,
} from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { RequisitionLinesView } from '../components/RequisitionLinesView';
import { RequisitionStatusTag } from '../components/RequisitionStatusTag';
import { useRequisitionActions } from '../components/useRequisitionActions';
import { REQUISITION_FEATURE, requisitionPaths } from '../constants';

export function RequisitionViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(REQUISITION_FEATURE);
  const config = useFeatureConfig(REQUISITION_FEATURE, access.view);
  const query = useGetPurchaseRequisition(id, {
    query: { queryKey: useUnitKey(getGetPurchaseRequisitionQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  if (!query.data || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('requisition.notFound')}
        missingActions={[
          { key: 'back', label: t('requisition.backToList'), to: requisitionPaths.list },
        ]}
      />
    );
  }

  return <RequisitionView requisition={query.data} config={config.data} />;
}

function RequisitionView({
  requisition,
  config,
}: {
  requisition: RequisitionDetail;
  config: FeatureConfig;
}) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const access = useFeatureAccess(REQUISITION_FEATURE);
  const tabs = useViewTabs(config, requisition, {}, useCustomLookups(config));
  const { actions, modal } = useRequisitionActions((action) => {
    if (action === 'deleted') void navigate(requisitionPaths.list, { replace: true });
  });
  const [approve, ...more] = actions(requisition);

  const review = requisition.reviewedAt && (
    <DetailSection title={t('requisition.side.review')}>
      <DescriptionList
        columns={3}
        items={[
          {
            key: 'by',
            label: t('requisition.side.reviewedBy'),
            value: requisition.reviewedByName,
          },
          {
            key: 'at',
            label: t('requisition.side.reviewedAt'),
            value: formatDateTime(requisition.reviewedAt),
          },
          {
            key: 'comment',
            label: t(
              requisition.status === 'rejected' ? 'requisition.reason' : 'requisition.comment',
            ),
            value: requisition.reviewComment,
            wide: true,
          },
        ]}
      />
    </DetailSection>
  );

  const recordInfo = (
    <DetailSection title={t('requisition.side.recordInfo')}>
      <DescriptionList
        columns={3}
        items={[
          {
            key: 'created',
            label: t('requisition.side.created'),
            value: formatDateTime(requisition.createdAt),
          },
          {
            key: 'updated',
            label: t('requisition.side.updated'),
            value: requisition.modifiedAt ? formatDateTime(requisition.modifiedAt) : null,
          },
        ]}
      />
    </DetailSection>
  );

  const extra = [
    { key: 'lines', content: <RequisitionLinesView requisition={requisition} /> },
    ...(review ? [{ key: 'review', content: review }] : []),
    { key: 'record', content: recordInfo },
  ];

  return (
    <>
      <DetailsPageLayout
        crumbs={[requisition.code]}
        header={{
          backTo: requisitionPaths.list,
          icon: <FileTextOutlined />,
          tint: 'blue',
          title: requisition.code,
          tags: (
            <>
              <RequisitionStatusTag status={requisition.status} />
              {requisition.requestTypeName && (
                <StatusTag tone="violet">{requisition.requestTypeName}</StatusTag>
              )}
            </>
          ),
          meta: [
            {
              key: 'dates',
              icon: <CalendarOutlined />,
              label: [
                t('requisition.meta.requested', { date: formatDate(requisition.requestDate) }),
                requisition.dueDate
                  ? t('requisition.meta.due', { date: formatDate(requisition.dueDate) })
                  : t('requisition.meta.noDue'),
              ].join(' · '),
            },
            { key: 'warehouse', icon: <HomeOutlined />, label: requisition.warehouseName },
            {
              key: 'requester',
              icon: <UserOutlined />,
              label: [requisition.employeeName, requisition.departmentName]
                .filter(Boolean)
                .join(' · '),
            },
          ].filter((item) => item.label),
          actions: [
            {
              key: 'edit',
              label: t('master.edit'),
              icon: <EditOutlined />,
              variant: 'primary',
              hidden: !access.edit || requisition.status !== 'pending',
              to: requisitionPaths.edit(requisition.id),
            },
            ...(approve ? [approve] : []),
            {
              key: 'more',
              label: t('common.moreActions'),
              hidden: more.every((action) => action.hidden),
              menu: more,
            },
          ],
        }}
        tabs={tabs.map((tab, index) =>
          index === tabs.length - 1 ? { ...tab, sections: [...tab.sections, ...extra] } : tab,
        )}
      />
      {modal}
    </>
  );
}
