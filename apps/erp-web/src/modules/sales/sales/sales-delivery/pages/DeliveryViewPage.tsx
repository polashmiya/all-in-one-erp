import {
  CalendarOutlined,
  CarOutlined,
  CheckSquareOutlined,
  HomeOutlined,
  IdcardOutlined,
  PrinterOutlined,
  RollbackOutlined,
  ShoppingOutlined,
  UserOutlined,
  WalletOutlined,
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
  formatNumber,
  useDisclosure,
} from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { DocumentView } from '@/common/documents/DocumentView';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { SalesLinesView } from '../../../components/SalesLinesView';
import { isPrintable } from '../../../print/print';
import { COLLECTION_FEATURE, collectionPaths } from '../../sales-collection/constants';
import { RETURN_FEATURE, returnPaths } from '../../sales-return/constants';
import { ConfirmDeliveryModal } from '../components/ConfirmDeliveryModal';
import { DeliveryFollowUps } from '../components/DeliveryFollowUps';
import { DELIVERY_FEATURE, deliveryPaths } from '../constants';
import { deliveryCalls } from '../delivery-form';

export function DeliveryViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(DELIVERY_FEATURE);
  const navigate = useNavigate();
  const returns = useFeatureAccess(RETURN_FEATURE);
  const collections = useFeatureAccess(COLLECTION_FEATURE);
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
          {
            key: 'return',
            label: t('salesDelivery.return'),
            icon: <RollbackOutlined />,
            hidden:
              !returns.create ||
              delivery.status !== 'approved' ||
              delivery.lines.every((line) => line.returnedQuantity >= line.quantity),
            onClick: () => navigate(returnPaths.fromDelivery(delivery.id)),
          },
          {
            key: 'collect',
            label: t('salesDelivery.collect'),
            icon: <WalletOutlined />,
            hidden:
              !collections.create || delivery.status !== 'approved' || delivery.pendingAmount <= 0,
            onClick: () => navigate(collectionPaths.fromCustomer(delivery.customerId)),
          },
          {
            key: 'invoice',
            label: t('print.invoice'),
            icon: <PrinterOutlined />,
            hidden: !isPrintable(delivery),
            onClick: () => navigate(deliveryPaths.invoice(delivery.id)),
          },
          {
            key: 'challan',
            label: t('print.challan'),
            icon: <PrinterOutlined />,
            hidden: !isPrintable(delivery),
            onClick: () => navigate(deliveryPaths.challan(delivery.id)),
          },
        ]}
        sections={[
          {
            key: 'money',
            content: (
              <DetailSection title={t('salesDelivery.money')}>
                <DescriptionList
                  items={[
                    money('net', t('sales.totals.total'), delivery.netAmount),
                    money('advance', t('salesDelivery.advanceUsed'), delivery.advanceBalanceAdjust),
                    money('received', t('salesDelivery.received'), delivery.receiveAmount),
                    money('collected', t('salesDelivery.collected'), delivery.collectedAmount),
                    money('returned', t('salesDelivery.returnAdjusted'), delivery.returnAdjusted),
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
          ...(delivery.followUps.length
            ? [{ key: 'followUps', content: <DeliveryFollowUps rows={delivery.followUps} /> }]
            : []),
          ...(delivery.isConfirmed
            ? [
                {
                  key: 'receipt',
                  content: (
                    <DetailSection title={t('salesDelivery.receipt')}>
                      <DescriptionList
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
            columns={[
              {
                key: 'returned',
                title: t('salesDelivery.lines.returned'),
                align: 'end',
                render: (_, line) => formatNumber(line.returnedQuantity, QUANTITY_DECIMALS_SHOWN),
              },
            ]}
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
