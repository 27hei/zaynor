// Shared category definitions: translation key + the search each seeds.
// Used by the home page strip and the Categories page (spec FR10/Section 16).
export const CATEGORY_SEEDS = [
  { key: 'electronics', seed: 'laptop' },
  { key: 'gaming', seed: 'PlayStation 5' },
  { key: 'phones', seed: 'iPhone 15' },
  { key: 'computers', seed: 'MacBook' },
  { key: 'tv', seed: 'Samsung TV' },
  { key: 'appliances', seed: 'air fryer' },
] as const

export type CategoryKey = (typeof CATEGORY_SEEDS)[number]['key']
