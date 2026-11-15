import {
  countStructure,
  isDuplicateCode,
  stepOfField,
  suggestCode,
  toFieldPath,
  toSetupRequest,
  type SetupFormValues,
} from './setup-model';

describe('suggestCode', () => {
  it('drops company suffixes and joins words with dashes', () => {
    expect(suggestCode('Demo Pharma Ltd.')).toBe('DEMO-PHARMA');
    expect(suggestCode('ACI Limited')).toBe('ACI');
    expect(suggestCode('  Square   Toiletries & Co ')).toBe('SQUARE-TOILETRIES');
  });

  it('keeps the suffix when it is the only word', () => {
    expect(suggestCode('Limited')).toBe('LIMITED');
  });

  it('keeps whole words that fit the maximum length', () => {
    expect(suggestCode('North Division', 12)).toBe('NORTH');
    expect(suggestCode('Head Office', 12)).toBe('HEAD-OFFICE');
    expect(suggestCode('Dhaka Central Distribution Hub', 20)).toBe('DHAKA-CENTRAL');
  });

  it('truncates a single word that is too long', () => {
    expect(suggestCode('Pharmaceuticals', 12)).toBe('PHARMACEUTIC');
  });

  it('returns an empty code for names without Latin letters or digits', () => {
    expect(suggestCode('ঢাকা ডিপো')).toBe('');
    expect(suggestCode(undefined)).toBe('');
  });
});

describe('toSetupRequest', () => {
  const values: SetupFormValues = {
    account: {
      code: ' demo-pharma ',
      name: ' Demo Pharma ',
      email: 'info@demo.com',
      phone: '+8801700000000',
      website: '  ',
      address: 'Tejgaon',
      country: 'BD',
      baseCurrency: 'BDT',
      timeZone: 'Asia/Dhaka',
    },
    businessUnits: [
      {
        code: 'dhk',
        name: 'Dhaka',
        offices: [{ code: 'ho', name: 'Head Office', phone: '' }],
        warehouses: [
          { code: 'cwh', name: 'Central', officeCode: 'ho' },
          { code: 'trn', name: 'Transit', officeCode: '' },
        ],
      },
    ],
    admin: {
      fullName: ' Admin ',
      username: ' Demo.Admin ',
      email: ' Admin@Demo.com ',
      passwordMode: 'generate',
      password: 'ignored',
    },
  };

  it('trims, upper-cases codes and nulls empty optional fields', () => {
    const request = toSetupRequest(values);

    expect(request.account.code).toBe('DEMO-PHARMA');
    expect(request.account.name).toBe('Demo Pharma');
    expect(request.account.website).toBeNull();
    expect(request.businessUnits[0]!.code).toBe('DHK');
    expect(request.businessUnits[0]!.offices[0]).toEqual({
      code: 'HO',
      name: 'Head Office',
      email: null,
      phone: null,
      address: null,
    });
  });

  it('sends the chosen features, or none when the step was never filled', () => {
    expect(toSetupRequest({ ...values, featureCodes: ['system-setup.uom'] }).featureCodes).toEqual([
      'system-setup.uom',
    ]);
    expect(toSetupRequest(values).featureCodes).toEqual([]);
  });

  it('maps the unit-level choice to a null office', () => {
    const [central, transit] = toSetupRequest(values).businessUnits[0]!.warehouses;

    expect(central!.officeCode).toBe('HO');
    expect(transit!.officeCode).toBeNull();
  });

  it('sends no password when it should be generated, and the typed one otherwise', () => {
    expect(toSetupRequest(values).admin).toEqual({
      fullName: 'Admin',
      username: 'demo.admin',
      email: 'admin@demo.com',
      password: null,
    });
    expect(
      toSetupRequest({
        ...values,
        admin: { ...values.admin, passwordMode: 'manual', password: 'Secret@123' },
      }).admin.password,
    ).toBe('Secret@123');
  });
});

describe('server error mapping', () => {
  it('turns API keys into form paths', () => {
    expect(toFieldPath('account.code')).toEqual(['account', 'code']);
    expect(toFieldPath('businessUnits[1].offices[0].code')).toEqual([
      'businessUnits',
      1,
      'offices',
      0,
      'code',
    ]);
    expect(toFieldPath('businessUnits')).toEqual(['businessUnits']);
  });

  it('finds the step that owns a field', () => {
    expect(stepOfField(['account', 'code'])).toBe(0);
    expect(stepOfField(['businessUnits', 1, 'code'])).toBe(1);
    expect(stepOfField(['businessUnits', 0, 'offices', 2, 'name'])).toBe(2);
    expect(stepOfField(['businessUnits', 0, 'warehouses', 0, 'officeCode'])).toBe(3);
    expect(stepOfField(['featureCodes'])).toBe(4);
    expect(stepOfField(['admin', 'email'])).toBe(5);
  });
});

describe('structure helpers', () => {
  it('counts units, offices and warehouses', () => {
    expect(
      countStructure([
        { offices: [{}, {}], warehouses: [{}] },
        { offices: [], warehouses: [{}, {}] },
      ]),
    ).toEqual({ businessUnits: 2, offices: 2, warehouses: 3 });
  });

  it('detects duplicate sibling codes case-insensitively', () => {
    expect(isDuplicateCode(['DHK', 'dhk ', 'CTG'], 1)).toBe(true);
    expect(isDuplicateCode(['DHK', 'CTG'], 1)).toBe(false);
    expect(isDuplicateCode(['', ''], 0)).toBe(false);
  });
});
