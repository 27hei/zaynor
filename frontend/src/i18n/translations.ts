export type Lang = 'en' | 'ar'

export const LANGUAGES: { code: Lang; label: string; dir: 'ltr' | 'rtl' }[] = [
  { code: 'en', label: 'English', dir: 'ltr' },
  { code: 'ar', label: 'العربية', dir: 'rtl' },
]

// Flat key → string. Use {name} placeholders; interpolation is handled by `t`.
export const translations: Record<Lang, Record<string, string>> = {
  en: {
    'brand.tagline': 'Smart Shopping Decisions',

    'nav.home': 'Home',
    'nav.categories': 'Categories',
    'nav.howItWorks': 'How It Works',
    'nav.about': 'About',
    'nav.login': 'Log in',
    'nav.register': 'Sign up',
    'nav.account': 'My Account',
    'nav.logout': 'Log out',
    'nav.menu': 'Menu',

    'hero.eyebrow': "Saudi Arabia's smart price comparison",
    'hero.title': 'Compare prices. Buy with confidence.',
    'hero.subtitle':
      'Search once — Zaynor checks every store, finds the lowest price, and tells you where to buy.',
    'hero.searchPlaceholder': 'Search for a product, e.g. Sony PlayStation 5',
    'hero.searchButton': 'Search',
    'hero.neutrality': '100% impartial — every store ranked by price alone, never by commission',
    'hero.popularLabel': 'Try:',
    'hero.recentLabel': 'Recent:',
    'hero.clearRecent': 'Clear',
    'hero.trustLine': 'Comparing offers across',

    'results.searching': 'Searching stores…',
    'results.noResults': 'No offers found for "{query}".',
    'results.heading': '{count} offers for "{query}"',
    'results.showAll': 'Show all {total} offers ({more} more)',
    'results.lowestPrice': 'Lowest price',
    'results.outOfStock': 'Out of stock',
    'results.goToStore': 'Go to store',
    'results.error': 'Search failed: {message}',
    'results.save': 'Save product',
    'results.savedDone': 'Saved ✓',
    'results.notify': 'Notify me if the price drops',
    'results.notifySet': 'Alert set ✓',
    'results.actionError': 'That did not work — please try again.',
    'results.demoData':
      'Sample data — this product is not covered by our price catalog yet, so these are demonstration prices, not market prices.',
    'results.wakeHint':
      'The first request can take up to a minute while the server wakes up — thanks for your patience.',

    'summary.meta': '{count} offers · best price {price}',

    'history.show': 'Show price history',
    'history.hide': 'Hide price history',
    'history.title': 'Price history',
    'history.accumulating':
      'Zaynor is still collecting price history for this product — every search adds to it. Check back soon.',

    'reco.badge': 'Best deal',
    'reco.saveUpTo': 'Save up to {amount}',
    'reco.message': 'Buy from {store} at {price} — save {savings} versus {comparedStore} at {comparedPrice}.',
    'reco.cta': 'Go to best price',

    'offer.freeShipping': 'Free shipping',
    'offer.deliveryNextDay': 'Next-day delivery',
    'offer.delivery': 'Delivery in {days} days',

    'feature.trust.title': 'Trust',
    'feature.trust.text': 'Neutral recommendations — the best deal wins, even without commission.',
    'feature.intelligence.title': 'Intelligence',
    'feature.intelligence.text': 'Offers are matched and organized so you compare the same product.',
    'feature.savings.title': 'Savings',
    'feature.savings.text': 'See exactly how much you save by buying at the lowest price.',
    'feature.discovery.title': 'Discovery',
    'feature.discovery.text': 'One search reaches every store Zaynor tracks, instantly.',
    'feature.analysis.title': 'Analysis',
    'feature.analysis.text': 'Clear, sorted results — no more tab-switching to compare prices.',
    'feature.alerts.title': 'Alerts',
    'feature.alerts.text': 'Track a product and Zaynor watches its price in the background for you.',

    'footer.note': 'Zaynor does not sell products — it helps you find the best price before you buy.',
    'footer.amazonDisclosure': 'As an Amazon Associate, Zaynor earns from qualifying purchases.',
    'footer.product': 'Product',
    'footer.company': 'Company',
    'footer.legal': 'Legal',
    'footer.rights': '© {year} Zaynor. All rights reserved.',

    'auth.loginTitle': 'Welcome back',
    'auth.loginSubtitle': 'Log in to track products and manage your alerts.',
    'auth.registerTitle': 'Create your account',
    'auth.registerSubtitle': 'Save products, set price alerts, and shop smarter.',
    'auth.email': 'Email',
    'auth.password': 'Password',
    'auth.emailPlaceholder': 'you@example.com',
    'auth.passwordPlaceholder': 'At least 8 characters',
    'auth.loginButton': 'Log in',
    'auth.registerButton': 'Create account',
    'auth.noAccount': "Don't have an account?",
    'auth.haveAccount': 'Already have an account?',
    'auth.submitting': 'Please wait…',
    'auth.genericError': 'Something went wrong. Please try again.',

    'account.title': 'My Account',
    'account.welcome': 'Welcome, {email}',
    'account.emailLabel': 'Email',
    'account.memberSince': 'Member since',
    'account.localeLabel': 'Preferred language',
    'account.savedTitle': 'Saved products',
    'account.alertsTitle': 'Price alerts',
    'account.emptySaved': 'No saved products yet — search for a product and press "Save product".',
    'account.emptyAlerts': 'No price alerts yet — search for a product and press "Notify me if the price drops".',
    'account.alertNote':
      'Zaynor checks tracked prices in the background. Email/push delivery arrives with live store feeds.',
    'account.alertActive': 'Active',
    'account.alertTriggered': 'Price dropped to {price}!',
    'account.loadError': 'Could not load your items — please refresh.',
    'account.remove': 'Remove',
    'account.logout': 'Log out',

    'categories.title': 'Browse by category',
    'catalog.from': 'from {price}',
    'categories.subtitle': 'Category browsing is expanding — start with a search for now.',
    'category.electronics': 'Electronics',
    'category.gaming': 'Gaming',
    'category.phones': 'Phones',
    'category.computers': 'Computers',
    'category.tv': 'TV & Audio',
    'category.appliances': 'Home Appliances',

    'about.title': 'About Zaynor',
    'about.p1':
      'Zaynor is an intelligent, impartial shopping assistant. Instead of searching across dozens of stores and comparing prices by hand, you enter a product once — and Zaynor gathers the offers, finds the real lowest price, and recommends the best deal.',
    'about.p2':
      'Zaynor does not sell products. It sells confidence in the buying decision. Our promise: never buy until you are sure you got the best price and the best value.',
    'about.p3':
      'The name blends the Arabic roots zayn (the finest) and noor (light, clarity): the best choice, made clear.',

    'how.title': 'How It Works',
    'how.step1.title': 'Search',
    'how.step1.text': 'Enter a product name. Zaynor queries every store it tracks, live.',
    'how.step2.title': 'Compare',
    'how.step2.text': 'Offers are gathered, matched, and sorted from lowest price to highest.',
    'how.step3.title': 'Decide',
    'how.step3.text': 'A clear recommendation shows the best deal and how much you save.',
    'how.step4.title': 'Buy',
    'how.step4.text': 'Go straight to the store with the best price and complete your purchase.',
    'how.moneyTitle': 'How do we make money?',
    'how.moneyText':
      'When you buy through a Zaynor link, the store may pay us a small commission — at no extra cost to you. Commissions never affect our ranking: the lowest price wins, always, even when a store pays us nothing.',

    'legal.updated': 'Last updated: 18 July 2026',

    'privacy.title': 'Privacy Policy',
    'privacy.intro':
      'Zaynor is a price-comparison platform. We collect as little personal data as possible, and this policy explains exactly what we collect, why, and your rights over it.',
    'privacy.collect.title': 'What we collect',
    'privacy.collect.text':
      'If you create an account: your email address, a securely hashed password (never stored in plain text), and your preferred language. If you use saved products or price alerts: the products you chose to track. Search queries are processed to serve results and improve product matching.',
    'privacy.use.title': 'How we use it',
    'privacy.use.text':
      'To operate your account, keep your saved products and alerts, monitor tracked prices in the background, and improve the service. We do not sell your personal data, and we do not use it for third-party advertising.',
    'privacy.local.title': 'Data stored on your device',
    'privacy.local.text':
      'Your browser stores a session token (to keep you signed in), your language choice, and your recent searches. Clearing your browser storage removes them; recent searches never leave your device.',
    'privacy.affiliate.title': 'Affiliate links',
    'privacy.affiliate.text':
      'Outbound "Go to store" links may carry affiliate tracking, and the store may pay Zaynor a commission at no extra cost to you. Commissions never affect our ranking: the lowest price wins, always.',
    'privacy.sharing.title': 'Sharing',
    'privacy.sharing.text':
      'We do not share your personal data with third parties, except where required by law. When you click through to a store, that store’s own privacy policy applies to what you do there.',
    'privacy.security.title': 'Security',
    'privacy.security.text':
      'Passwords are hashed with BCrypt, sessions use signed tokens, and the API applies rate limiting. No system is perfectly secure, but we design for data protection from the start.',
    'privacy.rights.title': 'Your rights and contact',
    'privacy.rights.text':
      'You can delete your saved products and alerts from your account at any time. To request account deletion or ask any privacy question, contact us at abdluazez796@gmail.com.',
    'privacy.changes.title': 'Changes to this policy',
    'privacy.changes.text':
      'We will update this page when the policy changes and revise the date above. Substantial changes will be announced on the site.',

    'terms.title': 'Terms of Use',
    'terms.intro':
      'By using Zaynor you agree to these terms. If you do not agree, please do not use the service.',
    'terms.service.title': 'What Zaynor is',
    'terms.service.text':
      'Zaynor compares product prices across third-party stores and links you to them. Zaynor does not sell products, process payments, or fulfil orders — your purchase contract is always with the store.',
    'terms.prices.title': 'Prices and availability',
    'terms.prices.text':
      'Prices, stock, and shipping information come from external sources and can change or contain errors. Always verify the final price at the store before completing a purchase.',
    'terms.affiliate.title': 'How Zaynor earns',
    'terms.affiliate.text':
      'Some outbound links are affiliate links that may earn Zaynor a commission at no extra cost to you. Commissions never influence ranking or recommendations.',
    'terms.accounts.title': 'Accounts',
    'terms.accounts.text':
      'You are responsible for the accuracy of your account details and for keeping your password confidential. We may suspend accounts used abusively or unlawfully.',
    'terms.use.title': 'Acceptable use',
    'terms.use.text':
      'Do not disrupt the service, attempt unauthorized access, scrape at abusive volumes, or use Zaynor for any unlawful purpose.',
    'terms.liability.title': 'Liability',
    'terms.liability.text':
      'Zaynor is provided "as is". To the maximum extent permitted by law, we are not liable for losses arising from purchase decisions, store conduct, or price inaccuracies originating from external sources.',
    'terms.changes.title': 'Changes to these terms',
    'terms.changes.text':
      'We may update these terms; the date above reflects the latest version. Continued use after changes means you accept them.',
    'terms.law.title': 'Governing law',
    'terms.law.text':
      'These terms are governed by the laws of the Kingdom of Saudi Arabia.',

    'notFound.title': 'Page not found',
    'notFound.text': "The page you're looking for doesn't exist.",
    'notFound.home': 'Back to home',
  },

  ar: {
    'brand.tagline': 'قرارات تسوّق ذكية',

    'nav.home': 'الرئيسية',
    'nav.categories': 'الفئات',
    'nav.howItWorks': 'كيف يعمل',
    'nav.about': 'عن زينور',
    'nav.login': 'تسجيل الدخول',
    'nav.register': 'إنشاء حساب',
    'nav.account': 'حسابي',
    'nav.logout': 'تسجيل الخروج',
    'nav.menu': 'القائمة',

    'hero.eyebrow': 'مقارنة الأسعار الذكية في السعودية',
    'hero.title': 'قارن الأسعار. اشترِ بثقة.',
    'hero.subtitle':
      'ابحث مرة واحدة — يفحص زينور كل المتاجر، يجد أقل سعر، ويخبرك من أين تشتري.',
    'hero.searchPlaceholder': 'ابحث عن منتج، مثل سوني بلايستيشن ٥',
    'hero.searchButton': 'بحث',
    'hero.neutrality': 'محايد ١٠٠٪ — كل متجر يُرتَّب حسب السعر فقط، لا حسب العمولة',
    'hero.popularLabel': 'جرّب:',
    'hero.recentLabel': 'بحثت مؤخرًا:',
    'hero.clearRecent': 'مسح',
    'hero.trustLine': 'نقارن العروض عبر',

    'results.searching': 'جارٍ البحث في المتاجر…',
    'results.noResults': 'لا توجد عروض لـ "{query}".',
    'results.heading': '{count} عرض لـ "{query}"',
    'results.showAll': 'عرض كل العروض ({total}) ({more} إضافية)',
    'results.lowestPrice': 'أقل سعر',
    'results.outOfStock': 'غير متوفر',
    'results.goToStore': 'اذهب للمتجر',
    'results.error': 'فشل البحث: {message}',
    'results.save': 'حفظ المنتج',
    'results.savedDone': 'تم الحفظ ✓',
    'results.notify': 'نبّهني إذا انخفض السعر',
    'results.notifySet': 'تم ضبط التنبيه ✓',
    'results.actionError': 'لم تنجح العملية — حاول مرة أخرى.',
    'results.demoData':
      'بيانات تجريبية — هذا المنتج غير مغطى في كتالوج الأسعار بعد، فهذه أسعار توضيحية وليست أسعار السوق.',
    'results.wakeHint':
      'قد يستغرق أول طلب حتى دقيقة ريثما يستيقظ الخادم — شكرًا لصبرك.',

    'summary.meta': '{count} عروض · أفضل سعر {price}',

    'history.show': 'عرض تاريخ السعر',
    'history.hide': 'إخفاء تاريخ السعر',
    'history.title': 'تاريخ السعر',
    'history.accumulating':
      'ما زال زينور يجمع تاريخ الأسعار لهذا المنتج — كل عملية بحث تضيف إليه. عد قريبًا.',

    'reco.badge': 'أفضل عرض',
    'reco.saveUpTo': 'وفّر حتى {amount}',
    'reco.message': 'اشترِ من {store} بسعر {price} — ووفّر {savings} مقارنةً بـ {comparedStore} بسعر {comparedPrice}.',
    'reco.cta': 'اذهب لأفضل سعر',

    'offer.freeShipping': 'شحن مجاني',
    'offer.deliveryNextDay': 'توصيل في اليوم التالي',
    'offer.delivery': 'التوصيل خلال {days} أيام',

    'feature.trust.title': 'ثقة',
    'feature.trust.text': 'توصيات محايدة — الأفضل يفوز، حتى بدون عمولة.',
    'feature.intelligence.title': 'ذكاء',
    'feature.intelligence.text': 'تُطابَق العروض وتُنظَّم لتقارن المنتج نفسه.',
    'feature.savings.title': 'توفير',
    'feature.savings.text': 'شاهد كم توفّر بالضبط عند الشراء بأقل سعر.',
    'feature.discovery.title': 'اكتشاف',
    'feature.discovery.text': 'بحث واحد يصل إلى كل متجر يتابعه زينور، فورًا.',
    'feature.analysis.title': 'تحليل',
    'feature.analysis.text': 'نتائج واضحة ومرتّبة — دون التنقّل بين النوافذ.',
    'feature.alerts.title': 'تنبيهات',
    'feature.alerts.text': 'تابع منتجًا ويراقب زينور سعره في الخلفية نيابة عنك.',

    'footer.note': 'زينور لا يبيع المنتجات — بل يساعدك على إيجاد أفضل سعر قبل الشراء.',
    'footer.amazonDisclosure': 'بصفتنا شريكًا في برنامج Amazon Associates، يربح زينور من عمليات الشراء المؤهلة.',
    'footer.product': 'المنتج',
    'footer.company': 'الشركة',
    'footer.legal': 'قانوني',
    'footer.rights': '© {year} زينور. جميع الحقوق محفوظة.',

    'auth.loginTitle': 'مرحبًا بعودتك',
    'auth.loginSubtitle': 'سجّل الدخول لمتابعة المنتجات وإدارة تنبيهاتك.',
    'auth.registerTitle': 'أنشئ حسابك',
    'auth.registerSubtitle': 'احفظ المنتجات، اضبط تنبيهات الأسعار، وتسوّق بذكاء.',
    'auth.email': 'البريد الإلكتروني',
    'auth.password': 'كلمة المرور',
    'auth.emailPlaceholder': 'you@example.com',
    'auth.passwordPlaceholder': '٨ أحرف على الأقل',
    'auth.loginButton': 'تسجيل الدخول',
    'auth.registerButton': 'إنشاء الحساب',
    'auth.noAccount': 'ليس لديك حساب؟',
    'auth.haveAccount': 'لديك حساب بالفعل؟',
    'auth.submitting': 'يرجى الانتظار…',
    'auth.genericError': 'حدث خطأ ما. حاول مرة أخرى.',

    'account.title': 'حسابي',
    'account.welcome': 'مرحبًا، {email}',
    'account.emailLabel': 'البريد الإلكتروني',
    'account.memberSince': 'عضو منذ',
    'account.localeLabel': 'اللغة المفضّلة',
    'account.savedTitle': 'المنتجات المحفوظة',
    'account.alertsTitle': 'تنبيهات الأسعار',
    'account.emptySaved': 'لا منتجات محفوظة بعد — ابحث عن منتج واضغط «حفظ المنتج».',
    'account.emptyAlerts': 'لا تنبيهات أسعار بعد — ابحث عن منتج واضغط «نبّهني إذا انخفض السعر».',
    'account.alertNote':
      'يفحص زينور الأسعار المتتبَّعة في الخلفية. إشعارات البريد/الجوال تصل مع المصادر الحية.',
    'account.alertActive': 'نشط',
    'account.alertTriggered': 'انخفض السعر إلى {price}!',
    'account.loadError': 'تعذّر تحميل عناصرك — حدّث الصفحة.',
    'account.remove': 'إزالة',
    'account.logout': 'تسجيل الخروج',

    'categories.title': 'تصفّح حسب الفئة',
    'catalog.from': 'يبدأ من {price}',
    'categories.subtitle': 'تصفّح الفئات في توسّع مستمر — ابدأ بالبحث الآن.',
    'category.electronics': 'إلكترونيات',
    'category.gaming': 'ألعاب',
    'category.phones': 'هواتف',
    'category.computers': 'حواسيب',
    'category.tv': 'تلفزيونات وصوتيات',
    'category.appliances': 'أجهزة منزلية',

    'about.title': 'عن زينور',
    'about.p1':
      'زينور مساعد تسوّق ذكي ومحايد. بدلًا من البحث في عشرات المتاجر ومقارنة الأسعار يدويًا، تُدخل المنتج مرة واحدة — فيجمع زينور العروض، ويجد أقل سعر حقيقي، ويوصي بأفضل صفقة.',
    'about.p2':
      'زينور لا يبيع المنتجات، بل يبيع الثقة في قرار الشراء. وعدنا: لا تشترِ حتى تتأكد أنك حصلت على أفضل سعر وأفضل قيمة.',
    'about.p3':
      'الاسم يمزج الجذرين العربيين «زين» (الأفضل) و«نور» (الوضوح): الخيار الأفضل، بوضوح.',

    'how.title': 'كيف يعمل',
    'how.step1.title': 'ابحث',
    'how.step1.text': 'أدخل اسم المنتج. يستعلم زينور من كل متجر يتابعه، مباشرة.',
    'how.step2.title': 'قارن',
    'how.step2.text': 'تُجمع العروض وتُطابق وتُرتّب من الأقل سعرًا إلى الأعلى.',
    'how.step3.title': 'قرّر',
    'how.step3.text': 'توصية واضحة تُظهر أفضل صفقة وكم توفّر.',
    'how.step4.title': 'اشترِ',
    'how.step4.text': 'اذهب مباشرة إلى المتجر صاحب أفضل سعر وأكمل شراءك.',
    'how.moneyTitle': 'كيف نربح؟',
    'how.moneyText':
      'عندما تشتري عبر رابط زينور، قد يدفع لنا المتجر عمولة صغيرة — دون أي تكلفة إضافية عليك. العمولة لا تؤثر أبدًا على الترتيب: أقل سعر يفوز دائمًا، حتى لو لم يدفع لنا المتجر شيئًا.',

    'legal.updated': 'آخر تحديث: ١٨ يوليو ٢٠٢٦',

    'privacy.title': 'سياسة الخصوصية',
    'privacy.intro':
      'زينور منصة لمقارنة الأسعار. نجمع أقل قدر ممكن من البيانات الشخصية، وتوضح هذه السياسة بدقة ما نجمعه ولماذا، وحقوقك عليه.',
    'privacy.collect.title': 'ما الذي نجمعه',
    'privacy.collect.text':
      'إذا أنشأت حسابًا: بريدك الإلكتروني، وكلمة مرور مشفّرة بأمان (لا تُخزَّن نصًا صريحًا أبدًا)، ولغتك المفضلة. وإذا استخدمت الحفظ أو التنبيهات: المنتجات التي اخترت متابعتها. وتُعالَج استعلامات البحث لعرض النتائج وتحسين مطابقة المنتجات.',
    'privacy.use.title': 'كيف نستخدمها',
    'privacy.use.text':
      'لتشغيل حسابك، وحفظ منتجاتك وتنبيهاتك، ومراقبة الأسعار المتتبَّعة في الخلفية، وتحسين الخدمة. لا نبيع بياناتك الشخصية، ولا نستخدمها لإعلانات طرف ثالث.',
    'privacy.local.title': 'بيانات مخزنة على جهازك',
    'privacy.local.text':
      'يخزّن متصفحك رمز الجلسة (لبقائك مسجَّلًا)، واختيار اللغة، وعمليات بحثك الأخيرة. مسح تخزين المتصفح يزيلها؛ وعمليات البحث الأخيرة لا تغادر جهازك أبدًا.',
    'privacy.affiliate.title': 'روابط العمولة',
    'privacy.affiliate.text':
      'قد تحمل روابط «اذهب للمتجر» تتبّعًا للعمولة، وقد يدفع المتجر لزينور عمولة دون أي تكلفة إضافية عليك. العمولة لا تؤثر أبدًا على الترتيب: أقل سعر يفوز دائمًا.',
    'privacy.sharing.title': 'المشاركة',
    'privacy.sharing.text':
      'لا نشارك بياناتك الشخصية مع أطراف ثالثة إلا حيث يلزم القانون. وعند انتقالك إلى متجر، تسري سياسة خصوصية ذلك المتجر على ما تفعله هناك.',
    'privacy.security.title': 'الأمان',
    'privacy.security.text':
      'كلمات المرور مشفّرة بخوارزمية BCrypt، والجلسات برموز موقَّعة، والواجهة البرمجية محمية بحدّ معدل الطلبات. لا يوجد نظام آمن تمامًا، لكننا نصمم لحماية البيانات من البداية.',
    'privacy.rights.title': 'حقوقك والتواصل',
    'privacy.rights.text':
      'يمكنك حذف منتجاتك المحفوظة وتنبيهاتك من حسابك في أي وقت. لطلب حذف الحساب أو لأي سؤال عن الخصوصية، راسلنا على abdluazez796@gmail.com.',
    'privacy.changes.title': 'تغييرات هذه السياسة',
    'privacy.changes.text':
      'سنحدّث هذه الصفحة عند تغيّر السياسة مع تعديل التاريخ أعلاه، وسنعلن عن التغييرات الجوهرية في الموقع.',

    'terms.title': 'شروط الاستخدام',
    'terms.intro': 'باستخدامك زينور فأنت توافق على هذه الشروط. إن لم توافق، فلا تستخدم الخدمة.',
    'terms.service.title': 'ما هو زينور',
    'terms.service.text':
      'يقارن زينور أسعار المنتجات عبر متاجر خارجية ويوصلك إليها. زينور لا يبيع المنتجات ولا يعالج المدفوعات ولا ينفّذ الطلبات — عقد شرائك دائمًا مع المتجر.',
    'terms.prices.title': 'الأسعار والتوفر',
    'terms.prices.text':
      'الأسعار والمخزون ومعلومات الشحن تأتي من مصادر خارجية وقد تتغير أو تتضمن أخطاء. تحقق دائمًا من السعر النهائي في المتجر قبل إتمام الشراء.',
    'terms.affiliate.title': 'كيف يربح زينور',
    'terms.affiliate.text':
      'بعض الروابط الخارجية روابط عمولة قد تُكسب زينور عمولة دون تكلفة إضافية عليك. العمولة لا تؤثر أبدًا على الترتيب أو التوصيات.',
    'terms.accounts.title': 'الحسابات',
    'terms.accounts.text':
      'أنت مسؤول عن صحة بيانات حسابك وعن سرية كلمة مرورك. ويجوز لنا إيقاف الحسابات المستخدمة بشكل مسيء أو مخالف للقانون.',
    'terms.use.title': 'الاستخدام المقبول',
    'terms.use.text':
      'لا تعطّل الخدمة، ولا تحاول وصولًا غير مصرّح به، ولا تجمع البيانات بأحجام مسيئة، ولا تستخدم زينور لأي غرض غير قانوني.',
    'terms.liability.title': 'المسؤولية',
    'terms.liability.text':
      'يُقدَّم زينور «كما هو». وإلى الحد الأقصى الذي يسمح به القانون، لسنا مسؤولين عن خسائر ناتجة عن قرارات الشراء أو سلوك المتاجر أو أخطاء أسعار مصدرها جهات خارجية.',
    'terms.changes.title': 'تغييرات هذه الشروط',
    'terms.changes.text':
      'قد نحدّث هذه الشروط، ويعكس التاريخ أعلاه أحدث نسخة. استمرارك في الاستخدام بعد التغييرات يعني قبولك لها.',
    'terms.law.title': 'القانون الواجب التطبيق',
    'terms.law.text': 'تخضع هذه الشروط لأنظمة المملكة العربية السعودية.',

    'notFound.title': 'الصفحة غير موجودة',
    'notFound.text': 'الصفحة التي تبحث عنها غير موجودة.',
    'notFound.home': 'العودة للرئيسية',
  },
}
