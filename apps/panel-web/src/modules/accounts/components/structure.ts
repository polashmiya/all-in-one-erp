import type { BusinessUnitDetail } from '@erp/api-client';
import type { BusinessUnitForm } from '../account-setup/model/setup-model';

export type StructureKind = 'business-unit' | 'office' | 'warehouse';

export interface StructureLeaf {
  key: string;
  code: string;
  name: string;
  isActive: boolean;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
}

export interface StructureOffice extends StructureLeaf {
  warehouses: StructureLeaf[];
}

export interface StructureUnit extends StructureLeaf {
  offices: StructureOffice[];
  warehouses: StructureLeaf[];
}

const upper = (value: string | undefined) => value?.trim().toUpperCase() ?? '';

export function structureFromForm(units: BusinessUnitForm[] | undefined): StructureUnit[] {
  return (units ?? []).map((unit, u) => {
    const offices: StructureOffice[] = (unit.offices ?? []).map((office, o) => ({
      key: `u${u}-o${o}`,
      code: upper(office?.code),
      name: office?.name?.trim() ?? '',
      isActive: true,
      warehouses: [],
    }));
    const unitWarehouses: StructureLeaf[] = [];

    (unit.warehouses ?? []).forEach((warehouse, w) => {
      const leaf = {
        key: `u${u}-w${w}`,
        code: upper(warehouse?.code),
        name: warehouse?.name?.trim() ?? '',
        isActive: true,
      };
      const office = warehouse?.officeCode
        ? offices.find((o) => o.code === upper(warehouse.officeCode))
        : undefined;
      (office ? office.warehouses : unitWarehouses).push(leaf);
    });

    return {
      key: `u${u}`,
      code: upper(unit.code),
      name: unit.name?.trim() ?? '',
      isActive: true,
      offices,
      warehouses: unitWarehouses,
    };
  });
}

export function structureFromDetail(units: BusinessUnitDetail[]): StructureUnit[] {
  return units.map((unit) => ({
    key: unit.id,
    code: unit.code,
    name: unit.name,
    isActive: unit.isActive,
    email: unit.email,
    phone: unit.phone,
    address: unit.address,
    offices: unit.offices.map((office) => ({
      key: office.id,
      code: office.code,
      name: office.name,
      isActive: office.isActive,
      email: office.email,
      phone: office.phone,
      address: office.address,
      warehouses: office.warehouses.map((w) => ({ key: w.id, ...w })),
    })),
    warehouses: unit.warehouses.map((w) => ({ key: w.id, ...w })),
  }));
}

export function structureCounts(units: StructureUnit[]) {
  return {
    units: units.length,
    offices: units.reduce((sum, u) => sum + u.offices.length, 0),
    warehouses: units.reduce(
      (sum, u) =>
        sum + u.warehouses.length + u.offices.reduce((s, o) => s + o.warehouses.length, 0),
      0,
    ),
  };
}

export const STRUCTURE_TINT = { unit: 'primary', office: 'violet', warehouse: 'amber' } as const;
