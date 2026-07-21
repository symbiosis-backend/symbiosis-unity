namespace MahjongGame
{
    public static class StoryChinaContentLibrary
    {
        public const int LevelNumber = 2;
        public const int StageCount = 10;

        private readonly struct LocalizedText
        {
            private readonly string russian;
            private readonly string english;
            private readonly string turkish;
            private readonly string german;

            public LocalizedText(string russian, string english, string turkish, string german)
            {
                this.russian = russian;
                this.english = english;
                this.turkish = turkish;
                this.german = german;
            }

            public string Get(GameLanguage language)
            {
                return language switch
                {
                    GameLanguage.English => english,
                    GameLanguage.Turkish => turkish,
                    GameLanguage.German => german,
                    _ => russian
                };
            }
        }

        private readonly struct StageEntry
        {
            public readonly LocalizedText Title;
            public readonly LocalizedText Description;

            public StageEntry(LocalizedText title, LocalizedText description)
            {
                Title = title;
                Description = description;
            }
        }

        private static readonly LocalizedText LevelTitle = new(
            "Китай",
            "China",
            "Çin",
            "China"
        );

        private static readonly StageEntry[] Stages =
        {
            new(
                new LocalizedText("Кости памяти", "Bones of Memory", "Hafıza Kemikleri", "Knochen der Erinnerung"),
                new LocalizedText(
                    "Факт 1/10. В эпоху Шан вопросы вырезали на костях и панцирях. Эти гадательные кости относятся к самым ранним известным памятникам китайской письменности: власть, ритуал и память уже работали вместе.",
                    "Fact 1/10. In the Shang period, questions were carved on bones and shells. These oracle bones are among the earliest known records of Chinese writing, where rule, ritual, and memory already worked together.",
                    "Bilgi 1/10. Shang döneminde sorular kemiklere ve kabuklara kazınırdı. Bu kehanet kemikleri, Çin yazısının bilinen en erken kayıtları arasındadır; yönetim, ritüel ve hafıza birlikte görünür.",
                    "Fakt 1/10. In der Shang-Zeit wurden Fragen in Knochen und Panzer geritzt. Diese Orakelknochen gehören zu den frühesten bekannten Zeugnissen chinesischer Schrift: Herrschaft, Ritual und Erinnerung wirkten bereits zusammen."
                )
            ),
            new(
                new LocalizedText("Длинная стена", "The Long Wall", "Uzun Duvar", "Die Lange Mauer"),
                new LocalizedText(
                    "Факт 2/10. Великая стена строилась и перестраивалась веками. К эпохе Мин она стала крупнейшей военной оборонительной системой своего времени, и сегодня это объект Всемирного наследия UNESCO.",
                    "Fact 2/10. The Great Wall was built and rebuilt across centuries. By the Ming dynasty it had become the largest military defensive system of its time, and today it is a UNESCO World Heritage Site.",
                    "Bilgi 2/10. Çin Seddi yüzyıllar boyunca yapıldı ve yeniden güçlendirildi. Ming döneminde çağının en büyük askeri savunma sistemi haline geldi; bugün UNESCO Dünya Mirası'dır.",
                    "Fakt 2/10. Die Große Mauer wurde über Jahrhunderte gebaut und erneuert. In der Ming-Dynastie wurde sie zur größten militärischen Verteidigungsanlage ihrer Zeit; heute ist sie UNESCO-Welterbe."
                )
            ),
            new(
                new LocalizedText("Первый император", "The First Emperor", "İlk İmparator", "Der Erste Kaiser"),
                new LocalizedText(
                    "Факт 3/10. Цинь Шихуан объединил Китай в 221 году до н. э. Его мавзолей рядом с Сианем знаменит терракотовыми воинами, а сам археологический комплекс был открыт только в 1974 году.",
                    "Fact 3/10. Qin Shi Huang unified China in 221 BCE. His mausoleum near Xi'an is famous for the terracotta warriors, and the archaeological complex was not discovered until 1974.",
                    "Bilgi 3/10. Qin Shi Huang Çin'i MÖ 221'de birleştirdi. Xi'an yakınındaki mozolesi terrakotta askerleriyle ünlüdür; arkeolojik alan ancak 1974'te keşfedildi.",
                    "Fakt 3/10. Qin Shi Huang vereinte China 221 v. Chr. Sein Mausoleum bei Xi'an ist durch die Terrakotta-Krieger berühmt; die Anlage wurde erst 1974 entdeckt."
                )
            ),
            new(
                new LocalizedText("Шёлковый путь", "The Silk Road", "İpek Yolu", "Die Seidenstraße"),
                new LocalizedText(
                    "Факт 4/10. Шёлковый путь был не одной дорогой, а сетью маршрутов. Он связывал Чанъань с Центральной Азией и дальше со Средиземноморьем, перенося товары, идеи и религии.",
                    "Fact 4/10. The Silk Road was not one road, but a network of routes. It linked Chang'an with Central Asia and the Mediterranean world, carrying goods, ideas, and religions.",
                    "Bilgi 4/10. İpek Yolu tek bir yol değil, yollar ağıydı. Chang'an'ı Orta Asya ve Akdeniz dünyasına bağladı; mallar, fikirler ve dinler bu ağda taşındı.",
                    "Fakt 4/10. Die Seidenstraße war keine einzelne Straße, sondern ein Routennetz. Sie verband Chang'an mit Zentralasien und dem Mittelmeerraum und trug Waren, Ideen und Religionen weiter."
                )
            ),
            new(
                new LocalizedText("Бумага", "Paper", "Kağıt", "Papier"),
                new LocalizedText(
                    "Факт 5/10. Бумага появилась в Китае. Придворный Цай Лунь около 105 года н. э. усовершенствовал способ делать листы из коры, волокон, старой ткани и сетей.",
                    "Fact 5/10. Paper appeared in China. Around 105 CE, the court official Cai Lun improved a process for making sheets from bark, fibers, old cloth, and nets.",
                    "Bilgi 5/10. Kağıt Çin'de ortaya çıktı. Saray görevlisi Cai Lun, MS 105 civarında kabuk, lif, eski kumaş ve ağlardan levha yapma yöntemini geliştirdi.",
                    "Fakt 5/10. Papier entstand in China. Um 105 n. Chr. verbesserte der Hofbeamte Cai Lun ein Verfahren, Blätter aus Rinde, Fasern, alten Stoffen und Netzen herzustellen."
                )
            ),
            new(
                new LocalizedText("Чайная тишина", "Tea Quiet", "Çayın Sessizliği", "Tee und Stille"),
                new LocalizedText(
                    "Факт 6/10. Китайская чайная традиция очень древняя: выращивание, обработка и употребление чая передавались поколениями. В 2022 году традиционные техники чая Китая вошли в список нематериального наследия UNESCO.",
                    "Fact 6/10. China's tea tradition is ancient: cultivation, processing, and drinking practices passed through generations. In 2022, traditional Chinese tea-making techniques were inscribed by UNESCO as intangible cultural heritage.",
                    "Bilgi 6/10. Çin'in çay geleneği çok eskidir: yetiştirme, işleme ve içme alışkanlıkları nesiller boyunca aktarıldı. Geleneksel Çin çay yapım teknikleri 2022'de UNESCO somut olmayan mirasına alındı.",
                    "Fakt 6/10. Chinas Teetradition ist sehr alt: Anbau, Verarbeitung und Trinkkultur wurden über Generationen weitergegeben. 2022 nahm UNESCO traditionelle chinesische Teetechniken in das immaterielle Kulturerbe auf."
                )
            ),
            new(
                new LocalizedText("Живой знак", "The Living Sign", "Yaşayan İşaret", "Das Lebendige Zeichen"),
                new LocalizedText(
                    "Факт 7/10. Китайская каллиграфия считается не просто красивым письмом. UNESCO отмечает её как важный путь понимания традиционной культуры и художественного образования.",
                    "Fact 7/10. Chinese calligraphy is not just beautiful writing. UNESCO describes it as an important way to appreciate traditional culture and support arts education.",
                    "Bilgi 7/10. Çin kaligrafisi sadece güzel yazı değildir. UNESCO onu geleneksel kültürü anlamanın ve sanat eğitimini desteklemenin önemli bir yolu olarak tanımlar.",
                    "Fakt 7/10. Chinesische Kalligrafie ist mehr als schöne Schrift. UNESCO beschreibt sie als wichtigen Weg, traditionelle Kultur zu verstehen und Kunsterziehung zu fördern."
                )
            ),
            new(
                new LocalizedText("Север и юг", "North and South", "Kuzey ve Güney", "Norden und Süden"),
                new LocalizedText(
                    "Факт 8/10. Китайская кухня сильно зависит от региона. На севере важны пшеница, лапша и пельмени; на юге тёплый влажный климат сделал рис главной основой питания.",
                    "Fact 8/10. Chinese cuisine changes strongly by region. In the north, wheat, noodles, and dumplings are important; in the warm, humid south, rice became a primary staple.",
                    "Bilgi 8/10. Çin mutfağı bölgeye göre çok değişir. Kuzeyde buğday, erişte ve mantı önemlidir; sıcak ve nemli güneyde pirinç temel gıda haline gelmiştir.",
                    "Fakt 8/10. Die chinesische Küche unterscheidet sich stark nach Region. Im Norden sind Weizen, Nudeln und Teigtaschen wichtig; im warmen, feuchten Süden wurde Reis zum Grundnahrungsmittel."
                )
            ),
            new(
                new LocalizedText("Великий канал", "The Grand Canal", "Büyük Kanal", "Der Große Kanal"),
                new LocalizedText(
                    "Факт 9/10. Великий канал Китая считается самым длинным и одним из старейших искусственных водных путей мира. Он связывал север и юг и помогал удерживать огромную страну в единой системе.",
                    "Fact 9/10. China's Grand Canal is considered the world's longest and one of its oldest artificial waterways. It linked north and south and helped hold a vast country in one system.",
                    "Bilgi 9/10. Çin Büyük Kanalı, dünyanın en uzun ve en eski yapay su yollarından biri kabul edilir. Kuzey ile güneyi bağladı ve büyük ülkeyi tek sistemde tutmaya yardım etti.",
                    "Fakt 9/10. Der Große Kanal Chinas gilt als längster und einer der ältesten künstlichen Wasserwege der Welt. Er verband Norden und Süden und half, ein riesiges Land in einem System zu halten."
                )
            ),
            new(
                new LocalizedText("Фарфор", "Porcelain", "Porselen", "Porzellan"),
                new LocalizedText(
                    "Факт 10/10. Китайский фарфор стал настолько влиятельным, что само европейское слово porcelain связано с описаниями китайской керамики. Тонкий материал превратил ремесло в язык торговли и вкуса.",
                    "Fact 10/10. Chinese porcelain became so influential that the European word porcelain is tied to descriptions of ceramics seen in China. The fine material turned craft into a language of trade and taste.",
                    "Bilgi 10/10. Çin porseleni o kadar etkili oldu ki Avrupa'daki porcelain sözcüğü Çin'de görülen seramiklerin anlatımıyla bağlantılıdır. İnce malzeme zanaatı ticaret ve zevk diline çevirdi.",
                    "Fakt 10/10. Chinesisches Porzellan wurde so einflussreich, dass das europäische Wort porcelain mit Beschreibungen chinesischer Keramik verbunden ist. Das feine Material machte Handwerk zu einer Sprache von Handel und Geschmack."
                )
            )
        };

        public static bool HasLevel(int levelNumber)
        {
            return levelNumber == LevelNumber;
        }

        public static string GetLevelDisplayName(GameLanguage language)
        {
            return LevelTitle.Get(language);
        }

        public static bool TryCreateStage(int levelNumber, int stageIndex, GameLanguage language, out LevelStageContent content)
        {
            content = null;

            if (!HasLevel(levelNumber) || stageIndex < 1 || stageIndex > Stages.Length)
                return false;

            StageEntry entry = Stages[stageIndex - 1];
            content = new LevelStageContent
            {
                StageIndex = stageIndex,
                LayoutLevel = stageIndex,
                UseCustomLayout = false,
                Title = entry.Title.Get(language),
                Description = entry.Description.Get(language)
            };

            return true;
        }
    }
}
