import {
  CalendarOutlined,
  CarOutlined,
  CheckSquareOutlined,
  HomeOutlined,
  IdcardOutlined,
  ShoppingOutlined,
  UserOutlined,
} from '@ant-design/icons';
import {
  getGetEmployeeOptionsQueryKey,
  getGetSalesDeliveryQueryKey,
  useGetEmployeeOptions,
  useGetSalesDelivery,
  type SalesDeliveryDetail,
} from '@erp/api-client';
import {
  DescriptionList,
  DetailSection,
  DetailsPageLayout,
  ForbiddenResult,
  formatDate,
  formatMoney,
  useDisclosure,
} from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { DocumentView } from '@/common/documents/DocumentView';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { SalesLinesView } from '../../../components/SalesLinesView';
import { ConfirmDeliveryModal } from '../components/ConfirmDeliveryModal';
import { DELIVERY_FEATURE, deliveryPaths } from '../constants';
import { deliveryCalls } from '../delivery-form';

export function DeliveryViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(DELIVERY_FEATURE);
  const confirm = useDisclosure();
  const config = useFeatureConfig(DELIVERY_FEATURE, access.view);
  const query = useGetSalesDelivery(id, {
    query: { queryKey: useUnitKey(getGetSalesDeliveryQueryKey(id)), enabled: access.view },
  });
  const employees = useGetEmployeeOptions({
    query: {
      queryKey: useUnitKey(getGetEmployeeOptionsQueryKey()),
      enabled: access.edit && confirm.open,
    },
  });

  if (!access.view) return <ForbiddenResult />;

  const delivery = query.data;
  if (!delivery || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('salesDelivery.notFound')}
        missingActions={[
          { key: 'back', label: t('salesDelivery.backToList'), to: deliveryPaths.list },
        ]}
      />
    );
  }

  const money = (key: string, label: string, value: number) => ({
    key,
    label,
    value: formatMoney(value),
  });

  return (
    <>
      <DocumentView<SalesDeliveryDetail>
        document={delivery}
        config={config.data}
        feature={DELIVERY_FEATURE}
        paths={deliveryPaths}
        calls={{ ...deliveryCalls, canCancel: () => !delivery.isConfirmed }}
        icon={<CarOutlined />}
        tag={
          delivery.isConfirmed
            ? t('salesDelivery.confirmed')
            : t(`salesDelivery.type.${delivery.type}`)
        }
        meta={[
          { key: 'customer', icon: <UserOutlined />, label: delivery.customerName },
          { key: 'order', icon: <ShoppingOutlined />, label: delivery.orderName },
          {
            key: 'date',
            icon: <CalendarOutlined />,
            label: t('salesDelivery.meta.delivery', { date: formatDate(delivery.deliveryDate) }),
          },
          { key: 'warehouse', icon: <HomeOutlined />, label: delivery.warehouseName },
          { key: 'salesForce', icon: <IdcardOutlined />, label: delivery.salesForceName },
        ]}
        renderers={{
          shippingAddressId: (record) => record.shipToName ?? '—',
          type: (record) => t(`salesDelivery.type.${record.type}`),
        }}
        moreActions={[
          {
            key: 'confirm',
            label: t('salesDelivery.confirm'),
            icon: <CheckSquareOutlined />,
            hidden: !access.edit || delivery.status !== 'approved' || delivery.isConfirmed,
            onClick: () => confirm.onOpen(),
          },
        ]}
        sections={[
          {
            key: 'money',
            content: (
              <DetailSection title={t('salesDelivery.money')}>
                <DescriptionList
                  columns={3}
                  items={[
                    money('net', t('sales.totals.total'), delivery.netAmount),
                    money('advance', t('salesDelivery.advanceUsed'), delivery.advanceBalanceAdjust),
                    money('received', t('salesDelivery.received'), delivery.receiveAmount),
                    money('due', t('salesDelivery.due'), delivery.pendingAmount),
                    ...(delivery.totalNoOfInstallment
                      ? [
                          money(
                            'interest',
                            t('salesDelivery.withInterest'),
                            delivery.netAmountWithInterest,
                          ),
                          money(
                            'installment',
                            t('salesDelivery.perInstallment'),
                            delivery.amountPerInstallment,
                          ),
                        ]
                      : []),
                    ...(delivery.totalCost
                      ? [money('cost', t('salesDelivery.cost'), delivery.totalCost)]
                      : []),
                  ]}
                />
              </DetailSection>
            ),
          },
          ...(delivery.isConfirmed
            ? [
                {
                  key: 'receipt',
                  content: (
                    <DetailSection title={t('salesDelivery.receipt')}>
                      <DescriptionList
                        columns={3}
                        items={[
                          {
                            key: 'date',
                            label: t('salesDelivery.f.actualDate'),
                            value: formatDate(delivery.actualDeliveryDate),
                          },
                          {
                            key: 'collectedBy',
                            label: t('salesDelivery.f.collectedBy'),
                            value: delivery.collectedByName,
                          },
                          {
                            key: 'confirmedBy',
                            label: t('salesDelivery.f.confirmedBy'),
                            value: delivery.confirmedByName,
                          },
                          {
                            key: 'grnNo',
                            label: t('salesDelivery.f.grnNo'),
                            value: delivery.grnNo,
                          },
                          {
                            key: 'grnDate',
                            label: t('salesDelivery.f.grnDate'),
                            value: delivery.grnDate && formatDate(delivery.grnDate),
                          },
                          {
                            key: 'grnAmount',
                            label: t('salesDelivery.f.grnAmount'),
                            value:
                              delivery.grnAmount === null ? null : formatMoney(delivery.grnAmount),
                          },
                          {
                            key: 'grnCollectionDate',
                            label: t('salesDelivery.f.grnCollectionDate'),
                            value:
                              delivery.grnCollectionDate && formatDate(delivery.grnCollectionDate),
                          },
                          {
                            key: 'remarks',
                            label: t('salesDelivery.f.remarks'),
                            value: delivery.confirmRemarks,
                            wide: true,
                          },
                        ]}
                      />
                    </DetailSection>
                  ),
                },
              ]
            : []),
        ]}
        lines={
          <SalesLinesView<SalesDeliveryDetail>
            record={delivery}
            details={(line) => [
              { key: 'batch', label: t('salesDelivery.lines.batch'), value: line.batch },
              { key: 'serial', label: t('salesDelivery.lines.serial'), value: line.itemSerial },
              { key: 'pack', label: t('salesDelivery.lines.pack'), value: line.packQuantity },
              {
                key: 'expiry',
                label: t('salesDelivery.lines.warrantyExpiry'),
                value: line.warrantyExpiredDate && formatDate(line.warrantyExpiredDate),
              },
              {
                key: 'warrantyText',
                label: t('salesDelivery.lines.warrantyText'),
                value: line.warrantyDescription,
              },
              ...(line.costTotal
                ? [money('cost', t('salesDelivery.lines.cost'), line.costTotal)]
                : []),
            ]}
          />
        }
      />
      <ConfirmDeliveryModal
        delivery={delivery}
        employees={employees.data}
        open={confirm.open}
        onClose={confirm.onClose}
      />
    </>
  );
}
