using System;
using System.Collections.Generic;

namespace MahjongGame
{
    public static class GameLocalization
    {
        private static readonly Dictionary<string, Translation> Translations = new(StringComparer.Ordinal)
        {
            { "common.player", new Translation("Игрок", "Player", "Oyuncu") },
            { "common.continue", new Translation("Продолжить", "Continue", "Devam") },
            { "common.title_empty", new Translation("Титул: -", "Title: -", "Unvan: -") },
            { "common.rank_unranked", new Translation("Ранг: без ранга", "Rank: Unranked", "Rütbe: Derecesiz") },
            { "common.unranked", new Translation("Без ранга", "Unranked", "Derecesiz") },
            { "common.oz_altin", new Translation("Оз Алтын", "Oz Gold", "Öz Altın") },

            { "settings.sound", new Translation("Звук", "Sound", "Ses") },
            { "settings.music", new Translation("Музыка", "Music", "Müzik") },
            { "settings.vibration", new Translation("Вибрация", "Vibration", "Titreşim") },
            { "settings.language", new Translation("Язык", "Language", "Dil") },
            { "settings.language_ru", new Translation("Русский", "Russian", "Rusça") },
            { "settings.language_en", new Translation("Английский", "English", "İngilizce") },
            { "settings.language_tr", new Translation("Турецкий", "Turkish", "Türkçe") },
            { "settings.menu", new Translation("В меню", "Menu", "Menü") },
            { "settings.restart", new Translation("Заново", "Restart", "Yeniden") },
            { "settings.close", new Translation("Закрыть", "Close", "Kapat") },

            { "profile.setup.title", new Translation("Создать профиль", "Create Profile", "Profil Oluştur") },
            { "profile.setup.name_placeholder", new Translation("Введите имя", "Enter your name", "İsmini gir") },
            { "profile.error.avatars_missing", new Translation("Аватары не настроены.", "Avatars are not configured.", "Avatarlar ayarlanmamış.") },
            { "profile.error.service_missing", new Translation("ProfileService не найден.", "ProfileService was not found.", "ProfileService bulunamadı.") },
            { "profile.error.bootstrap_missing", new Translation("Bootstrap не найден.", "Bootstrap was not found.", "Bootstrap bulunamadı.") },
            { "profile.error.enter_name", new Translation("Введите имя.", "Enter a name.", "Bir isim girin.") },
            { "profile.error.name_too_short", new Translation("Имя должно быть минимум {0} символа.", "Name must be at least {0} characters.", "İsim en az {0} karakter olmalı.") },
            { "profile.title", new Translation("Титул: {0}", "Title: {0}", "Unvan: {0}") },
            { "profile.rank", new Translation("Ранг: {0}", "Rank: {0}", "Rütbe: {0}") },
            { "profile.mahjong_title", new Translation("Маджонг: {0}", "Mahjong: {0}", "Mahjong: {0}") },
            { "profile.mahjong_rank", new Translation("Ранг маджонга: {0}", "Mahjong Rank: {0}", "Mahjong Rütbesi: {0}") },

            { "battle.character.Tiger_Male.name", new Translation("Тигр", "Tiger", "Kaplan") },
            { "battle.character.Tiger_Female.name", new Translation("Тигрица", "Tigress", "Dişi Kaplan") },
            { "battle.character.Fox_Male.name", new Translation("Лис", "Fox", "Tilki") },
            { "battle.character.Fox_Female.name", new Translation("Лисица", "Vixen", "Dişi Tilki") },
            { "battle.character.Wolf_Male.name", new Translation("Волк", "Wolf", "Kurt") },
            { "battle.character.Wolf_Female.name", new Translation("Волчица", "She-Wolf", "Dişi Kurt") },
            { "battle.character.Bear_Male.name", new Translation("Медведь", "Bear", "Ayı") },
            { "battle.character.Bear_Female.name", new Translation("Медведица", "She-Bear", "Dişi Ayı") },
            { "battle.character.unlocked", new Translation("Открыт", "Unlocked", "Açık") },
            { "battle.character.free", new Translation("Бесплатно", "Free", "Ücretsiz") },
            { "battle.character.selected", new Translation("Выбран", "Selected", "Seçildi") },
            { "battle.character.select", new Translation("Выбрать", "Select", "Seç") },
            { "battle.character.buy", new Translation("Купить", "Buy", "Satın al") },
            { "battle.character.not_enough_gold", new Translation("Недостаточно Оз Алтын", "Not enough Oz Gold", "Yeterli Öz Altın yok") },
            { "battle.character.need_gold", new Translation("Нужно: {0} {1}", "Need: {0} {1}", "Gerekli: {0} {1}") },
            { "battle.character.select_character", new Translation("Выбрать персонажа", "Select Character", "Karakter Seç") },
            { "battle.character.change_character", new Translation("Сменить персонажа", "Change Character", "Karakter Değiştir") },

            { "mahjong.score", new Translation("Счёт: {0}", "Score: {0}", "Skor: {0}") },
            { "mahjong.reward", new Translation("Награда: {0} {1}", "Reward: {0} {1}", "Ödül: {0} {1}") },
            { "mahjong.story", new Translation("История", "Story", "Hikaye") },
            { "mahjong.battle", new Translation("Битва", "Battle", "Savaş") },
            { "mahjong.level_select", new Translation("Выбор уровня", "Level Select", "Seviye Seç") },
            { "mahjong.reset_progress", new Translation("Сбросить прогресс", "Reset Progress", "İlerlemeyi Sıfırla") },
            { "mahjong.back", new Translation("Назад", "Back", "Geri") },
            { "mahjong.title.novice", new Translation("Новичок", "Novice", "Çaylak") },

            { "void.title", new Translation("AVOYDER", "AVOYDER", "AVOYDER") },
            { "void.start", new Translation("НАЧАТЬ", "START", "BAŞLA") },
            { "void.level_select", new Translation("ВЫБОР УРОВНЯ", "LEVEL SELECT", "SEVİYE SEÇ") },
            { "void.level_complete", new Translation("УРОВЕНЬ ПРОЙДЕН", "LEVEL COMPLETE", "SEVİYE TAMAMLANDI") },
            { "void.retry", new Translation("ЕЩЁ РАЗ", "RETRY", "TEKRAR") },
            { "void.next", new Translation("СЛЕДУЮЩИЙ", "NEXT", "SONRAKİ") },
            { "void.victory", new Translation("VOID ОЧИЩЕН", "VOID CLEARED", "VOID TEMİZLENDİ") },
            { "void.defeat", new Translation("ПОГЛОЩЁН VOID", "ABSORBED BY VOID", "VOID TARAFINDAN YUTULDU") },
            { "void.hp", new Translation("HP {0} / {1}", "HP {0} / {1}", "CP {0} / {1}") },
            { "void.level", new Translation("УРОВЕНЬ {0}", "LEVEL {0}", "SEVİYE {0}") },
            { "void.score", new Translation("СЧЁТ {0}", "SCORE {0}", "SKOR {0}") }
        };

        public static string Text(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            if (!Translations.TryGetValue(key, out Translation translation))
                return key;

            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
            return translation.Get(language);
        }

        public static string Format(string key, params object[] args)
        {
            string pattern = Text(key);
            return args == null || args.Length == 0 ? pattern : string.Format(pattern, args);
        }

        private readonly struct Translation
        {
            private readonly string russian;
            private readonly string english;
            private readonly string turkish;

            public Translation(string russian, string english, string turkish)
            {
                this.russian = russian;
                this.english = english;
                this.turkish = turkish;
            }

            public string Get(GameLanguage language)
            {
                return language switch
                {
                    GameLanguage.English => english,
                    GameLanguage.Turkish => turkish,
                    _ => russian
                };
            }
        }
    }
}
