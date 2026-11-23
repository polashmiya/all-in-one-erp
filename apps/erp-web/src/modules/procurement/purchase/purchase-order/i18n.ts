import type { AppResources } from '@erp/ui';

export const purchaseOrderResources: AppResources = {
  en: {
    'order.title': 'Purchase Order',
    'order.subtitle': 'Orders placed with suppliers for {{unit}}.',
    'order.new': 'New purchase order',
    'order.search': 'Search number, supplier, item, reference…',
    'order.searchLabel': 'Search purchase orders',
    'order.emptyTitle': 'No purchase orders yet',
    'order.emptyText':
      'Order goods from a supplier, or start from an approved requisition. Approved orders can then be received.',
    'order.exportFile': 'purchase-orders',
    'order.notFound': 'This purchase order does not exist.',
    'order.backToList': 'Back to purchase orders',
    'order.form.newTitle': 'New purchase order',
    'order.form.editTitle': 'Edit purchase order',
    'order.form.save': 'Save purchase order',
    'order.form.saved': 'Purchase order {{code}} saved',
    'order.lines.description':
      'What to buy, at what price. VAT comes from the item and can be changed.',
    'order.lines.specification': 'Specification',
    'order.lines.specificationPlaceholder': 'Brand, size, colour…',
    'order.meta.ordered': 'Ordered {{date}}',
    'order.meta.expected': 'Expected {{date}}',
  },
  bn: {
    'order.title': 'ক্রয় আদেশ',
    'order.subtitle': '{{unit}}-এর জন্য সরবরাহকারীকে দেওয়া আদেশ।',
    'order.new': 'নতুন ক্রয় আদেশ',
    'order.search': 'নম্বর, সরবরাহকারী, আইটেম, রেফারেন্স খুঁজুন…',
    'order.searchLabel': 'ক্রয় আদেশ খুঁজুন',
    'order.emptyTitle': 'এখনও কোনো ক্রয় আদেশ নেই',
    'order.emptyText':
      'সরবরাহকারীর কাছে মাল অর্ডার দিন, অথবা অনুমোদিত চাহিদাপত্র থেকে শুরু করুন। অনুমোদিত আদেশের মাল পরে গ্রহণ করা যাবে।',
    'order.exportFile': 'purchase-orders',
    'order.notFound': 'এই ক্রয় আদেশ নেই।',
    'order.backToList': 'ক্রয় আদেশের তালিকায় ফিরুন',
    'order.form.newTitle': 'নতুন ক্রয় আদেশ',
    'order.form.editTitle': 'ক্রয় আদেশ সম্পাদনা',
    'order.form.save': 'ক্রয় আদেশ সংরক্ষণ',
    'order.form.saved': 'ক্রয় আদেশ {{code}} সংরক্ষিত হয়েছে',
    'order.lines.description': 'কী কিনবেন, কত দামে। ভ্যাট আইটেম থেকে আসে, বদলানো যায়।',
    'order.lines.specification': 'বিবরণ',
    'order.lines.specificationPlaceholder': 'ব্র্যান্ড, মাপ, রং…',
    'order.meta.ordered': 'আদেশের তারিখ {{date}}',
    'order.meta.expected': 'সম্ভাব্য {{date}}',
  },
};
