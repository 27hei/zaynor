// Derives a brand-styled product illustration from the query/title. This is a
// visual placeholder only: when a real offer carries imageUrl (from a live
// feed), that image wins — see usages (offer.imageUrl ?? productArtFor(...)).

const ART: { art: string; keywords: string[] }[] = [
  {
    art: '/product-art/console.svg',
    keywords: ['playstation', 'ps5', 'ps4', 'xbox', 'switch', 'console', 'بلايستيشن', 'اكس بوكس'],
  },
  {
    art: '/product-art/phone.svg',
    keywords: ['iphone', 'galaxy', 'pixel', 'phone', 'ايفون', 'آيفون', 'جالكسي', 'هاتف', 'جوال'],
  },
  {
    art: '/product-art/laptop.svg',
    keywords: ['laptop', 'macbook', 'notebook', 'thinkpad', 'لابتوب', 'حاسوب', 'ماك بوك'],
  },
  {
    art: '/product-art/tv.svg',
    keywords: ['tv', 'television', 'monitor', 'شاشة', 'تلفزيون', 'تلفاز'],
  },
  {
    art: '/product-art/audio.svg',
    keywords: ['airpods', 'headphone', 'earbud', 'speaker', 'سماعة', 'سماعات', 'ايربودز'],
  },
]

const DEFAULT_ART = '/product-art/box.svg'

/** Returns the illustration path best matching a product title/query. */
export function productArtFor(title: string): string {
  const lowered = title.toLowerCase()
  for (const { art, keywords } of ART) {
    if (keywords.some((k) => lowered.includes(k))) {
      return art
    }
  }
  return DEFAULT_ART
}
