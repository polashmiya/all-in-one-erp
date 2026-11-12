export interface ChangedValue {
  path: string[];
  before: unknown;
  after: unknown;
}

type Flat = Map<string, { path: string[]; value: unknown }>;

const isObject = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null && !Array.isArray(value);

const isKeyedList = (value: unknown): value is { key: string }[] =>
  Array.isArray(value) &&
  value.length > 0 &&
  value.every((item) => isObject(item) && 'key' in item);

function flatten(value: unknown, path: string[] = [], out: Flat = new Map()): Flat {
  if (isKeyedList(value)) {
    for (const item of value) {
      const { key, ...rest } = item;
      flatten(rest, [...path, String(key)], out);
    }
  } else if (isObject(value)) {
    for (const [key, child] of Object.entries(value))
      flatten(child, isKeyedList(child) ? path : [...path, key], out);
  } else if (path.length > 0) {
    out.set(path.join('.'), { path, value });
  }
  return out;
}

export function changedValues(before: unknown, after: unknown): ChangedValue[] {
  const from = flatten(before);
  const to = flatten(after);
  const keys = [...new Set([...from.keys(), ...to.keys()])];

  return keys
    .map((key) => ({
      path: (to.get(key) ?? from.get(key))!.path,
      before: from.get(key)?.value,
      after: to.get(key)?.value,
    }))
    .filter((change) => JSON.stringify(change.before) !== JSON.stringify(change.after));
}
