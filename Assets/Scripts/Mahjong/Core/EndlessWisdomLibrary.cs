using UnityEngine;

namespace MahjongGame
{
    public readonly struct EndlessWisdomEntry
    {
        public readonly string Title;
        public readonly string Body;

        public EndlessWisdomEntry(string title, string body)
        {
            Title = title;
            Body = body;
        }
    }

    public static class EndlessWisdomLibrary
    {
        private static int lastIndex = -1;

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

        private readonly struct LocalizedEntry
        {
            public readonly LocalizedText Title;
            public readonly LocalizedText Body;

            public LocalizedEntry(LocalizedText title, LocalizedText body)
            {
                Title = title;
                Body = body;
            }
        }

        private readonly struct WisdomTemplate
        {
            public readonly LocalizedText Title;
            public readonly LocalizedText Lead;

            public WisdomTemplate(LocalizedText title, LocalizedText lead)
            {
                Title = title;
                Lead = lead;
            }
        }

        private readonly struct WisdomFocus
        {
            public readonly LocalizedText Subject;
            public readonly LocalizedText Lesson;

            public WisdomFocus(LocalizedText subject, LocalizedText lesson)
            {
                Subject = subject;
                Lesson = lesson;
            }
        }

        private static readonly LocalizedEntry[] CuratedEntries =
        {
            Entry(
                T("Тихий взгляд", "Quiet Sight", "Sessiz Bakis", "Ruhiger Blick"),
                T("Перед сильным ходом всегда есть короткая пауза. Посмотри на поле спокойно, и лишнее само исчезнет.", "Before a strong move, there is often a short pause. Look at the board calmly, and the noise begins to fade.", "Güclü bir hamleden önce kisa bir duraklama vardir. Tahtaya sakin bak; gereksiz olan kendiliginden azalir.", "Vor einem starken Zug gibt es oft eine kurze Pause. Schau ruhig auf das Feld, und das Unwichtige tritt zurueck.")),
            Entry(
                T("Память формы", "Memory of Shape", "Seklin Hafizasi", "Gedaechtnis der Form"),
                T("Мозг быстрее запоминает не отдельные предметы, а рисунок между ними. В Mahjong важно видеть всю форму раскладки.", "The mind remembers patterns between objects faster than isolated objects. In Mahjong, the full shape of the layout matters.", "Zihin tek tek nesnelerden cok aralarindaki deseni hatirlar. Mahjong'da dizilimin genel seklini okumak onemlidir.", "Der Geist merkt sich Beziehungen zwischen Dingen schneller als einzelne Dinge. In Mahjong zaehlt die ganze Form des Aufbaus.")),
            Entry(
                T("Открытый край", "Open Edge", "Acik Kenar", "Offene Kante"),
                T("Тайл свободен, если сверху его ничто не держит и открыт хотя бы один бок.", "A tile is free when nothing covers it from above and at least one side is open.", "Bir tas, ustu kapali degilse ve en az bir yani aciksa serbesttir.", "Ein Stein ist frei, wenn nichts auf ihm liegt und mindestens eine Seite offen ist.")),
            Entry(
                T("Путь пары", "Path of a Pair", "Ciftin Yolu", "Weg des Paares"),
                T("Каждая снятая пара не просто дает очки. Она открывает будущие решения.", "Every removed pair does more than score points. It opens future decisions.", "Kaldirilan her cift sadece puan vermez; gelecekteki kararlarin yolunu acar.", "Jedes entfernte Paar bringt nicht nur Punkte. Es oeffnet kommende Entscheidungen.")),
            Entry(
                T("Первый ключ", "First Key", "Ilk Anahtar", "Erster Schluessel"),
                T("В сложной раскладке ищи не самую красивую пару, а ту, которая открывает больше места.", "In a complex layout, look for the pair that opens the most space, not the one that only looks tempting.", "Zor bir dizilimde en guzel cifti degil, en cok alan acan cifti ara.", "In einem komplexen Aufbau suche nicht das schoenste Paar, sondern das Paar, das den meisten Raum oeffnet.")),
            Entry(
                T("Слои мысли", "Layers of Thought", "Dusuncenin Katmanlari", "Schichten des Denkens"),
                T("Слои на поле похожи на слои мысли. Иногда сначала нужно убрать верхнее, чтобы увидеть главное.", "The layers on the board resemble layers of thought. Sometimes the surface must move before the important part becomes visible.", "Tahtadaki katmanlar dusuncenin katmanlari gibidir. Bazen onemli olani gormek icin once usttekini kaldirmak gerekir.", "Die Schichten auf dem Feld erinnern an Denkschichten. Manchmal muss zuerst die Oberflaeche weichen, damit das Wichtige sichtbar wird.")),
            Entry(
                T("Честная задача", "Fair Challenge", "Adil Zorluk", "Faire Aufgabe"),
                T("Игрок должен проигрывать своим решениям, а не скрытой ошибке генератора.", "A player should lose to their own decisions, not to a hidden generator mistake.", "Oyuncu gizli bir uretici hatasina degil, kendi kararlarina yenilmelidir.", "Ein Spieler sollte an eigenen Entscheidungen scheitern, nicht an einem versteckten Fehler des Generators.")),
            Entry(
                T("Проходимость", "Solvability", "Cozulebilirlik", "Loesbarkeit"),
                T("В любой хорошей задаче есть честный путь. Mahjong напоминает: сложность ценна, когда она открывается вниманию.", "Every good challenge has a fair path. Mahjong reminds us that difficulty matters when attention can uncover it.", "Her iyi zorlugun adil bir yolu vardir. Mahjong sunu hatirlatir: zorluk, dikkat onu acabildiginde degerlidir.", "Jede gute Aufgabe hat einen fairen Weg. Mahjong erinnert daran: Schwierigkeit zaehlt, wenn Aufmerksamkeit sie freilegen kann.")),
            Entry(
                T("Широкое поле", "Wide Board", "Genis Tahta", "Breites Feld"),
                T("На широком поле легче читать края. Landscape-раскладка должна быть ясной, просторной и честной для касания.", "A wide board makes edges easier to read. A landscape layout should feel clear, spacious, and fair to touch.", "Genis tahtada kenarlari okumak daha kolaydir. Landscape dizilim temiz, ferah ve dokunusa adil olmalidir.", "Auf einem breiten Feld lassen sich Kanten leichter lesen. Ein Landscape-Aufbau sollte klar, weit und fair zu bedienen sein.")),
            Entry(
                T("Мягкая сложность", "Gentle Difficulty", "Yumusak Zorluk", "Sanfte Schwierigkeit"),
                T("Сложность должна расти как лестница, а не как стена.", "Difficulty should rise like stairs, not like a wall.", "Zorluk bir duvar gibi degil, basamak gibi yukselmelidir.", "Schwierigkeit sollte wie eine Treppe wachsen, nicht wie eine Wand.")),
            Entry(
                T("Смена тайлов", "Changing Tiles", "Taslarin Degismesi", "Wechselnde Steine"),
                T("Когда меняются тайлы, мозг заново учится различать формы. Это освежает внимание.", "When tiles change, the mind learns to distinguish shapes again. That refreshes attention.", "Taslar degistiginde zihin sekilleri yeniden ayirt etmeyi ogrenir. Bu dikkati tazeler.", "Wenn sich Steine aendern, lernt der Geist Formen neu zu unterscheiden. Das erfrischt die Aufmerksamkeit.")),
            Entry(
                T("План на два хода", "Two Moves Ahead", "Iki Hamle Ilerisi", "Zwei Zuege Voraus"),
                T("Смотри не только на пару, которую снимешь сейчас, но и на пару, которую она откроет.", "Look not only at the pair you remove now, but also at the pair it may reveal.", "Sadece simdi kaldiracagin cifte degil, onun acacagi cifte de bak.", "Schau nicht nur auf das Paar, das du jetzt entfernst, sondern auch auf das Paar, das dadurch frei wird.")),
            Entry(
                T("Символы", "Symbols", "Semboller", "Symbole"),
                T("Символы на тайлах помогают памяти. Чем лучше ты различаешь знаки, тем быстрее видишь пары.", "Tile symbols support memory. The better you distinguish signs, the faster you see pairs.", "Tas sembolleri hafizayi destekler. Isaretleri ne kadar iyi ayirirsan ciftleri o kadar hizli gorursun.", "Symbole auf Steinen stuetzen das Gedaechtnis. Je klarer du Zeichen unterscheidest, desto schneller findest du Paare.")),
            Entry(
                T("Сила паузы", "Power of Pause", "Duraklamanin Gucu", "Kraft der Pause"),
                T("Пауза не останавливает игру. Она возвращает тебе поле целиком.", "A pause does not stop the game. It gives the whole board back to you.", "Duraklama oyunu durdurmaz. Sana tahtayi yeniden butun olarak verir.", "Eine Pause haelt das Spiel nicht auf. Sie gibt dir das ganze Feld zurueck.")),
            Entry(
                T("Культура игры", "Culture of Play", "Oyunun Kulturu", "Kultur des Spiels"),
                T("Старые игры часто учили не словам, а привычке думать: наблюдать, ждать, сравнивать.", "Old games often taught without lectures: observe, wait, compare, decide.", "Eski oyunlar cogu zaman ders vermeden ogretir: gozlemle, bekle, karsilastir, karar ver.", "Alte Spiele lehrten oft ohne Vortrag: beobachten, warten, vergleichen, entscheiden.")),
            Entry(
                T("Для Endless", "For Endless", "Endless Icin", "Fuer Endless"),
                T("Endless тренирует состояние: спокойствие, ясность, память и умение видеть связь между частями.", "Endless trains a state of mind: calm, clarity, memory, and the ability to see connections between parts.", "Endless zihin halini calistirir: sakinlik, netlik, hafiza ve parcalar arasindaki bagi gorme becerisi.", "Endless trainiert einen Zustand: Ruhe, Klarheit, Gedaechtnis und die Faehigkeit, Verbindungen zwischen Teilen zu sehen.")),
            Entry(
                T("Маленький прогресс", "Small Progress", "Kucuk Ilerleme", "Kleiner Fortschritt"),
                T("Одна точная пара уже делает игрока сильнее.", "One precise pair already makes the player stronger.", "Tek dogru cift bile oyuncuyu guclendirir.", "Ein praezises Paar macht den Spieler bereits staerker.")),
            Entry(
                T("Ритм Endless", "Endless Rhythm", "Endless Ritmi", "Endless-Rhythmus"),
                T("Endless должен звучать как дыхание: наблюдение, мысль, новый взгляд.", "Endless should breathe: observation, thought, a new way of seeing.", "Endless nefes gibi akmali: gozlem, dusunce, yeni bir bakis.", "Endless sollte atmen: Beobachtung, Gedanke, ein neuer Blick.")),
            Entry(
                T("Найти дыхание", "Find the Breath", "Nefesi Bul", "Den Atem Finden"),
                T("Если поле кажется тяжелым, найди самый свободный участок и начни оттуда.", "If the board feels heavy, find the freest area and begin there.", "Tahta agir geliyorsa en serbest bolgeyi bul ve oradan basla.", "Wenn das Feld schwer wirkt, finde den freisten Bereich und beginne dort.")),
            Entry(
                T("Продолжай", "Continue", "Devam Et", "Weiter"),
                T("Каждая новая попытка похожа на прежнюю только снаружи. Внутри игрок уже несет память прошлых решений.", "Each new attempt only looks the same from outside. Inside, the player carries the memory of past decisions.", "Her yeni deneme disaridan ayni gorunur. Iceride oyuncu onceki kararlarin hafizasini tasir.", "Jeder neue Versuch wirkt nur von aussen gleich. Innen traegt der Spieler die Erinnerung frueherer Entscheidungen."))
        };

