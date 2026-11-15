import { AppstoreOutlined, FileTextOutlined } from '@ant-design/icons';
import type { LocalizedLabel } from '@erp/api-client';
import { localize, resolveIcon, useLanguageStore } from '@erp/ui';
import {
  Button,
  Empty,
  Flex,
  Skeleton,
  Space,
  Tag,
  Tree,
  Typography,
  type TreeDataNode,
} from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { limitCatalog, useFeatureCatalog } from './feature-catalog';

function useLabel() {
  const language = useLanguageStore((s) => s.language);
  return (label: LocalizedLabel) =>
    localize({ en: label.en, ...(label.bn ? { bn: label.bn } : {}) }, language);
}

export function FeatureAccessTree({
  value,
  onChange,
  available,
}: {
  value?: string[];
  onChange?: (codes: string[]) => void;
  available?: string[];
}) {
  const { t } = useTranslation();
  const label = useLabel();
  const catalog = useFeatureCatalog();
  const { modules, allCodes } = useMemo(
    () => limitCatalog(catalog.modules, available),
    [catalog.modules, available],
  );
  const loading = catalog.loading;
  const selected = useMemo(() => value ?? [], [value]);

  const treeData = useMemo<TreeDataNode[]>(
    () =>
      modules.map((module) => {
        const codes = module.subModules.flatMap((s) => s.features.map((f) => f.code));
        const chosen = codes.filter((c) => selected.includes(c)).length;
        return {
          key: `module:${module.key}`,
          icon: resolveIcon(module.icon) ?? <AppstoreOutlined />,
          title: (
            <Space size={8}>
              <Typography.Text strong>{label(module.label)}</Typography.Text>
              <Tag bordered={false} color={chosen ? 'processing' : 'default'} className="erp-flush">
                {chosen}/{codes.length}
              </Tag>
            </Space>
          ),
          children: module.subModules.map((subModule) => ({
            key: `sub:${module.key}.${subModule.key}`,
            icon: resolveIcon(subModule.icon) ?? <AppstoreOutlined />,
            title: <Typography.Text>{label(subModule.label)}</Typography.Text>,
            children: subModule.features.map((feature) => ({
              key: feature.code,
              isLeaf: true,
              icon: <FileTextOutlined className="erp-fg--muted" />,
              title: (
                <Space size={8}>
                  {label(feature.label)}
                  <Typography.Text className="acc-feature-code erp-code">
                    {feature.code}
                  </Typography.Text>
                </Space>
              ),
            })),
          })),
        };
      }),
    [modules, selected, label],
  );

  return (
    <div>
      <Flex justify="space-between" align="center" gap={12} wrap className="acc-toolbar">
        <Typography.Text type="secondary">
          {t('menus.selected', { count: selected.length, total: allCodes.length })}
        </Typography.Text>
        <Space>
          <Button size="small" onClick={() => onChange?.(allCodes)} disabled={loading}>
            {t('menus.selectAll')}
          </Button>
          <Button size="small" onClick={() => onChange?.([])} disabled={loading}>
            {t('menus.clear')}
          </Button>
        </Space>
      </Flex>
      <div className="erp-boxed">
        {loading ? (
          <Skeleton active paragraph={{ rows: 5 }} title={false} />
        ) : modules.length === 0 ? (
          <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={t('menus.empty')} />
        ) : (
          <Tree
            checkable
            showIcon
            blockNode
            selectable={false}
            defaultExpandAll
            treeData={treeData}
            checkedKeys={selected}
            onCheck={(checked) => {
              const keys = Array.isArray(checked) ? checked : checked.checked;
              onChange?.(keys.map(String).filter((key) => allCodes.includes(key)));
            }}
          />
        )}
      </div>
    </div>
  );
}

export function FeatureAccessSummary({ codes }: { codes: string[] }) {
  const { t } = useTranslation();
  const label = useLabel();
  const { modules, loading } = useFeatureCatalog();

  if (loading) return <Skeleton active paragraph={{ rows: 2 }} title={false} />;

  const groups = modules.flatMap((module) =>
    module.subModules
      .map((subModule) => ({
        key: `${module.key}.${subModule.key}`,
        title: `${label(module.label)} › ${label(subModule.label)}`,
        features: subModule.features.filter((f) => codes.includes(f.code)),
      }))
      .filter((group) => group.features.length > 0),
  );

  if (groups.length === 0)
    return <Typography.Text type="secondary">{t('menus.none')}</Typography.Text>;

  return (
    <Flex vertical gap={10}>
      {groups.map((group) => (
        <div key={group.key} className="acc-group">
          <Typography.Text className="acc-group__title">{group.title}</Typography.Text>
          <div>
            {group.features.map((feature) => (
              <Tag key={feature.code} bordered={false} color="processing">
                {label(feature.label)}
              </Tag>
            ))}
          </div>
        </div>
      ))}
    </Flex>
  );
}
