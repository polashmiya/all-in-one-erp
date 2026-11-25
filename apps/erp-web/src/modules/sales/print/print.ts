const PRINTABLE = ['approved', 'closed'];

export const isPrintable = (record: { status: string }) => PRINTABLE.includes(record.status);