        private static readonly WisdomTemplate[] Templates =
        {
            Template(T("Ход мысли", "Move of Thought", "Dusuncenin Hamlesi", "Zug des Denkens"), T("Каждая мысль предлагает другой угол взгляда.", "Every thought offers a different angle of sight.", "Her dusunce bakisa baska bir aci verir.", "Jeder Gedanke bietet einen anderen Blickwinkel.")),
            Template(T("Спокойная практика", "Calm Practice", "Sakin Pratik", "Ruhige Uebung"), T("Endless силен не скоростью, а повторением с вниманием.", "Endless is powerful through attentive repetition, not speed.", "Endless hizla degil, dikkatli tekrar ile guclenir.", "Endless wirkt durch aufmerksame Wiederholung, nicht durch Tempo.")),
            Template(T("Между мыслями", "Between Thoughts", "Dusunceler Arasinda", "Zwischen Gedanken"), T("Короткая пауза нужна не для остановки, а для настройки зрения.", "A short pause is not a stop; it is a way to tune your sight.", "Kisa mola durmak icin degil, bakisi ayarlamak icindir.", "Eine kurze Pause ist kein Stillstand, sondern ein Stimmen des Blicks.")),
            Template(T("Тихая стратегия", "Quiet Strategy", "Sessiz Strateji", "Leise Strategie"), T("Хороший игрок не угадывает поле, а читает его постепенно.", "A good player does not guess the board; they read it gradually.", "Iyi oyuncu tahtayi tahmin etmez, adim adim okur.", "Ein guter Spieler raet das Feld nicht, sondern liest es Schritt fuer Schritt.")),
            Template(T("Путь внимания", "Path of Attention", "Dikkatin Yolu", "Weg der Aufmerksamkeit"), T("Каждая новая раскладка тренирует отдельную часть мышления.", "Every new layout trains a different part of thinking.", "Her yeni dizilim dusuncenin baska bir tarafini calistirir.", "Jeder neue Aufbau trainiert einen anderen Teil des Denkens.")),
            Template(T("Мягкая сложность", "Gentle Difficulty", "Yumusak Zorluk", "Sanfte Schwierigkeit"), T("Сложность полезна, когда у игрока остается пространство для выбора.", "Difficulty is useful when the player still has room to choose.", "Zorluk, oyuncuya secim alani biraktiginda faydalidir.", "Schwierigkeit ist hilfreich, wenn dem Spieler Raum fuer Wahl bleibt.")),
            Template(T("Ясная доска", "Clear Board", "Temiz Tahta", "Klares Feld"), T("Красивое поле должно помогать глазам находить связи.", "A beautiful board should help the eyes find connections.", "Guzel bir tahta gozlerin baglari bulmasina yardim etmelidir.", "Ein schoenes Feld sollte den Augen helfen, Verbindungen zu finden.")),
            Template(T("Ритм пары", "Rhythm of a Pair", "Ciftin Ritmi", "Rhythmus des Paares"), T("Пара исчезает быстро, но ее последствие остается на всем поле.", "A pair disappears quickly, but its consequence stays across the board.", "Cift hizli kaybolur, ama etkisi tum tahtada kalir.", "Ein Paar verschwindet schnell, doch seine Folge bleibt im ganzen Feld.")),
            Template(T("Маленький ключ", "Small Key", "Kucuk Anahtar", "Kleiner Schluessel"), T("Иногда большое понимание держится на одном скромном открытии.", "Sometimes a large understanding depends on one modest opening.", "Bazen buyuk bir anlayis tek kucuk acilisa dayanir.", "Manchmal haengt grosses Verstehen an einer kleinen Oeffnung.")),
            Template(T("Внутренний компас", "Inner Compass", "Ic Pusula", "Innerer Kompass"), T("Когда вариантов много, вернись к простым правилам.", "When there are many options, return to the simple rules.", "Secenek cok oldugunda basit kurallara geri don.", "Wenn es viele Optionen gibt, kehre zu den einfachen Regeln zurueck.")),
            Template(T("Память игрока", "Player Memory", "Oyuncu Hafizasi", "Spielergedaechtnis"), T("Опыт прошлой раскладки остается в руке даже тогда, когда тайлы уже другие.", "The previous layout remains in your hand even when the tiles change.", "Onceki dizilimin deneyimi, taslar degisse de elinde kalir.", "Die Erfahrung des vorigen Aufbaus bleibt in der Hand, auch wenn die Steine anders sind.")),
            Template(T("Endless как путь", "Endless as a Path", "Yol Olarak Endless", "Endless als Weg"), T("Бесконечный режим ценен тем, что не требует финальной точки.", "Endless is valuable because it does not demand a final point.", "Endless degerlidir cunku son nokta istemez.", "Endless ist wertvoll, weil es keinen endgueltigen Punkt verlangt.")),
            Template(T("Чистая логика", "Clean Logic", "Temiz Mantik", "Klare Logik"), T("Логика начинается с вопроса о том, что станет свободным дальше.", "Logic begins with the question: what becomes free next?", "Mantik su soruyla baslar: sirada ne serbest kalacak?", "Logik beginnt mit der Frage: Was wird als Naechstes frei?")),
            Template(T("Плавный рост", "Smooth Growth", "Akici Gelisim", "Sanftes Wachstum"), T("Через повторение игрок учится видеть не больше, а точнее.", "Through repetition, the player learns to see not more, but more precisely.", "Tekrarla oyuncu daha cok degil, daha net gormeyi ogrenir.", "Durch Wiederholung lernt der Spieler nicht mehr zu sehen, sondern genauer.")),
            Template(T("Смысл формы", "Meaning of Shape", "Seklin Anlami", "Sinn der Form"), T("Форма раскладки говорит раньше, чем отдельный символ на тайле.", "The layout shape speaks before any single tile symbol.", "Dizilimin sekli, tek bir tas sembolunden once konusur.", "Die Form des Aufbaus spricht vor jedem einzelnen Steinsymbol.")),
            Template(T("Свет решения", "Light of Solution", "Cozum Isigi", "Licht der Loesung"), T("Решение появляется, когда поле перестает казаться шумом.", "The solution appears when the board stops feeling like noise.", "Tahta gurultu gibi gelmeyi biraktiginda cozum belirir.", "Die Loesung erscheint, wenn das Feld nicht mehr wie Rauschen wirkt.")),
            Template(T("Пауза мастера", "Master's Pause", "Ustanin Molasi", "Pause des Meisters"), T("Пауза перед ходом может сэкономить несколько неверных попыток.", "A pause before moving can save several wrong attempts.", "Hamleden onceki mola birkac yanlis denemeyi kurtarabilir.", "Eine Pause vor dem Zug kann mehrere falsche Versuche sparen.")),
            Template(T("Долгая ясность", "Long Clarity", "Uzun Netlik", "Lange Klarheit"), T("Endless должен утомлять меньше, чем хаос, и бодрить больше, чем случайность.", "Endless should tire less than chaos and awaken more than randomness.", "Endless kaostan az yormali, rastgelelikten cok uyandirmali.", "Endless sollte weniger ermueden als Chaos und mehr wecken als Zufall."))
        };

