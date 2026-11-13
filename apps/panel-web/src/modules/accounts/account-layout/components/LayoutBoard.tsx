import { EditOutlined, HolderOutlined, UndoOutlined } from '@ant-design/icons';
import {
  closestCenter,
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  pointerWithin,
  useSensor,
  useSensors,
  type CollisionDetection,
  type DragEndEvent,
} from '@dnd-kit/core';
import {
  rectSortingStrategy,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import type { FieldConfigRow, SectionConfigRow } from '@erp/api-client';
import { useText } from '@erp/metadata-engine';
import { ActionGroup, StatusTag } from '@erp/ui';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { effective } from '../../account-configuration/field-settings';
import {
  DROP_TARGETS,
  nodeId,
  parseId,
  type NodeKind,
  type SectionNode,
  type TabNode,
} from '../layout-model';

const DRAG_START_DISTANCE = 6;
const MAX_COLUMNS = 4;

const columnsOf = (count?: number | null) =>
  Math.min(Math.max(count ?? MAX_COLUMNS, 1), MAX_COLUMNS);

const collision: CollisionDetection = (args) => {
  const kind = parseId(String(args.active.id)).kind;
  const kindOf = (id: unknown) => parseId(String(id)).kind;
  const droppableContainers = args.droppableContainers.filter((container) =>
    DROP_TARGETS[kind].includes(kindOf(container.id)),
  );
  const hits = pointerWithin({ ...args, droppableContainers });
  if (hits.length === 0) return closestCenter({ ...args, droppableContainers });
  const same = hits.filter((hit) => kindOf(hit.id) === kind);
  return same.length > 0 ? same : hits;
};

function useNode(kind: NodeKind, key: string) {
  const sortable = useSortable({ id: nodeId(kind, key) });
  const state = sortable.isDragging ? 'is-dragging' : sortable.isOver ? 'is-over' : '';

  return {
    ref: sortable.setNodeRef,
    style: {
      transform: CSS.Translate.toString(sortable.transform),
      transition: sortable.transition,
    },
    handle: { ...sortable.attributes, ...sortable.listeners },
    state,
  };
}

interface BoardActions {
  /** Mark tabs and sections changed from the standard layout and offer to reset each one. */
  showChanges: boolean;
  onEdit: (row: SectionConfigRow) => void;
  onReset: (row: SectionConfigRow) => void;
}

function useLabel() {
  const text = useText();
  return (row: FieldConfigRow | SectionConfigRow) => {
    const values = effective<{ labelEn?: string | null; labelBn?: string | null }>(row);
    return text({ en: values.labelEn ?? row.key, bn: values.labelBn ?? null }) ?? row.key;
  };
}

function Head({
  row,
  kind,
  handle,
  showChanges,
  onEdit,
  onReset,
}: { row: SectionConfigRow; kind: 'tab' | 'section'; handle: object } & BoardActions) {
  const { t } = useTranslation();
  const label = useLabel();
  const changed = showChanges && !!row.override;

  return (
    <header className="acc-layout__head">
      <button type="button" className="acc-layout__grip" aria-label={t('layout.drag')} {...handle}>
        <HolderOutlined />
      </button>
      <span className="acc-layout__title">{label(row)}</span>
      <StatusTag>{t(`layout.${kind}`)}</StatusTag>
      {effective(row).isVisible === false && (
        <StatusTag tone="warning">{t('layout.hidden')}</StatusTag>
      )}
      {changed && <StatusTag tone="violet">{t('config.changed')}</StatusTag>}
      <ActionGroup
        size="small"
        className="acc-layout__actions"
        actions={[
          {
            key: 'edit',
            label: t(`layout.edit.${kind}`),
            icon: <EditOutlined />,
            variant: 'text',
            iconOnly: true,
            onClick: () => onEdit(row),
          },
          {
            key: 'reset',
            label: t('config.reset'),
            icon: <UndoOutlined />,
            variant: 'text',
            iconOnly: true,
            hidden: !changed,
            onClick: () => onReset(row),
          },
        ]}
      />
    </header>
  );
}

function FieldChip({ row, columns }: { row: FieldConfigRow; columns: number }) {
  const { t } = useTranslation();
  const label = useLabel();
  const node = useNode('field', row.key);
  const values = effective(row);
  const span = Math.min(values.colSpan ?? 1, columns);

  return (
    <div
      ref={node.ref}
      style={node.style}
      className={`acc-layout__field ${span > 1 ? `erp-field-span-${span}` : ''} ${node.state}`}
      {...node.handle}
    >
      <HolderOutlined className="acc-layout__grip-icon" />
      <span className="acc-layout__field-label">{label(row)}</span>
      {values.isVisible === false && <StatusTag tone="warning">{t('layout.hidden')}</StatusTag>}
      {row.isCustom && <StatusTag tone="info">{t('config.custom.tag')}</StatusTag>}
    </div>
  );
}

function SectionBlock({ section, ...actions }: { section: SectionNode } & BoardActions) {
  const { t } = useTranslation();
  const node = useNode('section', section.key);
  const columns = columnsOf(effective(section.row).columns);

  return (
    <section ref={node.ref} style={node.style} className={`acc-layout__section ${node.state}`}>
      <Head row={section.row} kind="section" handle={node.handle} {...actions} />
      <SortableContext
        items={section.fields.map((field) => nodeId('field', field.key))}
        strategy={rectSortingStrategy}
      >
        <div className={`acc-layout__fields erp-field-grid erp-field-grid--${columns}`}>
          {section.fields.map((field) => (
            <FieldChip key={field.key} row={field} columns={columns} />
          ))}
        </div>
      </SortableContext>
      {section.fields.length === 0 && (
        <p className="acc-layout__empty">{t('layout.emptySection')}</p>
      )}
    </section>
  );
}

function TabBlock({ tab, ...actions }: { tab: TabNode } & BoardActions) {
  const { t } = useTranslation();
  const node = useNode('tab', tab.key);

  return (
    <section ref={node.ref} style={node.style} className={`erp-card acc-layout__tab ${node.state}`}>
      <Head row={tab.row} kind="tab" handle={node.handle} {...actions} />
      <SortableContext
        items={tab.sections.map((section) => nodeId('section', section.key))}
        strategy={verticalListSortingStrategy}
      >
        <div className="acc-layout__sections">
          {tab.sections.map((section) => (
            <SectionBlock key={section.key} section={section} {...actions} />
          ))}
        </div>
      </SortableContext>
      {tab.sections.length === 0 && <p className="acc-layout__empty">{t('layout.emptyTab')}</p>}
    </section>
  );
}

export function LayoutBoard({
  tabs,
  onMove,
  ...actions
}: { tabs: TabNode[]; onMove: (activeId: string, overId: string) => void } & BoardActions) {
  const label = useLabel();
  const [dragging, setDragging] = useState<string>();
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: DRAG_START_DISTANCE } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  const rows = tabs.flatMap((tab) => [
    [nodeId('tab', tab.key), tab.row] as const,
    ...tab.sections.flatMap((section) => [
      [nodeId('section', section.key), section.row] as const,
      ...section.fields.map((field) => [nodeId('field', field.key), field] as const),
    ]),
  ]);
  const dragged = rows.find(([id]) => id === dragging)?.[1];

  const drop = ({ active, over }: DragEndEvent) => {
    setDragging(undefined);
    if (over) onMove(String(active.id), String(over.id));
  };

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={collision}
      onDragStart={({ active }) => setDragging(String(active.id))}
      onDragCancel={() => setDragging(undefined)}
      onDragEnd={drop}
    >
      <SortableContext
        items={tabs.map((tab) => nodeId('tab', tab.key))}
        strategy={verticalListSortingStrategy}
      >
        <div className="acc-layout__tabs">
          {tabs.map((tab) => (
            <TabBlock key={tab.key} tab={tab} {...actions} />
          ))}
        </div>
      </SortableContext>
      <DragOverlay>
        {dragged && (
          <div className="acc-layout__field is-overlay">
            <HolderOutlined className="acc-layout__grip-icon" />
            <span className="acc-layout__field-label">{label(dragged)}</span>
          </div>
        )}
      </DragOverlay>
    </DndContext>
  );
}
