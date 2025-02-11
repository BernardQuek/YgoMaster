using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using YgoMaster.Net;
using YgoMaster.Net.Message;

namespace YgoMaster
{
    partial class GameServer
    {
        // EDITED
        void UpgradeLoanerDeckFinish(DuelSettings duelSettings, PlayerCards playerCards)
        {
            PlayerCards playerCardsCopy = new PlayerCards();
            playerCardsCopy.FromDictionary(playerCards.ToDictionary());
            DeckInfo loanerDeck = duelSettings.Deck[DuelSettings.PlayerIndex];
            UpgradeDeckFinishImpl(loanerDeck.MainDeckCards, playerCardsCopy);
            UpgradeDeckFinishImpl(loanerDeck.ExtraDeckCards, playerCardsCopy);
            UpgradeDeckFinishImpl(loanerDeck.SideDeckCards, playerCardsCopy);
        }

        void UpgradeDeckFinishImpl(CardCollection deck, PlayerCards playerCards)
        {
            List<KeyValuePair<int, CardStyleRarity>> cards = new List<KeyValuePair<int, CardStyleRarity>>(deck.GetCollection());
            deck.Clear();
            foreach (KeyValuePair<int, CardStyleRarity> card in cards)
            {
                int cardId = card.Key;
                if (altArtIdByCardId.ContainsKey(cardId))
                {
                    int altArtCardId = altArtIdByCardId[cardId];
                    cardId = playerCards.Contains(altArtCardId) && UpgradeLoanerDeckToOwnedAltArts ? altArtCardId : cardId;
                }
                CardStyleRarity style = card.Value;
                if (playerCards.GetCount(cardId, PlayerCardKind.All, CardStyleRarity.Royal) > 0)
                {
                    if (style < CardStyleRarity.Royal)
                    {
                        style = CardStyleRarity.Royal;
                        PlayerCardKind kind = playerCards.GetCount(cardId, PlayerCardKind.Dismantle, CardStyleRarity.Royal) > 0 ? PlayerCardKind.Dismantle : PlayerCardKind.NoDismantle;
                        playerCards.Subtract(cardId, 1, kind, CardStyleRarity.Royal);
                    }
                }
                else if (playerCards.GetCount(cardId, PlayerCardKind.All, CardStyleRarity.Shine) > 0)
                {
                    if (style < CardStyleRarity.Shine)
                    {
                        style = CardStyleRarity.Shine;
                        PlayerCardKind kind = playerCards.GetCount(cardId, PlayerCardKind.Dismantle, CardStyleRarity.Shine) > 0 ? PlayerCardKind.Dismantle : PlayerCardKind.NoDismantle;
                        playerCards.Subtract(cardId, 1, kind, CardStyleRarity.Shine);
                    }
                }
                deck.Add(cardId, style);
            }
        }

        void UpgradeCpuDeckFinish(DuelSettings duelSettings, PlayerCards playerCards, int chapterId)
        {
            PlayerCards playerCardsCopy = new PlayerCards();
            playerCardsCopy.FromDictionary(playerCards.ToDictionary());
            CardStyleRarity minStyle = CardStyleRarity.Normal;
            double minStyleRoyalRate = GetUpgradeCpuDeckMinStyleRoyalRate(chapterId);
            double minStyleShineRate = GetUpgradeCpuDeckMinStyleShineRate(chapterId);
            double rng = rand.NextDouble();
            if (rng < minStyleRoyalRate + minStyleShineRate)
            {
                minStyle = rng < minStyleRoyalRate ? CardStyleRarity.Royal : CardStyleRarity.Shine;
            }
            DeckInfo cpuDeck = duelSettings.Deck[DuelSettings.CpuIndex];
            UpgradeCpuDeckFinishImpl(cpuDeck.MainDeckCards, playerCardsCopy, chapterId, minStyle);
            UpgradeCpuDeckFinishImpl(cpuDeck.ExtraDeckCards, playerCardsCopy, chapterId, minStyle);
            UpgradeCpuDeckFinishImpl(cpuDeck.SideDeckCards, playerCardsCopy, chapterId, minStyle);
        }

        void UpgradeCpuDeckFinishImpl(CardCollection deck, PlayerCards playerCards, int chapterId, CardStyleRarity minStyle)
        {
            List<KeyValuePair<int, CardStyleRarity>> cards = new List<KeyValuePair<int, CardStyleRarity>>(deck.GetCollection());
            deck.Clear();
            double altArtRate = GetUpgradeCpuDeckAltArtRate(chapterId);
            foreach (KeyValuePair<int, CardStyleRarity> card in cards)
            {
                int cardId = card.Key;
                if (altArtIdByCardId.ContainsKey(cardId))
                {
                    int altArtCardId = altArtIdByCardId[cardId];
                    altArtRate = playerCards.Contains(altArtCardId) ? Math.Max(0.5, altArtRate) : altArtRate;
                    cardId = rand.NextDouble() < altArtRate ? altArtCardId : cardId;
                }
                CardStyleRarity style = card.Value;
                style = style < minStyle ? minStyle : style;
                int royalCount = playerCards.GetCount(cardId, PlayerCardKind.All, CardStyleRarity.Royal);
                int shineCount = playerCards.GetCount(cardId, PlayerCardKind.All, CardStyleRarity.Shine);
                double royalRate = GetUpgradeCpuDeckCardRoyalRate(chapterId, cardId);
                double shineRate = GetUpgradeCpuDeckCardShineRate(chapterId, cardId);
                double rng = rand.NextDouble();
                if (rng < royalRate)
                {
                    style = CardStyleRarity.Royal;
                }
                else if (rng < royalRate + shineRate && style < CardStyleRarity.Shine)
                {
                    style = CardStyleRarity.Shine;
                }
                if (rand.NextDouble() < 0.8)
                {
                    if (royalCount > 0)
                    {
                        if (style < CardStyleRarity.Royal)
                        {
                            style = CardStyleRarity.Royal;
                            PlayerCardKind kind = playerCards.GetCount(cardId, PlayerCardKind.Dismantle, CardStyleRarity.Royal) > 0 ? PlayerCardKind.Dismantle : PlayerCardKind.NoDismantle;
                            playerCards.Subtract(cardId, 1, kind, CardStyleRarity.Royal);
                        }
                    }
                    else if  (shineCount > 0)
                    {
                        if (style < CardStyleRarity.Shine)
                        {
                            style = CardStyleRarity.Shine;
                            PlayerCardKind kind = playerCards.GetCount(cardId, PlayerCardKind.Dismantle, CardStyleRarity.Shine) > 0 ? PlayerCardKind.Dismantle : PlayerCardKind.NoDismantle;
                            playerCards.Subtract(cardId, 1, kind, CardStyleRarity.Shine);
                        }
                    }
                }
                deck.Add(cardId, style);
            }
        }