        private static readonly LocalizedEntry[] PhilosophicalEntries =
        {
            Entry(
                T("Сократ", "Socrates", "Sokrates", "Sokrates"),
                T("Сократ связывал мудрость с признанием незнания. В Mahjong это начинается с честного взгляда на поле: не спешить, а увидеть.", "Socrates linked wisdom with admitting what we do not know. In Mahjong, that begins with an honest look at the board: do not rush, see.", "Sokrates bilgeliği bilmedigini kabul etmekle iliskilendirirdi. Mahjong'da bu, tahtaya durust bakmakla baslar: acele etme, gor.", "Sokrates verband Weisheit mit dem Eingestaendnis des Nichtwissens. In Mahjong beginnt das mit einem ehrlichen Blick auf das Feld: nicht eilen, sehen.")),
            Entry(
                T("Конфуций", "Confucius", "Konfucyus", "Konfuzius"),
                T("Конфуций учил, что порядок рождается из малых правильных действий. Одна верная пара может изменить весь путь раскладки.", "Confucius taught that order grows from small right actions. One correct pair can change the whole path of a layout.", "Konfucyus duzenin kucuk dogru eylemlerden dogdugunu ogretirdi. Tek dogru cift tum dizilimin yolunu degistirebilir.", "Konfuzius lehrte, dass Ordnung aus kleinen richtigen Handlungen waechst. Ein richtiges Paar kann den ganzen Weg eines Aufbaus veraendern.")),
            Entry(
                T("Лао-цзы", "Lao Tzu", "Lao Tzu", "Laozi"),
                T("Лао-цзы напоминал: мягкое часто побеждает жесткое. В Endless спокойное внимание сильнее резкого движения.", "Lao Tzu reminds us that the soft often overcomes the hard. In Endless, calm attention is stronger than a sharp impulse.", "Lao Tzu yumusagin sert olani sikca astigini hatirlatir. Endless'te sakin dikkat keskin tepkiden daha gucludur.", "Laozi erinnert daran, dass das Weiche oft das Harte ueberwindet. In Endless ist ruhige Aufmerksamkeit staerker als ein harter Impuls.")),
            Entry(
                T("Аристотель", "Aristotle", "Aristoteles", "Aristoteles"),
                T("Аристотель видел знание в умении различать причины. В Mahjong важно понимать не только какой тайл снять, но что он откроет.", "Aristotle saw knowledge in understanding causes. In Mahjong, it matters not only which tile to remove, but what it will open.", "Aristoteles bilgiyi nedenleri anlamakta gorurdu. Mahjong'da hangi tasi kaldirdigin kadar onun ne acacagi da onemlidir.", "Aristoteles sah Wissen im Verstehen von Ursachen. In Mahjong zaehlt nicht nur, welcher Stein verschwindet, sondern was er oeffnet.")),
            Entry(
                T("Марк Аврелий", "Marcus Aurelius", "Marcus Aurelius", "Marc Aurel"),
                T("Марк Аврелий писал о власти над своим вниманием. Когда поле кажется сложным, первым делом верни себе спокойный взгляд.", "Marcus Aurelius wrote about command over attention. When the board feels complex, first recover your calm sight.", "Marcus Aurelius dikkatin uzerindeki hakimiyetten soz ederdi. Tahta karmasik gelince once sakin bakisini geri al.", "Marc Aurel schrieb ueber die Herrschaft ueber die eigene Aufmerksamkeit. Wenn das Feld schwierig wirkt, gewinne zuerst den ruhigen Blick zurueck.")),
            Entry(
                T("Сенека", "Seneca", "Seneca", "Seneca"),
                T("Сенека считал, что время становится ценным через осознанность. В Endless пауза перед ходом часто экономит больше, чем скорость.", "Seneca saw time becoming valuable through awareness. In Endless, a pause before a move often saves more than speed.", "Seneca zamani farkindalikla degerli gorurdu. Endless'te hamleden onceki mola hizdan daha cok kazandirir.", "Seneca sah Zeit durch Bewusstsein wertvoll werden. In Endless spart eine Pause vor dem Zug oft mehr als Tempo.")),
            Entry(
                T("Гераклит", "Heraclitus", "Herakleitos", "Heraklit"),
                T("Гераклит говорил о мире как о постоянном изменении. В Mahjong поле меняется после каждой пары, и мысль должна течь вместе с ним.", "Heraclitus saw the world as constant change. In Mahjong, the board changes after every pair, and thought must flow with it.", "Herakleitos dunyayi surekli degisim olarak gorurdu. Mahjong'da her ciftten sonra tahta degisir, dusunce de onunla akmalidir.", "Heraklit sah die Welt als staendige Veraenderung. In Mahjong veraendert sich das Feld nach jedem Paar, und der Gedanke muss mitfliessen.")),
            Entry(
                T("Ибн Сина", "Ibn Sina", "Ibn Sina", "Ibn Sina"),
                T("Ибн Сина соединял наблюдение и разум. Хороший ход в Mahjong рождается там, где глаз и логика работают вместе.", "Ibn Sina joined observation with reason. A good Mahjong move appears where the eye and logic work together.", "Ibn Sina gozlemi akilla birlestirirdi. Iyi Mahjong hamlesi goz ve mantik birlikte calistiginda dogar.", "Ibn Sina verband Beobachtung mit Vernunft. Ein guter Mahjong-Zug entsteht, wenn Auge und Logik zusammenarbeiten.")),
            Entry(
                T("Руми", "Rumi", "Rumi", "Rumi"),
                T("Руми часто говорил о внутреннем пути. Endless похож на такую дорогу: каждая пара убирает шум и оставляет больше ясности.", "Rumi often spoke about the inner path. Endless is similar: every pair removes noise and leaves more clarity.", "Rumi ic yolculuktan sikca soz ederdi. Endless buna benzer: her cift gurultuyu azaltir ve daha cok netlik birakir.", "Rumi sprach oft vom inneren Weg. Endless ist aehnlich: jedes Paar nimmt Rauschen weg und laesst mehr Klarheit.")),
            Entry(
                T("Леонардо да Винчи", "Leonardo da Vinci", "Leonardo da Vinci", "Leonardo da Vinci"),
                T("Леонардо искал связь искусства, природы и механики. Mahjong тоже учит видеть форму, движение и скрытую конструкцию.", "Leonardo searched for the connection between art, nature, and mechanics. Mahjong also teaches us to see form, movement, and hidden structure.", "Leonardo sanat, doga ve mekanik arasindaki bagi arardi. Mahjong da formu, hareketi ve gizli yapıyı gormeyi ogretir.", "Leonardo suchte die Verbindung von Kunst, Natur und Mechanik. Mahjong lehrt ebenfalls Form, Bewegung und verborgene Struktur zu sehen.")),
            Entry(
                T("Миямото Мусаси", "Miyamoto Musashi", "Miyamoto Musashi", "Miyamoto Musashi"),
                T("Мусаси ценил прямое видение ситуации. В Endless сильный ход часто прост: он убирает препятствие и открывает дорогу.", "Musashi valued direct sight of the situation. In Endless, a strong move is often simple: it removes an obstacle and opens a road.", "Musashi durumun dogrudan gorulmesine deger verirdi. Endless'te guclu hamle sikca basittir: engeli kaldirir ve yolu acar.", "Musashi schaetzte den direkten Blick auf die Lage. In Endless ist ein starker Zug oft einfach: er entfernt ein Hindernis und oeffnet den Weg.")),
            Entry(
                T("Паскаль", "Pascal", "Pascal", "Pascal"),
                T("Паскаль замечал, что человек часто теряет себя в рассеянности. Mahjong возвращает внимание к одному видимому выбору.", "Pascal noticed how easily people lose themselves in distraction. Mahjong returns attention to one visible choice.", "Pascal insanin daginiklikta kendini kolayca kaybettigini fark etmisti. Mahjong dikkati tek gorunur secime geri getirir.", "Pascal bemerkte, wie leicht der Mensch sich in Zerstreuung verliert. Mahjong fuehrt Aufmerksamkeit zu einer sichtbaren Wahl zurueck.")),
            Entry(
                T("Декарт", "Descartes", "Descartes", "Descartes"),
                T("Декарт советовал делить сложное на простые части. Большая раскладка становится понятной, когда ты читаешь ее маленькими областями.", "Descartes advised dividing the complex into simple parts. A large layout becomes readable when you read it in small areas.", "Descartes karmasigi basit parcalara ayirmayi onerirdi. Buyuk dizilim kucuk bolgeler halinde okununca anlasilir olur.", "Descartes empfahl, Komplexes in einfache Teile zu teilen. Ein grosser Aufbau wird lesbar, wenn man ihn in kleinen Bereichen liest.")),
            Entry(
                T("Кант", "Kant", "Kant", "Kant"),
                T("Кант связывал мышление с порядком восприятия. В Mahjong порядок появляется тогда, когда взгляд перестает хвататься за все сразу.", "Kant linked thought with the order of perception. In Mahjong, order appears when the eye stops grabbing everything at once.", "Kant dusunceyi alginin duzeniyle iliskilendirirdi. Mahjong'da duzen, goz her seyi birden yakalamayi biraktiginda gorunur.", "Kant verband Denken mit der Ordnung der Wahrnehmung. In Mahjong erscheint Ordnung, wenn der Blick nicht mehr alles zugleich greifen will.")),
            Entry(
                T("Ницше", "Nietzsche", "Nietzsche", "Nietzsche"),
                T("Ницше видел силу в преодолении себя. В Endless это не борьба с полем, а рост над прежней поспешностью.", "Nietzsche saw strength in overcoming oneself. In Endless, that is not a fight with the board, but growth beyond old haste.", "Nietzsche gucu insanin kendini asmasinda gorurdu. Endless'te bu tahta ile savas degil, eski aceleciliği asmaktir.", "Nietzsche sah Staerke im Ueberwinden seiner selbst. In Endless ist das kein Kampf mit dem Feld, sondern Wachstum ueber alte Hast hinaus.")),
            Entry(
                T("Толстой", "Tolstoy", "Tolstoy", "Tolstoi"),
                T("Толстой возвращал большие истины к простым человеческим действиям. В Mahjong ясность тоже начинается с малого честного шага.", "Tolstoy returned large truths to simple human actions. In Mahjong, clarity also begins with one small honest step.", "Tolstoy buyuk gercekleri basit insan eylemlerine geri getirirdi. Mahjong'da netlik de kucuk durust bir adimla baslar.", "Tolstoi fuehrte grosse Wahrheiten auf einfache menschliche Handlungen zurueck. In Mahjong beginnt Klarheit ebenfalls mit einem kleinen ehrlichen Schritt.")),
            Entry(
                T("Тагор", "Tagore", "Tagore", "Tagore"),
                T("Тагор видел знание как живой свет, а не сухую формулу. Endless хорош тогда, когда факт становится ощущением в игре.", "Tagore saw knowledge as living light, not a dry formula. Endless works when a fact becomes something felt through play.", "Tagore bilgiyi kuru formül degil, yasayan isik olarak gorurdu. Endless, bilgi oyun icinde hissedildiginde guclenir.", "Tagore sah Wissen als lebendiges Licht, nicht als trockene Formel. Endless wirkt, wenn ein Fakt im Spiel fuehlbar wird.")),
            Entry(
                T("Алан Уоттс", "Alan Watts", "Alan Watts", "Alan Watts"),
                T("Алан Уоттс говорил о внимании к настоящему моменту. В Mahjong настоящий момент - это открытый тайл, свободная сторона и следующий смысл.", "Alan Watts spoke about attention to the present moment. In Mahjong, the present moment is an open tile, a free side, and the next meaning.", "Alan Watts simdiki ana dikkat etmekten soz ederdi. Mahjong'da simdiki an acik tas, serbest yan ve sonraki anlamdir.", "Alan Watts sprach von Aufmerksamkeit fuer den gegenwaertigen Moment. In Mahjong ist dieser Moment ein offener Stein, eine freie Seite und die naechste Bedeutung.")),
            Entry(
                T("Карл Юнг", "Carl Jung", "Carl Jung", "Carl Jung"),
                T("Юнг изучал символы как язык глубокой памяти. Тайлы работают похоже: знак становится опорой для узнавания и выбора.", "Jung studied symbols as a language of deep memory. Tiles work in a similar way: a sign becomes support for recognition and choice.", "Jung sembolleri derin hafizanin dili olarak incelerdi. Taslar benzer calisir: isaret tanima ve secim icin dayanak olur.", "Jung untersuchte Symbole als Sprache des tiefen Gedaechtnisses. Steine wirken aehnlich: ein Zeichen wird zur Stuetzte fuer Erkennen und Wahl.")),
            Entry(
                T("Ханна Арендт", "Hannah Arendt", "Hannah Arendt", "Hannah Arendt"),
                T("Арендт связывала мышление с остановкой автоматизма. В Endless полезно иногда остановиться, чтобы ход стал выбором, а не привычкой.", "Arendt linked thinking with stopping automatic motion. In Endless, it helps to pause so a move becomes a choice, not a habit.", "Arendt dusunmeyi otomatik hareketi durdurmakla iliskilendirirdi. Endless'te hamlenin aliskanlik degil secim olmasi icin durmak yararlidir.", "Arendt verband Denken mit dem Anhalten des Automatismus. In Endless hilft eine Pause, damit ein Zug Wahl wird, nicht Gewohnheit."))
        };

