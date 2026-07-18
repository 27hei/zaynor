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

    'hero.eyebrow': "Saudi Arabia's smart price comparison",
    'hero.title': 'Compare prices. Buy with confidence.',
    'hero.subtitle':
      'Search once — Zaynor checks every store, finds the lowest price, and tells you where to buy.',
    'hero.searchPlaceholder': 'Search for a product, e.g. Sony PlayStation 5',
    'hero.searchButton': 'Search',
    'hero.neutrality': '100% impartial — every store ranked by price alone, never by commission',
    'hero.popularLabel': 'Try:',
    'hero.trustLine': 'Comparing offers across',

    'results.searching': 'Searching stores…',
    'results.noResults': 'No offers found for "{query}".',
    'results.heading': '{count} offers for "{query}"',
    'results.showAll': 'Show all {total} offers ({more} more)',
    'results.lowestPrice': 'Lowest price',
    'results.outOfStock': 'Out of stock',
    'results.goToStore': 'Go to store',
    'results.error': 'Search failed: {message}',

    'reco.badge': 'Best deal',
    'reco.saveUpTo': 'Save up to {amount}',
    'reco.message': 'Buy from {store} at {price} — save {savings} versus {comparedStore} at {comparedPrice}.',

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
    'feature.alerts.text': 'Price-drop notifications are coming — track a product, get notified.',

    'footer.note': 'Zaynor does not sell products — it helps you find the best price before you buy.',
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
    'account.comingSoon': 'Coming soon',
    'account.comingSoonText': 'This feature arrives in the expansion phase — your account is ready for it.',
    'account.logout': 'Log out',

    'categories.title': 'Browse by category',
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

    'privacy.title': 'Privacy Policy',
    'privacy.p1':
      'This is a placeholder privacy policy for the development version of Zaynor. It will be replaced with a complete policy before public launch and affiliate-network applications.',
    'privacy.p2':
      'When you create an account, we store your email and a securely hashed password. We never store your password in plain text, and we do not sell your personal data.',
    'privacy.p3':
      'Outbound links to stores may carry affiliate tracking. This never changes which store we recommend — the lowest price always wins.',

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

    'hero.eyebrow': 'مقارنة الأسعار الذكية في السعودية',
    'hero.title': 'قارن الأسعار. اشترِ بثقة.',
    'hero.subtitle':
      'ابحث مرة واحدة — يفحص زينور كل المتاجر، يجد أقل سعر، ويخبرك من أين تشتري.',
    'hero.searchPlaceholder': 'ابحث عن منتج، مثل سوني بلايستيشن ٥',
    'hero.searchButton': 'بحث',
    'hero.neutrality': 'محايد ١٠٠٪ — كل متجر يُرتَّب حسب السعر فقط، لا حسب العمولة',
    'hero.popularLabel': 'جرّب:',
    'hero.trustLine': 'نقارن العروض عبر',

    'results.searching': 'جارٍ البحث في المتاجر…',
    'results.noResults': 'لا توجد عروض لـ "{query}".',
    'results.heading': '{count} عرض لـ "{query}"',
    'results.showAll': 'عرض كل العروض ({total}) ({more} إضافية)',
    'results.lowestPrice': 'أقل سعر',
    'results.outOfStock': 'غير متوفر',
    'results.goToStore': 'اذهب للمتجر',
    'results.error': 'فشل البحث: {message}',

    'reco.badge': 'أفضل عرض',
    'reco.saveUpTo': 'وفّر حتى {amount}',
    'reco.message': 'اشترِ من {store} بسعر {price} — ووفّر {savings} مقارنةً بـ {comparedStore} بسعر {comparedPrice}.',

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
    'feature.alerts.text': 'تنبيهات انخفاض السعر قادمة — تابع منتجًا واستلم الإشعار.',

    'footer.note': 'زينور لا يبيع المنتجات — بل يساعدك على إيجاد أفضل سعر قبل الشراء.',
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
    'account.comingSoon': 'قريبًا',
    'account.comingSoonText': 'تصل هذه الميزة في مرحلة التوسّع — حسابك جاهز لها.',
    'account.logout': 'تسجيل الخروج',

    'categories.title': 'تصفّح حسب الفئة',
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

    'privacy.title': 'سياسة الخصوصية',
    'privacy.p1':
      'هذه سياسة خصوصية مبدئية لنسخة التطوير من زينور. ستُستبدل بسياسة كاملة قبل الإطلاق العام وطلبات شبكات التسويق بالعمولة.',
    'privacy.p2':
      'عند إنشاء حساب، نخزّن بريدك الإلكتروني وكلمة مرور مُشفّرة بأمان. لا نخزّن كلمة المرور كنص صريح أبدًا، ولا نبيع بياناتك الشخصية.',
    'privacy.p3':
      'قد تحمل الروابط الخارجية للمتاجر تتبّعًا للعمولة. هذا لا يغيّر أبدًا المتجر الذي نوصي به — أقل سعر يفوز دائمًا.',

    'notFound.title': 'الصفحة غير موجودة',
    'notFound.text': 'الصفحة التي تبحث عنها غير موجودة.',
    'notFound.home': 'العودة للرئيسية',
  },
}