        double GetUpgradeCpuDeckMinStyleShineRate(int chapterId)
        {
            return IsMysteryDuelChapter(chapterId) ? 0.00949 : 0.00297;
        }

        double GetUpgradeCpuDeckMinStyleRoyalRate(int chapterId)
        {
            return IsMysteryDuelChapter(chapterId) ? 0.00316 : 0.00099;
        }

        double GetUpgradeCpuDeckCardShineRate(int chapterId, int cardId)
        {
            bool isMysteryDuel = IsMysteryDuelChapter(chapterId);
            switch ((CardRarity)CardRare[cardId])
            {
                case CardRarity.Normal:
                    return isMysteryDuel ? 0.092 : 0.072;
                case CardRarity.Rare:
                    return isMysteryDuel ? 0.1765 : 0.108;
                case CardRarity.SuperRare:
                    return isMysteryDuel ? 0.2838 : 0.144; 
                case CardRarity.UltraRare:
                    return isMysteryDuel ? 0.3429 : 0.18;
                default:
                    return 0;
            }
        }

        double GetUpgradeCpuDeckCardRoyalRate(int chapterId, int cardId)
        {
            bool isMysteryDuel = IsMysteryDuelChapter(chapterId);
            switch ((CardRarity)CardRare[cardId])
            {
                case CardRarity.Normal:
                    return isMysteryDuel ? 0.034 : 0.008;  
                case CardRarity.Rare:
                    return isMysteryDuel ? 0.0735 : 0.012;
                case CardRarity.SuperRare:
                    return isMysteryDuel ? 0.1486 : 0.016; 
                case CardRarity.UltraRare:
                    return isMysteryDuel ? 0.2286 : 0.02;
                default:
                    return 0;
            }
        }