        private static readonly WisdomFocus[] Focuses =
        {
            Focus(T("края", "edges", "kenarlar", "Kanten"), T("крайние тайлы чаще всего открывают первый честный путь.", "edge tiles often reveal the first honest path.", "kenar taslari cogu zaman ilk adil yolu acar.", "Randsteine zeigen oft den ersten fairen Weg.")),
            Focus(T("верхний слой", "top layer", "ust katman", "obere Schicht"), T("верхние тайлы держат нижние возможности закрытыми.", "upper tiles keep lower possibilities locked.", "ust taslar alttaki olasiliklari kapali tutar.", "obere Steine halten untere Moeglichkeiten verschlossen.")),
            Focus(T("центр", "center", "merkez", "Mitte"), T("центр полезен тогда, когда к нему уже подготовлен проход.", "the center is useful when a path toward it is prepared.", "merkez, ona giden yol hazirlandiginda degerlidir.", "die Mitte ist wertvoll, wenn ein Weg dorthin vorbereitet ist.")),
            Focus(T("левый бок", "left side", "sol taraf", "linke Seite"), T("свободный бок делает тайл частью решения, а не декора.", "an open side turns a tile into part of the solution, not decoration.", "acik yan tasi dekor degil, cozumun parcasi yapar.", "eine offene Seite macht den Stein zum Teil der Loesung, nicht zur Dekoration.")),
            Focus(T("правый бок", "right side", "sag taraf", "rechte Seite"), T("правый край может открыть такой же важный путь, как и левый.", "the right edge can open a path as important as the left.", "sag kenar da sol kadar onemli bir yol acabilir.", "die rechte Kante kann einen ebenso wichtigen Weg oeffnen wie die linke.")),
            Focus(T("пустое место", "empty space", "bos alan", "leerer Raum"), T("пустота показывает, что порядок уже начал появляться.", "emptiness shows that order has already begun to appear.", "bosluk duzenin basladigini gosterir.", "Leere zeigt, dass Ordnung bereits entsteht.")),
            Focus(T("повтор символов", "repeated symbols", "tekrar eden semboller", "wiederholte Symbole"), T("повтор помогает памяти удерживать карту поля.", "repetition helps memory hold the board map.", "tekrar hafizanin tahta haritasini tutmasina yardim eder.", "Wiederholung hilft dem Gedaechtnis, die Feldkarte zu halten.")),
            Focus(T("контраст тайлов", "tile contrast", "tas kontrasti", "Steinkontrast"), T("четкий силуэт важнее мелкого украшения.", "a clear silhouette matters more than tiny decoration.", "net siluet kucuk suslerden daha onemlidir.", "eine klare Silhouette ist wichtiger als kleine Verzierung.")),
            Focus(T("симметрия", "symmetry", "simetri", "Symmetrie"), T("симметрия помогает читать поле, но решение часто прячется в ее нарушении.", "symmetry helps reading, but the solution often hides in a small break.", "simetri okumayi kolaylastirir, ama cozum bazen kucuk bozulmada saklanir.", "Symmetrie hilft beim Lesen, doch die Loesung steckt oft im kleinen Bruch.")),
            Focus(T("асимметрия", "asymmetry", "asimetrik nokta", "Asymmetrie"), T("маленькое отличие иногда показывает самый полезный ход.", "a small difference sometimes reveals the most useful move.", "kucuk fark bazen en faydali hamleyi gosterir.", "ein kleiner Unterschied zeigt manchmal den nuetzlichsten Zug.")),
            Focus(T("первый ход", "first move", "ilk hamle", "erster Zug"), T("первый выбор задает направление всей раскладке.", "the first choice sets the direction of the whole layout.", "ilk secim tum dizilimin yonunu belirler.", "die erste Wahl gibt dem ganzen Aufbau Richtung.")),
            Focus(T("последний ход", "last move", "son hamle", "letzter Zug"), T("последняя пара хороша только тогда, когда путь к ней был честным.", "the last pair feels right when the path to it was fair.", "son cift, ona giden yol adilse guzel hissettirir.", "das letzte Paar wirkt gut, wenn der Weg dorthin fair war.")),
            Focus(T("два хода вперед", "two moves ahead", "iki hamle ilerisi", "zwei Zuege voraus"), T("смотри не только на снятую пару, но и на дверь, которую она откроет.", "watch not only the removed pair, but the door it opens.", "sadece kaldirilan cifte degil, actigi kapiya da bak.", "achte nicht nur auf das entfernte Paar, sondern auf die Tuer, die es oeffnet.")),
            Focus(T("три возможных пары", "three possible pairs", "uc olasi cift", "drei moegliche Paare"), T("если вариантов много, выбирай тот, что освобождает больше тайлов.", "when options multiply, choose the one that frees more tiles.", "secenek cogalinca daha cok tas acani sec.", "wenn die Optionen wachsen, waehle die, die mehr Steine befreit.")),
            Focus(T("закрытая пара", "blocked pair", "kapali cift", "blockiertes Paar"), T("видимая пара не всегда доступна, и это часть глубины.", "a visible pair is not always available, and that creates depth.", "gorunen cift her zaman alinmaz; derinlik buradan gelir.", "ein sichtbares Paar ist nicht immer verfuegbar, und genau das schafft Tiefe.")),
            Focus(T("доступная пара", "available pair", "alinabilir cift", "verfuegbares Paar"), T("свободная пара ценна последствиями, а не только очками.", "a free pair is valuable for its consequences, not only for points.", "serbest cift sadece puanla degil, sonucuyla degerlidir.", "ein freies Paar zaehlt wegen seiner Folgen, nicht nur wegen der Punkte.")),
            Focus(T("сложная форма", "complex shape", "karma sekil", "komplexe Form"), T("сложность становится понятной, если разбить ее на маленькие участки.", "complexity becomes readable when divided into small areas.", "zorluk kucuk bolgelere ayrilinca okunur olur.", "Komplexitaet wird lesbar, wenn man sie in kleine Bereiche teilt.")),
            Focus(T("простая линия", "simple line", "basit cizgi", "einfache Linie"), T("простая линия учит читать расстояние между символами.", "a simple line trains the eye to read distance between symbols.", "basit cizgi, semboller arasindaki mesafeyi okumayi ogretir.", "eine einfache Linie trainiert den Blick fuer Abstand zwischen Symbolen.")),
            Focus(T("широкая раскладка", "wide layout", "genis dizilim", "breiter Aufbau"), T("landscape-поле должно давать глазам место для дыхания.", "a landscape board should give the eyes room to breathe.", "landscape tahta gozlere nefes alacak alan vermelidir.", "ein Landscape-Feld sollte den Augen Raum zum Atmen geben.")),
            Focus(T("узкий проход", "narrow passage", "dar gecit", "enger Durchgang"), T("узкое место лучше раскрывать осторожно, чтобы не закрыть будущие ходы.", "a narrow passage should open carefully, so future moves stay alive.", "dar gecit dikkatle acilmali ki gelecek hamleler yasasin.", "ein enger Durchgang sollte vorsichtig geoeffnet werden, damit kommende Zuege leben.")),
            Focus(T("высокая башня", "tall stack", "yuksek kule", "hoher Stapel"), T("вертикальные слои требуют терпения и порядка.", "vertical layers demand patience and order.", "dikey katmanlar sabir ve duzen ister.", "vertikale Schichten verlangen Geduld und Ordnung.")),
            Focus(T("низкий ряд", "low row", "alttaki sira", "niedrige Reihe"), T("плоская часть поля помогает быстро восстановить темп.", "a flat area helps restore tempo quickly.", "duz alan tempoyu hizla geri getirir.", "ein flacher Bereich hilft, das Tempo schnell wiederzufinden.")),
            Focus(T("новый набор тайлов", "new tile set", "yeni tas seti", "neues Steinset"), T("новые символы освежают внимание и ломают автоматизм.", "new symbols refresh attention and break automatic play.", "yeni semboller dikkati tazeler ve otomatik oyunu bozar.", "neue Symbole erfrischen die Aufmerksamkeit und brechen Automatismus.")),
            Focus(T("старый набор тайлов", "familiar tile set", "tanidik tas seti", "vertrautes Steinset"), T("знакомые символы дают скорость, но могут усыпить внимательность.", "familiar symbols give speed, but they can dull attention.", "tanidik semboller hiz verir, ama dikkati uyutabilir.", "vertraute Symbole geben Tempo, koennen aber die Aufmerksamkeit einschlaefern.")),
            Focus(T("цвет", "color", "renk", "Farbe"), T("цвет помогает искать, если не спорит с формой.", "color helps searching when it does not fight the shape.", "renk, sekille kavga etmezse aramaya yardim eder.", "Farbe hilft beim Suchen, wenn sie nicht gegen die Form arbeitet.")),
            Focus(T("свет", "light", "isik", "Licht"), T("свет должен показывать тайл, а не забирать внимание у игры.", "light should reveal the tile, not steal attention from play.", "isik tasi gostermeli, oyundan dikkat calmamali.", "Licht sollte den Stein zeigen, nicht Aufmerksamkeit vom Spiel stehlen.")),
            Focus(T("тень", "shadow", "golge", "Schatten"), T("тень полезна, когда помогает понять слой и глубину.", "shadow is useful when it explains layer and depth.", "golge katmani ve derinligi anlatiyorsa faydalidir.", "Schatten ist nuetzlich, wenn er Schicht und Tiefe erklaert.")),
            Focus(T("тишина", "quiet", "sessizlik", "Stille"), T("тихий интерфейс оставляет больше сил для решения.", "a quiet interface leaves more strength for solving.", "sessiz arayuz cozum icin daha cok guc birakir.", "eine stille Oberflaeche laesst mehr Kraft fuer die Loesung.")),
            Focus(T("ошибка", "mistake", "hata", "Fehler"), T("ошибка полезна, если после нее видно, какая возможность исчезла.", "a mistake teaches when it shows which possibility disappeared.", "hata, hangi ihtimalin kayboldugunu gosterirse ogretir.", "ein Fehler lehrt, wenn er zeigt, welche Moeglichkeit verschwand.")),
            Focus(T("удача", "luck", "sans", "Glueck"), T("удача приятна, но сильный Endless строится на проходимости.", "luck feels good, but strong Endless is built on solvability.", "sans guzeldir, ama guclu Endless cozulebilirlik uzerine kurulur.", "Glueck fuehlt sich gut an, doch starkes Endless steht auf Loesbarkeit.")),
            Focus(T("память", "memory", "hafiza", "Gedaechtnis"), T("память держит найденные пары как маленькие точки на карте.", "memory holds found pairs like small marks on a map.", "hafiza bulunan ciftleri haritadaki isaretler gibi tutar.", "das Gedaechtnis haelt gefundene Paare wie kleine Punkte auf einer Karte.")),
            Focus(T("внимание", "attention", "dikkat", "Aufmerksamkeit"), T("внимание выбирает, что важно сейчас, а что может подождать.", "attention chooses what matters now and what can wait.", "dikkat simdi ne onemli, ne bekleyebilir onu secer.", "Aufmerksamkeit waehlt, was jetzt zaehlt und was warten kann.")),
            Focus(T("терпение", "patience", "sabir", "Geduld"), T("терпение позволяет не ломать поле преждевременным ходом.", "patience keeps you from breaking the board with a premature move.", "sabir, tahtayi erken hamleyle bozmani engeller.", "Geduld schuetzt davor, das Feld mit einem verfruehten Zug zu brechen.")),
            Focus(T("скорость", "speed", "hiz", "Tempo"), T("скорость хороша после понимания, но опасна до него.", "speed is useful after understanding, but risky before it.", "hiz anlamadan sonra iyidir, oncesinde risklidir.", "Tempo ist nach dem Verstehen gut, davor riskant.")),
            Focus(T("ритм", "rhythm", "ritim", "Rhythmus"), T("ритм игры рождается из чередования поиска и открытия.", "the rhythm of play is born from searching and revealing.", "oyunun ritmi arama ve acma arasindan dogar.", "der Spielrhythmus entsteht aus Suchen und Oeffnen.")),
            Focus(T("обучение", "learning", "ogrenme", "Lernen"), T("каждая честная раскладка немного учит, даже без текста.", "every fair layout teaches a little, even without words.", "her adil dizilim soz olmadan da biraz ogretir.", "jeder faire Aufbau lehrt ein wenig, auch ohne Text.")),
            Focus(T("культура", "culture", "kultur", "Kultur"), T("символы становятся сильнее, когда за ними есть знание.", "symbols become stronger when knowledge stands behind them.", "arkasinda bilgi olan semboller guclenir.", "Symbole werden staerker, wenn Wissen hinter ihnen steht.")),
            Focus(T("история", "history", "tarih", "Geschichte"), T("старые игры живут долго, потому что тренируют простые человеческие способности.", "old games live long because they train simple human abilities.", "eski oyunlar uzun yasar, cunku temel insan yeteneklerini calistirir.", "alte Spiele leben lange, weil sie einfache menschliche Faehigkeiten trainieren.")),
            Focus(T("Китай", "China", "Cin", "China"), T("тема Китая может раскрывать не только фон, но и происхождение символов.", "the China theme can reveal not only scenery, but the origin of symbols.", "Cin temasi sadece fonu degil, sembollerin kokenini de acabilir.", "das China-Thema kann nicht nur Kulisse, sondern auch Ursprung der Symbole zeigen.")),
            Focus(T("Турция", "Turkey", "Turkiye", "Tuerkei"), T("тема Турции может говорить о беседе, терпении и настольной традиции.", "the Turkey theme can speak about conversation, patience, and table-game tradition.", "Turkiye temasi sohbeti, sabri ve masa oyunu gelenegini anlatabilir.", "das Tuerkei-Thema kann von Gespraech, Geduld und Tischspieltradition erzaehlen.")),
            Focus(T("мир", "the world", "dunya", "Welt"), T("каждая культура дает свой способ видеть порядок, терпение и связь между символами.", "every culture offers its own way to see order, patience, and connection between symbols.", "her kultur duzeni, sabri ve semboller arasindaki bagi gormek icin kendi yolunu sunar.", "jede Kultur bietet eine eigene Art, Ordnung, Geduld und Verbindung zwischen Symbolen zu sehen.")),
            Focus(T("Symbiosis", "Symbiosis", "Symbiosis", "Symbiosis"), T("смысл Endless раскрывается, когда знание, внимание и игра становятся одним движением.", "Endless becomes meaningful when knowledge, attention, and play become one movement.", "Endless, bilgi, dikkat ve oyun tek harekete donustugunde anlam kazanir.", "Endless gewinnt Sinn, wenn Wissen, Aufmerksamkeit und Spiel zu einer Bewegung werden.")),
            Focus(T("Endless Mode", "Endless Mode", "Endless Mode", "Endless Mode"), T("Endless тренирует состояние: ясность, темп и спокойное внимание.", "Endless trains a state of mind: clarity, tempo, and calm attention.", "Endless zihin halini calistirir: netlik, tempo ve sakin dikkat.", "Endless trainiert einen Zustand: Klarheit, Tempo und ruhige Aufmerksamkeit.")),
            Focus(T("честный генератор", "fair generator", "adil uretici", "fairer Generator"), T("игрок должен доверять, что раскладка имеет путь.", "the player must trust that the layout has a path.", "oyuncu dizilimin bir yolu olduguna guvenmelidir.", "der Spieler muss darauf vertrauen, dass der Aufbau einen Weg hat.")),
            Focus(T("проходимость", "solvability", "cozulebilirlik", "Loesbarkeit"), T("красивая форма недостаточна, если в ней нет решения.", "a beautiful shape is not enough if it has no solution.", "cozumu yoksa guzel sekil yeterli degildir.", "eine schoene Form reicht nicht, wenn sie keine Loesung hat.")),
            Focus(T("масштаб тайлов", "tile scale", "tas olcegi", "Steingroesse"), T("крупный тайл дает руке удовольствие, а глазам отдых.", "a larger tile gives the hand pleasure and the eyes rest.", "buyuk tas ele keyif, goze dinlenme verir.", "ein groesserer Stein gibt der Hand Freude und den Augen Ruhe.")),
            Focus(T("мобильный экран", "mobile screen", "mobil ekran", "mobiler Bildschirm"), T("на маленьком экране важны чистые края и понятные касания.", "on a small screen, clean edges and clear taps matter.", "kucuk ekranda temiz kenarlar ve net dokunuslar onemlidir.", "auf kleinem Bildschirm zaehlen klare Kanten und eindeutige Beruehrungen.")),
            Focus(T("ландшафт", "landscape", "landscape", "Landscape"), T("landscape требует широких форм, а не вертикальной тесноты.", "landscape needs wide forms, not vertical cramped shapes.", "landscape dikey sikisiklik degil, genis formlar ister.", "Landscape braucht breite Formen, keine vertikale Enge.")),
            Focus(T("награда", "reward", "odul", "Belohnung"), T("ощущение награды растет, когда каждый ход видимо меняет поле.", "reward feels stronger when every move visibly changes the board.", "her hamle tahtayi gorunur degistirince odul hissi artar.", "Belohnung fuehlt sich staerker an, wenn jeder Zug das Feld sichtbar veraendert.")),
            Focus(T("следующая мысль", "next thought", "sonraki dusunce", "naechster Gedanke"), T("новая раскладка должна продолжать опыт, а не становиться случайной стеной.", "a new layout should continue experience, not become a random wall.", "yeni dizilim deneyimi surdurmeli, rastgele duvar olmamali.", "ein neuer Aufbau sollte Erfahrung fortsetzen, nicht zur zufaelligen Wand werden.")),
            Focus(T("маленький прогресс", "small progress", "kucuk ilerleme", "kleiner Fortschritt"), T("одна точная пара уже делает игрока сильнее.", "one precise pair already makes the player stronger.", "tek dogru cift bile oyuncuyu guclendirir.", "ein praezises Paar macht den Spieler bereits staerker.")),
            Focus(T("большой путь", "long path", "buyuk yol", "langer Weg"), T("большой путь держится на маленьких честных шагах.", "a long path stands on small fair steps.", "buyuk yol kucuk adil adimlarla ayakta durur.", "ein langer Weg steht auf kleinen fairen Schritten."))
        };

