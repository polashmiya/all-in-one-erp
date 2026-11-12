import { changedValues } from './history';

describe('change history', () => {
  it('lists only the settings that changed', () => {
    expect(
      changedValues(
        { labelEn: 'Old', isVisible: true },
        { labelEn: 'New', isVisible: true, sortOrder: 5 },
      ),
    ).toEqual([
      { path: ['labelEn'], before: 'Old', after: 'New' },
      { path: ['sortOrder'], before: undefined, after: 5 },
    ]);
  });

  it('treats a new or removed override as a change from or to nothing', () => {
    expect(changedValues(null, { isRequired: true })).toEqual([
      { path: ['isRequired'], before: undefined, after: true },
    ]);
  });

  it('names layout changes by tab and section', () => {
    const before = { tabs: [{ key: 'general', sections: [{ key: 'basic', fields: ['a', 'b'] }] }] };
    const after = { tabs: [{ key: 'general', sections: [{ key: 'basic', fields: ['b', 'a'] }] }] };

    expect(changedValues(before, after)).toEqual([
      { path: ['general', 'basic', 'fields'], before: ['a', 'b'], after: ['b', 'a'] },
    ]);
  });
});
