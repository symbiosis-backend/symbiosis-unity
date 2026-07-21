namespace MahjongGame
{
    public enum StoryThemeBranch
    {
        Countries = 0,
        Cosmos = 1,
        Nature = 2,
        Human = 3
    }

    public static class StoryThemedContentLibrary
    {
        public readonly struct LocalizedText
        {
            private readonly string russian;
            private readonly string english;
            private readonly string turkish;
            private readonly string german;

            public LocalizedText(string russian, string english, string turkish = null, string german = null)
            {
                this.russian = russian;
                this.english = english;
                this.turkish = string.IsNullOrWhiteSpace(turkish) ? english : turkish;
                this.german = string.IsNullOrWhiteSpace(german) ? english : german;
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

        public readonly struct StageEntry
        {
            public readonly LocalizedText Title;
            public readonly LocalizedText Description;

            public StageEntry(LocalizedText title, LocalizedText description)
            {
                Title = title;
                Description = description;
            }
        }

        public sealed class StoryLevelDefinition
        {
            public readonly int LevelNumber;
            public readonly StoryThemeBranch Branch;
            public readonly string LevelId;
            public readonly string ResourceFolder;
            public readonly string TilePrefix;
            public readonly int TileCount;
            private readonly LocalizedText displayName;
            private readonly LocalizedText subtitle;
            private readonly StageEntry[] stages;

            public int StageCount => stages != null ? stages.Length : 0;

            public StoryLevelDefinition(
                int levelNumber,
                StoryThemeBranch branch,
                string levelId,
                string resourceFolder,
                string tilePrefix,
                int tileCount,
                LocalizedText displayName,
                LocalizedText subtitle,
                StageEntry[] stages)
            {
                LevelNumber = levelNumber;
                Branch = branch;
                LevelId = levelId;
                ResourceFolder = resourceFolder;
                TilePrefix = tilePrefix;
                TileCount = tileCount;
                this.displayName = displayName;
                this.subtitle = subtitle;
                this.stages = stages;
            }

            public string GetDisplayName(GameLanguage language)
            {
                return displayName.Get(language);
            }

            public string GetSubtitle(GameLanguage language)
            {
                return subtitle.Get(language);
            }

            public bool TryCreateStage(int stageIndex, GameLanguage language, out LevelStageContent content)
            {
                content = null;

                if (stageIndex < 1 || stages == null || stageIndex > stages.Length)
                    return false;

                StageEntry entry = stages[stageIndex - 1];
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

        private static readonly StoryLevelDefinition[] Levels =
        {
            new(
                3,
                StoryThemeBranch.Countries,
                "turkey",
                "StoryTurkey",
                "turkey",
                35,
                new LocalizedText("Турция", "Turkey", "Türkiye", "Türkei"),
                new LocalizedText("Анатолия, города, море, ремесло и культурная память.", "Anatolia, cities, seas, craft, and cultural memory.", "Anadolu, şehirler, denizler, zanaat ve kültürel hafıza.", "Anatolien, Städte, Meere, Handwerk und kulturelles Gedächtnis."),
                new[]
                {
                    Stage("Анатолийский мост", "Anatolian Bridge", "Факт 1/10. Анатолия соединяет Европу и Азию. Через неё веками проходили армии, караваны, языки и ремесла, поэтому Турция стала живым перекрёстком культур.", "Fact 1/10. Anatolia links Europe and Asia. Armies, caravans, languages, and crafts moved through it for centuries, making Turkey a living crossroads of cultures."),
                    Stage("Троя и слои времени", "Troy and Layers", "Факт 2/10. Археологическая Троя в северо-западной Анатолии состоит из многих слоёв поселений. Это напоминает, что история часто лежит не линией, а пластами.", "Fact 2/10. Archaeological Troy in northwestern Anatolia contains many settlement layers. History often lies not as one line, but as stacked layers."),
                    Stage("Византийская столица", "Byzantine Capital", "Факт 3/10. Константинополь был столицей Восточной Римской империи больше тысячи лет. Его стены, гавани и храмы сделали город центром Средиземноморья.", "Fact 3/10. Constantinople was the capital of the Eastern Roman Empire for more than a thousand years. Its walls, harbors, and churches shaped a Mediterranean center."),
                    Stage("Османский горизонт", "Ottoman Horizon", "Факт 4/10. Османская империя выросла из анатолийского бейлика и стала державой на трёх континентах. Её наследие видно в архитектуре, кухне и городском устройстве.", "Fact 4/10. The Ottoman Empire grew from an Anatolian beylik into a power across three continents. Its legacy remains in architecture, cuisine, and city life."),
                    Stage("Стамбул и пролив", "Istanbul and the Strait", "Факт 5/10. Босфор делит Стамбул между Европой и Азией. Пролив был важнейшим морским проходом между Чёрным и Средиземным морями.", "Fact 5/10. The Bosporus divides Istanbul between Europe and Asia. The strait has long been a vital sea route between the Black Sea and the Mediterranean."),
                    Stage("Каппадокия", "Cappadocia", "Факт 6/10. Каппадокия известна мягким вулканическим туфом, скальными жилищами и подземными городами. Ландшафт стал одновременно природным и культурным архивом.", "Fact 6/10. Cappadocia is known for soft volcanic tuff, rock-cut dwellings, and underground cities. Its landscape became both a natural and cultural archive."),
                    Stage("Гобекли-Тепе", "Gobekli Tepe", "Факт 7/10. Гобекли-Тепе на юго-востоке Турции относится к самым ранним известным монументальным святилищам. Он изменил представления о неолитических обществах.", "Fact 7/10. Gobekli Tepe in southeastern Turkey is among the earliest known monumental ritual sites. It changed how scholars think about Neolithic societies."),
                    Stage("Чай и разговор", "Tea and Conversation", "Факт 8/10. Турецкий чай стал повседневным ритуалом общения. Его обычно заваривают крепко и подают в маленьком стеклянном стакане.", "Fact 8/10. Turkish tea became an everyday ritual of conversation. It is usually brewed strong and served in a small tulip-shaped glass."),
                    Stage("Ковёр как карта", "Carpet as a Map", "Факт 9/10. Анатолийские ковры несут геометрические мотивы, цвета и местные традиции. Узоры часто работают как память семьи, региона и ремесла.", "Fact 9/10. Anatolian carpets carry geometric motifs, colors, and local traditions. Their patterns can preserve family, regional, and craft memory."),
                    Stage("Республика", "Republic", "Факт 10/10. Турецкая Республика была провозглашена 29 октября 1923 года. Новая государственная система изменила образование, право, письмо и общественную жизнь.", "Fact 10/10. The Republic of Turkey was proclaimed on October 29, 1923. The new state system changed education, law, writing, and public life.")
                }),
            new(
                4,
                StoryThemeBranch.Cosmos,
                "space",
                "StorySpace",
                "space",
                32,
                new LocalizedText("Космос", "Space", "Uzay", "Weltraum"),
                new LocalizedText("Планеты, звёзды, орбиты, телескопы и исследование Вселенной.", "Planets, stars, orbits, telescopes, and exploration of the universe.", "Gezegenler, yıldızlar, yörüngeler, teleskoplar ve evrenin keşfi.", "Planeten, Sterne, Umlaufbahnen, Teleskope und Erforschung des Universums."),
                new[]
                {
                    Stage("Гравитация", "Gravity", "Факт 1/10. Гравитация удерживает планеты на орбитах и собирает вещество в звёзды, луны и галактики. Без неё космос не имел бы знакомой структуры.", "Fact 1/10. Gravity holds planets in orbit and gathers matter into stars, moons, and galaxies. Without it, space would not have its familiar structure."),
                    Stage("Солнечная система", "Solar System", "Факт 2/10. В Солнечной системе восемь планет. Внутренние планеты каменные, а внешние включают газовые и ледяные гиганты.", "Fact 2/10. The Solar System has eight planets. The inner planets are rocky, while the outer worlds include gas and ice giants."),
                    Stage("Луна", "The Moon", "Факт 3/10. Луна стабилизирует наклон земной оси и создаёт приливы вместе с Солнцем. Её поверхность хранит следы ударов миллиарды лет.", "Fact 3/10. The Moon helps stabilize Earth's axial tilt and drives tides together with the Sun. Its surface preserves impact records for billions of years."),
                    Stage("Марс", "Mars", "Факт 4/10. На Марсе есть полярные шапки, высохшие русла и крупнейший вулкан Солнечной системы - Олимп. Планета важна для поиска следов древней воды.", "Fact 4/10. Mars has polar caps, dry channels, and Olympus Mons, the Solar System's largest volcano. It matters in the search for ancient water."),
                    Stage("Пояс астероидов", "Asteroid Belt", "Факт 5/10. Между Марсом и Юпитером лежит пояс астероидов. Это не плотная стена камней, а огромная область с большим расстоянием между телами.", "Fact 5/10. The asteroid belt lies between Mars and Jupiter. It is not a dense wall of rocks, but a vast region with large gaps between bodies."),
                    Stage("Кометы", "Comets", "Факт 6/10. Кометы состоят из льда, пыли и камня. Когда они приближаются к Солнцу, лёд испаряется и образует кому и хвост.", "Fact 6/10. Comets contain ice, dust, and rock. As they approach the Sun, ice vaporizes and forms a coma and tail."),
                    Stage("Звёздный свет", "Starlight", "Факт 7/10. Свет от далёких звёзд идёт к нам годы, тысячи или миллионы лет. Наблюдать космос значит видеть прошлое.", "Fact 7/10. Light from distant stars travels for years, thousands, or millions of years. Looking into space means looking into the past."),
                    Stage("Телескопы", "Telescopes", "Факт 8/10. Телескопы собирают больше света, чем человеческий глаз. Радио-, инфракрасные и рентгеновские приборы показывают разные стороны Вселенной.", "Fact 8/10. Telescopes collect more light than the human eye. Radio, infrared, and X-ray instruments reveal different sides of the universe."),
                    Stage("Орбитальные станции", "Orbital Stations", "Факт 9/10. Орбитальные станции позволяют долго проводить эксперименты в микрогравитации. Такие лаборатории изучают материалы, медицину и жизнь в космосе.", "Fact 9/10. Orbital stations enable long experiments in microgravity. These laboratories study materials, medicine, and life in space."),
                    Stage("Экзопланеты", "Exoplanets", "Факт 10/10. Экзопланеты - планеты у других звёзд. Их находят по транзитам, колебаниям звезды и другим признакам, расширяя карту возможных миров.", "Fact 10/10. Exoplanets orbit other stars. Scientists find them through transits, stellar wobble, and other clues, expanding the map of possible worlds.")
                }),
            new(
                5,
                StoryThemeBranch.Nature,
                "nature",
                "StoryNature",
                "nature",
                32,
                new LocalizedText("Природа", "Nature", "Doğa", "Natur"),
                new LocalizedText("Экосистемы, вода, леса, животные, климат и равновесие жизни.", "Ecosystems, water, forests, animals, climate, and the balance of life.", "Ekosistemler, su, ormanlar, hayvanlar, iklim ve yaşam dengesi.", "Ökosysteme, Wasser, Wälder, Tiere, Klima und Gleichgewicht des Lebens."),
                new[]
                {
                    Stage("Экосистема", "Ecosystem", "Факт 1/10. Экосистема состоит из живых организмов и среды, где они обмениваются энергией и веществом. Даже маленькое изменение может пройти по всей цепи.", "Fact 1/10. An ecosystem includes living organisms and their environment, exchanging energy and matter. Even a small change can move through the whole chain."),
                    Stage("Фотосинтез", "Photosynthesis", "Факт 2/10. Растения и водоросли используют свет, воду и углекислый газ, чтобы создавать органические вещества. Так начинается большая часть пищевых цепей.", "Fact 2/10. Plants and algae use light, water, and carbon dioxide to create organic matter. Much of the food web begins there."),
                    Stage("Лесной этаж", "Forest Layers", "Факт 3/10. Лес имеет ярусы: кроны, подлесок, травы, почву. Каждый слой даёт свой дом для видов и свой путь для света и влаги.", "Fact 3/10. A forest has layers: canopy, understory, herbs, and soil. Each layer gives species a home and shapes light and moisture."),
                    Stage("Опылители", "Pollinators", "Факт 4/10. Пчёлы, бабочки, птицы и летучие мыши переносят пыльцу между цветами. Без опылителей многие растения и урожаи были бы под угрозой.", "Fact 4/10. Bees, butterflies, birds, and bats move pollen between flowers. Without pollinators, many wild plants and crops would be at risk."),
                    Stage("Круг воды", "Water Cycle", "Факт 5/10. Вода испаряется, образует облака, выпадает осадками и возвращается в реки, почву и океан. Этот круг связывает климат и жизнь.", "Fact 5/10. Water evaporates, forms clouds, falls as precipitation, and returns to rivers, soil, and ocean. This cycle links climate and life."),
                    Stage("Почва", "Soil", "Факт 6/10. Почва - не просто пыль. В ней живут грибы, бактерии, корни и беспозвоночные, которые превращают остатки жизни в питание для новых растений.", "Fact 6/10. Soil is not just dust. Fungi, bacteria, roots, and invertebrates turn remains of life into nutrients for new plants."),
                    Stage("Миграция", "Migration", "Факт 7/10. Многие животные мигрируют, чтобы найти пищу, тепло или места размножения. Птицы могут ориентироваться по Солнцу, звёздам и магнитному полю.", "Fact 7/10. Many animals migrate to find food, warmth, or breeding sites. Birds can navigate by the Sun, stars, and Earth's magnetic field."),
                    Stage("Коралловые рифы", "Coral Reefs", "Факт 8/10. Коралловые рифы занимают малую часть океана, но поддерживают огромное разнообразие жизни. Они чувствительны к нагреву и загрязнению воды.", "Fact 8/10. Coral reefs cover a small part of the ocean but support huge biodiversity. They are sensitive to warming and water pollution."),
                    Stage("Климат", "Climate", "Факт 9/10. Климат описывает долгосрочные закономерности погоды. Температура, осадки, океаны и атмосфера вместе задают условия для экосистем.", "Fact 9/10. Climate describes long-term weather patterns. Temperature, precipitation, oceans, and atmosphere together set conditions for ecosystems."),
                    Stage("Биоразнообразие", "Biodiversity", "Факт 10/10. Биоразнообразие делает природу устойчивее: разные виды выполняют разные роли. Когда исчезает вид, меняется вся сеть связей.", "Fact 10/10. Biodiversity makes nature more resilient because different species play different roles. When one species disappears, the whole network changes.")
                }),
            new(
                6,
                StoryThemeBranch.Human,
                "world",
                "StoryWorld",
                "world",
                30,
                new LocalizedText("Мир", "World", "Dünya", "Welt"),
                new LocalizedText("Материки, города, пути обмена, памятники и общая история людей.", "Continents, cities, exchange routes, monuments, and shared human history.", "Kıtalar, şehirler, değişim yolları, anıtlar ve ortak insan tarihi.", "Kontinente, Städte, Austauschwege, Denkmäler und gemeinsame Menschheitsgeschichte."),
                new[]
                {
                    Stage("Материки", "Continents", "Факт 1/10. Землю обычно делят на семь материков. Это удобная карта для изучения природы, народов, климата и исторических связей.", "Fact 1/10. Earth is commonly divided into seven continents. It is a useful map for studying nature, peoples, climate, and historical connections."),
                    Stage("Океаны", "Oceans", "Факт 2/10. Океаны покрывают большую часть поверхности Земли. Они регулируют климат, питают круг воды и связывают континенты морскими путями.", "Fact 2/10. Oceans cover most of Earth's surface. They regulate climate, feed the water cycle, and connect continents through sea routes."),
                    Stage("Письменность", "Writing", "Факт 3/10. Письменность возникала независимо в разных регионах. Она позволила хранить законы, торговые записи, мифы, науку и личную память.", "Fact 3/10. Writing arose independently in different regions. It allowed people to store laws, trade records, myths, science, and personal memory."),
                    Stage("Города", "Cities", "Факт 4/10. Ранние города росли там, где были вода, земледелие, обмен и управление. Город стал машиной памяти: улицы, рынки, храмы и мастерские.", "Fact 4/10. Early cities grew where water, farming, exchange, and administration met. A city became a memory machine of streets, markets, temples, and workshops."),
                    Stage("Шёлк и специи", "Silk and Spices", "Факт 5/10. Торговые пути переносили не только товары. Вместе с шёлком, специями и металлами двигались технологии, религии, болезни и идеи.", "Fact 5/10. Trade routes moved more than goods. Alongside silk, spices, and metals traveled technologies, religions, diseases, and ideas."),
                    Stage("Карты", "Maps", "Факт 6/10. Карты отражают не только территорию, но и знания своего времени. Они показывают, что общество считает центром, границей и дорогой.", "Fact 6/10. Maps show not only territory, but also the knowledge of their time. They reveal what a society treats as center, border, and road."),
                    Stage("Памятники", "Monuments", "Факт 7/10. Памятники помогают обществам помнить события, веру и власть. Архитектура превращает камень, дерево и металл в язык истории.", "Fact 7/10. Monuments help societies remember events, beliefs, and power. Architecture turns stone, wood, and metal into a language of history."),
                    Stage("Языки", "Languages", "Факт 8/10. В мире тысячи языков. Каждый язык хранит способы описывать родство, природу, числа, время и человеческий опыт.", "Fact 8/10. The world has thousands of languages. Each language preserves ways to describe kinship, nature, numbers, time, and human experience."),
                    Stage("Миграции людей", "Human Migrations", "Факт 9/10. Люди расселялись по планете волнами. Миграции меняли гены, технологии, еду, музыку и формы совместной жизни.", "Fact 9/10. Humans spread across the planet in waves. Migrations changed genes, technologies, food, music, and forms of shared life."),
                    Stage("Общее наследие", "Shared Heritage", "Факт 10/10. Всемирное наследие ценно тем, что принадлежит не только одной стране. Оно напоминает: культура локальна по форме, но общая по значению.", "Fact 10/10. World heritage matters because it belongs to more than one country. It reminds us that culture is local in form, but shared in meaning.")
                })
        };

        public static bool HasLevel(int levelNumber)
        {
            return TryGetDefinition(levelNumber, out _);
        }

        public static bool TryGetDefinition(int levelNumber, out StoryLevelDefinition definition)
        {
            for (int i = 0; i < Levels.Length; i++)
            {
                if (Levels[i].LevelNumber == levelNumber)
                {
                    definition = Levels[i];
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public static int GetStageCount(int levelNumber)
        {
            return TryGetDefinition(levelNumber, out StoryLevelDefinition definition)
                ? definition.StageCount
                : 0;
        }

        public static string GetLevelDisplayName(int levelNumber, GameLanguage language)
        {
            return TryGetDefinition(levelNumber, out StoryLevelDefinition definition)
                ? definition.GetDisplayName(language)
                : $"Level {levelNumber}";
        }

        public static string GetLevelSubtitle(int levelNumber, GameLanguage language)
        {
            return TryGetDefinition(levelNumber, out StoryLevelDefinition definition)
                ? definition.GetSubtitle(language)
                : string.Empty;
        }

        public static bool TryCreateStage(int levelNumber, int stageIndex, GameLanguage language, out LevelStageContent content)
        {
            content = null;
            return TryGetDefinition(levelNumber, out StoryLevelDefinition definition) &&
                   definition.TryCreateStage(stageIndex, language, out content);
        }

        public static int GetMaxLevelNumber()
        {
            int max = 0;
            for (int i = 0; i < Levels.Length; i++)
            {
                if (Levels[i].LevelNumber > max)
                    max = Levels[i].LevelNumber;
            }

            return max;
        }

        public static int GetFirstLevelNumber()
        {
            int first = int.MaxValue;
            for (int i = 0; i < Levels.Length; i++)
            {
                if (Levels[i].LevelNumber < first)
                    first = Levels[i].LevelNumber;
            }

            return first == int.MaxValue ? 0 : first;
        }

        public static bool TryGetNextLevelNumber(int currentLevel, out int nextLevel)
        {
            nextLevel = int.MaxValue;
            bool found = false;

            for (int i = 0; i < Levels.Length; i++)
            {
                int candidate = Levels[i].LevelNumber;
                if (candidate <= currentLevel || candidate >= nextLevel)
                    continue;

                nextLevel = candidate;
                found = true;
            }

            if (!found)
            {
                nextLevel = 0;
                return false;
            }

            return true;
        }

        public static int[] GetLevelNumbers(StoryThemeBranch branch)
        {
            int count = 0;
            for (int i = 0; i < Levels.Length; i++)
            {
                if (Levels[i].Branch == branch)
                    count++;
            }

            int[] numbers = new int[count];
            int index = 0;
            for (int i = 0; i < Levels.Length; i++)
            {
                if (Levels[i].Branch == branch)
                    numbers[index++] = Levels[i].LevelNumber;
            }

            return numbers;
        }

        private static StageEntry Stage(string russianTitle, string englishTitle, string russianDescription, string englishDescription)
        {
            return new StageEntry(
                new LocalizedText(russianTitle, englishTitle, GetTurkishTitle(englishTitle), GetGermanTitle(englishTitle)),
                new LocalizedText(russianDescription, englishDescription, GetTurkishDescription(englishTitle), GetGermanDescription(englishTitle)));
        }

        private static string GetTurkishTitle(string englishTitle)
        {
            return englishTitle switch
            {
                "Anatolian Bridge" => "Anadolu Köprüsü",
                "Troy and Layers" => "Troya ve Katmanlar",
                "Byzantine Capital" => "Bizans Başkenti",
                "Ottoman Horizon" => "Osmanlı Ufku",
                "Istanbul and the Strait" => "İstanbul ve Boğaz",
                "Cappadocia" => "Kapadokya",
                "Gobekli Tepe" => "Göbekli Tepe",
                "Tea and Conversation" => "Çay ve Sohbet",
                "Carpet as a Map" => "Harita Gibi Halı",
                "Republic" => "Cumhuriyet",
                "Gravity" => "Yerçekimi",
                "Solar System" => "Güneş Sistemi",
                "The Moon" => "Ay",
                "Mars" => "Mars",
                "Asteroid Belt" => "Asteroit Kuşağı",
                "Comets" => "Kuyruklu Yıldızlar",
                "Starlight" => "Yıldız Işığı",
                "Telescopes" => "Teleskoplar",
                "Orbital Stations" => "Yörünge İstasyonları",
                "Exoplanets" => "Ötegezegenler",
                "Ecosystem" => "Ekosistem",
                "Photosynthesis" => "Fotosentez",
                "Forest Layers" => "Orman Katmanları",
                "Pollinators" => "Tozlaştırıcılar",
                "Water Cycle" => "Su Döngüsü",
                "Soil" => "Toprak",
                "Migration" => "Göç",
                "Coral Reefs" => "Mercan Resifleri",
                "Climate" => "İklim",
                "Biodiversity" => "Biyoçeşitlilik",
                "Continents" => "Kıtalar",
                "Oceans" => "Okyanuslar",
                "Writing" => "Yazı",
                "Cities" => "Şehirler",
                "Silk and Spices" => "İpek ve Baharatlar",
                "Maps" => "Haritalar",
                "Monuments" => "Anıtlar",
                "Languages" => "Diller",
                "Human Migrations" => "İnsan Göçleri",
                "Shared Heritage" => "Ortak Miras",
                _ => englishTitle
            };
        }

        private static string GetGermanTitle(string englishTitle)
        {
            return englishTitle switch
            {
                "Anatolian Bridge" => "Anatolische Brücke",
                "Troy and Layers" => "Troja und Schichten",
                "Byzantine Capital" => "Byzantinische Hauptstadt",
                "Ottoman Horizon" => "Osmanischer Horizont",
                "Istanbul and the Strait" => "Istanbul und der Bosporus",
                "Cappadocia" => "Kappadokien",
                "Gobekli Tepe" => "Göbekli Tepe",
                "Tea and Conversation" => "Tee und Gespräch",
                "Carpet as a Map" => "Teppich als Karte",
                "Republic" => "Republik",
                "Gravity" => "Gravitation",
                "Solar System" => "Sonnensystem",
                "The Moon" => "Der Mond",
                "Mars" => "Mars",
                "Asteroid Belt" => "Asteroidengürtel",
                "Comets" => "Kometen",
                "Starlight" => "Sternenlicht",
                "Telescopes" => "Teleskope",
                "Orbital Stations" => "Orbitalstationen",
                "Exoplanets" => "Exoplaneten",
                "Ecosystem" => "Ökosystem",
                "Photosynthesis" => "Fotosynthese",
                "Forest Layers" => "Waldschichten",
                "Pollinators" => "Bestäuber",
                "Water Cycle" => "Wasserkreislauf",
                "Soil" => "Boden",
                "Migration" => "Migration",
                "Coral Reefs" => "Korallenriffe",
                "Climate" => "Klima",
                "Biodiversity" => "Biodiversität",
                "Continents" => "Kontinente",
                "Oceans" => "Ozeane",
                "Writing" => "Schrift",
                "Cities" => "Städte",
                "Silk and Spices" => "Seide und Gewürze",
                "Maps" => "Karten",
                "Monuments" => "Denkmäler",
                "Languages" => "Sprachen",
                "Human Migrations" => "Menschliche Migrationen",
                "Shared Heritage" => "Gemeinsames Erbe",
                _ => englishTitle
            };
        }

        private static string GetTurkishDescription(string englishTitle)
        {
            return englishTitle switch
            {
                "Anatolian Bridge" => "Bilgi 1/10. Anadolu Avrupa ile Asya'yı birbirine bağlar. Yüzyıllar boyunca ordular, kervanlar, diller ve zanaatlar buradan geçti; bu yüzden Türkiye kültürlerin yaşayan bir kavşağı oldu.",
                "Troy and Layers" => "Bilgi 2/10. Kuzeybatı Anadolu'daki arkeolojik Troya, birçok yerleşim katmanından oluşur. Bu, tarihin çoğu zaman tek çizgi değil, üst üste duran katmanlar olduğunu hatırlatır.",
                "Byzantine Capital" => "Bilgi 3/10. Konstantinopolis bin yıldan uzun süre Doğu Roma İmparatorluğu'nun başkentiydi. Surları, limanları ve kiliseleri kenti Akdeniz'in merkezlerinden biri yaptı.",
                "Ottoman Horizon" => "Bilgi 4/10. Osmanlı İmparatorluğu bir Anadolu beyliğinden doğup üç kıtaya yayılan bir güce dönüştü. Mirası mimaride, mutfakta ve şehir yaşamında hâlâ görülür.",
                "Istanbul and the Strait" => "Bilgi 5/10. Boğaz, İstanbul'u Avrupa ve Asya arasında böler. Bu geçit uzun süre Karadeniz ile Akdeniz arasındaki en önemli deniz yollarından biri oldu.",
                "Cappadocia" => "Bilgi 6/10. Kapadokya yumuşak volkanik tüfü, kaya oyma evleri ve yeraltı şehirleriyle bilinir. Manzara hem doğal hem de kültürel bir arşive dönüşmüştür.",
                "Gobekli Tepe" => "Bilgi 7/10. Türkiye'nin güneydoğusundaki Göbekli Tepe, bilinen en eski anıtsal ritüel alanlardan biridir. Neolitik toplumlara bakışımızı değiştirmiştir.",
                "Tea and Conversation" => "Bilgi 8/10. Türk çayı günlük sohbetin ritüeli haline gelmiştir. Genellikle demli hazırlanır ve küçük ince belli bardakta sunulur.",
                "Carpet as a Map" => "Bilgi 9/10. Anadolu halıları geometrik motifler, renkler ve yerel gelenekler taşır. Desenler aile, bölge ve zanaat hafızasını koruyabilir.",
                "Republic" => "Bilgi 10/10. Türkiye Cumhuriyeti 29 Ekim 1923'te ilan edildi. Yeni devlet sistemi eğitim, hukuk, yazı ve kamusal yaşamı değiştirdi.",
                "Gravity" => "Bilgi 1/10. Yerçekimi gezegenleri yörüngede tutar ve maddeyi yıldızlara, uydulara ve galaksilere toplar. O olmadan uzayın bildiğimiz yapısı olmazdı.",
                "Solar System" => "Bilgi 2/10. Güneş Sistemi'nde sekiz gezegen vardır. İç gezegenler kayalıktır; dış gezegenler arasında gaz ve buz devleri bulunur.",
                "The Moon" => "Bilgi 3/10. Ay, Dünya ekseninin eğimini dengelemeye yardım eder ve Güneş ile birlikte gelgitleri oluşturur. Yüzeyi milyarlarca yıllık çarpma izlerini saklar.",
                "Mars" => "Bilgi 4/10. Mars'ta kutup buzulları, kurumuş akarsu yatakları ve Güneş Sistemi'nin en büyük volkanı Olympus Mons bulunur. Gezegen eski su izleri için önemlidir.",
                "Asteroid Belt" => "Bilgi 5/10. Asteroit kuşağı Mars ile Jüpiter arasında yer alır. Yoğun bir taş duvarı değil, cisimler arasında büyük boşluklar olan geniş bir bölgedir.",
                "Comets" => "Bilgi 6/10. Kuyruklu yıldızlar buz, toz ve kayadan oluşur. Güneş'e yaklaştıklarında buz buharlaşır, koma ve kuyruk oluşur.",
                "Starlight" => "Bilgi 7/10. Uzak yıldızlardan gelen ışık bize yıllar, binlerce yıl ya da milyonlarca yıl sonra ulaşır. Uzaya bakmak geçmişe bakmaktır.",
                "Telescopes" => "Bilgi 8/10. Teleskoplar insan gözünden daha fazla ışık toplar. Radyo, kızılötesi ve X-ışını araçları evrenin farklı yönlerini gösterir.",
                "Orbital Stations" => "Bilgi 9/10. Yörünge istasyonları mikro yerçekiminde uzun deneyler yapmayı sağlar. Bu laboratuvarlar malzemeleri, tıbbı ve uzayda yaşamı inceler.",
                "Exoplanets" => "Bilgi 10/10. Ötegezegenler başka yıldızların çevresinde dönen gezegenlerdir. Geçişler, yıldız salınımları ve başka ipuçlarıyla bulunurlar.",
                "Ecosystem" => "Bilgi 1/10. Ekosistem canlı organizmalar ve onların çevresinden oluşur; enerji ve madde alışverişi yaparlar. Küçük bir değişim bile tüm zincire yayılabilir.",
                "Photosynthesis" => "Bilgi 2/10. Bitkiler ve algler ışık, su ve karbondioksit kullanarak organik madde üretir. Besin ağlarının büyük bölümü burada başlar.",
                "Forest Layers" => "Bilgi 3/10. Ormanın katmanları vardır: taç örtüsü, alt tabaka, otlar ve toprak. Her katman türlere ayrı bir yaşam alanı sağlar.",
                "Pollinators" => "Bilgi 4/10. Arılar, kelebekler, kuşlar ve yarasalar çiçekler arasında polen taşır. Tozlaştırıcılar olmadan birçok bitki ve ürün risk altına girer.",
                "Water Cycle" => "Bilgi 5/10. Su buharlaşır, bulutları oluşturur, yağış olarak düşer ve nehirlere, toprağa ve okyanusa döner. Bu döngü iklimi ve yaşamı bağlar.",
                "Soil" => "Bilgi 6/10. Toprak sadece toz değildir. Mantarlar, bakteriler, kökler ve omurgasızlar yaşam kalıntılarını yeni bitkiler için besine dönüştürür.",
                "Migration" => "Bilgi 7/10. Birçok hayvan yiyecek, sıcaklık ya da üreme alanı bulmak için göç eder. Kuşlar Güneş, yıldızlar ve manyetik alanla yön bulabilir.",
                "Coral Reefs" => "Bilgi 8/10. Mercan resifleri okyanusun küçük bir bölümünü kaplar ama büyük bir biyolojik çeşitliliği destekler. Isınmaya ve su kirliliğine duyarlıdır.",
                "Climate" => "Bilgi 9/10. İklim uzun dönemli hava düzenlerini anlatır. Sıcaklık, yağış, okyanuslar ve atmosfer ekosistemlerin koşullarını birlikte belirler.",
                "Biodiversity" => "Bilgi 10/10. Biyoçeşitlilik doğayı daha dayanıklı kılar; farklı türler farklı roller üstlenir. Bir tür yok olduğunda tüm ilişki ağı değişir.",
                "Continents" => "Bilgi 1/10. Dünya genellikle yedi kıtaya ayrılır. Bu, doğayı, halkları, iklimi ve tarihsel bağları incelemek için kullanışlı bir haritadır.",
                "Oceans" => "Bilgi 2/10. Okyanuslar Dünya yüzeyinin büyük bölümünü kaplar. İklimi düzenler, su döngüsünü besler ve kıtaları deniz yollarıyla bağlar.",
                "Writing" => "Bilgi 3/10. Yazı farklı bölgelerde bağımsız olarak ortaya çıktı. Yasaları, ticaret kayıtlarını, mitleri, bilimi ve kişisel hafızayı saklamayı sağladı.",
                "Cities" => "Bilgi 4/10. İlk şehirler su, tarım, değişim ve yönetimin buluştuğu yerlerde büyüdü. Şehir; sokakları, pazarları, tapınakları ve atölyeleriyle bir hafıza makinesidir.",
                "Silk and Spices" => "Bilgi 5/10. Ticaret yolları yalnızca malları taşımazdı. İpek, baharat ve metallerle birlikte teknolojiler, dinler, hastalıklar ve fikirler de hareket etti.",
                "Maps" => "Bilgi 6/10. Haritalar yalnızca bölgeyi değil, dönemlerinin bilgisini de yansıtır. Bir toplumun merkezi, sınırı ve yolu nasıl gördüğünü gösterir.",
                "Monuments" => "Bilgi 7/10. Anıtlar toplumların olayları, inancı ve iktidarı hatırlamasına yardım eder. Mimari taş, ahşap ve metali tarih diline çevirir.",
                "Languages" => "Bilgi 8/10. Dünyada binlerce dil vardır. Her dil akrabalığı, doğayı, sayıları, zamanı ve insan deneyimini anlatmanın yollarını saklar.",
                "Human Migrations" => "Bilgi 9/10. İnsanlar gezegene dalgalar halinde yayıldı. Göçler genleri, teknolojileri, yiyecekleri, müziği ve ortak yaşam biçimlerini değiştirdi.",
                "Shared Heritage" => "Bilgi 10/10. Dünya mirası önemlidir çünkü yalnızca tek bir ülkeye ait değildir. Kültürün biçimde yerel, anlamda ortak olduğunu hatırlatır.",
                _ => string.Empty
            };
        }

        private static string GetGermanDescription(string englishTitle)
        {
            return englishTitle switch
            {
                "Anatolian Bridge" => "Fakt 1/10. Anatolien verbindet Europa und Asien. Über Jahrhunderte zogen Armeen, Karawanen, Sprachen und Handwerke hindurch; so wurde die Türkei zu einem lebendigen Kreuzweg der Kulturen.",
                "Troy and Layers" => "Fakt 2/10. Das archäologische Troja im Nordwesten Anatoliens besteht aus vielen Siedlungsschichten. Es erinnert daran, dass Geschichte oft nicht als Linie, sondern in Schichten liegt.",
                "Byzantine Capital" => "Fakt 3/10. Konstantinopel war mehr als tausend Jahre Hauptstadt des Oströmischen Reiches. Seine Mauern, Häfen und Kirchen machten die Stadt zu einem Zentrum des Mittelmeerraums.",
                "Ottoman Horizon" => "Fakt 4/10. Das Osmanische Reich wuchs aus einem anatolischen Beylik zu einer Macht auf drei Kontinenten. Sein Erbe ist in Architektur, Küche und Stadtleben sichtbar.",
                "Istanbul and the Strait" => "Fakt 5/10. Der Bosporus teilt Istanbul zwischen Europa und Asien. Die Meerenge war lange ein wichtiger Seeweg zwischen Schwarzem Meer und Mittelmeer.",
                "Cappadocia" => "Fakt 6/10. Kappadokien ist bekannt für weichen vulkanischen Tuff, Felsenwohnungen und unterirdische Städte. Die Landschaft wurde zu einem natürlichen und kulturellen Archiv.",
                "Gobekli Tepe" => "Fakt 7/10. Göbekli Tepe im Südosten der Türkei gehört zu den frühesten bekannten monumentalen Ritualstätten. Es veränderte den Blick auf neolithische Gesellschaften.",
                "Tea and Conversation" => "Fakt 8/10. Türkischer Tee wurde zu einem alltäglichen Ritual des Gesprächs. Er wird meist stark aufgebrüht und in einem kleinen tulpenförmigen Glas serviert.",
                "Carpet as a Map" => "Fakt 9/10. Anatolische Teppiche tragen geometrische Motive, Farben und lokale Traditionen. Ihre Muster können Familien-, Regional- und Handwerksgedächtnis bewahren.",
                "Republic" => "Fakt 10/10. Die Republik Türkei wurde am 29. Oktober 1923 ausgerufen. Das neue Staatssystem veränderte Bildung, Recht, Schrift und öffentliches Leben.",
                "Gravity" => "Fakt 1/10. Gravitation hält Planeten auf Umlaufbahnen und sammelt Materie zu Sternen, Monden und Galaxien. Ohne sie hätte der Kosmos nicht seine vertraute Struktur.",
                "Solar System" => "Fakt 2/10. Das Sonnensystem hat acht Planeten. Die inneren Planeten sind felsig, während zu den äußeren Welten Gas- und Eisriesen gehören.",
                "The Moon" => "Fakt 3/10. Der Mond hilft, die Neigung der Erdachse zu stabilisieren, und erzeugt zusammen mit der Sonne die Gezeiten. Seine Oberfläche bewahrt Einschlagsspuren über Milliarden Jahre.",
                "Mars" => "Fakt 4/10. Auf dem Mars gibt es Polkappen, trockene Flussbetten und Olympus Mons, den größten Vulkan des Sonnensystems. Der Planet ist wichtig für die Suche nach altem Wasser.",
                "Asteroid Belt" => "Fakt 5/10. Der Asteroidengürtel liegt zwischen Mars und Jupiter. Er ist keine dichte Steinmauer, sondern eine riesige Region mit großen Abständen zwischen den Körpern.",
                "Comets" => "Fakt 6/10. Kometen bestehen aus Eis, Staub und Gestein. Wenn sie sich der Sonne nähern, verdampft Eis und bildet Koma und Schweif.",
                "Starlight" => "Fakt 7/10. Licht ferner Sterne braucht Jahre, Tausende oder Millionen Jahre bis zu uns. In den Weltraum zu schauen bedeutet, in die Vergangenheit zu schauen.",
                "Telescopes" => "Fakt 8/10. Teleskope sammeln mehr Licht als das menschliche Auge. Radio-, Infrarot- und Röntgeninstrumente zeigen verschiedene Seiten des Universums.",
                "Orbital Stations" => "Fakt 9/10. Orbitalstationen ermöglichen lange Experimente in Mikrogravitation. Solche Labore erforschen Materialien, Medizin und Leben im Weltraum.",
                "Exoplanets" => "Fakt 10/10. Exoplaneten kreisen um andere Sterne. Man findet sie durch Transite, Sternwackeln und andere Hinweise, wodurch die Karte möglicher Welten wächst.",
                "Ecosystem" => "Fakt 1/10. Ein Ökosystem umfasst lebende Organismen und ihre Umwelt, die Energie und Stoffe austauschen. Schon eine kleine Veränderung kann durch die ganze Kette wandern.",
                "Photosynthesis" => "Fakt 2/10. Pflanzen und Algen nutzen Licht, Wasser und Kohlendioxid, um organische Stoffe zu bilden. Dort beginnt ein großer Teil der Nahrungsnetze.",
                "Forest Layers" => "Fakt 3/10. Ein Wald hat Schichten: Kronendach, Unterwuchs, Kräuter und Boden. Jede Schicht bietet Arten einen eigenen Lebensraum und lenkt Licht und Feuchtigkeit.",
                "Pollinators" => "Fakt 4/10. Bienen, Schmetterlinge, Vögel und Fledermäuse tragen Pollen zwischen Blüten. Ohne Bestäuber wären viele Pflanzen und Ernten gefährdet.",
                "Water Cycle" => "Fakt 5/10. Wasser verdunstet, bildet Wolken, fällt als Niederschlag und kehrt in Flüsse, Boden und Ozean zurück. Dieser Kreislauf verbindet Klima und Leben.",
                "Soil" => "Fakt 6/10. Boden ist nicht nur Staub. Pilze, Bakterien, Wurzeln und Wirbellose verwandeln Lebensreste in Nährstoffe für neue Pflanzen.",
                "Migration" => "Fakt 7/10. Viele Tiere wandern, um Nahrung, Wärme oder Brutplätze zu finden. Vögel können sich an Sonne, Sternen und Magnetfeld orientieren.",
                "Coral Reefs" => "Fakt 8/10. Korallenriffe bedecken nur einen kleinen Teil des Ozeans, tragen aber enorme Artenvielfalt. Sie reagieren empfindlich auf Erwärmung und Wasserverschmutzung.",
                "Climate" => "Fakt 9/10. Klima beschreibt langfristige Wettermuster. Temperatur, Niederschlag, Ozeane und Atmosphäre bestimmen gemeinsam die Bedingungen für Ökosysteme.",
                "Biodiversity" => "Fakt 10/10. Biodiversität macht Natur widerstandsfähiger, weil verschiedene Arten verschiedene Rollen erfüllen. Wenn eine Art verschwindet, verändert sich das ganze Netz.",
                "Continents" => "Fakt 1/10. Die Erde wird meist in sieben Kontinente geteilt. Das ist eine nützliche Karte, um Natur, Völker, Klima und historische Verbindungen zu verstehen.",
                "Oceans" => "Fakt 2/10. Ozeane bedecken den größten Teil der Erdoberfläche. Sie regulieren das Klima, nähren den Wasserkreislauf und verbinden Kontinente durch Seewege.",
                "Writing" => "Fakt 3/10. Schrift entstand unabhängig in verschiedenen Regionen. Sie machte es möglich, Gesetze, Handelsaufzeichnungen, Mythen, Wissenschaft und persönliche Erinnerung zu speichern.",
                "Cities" => "Fakt 4/10. Frühe Städte wuchsen dort, wo Wasser, Landwirtschaft, Austausch und Verwaltung zusammentrafen. Eine Stadt wurde zu einer Gedächtnismaschine aus Straßen, Märkten, Tempeln und Werkstätten.",
                "Silk and Spices" => "Fakt 5/10. Handelswege transportierten nicht nur Waren. Mit Seide, Gewürzen und Metallen bewegten sich auch Technologien, Religionen, Krankheiten und Ideen.",
                "Maps" => "Fakt 6/10. Karten zeigen nicht nur Gebiet, sondern auch das Wissen ihrer Zeit. Sie verraten, was eine Gesellschaft als Zentrum, Grenze und Weg versteht.",
                "Monuments" => "Fakt 7/10. Denkmäler helfen Gesellschaften, Ereignisse, Glauben und Macht zu erinnern. Architektur verwandelt Stein, Holz und Metall in eine Sprache der Geschichte.",
                "Languages" => "Fakt 8/10. Auf der Welt gibt es Tausende Sprachen. Jede Sprache bewahrt eigene Arten, Verwandtschaft, Natur, Zahlen, Zeit und menschliche Erfahrung zu beschreiben.",
                "Human Migrations" => "Fakt 9/10. Menschen breiteten sich in Wellen über den Planeten aus. Migrationen veränderten Gene, Technologien, Nahrung, Musik und Formen gemeinsamen Lebens.",
                "Shared Heritage" => "Fakt 10/10. Welterbe ist wertvoll, weil es nicht nur einem Land gehört. Es erinnert daran, dass Kultur in der Form lokal, in der Bedeutung aber gemeinsam ist.",
                _ => string.Empty
            };
        }
    }
}