        double GetUpgradeCpuDeckAltArtRate(int chapterId)
        {
            return IsMysteryDuelChapter(chapterId) ? 0.2 : 0.1;
        }
        // END EDITED
        DuelSettings GetSoloDuelSettings(Player player, int chapterId)
        {
            DuelSettings duelSettings;
            ChapterStatus chapterStatus;
            if (SoloDuels.TryGetValue(chapterId, out duelSettings) &&
                player.SoloChapters.TryGetValue(chapterId, out chapterStatus))
            {
                if (!IsPracticeDuel(chapterId))
                {
                    // NOTE: "Solo.detail" is only requested once. We might need to tell the client of updates on duel completion?
                    if (chapterStatus == ChapterStatus.COMPLETE ||
                        (player.Duel.IsMyDeck && chapterStatus == ChapterStatus.MYDECK_CLEAR) ||
                        (!player.Duel.IsMyDeck && chapterStatus == ChapterStatus.RENTAL_CLEAR))
                    {
                        duelSettings.FirstPlayer = -1;
                    }
                    else if (player.Duel.IsMyDeck)
                    {
                        // As GetSoloDuelSettings is called over multiple functions this might show a different value in different places?
                        // i.e. "XXX is going first" then a different player starts first in-game
                        duelSettings.FirstPlayer = rand.Next(2);
                    }
                }

                FileInfo customDuelFile = new FileInfo(Path.Combine(dataDirectory, "CustomDuel.json"));
                if (chapterStatus == ChapterStatus.COMPLETE && customDuelFile.Exists)
                {
                    if (CustomDuelSettings == null || customDuelFile.LastWriteTime != CustomDuelLastModified)
                    {
                        CustomDuelLastModified = customDuelFile.LastWriteTime;
                        try
                        {
                            Dictionary<string, object> duelData = MiniJSON.Json.DeserializeStripped(File.ReadAllText(customDuelFile.FullName)) as Dictionary<string, object>;
                            object deckListObj;
                            int targetChapterId;
                            if (duelData.TryGetValue("Deck", out deckListObj) &&
                                Utils.TryGetValue(duelData, "targetChapterId", out targetChapterId) &&
                                targetChapterId == chapterId)
                            {
                                List<object> deckList = deckListObj as List<object>;
                                if (deckList.Count >= 2)
                                {
                                    List<DeckInfo> decks = new List<DeckInfo>();
                                    for (int i = 0; i < deckList.Count; i++)
                                    {
                                        string deckFile = deckList[i] as string;
                                        if (deckFile != null)
                                        {
                                            string possibleFile = Path.Combine(dataDirectory, deckFile);
                                            if (!File.Exists(possibleFile))
                                            {
                                                possibleFile = Path.Combine(dataDirectory, deckFile + ".json");
                                            }
                                            if (!File.Exists(possibleFile))
                                            {
                                                possibleFile = Path.Combine(dataDirectory, deckFile + ".ydk");
                                            }
                                            if (!File.Exists(possibleFile))
                                            {
                                                foreach (DeckInfo playerDeck in player.Decks.Values)
                                                {
                                                    if (Path.GetFileNameWithoutExtension(playerDeck.File) == deckFile)
                                                    {
                                                        possibleFile = playerDeck.File;
                                                        break;
                                                    }
                                                    if (playerDeck.Name == deckFile)
                                                    {
                                                        possibleFile = playerDeck.Name;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (File.Exists(possibleFile))
                                            {
                                                DeckInfo deckInfo = new DeckInfo();
                                                deckInfo.File = possibleFile;
                                                deckInfo.Load();
                                                decks.Add(deckInfo);
                                            }
                                        }
                                    }
                                    duelData.Remove("Deck");

                                    if (decks.Count == 2)
                                    {
                                        DuelSettings customDuel = new DuelSettings();
                                        for (int i = 0; i < decks.Count && i < DuelSettings.MaxPlayers; i++)
                                        {
                                            customDuel.Deck[i] = decks[i];
                                        }
                                        customDuel.IsCustomDuel = true;
                                        customDuel.FromDictionary(duelData);
                                        customDuel.SetRequiredDefaults();
                                        customDuel.chapter = 0;
                                        CustomDuelSettings = customDuel;
                                        return customDuel;
                                    }
                                }
                                Utils.LogWarning("Failed to load custom duel");
                            }
                        }
                        catch
                        {
                        }
                    }
                    if (CustomDuelSettings != null)
                    {
                        return CustomDuelSettings;
                    }
                }
            }
            return duelSettings;
        }

        DuelSettings CreateSoloDuelSettingsInstance(Player player, int chapterId)
        {
            DuelSettings duelSettings = null;
            PlayerDuelState duel = player.Duel;
            DuelSettings ds = GetSoloDuelSettings(player, chapterId);
            if (ds != null)
            {
                duelSettings = new DuelSettings();
                duelSettings.CopyFrom(ds);
                if (SoloDisableNoShuffle)
                {
                    duelSettings.noshuffle = false;
                }
                duelSettings.SetP1ItemValue(duel.IsMyDeck, ItemID.Category.AVATAR, player.AvatarId);
                if (duel.IsMyDeck && !duelSettings.IsCustomDuel)
                {
                    DeckInfo deck = duel.GetDeck(GameMode.SoloSingle);
                    if (deck != null)
                    {
                        duelSettings.Deck[DuelSettings.PlayerIndex].CopyFrom(deck);
                        if (deck.Accessory.AvatarId > 0)
                        {
                            duelSettings.SetP1ItemValue(duel.IsMyDeck, ItemID.Category.AVATAR, deck.Accessory.AvatarId);
                        }
                        duelSettings.SetP1ItemValue(duel.IsMyDeck, ItemID.Category.AVATAR_HOME, deck.Accessory.AvBase);
                        duelSettings.SetP1ItemValue(duel.IsMyDeck, ItemID.Category.PROTECTOR, deck.Accessory.Sleeve);
                        duelSettings.SetP1ItemValue(duel.IsMyDeck, ItemID.Category.FIELD, deck.Accessory.Field);
                        duelSettings.SetP1ItemValue(duel.IsMyDeck, ItemID.Category.FIELD_OBJ, deck.Accessory.FieldObj);
                        duelSettings.story_deck_id[DuelSettings.PlayerIndex] = 0;
                        if (player.DuelBgmMode == DuelBgmMode.Myself && !duelSettings.OverrideUserBgm)
                        {
                            duelSettings.SetBgm(player.DuelBgmMode);
                        }
                    }
                }
                duelSettings.SetP1ItemValue(duel.IsMyDeck, ItemID.Category.ICON, player.IconId);
                duelSettings.SetP1ItemValue(duel.IsMyDeck, ItemID.Category.ICON_FRAME, player.IconFrameId);
                duelSettings.SetP1ItemValue(duel.IsMyDeck, ItemID.Category.WALLPAPER, player.Wallpaper);
                // EDITED
                // Randomise decks for mystery chapters.
                if (IsMysteryDuelChapter(duelSettings.chapter))
                {
                    List<KeyValuePair<int, DuelSettings>> leCampaignChapter = SoloDuels.Where(duelSettingsByChapterId => IsLeCampaignChapter(duelSettingsByChapterId.Key)).ToList();
                    List<KeyValuePair<int, DuelSettings>> leDuelistChallengeChapter = SoloDuels.Where(duelSettingsByChapterId => IsLeDuelistChallengeChapter(duelSettingsByChapterId.Key)).ToList();
                    DeckInfo playerDeck;
                    DeckInfo cpuDeck;
                    if (IsMysteryCampaignChapter(duelSettings.chapter))
                    {
                        playerDeck = leCampaignChapter.ElementAt(rand.Next(leCampaignChapter.Count)).Value.Deck[DuelSettings.PlayerIndex];
                        cpuDeck = leCampaignChapter.ElementAt(rand.Next(leCampaignChapter.Count)).Value.Deck[DuelSettings.CpuIndex];
                    } else {
                        playerDeck = leDuelistChallengeChapter.ElementAt(rand.Next(leDuelistChallengeChapter.Count)).Value.Deck[DuelSettings.CpuIndex];
                        cpuDeck = leDuelistChallengeChapter.ElementAt(rand.Next(leDuelistChallengeChapter.Count)).Value.Deck[DuelSettings.CpuIndex];
                    }
                    DeckInfo[] randomDecks = {playerDeck, cpuDeck};
                    duelSettings.SetDeck(randomDecks);
                    UpgradeLoanerDeckFinish(duelSettings, player.Cards);
                }
                // Randomise battlefield and accessories for LE and mystery chapters.
                if (IsLeChapter(duelSettings.chapter) || IsMysteryDuelChapter(duelSettings.chapter))
                {
                    ItemID.Category[] categories =
                    {
                        ItemID.Category.AVATAR,
                        ItemID.Category.ICON,
                        ItemID.Category.ICON_FRAME,
                        ItemID.Category.PROTECTOR,
                        ItemID.Category.FIELD,
                        ItemID.Category.FIELD_OBJ,
                        ItemID.Category.AVATAR_HOME,
                        ItemID.Category.WALLPAPER,
                    };
                    List<ItemID.Category> mirroredCategories = new List<ItemID.Category>
                    {
                        ItemID.Category.PROTECTOR,
                        ItemID.Category.FIELD,
                        ItemID.Category.FIELD_OBJ,
                        ItemID.Category.AVATAR_HOME,
                    };
                    if (RandomiseLEChapterBattlefield)
                    {
                        foreach (ItemID.Category category in categories)
                        {
                            int itemId = ItemID.Values[category].ElementAt(rand.Next(ItemID.Values[category].Count()));
                            if (category == ItemID.Category.AVATAR_HOME && rand.Next(ItemID.Values[category].Count() + 1) == 0)
                            {
                                itemId = 0;
                            }
                            duelSettings.SetP2ItemValue(category, itemId);
                            if (!duel.IsMyDeck && mirroredCategories.Contains(category))
                            {
                                duelSettings.SetP1ItemValue(false, category, itemId);
                            }
                        }
                    }
                    if (duel.IsMyDeck && UsePlayerBattlefieldForLEChapter)
                    {
                        foreach (ItemID.Category category in categories)
                        {
                            duelSettings.SetP2ItemValue(category, duelSettings.GetP1ItemValue(category));
                        }
                    }
                }

                UpgradeCpuDeckFinish(duelSettings, player.Cards, chapterId);
                // END EDITED
            }
            return duelSettings;
        }

        void Act_DuelBegin(GameServerWebRequest request)
        {
            Dictionary<string, object> rule;
            if (Utils.TryGetValue(request.ActParams, "rule", out rule))
            {
                PlayerDuelState duel = request.Player.Duel;
                duel.Mode = (GameMode)Utils.GetValue<int>(rule, "GameMode");
                duel.ChapterId = Utils.GetValue<int>(rule, "chapter");

                switch (duel.Mode)
                {
                    case GameMode.Room:
                        Act_DuelBeginPvp(request);
                        return;
                    case GameMode.Audience:
                        Act_RoomWatchDuel(request, true);
                        break;
                    case GameMode.Replay:
                        Act_DuelBeginReplay(request);
                        return;
                }

                DuelSettings duelSettings = null;
                Dictionary<string, object> duelStarterData = Utils.GetDictionary(rule, "duelStarterData");
                if (duelStarterData != null)
                {
                    duelSettings = new DuelSettings();
                    duelSettings.FromDictionary(duelStarterData);
                    duelSettings.IsCustomDuel = true;
                    duelSettings.SetRequiredDefaults();
                }
                else
                {
                    switch (duel.Mode)
                    {
                        case GameMode.SoloSingle:
                            duelSettings = CreateSoloDuelSettingsInstance(request.Player, duel.ChapterId);
                            break;
                    }
                }
                if (duelSettings != null)
                {
                    int firstPlayer;
                    if (Utils.TryGetValue(rule, "FirstPlayer", out firstPlayer))
                    {
                        duelSettings.FirstPlayer = firstPlayer;
                    }
                    if (duelSettings.FirstPlayer <= -1)
                    {
                        duelSettings.FirstPlayer = rand.Next(2);
                    }
                    if (!duelSettings.IsCustomDuel || string.IsNullOrEmpty(duelSettings.name[DuelSettings.PlayerIndex]))
                    {
                        // EDITED
                        if (IsLeChapter(duelSettings.chapter) && !duel.IsMyDeck && !string.IsNullOrEmpty(duelSettings.name[DuelSettings.PlayerIndex]))
                        {
                            duelSettings.SetP1Name(duel.IsMyDeck, duelSettings.name[DuelSettings.PlayerIndex]);
                        } else {
                            duelSettings.SetP1Name(duel.IsMyDeck, request.Player.Name);
                        }
                        // duelSettings.SetP1Name(duel.IsMyDeck, request.Player.Name);
                        // END EDITED
                    }
                    if (!duelSettings.IsCustomDuel || duelSettings.RandSeed == 0)
                    {
                        duelSettings.RandSeed = (uint)rand.Next();
                    }
                    if (duelSettings.bgms.Count == 0)
                    {
                        duelSettings.SetBgm(request.Player.DuelBgmMode);
                    }
                    
                    duelSettings.pcode[0] = (int)request.Player.Code;
                    duelSettings.follow_num[0] = request.Player.Friends.Count(x => x.Value.HasFlag(FriendState.Following));
                    duelSettings.follower_num[0] = request.Player.Friends.Count(x => x.Value.HasFlag(FriendState.Follower));
                    duelSettings.level[0] = request.Player.Level;
                    duelSettings.rank[0] = request.Player.Rank;
                    duelSettings.rate[0] = request.Player.Rate;

                    request.Player.ActiveDuelSettings.CopyFrom(duelSettings);
                    request.Player.ActiveDuelSettings.HasSavedReplay = false;
                    request.Player.ActiveDuelSettings.DuelBeginTime = Utils.GetEpochTime();
                    request.Response["Duel"] = duelSettings.ToDictionary();
                }
            }
            request.Remove("Duel", "DuelResult", "Result");
        }

        void Act_DuelEnd(GameServerWebRequest request)
        {
            request.Remove("Duel", "User.review", "Solo.Result", "Achievement");

            GameMode gameMode = request.Player.Duel.Mode;
            switch (gameMode)
            {
                case GameMode.Replay:
                    return;
                case GameMode.Audience:
                    ClearSpectatingDuel(request.Player);
                    return;
            }

            DuelResultType res;
            DuelFinishType finish;
            Dictionary<string, object> endParams;
            if (Utils.TryGetValue(request.ActParams, "params", out endParams) &&
                Utils.TryGetValue(endParams, "res", out res) &&
                Utils.TryGetValue(endParams, "finish", out finish))
            {
                request.Player.ActiveDuelSettings.DuelEndTime = Utils.GetEpochTime();
                request.Player.ActiveDuelSettings.res = (int)res;
                request.Player.ActiveDuelSettings.finish = (int)finish;
                request.Player.ActiveDuelSettings.turn = Utils.GetValue<int>(endParams, "turn");
                string replayData = Utils.GetValue<string>(endParams, "replayData");

                if (!string.IsNullOrEmpty(replayData) && !request.Player.ActiveDuelSettings.HasSavedReplay &&
                    DuelReplaySaveForGameModes.Contains(gameMode) && DuelReplaySaveFileLimit > 0 &&
                    gameMode != GameMode.Audience)
                {
                    request.Player.ActiveDuelSettings.replaym = replayData;
                    request.Player.ActiveDuelSettings.HasSavedReplay = true;
                    request.Player.ActiveDuelSettings.open = DuelReplayMakePublicByDefault;
                    string replaysDir = GetReplaysDirectory(request.Player);
                    Utils.TryCreateDirectory(replaysDir);
                    try
                    {
                        bool canSaveReplay = true;
                        string replayPath = Path.Combine(replaysDir, request.Player.ActiveDuelSettings.DuelBeginTime + ".json");
                        if (!File.Exists(replayPath))
                        {
                            List<string> replays = Directory.GetFiles(replaysDir, "*.json", SearchOption.TopDirectoryOnly).ToList();

                            // Auto saved replays are files where their name is number
                            long temp;
                            FileInfo[] autoSavedReplaysByCreationDate = replays
                                .Select(filePath => new FileInfo(filePath))
                                .Where(fileInfo => long.TryParse(Path.GetFileNameWithoutExtension(fileInfo.Name), out temp))
                                .OrderBy(x => x.CreationTimeUtc)
                                .ToArray();

                            // Delete auto saved replays when we reach the save limit for auto saved replays
                            if (autoSavedReplaysByCreationDate.Length >= DuelReplaySaveFileLimit)
                            {
                                canSaveReplay = false;
                                int deletedReplays = 0;
                                foreach (FileInfo replayFileInfo in autoSavedReplaysByCreationDate)
                                {
                                    try
                                    {
                                        try
                                        {
                                            // TODO: Cache this data so that we don't have to keep reopening all replay files when the player reaches their limit
                                            DuelSettings duelSettings = new DuelSettings();
                                            duelSettings.FromDictionary(MiniJSON.Json.Deserialize(File.ReadAllText(replayFileInfo.FullName)) as Dictionary<string, object>);
                                            if (duelSettings.IsReplayLocked)
                                            {
                                                continue;
                                            }
                                        }
                                        catch
                                        {
                                        }

                                        replayFileInfo.Delete();
                                        deletedReplays++;
                                        if (replays.Count - deletedReplays < DuelReplaySaveFileLimit)
                                        {
                                            canSaveReplay = true;
                                            break;
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        }
                        if (canSaveReplay)
                        {
                            File.WriteAllText(replayPath, MiniJSON.Json.Serialize(request.Player.ActiveDuelSettings.ToDictionary()));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to save replay data. Error: " + e);
                    }
                }

                switch (gameMode)
                {
                    case GameMode.SoloSingle:
                        bool chapterStatusChanged = false;
                        if (request.Player.Duel.ChapterId != 0 && res != DuelResultType.None && !request.Player.Duel.IsCustomSoloDuel)
                        {
                            ChapterStatus oldChapterStatus;
                            request.Player.SoloChapters.TryGetValue(request.Player.Duel.ChapterId, out oldChapterStatus);
                            SoloUpdateChapterStatus(request, request.Player.Duel.ChapterId, res, finish);
                            ChapterStatus newChapterStatus;
                            request.Player.SoloChapters.TryGetValue(request.Player.Duel.ChapterId, out newChapterStatus);
                            chapterStatusChanged = oldChapterStatus != newChapterStatus;
                        }
                        GiveDuelReward(request, request.Player, DuelRewards, res, finish, chapterStatusChanged);
                        SavePlayer(request.Player);
                        break;

                    case GameMode.Room:
                        NetClient opponentClient = null;
                        Player opponentPlayer = GetDuelingOpponent(request.Player);
                        if (opponentPlayer != null)
                        {
                            opponentClient = opponentPlayer.NetClient;
                        }
                        DuelRoom duelRoom = request.Player.DuelRoom;
                        if (duelRoom == null)
                        {
                            return;
                        }
                        DuelRoomTable duelRoomTable = duelRoom.GetTable(request.Player);
                        if (duelRoomTable == null)
                        {
                            return;
                        }

                        DuelResultType opponentResult;
                        switch (res)
                        {
                            case DuelResultType.Win: opponentResult = DuelResultType.Lose; break;
                            case DuelResultType.Lose: opponentResult = DuelResultType.Win; break;
                            case DuelResultType.Draw: opponentResult = DuelResultType.Draw; break;
                            default: return;
                        }

                        if (duelRoom.ViewReplays)
                        {
                            lock (duelRoom.Replays)
                            {
                                if (duelRoomTable.Replay == null)
                                {
                                    duelRoomTable.Replay = new DuelRoomReplay();
                                }
                                if (!duelRoomTable.Replay.IsComplete)
                                {
                                    duelRoomTable.Replay.AddReplay(request.Player);
                                    if (duelRoomTable.Replay.IsComplete)
                                    {
                                        duelRoom.AddReplay(duelRoomTable.Replay);
                                    }
                                }
                            }
                        }

                        lock (duelRoomTable.Rewards)
                        {
                            if (duelRoomTable.Rewards.Player1Rewards == null && duelRoomTable.Rewards.Player2Rewards == null)
                            {
                                GiveDuelReward(request, request.Player, DuelRoomRewards, res, DuelFinishType.None, false);
                                duelRoomTable.Rewards.Player1 = request.Player;
                                duelRoomTable.Rewards.Player1Rewards = request.Response;
                                request.Response = new Dictionary<string, object>();

                                GiveDuelReward(request, opponentPlayer, DuelRoomRewards, opponentResult, DuelFinishType.None, false);
                                duelRoomTable.Rewards.Player2 = opponentPlayer;
                                duelRoomTable.Rewards.Player2Rewards = request.Response;
                                request.Response = new Dictionary<string, object>();

                                UpdateUnlockedSecretsForCompletedDuels(request.Player, res, finish);
                                UpdateUnlockedSecretsForCompletedDuels(opponentPlayer, opponentResult, finish);

                                SavePlayerNow(request.Player);
                                SavePlayerNow(opponentPlayer);

                                DuelRoomRecord playerDuelRoomRecords;
                                if (duelRoom.Members.TryGetValue(request.Player, out playerDuelRoomRecords))
                                {
                                    switch (res)
                                    {
                                        case DuelResultType.Win: playerDuelRoomRecords.Win++; break;
                                        case DuelResultType.Lose: playerDuelRoomRecords.Loss++; break;
                                        case DuelResultType.Draw: playerDuelRoomRecords.Draw++; break;
                                    }
                                }

                                DuelRoomRecord opponentDuelRoomRecords;
                                if (duelRoom.Members.TryGetValue(opponentPlayer, out opponentDuelRoomRecords))
                                {
                                    switch (res)
                                    {
                                        case DuelResultType.Win: opponentDuelRoomRecords.Loss++; break;
                                        case DuelResultType.Lose: opponentDuelRoomRecords.Win++; break;
                                        case DuelResultType.Draw: opponentDuelRoomRecords.Draw++; break;
                                    }
                                }
                            }

                            if (request.Player == duelRoomTable.Rewards.Player1)
                            {
                                request.Response = duelRoomTable.Rewards.Player1Rewards;
                            }
                            else if (request.Player == duelRoomTable.Rewards.Player2)
                            {
                                request.Response = duelRoomTable.Rewards.Player2Rewards;
                            }

                            if (res == DuelResultType.Lose && (finish == DuelFinishType.Surrender || finish == DuelFinishType.TimeOut))
                            {
                                lock (duelRoomTable.Spectators)
                                {
                                    byte finn = (byte)finish;

                                    DuelSpectatorDataMessage message = new DuelSpectatorDataMessage();
                                    if (request.Player == duelRoomTable.Player1)
                                    {
                                        message.Buffer = new byte[]
                                        {
                                            0x22, 0x00, finn, 0x00, 0x00, 0x00, 0x00, 0x00,
                                            0x05, 0x00, 0x02, 0x00, 0x00, 0x00, finn, 0x00
                                        };
                                    }
                                    else
                                    {
                                        message.Buffer = new byte[]
                                        {
                                            0x22, 0x80, finn, 0x00, 0x00, 0x00, 0x00, 0x00,
                                            0x05, 0x00, 0x01, 0x00, finn, 0x00, 0x00, 0x00
                                        };
                                    }

                                    Player p1 = duelRoomTable.Player1;
                                    uint p1Code = p1 == null ? 0 : p1.Code;
                                    if (p1Code != 0)
                                    {
                                        duelRoomTable.SpectatorData.AddRange(message.Buffer);
                                        foreach (Player spectator in new HashSet<Player>(duelRoomTable.Spectators))
                                        {
                                            NetClient spectatorClient = spectator.NetClient;
                                            if (spectatorClient != null && spectator.SpectatingPlayerCode == p1Code)
                                            {
                                                spectatorClient.Send(message);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (opponentClient != null)
                        {
                            opponentClient.Send(new OpponentDuelEndedMessage()
                            {
                                Result = res,
                                Finish = finish
                            });

                            NetClient pvpClient = duelRoomTable.PvpClient;
                            if (pvpClient != null)
                            {
                                pvpClient.Send(new OpponentDuelEndedMessage());
                            }
                        }
                        break;
                }
            }
        }

        void GiveDuelReward(GameServerWebRequest request, Player player, DuelRewardInfos rewards, DuelResultType result, DuelFinishType finishType, bool chapterStatusChanged)
        {
            Player temp = request.Player;
            request.Player = player;
            GiveDuelRewardImpl(request, rewards, result, finishType, chapterStatusChanged);
            request.Player = temp;
        }

        void GiveDuelRewardImpl(GameServerWebRequest request, DuelRewardInfos rewards, DuelResultType result, DuelFinishType finishType, bool chapterStatusChanged)
        {
            int turn = 0;
            Dictionary<string, object> endParams;
            if (Utils.TryGetValue(request.ActParams, "params", out endParams))
            {
                turn = Utils.GetValue<int>(endParams, "turn");
            }

            int minimumTurnsForSurrenderRewards = 0;
            switch (result)
            {
                case DuelResultType.Win: minimumTurnsForSurrenderRewards = rewards.MinimumTurnsForSurrenderRewardsWin; break;
                case DuelResultType.Lose: minimumTurnsForSurrenderRewards = rewards.MinimumTurnsForSurrenderRewardsLose; break;
                case DuelResultType.Draw: minimumTurnsForSurrenderRewards = rewards.MinimumTurnsForSurrenderRewardsDraw; break;
            }

            if ((rewards.Win.Count == 0 && rewards.Lose.Count == 0 && rewards.Draw.Count == 0) ||
                (rewards.ChapterStatusChangedNoRewards && chapterStatusChanged) ||
                (rewards.ChapterStatusChangedOnly && !chapterStatusChanged) ||
                (turn >= minimumTurnsForSurrenderRewards && finishType == DuelFinishType.Surrender))
            {
                return;
            }

            request.Remove("Duel", "Solo.Result");

            Dictionary<string, object> duel = request.GetOrCreateDictionary("Duel");
            duel["result"] = 1;
            Dictionary<string, object> duelResult = request.GetOrCreateDictionary("DuelResult");
            duelResult["mode"] = (int)GameMode.Normal;// Use anything other than SoloSingle (as it only shows level / exp)
            Dictionary<string, object> duelResultInfo = Utils.GetOrCreateDictionary(duelResult, "resultInfo");
            duelResultInfo["result"] = 1;
            if (request.Player.ActiveDuelSettings != null)
            {
                int playerIndex = request.Player.ActiveDuelSettings.pcode[0] == request.Player.Code || request.Player.ActiveDuelSettings.pcode[0] == 0 ? 0 : 1;
                int avatarId = request.Player.ActiveDuelSettings.avatar[playerIndex];
                if (avatarId > 0)
                {
                    duelResultInfo["avatar"] = avatarId;
                }
            }
            Dictionary<string, object> duelScoreInfo = Utils.GetOrCreateDictionary(duelResultInfo, "scoreInfo");
            Dictionary<string, object> duelScore = Utils.GetOrCreateDictionary(duelScoreInfo, "score");
            int duelScoreTotal = Utils.GetValue<int>(duelScore, "total");
            List<object> duelRewards = Utils.GetOrCreateList(duelScoreInfo, "rewards");

            // 1 = blue box, 2 = gold box, 3 = breaks client (no back button)
            const int blueBox = 1;
            const int goldBox = 2;
            const int duelScoreRewardValue = 1000;

            List<DuelRewardInfo> rewardInfos;
            switch (result)
            {
                case DuelResultType.Win: rewardInfos = rewards.Win; break;
                case DuelResultType.Lose: rewardInfos = rewards.Lose; break;
                case DuelResultType.Draw: rewardInfos = rewards.Draw; break;
                default: return;
            }

            foreach (DuelRewardInfo reward in rewardInfos)
            {
                switch (reward.Type)
                {
                    case DuelCustomRewardType.Gem:
                        {
                            double randValue = rand.NextDouble() * 100;
                            if (reward.Rate >= randValue)
                            {
                                int amount = rewards.GetAmount(rand, reward, chapterStatusChanged);
                                if (amount == 0)
                                {
                                    continue;
                                }
                                request.Player.Gems += amount;
                                WriteItem(request, (int)ItemID.Value.Gem);
                                duelScoreTotal += duelScoreRewardValue;
                                duelRewards.Add(new Dictionary<string, object>()
                                {
                                    { "type", reward.Rare ? goldBox : blueBox },
                                    { "category", (int)ItemID.Category.CONSUME },
                                    { "item_id", (int)ItemID.Value.Gem },
                                    { "num", amount },
                                    { "is_prize", true },
                                });
                            }
                        }
                        break;
                    case DuelCustomRewardType.Item:
                        {
                            double randValue = rand.NextDouble() * 100;
                            if (reward.Rate >= randValue)
                            {
                                // EDITED
                                int rewardCount = 1;
                                if (reward.Ids == null)
                                {
                                    double rng = rand.NextDouble();
                                    if (rng < 0.03)
                                    {
                                        while (rand.NextDouble() < 0.9) { rewardCount++; }
                                    }
                                    else if (rng < 0.1)
                                    {
                                        while (rand.NextDouble() < 0.75) { rewardCount++; }
                                    }
                                }
                                for (int i = 0; i < rewardCount; i++)
                                {
                                // END EDITED
                                // EDITED (indent)
                                    HashSet<int> unownedIds = new HashSet<int>();
                                    if (reward.Ids != null && reward.Ids.Count > 0)
                                    {
                                        foreach (int id in reward.Ids)
                                        {
                                            if (ItemID.GetCategoryFromID(id) == ItemID.Category.CONSUME || !request.Player.Items.Contains(id))
                                            {
                                                unownedIds.Add(id);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Also see LoadPlayer which does the same thing (loads all items)
                                        ItemID.Category[] categories =
                                        {
                                            ItemID.Category.AVATAR,
                                            ItemID.Category.ICON,
                                            ItemID.Category.ICON_FRAME,
                                            ItemID.Category.PROTECTOR,
                                            ItemID.Category.DECK_CASE,
                                            ItemID.Category.FIELD,
                                            ItemID.Category.FIELD_OBJ,
                                            ItemID.Category.AVATAR_HOME,
                                            ItemID.Category.WALLPAPER,
                                        };
                                        foreach (ItemID.Category category in categories)
                                        {
                                            foreach (int id in ItemID.Values[category])
                                            {
                                                // EDITED
                                                // if (!request.Player.Items.Contains(id))
                                                // {
                                                //     unownedIds.Add(id);
                                                // }
                                                if (request.Player.Items.Contains(id))
                                                {
                                                    continue;
                                                }
                                                // Exclude:
                                                // 1. Items sold in shop (including the mate's base and field parts given upon purchasing the corresponding fields).
                                                // 2. Deck cases included with structure decks.
                                                // 3. Items available as specific chapter rewards.
                                                List<int> excludedItemIds = new List<int>{1000002,1000003,1000004,1000005,1000006,1000007,1000008,1000009,1000010,1000011,1000013,1000014,1000015,1000016,1000017,1000018,1000019,1000020,1000022,1000023,1000024,1000026,1000027,1000028,1000029,1000030,1000031,1000032,1000034,1000041,1001003,1001004,1001013,1001018,1001019,1001020,1001024,1001025,1001026,1003001,1003002,1010005,1010009,1010011,1010012,1010013,1010014,1010016,1010017,1010018,1010019,1010020,1010022,1010023,1010024,1010025,1010026,1010027,1010029,1010031,1010032,1010034,1010035,1010037,1010041,1010044,1010045,1010047,1010048,1010050,1010051,1010053,1010059,1010066,1010068,1010071,1010073,1010074,1010076,1010077,1010087,1010088,1010089,1010092,1010094,1010095,1010097,1010098,1012001,1012002,1012003,1012004,1012005,1012006,1012007,1012008,1012009,1012010,1012011,1012012,1012013,1012014,1012015,1012016,1012017,1012018,1012019,1012020,1012021,1012022,1012023,1012024,1012025,1012026,1012027,1012028,1012029,1012030,1012031,1012032,1012033,1012034,1012036,1012037,1012038,1012040,1012041,1012042,1012043,1012044,1012045,1012046,1012047,1012048,1012050,1012051,1012052,1012053,1012054,1012055,1012056,1012057,1012058,1012061,1012062,1012063,1012064,1012065,1012066,1012067,1012068,1012069,1012070,1030014,1030015,1030016,1030017,1030018,1030019,1030030,1030031,1031001,1031002,1031003,1031005,1031006,1031008,1031009,1031011,1031013,1031014,1032001,1032002,1032003,1032004,1032005,1032007,1070002,1070003,1070007,1070008,1070009,1070010,1070011,1070012,1070013,1070014,1070015,1070016,1070017,1070018,1070019,1070022,1070023,1070024,1070025,1070026,1070027,1070028,1070029,1070032,1070033,1070034,1070035,1070036,1070037,1070038,1070041,1070043,1070044,1070045,1070047,1070048,1070049,1070050,1070051,1070052,1070054,1070055,1070056,1070057,1070058,1070062,1070063,1070064,1070065,1070066,1070067,1070069,1070073,1070074,1070075,1070080,1070081,1070082,1070083,1070084,1070085,1070086,1070087,1070091,1070092,1070093,1070098,1070099,1070103,1070105,1070106,1070107,1070108,1070109,1070110,1070111,1071001,1071002,1071003,1071006,1071007,1071008,1071009,1071010,1071011,1071012,1071013,1071014,1071015,1071016,1071017,1071018,1071019,1071020,1071021,1071022,1071023,1071024,1071025,1071026,1071027,1071028,1071029,1071030,1071031,1071032,1071033,1072001,1072002,1072003,1080005,1080006,1080007,1080008,1080009,1080010,1080011,1080012,1080013,1080014,1080015,1080016,1080018,1080019,1080020,1080021,1080022,1080023,1080024,1080025,1080026,1080027,1080028,1082001,1082004,1082005,1082006,1082007,1082008,1082009,1082010,1082014,1082020,1090002,1090003,1090004,1090005,1090006,1090007,1090008,1090009,1090010,1090011,1090012,1090013,1090014,1090015,1090016,1090017,1090019,1090020,1090022,1090023,1090024,1090030,1090031,1100002,1100003,1100004,1100005,1100006,1100007,1100008,1100009,1100010,1100011,1100012,1100013,1100014,1100015,1100016,1100017,1100019,1100020,1100022,1100023,1100024,1100030,1100031,1101001,1101002,1101003,1101009,1101010,1101012,1101013,1101014,1101017,1101019,1101025,1101032,1110002,1110003,1110004,1110005,1110006,1110007,1110008,1110009,1110010,1110011,1110012,1110013,1110014,1110015,1110016,1110017,1110019,1110020,1110022,1110023,1110024,1110030,1110031,1111001,1111002,1111003,1111005,1111006,1111007,1111011,1111012,1111013,1111014,1111015,1111016,1111019,1111021,1111022,1111024,1111026,1111039,1111041,1111043,1111045,1111048,1111057,1130003,1130005,1130015,1130016,1130018,1130020,1130021,1130022,1130025,1130026,1130027,1130028,1130029,1130031,1130035,1130038,1130041,1130042,1130045,1130046,1130048,1130049,1130051,1130052,1130056,1130058,1130059,1130064,1130066,1130067,1130068,1130070,1130071,1130072};
                                                if (excludedItemIds.Contains(id))
                                                {
                                                    continue;
                                                }
                                                unownedIds.Add(id);
                                                // END EDITED
                                            }
                                        }
                                    }
                                    if (unownedIds.Count > 0)
                                    {
                                        int amount = 1;
                                        int id = unownedIds.ElementAt(rand.Next(unownedIds.Count));
                                        if (ItemID.GetCategoryFromID(id) == ItemID.Category.STRUCTURE)
                                        {
                                            GiveStructureDeck(request, id);
                                        }
                                        else if (ItemID.GetCategoryFromID(id) == ItemID.Category.CONSUME)
                                        {
                                            amount = rewards.GetAmount(rand, reward, chapterStatusChanged);
                                            if (amount == 0)
                                            {
                                                continue;
                                            }
                                            request.Player.AddItem(id, amount);
                                        }
                                        else
                                        {
                                            request.Player.Items.Add(id);
                                            WriteItem(request, id);
                                        }
                                        duelScoreTotal += duelScoreRewardValue;
                                        duelRewards.Add(new Dictionary<string, object>()
                                        {
                                            { "type", reward.Rare ? goldBox : blueBox },
                                            { "category", (int)ItemID.GetCategoryFromID(id) },
                                            { "item_id", id },
                                            { "num", amount },
                                            { "is_prize", true },
                                        });
                                    }
                                // END EDITED (indent)
                                // EDITED
                                }
                                // END EDITED
                            }
                        }
                        break;
                    case DuelCustomRewardType.Card:
                        {
                            PlayerCardKind dismantle = reward.CardNoDismantle && !DisableNoDismantle ? PlayerCardKind.NoDismantle : PlayerCardKind.Dismantle;
                            int numCards = Math.Max(1, Math.Min(reward.MinValue, reward.MaxValue));
                            double randValue = rand.NextDouble() * 100;
                            if (reward.Rate >= randValue)
                            {
                                if (reward.Ids != null && reward.Ids.Count > 0)
                                {
                                    HashSet<int> cardIds = new HashSet<int>();
                                    foreach (int id in reward.Ids)
                                    {
                                        if (reward.CardOwnedLimit == 0 || reward.CardOwnedLimit > request.Player.Cards.GetCount(id))
                                        {
                                            cardIds.Add(id);
                                        }
                                    }
                                    if (cardIds.Count > 0)
                                    {
                                        int cardId = cardIds.ElementAt(rand.Next(cardIds.Count));
                                        request.Player.Cards.Add(cardId, numCards, dismantle, CardStyleRarity.Normal);
                                        WriteCards_have(request, cardId);
                                        duelScoreTotal += duelScoreRewardValue;
                                        duelRewards.Add(new Dictionary<string, object>()
                                        {
                                            { "type", reward.Rare ? goldBox : blueBox },
                                            { "category", (int)ItemID.Category.CARD },
                                            { "item_id", cardId },
                                            { "num", numCards },
                                            { "is_prize", true },
                                        });
                                    }
                                }
                                else if (reward.CardRate != null && reward.CardRate.Count > 0)
                                {
                                    Dictionary<CardRarity, double> accumaltiveRate = new Dictionary<CardRarity, double>();
                                    Dictionary<CardRarity, bool> rare = new Dictionary<CardRarity, bool>();
                                    double totalRate = 0;
                                    for (int i = 0; i < reward.CardRate.Count; i++)
                                    {
                                        totalRate += reward.CardRate[i];
                                        accumaltiveRate[(CardRarity)i + 1] = totalRate;
                                        if (reward.CardRare != null && reward.CardRare.Count > i)
                                        {
                                            rare[(CardRarity)i + 1] = reward.CardRare[i];
                                        }
                                    }
                                    double cardRate = rand.NextDouble() * 100;
                                    CardRarity cardRarity = CardRarity.None;
                                    bool isRare = reward.Rare;
                                    foreach (KeyValuePair<CardRarity, double> rate in accumaltiveRate.OrderBy(x => x.Key))
                                    {
                                        if (cardRate < rate.Value)
                                        {
                                            cardRarity = rate.Key;
                                            if (rare.ContainsKey(rate.Key))
                                            {
                                                isRare = rare[rate.Key];
                                            }
                                            break;
                                        }
                                    }
                                    if (cardRarity != CardRarity.None)
                                    {
                                        Dictionary<int, int> cardRare = GetCardRarities(request.Player);
                                        List<int> cardIds = new List<int>();
                                        foreach (KeyValuePair<int, int> card in cardRare)
                                        {
                                            if (card.Value == (int)cardRarity && (reward.CardOwnedLimit == 0 ||
                                                reward.CardOwnedLimit > request.Player.Cards.GetCount(card.Key)))
                                            {
                                                cardIds.Add(card.Key);
                                            }
                                        }
                                        if (cardIds.Count > 0)
                                        {
                                            int cardId = cardIds[rand.Next(cardIds.Count)];
                                            request.Player.Cards.Add(cardId, numCards, dismantle, CardStyleRarity.Normal);
                                            WriteCards_have(request, cardId);
                                            duelScoreTotal += duelScoreRewardValue;
                                            duelRewards.Add(new Dictionary<string, object>()
                                            {
                                                { "type", isRare ? goldBox : blueBox },
                                                { "category", (int)ItemID.Category.CARD },
                                                { "item_id", cardId },
                                                { "num", numCards },
                                                { "is_prize", true },
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            duelScore["total"] = duelScoreTotal;
            // EDITED
            // Force inventory reload so that gems and items amounts are updated immediately. 
            request.Response["Item"] = new Dictionary<string, object>()
            {
                { "have", GetItemHaveDictionary(request.Player) },
            };
            // END EDITED
        }

        void UpdateUnlockedSecretsForCompletedDuels(Player player, DuelResultType result, DuelFinishType finishType)
        {
            if (finishType == DuelFinishType.Surrender)
            {
                return;
            }
            player.ShopState.DuelsCompletedForNextSecretUnlock++;
            foreach (ShopItemInfo shopItem in Shop.AllShops.Values.OrderBy(x => x.ShopId))
            {
                if (shopItem.UnlockSecrets.Count > 0 && shopItem.UnlockSecretsAtNumDuels > 0 &&
                    player.ShopState.GetAvailability(Shop, shopItem) == PlayerShopItemAvailability.Available &&
                    !shopItem.HasUnlockedAllSecrets(player, Shop))
                {
                    shopItem.DoUnlockSecrets(player, Shop);
                    break;
                }
            }
        }
    }
}
