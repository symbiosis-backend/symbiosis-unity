using System;
using System.Collections.Generic;

namespace MahjongGame
{
    public static class GameLocalization
    {
        private static readonly HashSet<string> KnownLocalizedValues = new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Translation> Translations = new(StringComparer.Ordinal)
        {
            { "common.player", new Translation("Игрок", "Player", "Oyuncu") },
            { "common.loading", new Translation("Загрузка...", "Loading...", "Yukleniyor...", "Wird geladen...") },
            { "common.continue", new Translation("Продолжить", "Continue", "Devam") },
            { "common.back", new Translation("Назад", "Back", "Geri", "Zurueck") },
            { "main.orbiosis_unavailable.status", new Translation("НА ДОРАБОТКЕ", "UNDER REFINEMENT", "GELİŞTİRİLİYOR", "IN UEBERARBEITUNG") },
            { "main.orbiosis_unavailable.body", new Translation("Orbiosis временно закрыта. Мы дорабатываем игру и откроем доступ, когда текущий этап работ будет завершён.", "Orbiosis is temporarily unavailable. We are refining the game and will reopen access when the current work is complete.", "Orbiosis geçici olarak kapalı. Oyunu geliştiriyoruz ve mevcut çalışmalar tamamlandığında erişimi yeniden açacağız.", "Orbiosis ist voruebergehend geschlossen. Wir ueberarbeiten das Spiel und oeffnen den Zugang wieder, sobald die aktuellen Arbeiten abgeschlossen sind.") },
            { "main.mahjong_endless_unavailable.status", new Translation("НА ДОРАБОТКЕ", "UNDER REFINEMENT", "GELİŞTİRİLİYOR", "IN UEBERARBEITUNG") },
            { "main.mahjong_endless_unavailable.body", new Translation("Режим Mahjong Endless временно закрыт. Мы дорабатываем бесконечный режим и откроем доступ, когда он будет готов.", "Mahjong Endless is temporarily unavailable. We are refining the endless mode and will reopen access when it is ready.", "Mahjong Endless geçici olarak kapalı. Sonsuz modu geliştiriyoruz ve hazır olduğunda erişimi yeniden açacağız.", "Mahjong Endless ist voruebergehend geschlossen. Wir ueberarbeiten den Endlosmodus und oeffnen den Zugang wieder, sobald er bereit ist.") },
            { "main.feature_unavailable.status", new Translation("НА ДОРАБОТКЕ", "UNDER REFINEMENT", "GELİŞTİRİLİYOR", "IN UEBERARBEITUNG") },
            { "mail.unavailable.body", new Translation("Почта временно закрыта. Мы дорабатываем получение писем, уведомлений и наград и откроем доступ, когда система будет готова.", "Mail is temporarily unavailable. We are refining messages, notifications, and rewards and will reopen access when the system is ready.", "Posta geçici olarak kapalı. Mesajları, bildirimleri ve ödülleri geliştiriyoruz; sistem hazır olduğunda erişimi yeniden açacağız.", "Die Post ist voruebergehend geschlossen. Wir ueberarbeiten Nachrichten, Benachrichtigungen und Belohnungen und oeffnen den Zugang wieder, sobald das System bereit ist.") },
            { "common.title_empty", new Translation("Титул: -", "Title: -", "Unvan: -") },
            { "common.rank_unranked", new Translation("Ранг: без ранга", "Rank: Unranked", "Rutbe: Derecesiz") },
            { "common.unranked", new Translation("Без ранга", "Unranked", "Derecesiz") },
            { "common.oz_altin", new Translation("Оз Алтын", "Oz Gold", "Oz Altın") },

            { "intro.skip", new Translation("ПРОПУСТИТЬ", "SKIP", "ATLA") },

            { "orbiosis.top.station", new Translation("СТАНЦИЯ", "STATION", "İSTASYON", "STATION") },
            { "orbiosis.top.settings", new Translation("НАСТРОЙКИ", "SETTINGS", "AYARLAR", "SETTINGS") },
            { "orbiosis.top.base", new Translation("БАЗА", "BASE", "ÜS", "BASE") },
            { "orbiosis.hud.parts", new Translation("ЧАСТИ {0}", "PARTS {0}", "PARÇA {0}", "PARTS {0}") },
            { "orbiosis.hud.base", new Translation("БАЗА {0}/{1}", "BASE {0}/{1}", "ÜS {0}/{1}", "BASE {0}/{1}") },
            { "orbiosis.hud.shield", new Translation("ЩИТ {0}", "SHIELD {0}", "KALKAN {0}", "SHIELD {0}") },
            { "orbiosis.menu.best", new Translation("ЛУЧШИЙ СЧЁТ {0}", "BEST SCORE {0}", "EN İYİ SKOR {0}", "BEST SCORE {0}") },
            { "orbiosis.menu.technology", new Translation("ТЕХНОЛОГИИ", "TECHNOLOGY", "TEKNOLOJİ", "TECHNOLOGY") },
            { "orbiosis.menu.hangar", new Translation("АНГАР", "HANGAR", "HANGAR", "HANGAR") },
            { "orbiosis.menu.start", new Translation("СТАРТ", "START", "BAŞLA", "START") },
            { "orbiosis.menu.tutorial", new Translation("ОБУЧЕНИЕ", "TUTORIAL", "ÖĞRETİCİ", "TUTORIAL") },
            { "orbiosis.menu.bestiary", new Translation("БЕСТИАРИЙ", "BESTIARY", "BESTİYAR", "BESTIARY") },
            { "orbiosis.menu.endless", new Translation("БЕСКОНЕЧНЫЙ", "ENDLESS", "SONSUZ", "ENDLESS") },
            { "orbiosis.menu.story", new Translation("ИСТОРИЯ", "STORY", "HİKÂYE", "STORY") },
            { "orbiosis.menu.story_adventure", new Translation("STORY / ADVENTURE", "STORY / ADVENTURE", "STORY / ADVENTURE", "STORY / ADVENTURE") },
            { "orbiosis.menu.story_adventure_body", new Translation("Сюжетные главы, обучение и приключения.", "Story chapters, tutorial and adventures.", "Hikaye bolumleri, ogretici ve maceralar.", "Story chapters, tutorial and adventures.") },
            { "orbiosis.menu.chaos_mod", new Translation("CHAOS MOD", "CHAOS MOD", "CHAOS MOD", "CHAOS MOD") },
            { "orbiosis.menu.chaos_subtitle", new Translation("СВОБОДНЫЙ ВЫЛЕТ", "FREE RUN", "SERBEST UÇUŞ", "FREE RUN") },
            { "orbiosis.menu.chaos_body", new Translation("Свободная битва волн для прокачки и испытаний.", "Free wave battle for upgrades and trials.", "Yukseltmeler ve denemeler icin serbest dalga savasi.", "Free wave battle for upgrades and trials.") },
            { "orbiosis.menu.online_battle", new Translation("ONLINE BATTLE", "ONLINE BATTLE", "ONLINE BATTLE", "ONLINE BATTLE") },
            { "orbiosis.menu.online_body", new Translation("PvP-режим появится позже.", "PvP mode will arrive later.", "PvP modu daha sonra gelecek.", "PvP mode will arrive later.") },
            { "orbiosis.menu.in_development", new Translation("В РАЗРАБОТКЕ", "IN DEVELOPMENT", "GELİŞTİRİLİYOR", "IN DEVELOPMENT") },
            { "orbiosis.common.back", new Translation("НАЗАД", "BACK", "GERİ", "BACK") },
            { "orbiosis.common.open", new Translation("ОТКРЫТЬ", "OPEN", "AÇ", "OPEN") },
            { "orbiosis.common.close", new Translation("ЗАКРЫТЬ", "CLOSE", "KAPAT", "CLOSE") },
            { "orbiosis.common.continue", new Translation("ПРОДОЛЖИТЬ", "CONTINUE", "DEVAM", "CONTINUE") },
            { "orbiosis.common.cancel", new Translation("ОТМЕНА", "CANCEL", "İPTAL", "CANCEL") },
            { "orbiosis.common.page", new Translation("СТРАНИЦА {0} / {1}", "PAGE {0} / {1}", "SAYFA {0} / {1}", "PAGE {0} / {1}") },
            { "orbiosis.common.off", new Translation("ВЫКЛ", "OFF", "KAPALI", "OFF") },
            { "orbiosis.story.levels", new Translation("УРОВНИ ИСТОРИИ", "STORY LEVELS", "HİKÂYE BÖLÜMLERİ", "STORY LEVELS") },
            { "orbiosis.story.tutorial_card", new Translation("ОБУЧЕНИЕ\nПервый симбиоз: отступить, объединиться, выжить.", "TUTORIAL\nFirst symbiosis: retreat, unite, survive.", "ÖĞRETİCİ\nİlk simbiyoz: geri çekil, birleş, hayatta kal.", "TUTORIAL\nFirst symbiosis: retreat, unite, survive.") },
            { "orbiosis.story.mines_card", new Translation("МИНЫ\n10 волн - босс на 10-й волне", "MINES\n10 waves - boss on wave 10", "MAYINLAR\n10 dalga - 10. dalgada boss", "MINES\n10 waves - boss on wave 10") },
            { "orbiosis.difficulty.easy", new Translation("ЛЕГКО", "EASY", "KOLAY", "EASY") },
            { "orbiosis.difficulty.medium", new Translation("СРЕД", "MED", "ORTA", "MED") },
            { "orbiosis.difficulty.hardcore", new Translation("ХАРД", "HARD", "ZOR", "HARD") },
            { "orbiosis.tutorial_intro.title", new Translation("ДОБРО ПОЖАЛОВАТЬ\nВ ORBIOSIS", "WELCOME\nTO ORBIOSIS", "ORBIOSIS'E\nHOŞ GELDİN", "WELCOME\nTO ORBIOSIS") },
            { "orbiosis.tutorial_intro.body", new Translation("Пройди обучение.\n\nПосле него откроются:\nрежимы, история,\nтехнологии и ангар.", "Complete the tutorial.\n\nAfter it you unlock:\nmodes, story,\ntechnology and hangar.", "Öğreticiyi tamamla.\n\nSonrasında açılır:\nmodlar, hikâye,\nteknoloji ve hangar.", "Complete the tutorial.\n\nAfter it you unlock:\nmodes, story,\ntechnology and hangar.") },
            { "orbiosis.tutorial.material_counter", new Translation("ДЕТАЛИ {0}/{1}   ГРУЗ {2}", "PARTS {0}/{1}   CARGO {2}", "PARÇA {0}/{1}   YÜK {2}", "PARTS {0}/{1}   CARGO {2}") },
            { "orbiosis.tutorial_result.title", new Translation("ОБУЧЕНИЕ ПРОЙДЕНО", "TUTORIAL COMPLETE", "ÖĞRETİCİ TAMAMLANDI", "TUTORIAL COMPLETE") },
            { "orbiosis.tutorial_result.body", new Translation("Первый симбиоз удержан.\nБаза готова к свободным вылетам, технологиям и ангару.", "First symbiosis held.\nThe base is ready for free flights, technology and hangar work.", "İlk simbiyoz korundu.\nÜs serbest uçuşlara, teknolojiye ve hangar çalışmalarına hazır.", "First symbiosis held.\nThe base is ready for free flights, technology and hangar work.") },
            { "orbiosis.tutorial_result.rewards", new Translation("ОСТАЁТСЯ: Orb Cannon, Crafter Drone и первая эволюция Orb.\nВременные боевые модули и дроны сброшены.", "KEPT: Orb Cannon, Crafter Drone and the first Orb evolution.\nTemporary combat modules and drones were reset.", "KALAN: Orb Topu, Üretici Dron ve Orb'un ilk evrimi.\nGeçici savaş modülleri ve dronlar sıfırlandı.", "KEPT: Orb Cannon, Crafter Drone and the first Orb evolution.\nTemporary combat modules and drones were reset.") },
            { "orbiosis.tutorial_result.complete", new Translation("ПРОЙДЕНО", "COMPLETE", "TAMAMLANDI", "COMPLETE") },
            { "orbiosis.settings.title", new Translation("НАСТРОЙКИ ORBIOSIS", "ORBIOSIS SETTINGS", "ORBIOSIS AYARLARI", "ORBIOSIS SETTINGS") },
            { "orbiosis.settings.sound_on", new Translation("ЗВУК: ВКЛ", "SOUND: ON", "SES: AÇIK", "SOUND: ON") },
            { "orbiosis.settings.sound_off", new Translation("ЗВУК: ВЫКЛ", "SOUND: OFF", "SES: KAPALI", "SOUND: OFF") },
            { "orbiosis.settings.music_on", new Translation("МУЗЫКА: ВКЛ", "MUSIC: ON", "MÜZİK: AÇIK", "MUSIC: ON") },
            { "orbiosis.settings.music_off", new Translation("МУЗЫКА: ВЫКЛ", "MUSIC: OFF", "MÜZİK: KAPALI", "MUSIC: OFF") },
            { "orbiosis.settings.test_on", new Translation("TEST: ВКЛ", "TEST: ON", "TEST: AÇIK", "TEST: ON") },
            { "orbiosis.settings.test_off", new Translation("TEST: ВЫКЛ", "TEST: OFF", "TEST: KAPALI", "TEST: OFF") },
            { "orbiosis.settings.reset", new Translation("СБРОСИТЬ ПРОГРЕСС", "RESET PROGRESS", "İLERLEMEYİ SIFIRLA", "RESET PROGRESS") },
            { "orbiosis.settings.back_menu", new Translation("ВЕРНУТЬСЯ В МЕНЮ", "RETURN TO MENU", "MENÜYE DÖN", "RETURN TO MENU") },
            { "orbiosis.settings.back_platform", new Translation("НАЗАД НА ПЛАТФОРМУ", "BACK TO PLATFORM", "PLATFORMA GERİ DÖN", "BACK TO PLATFORM") },
            { "orbiosis.gameover.title", new Translation("ОРБИТА ПОТЕРЯНА", "ORBIT LOST", "YÖRÜNGE KAYBEDİLDİ", "ORBIT LOST") },
            { "orbiosis.gameover.body", new Translation("Станция восстановится.", "The station will regrow.", "İstasyon yeniden büyüyecek.", "The station will regrow.") },
            { "orbiosis.gameover.result", new Translation("СЧЁТ {0}   ЛУЧШИЙ {1}", "SCORE {0}   BEST {1}", "SKOR {0}   EN İYİ {1}", "SCORE {0}   BEST {1}") },
            { "orbiosis.gameover.result_core", new Translation("СЧЁТ {0}   ЛУЧШИЙ {1}   ЯДРО +{2}", "SCORE {0}   BEST {1}   CORE +{2}", "SKOR {0}   EN İYİ {1}   ÇEKİRDEK +{2}", "SCORE {0}   BEST {1}   CORE +{2}") },
            { "orbiosis.gameover.restart", new Translation("ЗАНОВО", "RESTART", "YENİDEN", "RESTART") },
            { "orbiosis.gameover.main", new Translation("ГЛАВНАЯ", "MAIN", "ANA", "MAIN") },
            { "orbiosis.gameover.second_chance", new Translation("ВТОРОЙ ШАНС", "SECOND CHANCE", "İKİNCİ ŞANS", "SECOND CHANCE") },
            { "orbiosis.gameover.menu", new Translation("В МЕНЮ", "TO MENU", "MENÜYE DÖN", "TO MENU") },
            { "orbiosis.station.title", new Translation("УПРАВЛЕНИЕ СТАНЦИЕЙ", "STATION CONTROL", "İSTASYON KONTROLÜ", "STATION CONTROL") },
            { "orbiosis.station.header", new Translation("ПРИОРИТЕТ ЗАДАЧ ДРОНОВ", "DRONE TASK PRIORITY", "DRON GÖREV ÖNCELİĞİ", "DRONE TASK PRIORITY") },
            { "orbiosis.station.tab.orb", new Translation("ORB", "ORB", "ORB", "ORB") },
            { "orbiosis.station.tab.base", new Translation("БАЗА", "BASE", "ÜS", "BASE") },
            { "orbiosis.station.tab.drones", new Translation("ДРОНЫ", "DRONES", "DRONLAR", "DRONES") },
            { "orbiosis.station.tab.modules", new Translation("МОДУЛИ", "MODULES", "MODÜLLER", "MODULES") },
            { "orbiosis.station.catalog", new Translation("КАТАЛОГ СТАНЦИИ", "STATION CATALOG", "İSTASYON KATALOĞU", "STATION CATALOG") },
            { "orbiosis.station.select", new Translation("ВЫБРАТЬ", "SELECT", "SEÇ", "SELECT") },
            { "orbiosis.station.back_to_cards", new Translation("К КАРТОЧКАМ", "TO CARDS", "KARTLARA", "TO CARDS") },
            { "orbiosis.station.module_active", new Translation("СОСТОЯНИЕ МОДУЛЯ", "MODULE STATE", "MODÜL DURUMU", "MODULE STATE") },
            { "orbiosis.station.active", new Translation("АКТИВНЫЙ", "ACTIVE", "AKTİF", "ACTIVE") },
            { "orbiosis.station.no_priorities", new Translation("У ЭТОГО ОБЪЕКТА ПОКА НЕТ ОТДЕЛЬНЫХ ПРИОРИТЕТОВ.", "THIS OBJECT DOES NOT HAVE SEPARATE PRIORITIES YET.", "BU NESNENİN HENÜZ AYRI ÖNCELİKLERİ YOK.", "THIS OBJECT DOES NOT HAVE SEPARATE PRIORITIES YET.") },
            { "orbiosis.station.no_unlocked", new Translation("ПОКА НИЧЕГО НЕ КУПЛЕНО В ТЕХНОЛОГИЯХ.", "NOTHING HAS BEEN BOUGHT IN TECHNOLOGY YET.", "TEKNOLOJİDE HENÜZ BİR ŞEY ALINMADI.", "NOTHING HAS BEEN BOUGHT IN TECHNOLOGY YET.") },
            { "orbiosis.station.modules.empty", new Translation("МОДУЛЬНЫЕ ПРИОРИТЕТЫ БУДУТ ЗДЕСЬ", "MODULE PRIORITIES WILL LIVE HERE", "MODÜL ÖNCELİKLERİ BURADA OLACAK", "MODULE PRIORITIES WILL LIVE HERE") },
            { "orbiosis.station.task.crafter_repair", new Translation("КРАФТЕР: СНАЧАЛА РЕМОНТ", "CRAFTER: REPAIR FIRST", "ÜRETİCİ: ÖNCE ONARIM", "CRAFTER: REPAIR FIRST") },
            { "orbiosis.station.task.crafter_generator", new Translation("КРАФТЕР: СНАЧАЛА ГЕНЕРАТОР", "CRAFTER: GENERATOR FIRST", "ÜRETİCİ: ÖNCE JENERATÖR", "CRAFTER: GENERATOR FIRST") },
            { "orbiosis.station.task.crafter_shield", new Translation("КРАФТЕР: СНАЧАЛА ЩИТ", "CRAFTER: SHIELD FIRST", "ÜRETİCİ: ÖNCE KALKAN", "CRAFTER: SHIELD FIRST") },
            { "orbiosis.station.task.crafter_collector", new Translation("КРАФТЕР: СНАЧАЛА СБОРЩИК", "CRAFTER: COLLECTOR FIRST", "ÜRETİCİ: ÖNCE TOPLAYICI", "CRAFTER: COLLECTOR FIRST") },
            { "orbiosis.station.task.repair_orb", new Translation("РЕМОНТ: ORB", "REPAIR: ORB", "ONARIM: ORB", "REPAIR: ORB") },
            { "orbiosis.station.task.repair_drones", new Translation("РЕМОНТ: ДРОНЫ", "REPAIR: DRONES", "ONARIM: DRONLAR", "REPAIR: DRONES") },
            { "orbiosis.station.task.generator_orb", new Translation("ГЕНЕРАТОР: ORB", "GENERATOR: ORB", "JENERATÖR: ORB", "GENERATOR: ORB") },
            { "orbiosis.station.task.generator_drones", new Translation("ГЕНЕРАТОР: ДРОНЫ", "GENERATOR: DRONES", "JENERATÖR: DRONLAR", "GENERATOR: DRONES") },
            { "orbiosis.station.task.shield_base", new Translation("ЩИТ: ЗАЩИТА БАЗЫ", "SHIELD: BASE DEFENSE", "KALKAN: ÜS SAVUNMASI", "SHIELD: BASE DEFENSE") },
            { "orbiosis.upgrade.title", new Translation("ТЕХНОЛОГИИ", "TECHNOLOGY", "TEKNOLOJİ", "TECHNOLOGY") },
            { "orbiosis.upgrade.core_parts", new Translation("ЯДРО {0}   ЧАСТИ {1}", "CORE {0}   PARTS {1}", "ÇEKİRDEK {0}   PARÇA {1}", "CORE {0}   PARTS {1}") },
            { "orbiosis.upgrade.station", new Translation("СТАНЦИЯ", "STATION", "İSTASYON", "STATION") },
            { "orbiosis.upgrade.base", new Translation("БАЗА", "BASE", "ÜS", "BASE") },
            { "orbiosis.upgrade.drones", new Translation("ДРОНЫ", "DRONES", "DRONLAR", "DRONES") },
            { "orbiosis.upgrade.modules", new Translation("МОДУЛИ", "MODULES", "MODÜLLER", "MODULES") },
            { "orbiosis.upgrade.evolution", new Translation("ЭВОЛЮЦИЯ", "EVOLUTION", "EVRİM", "EVOLUTION") },
            { "orbiosis.upgrade.base_tech", new Translation("ТЕХНОЛОГИИ БАЗЫ", "BASE TECHNOLOGIES", "ÜS TEKNOLOJİLERİ", "BASE TECHNOLOGIES") },
            { "orbiosis.upgrade.base_tech_body", new Translation("Броня, двери ангара и системы слотов будут здесь.", "Armor, hangar doors and slot systems will live here.", "Zırh, hangar kapıları ve yuva sistemleri burada olacak.", "Armor, hangar doors and slot systems will live here.") },
            { "orbiosis.upgrade.core_cannon", new Translation("ЯДЕРНАЯ ПУШКА", "CORE CANNON", "ÇEKİRDEK TOPU", "CORE CANNON") },
            { "orbiosis.upgrade.missile", new Translation("РАКЕТЫ", "MISSILE", "FÜZE", "MISSILE") },
            { "orbiosis.upgrade.laser", new Translation("ЛАЗЕР", "LASER", "LAZER", "LASER") },
            { "orbiosis.upgrade.arc", new Translation("ДУГА", "ARC", "ARK", "ARC") },
            { "orbiosis.upgrade.rail", new Translation("РЕЛЬС", "RAIL", "RAY", "RAIL") },
            { "orbiosis.upgrade.crafter", new Translation("КРАФТЕР", "CRAFTER", "ÜRETİCİ", "CRAFTER") },
            { "orbiosis.upgrade.generator", new Translation("ГЕНЕРАТОР", "GENERATOR", "JENERATÖR", "GENERATOR") },
            { "orbiosis.upgrade.repair", new Translation("РЕМОНТ", "REPAIR", "ONARIM", "REPAIR") },
            { "orbiosis.upgrade.shield", new Translation("ЩИТ", "SHIELD", "KALKAN", "SHIELD") },
            { "orbiosis.upgrade.collector", new Translation("СБОРЩИК", "COLLECTOR", "TOPLAYICI", "COLLECTOR") },
            { "orbiosis.upgrade.mine_layer", new Translation("МИНЁР", "MINE LAYER", "MAYINCI", "MINE LAYER") },
            { "orbiosis.upgrade.core_miner", new Translation("ДОБЫТЧИК ЯДРА", "CORE MINER", "ÇEKİRDEK MADENCİSİ", "CORE MINER") },
            { "orbiosis.upgrade.hive", new Translation("УЛЕЙ", "HIVE", "KOVAN", "HIVE") },
            { "orbiosis.upgrade.gladiator", new Translation("ГЛАДИАТОР", "GLADIATOR", "GLADYATÖR", "GLADIATOR") },
            { "orbiosis.upgrade.horizon", new Translation("ГОРИЗОНТ", "HORIZON", "UFUK", "HORIZON") },
            { "orbiosis.upgrade.tamer", new Translation("УКРОТИТЕЛЬ", "TAMER", "EVCİLLEŞTİRİCİ", "TAMER") },
            { "orbiosis.upgrade.queen", new Translation("HELIN", "HELIN", "HELIN", "HELIN") },
            { "orbiosis.upgrade.mimic", new Translation("МИМИК", "MIMIC", "MİMİK", "MIMIC") },
            { "orbiosis.upgrade.sniper", new Translation("СНАЙПЕР", "SNIPER", "KESKİN NİŞANCI", "SNIPER") },
            { "orbiosis.upgrade.core_forge", new Translation("КУЗНИЦА ЯДРА", "CORE FORGE", "ÇEKİRDEK OCAĞI", "CORE FORGE") },
            { "orbiosis.upgrade.storage_depot", new Translation("СКЛАД", "STORAGE", "DEPO", "STORAGE") },
            { "orbiosis.upgrade.drone_pad", new Translation("ПЛОЩАДКА ДРОНОВ", "DRONE PAD", "DRON PLATFORMU", "DRONE PAD") },
            { "orbiosis.upgrade.outpost_pad", new Translation("ПЛОЩАДКА АВАНПОСТА", "OUTPOST PAD", "KARAKOL PLATFORMU", "OUTPOST PAD") },
            { "orbiosis.storage.title", new Translation("СКЛАД ЗАПАСОВ", "STORAGE DEPOT", "DEPO", "STORAGE DEPOT") },
            { "orbiosis.storage.stock", new Translation("ЗАПАСЫ СКЛАДА\nЧАСТИ: {0}\nЯДРО: {1}\nСТАРТ БОЯ: +{2} ЧАСТЕЙ", "WAREHOUSE STOCK\nPARTS: {0}\nCORE: {1}\nBATTLE START: +{2} PARTS", "DEPO STOĞU\nPARÇA: {0}\nÇEKİRDEK: {1}\nSAVAŞ BAŞLANGICI: +{2} PARÇA", "WAREHOUSE STOCK\nPARTS: {0}\nCORE: {1}\nBATTLE START: +{2} PARTS") },
            { "orbiosis.core_forge.modules_title", new Translation("КУЗНИЦА МОДУЛЕЙ", "MODULE FORGE", "MODÜL OCAĞI", "MODULE FORGE") },
            { "orbiosis.core_forge.select_body", new Translation("Выбери орудийный модуль для прокачки.", "Choose a weapon module to upgrade.", "Yükseltilecek silah modülünü seç.", "Choose a weapon module to upgrade.") },
            { "orbiosis.core_forge.select_module", new Translation("ВЫБРАТЬ МОДУЛЬ", "SELECT MODULE", "MODÜL SEÇ", "SELECT MODULE") },
            { "orbiosis.core_forge.back_to_modules", new Translation("К МОДУЛЯМ", "MODULES", "MODÜLLER", "MODULES") },
            { "orbiosis.core_forge.module_status", new Translation("УР {0}/{1}  ЯДРО {2}", "LV {0}/{1}  CORE {2}", "SV {0}/{1}  ÇEKİRDEK {2}", "LV {0}/{1}  CORE {2}") },
            { "orbiosis.upgrade.desc.crafter", new Translation("Строит дронов поддержки.", "Builds support drones.", "Destek dronları üretir.", "Builds support drones.") },
            { "orbiosis.upgrade.desc.generator", new Translation("Заряжает щиты.", "Charges shields.", "Kalkanları şarj eder.", "Charges shields.") },
            { "orbiosis.upgrade.desc.repair", new Translation("Ремонтирует Orb и дронов.", "Repairs Orb and drones.", "Orb'u ve dronları onarır.", "Repairs Orb and drones.") },
            { "orbiosis.upgrade.desc.shield", new Translation("Принимает удары спереди.", "Blocks hits in front.", "Önden gelen darbeleri engeller.", "Blocks hits in front.") },
            { "orbiosis.upgrade.desc.collector", new Translation("Собирает свободные OzParts.", "Collects loose OzParts.", "Boştaki OzParts parçalarını toplar.", "Collects loose OzParts.") },
            { "orbiosis.upgrade.desc.mine_layer", new Translation("Строит линию мин спереди.", "Builds a front mine line.", "Önde mayın hattı kurar.", "Builds a front mine line.") },
            { "orbiosis.upgrade.desc.core_miner", new Translation("Летает за ядром в дальний рейс.", "Runs long core trips.", "Uzak çekirdek seferleri yapar.", "Runs long core trips.") },
            { "orbiosis.upgrade.desc.hive", new Translation("Строит мини-турели перед базой.", "Builds mini turrets in front of the base.", "Üssün önünde mini taretler üretir.", "Builds mini turrets in front of the base.") },
            { "orbiosis.upgrade.desc.gladiator", new Translation("Ближний дрон с режущей дугой и отбрасыванием.", "Melee drone with a cutting arc and knockback.", "Kesici yaylı ve geri itmeli yakın dövüş dronu.", "Melee drone with a cutting arc and knockback.") },
            { "orbiosis.upgrade.desc.horizon", new Translation("Разведчик перед базой: открывает дальние цели оружию и сборщику.", "Forward scout: reveals distant targets for weapons and collector.", "İleri keşif: uzak hedefleri silahlara ve toplayıcıya açar.", "Forward scout: reveals distant targets for weapons and collector.") },
            { "orbiosis.upgrade.desc.tamer", new Translation("Парализует слабую цель и превращает её в симбиота.", "Paralyzes a weak target and turns it into a symbiote.", "Zayıf hedefi felç eder ve simbiyota çevirir.", "Paralyzes a weak target and turns it into a symbiote.") },
            { "orbiosis.upgrade.desc.queen", new Translation("Helin: командный премиум-дрон. Переносимая аура усиливает основные дроны и модули.", "Helin: premium command drone. Her movable aura buffs main drones and modules.", "Helin: premium komuta dronu. Taşınabilir aurası ana dronları ve modülleri güçlendirir.", "Helin: premium command drone. Her movable aura buffs main drones and modules.") },
            { "orbiosis.upgrade.desc.mimic", new Translation("Премиум-дрон свиты Helin. Наведи зелёный прицел на дрона, чтобы скопировать его с 50% эффективности.", "Premium drone from Helin's retinue. Aim the green reticle at a drone to copy it at 50% efficiency.", "Helin'in maiyetinden premium dron. Yeşil nişangahı bir drona tutarak onu %50 verimle kopyalar.", "Premium drone from Helin's retinue. Aim the green reticle at a drone to copy it at 50% efficiency.") },
            { "orbiosis.upgrade.desc.sniper", new Translation("Дальний боевой дрон. Перетащи красный прицел по карте и отпусти, чтобы выстрелить.", "Long-range combat drone. Drag the red reticle across the map and release to fire.", "Uzun menzilli savaş dronu. Kırmızı nişangahı haritada sürükle ve ateş etmek için bırak.", "Long-range combat drone. Drag the red reticle across the map and release to fire.") },
            { "orbiosis.upgrade.desc.core_forge", new Translation("Во время боя перерабатывает 10 частей в 1 ядро за 60 секунд.", "During battle, converts 10 parts into 1 core over 60 seconds.", "Savaşta 10 parçayı 60 saniyede 1 çekirdeğe dönüştürür.", "During battle, converts 10 parts into 1 core over 60 seconds.") },
            { "orbiosis.upgrade.desc.storage_depot", new Translation("Хранит детали и ядро. В бою складывает добычу и питает кузницу.", "Stores parts and core. In battle it banks salvage and feeds the forge.", "Parçaları ve çekirdeği saklar. Savaşta ganimeti depolar ve ocağı besler.", "Stores parts and core. In battle it banks salvage and feeds the forge.") },
            { "orbiosis.upgrade.desc.drone_pad", new Translation("Перетащи дрона на площадку, чтобы открыть модернизацию.", "Drag a drone onto this pad to open upgrades.", "Yükseltmeleri açmak için bir dronu platforma sürükle.", "Drag a drone onto this pad to open upgrades.") },
            { "orbiosis.upgrade.desc.outpost_pad", new Translation("Открывает площадку базы: в ангаре улучшает дронов, а в бою Crafter строит на ней аванпост.", "Unlocks the base pad: upgrades drones in the hangar, and lets Crafter build an outpost during battle.", "Üs platformunu açar: hangarda dronları yükseltir, savaşta Crafter bununla karakol kurar.", "Unlocks the base pad: upgrades drones in the hangar, and lets Crafter build an outpost during battle.") },
            { "orbiosis.upgrade.desc.core_cannon", new Translation("Оружие базовой станции.", "Base station weapon.", "Üs istasyonu silahı.", "Base station weapon.") },
            { "orbiosis.upgrade.desc.missile", new Translation("Самонаводящаяся ракетная установка.", "Homing rocket launcher.", "Güdümlü roket rampası.", "Homing rocket launcher.") },
            { "orbiosis.upgrade.desc.laser", new Translation("Непрерывный лучевой модуль.", "Burning beam module.", "Sürekli ışın modülü.", "Burning beam module.") },
            { "orbiosis.upgrade.desc.arc", new Translation("Прыгает между врагами.", "Jumps between enemies.", "Düşmanlar arasında sıçrar.", "Jumps between enemies.") },
            { "orbiosis.upgrade.desc.rail", new Translation("Пробивающий точный выстрел.", "Piercing rail strike.", "Delici hassas ray atışı.", "Piercing rail strike.") },
            { "orbiosis.upgrade.parts.crafter", new Translation("Крафтит ремонт, щит, сборщик, мины", "Crafts repair, shield, collector, mines", "Onarım, kalkan, toplayıcı ve mayın üretir", "Crafts repair, shield, collector, mines") },
            { "orbiosis.upgrade.parts.generator", new Translation("Цена крафта: {0} частей", "Craft cost: {0} parts", "Üretim bedeli: {0} parça", "Craft cost: {0} parts") },
            { "orbiosis.upgrade.parts.repair", new Translation("Ремонт: 1 часть = {0} HP", "Repair: 1 part = {0} HP", "Onarım: 1 parça = {0} CP", "Repair: 1 part = {0} HP") },
            { "orbiosis.upgrade.parts.shield", new Translation("Цена крафта: {0} частей", "Craft cost: {0} parts", "Üretim bedeli: {0} parça", "Craft cost: {0} parts") },
            { "orbiosis.upgrade.parts.collector", new Translation("Груз: {0} частей", "Carries: {0} parts", "Taşıma: {0} parça", "Carries: {0} parts") },
            { "orbiosis.upgrade.parts.mine_layer", new Translation("Цена мины: {0} часть", "Mine cost: {0} part", "Mayın bedeli: {0} parça", "Mine cost: {0} part") },
            { "orbiosis.upgrade.parts.core_miner", new Translation("Возвращает: +{0} ядра", "Returns: +{0} core", "Döndürür: +{0} çekirdek", "Returns: +{0} core") },
            { "orbiosis.upgrade.parts.hive", new Translation("Улей: {0} частей, турель: {1} часть", "Hive: {0} parts, turret: {1} part", "Kovan: {0} parça, taret: {1} parça", "Hive: {0} parts, turret: {1} part") },
            { "orbiosis.upgrade.parts.gladiator", new Translation("Крафт: {0} частей, ближняя дуга", "Craft: {0} parts, melee arc", "Üretim: {0} parça, yakın yay", "Craft: {0} parts, melee arc") },
            { "orbiosis.upgrade.parts.horizon", new Translation("Крафт: {0} частей, заправка: {1} / {2} сек", "Craft: {0} parts, fuel: {1} / {2}s", "Üretim: {0} parça, yakıt: {1} / {2} sn", "Craft: {0} parts, fuel: {1} / {2}s") },
            { "orbiosis.upgrade.parts.tamer", new Translation("Крафт: {0} частей, захват: {1} часть", "Craft: {0} parts, capture: {1} part", "Üretim: {0} parça, yakalama: {1} parça", "Craft: {0} parts, capture: {1} part") },
            { "orbiosis.upgrade.parts.queen", new Translation("Крафт: {0} частей, радиус: {1}", "Craft: {0} parts, radius: {1}", "Üretim: {0} parça, yarıçap: {1}", "Craft: {0} parts, radius: {1}") },
            { "orbiosis.upgrade.parts.mimic", new Translation("Крафт: {0} частей, копия: {1}%", "Craft: {0} parts, copy: {1}%", "Üretim: {0} parça, kopya: %{1}", "Craft: {0} parts, copy: {1}%") },
            { "orbiosis.upgrade.parts.sniper", new Translation("Крафт: {0} частей, выстрел: {1} часть", "Craft: {0} parts, shot: {1} part", "Üretim: {0} parça, atış: {1} parça", "Craft: {0} parts, shot: {1} part") },
            { "orbiosis.upgrade.parts.core_forge", new Translation("Переработка частей в ядро", "Converts parts into core", "Parçaları çekirdeğe dönüştürür", "Converts parts into core") },
            { "orbiosis.upgrade.parts.storage_depot", new Translation("Старт боя: +{0} частей", "Battle start: +{0} parts", "Savaş başlangıcı: +{0} parça", "Battle start: +{0} parts") },
            { "orbiosis.upgrade.parts.outpost_pad", new Translation("Ангар и аванпост Crafter", "Hangar and Crafter outpost", "Hangar ve Crafter karakolu", "Hangar and Crafter outpost") },
            { "orbiosis.upgrade.parts.installed", new Translation("Установлено по умолчанию", "Installed by default", "Varsayılan kurulu", "Installed by default") },
            { "orbiosis.upgrade.parts.missile", new Translation("Боезапас: 1 часть / 4 выстрела", "Ammo: 1 part / 4 shots", "Cephane: 1 parça / 4 atış", "Ammo: 1 part / 4 shots") },
            { "orbiosis.upgrade.parts.laser", new Translation("Тратит части при стрельбе", "Uses parts while firing", "Ateş ederken parça harcar", "Uses parts while firing") },
            { "orbiosis.upgrade.parts.module_shot", new Translation("Боезапас: 1 часть / выстрел", "Ammo: 1 part / shot", "Cephane: 1 parça / atış", "Ammo: 1 part / shot") },
            { "orbiosis.upgrade.status.max", new Translation("МАКС", "MAX", "MAKS", "MAX") },
            { "orbiosis.upgrade.status.evolve_free", new Translation("ЭВОЛЮЦИЯ БЕСПЛАТНО", "EVOLVE FREE", "ÜCRETSİZ EVRİM", "EVOLVE FREE") },
            { "orbiosis.upgrade.status.evolve_core", new Translation("ЭВОЛЮЦИЯ   ЯДРО {0}", "EVOLVE   CORE {0}", "EVRİM   ÇEKİRDEK {0}", "EVOLVE   CORE {0}") },
            { "orbiosis.upgrade.status.shield_system", new Translation("СИСТЕМА ЩИТА\nУР {0}  ЯДРО {1}", "SHIELD SYSTEM\nLV {0}  CORE {1}", "KALKAN SİSTEMİ\nSV {0}  ÇEKİRDEK {1}", "SHIELD SYSTEM\nLV {0}  CORE {1}") },
            { "orbiosis.upgrade.status.installed", new Translation("УСТАНОВЛЕНО", "INSTALLED", "KURULU", "INSTALLED") },
            { "orbiosis.upgrade.status.owned", new Translation("КУПЛЕНО", "OWNED", "ALINDI", "OWNED") },
            { "orbiosis.upgrade.status.buy_free", new Translation("КУПИТЬ БЕСПЛАТНО", "BUY FREE", "ÜCRETSİZ AL", "BUY FREE") },
            { "orbiosis.upgrade.status.core", new Translation("ЯДРО {0}", "CORE {0}", "ÇEKİRDEK {0}", "CORE {0}") },
            { "orbiosis.upgrade.status.locked", new Translation("ЗАКРЫТО", "LOCKED", "KİLİTLİ", "LOCKED") },
            { "orbiosis.upgrade.status.core_miner_locked", new Translation("ДОБЫТЧИК ЯДРА ЗАКРЫТ", "CORE MINER LOCKED", "ÇEKİRDEK MADENCİSİ KİLİTLİ", "CORE MINER LOCKED") },
            { "orbiosis.upgrade.status.collect_core", new Translation("ЗАБРАТЬ +{0} ЯДРА", "COLLECT +{0} CORE", "+{0} ÇEKİRDEK AL", "COLLECT +{0} CORE") },
            { "orbiosis.upgrade.status.mining", new Translation("ДОБЫЧА {0}", "MINING {0}", "MADENCİLİK {0}", "MINING {0}") },
            { "orbiosis.upgrade.status.send_core_miner", new Translation("ОТПРАВИТЬ ДОБЫТЧИКА", "SEND CORE MINER", "ÇEKİRDEK MADENCİSİNİ GÖNDER", "SEND CORE MINER") },
            { "orbiosis.hangar.upgrade", new Translation("УЛУЧШИТЬ", "UPGRADE", "YÜKSELT", "UPGRADE") },
            { "orbiosis.hangar.max", new Translation("МАКС", "MAX", "MAKS", "MAX") },
            { "orbiosis.hangar.max_level", new Translation("МАКС. УРОВЕНЬ", "MAX LEVEL", "MAKS. SEVİYE", "MAX LEVEL") },
            { "orbiosis.hangar.next_upgrade", new Translation("СЛЕДУЮЩЕЕ УЛУЧШЕНИЕ: ЯДРО {0}", "NEXT UPGRADE: CORE {0}", "SONRAKİ YÜKSELTME: ÇEKİRDEK {0}", "NEXT UPGRADE: CORE {0}") },
            { "orbiosis.hangar.body", new Translation("{0}\n\nУР {1} / {2}\n{3}\nОСКОЛКИ ЯДРА: {4}", "{0}\n\nLV {1} / {2}\n{3}\nCORE SHARDS: {4}", "{0}\n\nSV {1} / {2}\n{3}\nÇEKİRDEK PARÇASI: {4}", "{0}\n\nLV {1} / {2}\n{3}\nCORE SHARDS: {4}") },
            { "orbiosis.hangar.action_upgrade", new Translation("УЛУЧШИТЬ  ЯДРО {0}", "UPGRADE  CORE {0}", "YÜKSELT  ÇEKİRDEK {0}", "UPGRADE  CORE {0}") },
            { "orbiosis.hangar.profile.missile.body", new Translation("Самонаводящийся модуль. Улучшения повысят наведение, перезарядку и мощность боеголовки.", "Guided launcher module. Future upgrades can improve tracking, reload speed and warhead power.", "Güdümlü fırlatıcı modül. Yükseltmeler takip, yeniden yükleme ve başlık gücünü artırır.", "Guided launcher module. Future upgrades can improve tracking, reload speed and warhead power.") },
            { "orbiosis.hangar.profile.laser.body", new Translation("Непрерывный лучевой модуль. Улучшения повысят фокус, охлаждение и дальность.", "Continuous beam module. Future upgrades can improve beam focus, heat control and range.", "Sürekli ışın modülü. Yükseltmeler odak, ısı kontrolü ve menzili artırır.", "Continuous beam module. Future upgrades can improve beam focus, heat control and range.") },
            { "orbiosis.hangar.profile.arc.body", new Translation("Электрическая цепь. Улучшения увеличат прыжки, оглушение и урон дуги.", "Electric chain module. Future upgrades can increase jump count, stun pressure and arc damage.", "Elektrik zinciri modülü. Yükseltmeler sıçrama sayısı, sersemletme baskısı ve ark hasarını artırır.", "Electric chain module. Future upgrades can increase jump count, stun pressure and arc damage.") },
            { "orbiosis.hangar.profile.rail.body", new Translation("Пробивающий точный модуль. Улучшения ускорят заряд, пробитие и критические попадания.", "Piercing precision module. Future upgrades can improve charge time, penetration and critical hits.", "Delici hassas modül. Yükseltmeler şarj süresi, delme ve kritik vuruşları geliştirir.", "Piercing precision module. Future upgrades can improve charge time, penetration and critical hits.") },
            { "orbiosis.hangar.profile.crafter.body", new Translation("Строит системы поддержки и поддерживает производственную линию ангара.", "Builds support systems and keeps the hangar production line alive.", "Destek sistemleri kurar ve hangar üretim hattını canlı tutar.", "Builds support systems and keeps the hangar production line alive.") },
            { "orbiosis.hangar.profile.generator.body", new Translation("Заряжает щиты и питает защитные системы изнутри базы.", "Charges shields and powers defensive systems from inside the base.", "Kalkanları şarj eder ve üssün içinden savunma sistemlerini besler.", "Charges shields and powers defensive systems from inside the base.") },
            { "orbiosis.hangar.profile.repair.body", new Translation("Сначала ремонтирует пристыкованный Orb, затем восстанавливает броню базы из ангара.", "Repairs the orb first when docked, then restores base armor from inside the hangar.", "Kenetlenmiş Orb'u önce onarır, sonra hangardan üs zırhını yeniler.", "Repairs the orb first when docked, then restores base armor from inside the hangar.") },
            { "orbiosis.hangar.profile.shield.body", new Translation("Защитный дрон сопровождения. Улучшения усилят барьер и перехват.", "Defensive escort drone. Future upgrades can improve barrier strength and intercept range.", "Savunma eskort dronu. Yükseltmeler bariyer gücünü ve önleme menzilini artırır.", "Defensive escort drone. Future upgrades can improve barrier strength and intercept range.") },
            { "orbiosis.hangar.profile.collector.body", new Translation("Собирает части и разгружает груз в отсек ангара.", "Collects parts and unloads cargo into the hangar bay.", "Parçaları toplar ve yükü hangar bölmesine boşaltır.", "Collects parts and unloads cargo into the hangar bay.") },
            { "orbiosis.hangar.profile.core_miner.body", new Translation("Дальний добытчик, который приносит осколки ядра на базу.", "Long-range miner that brings core shards back to the base.", "Çekirdek parçalarını üsse getiren uzun menzilli madenci.", "Long-range miner that brings core shards back to the base.") },
            { "orbiosis.hangar.profile.hive.body", new Translation("Производственный боевой дрон: держит линию мини-турелей перед базой.", "Combat production drone: keeps a mini-turret line in front of the base.", "Savaş üretim dronu: üssün önünde mini taret hattı kurar.", "Combat production drone: keeps a mini-turret line in front of the base.") },
            { "orbiosis.hangar.profile.gladiator.body", new Translation("Ближний перехватчик: режет врагов перед базой и отбрасывает выживших.", "Melee interceptor: cuts enemies in front of the base and knocks survivors back.", "Yakın önleyici: üssün önündeki düşmanları keser ve hayatta kalanları geri iter.", "Melee interceptor: cuts enemies in front of the base and knocks survivors back.") },
            { "orbiosis.hangar.profile.horizon.body", new Translation("Передовой разведчик: его обзор открывает дальние цели. После 30 секунд возвращается на базу и заправляется за части.", "Forward scout: its vision opens distant targets. After 30 seconds it returns to base and refuels with parts.", "İleri keşif: görüşü uzak hedefleri açar. 30 saniye sonra üsse döner ve parçalarla yakıt alır.", "Forward scout: its vision opens distant targets. After 30 seconds it returns to base and refuels with parts.") },
            { "orbiosis.hangar.profile.tamer.body", new Translation("Укротитель парализует слабую цель, тратит часть на захват и возвращается на базу для дозарядки.", "The tamer paralyzes a weak target, spends one part to capture, then returns to base to recharge.", "Evcilleştirici zayıf hedefi felç eder, yakalama için bir parça harcar ve şarj için üsse döner.", "The tamer paralyzes a weak target, spends one part to capture, then returns to base to recharge.") },
            { "orbiosis.hangar.profile.queen.body", new Translation("Helin — премиум-командир основных дронов. Перетащи её по ангару: дроны и модули в радиусе получают баф, а связь видна тонкими энергетическими нитями.", "Helin is the premium commander for main drones. Drag her around the hangar: drones and modules inside the radius gain a buff, shown by thin energy threads.", "Helin ana dronlar için premium komutandır. Hangarda sürükle: yarıçap içindeki dronlar ve modüller güçlenir, ince enerji bağlarıyla görünür.", "Helin is the premium commander for main drones. Drag her around the hangar: drones and modules inside the radius gain a buff, shown by thin energy threads.") },
            { "orbiosis.hangar.profile.tech.body", new Translation("Улучшаемая система ангара.", "Upgradeable hangar system.", "Yükseltilebilir hangar sistemi.", "Upgradeable hangar system.") },
            { "orbiosis.speaker.crafter", new Translation("КРАФТЕР-ДРОН", "CRAFTER DRONE", "ÜRETİCİ DRON", "CRAFTER DRONE") },
            { "orbiosis.speaker.mine_carrier", new Translation("МИННЫЙ НОСИТЕЛЬ", "MINE CARRIER", "MAYIN TAŞIYICI", "MINE CARRIER") },
            { "orbiosis.speaker.orb_station", new Translation("СТАНЦИЯ ORB", "ORB STATION", "ORB İSTASYONU", "ORB STATION") },
            { "orbiosis.speaker.repair", new Translation("РЕМОНТНЫЙ ДРОН", "REPAIR DRONE", "ONARIM DRONU", "REPAIR DRONE") },
            { "orbiosis.speaker.player", new Translation("ОПЕРАТОР ORB", "ORB OPERATOR", "ORB OPERATÖRÜ", "ORB OPERATOR") },
            { "orbiosis.dialog.mine_intro_1", new Translation("Неизвестный симбиотический узел обнаружен.\nМаршрут через минный сектор закрыт.", "Unknown symbiotic node detected.\nThe route through the mine sector is sealed.", "Bilinmeyen simbiyotik düğüm algılandı.\nMayın sektöründen geçen rota kapalı.", "Unknown symbiotic node detected.\nThe route through the mine sector is sealed.") },
            { "orbiosis.dialog.mine_intro_2", new Translation("Он не просто ставит мины.\nОн перекрывает нам путь к внешнему кольцу.", "It is not just laying mines.\nIt is blocking our path to the outer ring.", "Sadece mayın döşemiyor.\nDış halkaya giden yolumuzu kapatıyor.", "It is not just laying mines.\nIt is blocking our path to the outer ring.") },
            { "orbiosis.dialog.mine_intro_3", new Translation("Ваш симбиоз новый. Связи слабые.\nДесять волн покажут, где они порвутся.", "Your symbiosis is new. The links are weak.\nTen waves will show where they break.", "Simbiyozunuz yeni. Bağlar zayıf.\nOn dalga nerede kopacaklarını gösterecek.", "Your symbiosis is new. The links are weak.\nTen waves will show where they break.") },
            { "orbiosis.dialog.mine_intro_player", new Translation("Держу управление Orb.\nВеду его через разрыв.", "I have control of the Orb.\nI am taking it through the breach.", "Orb'un kontrolü bende.\nOnu yarıktan geçiriyorum.", "I have control of the Orb.\nI am taking it through the breach.") },
            { "orbiosis.dialog.mine_intro_4", new Translation("{0}, держи Orb в движении.\nЯ буду собирать детали и поддерживать систему.", "{0}, keep the Orb moving.\nI will gather parts and keep the system alive.", "{0}, Orb'u hareket halinde tut.\nParçaları toplayıp sistemi ayakta tutacağım.", "{0}, keep the Orb moving.\nI will gather parts and keep the system alive.") },
            { "orbiosis.dialog.mine_intro_start", new Translation("НАЧАТЬ ПРОРЫВ", "START BREAKOUT", "YARMA BAŞLAT", "START BREAKOUT") },
            { "orbiosis.dialog.mine_mid_1", new Translation("Связь не разорвана.\nЗначит, давление будет увеличено.", "The link did not break.\nThen pressure will be increased.", "Bağ kopmadı.\nO halde baskı artırılacak.", "The link did not break.\nThen pressure will be increased.") },
            { "orbiosis.dialog.mine_mid_2", new Translation("Он открывает носовые ангары.\nЭто уже не заграждение. Это прямой удар.", "It is opening forward hangars.\nThis is no longer a barrier. It is a direct strike.", "Ön hangarları açıyor.\nBu artık bir barikat değil. Doğrudan saldırı.", "It is opening forward hangars.\nThis is no longer a barrier. It is a direct strike.") },
            { "orbiosis.dialog.mine_mid_3", new Translation("Три ствола. Три типа мин.\nПосмотрим, сколько выдержит ваш симбиоз.", "Three barrels. Three mine types.\nLet us see how much your symbiosis can endure.", "Üç namlu. Üç mayın türü.\nSimbiyozunuzun ne kadar dayanacağını görelim.", "Three barrels. Three mine types.\nLet us see how much your symbiosis can endure.") },
            { "orbiosis.dialog.mine_mid_player", new Translation("Я вижу раскрытие ангаров.\nДержу Orb на центре сектора.", "I see the hangars opening.\nI am holding the Orb in the center lane.", "Hangarların açıldığını görüyorum.\nOrb'u sektörün merkezinde tutuyorum.", "I see the hangars opening.\nI am holding the Orb in the center lane.") },
            { "orbiosis.dialog.mine_end", new Translation("Это только начало.\nВы даже не представляете, что ждёт вас дальше.", "This is only the beginning.\nYou cannot imagine what waits beyond.", "Bu yalnızca başlangıç.\nSizi ileride neyin beklediğini hayal bile edemezsiniz.", "This is only the beginning.\nYou cannot imagine what waits beyond.") },
            { "orbiosis.dialog.mine_end_player", new Translation("Записал его сигнал.\nВеду Orb обратно к базе.", "Signal recorded.\nI am guiding the Orb back to base.", "Sinyalini kaydettim.\nOrb'u üsse geri götürüyorum.", "Signal recorded.\nI am guiding the Orb back to base.") },
            { "orbiosis.dialog.tutorial_first_symbiosis", new Translation("Стыкуйтесь с ядром.\nЯ сведу управление Orb в одну систему.", "Dock with the core.\nI will merge Orb control into one system.", "Çekirdeğe kenetlenin.\nOrb kontrolünü tek sistemde birleştireceğim.", "Dock with the core.\nI will merge Orb control into one system.") },
            { "orbiosis.dialog.tutorial_first_symbiosis_button", new Translation("ВОЙТИ В СИМБИОЗ", "ENTER SYMBIOSIS", "SİMBİYOZA GİR", "ENTER SYMBIOSIS") },
            { "orbiosis.dialog.tutorial_1", new Translation("Сверху идут новые цели.\nСнизу тоже поднимается рой.", "New targets are coming from above.\nThe swarm is rising from below too.", "Yukarıdan yeni hedefler geliyor.\nAşağıdan da sürü yükseliyor.", "New targets are coming from above.\nThe swarm is rising from below too.") },
            { "orbiosis.dialog.tutorial_2", new Translation("Orb теряет тягу.\nЕсли я поведу его отдельно от дронов, нас сомнут.", "The Orb is losing thrust.\nIf I pilot it away from the drones, we will be crushed.", "Orb itiş gücünü kaybediyor.\nOnu dronlardan ayrı götürürsem eziliriz.", "The Orb is losing thrust.\nIf I pilot it away from the drones, we will be crushed.") },
            { "orbiosis.dialog.tutorial_3", new Translation("Дроны повреждены.\nСвязь рвётся на каждом манёвре.", "The drones are damaged.\nThe link tears on every maneuver.", "Dronlar hasarlı.\nBağ her manevrada kopuyor.", "The drones are damaged.\nThe link tears on every maneuver.") },
            { "orbiosis.dialog.tutorial_4", new Translation("Значит, нам не нужен строй.\nНам нужна общая система.", "Then we do not need a formation.\nWe need one shared system.", "O halde düzene değil,\northak bir sisteme ihtiyacımız var.", "Then we do not need a formation.\nWe need one shared system.") },
            { "orbiosis.dialog.tutorial_docked", new Translation("Стыковка принята.\nТеперь я веду Orb и дронов как один корпус.", "Docking accepted.\nNow I pilot the Orb and drones as one hull.", "Kenetlenme kabul edildi.\nArtık Orb'u ve dronları tek gövde gibi yönetiyorum.", "Docking accepted.\nNow I pilot the Orb and drones as one hull.") },
            { "orbiosis.dialog.tutorial_base_1", new Translation("Проход очищен.\nНо Orb повреждён сильнее, чем я думал.", "The passage is clear.\nBut the Orb is more damaged than I thought.", "Geçit temizlendi.\nAma Orb düşündüğümden daha fazla hasarlı.", "The passage is clear.\nBut the Orb is more damaged than I thought.") },
            { "orbiosis.dialog.tutorial_base_2", new Translation("Мы собрали подвижную базу ниже по курсу.\nТам есть посадочное поле и внешние боевые слоты.", "We assembled a mobile base down-course.\nIt has a landing field and external combat slots.", "Rotanın aşağısında hareketli bir üs kurduk.\nİniş alanı ve dış savaş yuvaları var.", "We assembled a mobile base down-course.\nIt has a landing field and external combat slots.") },
            { "orbiosis.dialog.tutorial_base_3", new Translation("Садись на центр платформы.\nАнгар пока заблокирован: работаем только через внешние боевые слоты.", "Land on the center of the platform.\nThe hangar is locked for now: we work through external combat slots only.", "Platformun merkezine in.\nHangar şimdilik kilitli: yalnızca dış savaş yuvalarıyla çalışıyoruz.", "Land on the center of the platform.\nThe hangar is locked for now: we work through external combat slots only.") },
            { "orbiosis.dialog.tutorial_land_button", new Translation("Я ПОСАЖУ ORB", "I WILL LAND THE ORB", "ORB'U İNDİRECEĞİM", "I WILL LAND THE ORB") },
            { "orbiosis.dialog.tutorial_takeoff_1", new Translation("Для восстановления популяции дронов базе нужны детали.\nВыведи Orb с платформы, уничтожай врагов сверху, собирай запчасти и возвращай груз на базу.", "The base needs parts to restore the drone population.\nLaunch the Orb from the platform, destroy enemies above, collect parts, and bring the cargo back to base.", "Dron nüfusunu yenilemek için üssün parçalara ihtiyacı var.\nOrb'u platformdan çıkar, yukarıdaki düşmanları yok et, parçaları topla ve yükü üsse geri getir.", "The base needs parts to restore the drone population.\nLaunch the Orb from the platform, destroy enemies above, collect parts, and bring the cargo back to base.") },
            { "orbiosis.dialog.tutorial_takeoff_button", new Translation("Я ВЗЛЕЧУ", "I WILL TAKE OFF", "KALKIŞ YAPACAĞIM", "I WILL TAKE OFF") },
            { "orbiosis.dialog.tutorial_repair_1", new Translation("Orb сильно повреждён.\nЯ проведу разовый полевой ремонт, пока база держит внешний контур.", "The Orb is badly damaged.\nI will run a one-time field repair while the base holds the outer perimeter.", "Orb ağır hasarlı.\nÜs dış hattı tutarken tek seferlik saha onarımı yapacağım.", "The Orb is badly damaged.\nI will run a one-time field repair while the base holds the outer perimeter.") },
            { "orbiosis.dialog.tutorial_repair_button", new Translation("НАЧАТЬ РЕМОНТ", "START REPAIR", "ONARIMI BAŞLAT", "START REPAIR") },
            { "orbiosis.dialog.tutorial_repair_done", new Translation("Детали приняты.\nТеперь тебе нужно освоить наши технологии и изучить новый Repair Drone.", "Parts accepted.\nNow you need to learn our technology and study the new Repair Drone.", "Parçalar alındı.\nŞimdi teknolojimizi öğrenip yeni Onarım Dronunu incelemelisin.", "Parts accepted.\nNow you need to learn our technology and study the new Repair Drone.") },
            { "orbiosis.dialog.tutorial_return_parts", new Translation("Отлично, запчасти собраны.\nТеперь верни груз на базу и разгрузи его на платформе.", "Good, parts collected.\nNow return the cargo to the base and unload it on the platform.", "Güzel, parçalar toplandı.\nŞimdi yükü üsse geri getir ve platformda boşalt.", "Good, parts collected.\nNow return the cargo to the base and unload it on the platform.") },
            { "orbiosis.dialog.tutorial_return_button", new Translation("ВЕРНУТЬ НА БАЗУ", "RETURN TO BASE", "ÜSSE DÖN", "RETURN TO BASE") },
            { "orbiosis.dialog.modules_intro", new Translation("Мы восстановили все энергопотоки в базе.\nПора наладить вооружение и поставить модули на боевые слоты.", "We restored every energy flow in the base.\nTime to tune the weapons and place modules on combat slots.", "Üsteki tüm enerji akışlarını yeniledik.\nSilahları ayarlayıp modülleri savaş yuvalarına yerleştirme zamanı.", "We restored every energy flow in the base.\nTime to tune the weapons and place modules on combat slots.") },
            { "orbiosis.dialog.open_modules", new Translation("ОТКРЫТЬ МОДУЛИ", "OPEN MODULES", "MODÜLLERİ AÇ", "OPEN MODULES") },
            { "orbiosis.dialog.modules_ready", new Translation("Вооружение установлено на базу.\nТеперь у нас есть ракетный, лазерный, дуговой и рельсовый модули.", "Weapons are installed on the base.\nNow we have missile, laser, arc and rail modules.", "Silahlar üsse kuruldu.\nArtık füze, lazer, ark ve ray modüllerimiz var.", "Weapons are installed on the base.\nNow we have missile, laser, arc and rail modules.") },
            { "orbiosis.dialog.defense_intro", new Translation("На нас движется армия.\nOrb временно остаётся в ядре базы: встретим их огнём всех модулей.", "An army is moving toward us.\nThe Orb stays in the base core for now: we meet them with every module.", "Bir ordu bize doğru geliyor.\nOrb şimdilik üs çekirdeğinde kalıyor: hepsini modül ateşiyle karşılayacağız.", "An army is moving toward us.\nThe Orb stays in the base core for now: we meet them with every module.") },
            { "orbiosis.dialog.to_defense", new Translation("К ОБОРОНЕ", "TO DEFENSE", "SAVUNMAYA", "TO DEFENSE") },
            { "orbiosis.dialog.defense_done", new Translation("Орудия держат строй.\nТеперь база может встретить первую армию без отступления.", "The guns hold formation.\nNow the base can face the first army without retreating.", "Silahlar düzeni koruyor.\nArtık üs ilk orduyu geri çekilmeden karşılayabilir.", "The guns hold formation.\nNow the base can face the first army without retreating.") },
            { "orbiosis.dialog.evolution_intro", new Translation("Orb закреплён на базе.\nТеперь усилим само ядро: проведи первую эволюцию в технологиях станции.", "The Orb is locked to the base.\nNow strengthen the core itself: perform the first evolution in station technology.", "Orb üsse sabitlendi.\nŞimdi çekirdeği güçlendirelim: istasyon teknolojisinde ilk evrimi yap.", "The Orb is locked to the base.\nNow strengthen the core itself: perform the first evolution in station technology.") },
            { "orbiosis.dialog.open_station", new Translation("ОТКРЫТЬ СТАНЦИЮ", "OPEN STATION", "İSTASYONU AÇ", "OPEN STATION") },
            { "orbiosis.dialog.evolution_done", new Translation("Эволюция прошла чисто.\nКорпус стал крепче, а ядро готово принять новый защитный контур.", "Evolution completed cleanly.\nThe hull is stronger, and the core is ready for a new defensive circuit.", "Evrim temiz tamamlandı.\nGövde güçlendi ve çekirdek yeni savunma hattını kabul etmeye hazır.", "Evolution completed cleanly.\nThe hull is stronger, and the core is ready for a new defensive circuit.") },
            { "orbiosis.dialog.open_base", new Translation("Перед прорывом нужно открыть базовый ангар.\nКнопка Base теперь под Station. Нажми её.", "Before the breakout we need to open the base hangar.\nThe Base button is now under Station. Press it.", "Yarmadan önce üs hangarını açmalıyız.\nBase düğmesi artık Station altında. Ona bas.", "Before the breakout we need to open the base hangar.\nThe Base button is now under Station. Press it.") },
            { "orbiosis.dialog.open_base_button", new Translation("ОТКРОЮ БАЗУ", "OPEN BASE", "ÜSSÜ AÇACAĞIM", "OPEN BASE") },
            { "orbiosis.dialog.shield_intro", new Translation("Пришло время подготовиться к прорыву.\nНам нужен Shield Drone: он встанет за Orb и примет первый удар.", "It is time to prepare for the breakout.\nWe need a Shield Drone: it will stand behind the Orb and take the first hit.", "Yarmaya hazırlanma zamanı.\nBir Kalkan Dronu lazım: Orb'un arkasında durup ilk darbeyi alacak.", "It is time to prepare for the breakout.\nWe need a Shield Drone: it will stand behind the Orb and take the first hit.") },
            { "orbiosis.dialog.shield_ready", new Translation("Shield Drone готов.\nЗакрепи его за Orb: перетащи дрон на корпус станции.", "Shield Drone is ready.\nAttach it to the Orb: drag the drone onto the station hull.", "Kalkan Dronu hazır.\nOnu Orb'a bağla: dronu istasyon gövdesine sürükle.", "Shield Drone is ready.\nAttach it to the Orb: drag the drone onto the station hull.") },
            { "orbiosis.dialog.drag_button", new Translation("ПЕРЕТАЩУ", "I WILL DRAG IT", "SÜRÜKLEYECEĞİM", "I WILL DRAG IT") },
            { "orbiosis.dialog.launch_intro", new Translation("Щит закреплён за Orb.\nПора вылетать: выведи станцию из слота и держи курс вперёд.", "The shield is linked to the Orb.\nTime to launch: pull the station out of the slot and hold course forward.", "Kalkan Orb'a bağlandı.\nKalkış zamanı: istasyonu yuvadan çıkar ve ileri rotayı tut.", "The shield is linked to the Orb.\nTime to launch: pull the station out of the slot and hold course forward.") },
            { "orbiosis.dialog.launch_button", new Translation("ВЫЛЕТАЮ", "LAUNCHING", "KALKIYORUM", "LAUNCHING") },
            { "orbiosis.dialog.breakout_intro", new Translation("Хорошо. Щит держит передний сектор.\nГотовимся к прорыву: впереди плотный заслон, он не двигается, но перекрывает путь.", "Good. The shield holds the front sector.\nPrepare for breakout: a dense blockade ahead is not moving, but it blocks the route.", "İyi. Kalkan ön sektörü tutuyor.\nYarmaya hazırlan: önde yoğun bir barikat var, hareket etmiyor ama yolu kapatıyor.", "Good. The shield holds the front sector.\nPrepare for breakout: a dense blockade ahead is not moving, but it blocks the route.") },
            { "orbiosis.dialog.breakout_button", new Translation("К ПРОРЫВУ", "TO BREAKOUT", "YARMAYA", "TO BREAKOUT") },
            { "orbiosis.dialog.complete_1", new Translation("Заслон пробит.\nСигналы в системе оживают: здесь ещё остались дроны, отрезанные роем.", "The blockade is broken.\nSignals in the system are waking: some drones here are still cut off by the swarm.", "Barikat kırıldı.\nSistemde sinyaller canlanıyor: burada sürüden kopmuş dronlar hâlâ var.", "The blockade is broken.\nSignals in the system are waking: some drones here are still cut off by the swarm.") },
            { "orbiosis.dialog.listen_button", new Translation("СЛУШАЮ", "LISTENING", "DİNLİYORUM", "LISTENING") },
            { "orbiosis.dialog.complete_2", new Translation("Мы не выведем всех одним строем.\nЭтот отряд уйдёт вперёд: прочешет внешние орбиты и найдёт выживших.", "We cannot extract everyone in one formation.\nThis squad will move ahead, sweep the outer orbits and find survivors.", "Herkesi tek düzende çıkaramayız.\nBu birlik öne gidecek, dış yörüngeleri tarayıp hayatta kalanları bulacak.", "We cannot extract everyone in one formation.\nThis squad will move ahead, sweep the outer orbits and find survivors.") },
            { "orbiosis.dialog.next_button", new Translation("ДАЛЬШЕ", "NEXT", "İLERİ", "NEXT") },
            { "orbiosis.dialog.complete_player", new Translation("Понял.\nЯ останусь за управлением Orb и удержу базу на маршруте.", "Understood.\nI will stay at the Orb controls and keep the base on route.", "Anlaşıldı.\nOrb kontrolünde kalıp üssü rotada tutacağım.", "Understood.\nI will stay at the Orb controls and keep the base on route.") },
            { "orbiosis.dialog.complete_3", new Translation("Я остаюсь с тобой.\nБуду вести базу, открывать технологии и готовить нас к бою с роем.", "I stay with you.\nI will guide the base, unlock technology and prepare us for the swarm.", "Ben seninle kalıyorum.\nÜssü yönetecek, teknolojileri açacak ve bizi sürüyle savaşa hazırlayacağım.", "I stay with you.\nI will guide the base, unlock technology and prepare us for the swarm.") },
            { "orbiosis.dialog.accept_button", new Translation("ПРИНЯТО", "ACCEPTED", "KABUL", "ACCEPTED") },
            { "orbiosis.dialog.outpost_pad_intro_1", new Translation("Теперь одной базы мало.\nНужно освоить площадку аванпоста: она даст нам передовую точку для Orb и боевых модулей.", "One base is not enough now.\nWe need to master the outpost pad: it gives the Orb and combat modules a forward point.", "Artık tek üs yetmez.\nKarakol platformunu öğrenmeliyiz: Orb ve savaş modülleri için ileri bir nokta sağlar.", "One base is not enough now.\nWe need to master the outpost pad: it gives the Orb and combat modules a forward point.") },
            { "orbiosis.dialog.outpost_pad_intro_2", new Translation("Я открою технологии базы.\nКупи площадку аванпоста сам: после покупки установим её в ангаре.", "I will open base technology.\nBuy the outpost pad yourself: after the purchase we will install it in the hangar.", "Üs teknolojisini açacağım.\nKarakol platformunu kendin al: satın aldıktan sonra onu hangara kuracağız.", "I will open base technology.\nBuy the outpost pad yourself: after the purchase we will install it in the hangar.") },
            { "orbiosis.dialog.outpost_pad_ready", new Translation("Площадка аванпоста установлена.\nТеперь в бою отправь Crafter на площадку, когда накопишь части: он соберёт передовой узел.", "The outpost pad is installed.\nNow in battle, send Crafter to the pad when you have enough parts: it will assemble a forward node.", "Karakol platformu kuruldu.\nSavaşta yeterli parçan olduğunda Crafter'ı platforma gönder: ileri düğümü kuracak.", "The outpost pad is installed.\nNow in battle, send Crafter to the pad when you have enough parts: it will assemble a forward node.") },
            { "orbiosis.dialog.open_technology", new Translation("ОТКРЫТЬ ТЕХНОЛОГИИ", "OPEN TECHNOLOGY", "TEKNOLOJİYİ AÇ", "OPEN TECHNOLOGY") },
            { "orbiosis.dialog.continue", new Translation("ПРОДОЛЖИТЬ", "CONTINUE", "DEVAM", "CONTINUE") },
            { "orbiosis.status.choose_mode", new Translation("ВЫБЕРИ РЕЖИМ ПОЛЁТА", "CHOOSE FLIGHT MODE", "UÇUŞ MODUNU SEÇ", "CHOOSE FLIGHT MODE") },
            { "orbiosis.status.story_select", new Translation("ИСТОРИЯ - ВЫБОР УРОВНЯ", "STORY MODE - SELECT LEVEL", "HİKÂYE MODU - BÖLÜM SEÇ", "STORY MODE - SELECT LEVEL") },
            { "orbiosis.status.settings", new Translation("НАСТРОЙКИ", "SETTINGS", "AYARLAR", "SETTINGS") },
            { "orbiosis.status.tutorial_complete", new Translation("ОБУЧЕНИЕ ПРОЙДЕНО", "TUTORIAL COMPLETE", "ÖĞRETİCİ TAMAMLANDI", "TUTORIAL COMPLETE") },
            { "orbiosis.status.tutorial_unlocked", new Translation("ОТКРЫТЫ: START, TECHNOLOGY, HANGAR", "UNLOCKED: START, TECHNOLOGY, HANGAR", "AÇILDI: BAŞLA, TEKNOLOJİ, HANGAR", "UNLOCKED: START, TECHNOLOGY, HANGAR") },
            { "orbiosis.status.complete_tutorial", new Translation("СНАЧАЛА ПРОЙДИ ОБУЧЕНИЕ", "COMPLETE TUTORIAL FIRST", "ÖNCE ÖĞRETİCİYİ BİTİR", "COMPLETE TUTORIAL FIRST") },
            { "orbiosis.status.technology_bay", new Translation("ТЕХНОЛОГИЧЕСКИЙ ОТСЕК", "TECHNOLOGY BAY", "TEKNOLOJİ BÖLMESİ", "TECHNOLOGY BAY") },
            { "orbiosis.status.hangar", new Translation("АНГАР", "HANGAR", "HANGAR", "HANGAR") },
            { "orbiosis.status.hangar_locked_slots", new Translation("АНГАР ЗАКРЫТ - ТОЛЬКО БОЕВЫЕ СЛОТЫ", "HANGAR LOCKED - COMBAT SLOTS ONLY", "HANGAR KİLİTLİ - SADECE SAVAŞ YUVALARI", "HANGAR LOCKED - COMBAT SLOTS ONLY") },
            { "orbiosis.status.base_requires_orb", new Translation("СНАЧАЛА ПРИСТЫКУЙ ORB К БАЗЕ", "DOCK ORB TO BASE FIRST", "ÖNCE ORB'U ÜSSE KENETLE", "DOCK ORB TO BASE FIRST") },
            { "orbiosis.status.carrier_gates_locked", new Translation("ВОРОТА НОСИТЕЛЯ ЗАКРЫТЫ", "CARRIER GATES LOCKED", "TAŞIYICI KAPILARI KİLİTLİ", "CARRIER GATES LOCKED") },
            { "orbiosis.status.base_defense_core", new Translation("ОБОРОНА БАЗЫ - ORB ЗАКРЕПЛЁН НА ЯДРЕ ОРУЖИЯ", "BASE DEFENSE - ORB LOCKED TO WEAPON CORE", "ÜS SAVUNMASI - ORB SİLAH ÇEKİRDEĞİNE KİLİTLİ", "BASE DEFENSE - ORB LOCKED TO WEAPON CORE") },
            { "orbiosis.status.mine_layer_unlocked", new Translation("МИНЁР ОТКРЫТ", "MINE LAYER UNLOCKED", "MAYINCI AÇILDI", "MINE LAYER UNLOCKED") },
            { "orbiosis.status.mines_complete", new Translation("МИНЫ ЗАВЕРШЕНЫ", "MINES COMPLETE", "MAYINLAR TAMAMLANDI", "MINES COMPLETE") },
            { "orbiosis.status.seeker_mine_locked", new Translation("САМОИСКАТЕЛЬНАЯ МИНА ЗАКРЫТА", "SEEKER MINE LOCKED", "ARAYICI MAYIN KİLİTLİ", "SEEKER MINE LOCKED") },
            { "orbiosis.status.progress_reset", new Translation("ПРОГРЕСС СБРОШЕН - ЯДРО 999", "PROGRESS RESET - CORE 999", "İLERLEME SIFIRLANDI - ÇEKİRDEK 999", "PROGRESS RESET - CORE 999") },
            { "orbiosis.status.not_enough_core", new Translation("НЕ ХВАТАЕТ ОСКОЛКОВ ЯДРА", "NOT ENOUGH CORE SHARDS", "ÇEKİRDEK PARÇASI YETERSİZ", "NOT ENOUGH CORE SHARDS") },
            { "orbiosis.status.upgraded", new Translation("УЛУЧШЕНО", "UPGRADED", "YÜKSELTİLDİ", "UPGRADED") },
            { "orbiosis.status.upgraded_level", new Translation("УЛУЧШЕНО {0} УР {1}", "UPGRADED {0} LV {1}", "{0} SV {1} YÜKSELTİLDİ", "UPGRADED {0} LV {1}") },
            { "orbiosis.status.technology_max", new Translation("ТЕХНОЛОГИЯ МАКС", "TECHNOLOGY MAX", "TEKNOLOJİ MAKS", "TECHNOLOGY MAX") },
            { "orbiosis.status.evolution_fail", new Translation("СПРАЙТ ЭВОЛЮЦИИ НЕ ЗАГРУЖЕН", "EVOLUTION SPRITE FAIL", "EVRİM SPRITE'I YÜKLENEMEDİ", "EVOLUTION SPRITE FAIL") },
            { "orbiosis.status.evolution_max", new Translation("ЭВОЛЮЦИЯ МАКС", "EVOLUTION MAX", "EVRİM MAKS", "EVOLUTION MAX") },
            { "orbiosis.status.evolution_level", new Translation("ЭВОЛЮЦИЯ УРОВЕНЬ {0}", "EVOLUTION LEVEL {0}", "EVRİM SEVİYE {0}", "EVOLUTION LEVEL {0}") },
            { "orbiosis.status.tutorial_locked", new Translation("ШАГ ОБУЧЕНИЯ ЗАКРЫТ", "TUTORIAL STEP LOCKED", "ÖĞRETİCİ ADIMI KİLİTLİ", "TUTORIAL STEP LOCKED") },
            { "orbiosis.evolution.stats", new Translation("СИМБИОЗ ЯДРА\nHP {2}+{3}   ГРУЗ {4}+{5}\nORB {6}/4+{7}   ПОЛЁТ {8}/4+{9}", "CORE SYMBIOSIS\nHP {2}+{3}   CARGO {4}+{5}\nORB {6}/4+{7}   FLIGHT {8}/4+{9}", "ÇEKİRDEK SİMBİYOZU\nCP {2}+{3}   YÜK {4}+{5}\nORB {6}/4+{7}   UÇUŞ {8}/4+{9}", "CORE SYMBIOSIS\nHP {2}+{3}   CARGO {4}+{5}\nORB {6}/4+{7}   FLIGHT {8}/4+{9}") },
            { "orbiosis.status.core_miner_sent", new Translation("ДОБЫТЧИК ЯДРА ОТПРАВЛЕН", "CORE MINER SENT", "ÇEKİRDEK MADENCİSİ GÖNDERİLDİ", "CORE MINER SENT") },
            { "orbiosis.status.core_collected", new Translation("ЯДРО +{0} СОБРАНО", "CORE +{0} COLLECTED", "ÇEKİRDEK +{0} ALINDI", "CORE +{0} COLLECTED") },
            { "orbiosis.status.core_forge_ready", new Translation("КУЗНИЦА ЯДРА ГОТОВА", "CORE FORGE READY", "ÇEKİRDEK OCAĞI HAZIR", "CORE FORGE READY") },
            { "orbiosis.status.core_forge_modules", new Translation("КУЗНИЦА МОДУЛЕЙ ОТКРЫТА", "MODULE FORGE OPEN", "MODÜL OCAĞI AÇIK", "MODULE FORGE OPEN") },
            { "orbiosis.status.core_forge_converted", new Translation("КУЗНИЦА: -{0} ЧАСТЕЙ  ЯДРО +{1}", "FORGE: -{0} PARTS  CORE +{1}", "OCAK: -{0} PARÇA  ÇEKİRDEK +{1}", "FORGE: -{0} PARTS  CORE +{1}") },
            { "orbiosis.status.storage_depot_ready", new Translation("СКЛАД ГОТОВ", "STORAGE READY", "DEPO HAZIR", "STORAGE READY") },
            { "orbiosis.status.storage_start_parts", new Translation("СКЛАД ВЫДАЛ +{0} ЧАСТЕЙ", "STORAGE SUPPLIED +{0} PARTS", "DEPO +{0} PARÇA VERDİ", "STORAGE SUPPLIED +{0} PARTS") },
            { "orbiosis.status.drone_pad_ready", new Translation("ПЛОЩАДКА ДРОНОВ ГОТОВА", "DRONE PAD READY", "DRON PLATFORMU HAZIR", "DRONE PAD READY") },
            { "orbiosis.status.outpost_pad_ready", new Translation("ПЛОЩАДКА АВАНПОСТА ГОТОВА", "OUTPOST PAD READY", "KARAKOL PLATFORMU HAZIR", "OUTPOST PAD READY") },
            { "orbiosis.status.drone_on_upgrade_pad", new Translation("{0}: ГОТОВ К МОДЕРНИЗАЦИИ", "{0}: READY FOR UPGRADE", "{0}: YÜKSELTMEYE HAZIR", "{0}: READY FOR UPGRADE") },
            { "orbiosis.status.outpost_need_parts", new Translation("АВАНПОСТУ НУЖНО {0} ЧАСТЕЙ", "OUTPOST NEEDS {0} PARTS", "KARAKOL İÇİN {0} PARÇA GEREK", "OUTPOST NEEDS {0} PARTS") },
            { "orbiosis.status.outpost_started", new Translation("CRAFTER СТРОИТ АВАНПОСТ: -{0} ЧАСТЕЙ", "CRAFTER BUILDING OUTPOST: -{0} PARTS", "CRAFTER KARAKOL KURUYOR: -{0} PARÇA", "CRAFTER BUILDING OUTPOST: -{0} PARTS") },
            { "orbiosis.status.outpost_building", new Translation("АВАНПОСТ СТРОИТСЯ", "OUTPOST BUILDING", "KARAKOL KURULUYOR", "OUTPOST BUILDING") },
            { "orbiosis.status.outpost_progress", new Translation("АВАНПОСТ {0}%", "OUTPOST {0}%", "KARAKOL {0}%", "OUTPOST {0}%") },
            { "orbiosis.status.outpost_complete", new Translation("АВАНПОСТ ГОТОВ", "OUTPOST READY", "KARAKOL HAZIR", "OUTPOST READY") },
            { "orbiosis.status.outpost_ready", new Translation("АВАНПОСТ УЖЕ ГОТОВ", "OUTPOST ALREADY READY", "KARAKOL ZATEN HAZIR", "OUTPOST ALREADY READY") },
            { "orbiosis.status.buy_outpost_pad", new Translation("КУПИ ПЛОЩАДКУ АВАНПОСТА", "BUY THE OUTPOST PAD", "KARAKOL PLATFORMUNU AL", "BUY THE OUTPOST PAD") },
            { "orbiosis.status.outpost_pad_installing", new Translation("ORB УСТАНАВЛИВАЕТ ПЛОЩАДКУ", "ORB INSTALLING PAD", "ORB PLATFORMU KURUYOR", "ORB INSTALLING PAD") },
            { "orbiosis.status.outpost_pad_installed", new Translation("ПЛОЩАДКА АВАНПОСТА УСТАНОВЛЕНА", "OUTPOST PAD INSTALLED", "KARAKOL PLATFORMU KURULDU", "OUTPOST PAD INSTALLED") },
            { "orbiosis.status.outpost_unloading", new Translation("ОРУДИЯ ПЕРЕЕЗЖАЮТ НА АВАНПОСТ", "WEAPONS MOVING TO OUTPOST", "SİLAHLAR KARAKOLA TAŞINIYOR", "WEAPONS MOVING TO OUTPOST") },
            { "orbiosis.status.outpost_mode", new Translation("РЕЖИМ АВАНПОСТА", "OUTPOST MODE", "KARAKOL MODU", "OUTPOST MODE") },
            { "orbiosis.status.outpost_moving", new Translation("ЛИНИЯ АВАНПОСТА ПЕРЕМЕЩАЕТСЯ", "MOVING OUTPOST LINE", "KARAKOL HATTI TAŞINIYOR", "MOVING OUTPOST LINE") },

            { "language.title", new Translation("Выберите язык", "Choose Language", "Dil Seç", "Sprache wahlen") },
            { "language.subtitle", new Translation("Выберите язык перед созданием профиля.", "Select the language before creating your profile.", "Profil olusturmadan once dili seçin.", "Wahle die Sprache, bevor du dein Profil erstellst.") },
            { "language.russian", new Translation("Русский", "Русский", "Русский") },
            { "language.english", new Translation("English", "English", "English") },
            { "language.turkish", new Translation("Türkçe", "Türkçe", "Türkçe") },
            { "language.german", new Translation("Deutsch", "Deutsch", "Deutsch", "Deutsch") },

            { "settings.sound", new Translation("Звук", "Sound", "Ses", "Ton") },
            { "settings.music", new Translation("Музыка", "Music", "Müzik", "Musik") },
            { "settings.vibration", new Translation("Вибрация", "Vibration", "Titreşim", "Vibration") },
            { "settings.info_hints_on", new Translation("Подсказки: вкл", "Hints: On", "İpuçları: Açık", "Hinweise: An") },
            { "settings.info_hints_off", new Translation("Подсказки: выкл", "Hints: Off", "İpuçları: Kapalı", "Hinweise: Aus") },
            { "settings.info_understood", new Translation("Понятно", "Got it", "Anladım", "Verstanden") },
            { "settings.language", new Translation("Язык", "Language", "Dil", "Sprache") },
            { "settings.language_ru", new Translation("Русский", "Русский", "Русский") },
            { "settings.language_en", new Translation("English", "English", "English") },
            { "settings.language_tr", new Translation("Türkçe", "Türkçe", "Türkçe") },
            { "settings.language_de", new Translation("Deutsch", "Deutsch", "Deutsch", "Deutsch") },
            { "settings.menu", new Translation("В меню", "Menu", "Menü", "Menu") },
            { "settings.restart", new Translation("Заново", "Restart", "Yeniden", "Neu starten") },
            { "settings.surrender", new Translation("Сдаться", "Surrender", "Teslim ol", "Aufgeben") },
            { "settings.close", new Translation("Закрыть", "Close", "Kapat", "Schliessen") },
            { "settings.change_profile", new Translation("Сменить профиль", "Change Profile", "Profili Değiştir", "Profil wechseln") },
            { "settings.logout", new Translation("Выйти", "Logout", "Çıkış", "Abmelden") },

            { "profile.setup.title", new Translation("Создать профиль", "Create Profile", "Profil Olustur") },
            { "profile.setup.subtitle", new Translation("Выберите аватар и заполните данные профиля.", "Choose your avatar and fill in the profile details.", "Avatarini seç ve profil bilgilerini doldur.") },
            { "profile.setup.avatar", new Translation("Аватар", "Avatar", "Avatar") },
            { "profile.setup.id_auto", new Translation("ID будет назначен автоматически", "ID will be assigned automatically", "ID otomatik atanacak") },
            { "profile.setup.nickname", new Translation("Никнейм", "Nickname", "Takma ad") },
            { "profile.setup.dynasty", new Translation("Название династии", "Dynasty Name", "Hanedan Adi") },
            { "profile.setup.email", new Translation("Email", "Email", "Email") },
            { "profile.setup.password", new Translation("Пароль", "Password", "Sifre") },
            { "profile.setup.age", new Translation("Возраст", "Age", "Yas") },
            { "profile.setup.gender", new Translation("Пол", "Gender", "Cinsiyet") },
            { "profile.setup.male", new Translation("Мужчина", "Male", "Erkek") },
            { "profile.setup.female", new Translation("Женщина", "Female", "Kadin") },
            { "profile.setup.other", new Translation("Другое", "Other", "Diger") },
            { "profile.setup.register", new Translation("Регистрация", "Register", "Kayit") },
            { "profile.setup.login", new Translation("Войти", "Login", "Giriş") },
            { "profile.setup.name_placeholder", new Translation("Введите имя", "Enter your name", "Ismini gir") },
            { "profile.setup.slot", new Translation("Слот профиля", "Profile Slot", "Profil Yuvası") },
            { "profile.setup.remember", new Translation("Запомнить профиль", "Remember Profile", "Profili Hatirla") },
            { "profile.error.avatars_missing", new Translation("Аватары не настроены.", "Avatars are not configured.", "Avatarlar ayarlanmadi.") },
            { "profile.error.no_avatars", new Translation("Аватары не настроены", "No avatars configured", "Avatarlar ayarlanmadi") },
            { "profile.error.service_missing", new Translation("ProfileService не найден.", "ProfileService was not found.", "ProfileService bulunamadi.") },
            { "profile.error.bootstrap_missing", new Translation("Bootstrap не найден.", "Bootstrap was not found.", "Bootstrap bulunamadi.") },
            { "profile.error.enter_name", new Translation("Введите имя.", "Enter a name.", "Bir isim girin.") },
            { "profile.error.name_too_short", new Translation("Имя должно быть минимум {0} символа.", "Name must be at least {0} characters.", "Isim en az {0} karakter olmali.") },
            { "profile.error.name_latin_only", new Translation("Имя должно содержать только английские буквы A-Z.", "Name can contain only English letters A-Z.", "Isim sadece Ingilizce A-Z harflerinden olusmali.") },
            { "profile.error.enter_email", new Translation("Введите email.", "Enter email.", "Email girin.") },
            { "profile.error.email_invalid", new Translation("Введите корректный email.", "Enter a valid email.", "Gecerli bir email girin.") },
            { "profile.error.password_short", new Translation("Пароль должен быть минимум 6 символов.", "Password must be at least 6 characters.", "Sifre en az 6 karakter olmali.") },
            { "profile.error.age_invalid", new Translation("Введите возраст от 1 до 120.", "Enter an age from 1 to 120.", "1 ile 120 arasinda yas girin.") },
            { "profile.error.setup_failed", new Translation("Не удалось создать профиль. Перезапустите игру.", "Profile setup failed. Please restart the game.", "Profil olusturulamadi. Lutfen oyunu yeniden baslatin.") },
            { "profile.error.server", new Translation("Сервер временно недоступен.", "Server is temporarily unavailable.", "Sunucu gecici olarak kullanilamiyor.") },
            { "profile.title", new Translation("Титул: {0}", "Title: {0}", "Unvan: {0}") },
            { "profile.rank", new Translation("Ранг: {0}", "Rank: {0}", "Rutbe: {0}") },
            { "profile.mahjong_title", new Translation("Маджонг: {0}", "Mahjong: {0}", "Mahjong: {0}") },
            { "profile.mahjong_rank", new Translation("Ранг маджонга: {0}", "Mahjong Rank: {0}", "Mahjong Rutbesi: {0}") },

            { "battle.character.Tiger_Male.name", new Translation("Яростный", "Fierce", "Hiddetli", "Grimmig") },
            { "battle.character.Tiger_Female.name", new Translation("Яростная", "Fierce", "Hiddetli", "Grimmig") },
            { "battle.character.Fox_Male.name", new Translation("Хитрый", "Sly", "Kurnaz", "Listig") },
            { "battle.character.Fox_Female.name", new Translation("Хитрая", "Sly", "Kurnaz", "Listig") },
            { "battle.character.Wolf_Male.name", new Translation("Вольный", "Freeborn", "Özgür", "Frei") },
            { "battle.character.Wolf_Female.name", new Translation("Вольная", "Freeborn", "Özgür", "Frei") },
            { "battle.character.Bear_Male.name", new Translation("Несгибаемый", "Unbroken", "Eğilmez", "Unbeugsam") },
            { "battle.character.Bear_Female.name", new Translation("Несгибаемая", "Unbroken", "Eğilmez", "Unbeugsam") },
            { "battle.character.Dragon_Male.name", new Translation("Древний", "Ancient", "Kadim", "Uralter") },
            { "battle.character.Dragon_Female.name", new Translation("Древняя", "Ancient", "Kadim", "Uralte") },
            { "battle.character.Dog_Male.name", new Translation("Верный", "Faithful", "Sadık", "Treu") },
            { "battle.character.Dog_Female.name", new Translation("Верная", "Faithful", "Sadık", "Treu") },
            { "battle.character.unlocked", new Translation("Открыт", "Unlocked", "Açık") },
            { "battle.character.free", new Translation("Бесплатно", "Free", "Ücretsiz") },
            { "battle.character.selected", new Translation("Выбран", "Selected", "Seçildi") },
            { "battle.character.select", new Translation("Выбрать", "Select", "Seç") },
            { "battle.character.buy", new Translation("Купить", "Buy", "Satin al") },
            { "battle.character.buy_free", new Translation("Купить бесплатно", "Buy Free", "Ücretsiz al", "Kostenlos kaufen") },
            { "battle.character.unlock", new Translation("Открыть", "Unlock", "Aç") },
            { "battle.character.locked", new Translation("Закрыт", "Locked", "Kilitli") },
            { "battle.character.disabled", new Translation("Недоступен", "Disabled", "Kapali") },
            { "battle.character.not_enough_gold", new Translation("Недостаточно Оз Алтын", "Not enough Oz Gold", "Yeterli Oz Altın yok") },
            { "battle.character.need_gold", new Translation("Нужно: {0} {1}", "Need: {0} {1}", "Gerekli: {0} {1}") },
            { "battle.character.select_character", new Translation("Выбрать персонажа", "Select Character", "Karakter Seç") },
            { "battle.character.change_character", new Translation("Сменить персонажа", "Change Character", "Karakter Değiştir") },
            { "battle.character.first_hero.blackyang", new Translation("Выбери своего первого героя — он достанется тебе бесплатно.", "Choose your first hero — you will receive them for free.", "İlk kahramanını seç — onu ücretsiz alacaksın.", "Waehle deinen ersten Helden — du erhaeltst ihn kostenlos.") },
            { "battle.character.first_hero.whiteyin", new Translation("Выбирай с умом: этот герой станет началом твоего пути.", "Choose wisely: this hero will mark the beginning of your journey.", "Akıllıca seç: bu kahraman yolculuğunun başlangıcı olacak.", "Waehle mit Bedacht: Dieser Held wird der Anfang deines Weges sein.") },
            { "battle.character.stat.hp", new Translation("HP", "HP", "CP") },
            { "battle.character.stat.attack", new Translation("Атака", "Attack", "Saldırı") },
            { "battle.character.stat.attack_short", new Translation("АТК", "ATK", "SAL") },
            { "battle.character.stat.armor", new Translation("Броня", "Armor", "Zirh") },
            { "battle.character.stat.armor_short", new Translation("БРН", "ARM", "ZRH") },
            { "battle.character.stat.parry", new Translation("Парирование", "Parry", "Savuşturma") },
            { "battle.character.stat.crit", new Translation("Крит", "Crit", "Kritik") },
            { "battle.character.stat.crit_damage", new Translation("Крит урон", "Crit Damage", "Kritik Hasar") },

            { "battle.rank.bronze", new Translation("Бронза", "Bronze", "Bronz") },
            { "battle.rank.silver", new Translation("Серебро", "Silver", "Gümüş") },
            { "battle.rank.gold", new Translation("Золото", "Gold", "Altın") },
            { "battle.rank.platinum", new Translation("Платина", "Platinum", "Platin") },
            { "battle.rank.master", new Translation("Мастер", "Master", "Usta") },
            { "battle.rank.unranked", new Translation("Без ранга", "Unranked", "Derecesiz") },
            { "battle.common.player", new Translation("Игрок", "Player", "Oyuncu") },
            { "battle.common.opponent", new Translation("Соперник", "Opponent", "Rakip") },
            { "battle.common.searching", new Translation("Поиск...", "Searching...", "Aranıyor...") },
            { "battle.common.cancel", new Translation("Отмена", "Cancel", "İptal") },
            { "battle.common.close", new Translation("Закрыть", "Close", "Kapat") },
            { "battle.common.start", new Translation("Старт", "Start", "Başla") },
            { "battle.common.leave", new Translation("Выйти", "Leave", "Çık") },
            { "battle.common.level", new Translation("Уровень {0}", "Level {0}", "Seviye {0}") },
            { "battle.common.profile_line", new Translation("{0}\n{1} {2} RP\nПобеды {3}  Поражения {4}", "{0}\n{1} {2} RP\nWins {3}  Losses {4}", "{0}\n{1} {2} RP\nGalibiyet {3}  Mağlubiyet {4}") },
            { "battle.common.opponent_line", new Translation("{0} {1} RP\nПобеды {2}  Поражения {3}\n{4}", "{0} {1} RP\nWins {2}  Losses {3}\n{4}", "{0} {1} RP\nGalibiyet {2}  Mağlubiyet {3}\n{4}") },
            { "battle.random.title", new Translation("Случайный бой", "Random Match", "Rastgele Maç") },
            { "battle.random.searching", new Translation("Ищем соперника...", "Searching opponent...", "Rakip aranıyor...") },
            { "battle.random.searching_seconds", new Translation("Ищем соперника... {0}", "Searching opponent... {0}", "Rakip aranıyor... {0}") },
            { "battle.random.player_found", new Translation("Игрок найден", "Player Found", "Oyuncu Bulundu") },
            { "battle.random.starting", new Translation("Начинаем бой...", "Starting match...", "Maç başlıyor...") },
            { "battle.random.waiting", new Translation("Ожидание игрока", "Waiting for player", "Oyuncu bekleniyor") },
            { "battle.random.ready", new Translation("готов к бою", "ready for a match", "maça hazır") },
            { "battle.random.online_player", new Translation("Случайный игрок", "Random Player", "Rastgele Oyuncu") },
            { "battle.ranked.title", new Translation("Ранговый бой", "Ranked Match", "Rank Maçı") },
            { "battle.ranked.choose_league_first", new Translation("Сначала выберите ранговую лигу.", "Choose a ranked league first.", "Önce rank ligini seçin.") },
            { "battle.ranked.searching_entry", new Translation("Ищем рангового соперника... Вход {0} OzTile", "Searching ranked opponent... Entry {0} OzTile", "Rank rakibi aranıyor... Giriş {0} OzTile") },
            { "battle.ranked.searching_seconds", new Translation("Ищем рангового соперника... {0}", "Searching ranked opponent... {0}", "Rank rakibi aranıyor... {0}") },
            { "battle.ranked.extending_seconds", new Translation("Продлеваем поиск... {0}", "Extending search... {0}", "Arama uzatılıyor... {0}") },
            { "battle.ranked.slot", new Translation("Слот рангового соперника", "Ranked opponent slot", "Rank rakibi yuvasi") },
            { "battle.ranked.ready", new Translation("готов к ранговому бою", "ready for ranked", "rank maçına hazır") },
            { "battle.ranked.entry_expired", new Translation("Вход в ранговый бой истек. Выберите лигу снова.", "Ranked entry expired. Choose a league again.", "Rank girisi doldu. Ligi tekrar seçin.") },
            { "battle.ranked.online_player", new Translation("Ранговый игрок", "Ranked Player", "Rank Oyuncusu") },
            { "battle.duel.button", new Translation("Вызов на дуэль", "Duel Challenge", "Düello Daveti") },
            { "battle.duel.title", new Translation("Вызов на дуэль", "Duel Challenge", "Düello Daveti") },
            { "battle.duel.nickname", new Translation("Ник игрока или ID", "Player nickname or ID", "Oyuncu adı veya ID") },
            { "battle.duel.stake", new Translation("Ставка OzTile", "OzTile stake", "OzTile bahsi") },
            { "battle.duel.send", new Translation("Вызвать", "Challenge", "Davet et") },
            { "battle.duel.accept", new Translation("Принять", "Accept", "Kabul et") },
            { "battle.duel.decline", new Translation("Отказаться", "Decline", "Reddet") },
            { "battle.duel.sending", new Translation("Отправляем вызов...", "Sending challenge...", "Davet gönderiliyor...") },
            { "battle.duel.accepting", new Translation("Принимаем дуэль...", "Accepting duel...", "Düello kabul ediliyor...") },
            { "battle.duel.declining", new Translation("Отказываемся...", "Declining...", "Reddediliyor...") },
            { "battle.duel.waiting_seconds", new Translation("Ждем ответ... {0}", "Waiting for answer... {0}", "Cevap bekleniyor... {0}") },
            { "battle.duel.incoming_title", new Translation("Вас вызывают на дуэль", "Incoming Duel", "Düello Daveti") },
            { "battle.duel.incoming_body", new Translation("{0} вызывает вас на дуэль\nСтавка: {1} OzTile", "{0} challenges you to a duel\nStake: {1} OzTile", "{0} seni düelloya davet ediyor\nBahis: {1} OzTile") },
            { "battle.duel.incoming_button", new Translation("Вызов на дуэль", "Duel Challenge", "Düello Daveti") },
            { "battle.duel.incoming_button_from", new Translation("Дуэль: {0} / {1} OzTile", "Duel: {0} / {1} OzTile", "Düello: {0} / {1} OzTile") },
            { "battle.duel.max_stake", new Translation("Максимальная ставка вашей лиги: {0} OzTile", "Your league max stake: {0} OzTile", "Ligindeki en yüksek bahis: {0} OzTile") },
            { "battle.duel.need_oztile", new Translation("Недостаточно OzTile. Нужно {0}.", "Not enough OzTile. Need {0}.", "Yeterli OzTile yok. Gerekli {0}.") },
            { "battle.duel.enter_nickname", new Translation("Введите ник игрока.", "Enter player nickname.", "Oyuncu adını gir.") },
            { "battle.duel.player_not_found", new Translation("Игрок не найден.", "Player not found.", "Oyuncu bulunamadı.") },
            { "battle.duel.stake_exceeds", new Translation("Ставка выше лимита лиги.", "Stake exceeds league limit.", "Bahis lig limitini aşıyor.") },
            { "battle.duel.not_accepted", new Translation("Дуэль не принята.", "Duel was not accepted.", "Düello kabul edilmedi.") },
            { "battle.duel.online_player", new Translation("Дуэлянт", "Duelist", "Duellocu") },
            { "battle.wifi.title", new Translation("Wi-Fi бой", "Wi-Fi Battle", "Wi-Fi Maçı") },
            { "battle.wifi.choose", new Translation("Создайте комнату или начните поиск", "Choose Host or Search", "Kur veya Ara seç") },
            { "battle.wifi.create", new Translation("Создать игру", "Create Game", "Oyun Kur") },
            { "battle.wifi.search", new Translation("Поиск", "Search", "Ara") },
            { "battle.wifi.info", new Translation("Создайте игру или ищите в той же Wi-Fi сети", "Create a game or search in the same Wi-Fi", "Aynı Wi-Fi ağında oyun kur veya ara") },
            { "battle.wifi.none", new Translation("Локальные бои пока не найдены", "No local battles found yet", "Yerel maç bulunamadı") },
            { "battle.wifi.join", new Translation("Войти: {0}  {1}", "Join {0}  {1}", "Katıl {0}  {1}") },
            { "battle.wifi.player", new Translation("Wi-Fi игрок", "Wi-Fi Player", "Wi-Fi Oyuncusu") },
            { "battle.wifi.room", new Translation("Wi-Fi комната", "Wi-Fi Room", "Wi-Fi Odasi") },
            { "battle.wifi.joining", new Translation("Входим в комнату", "Joining Room", "Odaya giriliyor") },
            { "battle.wifi.room_created", new Translation("Комната создана", "Room Created", "Oda Kuruldu") },
            { "battle.wifi.connected_room", new Translation("Подключено к комнате", "Connected to Room", "Odaya Bağlandı") },
            { "battle.wifi.you_active", new Translation("Вы: активны", "You: active", "Sen: aktif") },
            { "battle.wifi.second_player", new Translation("Второй игрок: {0}", "Second player: {0}", "İkinci oyuncu: {0}") },
            { "battle.wifi.second_connected", new Translation("Второй игрок: подключен", "Second player: connected", "İkinci oyuncu: bağlandı") },
            { "battle.wifi.wait_second", new Translation("Ждем второго игрока...", "Waiting for second player...", "İkinci oyuncu bekleniyor...") },
            { "battle.wifi.host_hint", new Translation("Попросите второго игрока нажать Поиск и войти в эту комнату.", "Ask the second player to press Search and join this room.", "İkinci oyuncu Ara'ya basıp bu odaya katılsın.") },
            { "battle.wifi.joined_hint", new Translation("Второй игрок вошел. Нажмите Старт.", "Second player joined. Press Start to begin.", "İkinci oyuncu katıldı. Başlamak için Başla.") },
            { "battle.wifi.visible_hint", new Translation("Комната видна. Ждем второго игрока.", "Room is visible. Waiting for another player to join.", "Oda görünür. İkinci oyuncu bekleniyor.") },
            { "battle.wifi.wait_host", new Translation("Ждем, когда хост начнет бой.", "Waiting for host to start the battle.", "Hostun maçı başlatması bekleniyor.") },
            { "battle.league.arena", new Translation("РАНГОВАЯ АРЕНА", "RANKED ARENA", "RANK ARENASI") },
            { "battle.league.choose", new Translation("ВЫБЕРИТЕ ЛИГУ", "CHOOSE LEAGUE", "LİG SEÇ") },
            { "battle.league.global_leaderboard", new Translation("Общий рейтинг", "Global Leaderboard", "Genel Liderlik") },
            { "battle.league.league_leaderboard", new Translation("Рейтинг лиги {0}", "{0} Leaderboard", "{0} Liderliği") },
            { "battle.league.global", new Translation("Общий", "Global", "Genel") },
            { "battle.league.league", new Translation("Лига", "League", "Lig") },
            { "battle.league.open", new Translation("ОТКРЫТО", "OPEN", "AÇIK") },
            { "battle.league.play", new Translation("ИГРАТЬ", "PLAY", "OYNA") },
            { "battle.league.locked", new Translation("ЗАКРЫТО", "LOCKED", "KİLİTLİ") },
            { "battle.league.need", new Translation("НУЖНО", "NEED", "GEREK") },
            { "battle.league.need_rp", new Translation("НУЖНО {0} RP", "NEED {0} RP", "{0} RP GEREK") },
            { "battle.league.entry", new Translation("ВХОД", "ENTRY", "GİRİŞ") },
            { "battle.league.win", new Translation("ПОБЕДА", "WIN", "GALİBİYET") },
            { "battle.league.next", new Translation("Далее: {0} с {1} RP", "Next: {0} at {1} RP", "Sıradaki: {0}, {1} RP") },
            { "battle.league.top", new Translation("Высшая лига достигнута", "Top league reached", "En üst lige ulaşıldı") },
            { "battle.league.summary", new Translation("{0}\n{1}  {2} RP     П {3} / Пор {4}     {5}", "{0}\n{1}  {2} RP     W {3} / L {4}     {5}", "{0}\n{1}  {2} RP     G {3} / M {4}     {5}") },
            { "battle.league.confirm_title", new Translation("Лига {0}", "{0} League", "{0} Ligi") },
            { "battle.league.confirm_body", new Translation("Вход: {0}\nПобеда: +{1}\nПоражение: -{0}\nRP: +{2} / {3}", "Entry: {0}\nWin: +{1}\nLoss: -{0}\nRP: +{2} / {3}", "Giriş: {0}\nGalibiyet: +{1}\nMağlubiyet: -{0}\nRP: +{2} / {3}") },
            { "battle.league.you", new Translation("  ВЫ", "  YOU", "  SEN") },
            { "battle.league.no_real_leaderboard", new Translation("Реальных игроков в рейтинге пока нет.", "No real players in the leaderboard yet.", "Liderlikte henüz gerçek oyuncu yok.", "Noch keine echten Spieler in der Rangliste.") },
            { "battle.parry.choose_zone", new Translation("Выберите зону", "Choose zone", "Bölge seç") },
            { "battle.parry.parried", new Translation("ПАРИРОВАНО", "PARRIED", "SAVUSTURULDU") },
            { "battle.parry.damage", new Translation("УРОН", "DAMAGE", "HASAR") },
            { "battle.parry.time_up", new Translation("Время вышло", "Time is up", "Süre doldu") },
            { "battle.parry.matched", new Translation("Зона парирования совпала с атакой", "Parry zone matched the attack", "Savunma bolgesi saldiriyla eslesti") },
            { "battle.parry.missed", new Translation("Зона парирования не совпала", "Parry zone missed the attack", "Savunma bolgesi kacirdi") },
            { "battle.parry.you_attack_enemy_parries", new Translation("Вы атакуете, враг парирует", "You attack, enemy parries", "Sen saldirirsin, dusman savunur") },
            { "battle.parry.enemy_attacks_you_parry", new Translation("Враг атакует, вы парируете", "Enemy attacks, you parry", "Dusman saldirir, sen savunursun") },
            { "battle.parry.you_attack", new Translation("Вы атакуете", "You attack", "Sen saldirirsin") },
            { "battle.parry.you_parry", new Translation("Вы парируете", "You parry", "Sen savunursun") },
            { "battle.parry.enemy_parries", new Translation("Враг парирует", "Enemy parries", "Dusman savunur") },
            { "battle.parry.enemy_attacks", new Translation("Враг атакует", "Enemy attacks", "Dusman saldirir") },
            { "battle.parry.zone.top", new Translation("Верх", "Top", "Üst") },
            { "battle.parry.zone.middle", new Translation("Центр", "Mid", "Orta") },
            { "battle.parry.zone.bottom", new Translation("Низ", "Low", "Alt") },
            { "battle.shop.not_enough_ametist", new Translation("Недостаточно Аметиста.", "Not enough Ametist.", "Yeterli Ametist yok.") },
            { "battle.shop.purchase_failed", new Translation("Покупка не удалась.", "Purchase failed.", "Satin alma basarisiz.") },
            { "battle.shop.energy_purchased", new Translation("+{0} энергии куплено.", "+{0} Energy purchased.", "+{0} Enerji alindi.") },
            { "battle.shop.character_loading", new Translation("Персонажи загружаются.", "Character service is loading.", "Karakter servisi yukleniyor.") },
            { "battle.shop.character_failed", new Translation("Покупка персонажа не удалась.", "Character purchase failed.", "Karakter satin alma basarisiz.") },
            { "battle.shop.character_unlocked", new Translation("{0} открыт.", "{0} unlocked.", "{0} acildi.") },
            { "battle.shop.opening_purchase", new Translation("Открываем покупку...", "Opening purchase...", "Satin alma aciliyor...") },
            { "battle.shop.ametist_added", new Translation("+{0} Аметиста добавлено.", "+{0} Ametist added.", "+{0} Ametist eklendi.") },
            { "battle.shop.profile_loading", new Translation("Профиль загружается.", "Profile is loading.", "Profil yukleniyor.") },
            { "battle.energy.not_enough", new Translation("Недостаточно энергии. Нужно {0}.", "Not enough energy. Need {0}.", "Yeterli enerji yok. Gerekli {0}.") },
            { "battle.energy.full_admin", new Translation("Матч -0 | Админ", "Match -0 | Admin", "Maç -0 | Admin") },
            { "battle.lobby.level", new Translation("Уровень {0}", "Level {0}", "Seviye {0}") },
            { "battle.lobby.exp", new Translation("EXP {0}/{1}  До след.: {2}", "EXP {0}/{1}  Next: {2}", "EXP {0}/{1}  Sonraki: {2}") },
            { "battle.lobby.stats", new Translation("Победы {0}  Поражения {1}  MVP {2}%", "Wins {0}  Losses {1}  MVP {2}%", "Galibiyet {0}  Mağlubiyet {1}  MVP {2}%") },
            { "battle.lobby.energy", new Translation("Энергия {0}/{1}", "Energy {0}/{1}", "Enerji {0}/{1}") },
            { "battle.lobby.energy_ready", new Translation("Матч -{0} | Полная", "Match -{0} | Full", "Maç -{0} | Dolu") },
            { "battle.lobby.energy_refill", new Translation("Матч -{0} | +1 через {1}", "Match -{0} | +1 in {1}", "Maç -{0} | +1: {1}") },
            { "battle.lobby.energy_ad", new Translation("РЕКЛАМА +{0}", "AD +{0}", "REKLAM +{0}") },
            { "battle.lobby.top_level", new Translation("УР {0}", "LVL {0}", "Sev {0}") },
            { "battle.lobby.top_wl", new Translation("П {0}/Пор {1}", "W {0}/L {1}", "G {0}/M {1}") },
            { "battle.lobby.top_winrate", new Translation("ПОБ {0}%", "WIN {0}%", "Kaz {0}%") },
            { "battle.lobby.forge", new Translation("Кузница", "Forge", "Kuzhane", "Schmiede") },
            { "battle.daily.button", new Translation("Бонус дня", "Daily Bonus", "Günlük Bonus", "Tagesbonus") },
            { "battle.daily.title", new Translation("Бонус дня", "Daily Bonus", "Günlük Bonus", "Tagesbonus") },
            { "battle.daily.title_named", new Translation("Бонус дня: {0}", "Daily Bonus: {0}", "Günlük Bonus: {0}", "Tagesbonus: {0}") },
            { "battle.daily.subtitle", new Translation("Герой дня: {0}", "Hero of the Day: {0}", "Günün Kahramani: {0}", "Held des Tages: {0}") },
            { "battle.daily.empty", new Translation("Камни еще не выбрали героя дня.", "The stones have not chosen today's hero yet.", "Taşlar bugünün kahramanını henüz seçmedi.", "Die Steine haben den Helden des Tages noch nicht gewaehlt.") },
            { "battle.daily.silent", new Translation("Сегодня бамбуковый лес молчит.", "Today the bamboo forest is silent.", "Bugün bambu ormani sessiz.", "Heute schweigt der Bambuswald.") },
            { "battle.daily.no_sign", new Translation("В сгоревшем бамбуковом лесу не прозвучало нового знака.", "No new sign echoed through the burned bamboo forest.", "Yanmış bambu ormanında yeni bir işaret duyulmadı.", "Im verbrannten Bambuswald erklang kein neues Zeichen.") },
            { "battle.daily.bonuses", new Translation("Бонусы", "Bonuses", "Bonuslar", "Boni") },
            { "battle.daily.time_left", new Translation("До смены знака: {0}", "Sign changes in: {0}", "Isaret değişimi: {0}", "Zeichenwechsel in: {0}") },
            { "battle.daily.boost_button", new Translation("БУСТ +50%\nРЕКЛАМА", "BOOST +50%\nWATCH AD", "BOOST +50%\nREKLAM", "BOOST +50%\nWERBUNG") },
            { "battle.daily.boost_active", new Translation("Буст активен", "Boost active", "Boost aktif", "Boost aktiv") },
            { "battle.daily.boost_locked", new Translation("Герой не открыт", "Hero locked", "Kahraman kilitli", "Held gesperrt") },
            { "battle.daily.boost_locked_status", new Translation("Буст доступен только если герой дня есть у тебя.", "Boost is available only if you own today's hero.", "Boost sadece günün kahramani sende varsa kullanilir.", "Boost ist nur verfuegbar, wenn du den Tageshelden besitzt.") },
            { "battle.daily.boost_hint", new Translation("Реклама усилит бонус героя дня на 50% на 1 час.", "Watch an ad to increase today's hero bonus by 50% for 1 hour.", "Reklam izleyerek günün kahramani bonusunu 1 saat boyunca %50 artir.", "Werbung ansehen: Tagesheld-Bonus 1 Stunde lang +50%.") },
            { "battle.daily.boost_unlock_first", new Translation("Сначала открой героя дня. Буст усиливает только его дневной бонус.", "Unlock today's hero first. The boost only strengthens that daily bonus.", "Once günün kahramanini ac. Boost sadece onun günlük bonusunu guclendirir.", "Schalte zuerst den Tageshelden frei. Der Boost verstaerkt nur seinen Tagesbonus.") },
            { "battle.daily.bonus_none", new Translation("Сегодня бонус не активен.", "Today's bonus is inactive.", "Bugün bonus aktif değil.", "Heute ist kein Bonus aktiv.") },
            { "battle.daily.bonus.hp", new Translation("+{0} HP", "+{0} HP", "+{0} CP", "+{0} HP") },
            { "battle.daily.bonus.attack", new Translation("+{0} к атаке", "+{0} Attack", "+{0} Saldiri", "+{0} Angriff") },
            { "battle.daily.bonus.armor", new Translation("+{0}% к броне", "+{0}% Armor", "+{0}% Zirh", "+{0}% Ruestung") },
            { "battle.daily.bonus.parry", new Translation("+{0}% к парированию", "+{0}% Parry", "+{0}% Savusturma", "+{0}% Parade") },
            { "battle.daily.bonus.crit", new Translation("+{0}% к криту", "+{0}% Crit", "+{0}% Kritik", "+{0}% Krit") },
            { "battle.daily.bonus.crit_damage", new Translation("+{0:0.##} к силе крита", "+{0:0.##} Crit Power", "+{0:0.##} Kritik Gucu", "+{0:0.##} Krit-Kraft") },
            { "battle.daily.clan.tiger", new Translation("тигров", "tigers", "kaplanlar", "Tiger") },
            { "battle.daily.clan.fox", new Translation("лис", "foxes", "tilkiler", "Fuechse") },
            { "battle.daily.clan.wolf", new Translation("волков", "wolves", "kurtlar", "Woelfe") },
            { "battle.daily.clan.bear", new Translation("медведей", "bears", "ayilar", "Baeren") },
            { "battle.daily.clan.dragon", new Translation("драконов", "dragons", "ejderhalar", "Drachen") },
            { "battle.daily.clan.dog", new Translation("стражей", "guardians", "muhafizlar", "Waechter") },
            { "battle.daily.clan.default", new Translation("бойцов", "fighters", "savaşcilar", "Kaempfer") },
            { "battle.daily.lore.bear", new Translation("В сгоревшем бамбуковом лесу из-под пепла поднялся тяжелый теплый пар. Древние камни признали стойкость рода {1}, и сегодня {0} выходит в бой с укрепленным сердцем.", "Heavy warm steam rose from beneath the ash of the burned bamboo forest. The ancient stones recognized the endurance of the {1}, and today {0} enters battle with a fortified heart.", "Yanmış bambu ormanının külünden ağır ve sıcak bir buhar yükseldi. Kadim taşlar {1} soyunun direncini tanıdı ve bugün {0} savaşa güçlenmiş bir yürekle giriyor.", "Aus der Asche des verbrannten Bambuswaldes stieg schwerer warmer Dampf. Die alten Steine erkannten die Standhaftigkeit der {1}, und heute zieht {0} mit gestaerktem Herzen in den Kampf.") },
            { "battle.daily.lore.tiger", new Translation("На черной кромке бамбукового леса вспыхнули следы когтей. Пепел разошелся перед родом {1}, и сегодня {0} получает силу быстрого удара.", "Claw marks flared on the black edge of the bamboo forest. The ash parted before the {1}, and today {0} receives the strength of a swift strike.", "Bambu ormanının kara kıyısında pençe izleri parladı. Kül {1} soyunun önünde açıldı ve bugün {0} hızlı darbenin gücünü alıyor.", "Am schwarzen Rand des Bambuswaldes flammten Klauenspuren auf. Die Asche wich vor den {1}, und heute erhaelt {0} die Kraft eines schnellen Schlages.") },
            { "battle.daily.lore.wolf", new Translation("Над сгоревшими стеблями прошел холодный ветер, и в нем прозвучал зов охоты. Род {1} услышал его первым, поэтому сегодня {0} сражается точнее и хладнокровнее.", "A cold wind crossed the burned stalks, carrying the call of the hunt. The {1} heard it first, so today {0} fights with sharper, colder focus.", "Yanmış sapların üstünden soğuk bir rüzgar geçti ve avın çağrısı duyuldu. {1} bunu ilk duydu; bu yüzden bugün {0} daha keskin ve soğukkanlı savaşıyor.", "Ein kalter Wind strich ueber die verbrannten Halme und trug den Ruf der Jagd. Die {1} hoerten ihn zuerst, deshalb kaempft {0} heute praeziser und kuehler.") },
            { "battle.daily.lore.fox", new Translation("В золе бамбукового леса мелькнул хитрый огонь. Род {1} нашел скрытую тропу между удачей и расчетом, и сегодня {0} видит слабые места врага.", "A cunning flame flickered in the bamboo ash. The {1} found a hidden path between luck and calculation, and today {0} sees the enemy's weak points.", "Bambu külünün içinde kurnaz bir ateş parladı. {1} şans ile hesap arasında gizli yolu buldu ve bugün {0} düşmanın zayıf noktalarını görüyor.", "In der Bambusasche flackerte ein listiges Feuer. Die {1} fanden einen verborgenen Pfad zwischen Glueck und Kalkuel, und heute erkennt {0} die Schwachstellen des Feindes.") },
            { "battle.daily.lore.dragon", new Translation("Под корнями сгоревшего бамбука проснулось древнее тепло. Род {1} принял знак глубин, и сегодня {0} несет в бой силу старого пламени.", "Ancient heat woke beneath the roots of the burned bamboo. The {1} accepted the sign from the depths, and today {0} carries old flame into battle.", "Yanmış bambunun kökleri altında kadim bir sıcaklık uyandı. {1} derinlerden gelen işareti kabul etti ve bugün {0} savaşa eski alevin gücünü taşıyor.", "Unter den Wurzeln des verbrannten Bambus erwachte alte Hitze. Die {1} nahmen das Zeichen aus der Tiefe an, und heute traegt {0} die Kraft alten Feuers in den Kampf.") },
            { "battle.daily.lore.dog", new Translation("У входа в пепельный лес зажегся верный сторожевой свет. Род {1} держит границу, и сегодня {0} получает благословение защиты.", "A faithful watchlight lit at the entrance to the ash forest. The {1} hold the border, and today {0} receives a blessing of protection.", "Kul ormaninin girisinde sadik bir nobet isigi yandi. {1} siniri tutuyor ve bugün {0} korunma kutsamasini aliyor.", "Am Eingang des Aschewaldes entzuendete sich ein treues Wachlicht. Die {1} halten die Grenze, und heute erhaelt {0} den Segen des Schutzes.") },
            { "battle.daily.lore.default", new Translation("В сгоревшем бамбуковом лесу появился новый знак. Сегодня {0} получает особый боевой бонус.", "A new sign appeared in the burned bamboo forest. Today {0} receives a special battle bonus.", "Yanmış bambu ormanında yeni bir işaret belirdi. Bugün {0} özel bir savaş bonusu alıyor.", "Im verbrannten Bambuswald erschien ein neues Zeichen. Heute erhaelt {0} einen besonderen Kampfbonus.") },
            { "weekly.day_reward", new Translation("Награда дня {0}", "Day {0} Reward", "{0}. Gün Ödülü") },
            { "weekly.time_error", new Translation("Ошибка времени", "Time error detected", "Zaman hatası") },
            { "weekly.available", new Translation("Награда доступна", "Reward available", "Ödül hazır") },
            { "weekly.claimed_today", new Translation("Сегодняшняя награда уже получена", "Reward already claimed today", "Bugünkü ödül alındı") },
            { "weekly.no_profile", new Translation("Нет профиля", "No Profile", "Profil Yok") },
            { "weekly.button_available", new Translation("Награда\nДень {0}", "Reward\nDay {0}", "Ödül\nGün {0}") },
            { "weekly.claimed", new Translation("Получено", "Claimed", "Alındı") },

            { "mahjong.score", new Translation("Счет: {0}", "Score: {0}", "Skor: {0}") },
            { "mahjong.reward", new Translation("Награда: {0} {1}", "Reward: {0} {1}", "Ödül: {0} {1}") },
            { "mahjong.story", new Translation("История", "Story", "Hikaye") },
            { "mahjong.battle", new Translation("Битва", "Battle", "Savaş") },
            { "main.mahjong.title", new Translation("Mahjong Symbiosis", "Mahjong Symbiosis", "Mahjong Symbiosis", "Mahjong Symbiosis") },
            { "main.mahjong.body", new Translation("Игра про внимание, память и выбор пары: открывайте плитки, читайте символы поля и выбирайте между спокойным прохождением и боевым соревнованием.", "A game of focus, memory, and pair choices: read the board symbols, match tiles, and choose between calm progression and competitive battle.", "Dikkat, hafıza ve eş seçme oyunu: tahta sembollerini oku, taşları eşleştir, sakin ilerleme veya rekabetçi savaş seç.", "Ein Spiel ueber Fokus, Gedaechtnis und Paarwahl: Brettsymbole lesen, Steine kombinieren und zwischen ruhigem Fortschritt und Kampf waehlen.") },
            { "main.mahjong.endless.title", new Translation("Endless Mahjong", "Endless Mahjong", "Endless Mahjong", "Endless Mahjong") },
            { "main.mahjong.endless.body", new Translation("Спокойный режим уровней: собирайте пары, проходите всё более сложные раскладки и развивайте прогресс без прямого соперника.", "A calm level mode: match pairs, clear increasingly complex layouts, and grow your progress without a direct opponent.", "Sakin seviye modu: eşleri bul, giderek zorlaşan dizilimleri temizle ve rakipsiz ilerle.", "Ruhiger Levelmodus: Paare finden, immer komplexere Layouts loesen und ohne direkten Gegner vorankommen.") },
            { "main.mahjong.battle.title", new Translation("Mahjong Battle", "Mahjong Battle", "Mahjong Battle", "Mahjong Battle") },
            { "main.mahjong.battle.body", new Translation("Боевой режим: выбирайте персонажа, играйте против соперника, наносите урон удачными ходами и растите в лигах.", "Combat mode: choose a character, play against an opponent, deal damage with strong moves, and climb the leagues.", "Savaş modu: karakter seç, rakibe karşı oyna, güçlü hamlelerle hasar ver ve liglerde yüksel.", "Kampfmodus: Charakter waehlen, gegen Gegner spielen, mit starken Zuegen Schaden verursachen und in Ligen aufsteigen.") },
            { "mahjong.lobby.story.title", new Translation("Сюжетный режим", "Story Mode", "Hikaye Modu", "Story-Modus") },
            { "mahjong.lobby.story.body", new Translation("Образовательные главы Mahjong: выбирай тему, открывай уровни и проходи этапы с фактами, символами и культурными связями.", "Educational Mahjong chapters: choose a theme, open levels, and play stages built around facts, symbols, and cultural connections.", "Egitici Mahjong bolumleri: tema seç, seviyeleri ac ve bilgileri, sembolleri ve kulturel baglari anlatan asamalari oyna.", "Lernkapitel fuer Mahjong: Thema waehlen, Stufen oeffnen und Etappen mit Fakten, Symbolen und kulturellen Verbindungen spielen.") },
            { "mahjong.story.categories.title", new Translation("Story Mode", "Story Mode", "Hikaye Modu", "Story-Modus") },
            { "mahjong.story.categories.hint", new Translation("Пройди обучение, и перед тобой откроется весь путь Story Mode.", "Complete the tutorial, and the whole Story Mode path will open before you.", "Eğitimi tamamla; Story Mode'un bütün yolu önünde açılacak.", "Schliesse das Tutorial ab, und der ganze Story-Mode-Pfad oeffnet sich vor dir.") },
            { "mahjong.story.chapter.tutorial.title", new Translation("Tutorial", "Tutorial", "Eğitim", "Tutorial") },
            { "mahjong.story.chapter.tutorial.subtitle", new Translation("Основы правил, открытых сторон, пар и внимания.", "Rules, open sides, pairs, and attention basics.", "Kurallar, acik kenarlar, esler ve dikkat temeli.", "Regeln, freie Seiten, Paare und Aufmerksamkeit.") },
            { "mahjong.story.chapter.world.title", new Translation("Dünya", "Dünya", "Dünya", "Dünya") },
            { "mahjong.story.chapter.world.subtitle", new Translation("Мир знаний: страны, космос, человек и природа. Сейчас первой открывается ветка Countries.", "The world of knowledge: countries, cosmos, human, and nature. Countries opens first.", "Bilgi dünyası: ülkeler, kozmos, insan ve doğa. İlk olarak Ülkeler açılır.", "Welt des Wissens: Laender, Kosmos, Mensch und Natur. Countries oeffnet zuerst.") },
            { "mahjong.story.chapter.world.status", new Translation("Открывает Countries, Cosmos, Human, Nature", "Opens Countries, Cosmos, Human, Nature", "Ülkeler, Kozmos, İnsan ve Doğa açar", "Oeffnet Countries, Cosmos, Human, Nature") },
            { "mahjong.story.branch.countries.title", new Translation("Countries", "Countries", "Ülkeler", "Countries") },
            { "mahjong.story.branch.countries.subtitle", new Translation("China и Turkey готовы", "China and Turkey are ready", "Çin ve Turkiye hazır", "China und Tuerkei sind bereit") },
            { "mahjong.story.branch.cosmos.title", new Translation("Cosmos", "Cosmos", "Kozmos", "Kosmos") },
            { "mahjong.story.branch.cosmos.subtitle", new Translation("Планеты, орбиты и исследование Вселенной.", "Planets, orbits, and exploration of the universe.", "Gezegenler, yorungeler ve evren kesfi.", "Planeten, Umlaufbahnen und Erforschung des Universums.") },
            { "mahjong.story.branch.human.title", new Translation("Human", "Human", "İnsan", "Mensch") },
            { "mahjong.story.branch.human.subtitle", new Translation("Материки, города, языки и общее наследие.", "Continents, cities, languages, and shared heritage.", "Kitalar, sehirler, diller ve ortak miras.", "Kontinente, Staedte, Sprachen und gemeinsames Erbe.") },
            { "mahjong.story.branch.nature.title", new Translation("Nature", "Nature", "Doğa", "Natur") },
            { "mahjong.story.branch.nature.subtitle", new Translation("Экосистемы, вода, леса и равновесие жизни.", "Ecosystems, water, forests, and the balance of life.", "Ekosistemler, su, ormanlar ve yasam dengesi.", "Oekosysteme, Wasser, Waelder und Gleichgewicht des Lebens.") },
            { "mahjong.story.countries.title", new Translation("Countries", "Countries", "Ülkeler", "Countries") },
            { "mahjong.story.countries.subtitle", new Translation("Выбери страну: China открывает путь, Turkey идёт следующей.", "Choose a country: China opens the path, Turkey follows next.", "Ulke sec: yolu Çin acar, sonra Turkiye gelir.", "Waehle ein Land: China oeffnet den Weg, danach folgt die Tuerkei.") },
            { "mahjong.story.chapter.china.title", new Translation("Китай", "China", "Çin", "China") },
            { "mahjong.story.chapter.china.subtitle", new Translation("История, культура, символы и изобретения.", "History, culture, symbols, and inventions.", "Tarih, kültür, semboller ve icatlar.", "Geschichte, Kultur, Symbole und Erfindungen.") },
            { "mahjong.story.chapter.turkey.title", new Translation("Турция", "Turkey", "Türkiye", "Tuerkei") },
            { "mahjong.story.chapter.turkey.subtitle", new Translation("Будущая глава для истории, городов и культурных символов.", "Future chapter for history, cities, and cultural symbols.", "Tarih, şehirler ve kültür sembolleri için gelecek bölüm.", "Kuenftiges Kapitel fuer Geschichte, Staedte und Kultursymbole.") },
            { "mahjong.story.chapter.future.title", new Translation("Глава {0}", "Chapter {0}", "Bolum {0}", "Kapitel {0}") },
            { "mahjong.story.chapter.future.subtitle", new Translation("Зарезервировано под будущую образовательную тему.", "Reserved for a future educational topic.", "Gelecek eğitici konu için ayrıldı.", "Fur ein kunftiges Lernthema reserviert.") },
            { "mahjong.story.chapter.stage_count", new Translation("{0} этапов", "{0} stages", "{0} asama", "{0} Stufen") },
            { "mahjong.story.chapter.locked", new Translation("закрыто", "locked", "kilitli", "gesperrt") },
            { "mahjong.story.chapter.soon", new Translation("скоро", "soon", "yakinda", "bald") },
            { "mahjong.story.locked.tutorial", new Translation("Закрыто: пройди Tutorial", "Locked: finish Tutorial", "Kilitli: Tutorial'i bitir", "Gesperrt: Tutorial abschliessen") },
            { "mahjong.story.difficulty.easy", new Translation("Easy", "Easy", "Easy", "Easy") },
            { "mahjong.story.difficulty.easy.body", new Translation("Затемнение и подсказка пары", "Dimmed stones and pair hint", "Karartma ve cift ipucu", "Gedimmte Steine und Paarhinweis") },
            { "mahjong.story.difficulty.medium", new Translation("Medium", "Medium", "Medium", "Medium") },
            { "mahjong.story.difficulty.medium.body", new Translation("Без затемнения и подсказок", "No dimming, no hints", "Karartma yok, ipucu yok", "Kein Dimmen, keine Hinweise") },
            { "mahjong.story.difficulty.hardcore", new Translation("Hardcore", "Hardcore", "Hardcore", "Hardcore") },
            { "mahjong.story.difficulty.hardcore.body", new Translation("Ошибка сбрасывает цепочку", "One loss resets the chain", "Bir yenilgi zinciri sifirlar", "Eine Niederlage setzt die Reihe zurueck") },
            { "mahjong.story.hardcore.locked_stage", new Translation("Hardcore: пройди этапы подряд", "Hardcore: clear stages in order", "Hardcore: asamalari sirayla bitir", "Hardcore: Stufen der Reihe nach") },
            { "mahjong.story.hardcore.replay_level", new Translation("Пройти уровень заново", "Replay whole level", "Seviyeyi bastan oyna", "Ganzes Level erneut spielen") },
            { "mahjong.story.stage.completed", new Translation("Пройден", "Completed", "Tamamlandi", "Abgeschlossen") },
            { "mahjong.story.stage.not_completed", new Translation("Не пройден", "Not completed", "Tamamlanmadi", "Nicht abgeschlossen") },
            { "mahjong.story.stage.best_score", new Translation("Лучший score: {0}", "Best score: {0}", "En iyi score: {0}", "Bester Score: {0}") },
            { "mahjong.story.level.best_score_total", new Translation("Общий максимальный score: {0}", "Total max score: {0}", "Toplam max score: {0}", "Gesamt-Max-Score: {0}") },
            { "mahjong.story.stage.play", new Translation("Пройти", "Play", "Oyna", "Spielen") },
            { "mahjong.story.stage.replay", new Translation("Пройти заново", "Replay", "Yeniden oyna", "Erneut spielen") },
            { "mahjong.story.stage.level", new Translation("Уровень {0}", "Level {0}", "Seviye {0}", "Stufe {0}") },
            { "mahjong.story.stage.empty", new Translation("Скоро появятся этапы этой главы.", "Stages are coming soon.", "Bu bolumun asamalari yakinda.", "Stufen folgen bald.") },
            { "mahjong.story.tutorial.stage.1", new Translation("Первая пара\nНайди два одинаковых открытых камня и убери их с поля.", "First pair\nFind two matching open tiles and clear them from the board.", "İlk çift\nAynı iki açık taşı bul ve tahtadan kaldır.", "Erstes Paar\nFinde zwei gleiche freie Steine und entferne sie vom Feld.") },
            { "mahjong.story.tutorial.stage.2", new Translation("Открытые стороны\nКамень можно взять, если слева или справа есть свободный путь.", "Open sides\nA tile can be taken when its left or right side is free.", "Açık kenarlar\nBir taş, solu veya sagi aciksa alinabilir.", "Freie Seiten\nEin Stein ist spielbar, wenn links oder rechts ein Weg frei ist.") },
            { "mahjong.story.tutorial.stage.3", new Translation("Смотри на шаг вперёд\nЛучший ход открывает следующую пару, а не только убирает текущую.", "Look one move ahead\nThe best move opens the next pair, not only the current one.", "Bir hamle sonrasini gor\nEn iyi hamle sadece cifti almaz, sonraki cifti de acar.", "Einen Zug voraus\nDer beste Zug oeffnet das naechste Paar, statt nur das aktuelle zu nehmen.") },
            { "mahjong.story.tutorial.stage.4", new Translation("Цепочка ходов\nКаждая верная пара подряд усиливает темп и готовит combo.", "Move chain\nEvery correct pair in a row builds momentum and prepares a combo.", "Hamle zinciri\nArka arkaya dogru ciftler tempo kurar ve combo hazırlar.", "Zugkette\nJedes richtige Paar in Folge baut Tempo auf und bereitet ein Combo vor.") },
            { "mahjong.story.tutorial.stage.5", new Translation("Combo\nСобери пять верных пар подряд: combo включится и даст больше score.", "Combo\nClear five correct pairs in a row: combo activates and gives more score.", "Combo\nPes pese bes dogru cift yap: combo acilir ve daha fazla score verir.", "Combo\nRaeume fuenf richtige Paare in Folge ab: Combo startet und gibt mehr Score.") },
            { "mahjong.result.rescue", new Translation("Спасти ход", "Rescue move", "Hamleyi kurtar", "Zug retten") },
            { "mahjong.result.rescue_failed", new Translation("Спасение не удалось.", "Rescue failed.", "Kurtarma basarisiz.", "Rettung fehlgeschlagen.") },
            { "mahjong.result.rescue_ad_not_ready", new Translation("Реклама пока не готова.", "Ad is not ready yet.", "Reklam henüz hazır değil.", "Anzeige ist noch nicht bereit.") },
            { "mahjong.lobby.endless.title", new Translation("Бесконечный Mahjong", "Endless Mahjong", "Endless Mahjong", "Endless Mahjong") },
            { "mahjong.lobby.endless.body", new Translation("Бесконечный спокойный режим: каждая новая раскладка тренирует внимание, память, чтение свободных краев и планирование пар.", "A calm endless mode: each new layout trains attention, memory, open-edge reading, and pair planning.", "Sakin endless modu: her yeni dizilim dikkati, hafizayi, acik kenar okumayi ve cift planlamayi calistirir.", "Ruhiger Endless-Modus: jeder neue Aufbau trainiert Aufmerksamkeit, Gedaechtnis, offene Kanten und Paarplanung.") },
            { "mahjong.intro.start", new Translation("Начать", "Start", "Başla", "Start") },
            { "mahjong.bag.title", new Translation("Çanta", "Çanta", "Çanta", "Çanta") },
            { "mahjong.bag.subtitle", new Translation("Запасы для спокойной партии Mahjong", "Supplies for a calm Mahjong run", "Sakin Mahjong oyunu için çanta", "Vorrat fuer eine ruhige Mahjong-Runde") },
            { "mahjong.bag.hint", new Translation("Подсветить пару", "Highlight Pair", "Eşi göster", "Paar zeigen") },
            { "mahjong.bag.shuffle", new Translation("Перемешать", "Shuffle", "Karıştır", "Mischen") },
            { "mahjong.bag.undo", new Translation("Ход назад", "Undo", "Geri al", "Rueckgaengig") },
            { "mahjong.bag.ad", new Translation("РЕКЛАМА +1", "AD +1", "REKLAM +1", "WERBUNG +1") },
            { "mahjong.bag.pack", new Translation("КУПИТЬ PACK", "BUY PACK", "PACK AL", "PACK KAUFEN") },
            { "mahjong.bag.ad_loading", new Translation("Открываем рекламу...", "Opening ad...", "Reklam aciliyor...", "Werbung wird geoeffnet...") },
            { "mahjong.bag.ad_unavailable", new Translation("Реклама сейчас недоступна.", "Ad is not available right now.", "Reklam simdi kullanilamiyor.", "Werbung ist gerade nicht verfuegbar.") },
            { "mahjong.bag.purchase_unavailable", new Translation("Покупки пока не подключены.", "Purchases are not connected yet.", "Satın almalar henüz bağlanmadı.", "Kaeufe sind noch nicht verbunden.") },
            { "mahjong.bag.added", new Translation("+{0} к каждому запасу.", "+{0} to each supply.", "Her destege +{0}.", "+{0} zu jedem Vorrat.") },
            { "mahjong.bag.pack_added", new Translation("Pack добавлен: +{0} к каждому запасу.", "Pack added: +{0} to each supply.", "Pack eklendi: her destege +{0}.", "Pack hinzugefuegt: +{0} zu jedem Vorrat.") },
            { "main.info.profile.title", new Translation("Профиль", "Profile", "Profil", "Profil") },
            { "main.info.profile.body", new Translation("Один аккаунт Symbiosis поддерживает до трёх профилей. Поэтому с одного аккаунта могут играть до трёх человек — каждый под своим именем, аватаром и ID.", "One Symbiosis account supports up to three profiles. This allows up to three people to play from the same account, each with their own name, avatar, and ID.", "Bir Symbiosis hesabı en fazla üç profili destekler. Böylece aynı hesaptan üç kişiye kadar herkes kendi adı, avatarı ve ID'siyle oynayabilir.", "Ein Symbiosis-Konto unterstuetzt bis zu drei Profile. So koennen bis zu drei Personen dasselbe Konto mit eigenem Namen, Avatar und eigener ID nutzen.") },
            { "main.info.mail.title", new Translation("Почта", "Mail", "Posta", "Post") },
            { "main.info.mail.body", new Translation("Почта хранит системные сообщения, письма и награды, отправленные твоему профилю.", "Mail stores system messages, letters, and rewards sent to your profile.", "Posta, profiline gönderilen sistem mesajlarını, mektupları ve ödülleri saklar.", "Die Post bewahrt Systemnachrichten, Briefe und Belohnungen fuer dein Profil auf.") },
            { "main.info.vault.title", new Translation("Депо", "Vault", "Depo", "Lager") },
            { "main.info.vault.body", new Translation("Депо — общее хранилище всех профилей твоей династии. Ресурсы, внесённые одним профилем, доступны остальным.", "The vault is shared by every profile in your dynasty. Resources deposited by one profile are available to the others.", "Depo, hanedanındaki tüm profillerin ortak deposudur. Bir profil tarafından yatırılan kaynaklar diğer profiller tarafından da kullanılabilir.", "Das Lager wird von allen Profilen deiner Dynastie gemeinsam genutzt. Ressourcen eines Profils stehen auch den anderen zur Verfuegung.") },
            { "main.info.bank.title", new Translation("Банк", "Bank", "Banka", "Bank") },
            { "main.info.bank.body", new Translation("Банк обменивает личные аметисты на золото по установленному курсу и заранее показывает результат операции.", "The bank converts personal amethysts into gold at a fixed rate and shows the result before the exchange.", "Banka kişisel ametistleri sabit kurla altına çevirir ve işlemden önce sonucu gösterir.", "Die Bank tauscht persoenliche Amethyste zu einem festen Kurs in Gold und zeigt das Ergebnis vorab.") },
            { "main.info.exchange.title", new Translation("Пияса", "Market", "Piyasa", "Markt") },
            { "main.info.exchange.body", new Translation("Пияса показывает обменные курсы, направление движения и лимиты. Смотри сюда перед обменом ресурсов.", "Market shows exchange rates, direction, and limits. Check it before converting resources.", "Piyasa kurları, yönü ve limitleri gösterir. Kaynak değiştirmeden önce buraya bak.", "Der Markt zeigt Kurse, Richtung und Limits. Vor dem Tausch hier pruefen.") },
            { "main.info.rewards.title", new Translation("Награды", "Rewards", "Ödüller", "Belohnungen") },
            { "main.info.rewards.body", new Translation("Награды ведут семидневный цикл: каждый день открывает новый бонус, а ценность наград постепенно растёт.", "Rewards follow a seven-day cycle: each day unlocks a new bonus, and the rewards gradually become more valuable.", "Ödüller yedi günlük bir döngü izler: her gün yeni bir bonus açılır ve ödüller giderek değerlenir.", "Belohnungen folgen einem Sieben-Tage-Zyklus: Jeden Tag wird ein neuer, zunehmend wertvoller Bonus freigeschaltet.") },
            { "main.info.shop.title", new Translation("Магазин", "Shop", "Mağaza", "Shop") },
            { "main.info.shop.body", new Translation("Магазин содержит покупки, бесплатные бонусы и рекламные награды. Используй его для ускорения развития аккаунта.", "Shop contains purchases, free bonuses, and ad rewards. Use it to speed up account growth.", "Mağazada satın almalar, ücretsiz bonuslar ve reklam ödülleri bulunur. Hesabını hızlandırmak için kullan.", "Der Shop enthaelt Kaeufe, Gratisboni und Werbebelohnungen fuer schnelleren Fortschritt.") },
            { "main.info.alliance.title", new Translation("Иттифак", "Alliance", "İttifak", "Allianz") },
            { "main.info.alliance.body", new Translation("Иттифак открывает клановую игру: союзники, общий чат, казна, приглашения и совместные награды.", "Alliance opens clan play: allies, shared chat, treasury, invites, and group rewards.", "İttifak klan oyununu açar: müttefikler, ortak sohbet, hazine, davetler ve grup ödülleri.", "Allianz oeffnet Clan-Spiel: Verbuendete, Chat, Schatzkammer, Einladungen und Gruppenbelohnungen.") },
            { "alliance.unavailable.title", new Translation("Альянсы временно недоступны", "Alliances are temporarily unavailable", "İttifaklar geçici olarak kullanılamıyor", "Allianzen sind vorübergehend nicht verfügbar") },
            { "alliance.unavailable.body", new Translation("Система альянсов находится в разработке. Мы дорабатываем её перед открытием и временно ограничили доступ. Спасибо за понимание.", "The Alliance system is currently in development. We are preparing it for launch, so access is temporarily restricted. Thank you for your understanding.", "İttifak sistemi şu anda geliştirme aşamasındadır. Açılışa hazırladığımız için erişim geçici olarak sınırlandırılmıştır. Anlayışınız için teşekkür ederiz.", "Das Allianzsystem befindet sich derzeit in Entwicklung. Wir bereiten es für die Freigabe vor; der Zugriff ist deshalb vorübergehend eingeschränkt. Vielen Dank für dein Verständnis.") },
            { "main.info.friends.title", new Translation("Друзья", "Friends", "Arkadaşlar", "Freunde") },
            { "main.info.friends.body", new Translation("Друзья помогают быстро находить игроков, принимать заявки и держать связь с теми, с кем ты уже играл.", "The Friends section helps you find players, accept requests, and stay connected with people you played with.", "Arkadaşlar oyuncu bulmanı, istekleri kabul etmeni ve oynadığın kişilerle bağlantıda kalmanı sağlar.", "Freunde helfen, Spieler zu finden, Anfragen anzunehmen und Kontakte zu behalten.") },
            { "main.info.chat.title", new Translation("Общий чат", "Global Chat", "Genel Sohbet", "Globaler Chat") },
            { "main.info.chat.body", new Translation("Общий чат связывает игроков: пиши сообщения, переключай каналы и следи за живыми новостями сообщества.", "Global chat connects players: send messages, switch channels, and follow live community news.", "Genel sohbet oyuncuları bağlar: mesaj yaz, kanalları değiştir ve topluluk haberlerini takip et.", "Globaler Chat verbindet Spieler: schreiben, Kanaele wechseln und Community verfolgen.") },
            { "main.info.settings.title", new Translation("Настройки", "Settings", "Ayarlar", "Einstellungen") },
            { "main.info.settings.body", new Translation("Настройки управляют комфортом игры: звук, музыка, вибрация, язык, профиль и включение обучающих подсказок.", "Settings control play comfort: sound, music, vibration, language, profile, and tutorial hints.", "Ayarlar oyun konforunu yönetir: ses, müzik, titreşim, dil, profil ve eğitim ipuçları.", "Einstellungen steuern Komfort: Ton, Musik, Vibration, Sprache, Profil und Lernhinweise.") },
            { "main.intro.continue", new Translation("ПРОДОЛЖИТЬ", "CONTINUE", "DEVAM ET", "WEITER") },
            { "main.intro.profile.white", new Translation("Перед входом каждый выбирает свой профиль. Личные данные и прогресс хранятся отдельно, а Депо остаётся общим между всеми профилями аккаунта.", "Each person selects their own profile before entering. Personal data and progress are kept separately, while the Vault is shared by every profile on the account.", "Oyuna girmeden önce herkes kendi profilini seçer. Kişisel veriler ve ilerleme ayrı tutulur; Depo ise hesaptaki tüm profiller arasında ortaktır.", "Vor dem Einstieg waehlt jede Person ihr eigenes Profil. Persoenliche Daten und Fortschritt bleiben getrennt, das Lager wird von allen Profilen des Kontos gemeinsam genutzt.") },
            { "main.intro.friends.white", new Translation("Ищи игрока по имени, отправляй запрос и следи за входящими приглашениями. В друзья стоит добавлять тех, с кем ты действительно хочешь оставаться на связи.", "Search for a player by name, send a request, and watch incoming invitations. Add people you genuinely want to stay connected with.", "Oyuncuyu adıyla ara, istek gönder ve gelen davetleri takip et. Gerçekten iletişimde kalmak istediğin kişileri arkadaşlarına ekle.", "Suche Spieler nach Namen, sende Anfragen und beachte eingehende Einladungen. Fuege Menschen hinzu, mit denen du wirklich in Kontakt bleiben willst.") },
            { "main.intro.mail.white", new Translation("В письмах могут находиться вложения. Сначала забери предметы или валюту, а уже потом удаляй сообщение.", "Letters may contain attachments. Claim any items or currency before deleting the message.", "Mektuplarda ekler bulunabilir. Mesajı silmeden önce eşyaları veya para birimini teslim al.", "Briefe koennen Anhaenge enthalten. Hole Gegenstaende oder Waehrung ab, bevor du die Nachricht loeschst.") },
            { "main.intro.alliance.white", new Translation("Выбирай союз осознанно: общий чат, вклад участников, казна и совместные награды строятся на действиях всей команды.", "Choose your alliance thoughtfully: shared chat, member contributions, treasury, and group rewards depend on the whole team.", "İttifakını dikkatle seç: ortak sohbet, üye katkıları, hazine ve grup ödülleri tüm ekibin eylemlerine bağlıdır.", "Waehle deine Allianz bewusst: Chat, Beitraege, Schatzkammer und gemeinsame Belohnungen haengen vom ganzen Team ab.") },
            { "main.intro.mail.unavailable.black", new Translation("Мы готовим почту профиля — место для системных сообщений, писем, вложений и наград.", "We are preparing profile mail: a place for system messages, letters, attachments, and rewards.", "Profil postasını hazırlıyoruz: sistem mesajları, mektuplar, ekler ve ödüller için bir alan.", "Wir bereiten die Profilpost vor: einen Ort fuer Systemnachrichten, Briefe, Anhaenge und Belohnungen.") },
            { "main.intro.mail.unavailable.white", new Translation("Раздел ещё находится в разработке. Когда он откроется, мы отдельно сообщим об этом и покажем, как безопасно получать вложения.", "This section is still in development. When it opens, we will announce it and explain how to claim attachments safely.", "Bu bölüm hâlâ geliştiriliyor. Açıldığında duyuracağız ve ekleri güvenli biçimde nasıl alacağını göstereceğiz.", "Dieser Bereich ist noch in Entwicklung. Zur Freigabe informieren wir dich und erklaeren den sicheren Umgang mit Anhaengen.") },
            { "main.intro.alliance.unavailable.black", new Translation("Мы строим систему альянсов: союзников, общий чат, вклад участников, казну и совместные награды.", "We are building the Alliance system: allies, shared chat, member contributions, treasury, and group rewards.", "İttifak sistemini kuruyoruz: müttefikler, ortak sohbet, üye katkıları, hazine ve grup ödülleri.", "Wir entwickeln das Allianzsystem: Verbuendete, Chat, Beitraege, Schatzkammer und gemeinsame Belohnungen.") },
            { "main.intro.alliance.unavailable.white", new Translation("Сейчас доступ временно закрыт, пока мы доводим систему до рабочего состояния. После открытия раздел познакомит тебя со всеми возможностями.", "Access is temporarily closed while we bring the system to a ready state. Once it opens, the section will introduce every feature.", "Sistemi hazır hâle getirirken erişim geçici olarak kapalıdır. Açıldığında bölüm tüm özellikleri tanıtacak.", "Der Zugriff bleibt voruebergehend geschlossen, waehrend wir das System fertigstellen. Nach der Freigabe werden alle Funktionen vorgestellt.") },
            { "main.intro.vault.white", new Translation("Любой профиль может пополнять Депо из личного баланса и забирать ресурсы при необходимости. Все профили видят один общий запас.", "Any profile can deposit from its personal balance and withdraw resources when needed. Every profile sees the same shared reserve.", "Her profil kişisel bakiyesinden Depoya aktarım yapabilir ve gerektiğinde kaynak çekebilir. Tüm profiller aynı ortak stoku görür.", "Jedes Profil kann vom persoenlichen Guthaben einzahlen und bei Bedarf Ressourcen entnehmen. Alle Profile sehen denselben gemeinsamen Bestand.") },
            { "main.intro.bank.white", new Translation("Обмен здесь односторонний: аметисты превращаются в золото. Перед подтверждением проверь сумму и рассчитанный результат.", "Exchange here is one-way: amethysts become gold. Check the amount and calculated result before confirming.", "Buradaki dönüşüm tek yönlüdür: ametistler altına çevrilir. Onaylamadan önce miktarı ve hesaplanan sonucu kontrol et.", "Der Tausch ist hier einseitig: Amethyste werden zu Gold. Pruefe Betrag und Ergebnis vor der Bestaetigung.") },
            { "main.intro.exchange.white", new Translation("Пияса — информационный экран. Он помогает оценить курс и лимиты, но не выполняет обмен вместо тебя.", "Market is an information screen. It helps you assess rates and limits, but it does not perform the exchange for you.", "Piyasa bir bilgi ekranıdır. Kurları ve limitleri değerlendirmeni sağlar, ancak takası senin yerine yapmaz.", "Der Markt ist eine Informationsansicht. Er hilft bei Kursen und Limits, fuehrt den Tausch aber nicht fuer dich aus.") },
            { "main.intro.rewards.white", new Translation("Заходи каждый день цикла и забирай доступную награду. Дополнительный бонус за рекламу остаётся добровольным.", "Return on each day of the cycle and claim the available reward. The extra ad reward remains optional.", "Döngünün her günü geri gel ve açılan ödülü al. Reklam karşılığındaki ek bonus tamamen isteğe bağlıdır.", "Kehre an jedem Tag des Zyklus zurueck und hole die verfuegbare Belohnung ab. Der zusaetzliche Werbebonus bleibt freiwillig.") },
            { "main.intro.shop.white", new Translation("Здесь разделены бесплатные бонусы, добровольные рекламные награды и покупки. Выбирай только то, что подходит твоему пути развития.", "Free bonuses, optional ad rewards, and purchases are separated here. Choose only what fits your path of progression.", "Ücretsiz bonuslar, isteğe bağlı reklam ödülleri ve satın almalar burada ayrıdır. Yalnızca gelişim yoluna uygun olanı seç.", "Gratisboni, freiwillige Werbebelohnungen und Kaeufe sind hier getrennt. Waehle nur, was zu deinem Fortschritt passt.") },
            { "mahjong.level_select", new Translation("Выбор уровня", "Level Select", "Seviye Seç") },
            { "mahjong.reset_progress", new Translation("Сбросить прогресс", "Reset Progress", "Ilerlemeyi Sifirla") },
            { "mahjong.back", new Translation("Назад", "Back", "Geri") },
            { "battle.countdown.start", new Translation("СТАРТ", "START", "BASLA") },
            { "mahjong.title.novice", new Translation("Новичок", "Novice", "Caylak") },
            { "mahjong.title.story_seeker", new Translation("Искатель истории", "Story Seeker", "Hikaye Arayicisi") },
            { "mahjong.title.story_walker", new Translation("Путник истории", "Story Walker", "Hikaye Yolcusu") },
            { "mahjong.title.story_keeper", new Translation("Хранитель истории", "Story Keeper", "Hikaye Muhafizi") },
            { "mahjong.title.battle_first", new Translation("Первый бой", "First Duel", "İlk Duello") },
            { "mahjong.title.battle_veteran", new Translation("Ветеран битв", "Battle Veteran", "Savaş Ustaşı") },
            { "mahjong.title.battle_centurion", new Translation("Центурион", "Centurion", "Yuzbasi") },
            { "profile.titles", new Translation("Титулы", "Titles", "Unvanlar") },
            { "profile.title_selected", new Translation("Выбран", "Selected", "Seçildi") },

            { "void.title", new Translation("AVOYDER", "AVOYDER", "AVOYDER") },
            { "void.start", new Translation("НАЧАТЬ", "START", "BASLA") },
            { "void.level_select", new Translation("ВЫБОР УРОВНЯ", "LEVEL SELECT", "SEVIYE SEC") },
            { "void.level_complete", new Translation("УРОВЕНЬ ПРОЙДЕН", "LEVEL COMPLETE", "SEVIYE TAMAMLANDI") },
            { "void.retry", new Translation("ЕЩЕ РАЗ", "RETRY", "TEKRAR") },
            { "void.next", new Translation("СЛЕДУЮЩИЙ", "NEXT", "SONRAKI") },
            { "void.victory", new Translation("VOID ОЧИЩЕН", "VOID CLEARED", "VOID TEMIZLENDI") },
            { "void.defeat", new Translation("ПОГЛОЩЕН VOID", "ABSORBED BY VOID", "VOID TARAFINDAN YUTULDU") },
            { "void.hp", new Translation("HP {0} / {1}", "HP {0} / {1}", "CP {0} / {1}") },
            { "void.level", new Translation("УРОВЕНЬ {0}", "LEVEL {0}", "SEVIYE {0}") },
            { "void.score", new Translation("СЧЕТ {0}", "SCORE {0}", "SKOR {0}") },

            { "update.title", new Translation("Доступно обновление", "Update available", "Güncelleme var") },
            { "update.subtitle", new Translation("Вышла новая версия игры. Обновите Symbiosis, чтобы продолжить играть с актуальными исправлениями и онлайн-функциями.", "A new game version is available. Update Symbiosis to keep playing with the latest fixes and online features.", "Oyunun yeni surumu mevcut. Son duzeltmeler ve cevrimici özelliklerle oynamak icin Symbiosis'i guncelleyin.") },
            { "update.body_older", new Translation("Ваша версия устарела.", "Your version is out of date.", "Surumunuz eski.") },
            { "update.current_version", new Translation("Текущая версия: {0}", "Current version: {0}", "Mevcut surum: {0}") },
            { "update.latest_version", new Translation("Последняя версия: {0}", "Latest version: {0}", "Son surum: {0}") },
            { "update.required", new Translation("Это обновление обязательно для входа в игру.", "This update is required to enter the game.", "Oyuna girmek icin bu guncelleme zorunlu.") },
            { "update.notes_title", new Translation("Что изменилось", "What's new", "Yenilikler") },
            { "update.button", new Translation("Обновить", "Update", "Güncelle") },
            { "update.later", new Translation("Позже", "Later", "Sonra") },

            { "chat.title", new Translation("Общий чат", "Global Chat", "Genel Sohbet") },
            { "chat.intro.title", new Translation("ДОБРО ПОЖАЛОВАТЬ В ЧАТ", "WELCOME TO CHAT", "SOHBETE HOŞ GELDİN", "WILLKOMMEN IM CHAT") },
            { "chat.intro.black_line", new Translation(
                "Это общий чат Symbiosis. Здесь ты можешь общаться с другими игроками, обсуждать игру, делиться опытом и находить единомышленников.",
                "This is the Symbiosis global chat. Here you can talk with other players, discuss the game, share experience, and find like-minded people.",
                "Burası Symbiosis genel sohbeti. Burada diğer oyuncularla konuşabilir, oyunu tartışabilir, deneyimlerini paylaşabilir ve aynı düşüncedeki oyuncuları bulabilirsin.",
                "Dies ist der globale Chat von Symbiosis. Hier kannst du mit anderen Spielern sprechen, das Spiel diskutieren, Erfahrungen teilen und Gleichgesinnte finden.") },
            { "chat.intro.white_line", new Translation(
                "В разделе «{0}» можно напрямую оставить комментарий, идею или отзыв для команды. Каждое обращение сохраняется и передаётся нам на рассмотрение.",
                "In the “{0}” section, you can send the team a comment, idea, or review directly. Every submission is saved and sent to us for consideration.",
                "“{0}” bölümünde ekibe doğrudan yorum, fikir veya geri bildirim bırakabilirsin. Her başvuru kaydedilir ve değerlendirmemize iletilir.",
                "Im Bereich „{0}“ kannst du dem Team direkt einen Kommentar, eine Idee oder eine Rückmeldung senden. Jede Nachricht wird gespeichert und uns zur Prüfung vorgelegt.") },
            { "chat.intro.continue", new Translation("ПЕРЕЙТИ К ЧАТУ", "OPEN CHAT", "SOHBETE GEÇ", "ZUM CHAT") },
            { "chat.placeholder", new Translation("Сообщение", "Message", "Mesaj") },
            { "chat.send", new Translation("Отправить", "Send", "Gonder") },
            { "chat.empty", new Translation("Сообщений пока нет.", "No messages yet.", "Henuz mesaj yok.") },
            { "chat.channel.global", new Translation("Общий", "Global", "Genel") },
            { "chat.channel.mahjong", new Translation("Маджонг", "Mahjong", "Mahjong") },
            { "chat.channel.developer_support", new Translation("Разработчики", "Developers", "Geliştiriciler", "Entwickler") },
            { "chat.role.owner", new Translation("OWNER", "OWNER", "OWNER") },
            { "chat.support.placeholder", new Translation("Опишите проблему без паролей и личных данных", "Describe the issue without passwords or personal data", "Şifre veya kişisel bilgi vermeden sorunu açıklayın") },
            { "chat.support.empty", new Translation("Обращений пока нет.", "No support requests yet.", "Henüz destek talebi yok.") },
            { "chat.support.sent", new Translation("Обращение отправлено разработчикам.", "Support request sent to the developers.", "Destek talebi geliştiricilere gönderildi.") },
            { "chat.support.status_line", new Translation("Статус: {0}", "Status: {0}", "Durum: {0}") },
            { "chat.support.status.submitted", new Translation("Отправлено", "Submitted", "Gönderildi") },
            { "chat.support.status.voting", new Translation("На голосовании", "Voting", "Oylamada", "Abstimmung") },
            { "chat.support.status.confirmed", new Translation("Подтверждено", "Confirmed", "Onaylandı") },
            { "chat.support.status.under_review", new Translation("На рассмотрении", "Under review", "İnceleniyor") },
            { "chat.support.status.rejected", new Translation("Отказано", "Rejected", "Reddedildi") },
            { "chat.support.status.closed", new Translation("Вопрос закрыт", "Issue closed", "Konu kapatıldı") },
            { "chat.support.status_updated", new Translation("Статус обращения обновлён.", "Support status updated.", "Destek durumu güncellendi.") },
            { "chat.support.invalid_status", new Translation("Недопустимый статус обращения.", "Invalid support status.", "Geçersiz destek durumu.") },
            { "chat.support.manage_title", new Translation("{0} · {1}\nВыберите статус или прикрепите комментарий", "{0} · {1}\nChoose a status or attach a comment", "{0} · {1}\nDurum seçin veya yorum ekleyin") },
            { "chat.support.comment_placeholder", new Translation("Комментарий разработчика", "Developer comment", "Geliştirici yorumu") },
            { "chat.support.comment_send", new Translation("Прикрепить", "Attach", "Ekle") },
            { "chat.support.comment_empty", new Translation("Введите комментарий.", "Enter a comment.", "Yorum girin.") },
            { "chat.support.comment_added", new Translation("Комментарий прикреплён к обращению.", "Comment attached to the request.", "Yorum talebe eklendi.") },
            { "chat.support.vote.like", new Translation("Нравится · {0}", "Like · {0}", "Beğen · {0}", "Gefällt mir · {0}") },
            { "chat.support.vote.dislike", new Translation("Не нравится · {0}", "Dislike · {0}", "Beğenme · {0}", "Gefällt mir nicht · {0}") },
            { "chat.support.vote.recorded", new Translation("Ваш голос учтён.", "Your vote was recorded.", "Oyunuz kaydedildi.", "Deine Stimme wurde gezählt.") },
            { "chat.support.vote.removed", new Translation("Ваш голос снят.", "Your vote was removed.", "Oyunuz kaldırıldı.", "Deine Stimme wurde entfernt.") },
            { "chat.support.vote.inactive", new Translation("Голосование уже завершено.", "Voting has already ended.", "Oylama sona erdi.", "Die Abstimmung ist bereits beendet.") },
            { "chat.support.vote.failed", new Translation("Не удалось сохранить голос.", "Could not save the vote.", "Oy kaydedilemedi.", "Die Stimme konnte nicht gespeichert werden.") },
            { "chat.translation.original", new Translation("Оригинал: {0}", "Original: {0}", "Orijinal: {0}", "Original: {0}") },
            { "chat.translation.auto_on", new Translation("Автоперевод: ВКЛ", "Auto-translate: ON", "Otomatik çeviri: AÇIK", "Auto-Übersetzung: AN") },
            { "chat.translation.auto_off", new Translation("Автоперевод: ВЫКЛ", "Auto-translate: OFF", "Otomatik çeviri: KAPALI", "Auto-Übersetzung: AUS") },
            { "chat.error_empty", new Translation("Сообщение пустое.", "Message is empty.", "Mesaj bos.") },
            { "chat.moderated", new Translation("Сообщение очищено фильтром чата.", "Message cleaned by chat filter.", "Mesaj sohbet filtresi tarafindan temizlendi.") },
            { "chat.report", new Translation("Жалоба", "Report", "Sikayet") },
            { "chat.block", new Translation("Блок", "Block", "Engelle") },
            { "chat.cancel", new Translation("Отмена", "Cancel", "İptal") },
            { "chat.report_sent", new Translation("Жалоба отправлена.", "Report submitted.", "Sikayet gonderildi.") },
            { "chat.blocked", new Translation("Пользователь заблокирован.", "User blocked.", "Kullanici engellendi.") },
            { "chat.no_report_target", new Translation("Нет сообщения для жалобы.", "No message to report.", "Sikayet edilecek mesaj yok.") },
            { "chat.no_block_target", new Translation("Нет пользователя для блокировки.", "No user to block.", "Engellenecek kullanici yok.") },
            { "network.session_expired", new Translation("Сессия истекла. Войдите в профиль ещё раз.", "Session expired. Sign in to your profile again.", "Oturum sona erdi. Profilinize yeniden giriş yapın.", "Die Sitzung ist abgelaufen. Melde dich erneut bei deinem Profil an.") },
            { "network.session_recovery_wait", new Translation("Повторный вход временно недоступен. Попробуйте ещё раз через несколько секунд.", "Sign-in recovery is temporarily unavailable. Try again in a few seconds.", "Oturum yenileme geçici olarak kullanılamıyor. Birkaç saniye sonra tekrar deneyin.", "Die Wiederanmeldung ist vorübergehend nicht verfügbar. Versuche es in einigen Sekunden erneut.") },

            { "friends.title", new Translation("Друзья", "Friends", "Arkadaşlar", "Freunde") },
            { "friends.subtitle", new Translation("Играй вместе и оставайся на связи", "Play together and stay connected", "Birlikte oyna, bağlantıda kal", "Gemeinsam spielen und verbunden bleiben") },
            { "friends.my_friends", new Translation("Мои друзья", "My Friends", "Arkadaşlarım", "Meine Freunde") },
            { "friends.requests", new Translation("Заявки", "Requests", "İstekler", "Anfragen") },
            { "friends.count", new Translation("{0} онлайн  •  {1} всего", "{0} online  •  {1} total", "{0} çevrimiçi  •  toplam {1}", "{0} online  •  {1} gesamt") },
            { "friends.requests_count", new Translation("{0} новых  •  {1} отправлено", "{0} new  •  {1} sent", "{0} yeni  •  {1} gönderildi", "{0} neu  •  {1} gesendet") },
            { "friends.empty_title", new Translation("Пока здесь тихо", "It is quiet here", "Burası şimdilik sessiz", "Hier ist es noch ruhig") },
            { "friends.empty_hint", new Translation("Найди игрока по никнейму выше", "Find a player by nickname above", "Yukarıdan takma adla bir oyuncu bul", "Suche oben nach einem Spielernamen") },
            { "friends.requests_empty", new Translation("Новых заявок нет", "No new requests", "Yeni istek yok", "Keine neuen Anfragen") },
            { "friends.requests_empty_hint", new Translation("Новые приглашения появятся здесь", "New invitations will appear here", "Yeni davetler burada görünecek", "Neue Einladungen erscheinen hier") },
            { "friends.request_incoming", new Translation("Хочет добавить тебя в друзья", "Wants to add you as a friend", "Seni arkadaş olarak eklemek istiyor", "Möchte dich als Freund hinzufügen") },
            { "friends.request_outgoing", new Translation("Заявка отправлена", "Request sent", "İstek gönderildi", "Anfrage gesendet") },
            { "friends.online", new Translation("Онлайн", "Online", "Çevrimiçi", "Online") },
            { "friends.offline", new Translation("Офлайн", "Offline", "Çevrimdışı", "Offline") },
            { "friends.search_friend", new Translation("уже в друзьях", "already a friend", "zaten arkadaşın", "bereits befreundet") },
            { "friends.search_requested", new Translation("заявка отправлена", "request sent", "istek gönderildi", "Anfrage gesendet") },
            { "friends.search_incoming", new Translation("ждёт твоего ответа", "waiting for your answer", "yanıtını bekliyor", "wartet auf deine Antwort") },
            { "friends.search_empty", new Translation("Игроки не найдены", "No players found", "Oyuncu bulunamadı", "Keine Spieler gefunden") },
            { "friends.empty_online", new Translation("Нет активных друзей.", "No active friends.", "Aktif arkadaş yok.", "Keine Freunde online.") },
            { "friends.empty_offline", new Translation("Нет друзей офлайн.", "No offline friends.", "Çevrimdışı arkadaş yok.", "Keine Freunde offline.") },
            { "friends.request_sent", new Translation("Запрос отправлен.", "Request sent.", "İstek gönderildi.", "Anfrage gesendet.") },
            { "friends.error_profile", new Translation("Для друзей нужен серверный профиль.", "Friends require server profile.", "Arkadaşlar için sunucu profili gerekir.", "Für Freunde wird ein Serverprofil benötigt.") },
            { "friends.error_request_failed", new Translation("Запрос друзей не удался.", "Friends request failed.", "Arkadaşlık isteği başarısız.", "Freundschaftsanfrage fehlgeschlagen.") },
            { "friends.error_invalid_response", new Translation("Некорректный ответ друзей.", "Invalid friends response.", "Geçersiz arkadaş yanıtı.", "Ungültige Freundesantwort.") },

            { "symbigrid.back_to_platform", new Translation("Назад на платформу", "Back to Platform", "Platforma Dön", "Zur Plattform") },
            { "symbigrid.back", new Translation("НАЗАД", "BACK", "GERİ", "ZURUECK") },
            { "symbigrid.start", new Translation("СТАРТ", "START", "BAŞLA", "START") },
            { "symbigrid.menu", new Translation("МЕНЮ", "MENU", "MENÜ", "MENU") },
            { "symbigrid.settings", new Translation("НАСТРОЙКИ", "SETTINGS", "AYARLAR", "EINSTELLUNGEN") },
            { "symbigrid.mode", new Translation("РЕЖИМ", "MODE", "MOD", "MODUS") },
            { "symbigrid.new_run", new Translation("НОВЫЙ ЗАБЕГ", "NEW RUN", "YENİ TUR", "NEUER LAUF") },
            { "symbigrid.score", new Translation("СЧЁТ", "SCORE", "SKOR", "PUNKTE") },
            { "symbigrid.reroll_ad", new Translation("РЕКЛАМА  ОБНОВИТЬ", "REROLL  AD", "REKLAM  YENİLE", "WERBUNG  NEU") },
            { "symbigrid.reroll_ad_incomplete", new Translation("Посмотри рекламу полностью, чтобы обновить фигуры.", "Watch the full ad to reroll the pieces.", "Parçaları yenilemek için reklamın tamamını izle.", "Sieh dir die Werbung vollständig an, um die Teile neu zu ziehen.") },
            { "symbigrid.reroll_ad_failed", new Translation("Реклама не открылась. Попробуй ещё раз чуть позже.", "The ad could not open. Try again shortly.", "Reklam açılamadı. Biraz sonra tekrar dene.", "Die Werbung konnte nicht geöffnet werden. Versuche es gleich noch einmal.") },
            { "symbigrid.reroll_complete", new Translation("Фигуры обновлены.", "Pieces rerolled.", "Parçalar yenilendi.", "Teile wurden neu gezogen.") },
            { "symbigrid.flag", new Translation("ФЛАГ", "FLAG", "BAYRAK", "FLAGGE") },
            { "symbigrid.flag_on", new Translation("ФЛАГ ВКЛ", "FLAG ON", "BAYRAK AÇIK", "FLAGGE AN") },
            { "symbigrid.rotate", new Translation("ПОВОРОТ", "ROTATE", "DÖNDÜR", "DREHEN") },
            { "symbigrid.drop", new Translation("ВНИЗ", "DROP", "DÜŞÜR", "FALLEN") },
            { "symbigrid.retro_control_gesture", new Translation("RETROGRID: ЖЕСТЫ", "RETROGRID CONTROL: GESTURE", "RETROGRID KONTROL: HAREKET", "RETROGRID: GESTEN") },
            { "symbigrid.retro_gesture_hint", new Translation("ЖЕСТЫ: влево/вправо - движение, вверх - поворот, вниз - быстрее, двойной тап - сразу вниз.", "GESTURE: left/right to move, swipe up to rotate, drag down to speed up, double tap to drop.", "HAREKET: sola/sağa kaydır, yukarı çevir, aşağı hızlandır, çift dokun düşür.", "GESTEN: links/rechts bewegen, hoch drehen, runter beschleunigen, doppeltippen fallen.") },
            { "symbigrid.minefield_choose", new Translation("Выбери классическое минное поле", "Choose classic minefield", "Klasik mayın alanını seç", "Klassisches Minenfeld waehlen") },
            { "symbigrid.difficulty.beginner", new Translation("НОВИЧОК  9x9  10 МИН", "BEGINNER  9x9  10 MINES", "BAŞLANGIÇ  9x9  10 MAYIN", "ANFAENGER  9x9  10 MINEN") },
            { "symbigrid.difficulty.intermediate", new Translation("СРЕДНИЙ  9x18  40 МИН", "INTERMEDIATE  9x18  40 MINES", "ORTA  9x18  40 MAYIN", "MITTEL  9x18  40 MINEN") },
            { "symbigrid.difficulty.expert", new Translation("ЭКСПЕРТ  16x30  99 МИН", "EXPERT  16x30  99 MINES", "UZMAN  16x30  99 MAYIN", "EXPERTE  16x30  99 MINEN") },
            { "symbigrid.controls", new Translation("УПРАВЛЕНИЕ", "CONTROLS", "KONTROLLER", "STEUERUNG") },
            { "symbigrid.close", new Translation("ЗАКРЫТЬ", "CLOSE", "KAPAT", "SCHLIESSEN") },
            { "symbigrid.mine_unavailable.status", new Translation("НА ДОРАБОТКЕ", "UNDER REFINEMENT", "GELİŞTİRİLİYOR", "IN UEBERARBEITUNG") },
            { "symbigrid.mine_unavailable.body", new Translation("Режим SymbiMine временно закрыт. Мы дорабатываем игровой процесс и откроем доступ, когда режим будет готов.", "SymbiMine is temporarily unavailable. We are refining the gameplay and will reopen access when the mode is ready.", "SymbiMine geçici olarak kapalı. Oynanışı geliştiriyoruz ve mod hazır olduğunda erişimi yeniden açacağız.", "SymbiMine ist voruebergehend geschlossen. Wir ueberarbeiten das Gameplay und oeffnen den Zugang wieder, sobald der Modus bereit ist.") },
            { "symbigrid.preview.retro", new Translation("Укладывай падающие блоки, поворачивай фигуры и очищай полные линии, пока сетка не заполнилась.", "Drop falling blocks, rotate them into place, and clear full lines before the grid fills up.", "Düşen blokları yerleştir, parçaları çevir ve ızgara dolmadan tam satırları temizle.", "Lass Bloecke fallen, drehe sie passend und raeume volle Linien, bevor das Feld voll ist.") },
            { "symbigrid.preview.classic", new Translation("Выбирай одну из трёх фигур, ставь её на поле и очищай заполненные строки или столбцы.", "Place one of three pieces on the board. Fill rows or columns to clear space and keep the run alive.", "Üç parçadan birini seç, tahtaya yerleştir ve dolu satır ya da sütunları temizle.", "Setze eines von drei Teilen aufs Feld und raeume volle Reihen oder Spalten.") },
            { "symbigrid.preview.mine", new Translation("Открывай безопасные клетки, читай числа и отмечай скрытые мины до взрыва поля.", "Open safe cells, read the numbers, and flag hidden mines before the field detonates.", "Güvenli hücreleri aç, sayıları oku ve alan patlamadan gizli mayınları işaretle.", "Oeffne sichere Felder, lies die Zahlen und markiere versteckte Minen vor der Explosion.") },
            { "symbigrid.controls.retro", new Translation("Свайп влево или вправо - движение.\n\nСвайп вверх - поворот фигуры.\n\nСвайп вниз - быстрое падение.\n\nДвойной тап - сразу вниз.", "Swipe left or right to move.\n\nSwipe up to rotate the piece.\n\nSwipe down to drop faster.\n\nDouble tap to hard drop.", "Hareket için sola veya sağa kaydır.\n\nDöndürmek için yukarı kaydır.\n\nHızlı düşüş için aşağı kaydır.\n\nHemen düşürmek için çift dokun.", "Wische links oder rechts zum Bewegen.\n\nWische nach oben zum Drehen.\n\nWische nach unten fuer schnelles Fallen.\n\nDoppeltippen fuer Sofortfall.") },
            { "symbigrid.controls.classic", new Translation("Перетащи одну из трёх фигур.\n\nПоставь её на свободные клетки.\n\nПолные строки и столбцы очищаются.", "Drag one of three pieces.\n\nPlace it on open cells.\n\nFull rows and columns clear.", "Üç parçadan birini sürükle.\n\nBoş hücrelere yerleştir.\n\nDolu satır ve sütunlar temizlenir.", "Ziehe eines von drei Teilen.\n\nSetze es auf freie Felder.\n\nVolle Reihen und Spalten verschwinden.") },
            { "symbigrid.controls.mine", new Translation("Тап по клетке - открыть.\n\nFLAG отмечает мины.\n\nЧисла показывают мины рядом.", "Tap cells to open them.\n\nUse FLAG to mark mines.\n\nNumbers show nearby mines.", "Hücre açmak için dokun.\n\nMayınları işaretlemek için FLAG kullan.\n\nSayılar yakındaki mayınları gösterir.", "Tippe Felder zum Oeffnen.\n\nFLAG markiert Minen.\n\nZahlen zeigen nahe Minen.") },

            { "sudoku.title", new Translation("SymSudoku", "SymSudoku", "SymSudoku", "SymSudoku") },
            { "sudoku.leaderboard", new Translation("Лидерборд", "Leaderboard", "Lider Tablosu", "Bestenliste") },
            { "sudoku.menu", new Translation("В меню", "Menu", "Menü", "Menu") },
            { "sudoku.lobby", new Translation("Лобби", "Lobby", "Lobi", "Lobby") },
            { "sudoku.new", new Translation("Новый", "New", "Yeni", "Neu") },
            { "sudoku.back", new Translation("Назад", "Back", "Geri", "Zurueck") },
            { "sudoku.ad", new Translation("Реклама", "Ad", "Reklam", "Werbung") },
            { "sudoku.subtitle", new Translation("Выберите сложность, затем уровень в горизонтальной карусели.", "Choose difficulty, then pick a level from the horizontal carousel.", "Zorluğu seç, sonra yatay karuselden seviye seç.", "Waehle Schwierigkeit, dann ein Level aus dem horizontalen Karussell.") },
            { "sudoku.easy", new Translation("Легко", "Easy", "Kolay", "Leicht") },
            { "sudoku.easy.desc", new Translation("Спокойные уровни", "Calm levels", "Sakin seviyeler", "Ruhige Level") },
            { "sudoku.medium", new Translation("Средне", "Medium", "Orta", "Mittel") },
            { "sudoku.medium.desc", new Translation("Больше логики", "More logic", "Daha fazla mantık", "Mehr Logik") },
            { "sudoku.hard", new Translation("Сложно", "Hard", "Zor", "Schwer") },
            { "sudoku.hard.desc", new Translation("Меньше подсказок", "Fewer clues", "Daha az ipucu", "Weniger Hinweise") },
            { "sudoku.levels_range", new Translation("{0} - уровни {1}-{2}", "{0} - levels {1}-{2}", "{0} - seviyeler {1}-{2}", "{0} - Level {1}-{2}") },
            { "sudoku.level", new Translation("Уровень {0}", "Level {0}", "Seviye {0}", "Level {0}") },
            { "sudoku.level_short", new Translation("Ур. {0}", "Level {0}", "Sv. {0}", "Level {0}") },
            { "sudoku.complete", new Translation("ПРОЙДЕНО", "CLEARED", "BİTTİ", "GESCHAFFT") },
            { "sudoku.incomplete", new Translation("НЕ ПРОЙДЕНО", "NOT CLEARED", "BİTMEDİ", "OFFEN") },
            { "sudoku.best_score", new Translation("Лучшее\n{0}", "Best\n{0}", "En iyi\n{0}", "Bestzeit\n{0}") },
            { "sudoku.seed", new Translation("{0}\nSeed #{1}", "{0}\nSeed #{1}", "{0}\nSeed #{1}", "{0}\nSeed #{1}") },
            { "sudoku.start", new Translation("Начать", "Start", "Başla", "Start") },
            { "sudoku.ad_card", new Translation("РЕКЛАМА\nбонусная вкладка\nмежду уровнями", "AD\nbonus tab\nbetween levels", "REKLAM\nseviyeler arası\nbonus sekmesi", "WERBUNG\nBonus-Reiter\nzwischen Leveln") },
            { "sudoku.rules", new Translation("Строка, колонка и блок 3x3: числа 1-9 без повторов.", "Row, column, and 3x3 box: numbers 1-9 without repeats.", "Satır, sütun ve 3x3 kutu: 1-9 sayıları tekrar etmez.", "Zeile, Spalte und 3x3-Block: Zahlen 1-9 ohne Wiederholung.") },
            { "sudoku.undo", new Translation("Undo", "Undo", "Geri Al", "Rueckg.") },
            { "sudoku.undo_stock", new Translation("Undo x{0}", "Undo x{0}", "Geri Al x{0}", "Rueckg. x{0}") },
            { "sudoku.undo_ad", new Translation("Undo AD", "Undo AD", "Geri Al REKLAM", "Rueckg. AD") },
            { "sudoku.undo_loading", new Translation("Реклама...", "Ad...", "Reklam...", "Werbung...") },
            { "sudoku.erase", new Translation("Стереть", "Erase", "Sil", "Loeschen") },
            { "sudoku.notes_on", new Translation("Заметки: вкл", "Notes: On", "Notlar: Açık", "Notizen: An") },
            { "sudoku.notes_off", new Translation("Заметки: выкл", "Notes: Off", "Notlar: Kapalı", "Notizen: Aus") },
            { "sudoku.hint", new Translation("Подсказка", "Hint", "İpucu", "Hinweis") },
            { "sudoku.hint_stock", new Translation("Подсказка x{0}", "Hint x{0}", "İpucu x{0}", "Hinweis x{0}") },
            { "sudoku.hint_ad", new Translation("Подсказка AD", "Hint AD", "İpucu REKLAM", "Hinweis AD") },
            { "sudoku.hint_loading", new Translation("Реклама...", "Ad...", "Reklam...", "Werbung...") },
            { "sudoku.status.start", new Translation("Рейтинг: {0} / {1}. Выберите клетку и введите число.", "Rating: {0} / {1}. Select a cell and enter a number.", "Puan: {0} / {1}. Bir hücre seç ve sayı gir.", "Wertung: {0} / {1}. Feld waehlen und Zahl eingeben.") },
            { "sudoku.status.leaderboard", new Translation("Лидерборд обновлён.", "Leaderboard refreshed.", "Lider tablosu yenilendi.", "Bestenliste aktualisiert.") },
            { "sudoku.status.ad", new Translation("Здесь будет рекламная вкладка между уровнями.", "A bonus ad tab between levels will appear here.", "Seviyeler arasında bonus reklam sekmesi burada olacak.", "Hier erscheint ein Bonus-Werbereiter zwischen Leveln.") },
            { "sudoku.status.conflict", new Translation("Конфликт: это число нарушает решение. Ошибки: {0}", "Conflict: this number breaks the solution. Mistakes: {0}", "Çakışma: bu sayı çözümü bozuyor. Hata: {0}", "Konflikt: Diese Zahl passt nicht. Fehler: {0}") },
            { "sudoku.status.good", new Translation("Хороший ход.", "Good move.", "İyi hamle.", "Guter Zug.") },
            { "sudoku.status.erased", new Translation("Клетка очищена.", "Cell erased.", "Hücre silindi.", "Feld geloescht.") },
            { "sudoku.status.undo", new Translation("Ход отменён.", "Move undone.", "Hamle geri alındı.", "Zug rueckgaengig.") },
            { "sudoku.status.undo_empty", new Translation("Пока нечего отменять.", "There is nothing to undo yet.", "Henüz geri alınacak hamle yok.", "Noch nichts zum Rueckgaengigmachen.") },
            { "sudoku.status.undo_ad_opening", new Translation("Открываем рекламу за 3 отмены...", "Opening an ad for 3 undo moves...", "3 geri alma için reklam açılıyor...", "Werbung fuer 3 Rueckgaengig-Schritte wird geoeffnet...") },
            { "sudoku.status.undo_ad_not_ready", new Translation("Реклама Undo пока не готова. Попробуйте чуть позже.", "Undo ad is not ready yet. Try again shortly.", "Geri alma reklamı henüz hazır değil. Biraz sonra tekrar dene.", "Undo-Werbung ist noch nicht bereit. Bitte gleich erneut versuchen.") },
            { "sudoku.status.undo_ad_not_completed", new Translation("Реклама не завершена, отмены не выданы.", "Ad was not completed, undo moves were not granted.", "Reklam tamamlanmadı, geri almalar verilmedi.", "Werbung nicht abgeschlossen, Rueckgaengig-Schritte nicht vergeben.") },
            { "sudoku.status.undo_ad_rewarded", new Translation("Получено 3 отмены. Одна уже использована.", "Received 3 undo moves. One was used now.", "3 geri alma alındı. Biri şimdi kullanıldı.", "3 Rueckgaengig-Schritte erhalten. Einer wurde direkt benutzt.") },
            { "sudoku.status.hint_selected", new Translation("Подсказка открыла выбранную клетку.", "Hint revealed the selected cell.", "İpucu seçili hücreyi açtı.", "Hinweis hat das gewaehlte Feld gezeigt.") },
            { "sudoku.status.hint_next", new Translation("Подсказка открыла следующую пустую клетку.", "Hint revealed the next empty cell.", "İpucu sıradaki boş hücreyi açtı.", "Hinweis hat das naechste leere Feld gezeigt.") },
            { "sudoku.status.hint_empty", new Translation("Подсказка сейчас не нужна: поле уже решено.", "No hint needed now: the board is solved.", "Şu an ipucu gerekmiyor: tahta çözüldü.", "Kein Hinweis noetig: Das Feld ist geloest.") },
            { "sudoku.status.hint_ad_opening", new Translation("Открываем рекламу за 3 подсказки...", "Opening an ad for 3 hints...", "3 ipucu için reklam açılıyor...", "Werbung fuer 3 Hinweise wird geoeffnet...") },
            { "sudoku.status.hint_ad_not_ready", new Translation("Реклама пока не готова. Попробуйте чуть позже.", "Ad is not ready yet. Try again shortly.", "Reklam henüz hazır değil. Biraz sonra tekrar dene.", "Werbung ist noch nicht bereit. Bitte gleich erneut versuchen.") },
            { "sudoku.status.hint_ad_not_completed", new Translation("Реклама не завершена, подсказки не выданы.", "Ad was not completed, hints were not granted.", "Reklam tamamlanmadı, ipuçları verilmedi.", "Werbung nicht abgeschlossen, Hinweise nicht vergeben.") },
            { "sudoku.status.hint_ad_rewarded", new Translation("Получено 3 подсказки. Одна уже использована.", "Received 3 hints. One was used now.", "3 ipucu alındı. Biri şimdi kullanıldı.", "3 Hinweise erhalten. Einer wurde direkt benutzt.") },
            { "sudoku.status.select_cell", new Translation("Сначала выберите клетку.", "Select a cell first.", "Önce bir hücre seç.", "Zuerst ein Feld waehlen.") },
            { "sudoku.status.locked", new Translation("Стартовые числа менять нельзя.", "Starting numbers cannot be changed.", "Başlangıç sayıları değiştirilemez.", "Startzahlen koennen nicht geaendert werden.") },
            { "sudoku.status.complete", new Translation("Готово! Уровень {0} пройден за {1}. Ошибки: {2}.", "Done! Level {0} cleared in {1}. Mistakes: {2}.", "Tamam! Seviye {0}, {1} içinde bitti. Hata: {2}.", "Fertig! Level {0} in {1} geschafft. Fehler: {2}.") },
            { "sudoku.summary", new Translation("{0}: {1}/{2}\nЛучшее суммарно: {3}", "{0}: {1}/{2}\nBest total: {3}", "{0}: {1}/{2}\nToplam en iyi: {3}", "{0}: {1}/{2}\nBeste Gesamtzeit: {3}") }
        };