        private static int TotalEntryCount => CuratedEntries.Length + Templates.Length * Focuses.Length;

        public static EndlessWisdomEntry GetForEndlessLevel(int endlessLevel)
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
            int level = Mathf.Max(1, endlessLevel);

            if (CuratedEntries.Length > 0 && level <= CuratedEntries.Length)
                return Resolve(CuratedEntries[level - 1], language);

            if (PhilosophicalEntries.Length > 0)
            {
                int index = (level - CuratedEntries.Length - 1) % PhilosophicalEntries.Length;
                return Resolve(PhilosophicalEntries[index], language);
            }

            return GetRandom();
        }

        public static EndlessWisdomEntry GetRandom()
        {
            if (TotalEntryCount == 0)
                return new EndlessWisdomEntry("Endless Thought", "Mahjong turns observation into knowledge: compare, remember, open the hidden path.");

            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;

            if (TotalEntryCount == 1)
                return Resolve(CuratedEntries[0], language);

            int index;
            do
            {
                index = Random.Range(0, TotalEntryCount);
            }
            while (index == lastIndex);

            lastIndex = index;

            if (index < CuratedEntries.Length)
                return Resolve(CuratedEntries[index], language);

            int generatedIndex = index - CuratedEntries.Length;
            WisdomTemplate template = Templates[generatedIndex / Focuses.Length];
            WisdomFocus focus = Focuses[generatedIndex % Focuses.Length];

            return new EndlessWisdomEntry(
                template.Title.Get(language),
                BuildGeneratedBody(template, focus, language));
        }

