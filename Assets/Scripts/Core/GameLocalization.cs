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
            { "common.rank_unranked", new Translation("Ранг: без ранга", "Rank: Unranked", "Rutbe: Derecesiz") },
            { "common.unranked", new Translation("Без ранга", "Unranked", "Derecesiz") },
            { "common.oz_altin", new Translation("Оз Алтын", "Oz Gold", "Oz Altin") },

            { "intro.skip", new Translation("ПРОПУСТИТЬ", "SKIP", "ATLA") },

            { "language.title", new Translation("Выберите язык", "Choose Language", "Dil Sec") },
            { "language.subtitle", new Translation("Выберите язык перед созданием профиля.", "Select the language before creating your profile.", "Profil olusturmadan once dili secin.") },
            { "language.russian", new Translation("Русский", "Русский", "Русский") },
            { "language.english", new Translation("English", "English", "English") },
            { "language.turkish", new Translation("Türkçe", "Türkçe", "Türkçe") },

            { "settings.sound", new Translation("Звук", "Sound", "Ses") },
            { "settings.music", new Translation("Музыка", "Music", "Muzik") },
            { "settings.vibration", new Translation("Вибрация", "Vibration", "Titresim") },
            { "settings.language", new Translation("Язык", "Language", "Dil") },
            { "settings.language_ru", new Translation("Русский", "Русский", "Русский") },
            { "settings.language_en", new Translation("English", "English", "English") },
            { "settings.language_tr", new Translation("Türkçe", "Türkçe", "Türkçe") },
            { "settings.menu", new Translation("В меню", "Menu", "Menu") },
            { "settings.restart", new Translation("Заново", "Restart", "Yeniden") },
            { "settings.surrender", new Translation("Сдаться", "Surrender", "Teslim ol") },
            { "settings.close", new Translation("Закрыть", "Close", "Kapat") },
            { "settings.change_profile", new Translation("Сменить профиль", "Change Profile", "Profili Degistir") },
            { "settings.logout", new Translation("Выйти", "Logout", "Cikis") },

            { "profile.setup.title", new Translation("Создать профиль", "Create Profile", "Profil Olustur") },
            { "profile.setup.subtitle", new Translation("Выберите аватар и заполните данные профиля.", "Choose your avatar and fill in the profile details.", "Avatarini sec ve profil bilgilerini doldur.") },
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
            { "profile.setup.login", new Translation("Войти", "Login", "Giris") },
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

            { "battle.character.Tiger_Male.name", new Translation("Тигр", "Tiger", "Kaplan") },
            { "battle.character.Tiger_Female.name", new Translation("Тигрица", "Tigress", "Disi Kaplan") },
            { "battle.character.Fox_Male.name", new Translation("Лис", "Fox", "Tilki") },
            { "battle.character.Fox_Female.name", new Translation("Лисица", "Vixen", "Disi Tilki") },
            { "battle.character.Wolf_Male.name", new Translation("Волк", "Wolf", "Kurt") },
            { "battle.character.Wolf_Female.name", new Translation("Волчица", "She-Wolf", "Disi Kurt") },
            { "battle.character.Bear_Male.name", new Translation("Медведь", "Bear", "Ayi") },
            { "battle.character.Bear_Female.name", new Translation("Медведица", "She-Bear", "Disi Ayi") },
            { "battle.character.unlocked", new Translation("Открыт", "Unlocked", "Acik") },
            { "battle.character.free", new Translation("Бесплатно", "Free", "Ucretsiz") },
            { "battle.character.selected", new Translation("Выбран", "Selected", "Secildi") },
            { "battle.character.select", new Translation("Выбрать", "Select", "Sec") },
            { "battle.character.buy", new Translation("Купить", "Buy", "Satin al") },
            { "battle.character.not_enough_gold", new Translation("Недостаточно Оз Алтын", "Not enough Oz Gold", "Yeterli Oz Altin yok") },
            { "battle.character.need_gold", new Translation("Нужно: {0} {1}", "Need: {0} {1}", "Gerekli: {0} {1}") },
            { "battle.character.select_character", new Translation("Выбрать персонажа", "Select Character", "Karakter Sec") },
            { "battle.character.change_character", new Translation("Сменить персонажа", "Change Character", "Karakter Degistir") },

            { "mahjong.score", new Translation("Счет: {0}", "Score: {0}", "Skor: {0}") },
            { "mahjong.reward", new Translation("Награда: {0} {1}", "Reward: {0} {1}", "Odul: {0} {1}") },
            { "mahjong.story", new Translation("История", "Story", "Hikaye") },
            { "mahjong.battle", new Translation("Битва", "Battle", "Savas") },
            { "mahjong.level_select", new Translation("Выбор уровня", "Level Select", "Seviye Sec") },
            { "mahjong.reset_progress", new Translation("Сбросить прогресс", "Reset Progress", "Ilerlemeyi Sifirla") },
            { "mahjong.back", new Translation("Назад", "Back", "Geri") },
            { "mahjong.title.novice", new Translation("Новичок", "Novice", "Caylak") },

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

            { "update.title", new Translation("Доступно обновление", "Update available", "Guncelleme var") },
            { "update.body_older", new Translation("Установленная сборка старее серверной.", "Installed build is older than the server build.", "Yuklu surum sunucudaki surumden eski.") },
            { "update.latest_version", new Translation("Последняя версия: {0}", "Latest version: {0}", "Son surum: {0}") },
            { "update.required", new Translation("Это обновление обязательно.", "This update is required.", "Bu guncelleme zorunlu.") },
            { "update.button", new Translation("Обновить", "Update", "Guncelle") },
            { "update.later", new Translation("Позже", "Later", "Sonra") },

            { "chat.title", new Translation("Общий чат", "Global Chat", "Genel Sohbet") },
            { "chat.placeholder", new Translation("Сообщение", "Message", "Mesaj") },
            { "chat.send", new Translation("Отправить", "Send", "Gonder") },
            { "chat.empty", new Translation("Сообщений пока нет.", "No messages yet.", "Henuz mesaj yok.") },
            { "chat.error_empty", new Translation("Сообщение пустое.", "Message is empty.", "Mesaj bos.") },

            { "friends.title", new Translation("Друзья", "Friends", "Arkadaslar") },
            { "friends.empty_online", new Translation("Нет активных друзей.", "No active friends.", "Aktif arkadas yok.") },
            { "friends.empty_offline", new Translation("Нет друзей офлайн.", "No offline friends.", "Cevrimdisi arkadas yok.") },
            { "friends.request_sent", new Translation("Запрос отправлен.", "Request sent.", "Istek gonderildi.") },
            { "friends.error_profile", new Translation("Для друзей нужен серверный профиль.", "Friends require server profile.", "Arkadaslar icin sunucu profili gerekir.") },
            { "friends.error_request_failed", new Translation("Запрос друзей не удался.", "Friends request failed.", "Arkadas istegi basarisiz.") },
            { "friends.error_invalid_response", new Translation("Некорректный ответ друзей.", "Invalid friends response.", "Gecersiz arkadas yaniti.") }
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