        public static string Text(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            if (TryGetRuntimeTranslation(key, out string runtimeTranslation))
                return RegisterLocalizedValue(runtimeTranslation);

            if (!Translations.TryGetValue(key, out Translation translation))
                return key;

            GameLanguage language = ResolveLanguage();
            return RegisterLocalizedValue(translation.Get(language));
        }

        public static bool IsKnownLocalizedValue(string value)
        {
            return !string.IsNullOrEmpty(value) && KnownLocalizedValues.Contains(value);
        }

        private static bool TryGetRuntimeTranslation(string key, out string value)
        {
            GameLanguage language = ResolveLanguage();
            switch (key)
            {
                case "common.oz_ametist":
                    value = Pick(language, "Оз Аметист", "Oz Amethyst", "Oz Ametist");
                    return true;
                case "main.reward_bonus.menu":
                    value = Pick(language, "ПОДДЕРЖАТЬ SYMBIOSIS", "SUPPORT SYMBIOSIS", "SYMBIOSIS'E DESTEK", "SYMBIOSIS UNTERSTÜTZEN");
                    return true;
                case "main.reward_bonus.title":
                    value = Pick(language, "РЕКЛАМНЫЙ БОНУС", "AD BONUS", "REKLAM BONUSU", "WERBEBONUS");
                    return true;
                case "main.reward_bonus.creator_title":
                    value = Pick(language, "СЛОВО СОЗДАТЕЛЕЙ", "A WORD FROM THE CREATORS", "YARATICILARDAN BİR MESAJ", "EIN WORT DER SCHÖPFER");
                    return true;
                case "main.reward_bonus.black_yang":
                    value = "BLACK YANG";
                    return true;
                case "main.reward_bonus.white_yin":
                    value = "WHITE YIN";
                    return true;
                case "main.reward_bonus.black_line":
                    value = Pick(language,
                        "Мы развиваем Symbiosis независимо и последовательно. Не обещаем невозможного — мы просто продолжаем строить этот мир.",
                        "We are developing Symbiosis independently and steadily. We do not promise the impossible — we simply keep building this world.",
                        "Symbiosis'i bağımsız ve istikrarlı bir şekilde geliştiriyoruz. İmkânsızı vadetmiyoruz — yalnızca bu dünyayı inşa etmeye devam ediyoruz.",
                        "Wir entwickeln Symbiosis unabhängig und konsequent weiter. Wir versprechen nichts Unmögliches — wir bauen diese Welt einfach weiter.");
                    return true;
                case "main.reward_bonus.white_line":
                    value = Pick(language,
                        "Если тебе близко то, что мы создаём, оставайся с нами и говори, что можно сделать лучше. Мы слышим тебя.",
                        "If what we are creating resonates with you, stay with us and tell us what we can improve. We are listening.",
                        "Yarattığımız şey sana yakın geliyorsa bizimle kal ve neleri daha iyi yapabileceğimizi söyle. Seni dinliyoruz.",
                        "Wenn dir gefällt, was wir erschaffen, bleib bei uns und sag uns, was wir besser machen können. Wir hören dir zu.");
                    return true;
                case "main.reward_bonus.offer_title":
                    value = Pick(language, "ДОПОЛНИТЕЛЬНЫЙ БОНУС", "OPTIONAL BONUS", "İSTEĞE BAĞLI BONUS", "OPTIONALER BONUS");
                    return true;
                case "main.reward_bonus.body":
                    value = Pick(language, "Посмотри короткое рекламное видео и получи +{0} {1}.", "Watch a short ad video and receive +{0} {1}.", "Kısa bir reklam videosu izle ve +{0} {1} kazan.", "Sieh dir ein kurzes Werbevideo an und erhalte +{0} {1}.");
                    return true;
                case "main.reward_bonus.reward":
                    value = Pick(language, "+{0} {1}", "+{0} {1}", "+{0} {1}", "+{0} {1}");
                    return true;
                case "main.reward_bonus.watch":
                    value = Pick(language, "Смотреть рекламу  •  +{0} {1}", "Watch ad  •  +{0} {1}", "Reklam izle  •  +{0} {1}", "Werbung ansehen  •  +{0} {1}");
                    return true;
                case "main.reward_bonus.not_now":
                    value = Pick(language, "Не сейчас", "Not now", "Şimdi değil", "Nicht jetzt");
                    return true;
                case "main.reward_bonus.opening":
                    value = Pick(language, "Открываем рекламу...", "Opening ad...", "Reklam açılıyor...", "Werbung wird geöffnet...");
                    return true;
                case "main.reward_bonus.received":
                    value = Pick(language, "+1 Оз Аметист получен.", "+1 Oz Amethyst received.", "+1 Oz Ametist alındı.", "+1 Oz-Amethyst erhalten.");
                    return true;
                case "main.reward_bonus.not_completed":
                    value = Pick(language, "Реклама не завершена — бонус не выдан.", "Ad was not completed — no bonus was granted.", "Reklam tamamlanmadı — bonus verilmedi.", "Werbung nicht abgeschlossen — kein Bonus vergeben.");
                    return true;
                case "main.reward_bonus.limit":
                    value = Pick(language, "Сегодня доступны только 3 рекламных бонуса. Возвращайтесь завтра.", "Only 3 ad bonuses are available per day. Come back tomorrow.", "Günde yalnızca 3 reklam bonusu kullanılabilir. Yarın tekrar gel.", "Pro Tag sind nur 3 Werbeboni verfügbar. Komm morgen wieder.");
                    return true;
                case "main.reward_bonus.profile_unavailable":
                    value = Pick(language, "Профиль ещё загружается. Попробуйте через несколько секунд.", "Your profile is still loading. Try again in a few seconds.", "Profilin hâlâ yükleniyor. Birkaç saniye sonra tekrar dene.", "Dein Profil wird noch geladen. Versuche es in einigen Sekunden erneut.");
                    return true;
                case "main.reward_bonus.remaining":
                    value = Pick(language, "Сегодня доступно: {0}/{1}", "Available today: {0}/{1}", "Bugün kullanılabilir: {0}/{1}", "Heute verfügbar: {0}/{1}");
                    return true;
                case "menu.profile":
                    value = Pick(language, "ПРОФИЛЬ", "PROFILE", "PROFIL");
                    return true;
                case "menu.shop":
                    value = Pick(language, "МАГАЗИН", "SHOP", "MAĞAZA");
                    return true;
                case "menu.shop_short":
                    value = Pick(language, "МАЗ", "SHOP", "MAĞAZA");
                    return true;
                case "menu.online":
                    value = Pick(language, "Онлайн", "Online", "Cevrimici");
                    return true;
                case "menu.offline":
                    value = Pick(language, "Офлайн", "Offline", "Cevrimdisi");
                    return true;
                case "profile.dynasty_empty":
                    value = Pick(language, "Династия: -", "Dynasty: -", "Hanedan: -");
                    return true;
                case "profile.dynasty":
                    value = Pick(language, "Династия: {0}", "Dynasty: {0}", "Hanedan: {0}");
                    return true;
                case "profile.slot":
                    value = Pick(language, "Слот {0}", "Slot {0}", "Yuva {0}");
                    return true;
                case "profile.age_gender":
                    value = Pick(language, "Возраст: {0}  Пол: {1}", "Age: {0}  Gender: {1}", "Yas: {0}  Cinsiyet: {1}");
                    return true;
                case "profile.gender.male":
                    value = Pick(language, "Мужчина", "Male", "Erkek");
                    return true;
                case "profile.gender.female":
                    value = Pick(language, "Женщина", "Female", "Kadin");
                    return true;
                case "profile.gender.other":
                    value = Pick(language, "Другое", "Other", "Diger");
                    return true;
                case "profile.privacy.public":
                    value = Pick(language, "Профиль открыт", "Profile Open", "Profil Açık");
                    return true;
                case "profile.privacy.private":
                    value = Pick(language, "Профиль закрыт", "Profile Closed", "Profil Kapali");
                    return true;
                case "profile.privacy.closed_card":
                    value = Pick(language, "Профиль закрыт.", "This profile is closed.", "Bu profil kapali.");
                    return true;
                case "profile.chat_card":
                    value = Pick(language, "ID: {0}\nАльянс: {1}", "ID: {0}\nAlliance: {1}", "ID: {0}\nİttifak: {1}");
                    return true;
                case "friends.nickname":
                    value = Pick(language, "Никнейм", "Nickname", "Takma ad", "Spielername");
                    return true;
                case "friends.add":
                    value = Pick(language, "Добавить", "Add", "Ekle", "Hinzufügen");
                    return true;
                case "friends.refresh":
                    value = Pick(language, "Обновить", "Refresh", "Yenile", "Aktualisieren");
                    return true;
                case "mail.title":
                    value = Pick(language, "Почта", "Mail", "Posta", "Post");
                    return true;
                case "mail.inbox":
                    value = Pick(language, "Входящие", "Inbox", "Gelen", "Eingang");
                    return true;
                case "mail.sent":
                    value = Pick(language, "Мои письма", "My Letters", "Mektuplarim", "Meine Briefe");
                    return true;
                case "mail.claim":
                    value = Pick(language, "Забрать", "Claim", "Al", "Abholen");
                    return true;
                case "mail.send":
                    value = Pick(language, "Отправить", "Send", "Gonder", "Senden");
                    return true;
                case "mail.no_subject":
                    value = Pick(language, "Без темы", "No subject", "Konu yok", "Kein Betreff");
                    return true;
                case "mail.subject_placeholder":
                    value = Pick(language, "Тема письма", "Subject", "Konu", "Betreff");
                    return true;
                case "mail.body_placeholder":
                    value = Pick(language, "Напишите сообщение разработчикам", "Write a message to the developers", "Gelistiricilere mesaj yaz", "Nachricht an die Entwickler");
                    return true;
                case "mail.inbox_empty":
                    value = Pick(language, "Писем пока нет.", "No mail yet.", "Henuz posta yok.", "Noch keine Post.");
                    return true;
                case "mail.sent_empty":
                    value = Pick(language, "Ваши письма пока не сохранены.", "No saved letters yet.", "Kayitli mesaj yok.", "Noch keine gespeicherten Nachrichten.");
                    return true;
                case "mail.select_message":
                    value = Pick(language, "Выберите письмо.", "Select a message.", "Bir mesaj seç.", "Nachricht waehlen.");
                    return true;
                case "mail.from":
                    value = Pick(language, "От: {0}", "From: {0}", "Kimden: {0}", "Von: {0}");
                    return true;
                case "mail.attachments":
                    value = Pick(language, "Вложения", "Attachments", "Ekler", "Anhaenge");
                    return true;
                case "mail.attachment_stone":
                    value = Pick(language, "Камень", "Stone", "Taş", "Stein");
                    return true;
                case "mail.attachment_item":
                    value = Pick(language, "Предмет", "Item", "Esya", "Gegenstand");
                    return true;
                case "mail.rare_stone":
                    value = Pick(language, "Редкий камень", "Rare Stone", "Nadir Taş", "Seltener Stein");
                    return true;
                case "mail.epic_stone":
                    value = Pick(language, "Эпический камень", "Epic Stone", "Epik Taş", "Epischer Stein");
                    return true;
                case "mail.legendary_stone":
                    value = Pick(language, "Легендарный камень", "Legendary Stone", "Efsanevi Taş", "Legendärer Stein");
                    return true;
                case "mail.mythic_stone":
                    value = Pick(language, "Мифический камень", "Mythic Stone", "Mitik Taş", "Mythischer Stein");
                    return true;
                case "mail.deneme_gift_subject":
                    value = Pick(language, "Подарок: редкий камень", "Gift: Rare Stone", "Hediye: Nadir Taş", "Geschenk: Seltener Stein");
                    return true;
                case "mail.deneme_gift_body":
                    value = Pick(language, "Мы отправили вам редкий камень для боевого набора. Откройте письмо и заберите вложение.", "We sent you a rare stone for the battle set. Open this letter and claim the attachment.", "Savaş seti icin nadir bir taş gonderdik. Mektubu ac ve eki al.", "Wir haben dir einen seltenen Stein fuer das Kampfset geschickt. Oeffne den Brief und hole den Anhang ab.");
                    return true;
                case "mail.epic_gift_subject":
                    value = Pick(language, "Подарок: эпический камень", "Gift: Epic Stone", "Hediye: Epik Taş", "Geschenk: Epischer Stein");
                    return true;
                case "mail.epic_gift_body":
                    value = Pick(language, "Мы отправили вам эпический камень для боевого набора. Заберите вложение — предмет появится в хранилище.", "We sent you an epic stone for the battle set. Claim the attachment and it will appear in storage.", "Savaş seti için epik bir taş gönderdik. Eki alın; eşya depoda görünecek.", "Wir haben dir einen epischen Stein für das Kampfset geschickt. Hole den Anhang ab; der Gegenstand erscheint im Lager.");
                    return true;
                case "mail.epic_bonus_subject":
                    value = Pick(language, "Ещё один подарок", "Another Gift", "Bir Hediye Daha", "Noch ein Geschenk");
                    return true;
                case "mail.epic_bonus_body":
                    value = Pick(language, "Для вашей коллекции подготовлен ещё один эпический камень. Заберите его из вложения.", "Another epic stone is ready for your collection. Claim it from the attachment.", "Koleksiyonunuz için bir epik taş daha hazır. Eki alarak taşı koleksiyonunuza ekleyin.", "Ein weiterer epischer Stein wartet auf deine Sammlung. Hole ihn aus dem Anhang ab.");
                    return true;
                case "mail.ready_to_claim":
                    value = Pick(language, "Подарок готов к получению.", "Gift is ready to claim.", "Hediye alinmaya hazır.", "Geschenk ist bereit.");
                    return true;
                case "mail.already_claimed":
                    value = Pick(language, "Подарок уже получен.", "Gift already claimed.", "Hediye alindi.", "Geschenk bereits abgeholt.");
                    return true;
                case "mail.claimed":
                    value = Pick(language, "Подарок получен.", "Gift claimed.", "Hediye alindi.", "Geschenk abgeholt.");
                    return true;
                case "mail.claim_none":
                    value = Pick(language, "Нет доступных подарков.", "No claimable gifts.", "Alınacak hediye yok.", "Keine Geschenke verfuegbar.");
                    return true;
                case "mail.not_found":
                    value = Pick(language, "Письмо не найдено.", "Message not found.", "Mesaj bulunamadi.", "Nachricht nicht gefunden.");
                    return true;
                case "mail.write_body_required":
                    value = Pick(language, "Напишите текст письма.", "Write a message body.", "Mesaj metni yaz.", "Nachrichtentext schreiben.");
                    return true;
                case "mail.sent_local":
                    value = Pick(language, "Письмо сохранено. После подключения сервера оно будет уходить разработчикам.", "Letter saved. After server hookup it will be sent to developers.", "Mesaj kaydedildi. Sunucu baglaninca gelistiricilere gider.", "Nachricht gespeichert. Nach Server-Anbindung geht sie an die Entwickler.");
                    return true;
                case "mail.badge_gifts":
                    value = Pick(language, "подарки {0}", "gifts {0}", "hediye {0}", "Geschenke {0}");
                    return true;
                case "mail.welcome_subject":
                    value = Pick(language, "Добро пожаловать в почту", "Welcome to Mail", "Postaya hoş geldin", "Willkommen in der Post");
                    return true;
                case "mail.welcome_body":
                    value = Pick(language, "Здесь будут приходить новости от команды Symbiosis, подарки, бонусы и важные сообщения. Заглядывайте во входящие, чтобы ничего не пропустить.", "News from the Symbiosis team, gifts, bonuses, and important messages will arrive here. Check your inbox so you do not miss anything.", "Symbiosis ekibinden haberler, hediyeler, bonuslar ve önemli mesajlar buraya gelecek. Hiçbir şeyi kaçırmamak için gelen kutunuzu kontrol edin.", "Hier findest du Neuigkeiten vom Symbiosis-Team, Geschenke, Boni und wichtige Nachrichten. Schau regelmäßig in deinen Eingang.");
                    return true;
                case "friends.accept":
                    value = Pick(language, "Принять", "Accept", "Kabul", "Annehmen");
                    return true;
                case "friends.decline":
                    value = Pick(language, "Отклонить", "Decline", "Reddet", "Ablehnen");
                    return true;
                case "alliance.title":
                    value = Pick(language, "Альянс", "Alliance", "İttifak");
                    return true;
                case "alliance.info":
                    value = Pick(language, "Инфо", "Info", "Bilgi");
                    return true;
                case "alliance.members":
                    value = Pick(language, "Участники", "Members", "Üyeler");
                    return true;
                case "alliance.chat":
                    value = Pick(language, "Чат", "Chat", "Sohbet");
                    return true;
                case "alliance.rewards":
                    value = Pick(language, "Награды", "Rewards", "Ödüller");
                    return true;
                case "alliance.treasury":
                    value = Pick(language, "Казна", "Treasury", "Hazine");
                    return true;
                case "alliance.tournaments":
                    value = Pick(language, "Турниры", "Tournaments", "Turnuvalar", "Turniere");
                    return true;
                case "alliance.events":
                    value = Pick(language, "События", "Events", "Etkinlikler");
                    return true;
                case "alliance.manage":
                    value = Pick(language, "Управление", "Manage", "Yönet");
                    return true;
                case "alliance.leaderboard":
                    value = Pick(language, "Рейтинг", "Leaderboard", "Liderlik");
                    return true;
                case "alliance.treasury_hint":
                    value = Pick(language, "Баланс и экономика клана.", "Clan balance and economy.", "Klan bakiyesi ve ekonomisi.");
                    return true;
                case "alliance.tournaments_hint":
                    value = Pick(language, "Чемпион, фонд и правила турниров клана.", "Clan champion, fund and tournament rules.", "Klan sampiyonu, fonu ve turnuva kurallari.", "Clan-Champion, Fonds und Turnierregeln.");
                    return true;
                case "alliance.clan_balance":
                    value = Pick(language, "Баланс клана", "Clan Balance", "Klan Bakiyesi");
                    return true;
                case "alliance.lifetime_points":
                    value = Pick(language, "Очки за всё время", "Lifetime Points", "Toplam Puan");
                    return true;
                case "alliance.selected_member":
                    value = Pick(language, "Выбран игрок", "Selected Member", "Seçilen Uye");
                    return true;
                case "alliance.kick_member":
                    value = Pick(language, "Исключить", "Kick", "Cikar");
                    return true;
                case "alliance.promote_member":
                    value = Pick(language, "Повысить", "Promote", "Yukselt");
                    return true;
                case "alliance.demote_member":
                    value = Pick(language, "Понизить", "Demote", "Dusur");
                    return true;
                case "alliance.make_champion":
                    value = Pick(language, "Сделать чемпионом", "Make Champion", "Sampiyon Yap");
                    return true;
                case "alliance.transfer_leadership":
                    value = Pick(language, "Передать клан", "Transfer Clan", "Devretme");
                    return true;
                case "alliance.donations":
                    value = Pick(language, "Пожертвования", "Donations", "Bagislar");
                    return true;
                case "alliance.donate":
                    value = Pick(language, "Пожертвовать", "Donate", "Bagis");
                    return true;
                case "alliance.donation_amount":
                    value = Pick(language, "Сумма", "Amount", "Miktar");
                    return true;
                case "alliance.donation_sent":
                    value = Pick(language, "Пожертвование отправлено", "Donation sent", "Bagis gonderildi");
                    return true;
                case "alliance.not_enough_currency":
                    value = Pick(language, "Недостаточно валюты", "Not enough currency", "Yeterli para yok");
                    return true;
                case "alliance.add_test_bots":
                    value = Pick(language, "Добавить ботов", "Add Bots", "Bot Ekle");
                    return true;
                case "alliance.test_bots_added":
                    value = Pick(language, "Тестовые боты добавлены", "Test bots added", "Test botlari eklendi");
                    return true;
                case "alliance.name":
                    value = Pick(language, "Название", "Name", "Ad");
                    return true;
                case "alliance.tag":
                    value = Pick(language, "Тег", "Tag", "Etiket");
                    return true;
                case "alliance.create":
                    value = Pick(language, "Создать", "Create", "Oluştur");
                    return true;
                case "alliance.search":
                    value = Pick(language, "Поиск", "Search", "Ara");
                    return true;
                case "alliance.invite":
                    value = Pick(language, "Пригласить", "Invite", "Davet");
                    return true;
                case "alliance.join":
                    value = Pick(language, "Вступить", "Join", "Katıl");
                    return true;
                case "alliance.accept_first":
                    value = Pick(language, "Принять 1-ю", "Accept First", "İlkini Kabul");
                    return true;
                case "alliance.claim_chest":
                    value = Pick(language, "Сундук", "Claim Chest", "Sandık Al");
                    return true;
                case "alliance.select_champion":
                    value = Pick(language, "Чемпион", "Champion", "Şampiyon", "Champion");
                    return true;
                case "alliance.update":
                    value = Pick(language, "Обновить", "Update", "Güncelle");
                    return true;
                case "alliance.level":
                    value = Pick(language, "Уровень альянса", "Alliance Level", "İttifak Seviyesi");
                    return true;
                case "alliance.level_short":
                    value = Pick(language, "Ур", "Lv", "Sv");
                    return true;
                case "alliance.weekly_focus":
                    value = Pick(language, "Цель недели", "Weekly Focus", "Haftalık Hedef");
                    return true;
                case "alliance.weekly":
                    value = Pick(language, "Неделя", "Weekly", "Haftalık");
                    return true;
                case "alliance.points_short":
                    value = Pick(language, "оч.", "pts", "puan");
                    return true;
                case "alliance.rank":
                    value = Pick(language, "Ранг", "Rank", "Rutbe");
                    return true;
                case "alliance.role":
                    value = Pick(language, "Роль", "Role", "Rol");
                    return true;
                case "alliance.role.leader":
                    value = Pick(language, "Лидер", "Leader", "Lider");
                    return true;
                case "alliance.role.officer":
                    value = Pick(language, "Офицер", "Officer", "Yetkili");
                    return true;
                case "alliance.role.member":
                    value = Pick(language, "Участник", "Member", "Üye");
                    return true;
                case "alliance.visibility.open":
                    value = Pick(language, "Открыт", "Open", "Açık");
                    return true;
                case "alliance.visibility.invite_only":
                    value = Pick(language, "По заявке", "Invite Only", "Davetle");
                    return true;
                case "alliance.visibility.closed":
                    value = Pick(language, "Закрыт", "Closed", "Kapalı");
                    return true;
                case "alliance.focus.any":
                    value = Pick(language, "Любая", "Any", "Herhangi");
                    return true;
                case "alliance.focus.ranked":
                    value = Pick(language, "Ранг", "Ranked", "Rank");
                    return true;
                case "alliance.focus.duel":
                    value = Pick(language, "Дуэль", "Duel", "Düello");
                    return true;
                case "alliance.focus.daily":
                    value = Pick(language, "Ежедневно", "Daily", "Günlük");
                    return true;
                case "alliance.focus.random":
                    value = Pick(language, "Случайный", "Random", "Rastgele");
                    return true;
                case "alliance.chest":
                    value = Pick(language, "Сундук", "Chest", "Sandık");
                    return true;
                case "alliance.next_chest":
                    value = Pick(language, "След. сундук", "Next Chest", "Sonraki Sandik");
                    return true;
                case "alliance.my_contribution":
                    value = Pick(language, "Мой вклад", "My Contribution", "Benim Katkim");
                    return true;
                case "alliance.need_more":
                    value = Pick(language, "Нужно ещё", "Need More", "Daha Gerek");
                    return true;
                case "alliance.ready":
                    value = Pick(language, "Готово", "Ready", "Hazır");
                    return true;
                case "alliance.claim_status":
                    value = Pick(language, "Статус", "Status", "Durum");
                    return true;
                case "alliance.claimed":
                    value = Pick(language, "Забрано", "Claimed", "Alındı");
                    return true;
                case "alliance.not_ready":
                    value = Pick(language, "Не готово", "Not Ready", "Hazır Degil");
                    return true;
                case "alliance.max_tier":
                    value = Pick(language, "Макс.", "Max", "Maks.");
                    return true;
                case "alliance.objectives":
                    value = Pick(language, "Цели", "Objectives", "Hedefler");
                    return true;
                case "alliance.activity":
                    value = Pick(language, "Активность", "Activity", "Aktivite");
                    return true;
                case "alliance.recent_activity":
                    value = Pick(language, "Последние события", "Recent Activity", "Son Aktivite");
                    return true;
                case "alliance.top_contributors":
                    value = Pick(language, "Лучший вклад", "Top Contributors", "En Iyi Katki");
                    return true;
                case "alliance.recruitment":
                    value = Pick(language, "Набор", "Recruitment", "Uye Alimi");
                    return true;
                case "alliance.settings":
                    value = Pick(language, "Настройки", "Settings", "Ayarlar");
                    return true;
                case "alliance.leadership":
                    value = Pick(language, "Лидерство", "Leadership", "Liderlik");
                    return true;
                case "alliance.announcement":
                    value = Pick(language, "Объявление", "Announcement", "Duyuru");
                    return true;
                case "alliance.breakdown":
                    value = Pick(language, "Вклад по играм", "Game Contribution", "Oyun Katkısı");
                    return true;
                case "alliance.tournament_fund":
                    value = Pick(language, "Фонд альянса", "Alliance Fund", "İttifak Fonu", "Allianzfonds");
                    return true;
                case "alliance.champion":
                    value = Pick(language, "Чемпион недели", "Weekly Champion", "Haftalık Şampiyon", "Wochenchampion");
                    return true;
                case "alliance.role.champion":
                    value = Pick(language, "Чемпион", "Champion", "Şampiyon", "Champion");
                    return true;
                case "alliance.no_champion":
                    value = Pick(language, "не выбран", "not selected", "seçilmedi", "nicht gewählt");
                    return true;
                case "alliance.champion_split":
                    value = Pick(language, "доля фонд/игрок", "fund/player split", "fon/oyuncu payı", "Fonds/Spieler");
                    return true;
                case "alliance.tournament_unlock":
                    value = Pick(language, "Турниры альянса открываются с уровня", "Alliance tournaments unlock at level", "İttifak turnuvaları şu seviyede açılır", "Allianzturniere ab Level");
                    return true;
                case "alliance.no_alliance":
                    value = Pick(language, "Вы пока не в альянсе.", "You are not in an alliance yet.", "Henüz bir ittifakta değilsin.");
                    return true;
                case "alliance.invites":
                    value = Pick(language, "Приглашения", "Invites", "Davetler");
                    return true;
                case "alliance.search_results":
                    value = Pick(language, "Результаты поиска", "Search Results", "Arama Sonuçları");
                    return true;
                case "alliance.hint_select":
                    value = Pick(language, "Введите название и тег для создания или найдите альянс.", "Enter name and tag to create, or search alliances.", "Oluşturmak için ad ve etiket gir ya da ittifak ara.");
                    return true;
                case "alliance.empty_invites":
                    value = Pick(language, "Нет приглашений.", "No invites.", "Davet yok.");
                    return true;
                case "alliance.search_hint":
                    value = Pick(language, "Введите название или тег и нажмите поиск.", "Enter a name or tag and search.", "Ad veya etiket girip ara.");
                    return true;
                case "alliance.requests":
                    value = Pick(language, "Заявки", "Requests", "Başvurular");
                    return true;
                case "alliance.manage_hint":
                    value = Pick(language, "Управление заявками и ролями доступно лидеру и офицерам.", "Requests and role tools are available to leaders and officers.", "Başvuru ve rol araçları lider ve yetkililere açıktır.");
                    return true;
                case "alliance.no_permission":
                    value = Pick(language, "Недостаточно прав.", "Permission denied.", "Yetki yok.");
                    return true;
                case "alliance.error_profile":
                    value = Pick(language, "Для альянса нужен серверный профиль.", "Alliance requires server profile.", "İttifak için sunucu profili gerekir.");
                    return true;
                case "alliance.error_backend_unavailable":
                    value = Pick(language, "Сервер альянсов ещё не подключён.", "Alliance server is not connected yet.", "İttifak sunucusu henüz bağlı değil.");
                    return true;
                case "alliance.activity.created":
                    value = Pick(language, "Альянс создан", "Alliance created", "İttifak oluşturuldu");
                    return true;
                case "alliance.activity.joined":
                    value = Pick(language, "Игрок вступил", "Member joined", "Uye katildi");
                    return true;
                case "alliance.activity.left":
                    value = Pick(language, "Игрок вышел", "Member left", "Uye ayrildi");
                    return true;
                case "alliance.activity.kicked":
                    value = Pick(language, "Игрок исключён", "Member kicked", "Uye atildi");
                    return true;
                case "alliance.activity.promoted":
                    value = Pick(language, "Повышение", "Promoted", "Terfi");
                    return true;
                case "alliance.activity.demoted":
                    value = Pick(language, "Понижение", "Demoted", "Dusuruldu");
                    return true;
                case "alliance.activity.contribution":
                    value = Pick(language, "Вклад", "Contribution", "Katki");
                    return true;
                case "alliance.activity.fund_donation":
                    value = Pick(language, "Пожертвование", "Donation", "Bagis");
                    return true;
                case "alliance.activity.test_bots_added":
                    value = Pick(language, "Добавлены тестовые боты", "Test bots added", "Test botlari eklendi");
                    return true;
                case "alliance.activity.level_up":
                    value = Pick(language, "Новый уровень", "Level Up", "Seviye Atladi");
                    return true;
                case "alliance.activity.champion_selected":
                    value = Pick(language, "Выбран чемпион", "Champion selected", "Sampiyon seçildi");
                    return true;
                case "alliance.activity.request_accepted":
                    value = Pick(language, "Заявка принята", "Request accepted", "Basvuru kabul");
                    return true;
                case "alliance.activity.invite_accepted":
                    value = Pick(language, "Инвайт принят", "Invite accepted", "Davet kabul");
                    return true;
                case "alliance.activity.leadership_transferred":
                    value = Pick(language, "Лидерство передано", "Leadership transferred", "Liderlik devredildi");
                    return true;
                case "shop.title":
                    value = Pick(language, "МАГАЗИН", "SHOP", "MAĞAZA");
                    return true;
                case "shop.tab.mahjong":
                    value = Pick(language, "Маджонг", "Mahjong", "Mahjong");
                    return true;
                case "shop.tab.ametist":
                    value = Pick(language, "Аметист", "Amethyst", "Ametist");
                    return true;
                case "shop.tab.subscription":
                    value = Pick(language, "Подписка", "Subscription", "Abonelik");
                    return true;
                case "shop.placeholder.mahjong":
                    value = Pick(language, "Пакеты скоро появятся.", "Packs are coming soon.", "Paketler yakinda.");
                    return true;
                case "shop.placeholder.subscription":
                    value = Pick(language, "Подписка пока в разработке.", "Subscription is in development.", "Abonelik gelistiriliyor.");
                    return true;
                case "shop.no_ads_week_title":
                    value = Pick(language, "Неделя без рекламы", "One Week No Ads", "Bir Hafta Reklamsiz");
                    return true;
                case "shop.no_ads_week_body":
                    value = Pick(language, "Отключает рекламу после боя на 7 дней.", "Removes post-match ads for 7 days.", "Maç sonrasi reklamlari 7 gun kapatir.");
                    return true;
                case "shop.no_ads_week_badge":
                    value = Pick(language, "ПРЕМИУМ", "PREMIUM", "PREMIUM");
                    return true;
                case "shop.no_ads_week_price_caption":
                    value = Pick(language, "7 дней без рекламы", "7 days ad-free", "7 gun reklamsiz");
                    return true;
                case "shop.no_ads_week_feature_1":
                    value = Pick(language, "Нет рекламы после победы и поражения", "No ads after wins or defeats", "Kazanma ve kaybetme sonrasi reklam yok");
                    return true;
                case "shop.no_ads_week_feature_2":
                    value = Pick(language, "Подходит для ранговых боев и обычных матчей", "Works for ranked and regular matches", "Rankli ve normal maçlarda gecerli");
                    return true;
                case "shop.no_ads_week_feature_3":
                    value = Pick(language, "Автоматически закончится через неделю", "Ends automatically after one week", "Bir hafta sonra otomatik biter");
                    return true;
                case "shop.no_ads_week_active":
                    value = Pick(language, "Активно. Осталось дней: {0}", "Active. Days left: {0}", "Aktif. Kalan gun: {0}");
                    return true;
                case "shop.no_ads_week_buy":
                    value = Pick(language, "Купить на 7 дней\n{0}", "Buy 7 days\n{0}", "7 gun satin al\n{0}");
                    return true;
                case "shop.no_ads_week_cta":
                    value = Pick(language, "Отключить рекламу - {0}", "Remove ads - {0}", "Reklami kapat - {0}");
                    return true;
                case "shop.no_ads_week_active_cta":
                    value = Pick(language, "Уже активно", "Already Active", "Zaten Aktif");
                    return true;
                case "shop.no_ads_week_purchased":
                    value = Pick(language, "Реклама отключена на {0} дней.", "Ads removed for {0} days.", "Reklamlar {0} gun kapatildi.");
                    return true;
                case "shop.free":
                    value = Pick(language, "БЕСПЛАТНО", "FREE", "UCRETSIZ");
                    return true;
                case "shop.claimed":
                    value = Pick(language, "ПОЛУЧЕНО", "CLAIMED", "ALINDI");
                    return true;
                case "shop.ad":
                    value = Pick(language, "РЕКЛАМА", "AD", "REKLAM");
                    return true;
                case "shop.ad_ready":
                    value = Pick(language, "Реклама готова.", "Ad is ready.", "Reklam hazır.", "Werbung ist bereit.");
                    return true;
                case "shop.ad_initializing":
                    value = Pick(language, "Подключаем рекламу...", "Preparing ads...", "Reklam hazırlaniyor...", "Werbung wird vorbereitet...");
                    return true;
                case "shop.ad_consent_required":
                    value = Pick(language, "Нужно подтвердить настройки рекламы.", "Ad privacy choices are required.", "Reklam gizlilik seçimleri gerekli.", "Werbe-Datenschutzeinstellungen sind erforderlich.");
                    return true;
                case "shop.ad_loading":
                    value = Pick(language, "Загрузка рекламы...", "Loading ad...", "Reklam yukleniyor...", "Werbung wird geladen...");
                    return true;
                case "shop.ad_not_ready":
                    value = Pick(language, "Реклама пока недоступна.", "Ad is not ready yet.", "Reklam henuz hazır degil.", "Werbung ist noch nicht bereit.");
                    return true;
                case "shop.ad_no_fill":
                    value = Pick(language, "Сейчас нет доступной рекламы для вашего региона. Попробуйте чуть позже.", "No ad is available for your region right now. Try again shortly.", "Bolgeniz icin su anda reklam yok. Biraz sonra tekrar deneyin.", "Fur deine Region ist gerade keine Werbung verfugbar. Bitte versuche es gleich nochmal.");
                    return true;
                case "shop.ad_network_error":
                    value = Pick(language, "Реклама не загрузилась. Проверьте сеть, VPN, DNS или блокировщик рекламы.", "Ad did not load. Check network, VPN, DNS, or ad blocker.", "Reklam yuklenmedi. Agi, VPN'i, DNS'i veya reklam engelleyiciyi kontrol edin.", "Werbung konnte nicht geladen werden. Prufe Netzwerk, VPN, DNS oder Werbeblocker.");
                    return true;
                case "shop.ad_webview_update_required":
                    value = Pick(language, "Реклама не открылась. Обновите Android System WebView, Google Chrome и Google Play services, затем перезагрузите телефон. Награда выдается только после просмотра рекламы.", "Ad did not open. Update Android System WebView, Google Chrome, and Google Play services, then restart the phone. Rewards are granted only after an ad is watched.", "Reklam acilmadi. Android System WebView, Google Chrome ve Google Play services'i guncelleyip telefonu yeniden baslatin. Ödül yalnizca reklam izlendikten sonra verilir.", "Anzeige wurde nicht geoeffnet. Aktualisiere Android System WebView, Google Chrome und Google Play services und starte das Telefon neu. Belohnungen gibt es nur nach einer angesehenen Anzeige.");
                    return true;
                case "shop.ad_webview_title":
                    value = Pick(language, "Реклама не открылась", "Ad did not open", "Reklam acilmadi", "Anzeige wurde nicht geoeffnet");
                    return true;
                case "shop.ad_webview_webview":
                    value = Pick(language, "Обновить WebView", "Update WebView", "WebView guncelle", "WebView aktualisieren");
                    return true;
                case "shop.ad_webview_chrome":
                    value = Pick(language, "Обновить Chrome", "Update Chrome", "Chrome guncelle", "Chrome aktualisieren");
                    return true;
                case "shop.ad_webview_play_services":
                    value = Pick(language, "Обновить Play services", "Update Play services", "Play services guncelle", "Play services aktualisieren");
                    return true;
                case "shop.ad_webview_later":
                    value = Pick(language, "Позже", "Later", "Sonra", "Spaeter");
                    return true;
                case "shop.purchase_loading":
                    value = Pick(language, "Открываем покупку...", "Opening purchase...", "Satin alma aciliyor...");
                    return true;
                case "shop.purchase_not_ready":
                    value = Pick(language, "Покупки пока не подключены.", "Purchases are not connected yet.", "Satin almalar henuz baglanmadi.");
                    return true;
                case "shop.purchase_unknown":
                    value = Pick(language, "Пакет не найден.", "Package was not found.", "Paket bulunamadi.");
                    return true;
                case "shop.package_stub":
                    value = Pick(language, "Пакет пока заглушка: {0}", "Package placeholder: {0}", "Paket simdilik hazır degil: {0}");
                    return true;
                case "shop.free_claimed":
                    value = Pick(language, "Бесплатный бонус уже получен.", "Free bonus already claimed.", "Ücretsiz bonus zaten alindi.");
                    return true;
                case "shop.ad_limit":
                    value = Pick(language, "Лимит рекламы на сегодня исчерпан.", "Daily ad limit reached.", "Bugünkü reklam limiti doldu.");
                    return true;
                case "shop.balance_ametist":
                    value = Pick(language, "Аметист: {0}", "Amethyst: {0}", "Ametist: {0}");
                    return true;
                default:
                    value = string.Empty;
                    return false;
            }
        }

        private static string Pick(GameLanguage language, string russian, string english, string turkish, string german = null)
        {
            return language switch
            {
                GameLanguage.English => english,
                GameLanguage.Turkish => turkish,
                GameLanguage.German => german ?? english,
                _ => russian
            };
        }

        private static GameLanguage ResolveLanguage()
        {
            return AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Turkish;
        }

        public static string Format(string key, params object[] args)
        {
            string pattern = Text(key);
            return args == null || args.Length == 0 ? pattern : RegisterLocalizedValue(string.Format(pattern, args));
        }

        private static string RegisterLocalizedValue(string value)
        {
            if (!string.IsNullOrEmpty(value))
                KnownLocalizedValues.Add(value);

            return value;
        }

        private readonly struct Translation
        {
            private readonly string russian;
            private readonly string english;
            private readonly string turkish;
            private readonly string german;

            public Translation(string russian, string english, string turkish, string german = null)
            {
                this.russian = russian;
                this.english = english;
                this.turkish = turkish;
                this.german = german ?? english;
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
    }
}