        private static string BuildGeneratedBody(WisdomTemplate template, WisdomFocus focus, GameLanguage language)
        {
            string lead = template.Lead.Get(language);
            string subject = focus.Subject.Get(language);
            string lesson = focus.Lesson.Get(language);

            return language switch
            {
                GameLanguage.English => $"{lead} Related fact: {subject}. {lesson}",
                GameLanguage.Turkish => $"{lead} Bagli bilgi: {subject}. {lesson}",
                GameLanguage.German => $"{lead} Verbundener Gedanke: {subject}. {lesson}",
                _ => $"{lead} Связанный факт - {subject}: {lesson}"
            };
        }

        private static EndlessWisdomEntry Resolve(LocalizedEntry entry, GameLanguage language)
        {
            return new EndlessWisdomEntry(entry.Title.Get(language), entry.Body.Get(language));
        }

        private static LocalizedEntry Entry(LocalizedText title, LocalizedText body)
        {
            return new LocalizedEntry(title, body);
        }

        private static WisdomTemplate Template(LocalizedText title, LocalizedText lead)
        {
            return new WisdomTemplate(title, lead);
        }

        private static WisdomFocus Focus(LocalizedText subject, LocalizedText lesson)
        {
            return new WisdomFocus(subject, lesson);
        }

        private static LocalizedText T(string russian, string english, string turkish, string german)
        {
            return new LocalizedText(russian, english, turkish, german);
        }
    }
}
